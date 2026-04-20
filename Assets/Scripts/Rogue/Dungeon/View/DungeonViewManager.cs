using System.Collections.Generic;
using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 地牢视图管理器，消费 DungeonGenerated 事件执行全量实例化，
    /// 响应 RoomEntered 更新迷雾状态，完成后广播 DungeonReady。
    /// 场景级 MonoBehaviour（非 Singleton），生命周期与场景绑定。
    /// </summary>
    public class DungeonViewManager : MonoBehaviour
    {
        private const int MinCellWorldSize = 1;
        private static int _runtimeCellWorldSize = 10;

        [Header("Placement")]
        [Min(MinCellWorldSize)]
        [SerializeField] private int _cellWorldSize = 10; // 单个 cell 的世界空间边长（像素/单位）

        /// <summary>当前运行时使用的 cell 世界尺寸</summary>
        public static int CellWorldSize => _runtimeCellWorldSize;

        private readonly Dictionary<string, RoomView> _roomViews = new(); // roomId → RoomView 索引
        private DungeonMap _currentMap; // 当前关联的地牢地图

        [Header("Runtime Debug")]
        [SerializeField] private int _debugRoomViewCount; // 已实例化房间视图数

        private void OnValidate()
        {
            if (_cellWorldSize < MinCellWorldSize)
                _cellWorldSize = MinCellWorldSize;
            ApplyCellWorldSize();
        }

        private void OnEnable()
        {
            ApplyCellWorldSize();
            EventCenter.AddListener<DungeonGeneratedEvent>(
                GameEventType.DungeonGenerated, OnDungeonGenerated);
            EventCenter.AddListener<RoomEnteredEvent>(
                GameEventType.RoomEntered, OnRoomEntered);
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<DungeonGeneratedEvent>(
                GameEventType.DungeonGenerated, OnDungeonGenerated);
            EventCenter.RemoveListener<RoomEnteredEvent>(
                GameEventType.RoomEntered, OnRoomEntered);
        }

        /// <summary>
        /// 同步 Inspector 配置到运行时静态尺寸，供地图位置计算统一使用。
        /// </summary>
        private void ApplyCellWorldSize()
        {
            _runtimeCellWorldSize = Mathf.Max(MinCellWorldSize, _cellWorldSize);
        }

        /// <summary>
        /// 按 roomId 查询 RoomView
        /// </summary>
        /// <param name="roomId">房间唯一标识</param>
        /// <returns>对应的 RoomView，不存在时返回 null</returns>
        public RoomView GetRoomView(string roomId)
        {
            return _roomViews.TryGetValue(roomId, out var rv) ? rv : null;
        }

        /// <summary>
        /// 尝试按 roomId 查询 RoomView（Try 模式）
        /// </summary>
        /// <param name="roomId">房间唯一标识</param>
        /// <param name="roomView">输出的 RoomView</param>
        /// <returns>是否找到</returns>
        public bool TryGetRoomView(string roomId, out RoomView roomView)
        {
            return _roomViews.TryGetValue(roomId, out roomView);
        }

        /// <summary>
        /// 获取所有已实例化房间视图的只读枚举
        /// </summary>
        public IEnumerable<RoomView> AllRoomViews => _roomViews.Values;

        /// <summary>
        /// 响应 DungeonGenerated：清理旧视图 → 全量实例化 → 初始化迷雾 → 广播 DungeonReady
        /// </summary>
        private void OnDungeonGenerated(DungeonGeneratedEvent evt)
        {
            var map = evt.Map;
            if (map == null || map.AllRooms == null || map.AllRooms.Count == 0)
            {
                Debug.LogWarning("[DungeonViewManager] Received empty DungeonMap, skipping instantiation");
                return;
            }

            ClearPrevious();
            _currentMap = map;

            // 全量实例化所有房间
            foreach (var room in map.AllRooms)
            {
                if (room.Template == null || room.Template.Prefab == null)
                {
                    Debug.LogWarning($"[DungeonViewManager] Room '{room.Id}' has no template/prefab, skipping");
                    continue;
                }

                var worldPos = CalculateRoomRootPosition(room);

                var go = Instantiate(room.Template.Prefab, worldPos, Quaternion.identity, transform);
                go.name = $"Room_{room.Id}";

                var roomView = go.GetComponent<RoomView>();
                if (roomView == null)
                    roomView = go.AddComponent<RoomView>();

                roomView.Initialize(room);
                _roomViews[room.Id] = roomView;
            }

            // 起始房间 Revealed，其余保持 Hidden
            if (!string.IsNullOrEmpty(map.StartRoomId) && _roomViews.TryGetValue(map.StartRoomId, out var startView))
            {
                startView.SetVisibility(RoomVisibility.Revealed);
                // 起始房间邻居设为 Silhouette
                UpdateNeighborFog(startView);
            }

            // 广播 DungeonReady
            _debugRoomViewCount = _roomViews.Count;
            EventCenter.Broadcast(GameEventType.DungeonReady, new DungeonReadyEvent());
        }

        /// <summary>
        /// 响应 RoomEntered：目标房间 Revealed，邻居 Hidden→Silhouette
        /// </summary>
        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (evt.Room == null) return;

            if (_roomViews.TryGetValue(evt.Room.Id, out var roomView))
            {
                roomView.SetVisibility(RoomVisibility.Revealed);
                UpdateNeighborFog(roomView);
            }
        }

        /// <summary>
        /// 将目标房间的相邻房间从 Hidden 提升到 Silhouette
        /// </summary>
        private void UpdateNeighborFog(RoomView targetView)
        {
            if (targetView.Room?.Doors == null || _currentMap == null) return;

            foreach (var door in targetView.Room.Doors)
            {
                if (_roomViews.TryGetValue(door.ConnectedRoomId, out var neighborView))
                {
                    if (neighborView.Visibility == RoomVisibility.Hidden)
                        neighborView.SetVisibility(RoomVisibility.Silhouette);
                }
            }
        }

        /// <summary>
        /// 销毁所有旧房间视图 GameObject，清空索引
        /// </summary>
        private void ClearPrevious()
        {
            foreach (var kvp in _roomViews)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                    Destroy(kvp.Value.gameObject);
            }
            _roomViews.Clear();
            _currentMap = null;
            _debugRoomViewCount = 0;
        }

        /// <summary>
        /// 计算房间 Prefab 根节点的世界坐标。
        /// 资源规范：根节点位于 cell(0,0) 的中心点。
        /// </summary>
        private static Vector3 CalculateRoomRootPosition(RoomInstance room)
        {
            var roomBounds = DungeonCamera.CalculateRoomBounds(room);
            var cells = RoomShapeUtil.GetCells(room.Shape);

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var cell in cells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }

            float spanX = Mathf.Max(1, maxX - minX + 1);
            float spanY = Mathf.Max(1, maxY - minY + 1);
            float cellWidth = roomBounds.width / spanX;
            float cellHeight = roomBounds.height / spanY;

            float rootX = roomBounds.xMin - (minX * cellWidth) + (cellWidth * 0.5f);
            float rootY = roomBounds.yMin - (minY * cellHeight) + (cellHeight * 0.5f);
            return new Vector3(rootX, rootY, 0f);
        }
    }
}
