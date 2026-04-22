namespace RogueDungeon.Core.Buff
{
    /// <summary>
    /// Buff 持续类型枚举，决定 Buff 的生命周期和叠加行为
    /// </summary>
    public enum DurationType
    {
        Permanent  = 0, // 永久：持续整局，指数递减叠加
        Timed      = 1, // 计时：N 秒后过期，重获刷新时长
        RoomScoped = 2, // 房间限定：N 个房间后过期，重获刷新房间数
        Stack      = 3, // 层数：按层叠加，重获增加层数
        Instant    = 4  // 即时：立即生效后丢弃，不进入 ActiveBuffs
    }
}
