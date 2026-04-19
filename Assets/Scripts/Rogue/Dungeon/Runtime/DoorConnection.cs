using RogueDungeon.Rogue.Dungeon.Data;

namespace RogueDungeon.Rogue.Dungeon.Runtime
{
    /// <summary>
    /// 门连接数据，表示一个房间到另一个房间的单向门连接信息
    /// </summary>
    public readonly struct DoorConnection
    {
        public readonly DoorSlot LocalDoor;         // 本房间的门位
        public readonly string ConnectedRoomId;     // 连接的目标房间 Id
        public readonly DoorSlot RemoteDoor;        // 目标房间的门位

        /// <summary>
        /// 创建门连接
        /// </summary>
        /// <param name="localDoor">本房间的门位</param>
        /// <param name="connectedRoomId">目标房间 Id</param>
        /// <param name="remoteDoor">目标房间的门位</param>
        public DoorConnection(DoorSlot localDoor, string connectedRoomId, DoorSlot remoteDoor)
        {
            LocalDoor = localDoor;
            ConnectedRoomId = connectedRoomId;
            RemoteDoor = remoteDoor;
        }

        public override string ToString() =>
            $"DoorConnection({LocalDoor} -> {ConnectedRoomId}:{RemoteDoor})";
    }
}
