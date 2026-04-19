using NUnit.Framework;
using RogueDungeon.Rogue.Dungeon.Generation;

namespace RogueDungeon.Tests.Dungeon
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var rng1 = new SeededRandom(42);
            var rng2 = new SeededRandom(42);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(rng1.Range(0, 1000), rng2.Range(0, 1000));
        }

        [Test]
        public void DifferentSeed_ProducesDifferentSequence()
        {
            var rng1 = new SeededRandom(42);
            var rng2 = new SeededRandom(99);

            bool allSame = true;
            for (int i = 0; i < 10; i++)
            {
                if (rng1.Range(0, 1000) != rng2.Range(0, 1000))
                {
                    allSame = false;
                    break;
                }
            }
            Assert.IsFalse(allSame, "不同种子应产生不同序列");
        }

        [Test]
        public void Shuffle_Deterministic()
        {
            var rng1 = new SeededRandom(42);
            var rng2 = new SeededRandom(42);

            var list1 = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5 };
            var list2 = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5 };

            rng1.Shuffle(list1);
            rng2.Shuffle(list2);

            Assert.AreEqual(list1, list2);
        }

        [Test]
        public void WeightedSelect_RespectsWeights()
        {
            var rng = new SeededRandom(42);
            var items = new System.Collections.Generic.List<string> { "A", "B" };
            var weights = new System.Collections.Generic.List<float> { 100f, 0f };

            for (int i = 0; i < 50; i++)
                Assert.AreEqual("A", rng.WeightedSelect(items, weights));
        }

        [Test]
        public void Hash_Int_Deterministic()
        {
            Assert.AreEqual(SeededRandom.Hash(42, 7), SeededRandom.Hash(42, 7));
        }

        [Test]
        public void Hash_String_Deterministic()
        {
            Assert.AreEqual(SeededRandom.Hash(42, "layout"), SeededRandom.Hash(42, "layout"));
        }

        [Test]
        public void DeterministicHash_ConsistentAcrossCalls()
        {
            string input = "test_string";
            int h1 = SeededRandom.DeterministicHash(input);
            int h2 = SeededRandom.DeterministicHash(input);
            Assert.AreEqual(h1, h2);
        }

        [Test]
        public void DeterministicHash_DifferentStrings_DifferentHashes()
        {
            int h1 = SeededRandom.DeterministicHash("layout");
            int h2 = SeededRandom.DeterministicHash("content");
            Assert.AreNotEqual(h1, h2);
        }

        [Test]
        public void Value_ReturnsInRange()
        {
            var rng = new SeededRandom(42);
            for (int i = 0; i < 100; i++)
            {
                float v = rng.Value;
                Assert.IsTrue(v >= 0f && v < 1f, $"Value {v} 越界 [0, 1)");
            }
        }
    }
}
