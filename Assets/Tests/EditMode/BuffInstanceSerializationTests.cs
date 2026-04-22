using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Data.Runtime;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// BuffInstance 序列化行为的 EditMode 单元测试。
    /// 验证 JsonUtility 序列化/反序列化的正确性。
    /// </summary>
    public class BuffInstanceSerializationTests
    {
        [Test]
        public void BuffInstance_SerializeAndDeserialize_FieldsPreserved()
        {
            var original = new BuffInstance
            {
                BuffId = "test_buff",
                StackCount = 3,
                RemainingTime = 12.5f,
                RemainingRooms = 2,
                SourceId = "enemy_001"
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<BuffInstance>(json);

            Assert.AreEqual(original.BuffId, restored.BuffId);
            Assert.AreEqual(original.StackCount, restored.StackCount);
            Assert.AreEqual(original.RemainingTime, restored.RemainingTime, 0.001f);
            Assert.AreEqual(original.RemainingRooms, restored.RemainingRooms);
            Assert.AreEqual(original.SourceId, restored.SourceId);
        }

        [Test]
        public void PendingReward_SerializeAndDeserialize_FieldsPreserved()
        {
            var original = new PendingReward
            {
                RoomId = "room_42",
                Source = 1,
                OfferedBuffIds = new System.Collections.Generic.List<string> { "buff_a", "buff_b", "buff_c" }
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<PendingReward>(json);

            Assert.AreEqual(original.RoomId, restored.RoomId);
            Assert.AreEqual(original.Source, restored.Source);
            Assert.AreEqual(3, restored.OfferedBuffIds.Count);
            Assert.AreEqual("buff_a", restored.OfferedBuffIds[0]);
            Assert.AreEqual("buff_c", restored.OfferedBuffIds[2]);
        }
    }
}
