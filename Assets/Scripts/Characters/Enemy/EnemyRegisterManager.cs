using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon;
using RogueDungeon.Dungeon.Config;
using RogueDungeon.Dungeon.Generation;
using RogueDungeon.Dungeon.Map;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Dungeon.View;
using RogueDungeon.Core.Pool;

/// <summary>
/// 敌人注册管理器。DDOL 单例，管理房间→敌人映射、敌人生成/激活/死亡检测。
///
/// 逻辑链：
///   进入未清理房间 → 预生成相邻房间敌人（取消激活）→ 激活当前房间敌人
///   所有敌人死亡 / 事件房间 → 广播 RoomCleared
/// </summary>
public class EnemyRegisterManager : MonoBehaviour
{
    public static EnemyRegisterManager Instance { get; private set; }

    public const string EnemyPoolKey = "Enemy";

    [Header("生成约束（与掉落物同步）")]
    [Min(0f)]
    [SerializeField] private float cellEdgePadding = 2f;
    [Min(0f)]
    [SerializeField] private float enemyRadius = 0.5f;
    [Min(0.5f)]
    [SerializeField] private float minSpawnDistance = 1.5f;
    [Min(0f)]
    [SerializeField] private float playerAvoidRadius = 1f;
    [Min(1)]
    [SerializeField] private int maxPlacementAttempts = 30;

    /// <summary>房间 ID → 属于该房间的敌人列表</summary>
    private readonly Dictionary<string, List<Enemy>> _roomEnemies = new();

    /// <summary>敌人实例 ID → 所属房间 ID（反向索引，O(1) 死亡时查找）</summary>
    private readonly Dictionary<int, string> _enemyRoom = new();

    /// <summary>已生成过敌人的房间（防止重复生成）</summary>
    private readonly HashSet<string> _generatedRooms = new();

    private DungeonMap _currentMap;
    private FloorConfigSO _currentFloorConfig;
    private GameObject[] _enemyPrefabs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnregisterEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RegisterEvents();

    private void RegisterEvents()
    {
        UnregisterEvents();
        EventCenter.AddListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        EventCenter.AddListener<EnemyDiedEvent>(GameEventType.EnemyDied, OnEnemyDied);
    }

    private void UnregisterEvents()
    {
        EventCenter.RemoveListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        EventCenter.RemoveListener<RoomEnteredEvent>(GameEventType.RoomEntered, OnRoomEntered);
        EventCenter.RemoveListener<EnemyDiedEvent>(GameEventType.EnemyDied, OnEnemyDied);
    }

    /// <summary>
    /// 地牢就绪：加载配置 → 注册 RoomEntered → 处理当前房间。
    /// RoomEntered 延迟到初始化完成后才注册，避免被 DungeonReady 同步触发的 RoomEntered 抢先执行。
    /// </summary>
    private void OnDungeonReady(DungeonReadyEvent evt)
    {
        var dm = DungeonManager.Instance;
        if (dm == null || dm.CurrentMap == null) return;

        _currentMap = dm.CurrentMap;
        _currentFloorConfig = GetFloorConfig();

        if (_currentFloorConfig == null) return;

        _enemyPrefabs = _currentFloorConfig.EnemyPrefabs;
        if (_enemyPrefabs == null || _enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnemyRegisterManager] FloorConfig has no enemyPrefabs");
            return;
        }

        ClearAll();

        // 初始化完成后才注册 RoomEntered，确保不会先于本方法执行
        EventCenter.AddListener<RoomEnteredEvent>(GameEventType.RoomEntered, OnRoomEntered);

