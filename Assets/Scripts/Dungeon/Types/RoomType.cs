namespace RogueDungeon.Dungeon.Types
{
    /// <summary>
    /// 房间功能类型枚举，定义地牢中各房间的用途
    /// </summary>
    public enum RoomType
    {
        Start  = 0, // 起始房间
        Normal = 1, // 普通战斗房间
        Elite  = 2, // 精英房间
        Event  = 4, // 事件房间
        Boss   = 5  // Boss 房间
    }
}
