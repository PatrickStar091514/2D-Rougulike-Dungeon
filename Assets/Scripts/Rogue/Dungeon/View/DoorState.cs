namespace RogueDungeon.Rogue.Dungeon.View
{
    /// <summary>
    /// 门状态枚举，控制门的交互与视觉表现
    /// </summary>
    public enum DoorState
    {
        Locked,   // 锁定状态，不可通过
        Unlocked, // 解锁状态，可触发通行
        Transit   // 传送中，临时不可交互
    }
}
