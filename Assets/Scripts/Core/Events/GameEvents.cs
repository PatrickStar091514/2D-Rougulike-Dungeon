using RogueDungeon.Core.Buff;

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
        public int EnemyInstanceID;  // 敌人 GameObject 实例 ID (GetInstanceID)，用于 EnemyRegisterManager 反向查找
        public string EnemyId;       // 敌人逻辑 ID
        public string RoomId;        // 所在房间 ID
        public int DropSeed;         // 掉落随机种子
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

    /// <summary>
    /// Buff 被应用事件 Payload
    /// </summary>
    public struct BuffAppliedEvent
    {
        public BuffSnapshot Snapshot;  // Buff 运行时快照（自包含，消费者无需反向查找）
        public string SourceId;        // 来源标识
        public int StackCount;         // 应用后的叠加层数
    }

    /// <summary>
    /// Buff 过期移除事件 Payload
    /// </summary>
    public struct BuffExpiredEvent
    {
        public BuffSnapshot Snapshot; // Buff 运行时快照
    }

    /// <summary>
    /// Buff 叠加层数变化事件 Payload
    /// </summary>
    public struct BuffStackChangedEvent
    {
        public BuffSnapshot Snapshot; // Buff 运行时快照
        public int OldStack;          // 变化前层数
        public int NewStack;          // 变化后层数
    }

    /// <summary>
    /// 奖励领取事件 Payload
    /// </summary>
    public struct RewardClaimedEvent
    {
        public BuffSnapshot Snapshot; // Buff 运行时快照
        public string RoomId;         // 奖励所在房间 ID
    }

    /// <summary>
    /// Boss 被击杀事件 Payload（通知生成 Portal）
    /// </summary>
    public struct FloorBossDefeatedEvent
    {
        public string RoomId;     // Boss 房间 ID
        public int FloorIndex;    // 当前楼层索引
    }

    /// <summary>
    /// 楼层完成事件 Payload（玩家进入 Portal 后触发）
    /// </summary>
    public struct FloorCompletedEvent
    {
        public int FromFloorIndex; // 离开的楼层索引
        public int ToFloorIndex;   // 进入的楼层索引
    }
}
