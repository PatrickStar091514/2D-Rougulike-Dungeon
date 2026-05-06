using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon;
using RogueDungeon.Dungeon.View;

namespace RogueDungeon.Dungeon.Portal
{
    /// <summary>
    /// 传送门管理器。DDOL Singleton，订阅 FloorBossDefeated 事件，
    /// 在 Boss 房间 Prefab 子物体中查找 Portal 组件并激活，
    /// 监听预加载完成通知将 Portal 设为 Ready 状态。
    /// </summary>
    /// <remarks>
    /// <b>依赖约定</b>：房间 Prefab 内应包含名为 "Portal" 的子 GameObject，
    /// 其上挂载 Portal 组件。PortalManager 仅负责激活/停用，不负责创建/销毁。
    /// </remarks>
    public class PortalManager : MonoBehaviour
    {
        public static PortalManager Instance { get; private set; }

        private Portal _activePortal; // 当前活跃的传送门引用

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RegisterEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnregisterEvents();
        }

        private void RegisterEvents()
        {
            UnregisterEvents();
            EventCenter.AddListener<FloorBossDefeatedEvent>(GameEventType.FloorBossDefeated, OnFloorBossDefeated);
        }

        private void UnregisterEvents()
        {
            EventCenter.RemoveListener<FloorBossDefeatedEvent>(GameEventType.FloorBossDefeated, OnFloorBossDefeated);
        }

        private void Update()
        {
            if (_activePortal == null) return;
            if (_activePortal.State != PortalState.Loading) return;

            var dm = DungeonManager.Instance;
            if (dm != null && dm.NextFloorReady)
                _activePortal.SetReady();
        }

        private void OnFloorBossDefeated(FloorBossDefeatedEvent evt)
        {
            var dm = DungeonManager.Instance;
            if (dm == null || dm.TotalFloors <= 0) return;

            int nextFloor = evt.FloorIndex + 1;
            ActivatePortalInRoom(evt.RoomId, nextFloor);

            // 最终层无需预加载，直接设为 Ready
            if (nextFloor >= dm.TotalFloors && _activePortal != null)
                _activePortal.SetReady();
        }

        /// <summary>
        /// 在指定房间的 Prefab 子物体中查找 Portal 并激活
        /// </summary>
        /// <param name="roomId">Boss 房间 ID</param>
        /// <param name="targetFloorIndex">目标楼层索引</param>
        private void ActivatePortalInRoom(string roomId, int targetFloorIndex)
        {
            var viewManager = FindFirstObjectByType<DungeonViewManager>();
            if (viewManager == null)
            {
                Debug.LogWarning("[PortalManager] DungeonViewManager 不存在，无法激活 Portal");
                return;
            }

            if (!viewManager.TryGetRoomView(roomId, out var roomView))
            {
                Debug.LogWarning($"[PortalManager] 未找到 Boss 房间视图: {roomId}");
                return;
            }

            // 在房间 Prefab 子物体中查找 Portal 组件（包含非活跃物体）
            var portal = roomView.GetComponentInChildren<Portal>(true);
            if (portal == null)
            {
                Debug.LogWarning($"[PortalManager] Boss 房间 '{roomId}' 的 Prefab 中未找到 Portal 子物体，请确保房间 Prefab 包含挂载 Portal 组件的子 GameObject");
                return;
            }

            portal.Activate(targetFloorIndex);
            _activePortal = portal;

            // 若预加载已提前完成，立即设为 Ready
            var dm = DungeonManager.Instance;
            if (dm != null && dm.NextFloorReady)
                portal.SetReady();

        }
    }
}
