using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon.View;

namespace RogueDungeon.Debugging
{
    /// <summary>
    /// Debug 资源统一安装器。
    /// 在 DungeonReady 后为所有房间视图补齐调试探针等调试组件。
    /// </summary>
    public class DebugRuntimeInstaller : MonoBehaviour
    {
        [SerializeField] private DungeonViewManager viewManager; // 房间视图管理器
        [SerializeField] private bool installRoomClearProbe = true; // 是否安装房间清理调试探针

        private void Awake()
        {
            if (viewManager == null)
                viewManager = FindFirstObjectByType<DungeonViewManager>();
        }

        private void OnEnable()
        {
            EventCenter.AddListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<DungeonReadyEvent>(GameEventType.DungeonReady, OnDungeonReady);
        }

        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            if (!installRoomClearProbe || viewManager == null) return;

            foreach (var roomView in viewManager.AllRoomViews)
            {
                if (roomView == null) continue;
                if (roomView.GetComponent<RoomClearDebugProbe>() == null)
                    roomView.gameObject.AddComponent<RoomClearDebugProbe>();
            }
        }
    }
}
