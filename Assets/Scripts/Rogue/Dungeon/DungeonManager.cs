using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core.Events;
using RogueDungeon.Data.Runtime;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Generation;
using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Rogue.Dungeon
{
    /// <summary>
    /// 地牢系统 Singleton 入口。监听 RunReady 事件触发生成，管理当前地图与房间，
    /// 处理房间切换与层推进。使用 DontDestroyOnLoad 跨场景持久化，
    /// 通过 SceneManager.sceneLoaded 重注册事件监听以解决场景切换后监听丢失问题。
    /// </summary>
    /// <remarks>
    /// <b>房间预制体实例化约束</b>：渲染层实例化房间 Prefab 时，必须使用
    /// <c>Instantiate(prefab, worldPosition, rotation)</c> 在创建时指定位置，
    /// 而非先实例化再移动 Transform。原因：无 Rigidbody2D 的子物体上的
    /// 2D Collider/Trigger 不会在父 Transform 移动后自动同步位置。
    /// 若需移动后修正，可调用 <c>Physics2D.SyncTransforms()</c> 或在根节点添加 Static Rigidbody2D。
    /// </remarks>
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

        [SerializeField] private FloorConfigSO[] floorConfigs; // 按层索引的楼层配置

        /// <summary>
        /// 当前地牢地图（仅在 Run 进行中有效）
        /// </summary>
        public DungeonMap CurrentMap { get; private set; }

        /// <summary>
        /// 当前所在房间（仅在 Run 进行中有效）
        /// </summary>
        public RoomInstance CurrentRoom { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (floorConfigs == null || floorConfigs.Length == 0)
            {
                Debug.LogError("[DungeonManager] floorConfigs 未配置或为空，请在 Inspector 中赋值");
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            RegisterEvents();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnregisterEvents();
        }

        /// <summary>
        /// 场景加载后重新注册事件监听（解决 DontDestroyOnLoad + EventCenter.Clear 冲突）
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            EventCenter.AddListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);
        }

        private void UnregisterEvents()
        {
            EventCenter.RemoveListener<RunReadyEvent>(GameEventType.RunReady, OnRunReady);
        }

        /// <summary>
        /// 响应 RunReady 事件，触发地牢生成
        /// </summary>
        private void OnRunReady(RunReadyEvent evt)
        {
            var run = evt.Run;
            int floorIndex = run.FloorIndex;

            var config = GetFloorConfig(floorIndex);
            if (config == null) return;

            int floorSeed = SeededRandom.Hash(run.Seed, floorIndex);
            var map = DungeonGenerator.Generate(floorSeed, config);
            if (map == null)
            {
                Debug.LogError($"[DungeonManager] 地牢生成失败: Floor={floorIndex}, Seed={floorSeed}");
                CurrentMap = null;
                CurrentRoom = null;
                return;
            }

            var startRoom = map.GetRoom(map.StartRoomId);
            if (startRoom == null)
            {
                Debug.LogError($"[DungeonManager] 地牢生成结果无效: StartRoomId={map.StartRoomId} 不存在");
                CurrentMap = map;
                CurrentRoom = null;
                return;
            }

            CurrentMap = map;
            CurrentRoom = startRoom;
            CurrentRoom.Visited = true;

            Debug.Log($"[DungeonManager] 地牢已生成: Floor={floorIndex}, Seed={floorSeed}, " +
                      $"房间数={map.AllRooms.Count}, Start={map.StartRoomId}, Boss={map.BossRoomId}");

            EventCenter.Broadcast(GameEventType.DungeonGenerated, new DungeonGeneratedEvent { Map = map });
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

            Debug.Log($"[DungeonManager] 进入房间: {roomId}");
            EventCenter.Broadcast(GameEventType.RoomEntered, new RoomEnteredEvent { Room = target });
        }

        /// <summary>
        /// 推进到下一层。递增 FloorIndex、重新生成地牢并广播 DungeonGenerated 事件
        /// </summary>
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
                CurrentMap = null;
                CurrentRoom = null;
                return;
            }

            var startRoom = map.GetRoom(map.StartRoomId);
            if (startRoom == null)
            {
                Debug.LogError($"[DungeonManager] 推进楼层失败: StartRoomId={map.StartRoomId} 不存在");
                CurrentMap = map;
                CurrentRoom = null;
                return;
            }

            CurrentMap = map;
            CurrentRoom = startRoom;
            CurrentRoom.Visited = true;

            Debug.Log($"[DungeonManager] 推进到新层: Floor={run.FloorIndex}, Seed={floorSeed}, " +
                      $"房间数={map.AllRooms.Count}");

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
    }
}
