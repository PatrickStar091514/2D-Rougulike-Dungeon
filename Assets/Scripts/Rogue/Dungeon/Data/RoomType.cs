namespace RogueDungeon.Rogue.Dungeon.Data
{
    /// <summary>
    /// 房间功能类型枚举，定义地牢中各房间的用途
    /// </summary>
    public enum RoomType
    {
        Start,  // 起始房间
        Normal, // 普通战斗房间
        Elite,  // 精英房间
        Shop,   // 商店房间
        Event,  // 事件房间
        Boss    // Boss 房间
    }
}
