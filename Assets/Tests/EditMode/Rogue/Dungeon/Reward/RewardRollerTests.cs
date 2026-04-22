using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Core.Buff;
using RogueDungeon.Data.Config;
using RogueDungeon.Rogue.Dungeon.Reward;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// RewardRoller 的 EditMode 单元测试。
    /// </summary>
    public class RewardRollerTests
    {
        [Test]
        public void Roll_SameSeed_ReturnsSameResult()
        {
            var pool = CreatePool(
                CreateEntry("a", Rarity.Common, 1),
                CreateEntry("b", Rarity.Common, 2),
                CreateEntry("c", Rarity.Common, 3));

            var first = RewardRoller.Roll(pool, 12345, 2);
            var second = RewardRoller.Roll(pool, 12345, 2);

            CollectionAssert.AreEqual(first, second);
            DestroyPool(pool);
        }

        [Test]
        public void Roll_ResultHasNoDuplicates()
        {
            var pool = CreatePool(
                CreateEntry("a", Rarity.Common, 1),
                CreateEntry("b", Rarity.Common, 1),
                CreateEntry("c", Rarity.Common, 1));

            var result = RewardRoller.Roll(pool, 1, 3);
            Assert.AreEqual(result.Count, result.Distinct().Count());
            DestroyPool(pool);
        }

        [Test]
        public void Roll_CountGreaterThanPoolSize_ReturnsAll()
        {
            var pool = CreatePool(
                CreateEntry("a", Rarity.Common, 1),
                CreateEntry("b", Rarity.Common, 1));

            var result = RewardRoller.Roll(pool, 7, 5);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("a"));
            Assert.IsTrue(result.Contains("b"));
            DestroyPool(pool);
        }

        [Test]
        public void Roll_EmptyPool_ReturnsEmpty()
        {
            var pool = ScriptableObject.CreateInstance<BuffPoolSO>();
            SetField(pool, "entries", new BuffEntry[0]);

            var result = RewardRoller.Roll(pool, 0, 3);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Object.DestroyImmediate(pool);
        }

        [Test]
        public void Roll_ZeroWeightEntries_AreExcluded()
        {
            var pool = CreatePool(
                CreateEntry("a", Rarity.Common, 1),
                CreateEntry("b", Rarity.Common, 0));

            var result = RewardRoller.Roll(pool, 99, 2);
            Assert.IsTrue(result.Contains("a"));
            Assert.IsFalse(result.Contains("b"));
            DestroyPool(pool);
        }

        [Test]
        public void RollBoss_SelectsHighestRarityTier()
        {
            var pool = CreatePool(
                CreateEntry("common", Rarity.Common, 1),
                CreateEntry("epic_1", Rarity.Epic, 1),
                CreateEntry("epic_2", Rarity.Epic, 1),
                CreateEntry("rare", Rarity.Rare, 10));

            string boss = RewardRoller.RollBoss(pool, 42);
            Assert.IsTrue(boss == "epic_1" || boss == "epic_2");
            DestroyPool(pool);
        }

        [Test]
        public void RollBoss_EmptyPool_ReturnsNull()
        {
            var pool = ScriptableObject.CreateInstance<BuffPoolSO>();
            SetField(pool, "entries", new BuffEntry[0]);

            string result = RewardRoller.RollBoss(pool, 42);
            Assert.IsNull(result);
            Object.DestroyImmediate(pool);
        }

        private static BuffEntry CreateEntry(string buffId, Rarity rarity, int weight)
        {
            return new BuffEntry
            {
                Buff = CreateBuffConfig(buffId, rarity),
                Weight = weight
            };
        }

        private static BuffPoolSO CreatePool(params BuffEntry[] entries)
        {
            var pool = ScriptableObject.CreateInstance<BuffPoolSO>();
            SetField(pool, "entries", entries);
            return pool;
        }

        private static BuffConfigSO CreateBuffConfig(string id, Rarity rarity)
        {
            var config = ScriptableObject.CreateInstance<BuffConfigSO>();
            SetField(config, "buffId", id);
            SetField(config, "rarity", rarity);
            SetField(config, "duration", DurationType.Permanent);
            SetField(config, "modifiers", new StatModifier[0]);
            return config;
        }

        private static void DestroyPool(BuffPoolSO pool)
        {
            if (pool?.Entries != null)
            {
                foreach (var entry in pool.Entries)
                {
                    if (entry.Buff != null)
                        Object.DestroyImmediate(entry.Buff);
                }
            }

            if (pool != null)
                Object.DestroyImmediate(pool);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"字段 {fieldName} 未找到");
            field.SetValue(target, value);
        }
    }
}
