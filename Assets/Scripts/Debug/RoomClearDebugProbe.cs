using UnityEngine;
using RogueDungeon.Core.Events;
using RogueDungeon.Dungeon.View;

namespace RogueDungeon.Debugging
{
    /// <summary>
    /// 房间清理状态调试探针。
    /// 在 Inspector 中修改 cleared，会通过 RoomCleared 语义事件驱动门解锁链路。
    /// </summary>
    public class RoomClearDebugProbe : MonoBehaviour
    {
        [SerializeField] private RoomView roomView; // 目标房间视图
        [SerializeField] private bool cleared; // 调试开关：房间是否清空

        private bool _lastCleared; // 上次调试值

        private void Awake()
        {
            if (roomView == null)
                roomView = GetComponent<RoomView>();

            if (roomView != null && roomView.Room != null)
                cleared = roomView.Room.Cleared;

            _lastCleared = cleared;
        }

        private void Update()
        {
            if (!Application.isPlaying || roomView == null || roomView.Room == null) return;

            // Inspector 改值 -> 同步到运行时并走语义事件链路
            if (cleared != _lastCleared)
            {
                _lastCleared = cleared;
                if (roomView.Room.Cleared != cleared)
                {
                    roomView.Room.Cleared = cleared;
                    if (cleared)
                    {
                        EventCenter.Broadcast(
                            GameEventType.RoomCleared,
                            new RoomClearedEvent { RoomId = roomView.RoomId });
                    }
                }
                return;
            }

            // 运行时状态变化 -> 回写 Inspector
            if (cleared != roomView.Room.Cleared)
            {
                cleared = roomView.Room.Cleared;
                _lastCleared = cleared;
            }
        }
    }
}
