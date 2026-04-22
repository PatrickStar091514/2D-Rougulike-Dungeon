using System;
using System.Collections.Generic;
using RogueDungeon.Data.Config;

namespace RogueDungeon.Rogue.Dungeon.Reward
{
    /// <summary>
    /// 奖励随机工具。提供确定性、按权重、不重复的 Buff 候选 Roll。
    /// </summary>
    public static class RewardRoller
    {
        /// <summary>
        /// 从掉落池中按权重抽取不重复 BuffId。
        /// </summary>
        /// <param name="pool">Buff 掉落池配置</param>
        /// <param name="seed">确定性随机种子</param>
        /// <param name="count">请求数量</param>
        /// <returns>BuffId 列表（无重复）</returns>
        public static List<string> Roll(BuffPoolSO pool, int seed, int count)
        {
            var candidates = BuildCandidates(pool);
            if (count <= 0 || candidates.Count == 0)
                return new List<string>();

            if (count >= candidates.Count)
                return new List<string>(candidates.Keys);

            var rng = new Random(seed);
            var working = new List<Candidate>(candidates.Values);
            var result = new List<string>(count);

            for (int i = 0; i < count && working.Count > 0; i++)
            {
                int totalWeight = 0;
                for (int j = 0; j < working.Count; j++)
                    totalWeight += working[j].Weight;

                if (totalWeight <= 0)
                    break;

                int roll = rng.Next(0, totalWeight);
                int cumulative = 0;
                int selectedIndex = 0;
                for (int j = 0; j < working.Count; j++)
                {
                    cumulative += working[j].Weight;
                    if (roll < cumulative)
                    {
                        selectedIndex = j;
                        break;
                    }
                }

                result.Add(working[selectedIndex].BuffId);
                working.RemoveAt(selectedIndex);
            }

            return result;
        }

        /// <summary>
        /// 从掉落池最高稀有度层级中按权重选取一个 BuffId。
        /// </summary>
        /// <param name="pool">Buff 掉落池配置</param>
        /// <param name="seed">确定性随机种子</param>
        /// <returns>选中的 BuffId，空池返回 null</returns>
        public static string RollBoss(BuffPoolSO pool, int seed)
        {
            var candidates = BuildCandidates(pool);
            if (candidates.Count == 0)
                return null;

            int highestRarity = int.MinValue;
            foreach (var candidate in candidates.Values)
            {
                int rarity = (int)candidate.Config.Rarity;
                if (rarity > highestRarity)
                    highestRarity = rarity;
            }

            var highestTier = new List<Candidate>();
            foreach (var candidate in candidates.Values)
            {
                if ((int)candidate.Config.Rarity == highestRarity)
                    highestTier.Add(candidate);
            }

            if (highestTier.Count == 0)
                return null;

            int totalWeight = 0;
            for (int i = 0; i < highestTier.Count; i++)
                totalWeight += highestTier[i].Weight;

            var rng = new Random(seed);
            int roll = rng.Next(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < highestTier.Count; i++)
            {
                cumulative += highestTier[i].Weight;
                if (roll < cumulative)
                    return highestTier[i].BuffId;
            }

            return highestTier[highestTier.Count - 1].BuffId;
        }

        private static Dictionary<string, Candidate> BuildCandidates(BuffPoolSO pool)
        {
            // 基于掉落池配置构建 BuffId → Candidate 映射，合并重复 BuffId 的权重
            var candidates = new Dictionary<string, Candidate>(StringComparer.Ordinal);
            if (pool == null || pool.Entries == null)
                return candidates;

            var entries = pool.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                // 过滤掉无效条目：Buff 配置缺失、权重非正、BuffId 为空
                if (entry.Buff == null || entry.Weight <= 0 || string.IsNullOrEmpty(entry.Buff.BuffId))
                    continue;
                
                // 合并重复 BuffId 的权重
                if (candidates.TryGetValue(entry.Buff.BuffId, out var existing))
                {
                    existing.Weight += entry.Weight;
                    candidates[entry.Buff.BuffId] = existing;
                    continue;
                }

                // 添加新候选
                candidates.Add(entry.Buff.BuffId, new Candidate
                {
                    BuffId = entry.Buff.BuffId,
                    Config = entry.Buff,
                    Weight = entry.Weight
                });
            }

            return candidates;
        }

        private struct Candidate
        {
            public string BuffId;
            public BuffConfigSO Config;
            public int Weight;
        }
    }
}
