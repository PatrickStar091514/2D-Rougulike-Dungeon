using System.Collections.Generic;
using System;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 房间视图组件，挂载在每个房间 Prefab 实例根节点上。
    /// 持有运行时 RoomInstance 引用、管理可见性状态、绑定子 DoorView。
    /// </summary>
    public class RoomView : MonoBehaviour, ISpawnPointProvider
    {
        private RoomInstance _room;                // 绑定的运行时房间数据
        [SerializeField] private RoomVisibility _visibility; // 当前可见性状态
        private IRoomFogController _fogController; // 迷雾控制器
        private readonly List<DoorView> _activeDoors = new(); // 已激活的门视图列表
        private SpawnPoint[] _spawnPoints;         // 缓存的生成点数组

        /// <summary>
        /// 房间行为策略（POCO，由工厂方法在初始化时创建）
        /// </summary>
        public IRoomBehavior Behavior { get; private set; }

        [Header("Runtime Debug")]
        [SerializeField] private string _debugRoomId; // 房间 ID

        /// <summary>
        /// 绑定的运行时房间数据
        /// </summary>
        public RoomInstance Room => _room;

        /// <summary>
        /// 房间唯一标识（快捷访问）
        /// </summary>
        public string RoomId => _room?.Id;

        /// <summary>
        /// 当前可见性状态
        /// </summary>
        public RoomVisibility Visibility => _visibility;

        /// <summary>
        /// 已激活的门视图只读列表
        /// </summary>
        public IReadOnlyList<DoorView> ActiveDoors => _activeDoors;

        /// <summary>
        /// 初始化房间视图，绑定数据、创建迷雾控制器、激活关联门位
        /// </summary>
        /// <param name="room">运行时房间实例</param>
        public void Initialize(RoomInstance room)
        {
            _room = room;
            _debugRoomId = room?.Id;
            _fogController = new SimpleFogController(gameObject);
            _spawnPoints = GetComponentsInChildren<SpawnPoint>(true);
            Behavior = CreateBehavior(room?.Type ?? Data.RoomType.Normal);
            BindDoorViews();
            SetVisibility(RoomVisibility.Hidden);
        }

        /// <summary>
        /// 设置房间可见性状态（Revealed 状态不回退到 Hidden/Silhouette）
        /// </summary>
        /// <param name="visibility">目标可见性状态</param>
        public void SetVisibility(RoomVisibility visibility)
        {
            // Revealed 不回退
            if (_visibility == RoomVisibility.Revealed && visibility != RoomVisibility.Revealed)
                return;

            _visibility = visibility;
            _fogController?.ApplyVisibility(visibility);
        }

        /// <summary>
        /// 获取指定类型的生成点列表
        /// </summary>
        public IReadOnlyList<SpawnPoint> GetSpawnPoints(SpawnType type)
        {
            var result = new List<SpawnPoint>();
            if (_spawnPoints == null) return result;
            foreach (var sp in _spawnPoints)
            {
                if (sp.Type == type)
                    result.Add(sp);
            }
            return result;
        }

        /// <summary>
        /// 根据 RoomInstance.Doors 匹配 Prefab 中的 DoorSlot 子物体并绑定 DoorView
        /// </summary>
        private void BindDoorViews()
        {
            if (_room?.Doors == null) return;
            _activeDoors.Clear();

            // 先建立 DoorSlotName -> DoorConnection 的查找表
            var connectionBySlot = new Dictionary<string, DoorConnection>(StringComparer.Ordinal);
            foreach (var conn in _room.Doors)
            {
                var slotName = BuildDoorSlotName(conn.LocalDoor);
                if (connectionBySlot.ContainsKey(slotName))
                {
                    Debug.LogWarning($"[RoomView] Room '{_room.Id}': Duplicate door slot key '{slotName}', keeping first");
                    continue;
                }
                connectionBySlot.Add(slotName, conn);
            }

            // 按 Prefab 层级顺序扫描 DoorSlot，保证 ActiveDoors 顺序稳定
            var allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var slotTransform in allTransforms)
            {
                if (slotTransform == null || !slotTransform.name.StartsWith("DoorSlot", StringComparison.Ordinal))
                    continue;

                if (!connectionBySlot.TryGetValue(slotTransform.name, out var conn))
                    continue;

                slotTransform.gameObject.SetActive(true);

                // 在 DoorTrigger 子物体上挂载 DoorView
                var triggerTransform = slotTransform.Find("DoorTrigger");
                if (triggerTransform == null)
                {
                    Debug.LogWarning($"[RoomView] Room '{_room.Id}': DoorTrigger not found under '{slotTransform.name}'");
                    continue;
                }

                var doorView = triggerTransform.GetComponent<DoorView>();
                if (doorView == null)
                    doorView = triggerTransform.gameObject.AddComponent<DoorView>();
                doorView.Initialize(this, conn.LocalDoor, conn.ConnectedRoomId, conn.RemoteDoor);
                _activeDoors.Add(doorView);

                connectionBySlot.Remove(slotTransform.name);
            }

            // 对未匹配到的 DoorConnection 输出告警
            foreach (var missing in connectionBySlot.Keys)
            {
                Debug.LogWarning($"[RoomView] Room '{_room.Id}': DoorSlot '{missing}' not found in prefab");
            }
        }

        /// <summary>
        /// 根据 DoorSlot 构造对应的 Prefab 子物体名称。
        /// 命名规则: DoorSlot{CellX}{CellY}{DirectionLetter}
        /// 例: CellOffset(0,1) + North → "DoorSlot01N"
        /// </summary>
        private static string BuildDoorSlotName(DoorSlot slot)
        {
            var dirLetter = slot.Direction switch
            {
                Direction.North => "N",
                Direction.South => "S",
                Direction.East => "E",
                Direction.West => "W",
                _ => "?"
            };
            return $"DoorSlot{slot.CellOffset.x}{slot.CellOffset.y}{dirLetter}";
        }

        /// <summary>
        /// 根据 RoomType 创建对应的房间行为策略实例。
        /// Elite/Shop/Event 在 MVP 阶段复用 NormalRoomBehavior，未知类型降级。
        /// </summary>
        /// <param name="type">房间类型</param>
        /// <returns>对应的 IRoomBehavior 实例</returns>
        internal static IRoomBehavior CreateBehavior(Data.RoomType type)
        {
            switch (type)
            {
                case Data.RoomType.Start:
                    return new StartRoomBehavior();
                case Data.RoomType.Normal:
                case Data.RoomType.Elite:
                case Data.RoomType.Shop:
                case Data.RoomType.Event:
                    return new NormalRoomBehavior();
                case Data.RoomType.Boss:
                    return new BossRoomBehavior();
                default:
                    Debug.LogWarning($"[RoomView] Unknown RoomType '{type}', falling back to NormalRoomBehavior");
                    return new NormalRoomBehavior();
            }
        }

        /// <summary>
        /// 通知行为策略：玩家进入房间。Behavior 为 null 时记录错误并跳过。
        /// 不包 broad try-catch（fail-fast）。
        /// </summary>
        public void NotifyEnter()
        {
            if (Behavior == null)
            {
                Debug.LogError($"[RoomView] NotifyEnter failed: Behavior is null for room '{RoomId}'");
                return;
            }
            Behavior.OnEnter(this);
        }

        /// <summary>
        /// 通知行为策略：房间清敌完成。Behavior 为 null 时记录错误并跳过。
        /// 不包 broad try-catch（fail-fast）。
        /// </summary>
        public void NotifyClear()
        {
            if (Behavior == null)
            {
                Debug.LogError($"[RoomView] NotifyClear failed: Behavior is null for room '{RoomId}'");
                return;
            }
            Behavior.OnClear(this);
        }

        /// <summary>
        /// 通知行为策略：玩家离开房间。Behavior 为 null 时记录错误并跳过。
        /// 不包 broad try-catch（fail-fast）。
        /// </summary>
        public void NotifyExit()
        {
            if (Behavior == null)
            {
                Debug.LogError($"[RoomView] NotifyExit failed: Behavior is null for room '{RoomId}'");
                return;
            }
            Behavior.OnExit(this);
        }
    }
}
