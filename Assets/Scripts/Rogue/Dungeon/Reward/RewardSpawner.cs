using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core;
using RogueDungeon.Core.Buff;
using RogueDungeon.Core.Events;
using RogueDungeon.Core.Pool;
using RogueDungeon.Data.Config;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Generation;
using RogueDungeon.Rogue.Dungeon.Generation;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Rogue.Dungeon.Reward
{
    /// <summary>
    /// 奖励生成协调器。监听状态与房间事件，生成掉落并处理 PendingReward 恢复。
    /// </summary>
    public class RewardSpawner : MonoBehaviour
    {
        public const string PoolKey = "BuffDrop";

        private enum RewardSource
        {
            TwoChoice = 0,
            ThreeChoice = 1,
            Boss = 2
        }

        private enum CompletionState
        {
            None = 0,
            ToRoomPlaying = 1,
            ToRunEnd = 2
        }

        public static RewardSpawner Instance { get; private set; }

        [SerializeField] private BuffPoolSO buffPool; // 全局奖励池
        [SerializeField] private GameObject buffDropPrefab; // Buff 掉落预制体

        [Header("掉落生成布局")]
        [SerializeField] private float fallbackSpawnSpacing = 1.5f;
        [SerializeField] private float fallbackSpawnYOffset = 0f;

        [Header("Cell Spawn")]
        [SerializeField] private bool enableCellSpawn = true;
        [Min(0.5f)]
        [SerializeField] private float minDropDistance = 1.5f;
        [Min(0f)]
        [SerializeField] private float playerAvoidRadius = 1f;
        [Min(1)]
        [SerializeField] private int maxPlacementAttempts = 30;
        [Min(0f)]
        [SerializeField] private float cellEdgePadding = 0.5f;

        [Header("Runtime Debug")]
        [SerializeField] private string _debugActiveRoomId; // 当前奖励房间
        [SerializeField] private int _debugDropCount; // 当前活跃掉落数量

        private readonly List<BuffDrop> _activeDrops = new();
        private ExclusivePickupGroup _activeGroup;
        private CompletionState _completionState;
        private bool _rewardClaimed;

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
            if (Instance == this)
                Instance = null;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnregisterEvents();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            UnregisterEvents(); // 防止 DDOL + 场景重载导致重复订阅
            EventCenter.AddListener<GameStateChangedEvent>(GameEventType.GameStateChanged, OnGameStateChanged);
            EventCenter.AddListener<RoomEnteredEvent>(GameEventType.RoomEntered, OnRoomEntered);
            EventCenter.AddListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        private void UnregisterEvents()
        {
            EventCenter.RemoveListener<GameStateChangedEvent>(GameEventType.GameStateChanged, OnGameStateChanged);
            EventCenter.RemoveListener<RoomEnteredEvent>(GameEventType.RoomEntered, OnRoomEntered);
            EventCenter.RemoveListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.ToState != GameState.RewardSelect) return;

            var room = DungeonManager.Instance?.CurrentRoom;
            if (room == null) return;

            if (room.Type == RoomType.Normal || room.Type == RoomType.Elite)
            {
                EnsureRewardWave(room, 2, RewardSource.TwoChoice, CompletionState.ToRoomPlaying, false);
                return;
            }

            if (room.Type == RoomType.Boss)
            {
                EnsureRewardWave(room, 1, RewardSource.Boss, CompletionState.ToRunEnd, true);
            }
        }

        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (evt.Room == null || evt.Room.Type != RoomType.Event) return;
            if (evt.Room.Cleared) return;
            EnsureRewardWave(evt.Room, 3, RewardSource.ThreeChoice, CompletionState.ToRoomPlaying, false);
        }

        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            TryRestorePendingReward();
        }

        private void EnsureRewardWave(
            RoomInstance room,
            int count,
            RewardSource source,
            CompletionState completionState,
            bool useBossRoll)
        {
            if (room == null || buffPool == null || buffDropPrefab == null)
            {
                Debug.LogWarning("[RewardSpawner] 缺少必要配置，无法生成奖励");
                return;
            }

            EnsureBuffManagerReady();

            var run = RunManager.Instance?.CurrentRun;
            if (run == null)
            {
                Debug.LogWarning("[RewardSpawner] 当前无 RunState，跳过奖励生成");
                return;
            }

            if (_activeDrops.Count > 0 && _debugActiveRoomId == room.Id)
                return;

            List<string> offeredBuffIds;
            var pending = run.PendingReward;
            if (pending != null && pending.RoomId == room.Id && pending.OfferedBuffIds != null && pending.OfferedBuffIds.Count > 0)
            {
                offeredBuffIds = new List<string>(pending.OfferedBuffIds);
            }
            else
            {
                if (useBossRoll)
                {
                    string bossBuffId = RewardRoller.RollBoss(
                        buffPool,
                        BuildSeed(run.Seed, room.Id, "boss"));

                    if (string.IsNullOrEmpty(bossBuffId))
                    {
                        Debug.LogWarning($"[RewardSpawner] Boss 房间 '{room.Id}' 未 Roll 到可用奖励");
                        return;
                    }

                    offeredBuffIds = new List<string> { bossBuffId };
                }
                else
                {
                    offeredBuffIds = RewardRoller.Roll(
                        buffPool,
                        BuildSeed(run.Seed, room.Id, "reward"),
                        count);
                    if (offeredBuffIds.Count == 0)
                    {
                        Debug.LogWarning($"[RewardSpawner] 房间 '{room.Id}' 奖励池为空，跳过生成");
                        return;
                    }
                }

                run.PendingReward = new PendingReward
                {
                    RoomId = room.Id,
                    Source = (int)source,
                    OfferedBuffIds = new List<string>(offeredBuffIds)
                };
            }

            SpawnDrops(room, offeredBuffIds, completionState);
        }

        private void TryRestorePendingReward()
        {
            var run = RunManager.Instance?.CurrentRun;
            if (run == null || run.PendingReward == null) return;

            var pending = run.PendingReward;
            if (pending.OfferedBuffIds == null || pending.OfferedBuffIds.Count == 0)
            {
                run.PendingReward = null;
                return;
            }

            var room = DungeonManager.Instance?.CurrentMap?.GetRoom(pending.RoomId);
            if (room == null)
            {
                Debug.LogWarning($"[RewardSpawner] PendingReward 房间不存在: {pending.RoomId}");
                return;
            }

            SpawnDrops(room, new List<string>(pending.OfferedBuffIds), GetCompletionState(room.Type));
        }

        private void SpawnDrops(RoomInstance room, List<string> offeredBuffIds, CompletionState completionState)
        {
            ReleaseAllDrops();

            var positions = ResolveSpawnPositions(room, offeredBuffIds.Count);
            for (int i = 0; i < offeredBuffIds.Count; i++)
            {
                string buffId = offeredBuffIds[i];
                if (string.IsNullOrEmpty(buffId)) continue;

                var dropObject = ObjectPool.Instance != null
                    ? ObjectPool.Instance.Get(PoolKey, buffDropPrefab)
                    : Instantiate(buffDropPrefab);

                dropObject.transform.position = positions[i];

                var drop = dropObject.GetComponent<BuffDrop>();
                if (drop == null)
                {
                    Debug.LogError("[RewardSpawner] BuffDrop Prefab 缺少 BuffDrop 组件");
                    if (ObjectPool.Instance != null)
                        ObjectPool.Instance.Release(PoolKey, dropObject);
                    else
                        Destroy(dropObject);
                    continue;
                }

                var config = buffPool.FindByBuffId(buffId);
                var snapshot = config?.ToSnapshot();
                drop.Init(snapshot, config != null ? config.Icon : null, null, OnDropPicked);
                _activeDrops.Add(drop);
            }

            if (_activeDrops.Count > 1)
            {
                if (_activeGroup == null)
                {
                    var groupObject = new GameObject($"RewardGroup_{room.Id}");
                    _activeGroup = groupObject.AddComponent<ExclusivePickupGroup>();
                }

                _activeGroup.gameObject.name = $"RewardGroup_{room.Id}";
                _activeGroup.gameObject.SetActive(true);
                _activeGroup.Init(new List<BuffDrop>(_activeDrops));
                for (int i = 0; i < _activeDrops.Count; i++)
                    _activeDrops[i].SetGroup(_activeGroup);
            }
            else
            {
                ReleaseActiveGroup();
            }

            _debugActiveRoomId = room.Id;
            _completionState = completionState;
            _rewardClaimed = false;
            _debugDropCount = _activeDrops.Count;
        }

        private void OnDropPicked(BuffDrop picked)
        {
            if (_rewardClaimed || picked == null) return;
            _rewardClaimed = true;

            var run = RunManager.Instance?.CurrentRun;
            if (run != null)
                run.PendingReward = null;

            if (DungeonManager.Instance?.CurrentMap != null && !string.IsNullOrEmpty(_debugActiveRoomId))
            {
                var room = DungeonManager.Instance.CurrentMap.GetRoom(_debugActiveRoomId);
                if (room != null)
                {
                    room.Cleared = true;
                    EventCenter.Broadcast(GameEventType.RoomCleared, new RoomClearedEvent { RoomId = _debugActiveRoomId });
                }
            }

            var rewardConfig = buffPool?.FindByBuffId(picked.BuffId);
            EventCenter.Broadcast(GameEventType.RewardClaimed, new RewardClaimedEvent
            {
                Snapshot = rewardConfig?.ToSnapshot(),
                RoomId = _debugActiveRoomId
            });

            for (int i = 0; i < _activeDrops.Count; i++)
            {
                var drop = _activeDrops[i];
                if (drop == null || drop == picked) continue;
                drop.ReleaseFromGroup();
            }

            _activeDrops.Clear();
            ReleaseActiveGroup();
            _debugDropCount = 0;

            var gameManager = GameManager.Instance;
            if (gameManager == null) return;

            switch (_completionState)
            {
                case CompletionState.ToRoomPlaying:
                    if (gameManager.CurrentState != GameState.RoomPlaying)
                        gameManager.ChangeState(GameState.RoomPlaying);
                    break;
                case CompletionState.ToRunEnd:
                    if (gameManager.CurrentState != GameState.RunEnd)
                        gameManager.ChangeState(GameState.RunEnd);
                    break;
            }
        }

        /// <summary>
        /// 获取玩家当前位置（tag="Player"），未找到返回 null。
        /// </summary>
        private static Vector3? GetPlayerPosition()
        {
            var player = GameObject.FindWithTag("Player");
            return player != null ? player.transform.position : (Vector3?)null;
        }

        /// <summary>
        /// 在房间的 Cell 中随机生成不重叠的掉落位置，并避开玩家。
        /// </summary>
        private List<Vector3> GenerateCellSpawnPositions(
            RoomInstance room, int count, SeededRandom rng, Vector3? playerPos)
        {
            // 每个 cell 转为世界空间 Rect（内缩 cellEdgePadding）
            var cellRects = new List<Rect>(room.Cells.Count);
            for (int i = 0; i < room.Cells.Count; i++)
            {
                Rect r = CellToWorldRect(room.Cells[i]);
                float pad = Mathf.Min(cellEdgePadding, r.width * 0.45f);
                cellRects.Add(new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f));
            }

            if (cellRects.Count == 0) return new List<Vector3>();

            // Phase 1: 要求避开玩家
            var positions = TryPlacePositions(cellRects, count, rng, playerPos,
                requirePlayerAvoid: true, existingPositions: null);

            // Phase 2: 若不够，放宽玩家避让
            if (positions.Count < count)
            {
                positions = TryPlacePositions(cellRects, count, rng, playerPos,
                    requirePlayerAvoid: false, existingPositions: positions);
            }

            return positions;
        }

        /// <summary>
        /// 在给定的 cell Rect 列表中随机取点，满足最小距离与可选玩家避让约束。
        /// </summary>
        private List<Vector3> TryPlacePositions(
            List<Rect> cellRects,
            int targetCount,
            SeededRandom rng,
            Vector3? playerPos,
            bool requirePlayerAvoid,
            List<Vector3> existingPositions)
        {
            var positions = existingPositions != null
                ? new List<Vector3>(existingPositions)
                : new List<Vector3>();

            int maxAttempts = maxPlacementAttempts * targetCount;
            for (int attempt = 0; attempt < maxAttempts && positions.Count < targetCount; attempt++)
            {
                // 随机选一个 cell
                int cellIdx = rng.Range(0, cellRects.Count);
                Rect rect = cellRects[cellIdx];

                // 在 cell 内随机取点
                float x = Mathf.Lerp(rect.xMin, rect.xMax, rng.Value);
                float y = Mathf.Lerp(rect.yMin, rect.yMax, rng.Value);
                var candidate = new Vector3(x, y, 0f);

                // 约束：与已有点距离 ≥ minDropDistance
                if (!IsFarEnoughFromAll(positions, candidate, minDropDistance))
                    continue;

                // 约束：与玩家距离 ≥ playerAvoidRadius
                if (requirePlayerAvoid && playerPos.HasValue)
                {
                    if (Vector3.Distance(candidate, playerPos.Value) < playerAvoidRadius)
                        continue;
                }

                positions.Add(candidate);
            }

            return positions;
        }

        private static bool IsFarEnoughFromAll(List<Vector3> positions, Vector3 candidate, float minDist)
        {
            float minDistSqr = minDist * minDist;
            for (int i = 0; i < positions.Count; i++)
            {
                if ((candidate - positions[i]).sqrMagnitude < minDistSqr)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 将格子坐标转为世界空间 Rect（10×10 单位/cell）。
        /// </summary>
        private static Rect CellToWorldRect(Vector2Int cell)
        {
            float size = DungeonViewManager.CellWorldSize;
            return new Rect(cell.x * size, cell.y * size, size, size);
        }

        private void ReleaseAllDrops()
        {
            for (int i = 0; i < _activeDrops.Count; i++)
            {
                var drop = _activeDrops[i];
                if (drop == null) continue;
                drop.ReleaseFromGroup();
            }
            _activeDrops.Clear();
            ReleaseActiveGroup();
            _debugDropCount = 0;
        }

        /// <summary>
        /// 解析掉落物生成位置，按三级优先级：预设生成点 > Cell 随机 > 中心 Fallback。
        /// </summary>
        private List<Vector3> ResolveSpawnPositions(RoomInstance room, int count)
        {
            var positions = new List<Vector3>(count);

            // ── Tier 1: 预设 SpawnType.Reward 生成点 ──
            var viewManager = FindObjectOfType<DungeonViewManager>();
            RoomView roomView = null;
            if (viewManager != null)
                viewManager.TryGetRoomView(room.Id, out roomView);

            if (roomView != null)
            {
                var rewardPoints = roomView.GetSpawnPoints(SpawnType.Reward);
                if (rewardPoints.Count >= count)
                {
                    for (int i = 0; i < count; i++)
                        positions.Add(rewardPoints[i].transform.position);
                    return positions;
                }
            }

            // ── Tier 2: Cell 随机生成 ──
            if (enableCellSpawn && room.Cells != null && room.Cells.Count > 0)
            {
                var run = RunManager.Instance?.CurrentRun;
                if (run != null)
                {
                    int seed = SeededRandom.Hash(run.Seed, $"{room.Id}_cellspawn");
                    var rng = new SeededRandom(seed);
                    var playerPos = GetPlayerPosition();

                    positions = GenerateCellSpawnPositions(room, count, rng, playerPos);
                    if (positions.Count == count)
                        return positions;

                    if (positions.Count > 0)
                        Debug.LogWarning($"[RewardSpawner] Cell 生成只找到 {positions.Count}/{count} 个位置，补充 Fallback");
                }
            }

            // ── Tier 3: 边界框中心 Fallback ──
            int remaining = count - positions.Count;
            if (roomView != null)
            {
                var roomBounds = DungeonCamera.CalculateRoomBounds(roomView.Room);
                var center = new Vector3(roomBounds.center.x, roomBounds.center.y, roomView.transform.position.z);
                for (int i = 0; i < remaining; i++)
                    positions.Add(center + new Vector3((i - (remaining - 1) * 0.5f) * fallbackSpawnSpacing, fallbackSpawnYOffset, 0f));
            }
            else
            {
                Debug.LogWarning($"[RewardSpawner] 未找到房间视图 '{room.Id}'，使用世界原点 Fallback");
                for (int i = 0; i < remaining; i++)
                    positions.Add(new Vector3((i - (remaining - 1) * 0.5f) * fallbackSpawnSpacing, fallbackSpawnYOffset, 0f));
            }

            return positions;
        }

        private static CompletionState GetCompletionState(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Normal => CompletionState.ToRoomPlaying,
                RoomType.Elite => CompletionState.ToRoomPlaying,
                RoomType.Boss => CompletionState.ToRunEnd,
                _ => CompletionState.None
            };
        }

        private static int BuildSeed(int runSeed, string roomId, string suffix)
        {
            return SeededRandom.Hash(runSeed, $"{roomId}_{suffix}");
        }

        private void ReleaseActiveGroup()
        {
            if (_activeGroup == null) return;

            _activeGroup.Init(null);
            if (_activeGroup.gameObject != null)
                _activeGroup.gameObject.SetActive(false);
        }

        private void EnsureBuffManagerReady()
        {
            var manager = BuffManager.Instance;
            if (manager == null)
            {
                // 优先查找场景中已有但 Awake 尚未执行的 BuffManager，避免创建重复
                manager = FindFirstObjectByType<BuffManager>();
                if (manager == null)
                {
                    var managerObject = new GameObject("BuffManager");
                    manager = managerObject.AddComponent<BuffManager>();
                }
            }

            manager.BindBuffPoolIfMissing(buffPool);
        }
    }
}
