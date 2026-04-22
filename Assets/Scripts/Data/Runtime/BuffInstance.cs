using System;

namespace RogueDungeon.Data.Runtime
{
    /// <summary>
    /// Buff 运行时实例，记录单个 Buff 的当前状态（叠加数、剩余时间等）。
    /// 扁平结构：所有持续类型字段共存，按 DurationType 决定哪些字段有效。
    /// </summary>
    [Serializable]
    public class BuffInstance
    {
        public string BuffId;       // 对应 BuffConfigSO.BuffId
        public int StackCount;      // 当前叠加层数（≥1）
        public float RemainingTime; // 剩余时间（秒），仅 Timed 有效
        public int RemainingRooms;  // 剩余房间数，仅 RoomScoped 有效
        public string SourceId;     // 来源标识
    }
}
