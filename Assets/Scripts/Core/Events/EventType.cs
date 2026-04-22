namespace RogueDungeon.Core.Events
{
    /// <summary>
    /// 事件类型枚举，标识事件中心可分发的所有事件
    /// </summary>
    public enum GameEventType
    {
        GameStateChanged = 0,  // 游戏状态切换
        PlayerDamaged = 1,     // 玩家受到伤害
        PlayerDied = 2,        // 玩家死亡
        EnemyDamaged = 3,      // 敌人受到伤害
        EnemyDied = 4,         // 敌人死亡
        RoomCleared = 5,       // 房间清空
        RunEnded = 6,          // 本局结束
        RunReady = 7,          // Run 创建/恢复完成
        DungeonGenerated = 8,  // 地牢生成完成
        RoomEntered = 9,       // 进入新房间
        DungeonReady = 10,     // 地牢视觉实例化完成
        BuffApplied = 11,      // Buff 被应用
        BuffExpired = 12,      // Buff 过期移除
        BuffStackChanged = 13, // Buff 叠加层数变化
        RewardClaimed = 14     // 奖励已领取
    }
}
