using System.Collections.Generic;

namespace RogueDungeon.Data.Runtime
{
    /// <summary>
    /// 单局运行时状态，承载一次 Run 的全部可变数据，生命周期限定为单局
    /// </summary>
    [System.Serializable]
    public class RunState
    {
        public string RunId;                       // 本局唯一标识
        public int Seed;                           // 全局随机种子
        public int FloorIndex;                     // 当前层索引
        public int RoomIndex;                      // 当前房间索引
        public float ElapsedTime;                  // 已用时间（秒）
        public int CurrentHP;                      // 当前生命值
        public List<BuffInstance> ActiveBuffs;      // 激活 Buff 实例列表
        public List<SerializableKeyValue<string, int>> Inventory; // 物品快照（可序列化）
        public PendingReward PendingReward;        // 待领取奖励（可为 null）

        /// <summary>
        /// 创建默认的 RunState 实例，初始化集合字段
        /// </summary>
        public RunState()
        {
            ActiveBuffs = new List<BuffInstance>();
            Inventory = new List<SerializableKeyValue<string, int>>();
        }
    }
}
