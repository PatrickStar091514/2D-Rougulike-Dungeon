namespace RogueDungeon.Core.Events
{
    /// <summary>
    /// 事件类型枚举，标识事件中心可分发的所有事件
    /// </summary>
    public enum GameEventType
    {
        GameStateChanged,   // 游戏状态切换
        PlayerDamaged,      // 玩家受到伤害
        PlayerDied,         // 玩家死亡
        EnemyDamaged,       // 敌人受到伤害
        EnemyDied,          // 敌人死亡
        RoomCleared,        // 房间清空
        RunEnded,           // 本局结束
        RunReady,           // Run 创建/恢复完成
        DungeonGenerated,   // 地牢生成完成
        RoomEntered,        // 进入新房间
        DungeonReady        // 地牢视觉实例化完成
    }
}
