namespace RogueDungeon.Core.Events
{
    /// <summary>
    /// 游戏状态切换事件 Payload
    /// </summary>
    public struct GameStateChangedEvent
    {
        public GameState FromState; // 切换前状态
        public GameState ToState;   // 切换后状态
        public string RunId;        // 当前 RunId（Hub 阶段为空）
    }

    /// <summary>
    /// 玩家受伤事件 Payload
    /// </summary>
    public struct PlayerDamagedEvent
    {
        public string EntityId;  // 玩家实体 ID
        public int DeltaHP;      // 伤害量（负值）
        public string SourceId;  // 伤害来源 ID
        public string RoomId;    // 所在房间 ID
    }

    /// <summary>
    /// 玩家死亡事件 Payload
    /// </summary>
    public struct PlayerDiedEvent
    {
        public string EntityId; // 玩家实体 ID
        public string RoomId;   // 所在房间 ID
        public string RunId;    // 当前 RunId
    }

    /// <summary>
    /// 敌人受伤事件 Payload
    /// </summary>
    public struct EnemyDamagedEvent
    {
        public string EnemyId;   // 敌人实体 ID
        public int DeltaHP;      // 伤害量（负值）
        public string SourceId;  // 伤害来源 ID
        public string RoomId;    // 所在房间 ID
    }

    /// <summary>
    /// 敌人死亡事件 Payload
    /// </summary>
    public struct EnemyDiedEvent
    {
        public string EnemyId;  // 敌人 ID
        public string RoomId;   // 所在房间 ID
        public int DropSeed;    // 掉落随机种子
    }

    /// <summary>
    /// 房间清空事件 Payload
    /// </summary>
    public struct RoomClearedEvent
    {
        public string RoomId;     // 房间 ID
        public float ElapsedTime; // 该房间用时（秒）
    }

    /// <summary>
    /// 本局结束事件 Payload
    /// </summary>
    public struct RunEndedEvent
    {
        public string RunId;         // 本局 ID
        public bool IsVictory;       // 是否胜利
        public int Floor;            // 到达层数
        public string RewardSummary; // 奖励摘要
    }
}
