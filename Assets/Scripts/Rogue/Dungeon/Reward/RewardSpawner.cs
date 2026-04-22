using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using RogueDungeon.Core.Pool;
using RogueDungeon.Data.Config;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Rogue.Dungeon.Data;
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
            EnsureBuffManagerReady();
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

            var positions = ResolveSpawnPositions(room.Id, offeredBuffIds.Count);
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
                drop.Init(
                    buffId,
                    config != null ? config.Icon : null,
                    null,
                    OnDropPicked,
                    config != null ? config.DropSortingLayer : "Drop",
                    config != null ? config.DropSpriteScale : 1f,
                    config != null ? config.DropColliderRadius : 0.5f);
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
                    room.Cleared = true;
            }

            EventCenter.Broadcast(GameEventType.RewardClaimed, new RewardClaimedEvent
            {
                BuffId = picked.BuffId,
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

        private List<Vector3> ResolveSpawnPositions(string roomId, int count)
        {
            var positions = new List<Vector3>(count);

            var viewManager = FindObjectOfType<DungeonViewManager>();
            if (viewManager != null && viewManager.TryGetRoomView(roomId, out var roomView))
            {
                var rewardPoints = roomView.GetSpawnPoints(SpawnType.Reward);
                if (rewardPoints.Count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (i < rewardPoints.Count && rewardPoints[i] != null)
                            positions.Add(rewardPoints[i].transform.position);
                        else
                            positions.Add(roomView.transform.position + new Vector3((i - (count - 1) * 0.5f) * 0.8f, 0f, 0f));
                    }
                    return positions;
                }

                Debug.LogWarning($"[RewardSpawner] 房间 '{roomId}' 未找到 SpawnType.Reward，使用中心点 Fallback");
                var roomBounds = DungeonCamera.CalculateRoomBounds(roomView.Room);
                var center = new Vector3(roomBounds.center.x, roomBounds.center.y, roomView.transform.position.z);
                for (int i = 0; i < count; i++)
                    positions.Add(center + new Vector3((i - (count - 1) * 0.5f) * 0.8f, 0f, 0f));
                return positions;
            }

            Debug.LogWarning($"[RewardSpawner] 未找到房间视图 '{roomId}'，使用世界原点 Fallback");
            for (int i = 0; i < count; i++)
                positions.Add(new Vector3((i - (count - 1) * 0.5f) * 0.8f, 0f, 0f));
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
                var managerObject = new GameObject("BuffManager");
                manager = managerObject.AddComponent<BuffManager>();
            }

            manager.BindBuffPoolIfMissing(buffPool);
        }
    }
}