        // 处理已设置为 CurrentRoom 的起始房间
        var currentRoom = dm.CurrentRoom;
        if (currentRoom != null)
        {
            OnRoomEntered(new RoomEnteredEvent { Room = currentRoom });
        }
    }

    /// <summary>
    /// 进入房间：
    ///   1. 事件房间 → 立即标记已清理，广播 RoomCleared
    ///   2. 未清理房间 → 激活当前房间敌人 + 为相邻未清理房间预生成敌人
    /// </summary>
    private void OnRoomEntered(RoomEnteredEvent evt)
    {
        if (evt.Room == null) return;

        // 事件房间：立即清理
        if ((evt.Room.Type == RoomType.Event) && !evt.Room.Cleared)
        {
            evt.Room.Cleared = true;
            EventCenter.Broadcast(GameEventType.RoomCleared, new RoomClearedEvent
            {
                RoomId = evt.Room.Id,
                ElapsedTime = 0f
            });
            return;
        }

        if (evt.Room.Cleared) return;

        // 为相邻未清理房间预生成敌人
        evt.Room.TryGetNeighboringRooms(out var neighbors);
        foreach (var neighborId in neighbors)
        {
            var neighborRoom = _currentMap?.GetRoom(neighborId);
            if (neighborRoom != null && !neighborRoom.Cleared)
                GenerateEnemiesForRoom(neighborRoom);
        }

        if (evt.Room.Type == RoomType.Start)
        {
            evt.Room.Cleared = true;
            EventCenter.Broadcast(GameEventType.RoomCleared, new RoomClearedEvent
            {
                RoomId = evt.Room.Id,
                ElapsedTime = 0f
            });
            return;
        }

        // 激活当前房间敌人
        ActivateEnemiesInRoom(evt.Room.Id);
    }

    /// <summary>
    /// 敌人死亡：从注册表移除，检查房间是否清空。
    /// </summary>
    private void OnEnemyDied(EnemyDiedEvent evt)
    {
        if (!_enemyRoom.TryGetValue(evt.EnemyInstanceID, out var roomId)) return;

        _enemyRoom.Remove(evt.EnemyInstanceID);

        if (_roomEnemies.TryGetValue(roomId, out var list))
        {
            // 清理已销毁的敌人引用
            list.RemoveAll(e => e == null || e.gameObject.GetInstanceID() == evt.EnemyInstanceID);
            if (list.Count == 0)
            {
                _roomEnemies.Remove(roomId);
                OnRoomCleared(roomId);
            }
        }
    }

    private void OnRoomCleared(string roomId)
    {
        var room = _currentMap?.GetRoom(roomId);
        if (room == null) return;

        room.Cleared = true;

        EventCenter.Broadcast(GameEventType.RoomCleared, new RoomClearedEvent
        {
            RoomId = roomId,
            ElapsedTime = 0f
        });
    }

    /// <summary>
    /// 为指定房间生成敌人（取消激活状态）。
    /// </summary>
    private void GenerateEnemiesForRoom(RoomInstance room)
    {
        if (_generatedRooms.Contains(room.Id)) return;

        int enemyCount = GetEnemyCountForRoom(room);
        if (enemyCount <= 0 || _enemyPrefabs == null || _enemyPrefabs.Length == 0)
        {
            _generatedRooms.Add(room.Id);
            return;
        }

        var run = RunManager.Instance?.CurrentRun;
        int seed = run != null ? SeededRandom.Hash(run.Seed, room.Id) : 0;
        var rng = new SeededRandom(seed);
        var spawnPositions = GetSpawnPositions(room, enemyCount, rng);

        var enemyList = new List<Enemy>(enemyCount);
        for (int i = 0; i < enemyCount; i++)
        {
            var prefab = _enemyPrefabs[rng.Range(0, _enemyPrefabs.Length)];
            if (prefab == null) continue;

            var go = ObjectPool.Instance.Get(EnemyPoolKey, prefab);
            go.transform.position = spawnPositions[i];
            go.transform.rotation = Quaternion.identity;// 挂载到房间视图下，由房间的迷雾系统统一控制显隐

            var enemy = go.GetComponent<Enemy>();
            if (enemy == null)
            {
                ObjectPool.Instance.Release(EnemyPoolKey, go);
                continue;
            }
            go.SetActive(false);
            enemyList.Add(enemy);

            // 复用对象可能残留旧房间的映射，先清理后再指向新房间
            int instanceId = go.GetInstanceID();
            if (_enemyRoom.TryGetValue(instanceId, out var oldRoomId) && oldRoomId != room.Id)
            {
                if (_roomEnemies.TryGetValue(oldRoomId, out var oldList))
                    oldList.Remove(enemy);
            }
            _enemyRoom[instanceId] = room.Id;
        }

        _roomEnemies[room.Id] = enemyList;
        _generatedRooms.Add(room.Id);
        Debug.Log($"[EnemyRegisterManager] 为房间 {room.Id} 生成 {enemyList.Count} 个敌人（取消激活）, 位置分别在： {string.Join(", ", spawnPositions)}");
    }

    /// <summary>
    /// 激活指定房间的所有敌人。
    /// </summary>
    private void ActivateEnemiesInRoom(string roomId)
    {
        if (!_roomEnemies.TryGetValue(roomId, out var list)) return;

        int activated = 0;
        foreach (var enemy in list)
        {
            if (enemy == null) continue;
            enemy.gameObject.SetActive(true);
            activated++;
        }

        Debug.Log($"[EnemyRegisterManager] 房间 {roomId} 激活 {activated} 个敌人");
    }
    private static RoomView GetRoomView(string roomId)
        {
            var viewManager = Object.FindFirstObjectByType<DungeonViewManager>();
            if (viewManager != null && viewManager.TryGetRoomView(roomId, out var view))
                return view;
            return null;
        }

    /// <summary>
    /// 根据房间形状从楼层配置获取敌人数量。
    /// </summary>
    private int GetEnemyCountForRoom(RoomInstance room)
    {
        if (_currentFloorConfig?.ShapeWeights == null) return 0;

        foreach (var sw in _currentFloorConfig.ShapeWeights)
        {
            if (sw.shape == room.Shape)
                return sw.enemyCount;
        }
        return 0;
    }

    /// <summary>
    /// 在房间 Cell 中随机生成不重叠的敌人生成位置，约束与掉落物一致。
    /// </summary>
    private List<Vector3> GetSpawnPositions(RoomInstance room, int count, SeededRandom rng)
    {
        var positions = new List<Vector3>(count);

        if (room.Cells == null || room.Cells.Count == 0)
            return positions;

        if (RunManager.Instance?.CurrentRun == null)
            return positions;

        // 先构建所有 cell 的可用区域，再统一随机放置，保证敌人分布在全部 cell
        float size = DungeonViewManager.CellWorldSize;
        var cellRects = new List<Rect>(room.Cells.Count);
        for (int i = 0; i < room.Cells.Count; i++)
        {
            var r = new Rect(
                room.Cells[i].x * size,
                room.Cells[i].y * size,
                size, size);
            float pad = Mathf.Min(cellEdgePadding + enemyRadius, r.width * 0.45f);
            cellRects.Add(new Rect(r.x + pad, r.y + pad, r.width - 2 * pad, r.height - 2 * pad));
        }

        positions = TryPlacePosition(cellRects, count, rng, positions);
        if (positions.Count < count)
        {
            Debug.LogWarning($"[EnemyRegisterManager] 房间 {room.Id} 生成敌人位置不足: 目标 {count}, 实际 {positions.Count}");
        }
        return positions;
    }

    private List<Vector3> TryPlacePosition(
            List<Rect> cellRects,
            int targetCount,
            SeededRandom rng,
            List<Vector3> existingPositions)
    {
        var positions = existingPositions != null
            ? new List<Vector3>(existingPositions)
            : new List<Vector3>(targetCount);
        
        int maxAttempts = maxPlacementAttempts * targetCount;
        for (int attempt = 0; attempt < maxAttempts && positions.Count < targetCount; attempt++)
        {
            // 随机选一个 cell
            int cellIndex = rng.Range(0, cellRects.Count);
            Rect rect = cellRects[cellIndex];

            // 在 cell 内随机选一个点
            float x = Mathf.Lerp(rect.xMin, rect.xMax, rng.Value);
            float y = Mathf.Lerp(rect.yMin, rect.yMax, rng.Value);
            var candidate = new Vector3(x, y, 0f);

            // 检查与现有点的距离
            bool valid = true;
            float minDistSqr = minSpawnDistance * minSpawnDistance;
            for (int i = 0; i < positions.Count; i++)
                if ((candidate - positions[i]).sqrMagnitude < minDistSqr) {valid = false; break;}
            if (valid) positions.Add(candidate);
        }
        return positions;
    }

    private FloorConfigSO GetFloorConfig()
    {
        var dm = DungeonManager.Instance;
        if (dm == null) return null;

        var field = typeof(DungeonManager).GetField("floorConfigs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var configs = field?.GetValue(dm) as FloorConfigSO[];
        if (configs == null || configs.Length == 0) return null;

        var run = RunManager.Instance?.CurrentRun;
        int idx = run != null ? Mathf.Min(run.FloorIndex, configs.Length - 1) : 0;
        return configs[idx];
    }

    private void ClearAll()
    {
        foreach (var list in _roomEnemies.Values)
        {
            foreach (var enemy in list)
                if (enemy != null && ObjectPool.Instance != null)
                    ObjectPool.Instance.Release(EnemyPoolKey, enemy.gameObject);
        }
        _roomEnemies.Clear();
        _enemyRoom.Clear();
        _generatedRooms.Clear();
    }
}
