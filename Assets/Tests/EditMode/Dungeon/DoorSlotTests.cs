using System.Collections.Generic;
using NUnit.Framework;
using RogueDungeon.Rogue.Dungeon.Data;
using UnityEngine;

namespace RogueDungeon.Tests.Dungeon
{
    public class DoorSlotTests
    {
        [Test]
        public void Equals_SameCellOffsetAndDirection_ReturnsTrue()
        {
            var a = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            var b = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Equals_DifferentCellOffset_ReturnsFalse()
        {
            var a = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            var b = new DoorSlot(new Vector2Int(1, 0), Direction.North);
            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a != b);
        }

        [Test]
        public void Equals_DifferentDirection_ReturnsFalse()
        {
            var a = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            var b = new DoorSlot(new Vector2Int(0, 0), Direction.East);
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void GetHashCode_EqualSlots_ReturnSameHash()
        {
            var a = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            var b = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentSlots_ReturnDifferentHash()
        {
            var a = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            var b = new DoorSlot(new Vector2Int(0, 0), Direction.East);
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void DictionaryKey_CanStoreAndRetrieve()
        {
            var dict = new Dictionary<DoorSlot, string>();
            var key = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            dict[key] = "test_value";

            var lookupKey = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            Assert.IsTrue(dict.ContainsKey(lookupKey));
            Assert.AreEqual("test_value", dict[lookupKey]);
        }

        [Test]
        public void DictionaryKey_DifferentSlots_NoCrossLookup()
        {
            var dict = new Dictionary<DoorSlot, string>();
            var key1 = new DoorSlot(new Vector2Int(0, 0), Direction.North);
            var key2 = new DoorSlot(new Vector2Int(1, 0), Direction.North);
            dict[key1] = "value1";
            dict[key2] = "value2";

            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual("value1", dict[key1]);
            Assert.AreEqual("value2", dict[key2]);
        }

        [Test]
        public void ToString_ReturnsReadableFormat()
        {
            var slot = new DoorSlot(new Vector2Int(1, 2), Direction.East);
            StringAssert.Contains("1", slot.ToString());
            StringAssert.Contains("2", slot.ToString());
            StringAssert.Contains("East", slot.ToString());
        }
    }
}
