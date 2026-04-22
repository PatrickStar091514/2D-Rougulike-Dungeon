using System;
using UnityEngine;

namespace RogueDungeon.Data.Config
{
    /// <summary>
    /// Buff 条目，包含 Buff 配置引用和权重
    /// </summary>
    [Serializable]
    public struct BuffEntry
    {
        public BuffConfigSO Buff; // Buff 配置引用
        [Min(1)]
        public int Weight;        // 权重（≥1）
    }

    /// <summary>
    /// 全局 Buff 掉落池配置，包含带权重的 Buff 条目列表。
    /// 所有奖励来源（Normal/Elite/Event/Boss）从同一池子中 Roll。
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuffPool", menuName = "Data/Buff Pool")]
    public class BuffPoolSO : ScriptableObject
    {
        [SerializeField] private BuffEntry[] entries; // 掉落池条目

        public BuffEntry[] Entries => entries;

        /// <summary>
        /// 通过 BuffId 查找对应的 BuffConfigSO
        /// </summary>
        /// <param name="buffId">Buff 唯一标识</param>
        /// <returns>匹配的 BuffConfigSO，未找到返回 null</returns>
        public BuffConfigSO FindByBuffId(string buffId)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Buff != null && entries[i].Buff.BuffId == buffId)
                    return entries[i].Buff;
            }
            return null;
        }
    }
}
