using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 房间行为编排器。订阅 DungeonReady / RoomEntered / RoomCleared 事件，
    /// 维护 previousRoomId，统一调度 NotifyExit / NotifyEnter / NotifyClear。
    /// 场景级 MonoBehaviour，与 DungeonViewManager 同场景挂载。
    /// </summary>
    public class RoomBehaviorOrchestrator : MonoBehaviour
    {
        [SerializeField] private DungeonViewManager _viewManager; // 视图管理器引用

        private string _previousRoomId; // 上一个房间 Id（用于 OnExit 调度）
        private bool _missingViewManagerLogged; // 缺失引用错误是否已记录

        private void Awake()
        {
            ValidateViewManager();
        }

        private void OnEnable()
        {
            if (!ValidateViewManager()) return;

            EventCenter.AddListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.AddListener<RoomEnteredEvent>(
                GameEventType.RoomEntered, OnRoomEntered);
            EventCenter.AddListener<RoomClearedEvent>(
                GameEventType.RoomCleared, OnRoomCleared);
        }

        private bool ValidateViewManager()
        {
            if (_viewManager != null)
            {
                _missingViewManagerLogged = false;
                return true;
            }

            if (!_missingViewManagerLogged)
            {
                Debug.LogError("[RoomBehaviorOrchestrator] viewManager 未赋值，组件已禁用");
                _missingViewManagerLogged = true;
            }

            enabled = false;
            return false;
        }

        private void OnDisable()
        {
            EventCenter.RemoveListener<DungeonReadyEvent>(
                GameEventType.DungeonReady, OnDungeonReady);
            EventCenter.RemoveListener<RoomEnteredEvent>(
                GameEventType.RoomEntered, OnRoomEntered);
            EventCenter.RemoveListener<RoomClearedEvent>(
                GameEventType.RoomCleared, OnRoomCleared);
        }

        /// <summary>
        /// 响应 DungeonReady：调用起始房间 NotifyEnter，重置 previousRoomId
        /// </summary>
        private void OnDungeonReady(DungeonReadyEvent evt)
        {
            _previousRoomId = null;

            var dm = DungeonManager.Instance;
            if (dm == null || dm.CurrentRoom == null)
            {
                Debug.LogWarning("[RoomBehaviorOrchestrator] DungeonReady but no CurrentRoom");
                return;
            }

            var startRoomId = dm.CurrentRoom.Id;
            if (_viewManager.TryGetRoomView(startRoomId, out var startView))
            {
                startView.NotifyEnter();
                _previousRoomId = startRoomId;
            }
        }

        /// <summary>
        /// 响应 RoomEntered：先调度旧房间 OnExit，再调度新房间 OnEnter
        /// </summary>
        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (evt.Room == null) return;

            var newRoomId = evt.Room.Id;

            // 调度旧房间 OnExit
            if (!string.IsNullOrEmpty(_previousRoomId) && _previousRoomId != newRoomId)
            {
                if (_viewManager.TryGetRoomView(_previousRoomId, out var oldView))
                {
                    oldView.NotifyExit();
                }
            }

            // 调度新房间 OnEnter
            if (_viewManager.TryGetRoomView(newRoomId, out var newView))
            {
                newView.NotifyEnter();
            }

            _previousRoomId = newRoomId;
        }

        /// <summary>
        /// 响应 RoomCleared：调度当前房间 NotifyClear
        /// </summary>
        private void OnRoomCleared(RoomClearedEvent evt)
        {
            if (string.IsNullOrEmpty(evt.RoomId)) return;

            if (_viewManager.TryGetRoomView(evt.RoomId, out var roomView))
            {
                roomView.NotifyClear();
            }
        }
    }
}
