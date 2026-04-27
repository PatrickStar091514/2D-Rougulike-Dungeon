using UnityEngine;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 起始房间行为。OnEnter 仅记录日志，不锁门。OnClear/OnExit 无操作。
    /// </summary>
    public class StartRoomBehavior : IRoomBehavior
    {
        /// <inheritdoc/>
        public void OnEnter(RoomView room)
        {
            Debug.Log($"[StartRoomBehavior] Start room entered: {room.RoomId}");
        }

        /// <inheritdoc/>
        public void OnClear(RoomView room) { }

        /// <inheritdoc/>
        public void OnExit(RoomView room) { }
    }
}
