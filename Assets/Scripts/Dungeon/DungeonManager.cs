using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.Config;
using RogueDungeon.Dungeon.Generation;
using RogueDungeon.Dungeon.Map;

namespace RogueDungeon.Dungeon
{
    /// <summary>
    /// 地牢系统 Singleton 入口。监听 RunReady 事件触发生成，管理当前地图与房间，
    /// 处理房间切换、层推进与跨层预加载。使用 DontDestroyOnLoad 跨场景持久化，
    /// 通过 SceneManager.sceneLoaded 重注册事件监听以解决场景切换后监听丢失问题。
    /// </summary>
    /// <remarks>
    /// <b>双 Slot 架构</b>：维护大小为 2 的 DungeonMap 数组，通过 _activeMapIndex（0/1）乒乓切换。
    /// 活跃 Slot 展示当前可玩层，另一 Slot 用于后台预加载下一层。
    /// </remarks>
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

        [SerializeField] private FloorConfigSO[] floorConfigs; // 按层索引的楼层配置

        [Header("Runtime Debug")]
        [SerializeField] private int _debugActiveSlot; // 当前活跃 Slot 索引
        [SerializeField] private string _debugCurrentRoomId; // 当前房间 ID
        [SerializeField] private int _debugRoomCount; // 房间总数
        [SerializeField] private string _debugStartRoomId; // 起始房间 ID
        [SerializeField] private string _debugBossRoomId; // Boss 房间 ID
        [SerializeField] private bool _debugNextFloorReady; // 下一层预加载完成状态

        private readonly DungeonMap[] _maps = new DungeonMap[2]; // 双 Slot 地图缓冲
        private int _activeMapIndex; // 当前活跃 Slot（0 或 1）

        /// <summary>
        /// 当前地牢地图（仅在 Run 进行中有效）
        /// </summary>
        public DungeonMap CurrentMap => _maps[_activeMapIndex];

        /// <summary>
        /// 当前所在房间（仅在 Run 进行中有效）
        /// </summary>
        public RoomInstance CurrentRoom { get; private set; }

        /// <summary>
        /// 下一层预加载是否已完成
        /// </summary>
        public bool NextFloorReady
        {
            get => _nextFloorReady;
            internal set
            {
                _nextFloorReady = value;
                _debugNextFloorReady = value;
            }
        }
        private bool _nextFloorReady; // 下一层预加载完成标记

        /// <summary>
        /// 总层数（由 FloorConfigSO 数组长度决定）
        /// </summary>
        public int TotalFloors => floorConfigs != null ? floorConfigs.Length : 0;

        private bool _initialized; // 是否完成单例初始化

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            UnregisterEvents();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (floorConfigs == null || floorConfigs.Length == 0)
                Debug.LogError("[DungeonManager] floorConfigs 未配置或为空，请在 Inspector 中赋值");

