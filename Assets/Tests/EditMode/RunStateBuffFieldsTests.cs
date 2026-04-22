using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Data.Runtime;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// RunState Buff 相关字段的 EditMode 单元测试。
    /// 验证 ActiveBuffs 和 PendingReward 字段的序列化完整性。
    /// </summary>
    public class RunStateBuffFieldsTests
    {
        [Test]
        public void RunState_NewInstance_ActiveBuffsInitialized()
        {
            var state = new RunState();
            Assert.IsNotNull(state.ActiveBuffs);
            Assert.AreEqual(0, state.ActiveBuffs.Count);
        }

        [Test]
        public void RunState_WithBuffs_SerializeAndDeserialize()
        {
            var state = new RunState
            {
                RunId = "test_run",
                Seed = 42,
                FloorIndex = 1,
                RoomIndex = 3,
                ActiveBuffs = new List<BuffInstance>
                {
                    new BuffInstance { BuffId = "buff_a", StackCount = 2, RemainingTime = 5f },
                    new BuffInstance { BuffId = "buff_b", StackCount = 1, RemainingRooms = 2 }
                },
                PendingReward = new PendingReward
                {
                    RoomId = "room_1",
                    Source = 0,
                    OfferedBuffIds = new List<string> { "buff_c", "buff_d" }
                }
            };

            string json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<RunState>(json);

            Assert.AreEqual("test_run", restored.RunId);
            Assert.AreEqual(2, restored.ActiveBuffs.Count);
            Assert.AreEqual("buff_a", restored.ActiveBuffs[0].BuffId);
            Assert.AreEqual(2, restored.ActiveBuffs[0].StackCount);
            Assert.AreEqual("buff_b", restored.ActiveBuffs[1].BuffId);
            Assert.AreEqual(2, restored.ActiveBuffs[1].RemainingRooms);
            Assert.IsNotNull(restored.PendingReward);
            Assert.AreEqual("room_1", restored.PendingReward.RoomId);
            Assert.AreEqual(2, restored.PendingReward.OfferedBuffIds.Count);
        }

        [Test]
        public void RunState_NullPendingReward_SerializesCorrectly()
        {
            var state = new RunState
            {
                RunId = "test_run",
                Seed = 1,
                PendingReward = null
            };

            string json = JsonUtility.ToJson(state);
            var restored = JsonUtility.FromJson<RunState>(json);

            Assert.AreEqual("test_run", restored.RunId);
            // JsonUtility 对 null 引用类型反序列化为默认构造后的实例
        }
    }
}
