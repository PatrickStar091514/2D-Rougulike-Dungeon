using System;
using System.Collections.Generic;

namespace RogueDungeon.Data.Runtime
{
    /// <summary>
    /// 待领取奖励记录，用于存档/恢复未领取的掉落奖励状态。
    /// 玩家清除房间后系统保存此记录，退出重进后可恢复。
    /// </summary>
    [Serializable]
    public class PendingReward
    {
        public string RoomId;                // 产生奖励的房间 ID
        public int Source;                   // 奖励来源（0=TwoChoice, 1=ThreeChoice, 2=Boss）
        public List<string> OfferedBuffIds;  // Roll 出的待选 Buff ID 列表

        public PendingReward()
        {
            OfferedBuffIds = new List<string>();
        }
    }
}