            _initialized = true;
        }

        private void RegisterEvents()
        {
            UnregisterEvents(); // 防止 sceneLoaded 导致的重复订阅
            EventCenter.AddListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);
            EventCenter.AddListener<RoomClearedEvent>(GameEventType.RoomCleared, OnRoomCleared);
            EventCenter.AddListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.AddListener<FloorCompletedEvent>(GameEventType.FloorCompleted, OnFloorCompleted);
            Debug.Log($"[DEBUG] DungeonManager.RegisterEvents 完成");
        }

        private void UnregisterEvents()
        {
            EventCenter.RemoveListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);
            EventCenter.RemoveListener<RoomClearedEvent>(GameEventType.RoomCleared, OnRoomCleared);
            EventCenter.RemoveListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.RemoveListener<FloorCompletedEvent>(GameEventType.FloorCompleted, OnFloorCompleted);
        }

        /// <summary>
        /// 响应 RunReady 事件，触发生成首层地牢
        /// </summary>
        private void OnRunReady(RunReadyEvent evt)
        {
            Debug.Log($"[DEBUG] DungeonManager.OnRunReady 收到, FloorIndex={evt.Run?.FloorIndex}");
            var run = evt.Run;
            int floorIndex = run.FloorIndex;

            var config = GetFloorConfig(floorIndex);
            if (config == null) return;

            int floorSeed = SeededRandom.Hash(run.Seed, floorIndex);
            var map = DungeonGenerator.Generate(floorSeed, config);
            if (map == null)
            {
                Debug.LogError($"[DungeonManager] 地牢生成失败: Floor={floorIndex}, Seed={floorSeed}");
                _maps[0] = null;
                CurrentRoom = null;
                SyncDebugFields();
                return;
            }

            var startRoom = map.GetRoom(map.StartRoomId);
            if (startRoom == null)
            {
                Debug.LogError($"[DungeonManager] 地牢生成结果无效: StartRoomId={map.StartRoomId} 不存在");
                _maps[0] = map;
                CurrentRoom = null;
                SyncDebugFields();
                return;
            }

            _maps[0] = map;
            _activeMapIndex = 0;
            CurrentRoom = startRoom;
            CurrentRoom.Visited = true;
            NextFloorReady = false;
            SyncDebugFields();

            Debug.Log($"[DEBUG] DungeonManager: 广播 DungeonGenerated, StartRoomId={map.StartRoomId}, RoomCount={map.AllRooms?.Count}");
            EventCenter.Broadcast(GameEventType.DungeonGenerated, new DungeonGeneratedEvent { Map = map });
        }

        /// <summary>
        /// 响应 DungeonReady 事件，若存在下一层则启动预加载协程
        /// </summary>
        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            var run = RunManager.Instance?.CurrentRun;
            if (run == null) return;

            int nextFloor = run.FloorIndex + 1;
            if (nextFloor < TotalFloors)
                StartCoroutine(PreloadFloorAsync(nextFloor));
        }

        /// <summary>
        /// 响应 FloorCompleted 事件，启动下一层预加载协程
        /// </summary>
        private void OnFloorCompleted(FloorCompletedEvent evt)
        {
            int nextFloor = evt.ToFloorIndex + 1;
            if (nextFloor < TotalFloors)
                StartCoroutine(PreloadFloorAsync(nextFloor));
        }

        /// <summary>
        /// 切换到指定层（Portal 过渡时调用）。目标层必须已预加载
        /// </summary>
        /// <param name="floorIndex">目标楼层索引</param>
        public void SwitchToFloor(int floorIndex)
        {
            if (floorIndex >= TotalFloors)
            {
                EventCenter.Broadcast(GameEventType.RunEnded, new RunEndedEvent { IsVictory = true });
                return;
            }

            int targetSlot = floorIndex % 2;
            var targetMap = _maps[targetSlot];

            if (targetMap == null)
            {
                Debug.LogError($"[DungeonManager] SwitchToFloor 失败: Floor {floorIndex} 未预加载（Slot {targetSlot} 为 null）");
                return;
            }

            var run = RunManager.Instance?.CurrentRun;
            if (run != null)
                run.FloorIndex = floorIndex;

            _activeMapIndex = targetSlot;
            CurrentRoom = targetMap.GetRoom(targetMap.StartRoomId);
            if (CurrentRoom != null)
                CurrentRoom.Visited = true;
            NextFloorReady = false;
            SyncDebugFields();


            EventCenter.Broadcast(GameEventType.RoomEntered, new RoomEnteredEvent { Room = CurrentRoom });
        }

        /// <summary>
        /// 切换当前房间。验证 roomId 有效性后更新 CurrentRoom 并广播 RoomEntered 事件
        /// </summary>
        /// <param name="roomId">目标房间 ID（对应 RoomInstance.RoomId）</param>
        public void TransitToRoom(string roomId)
        {
            if (CurrentMap == null)
            {
                Debug.LogWarning("[DungeonManager] TransitToRoom 失败: CurrentMap 为 null");
                return;
            }

            var target = CurrentMap.GetRoom(roomId);

            if (target == null)
            {
                Debug.LogWarning($"[DungeonManager] TransitToRoom 失败: 未找到房间 {roomId}");
                return;
            }

            CurrentRoom = target;
            CurrentRoom.Visited = true;
            _debugCurrentRoomId = roomId;

            EventCenter.Broadcast(GameEventType.RoomEntered, new RoomEnteredEvent { Room = target });
        }

        /// <summary>
        /// 后台生成目标层地图数据并存入非活跃 Slot
        /// </summary>
        /// <param name="floorIndex">目标楼层索引</param>
        public DungeonMap PreloadFloorData(int floorIndex)
        {
            var run = RunManager.Instance?.CurrentRun;
            if (run == null)
            {
                Debug.LogWarning("[DungeonManager] PreloadFloorData 失败: RunManager 或 CurrentRun 为 null");
                return null;
            }

            var config = GetFloorConfig(floorIndex);
            if (config == null) return null;

            int floorSeed = SeededRandom.Hash(run.Seed, floorIndex);
            var map = DungeonGenerator.Generate(floorSeed, config);
            if (map == null)
            {
                Debug.LogError($"[DungeonManager] 预加载 Floor {floorIndex} 失败: 地牢生成返回 null");
                return null;
            }

            int targetSlot = floorIndex % 2;
            _maps[targetSlot] = map;

            return map;
        }

        /// <summary>
        /// 异步预加载目标层（生成地图数据 + 通知视图层实例化）
        /// </summary>
        /// <param name="floorIndex">目标楼层索引</param>
        public System.Collections.IEnumerator PreloadFloorAsync(int floorIndex)
        {

            var map = PreloadFloorData(floorIndex);
            if (map == null) yield break;

            // 通知 ViewManager 分帧实例化房间视图（通过事件或直接引用）
            var viewManager = Object.FindFirstObjectByType<Dungeon.View.DungeonViewManager>();
            if (viewManager != null)
            {
                yield return viewManager.StartCoroutine(
                    viewManager.InstantiateFloorAsync(map, floorIndex % 2));
            }

            NextFloorReady = true;
        }

        /// <summary>
        /// 清除所有 Slot 的地图数据和状态
        /// </summary>
        public void ClearAllSlots()
        {
            for (int i = 0; i < _maps.Length; i++)
                _maps[i] = null;
            CurrentRoom = null;
            NextFloorReady = false;
            SyncDebugFields();
        }

        /// <summary>
        /// 响应 RoomCleared 事件，写回运行时房间清理状态
        /// </summary>
        private void OnRoomCleared(RoomClearedEvent evt)
        {
            if (CurrentMap == null || string.IsNullOrEmpty(evt.RoomId))
                return;

            var room = CurrentMap.GetRoom(evt.RoomId);
            if (room == null)
            {
                Debug.LogWarning($"[DungeonManager] RoomCleared 指向未知房间: {evt.RoomId}");
                return;
            }

            room.Cleared = true;
        }

        /// <summary>
        /// 推进到下一层（同步）。已由 PreloadFloorAsync + SwitchToFloor 取代
        /// </summary>
        [System.Obsolete("使用 PreloadFloorAsync + SwitchToFloor 替代")]
        public void AdvanceFloor()
        {
            if (RunManager.Instance == null || RunManager.Instance.CurrentRun == null)
            {
                Debug.LogWarning("[DungeonManager] AdvanceFloor 失败: RunManager 或 CurrentRun 为 null");
                return;
            }

            var run = RunManager.Instance.CurrentRun;
            run.FloorIndex++;

            var config = GetFloorConfig(run.FloorIndex);
            if (config == null) return;

            int floorSeed = SeededRandom.Hash(run.Seed, run.FloorIndex);
            var map = DungeonGenerator.Generate(floorSeed, config);
            if (map == null)
            {
                Debug.LogError($"[DungeonManager] 推进楼层失败: Floor={run.FloorIndex}, Seed={floorSeed} 地牢生成返回 null");
                _maps[_activeMapIndex] = null;
                CurrentRoom = null;
                SyncDebugFields();
                return;
            }

            var startRoom = map.GetRoom(map.StartRoomId);
            if (startRoom == null)
            {
                Debug.LogError($"[DungeonManager] 推进楼层失败: StartRoomId={map.StartRoomId} 不存在");
                _maps[_activeMapIndex] = map;
                CurrentRoom = null;
                SyncDebugFields();
                return;
            }

            _maps[_activeMapIndex] = map;
            CurrentRoom = startRoom;
            CurrentRoom.Visited = true;
            NextFloorReady = false;
            SyncDebugFields();


            EventCenter.Broadcast(GameEventType.DungeonGenerated, new DungeonGeneratedEvent { Map = map });
        }

        /// <summary>
        /// 按层索引获取楼层配置，超出范围时返回最后一个配置
        /// </summary>
        private FloorConfigSO GetFloorConfig(int floorIndex)
        {
            if (floorConfigs == null || floorConfigs.Length == 0)
            {
                Debug.LogError("[DungeonManager] floorConfigs 未配置");
                return null;
            }

            int idx = Mathf.Min(floorIndex, floorConfigs.Length - 1);
            return floorConfigs[idx];
        }

        /// <summary>
        /// 同步 Inspector 调试字段
        /// </summary>
        private void SyncDebugFields()
        {
            _debugActiveSlot = _activeMapIndex;
            _debugCurrentRoomId = CurrentRoom?.Id;
            _debugRoomCount = CurrentMap?.AllRooms?.Count ?? 0;
            _debugStartRoomId = CurrentMap?.StartRoomId;
            _debugBossRoomId = CurrentMap?.BossRoomId;
        }
    }
}
