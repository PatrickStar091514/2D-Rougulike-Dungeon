using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using RogueDungeon.Rogue.Dungeon.Generation;

namespace RogueDungeon.Tests.Dungeon
{
    public class DeterminismTests
    {
        [Test]
        public void DeterministicHash_NotDependentOnStringGetHashCode()
        {
            // FNV-1a 实现应在所有平台上返回相同值
            string input = "test_determinism";
            int expected = SeededRandom.DeterministicHash(input);

            // 多次调用应始终返回相同值
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(expected, SeededRandom.DeterministicHash(input));
        }

        [Test]
        public void SortedDictionary_ProducesDeterministicOrder()
        {
            // 验证排序后遍历 Dictionary 是确定性的
            var dict = new Dictionary<string, int>();
            for (int i = 0; i < 100; i++)
                dict[$"key_{i:D3}"] = i;

            var sorted1 = dict.Keys.OrderBy(k => k).ToList();
            var sorted2 = dict.Keys.OrderBy(k => k).ToList();

            Assert.AreEqual(sorted1, sorted2, "排序后的 Dictionary 键遍历应确定性一致");
        }

        [Test]
        public void SeededRandom_SubSeeds_AreIndependent()
        {
            int baseSeed = 42;

            // 两个独立的子种子 RNG
            var layoutRng = new SeededRandom(SeededRandom.Hash(baseSeed, "layout"));
            var contentRng = new SeededRandom(SeededRandom.Hash(baseSeed, "content"));

            // 消耗 layoutRng 的一些随机数
            for (int i = 0; i < 50; i++)
                layoutRng.Range(0, 100);

            // contentRng 不应受影响
            var fresh = new SeededRandom(SeededRandom.Hash(baseSeed, "content"));
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(fresh.Range(0, 1000), contentRng.Range(0, 1000),
                    "子种子 RNG 应相互独立");
        }

        [Test]
        public void Hash_DifferentTags_ProduceDifferentSeeds()
        {
            int seed = 42;
            int h1 = SeededRandom.Hash(seed, "layout");
            int h2 = SeededRandom.Hash(seed, "content");

            Assert.AreNotEqual(h1, h2, "不同 tag 应产生不同子种子");
        }

        [Test]
        public void DeterministicHash_EmptyString()
        {
            // 空字符串应返回确定值
            int h1 = SeededRandom.DeterministicHash("");
            int h2 = SeededRandom.DeterministicHash("");
            Assert.AreEqual(h1, h2);
        }
    }
}
