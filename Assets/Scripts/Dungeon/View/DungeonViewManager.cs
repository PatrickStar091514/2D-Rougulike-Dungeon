using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Core.Pool;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.Config;
using RogueDungeon.Dungeon.Map;
using RogueDungeon.Data.Runtime;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 地牢视图管理器，消费 DungeonGenerated 事件执行全量实例化，
    /// 响应 RoomEntered 更新迷雾状态，完成后广播 DungeonReady。
    /// 场景级 MonoBehaviour（非 Singleton），生命周期与场景绑定。
    /// 支持双 FloorRoot 滑动窗口预加载，房间实例通过 ObjectPool 复用。
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

        private Transform[] _floorRoots; // 双 Floor Root 节点（Size=2）
        private int _activeFloorRootIndex; // 当前活跃 FloorRoot 索引

        [Header("Runtime Debug")]
        [SerializeField] private int _debugRoomViewCount; // 已实例化房间视图数
        [SerializeField] private int _debugActiveFloorRoot; // 当前活跃 FloorRoot 索引
        [SerializeField] private int _debugFloor0RoomCount; // Floor 0 房间数
        [SerializeField] private int _debugFloor1RoomCount; // Floor 1 房间数

        private void OnValidate()
        {
            if (_cellWorldSize < MinCellWorldSize)
                _cellWorldSize = MinCellWorldSize;
            ApplyCellWorldSize();
        }

        private void Awake()
        {
            EnsureFloorRoots();
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
        /// 确保双 FloorRoot 子节点存在
        /// </summary>
        private void EnsureFloorRoots()
        {
            if (_floorRoots != null && _floorRoots.Length == 2
                && _floorRoots[0] != null && _floorRoots[1] != null)
                return;

            _floorRoots = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var rootName = $"FloorRoot_{i}";
                var existing = transform.Find(rootName);
                if (existing != null)
                {
                    _floorRoots[i] = existing;
                }
                else
                {
                    var go = new GameObject(rootName);
                    go.transform.SetParent(transform);
                    go.transform.localPosition = Vector3.zero;
                    go.SetActive(false);
                    _floorRoots[i] = go.transform;
                }
            }
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
        /// 切换活跃 FloorRoot
        /// </summary>
        /// <param name="slotIndex">目标 Slot 索引（0 或 1）</param>
        public void SwitchActiveRoot(int slotIndex)
        {
            if (_floorRoots == null || slotIndex < 0 || slotIndex >= _floorRoots.Length)
                return;

            // 停用旧 Root
            if (_floorRoots[_activeFloorRootIndex] != null)
                _floorRoots[_activeFloorRootIndex].gameObject.SetActive(false);

            _activeFloorRootIndex = slotIndex;
            _debugActiveFloorRoot = slotIndex;

            // 激活新 Root
            if (_floorRoots[slotIndex] != null)
                _floorRoots[slotIndex].gameObject.SetActive(true);

        }

        /// <summary>
        /// 分帧实例化所有房间到指定 Slot（协程，供预加载使用）
        /// </summary>
        /// <param name="map">目标地牢地图</param>
        /// <param name="slotIndex">目标 Slot 索引</param>
        public IEnumerator InstantiateFloorAsync(DungeonMap map, int slotIndex)
        {
            if (map == null || map.AllRooms == null)
            {
                Debug.LogWarning("[DungeonViewManager] InstantiateFloorAsync: map 为空");
                yield break;
            }

            EnsureFloorRoots();
            var root = _floorRoots[slotIndex];
            if (root == null)
            {
                Debug.LogError($"[DungeonViewManager] FloorRoot_{slotIndex} 不存在");
                yield break;
            }

            int count = 0;
            foreach (var room in map.AllRooms)
            {
                var go = InstantiateRoom(room, root);
                if (go == null) continue;

                var roomView = go.GetComponent<RoomView>();
                if (roomView == null) roomView = go.AddComponent<RoomView>();
                roomView.Initialize(room); // 迷雾、门、行为

                count++;
                yield return null; // 分帧
            }

            root.gameObject.SetActive(false);

            if (slotIndex == 0) _debugFloor0RoomCount = count;
            else _debugFloor1RoomCount = count;

        }

        /// <summary>
        /// 释放指定 Slot 下所有房间视图到对象池
        /// </summary>
        /// <param name="slotIndex">目标 Slot 索引</param>
        public void ReleaseFloorSlot(int slotIndex)
        {
            if (_floorRoots == null || slotIndex < 0 || slotIndex >= _floorRoots.Length)
                return;

            var root = _floorRoots[slotIndex];
            if (root == null) return;

            var views = root.GetComponentsInChildren<RoomView>(true);
            foreach (var view in views)
            {
                if (view == null) continue;
                var roomId = view.RoomId;
                if (!string.IsNullOrEmpty(roomId))
                    _roomViews.Remove(roomId);

                var templateId = view.Room?.Template?.TemplateId;
                if (!string.IsNullOrEmpty(templateId))
                {
                    var poolKey = $"Room_{templateId}";
                    ObjectPool.Instance.Release(poolKey, view.gameObject);
                }
                else
                {
                    // 无法确定池 Key，直接销毁
                    Destroy(view.gameObject);
                }
            }

            if (slotIndex == 0) _debugFloor0RoomCount = 0;
            else _debugFloor1RoomCount = 0;
        }

        /// <summary>
        /// 将指定 Slot 下所有已初始化的房间视图注册到 _roomViews 字典（过渡时调用）
        /// </summary>
        /// <param name="slotIndex">目标 Slot 索引</param>
        public void RegisterFloorRooms(int slotIndex)
        {
            if (_floorRoots == null || slotIndex < 0 || slotIndex >= _floorRoots.Length) return;
            var root = _floorRoots[slotIndex];
            if (root == null) return;

            var views = root.GetComponentsInChildren<RoomView>(true);
            foreach (var view in views)
            {
                if (view == null || string.IsNullOrEmpty(view.RoomId)) continue;
                _roomViews[view.RoomId] = view;
            }
        }

        /// <summary>
        /// 更新当前地图引用（过渡时调用）
        /// </summary>
        /// <param name="map">新楼层地牢地图</param>
        public void SetCurrentMap(DungeonMap map)
        {
            _currentMap = map;
        }

        /// <summary>
        /// 响应 DungeonGenerated：释放旧 Slot → 全量实例化到新 Slot → 初始化迷雾 → 广播 DungeonReady
        /// </summary>
        private void OnDungeonGenerated(DungeonGeneratedEvent evt)
        {
            var map = evt.Map;
            if (map == null || map.AllRooms == null || map.AllRooms.Count == 0)
            {
                Debug.LogWarning("[DungeonViewManager] Received empty DungeonMap, skipping instantiation");
                return;
            }

            EnsureFloorRoots();

            // 首层 DungeonGenerated 时释放旧视图
            ReleasePreviousIfAny();
            _currentMap = map;

            // 确定目标 Slot
            var dm = Dungeon.DungeonManager.Instance;
            int targetSlot = dm != null
                ? (RunManager.Instance?.CurrentRun?.FloorIndex ?? 0) % 2
                : 0;

            var root = _floorRoots[targetSlot];

            // 全量实例化所有房间到目标 Slot
            foreach (var room in map.AllRooms)
            {
                var go = InstantiateRoom(room, root);
                if (go == null) continue;

                var roomView = go.GetComponent<RoomView>();
                if (roomView == null)
                    roomView = go.AddComponent<RoomView>();

                roomView.Initialize(room);
                _roomViews[room.Id] = roomView;
            }

            // 激活目标 FloorRoot
            SwitchActiveRoot(targetSlot);

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
        /// 从对象池获取房间实例（若池空则 Instantiate 新实例）
        /// </summary>
        private static GameObject InstantiateRoom(RoomInstance room, Transform parent)
        {
            if (room.Template == null || room.Template.Prefab == null)
            {
                Debug.LogWarning($"[DungeonViewManager] Room '{room.Id}' has no template/prefab, skipping");
                return null;
            }

            var worldPos = CalculateRoomRootPosition(room);
            var poolKey = $"Room_{room.Template.TemplateId}";

            var go = ObjectPool.Instance.Get(poolKey, room.Template.Prefab);
            go.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            go.transform.SetParent(parent);
            go.name = $"Room_{room.Id}";

            return go;
        }

        /// <summary>
        /// 释放之前活跃 Slot 下的所有房间视图
        /// </summary>
        private void ReleasePreviousIfAny()
        {
            if (_floorRoots == null) return;

            int oldSlot = _activeFloorRootIndex;
            if (_floorRoots[oldSlot] == null) return;

            var views = _floorRoots[oldSlot].GetComponentsInChildren<RoomView>(true);
            foreach (var view in views)
            {
                if (view == null) continue;
                var roomId = view.RoomId;
                if (!string.IsNullOrEmpty(roomId))
                    _roomViews.Remove(roomId);

                var templateId = view.Room?.Template?.TemplateId;
                if (!string.IsNullOrEmpty(templateId))
                {
                    var poolKey = $"Room_{templateId}";
                    ObjectPool.Instance.Release(poolKey, view.gameObject);
                }
            }

            _currentMap = null;
            _debugRoomViewCount = 0;
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
