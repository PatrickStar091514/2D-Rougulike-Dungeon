using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Rogue.Dungeon
{
    /// <summary>
    /// DungeonManager 生成地牢后广播的事件 Payload
    /// </summary>
    public struct DungeonGeneratedEvent
    {
        public DungeonMap Map; // 生成的地牢地图
    }

    /// <summary>
    /// DungeonManager 在房间切换后广播的事件 Payload
    /// </summary>
    public struct RoomEnteredEvent
    {
        public RoomInstance Room; // 进入的房间
    }
}
