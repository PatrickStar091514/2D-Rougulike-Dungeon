using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RogueDungeon.Core.Events;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// EventCenter 事件中心的 EditMode 单元测试
    /// </summary>
    public class EventCenterTests
    {
        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventCenter.Clear();
        }

        #region 无参委托

        [Test]
        public void AddListener_NoArg_BroadcastInvokesHandler()
        {
            bool called = false;
            EventCenter.AddListener(GameEventType.RoomCleared, () => called = true);

            EventCenter.Broadcast(GameEventType.RoomCleared);

            Assert.IsTrue(called);
        }

        [Test]
        public void RemoveListener_NoArg_BroadcastDoesNotInvokeHandler()
        {
            bool called = false;
            CallBack handler = () => called = true;
            EventCenter.AddListener(GameEventType.RoomCleared, handler);
            EventCenter.RemoveListener(GameEventType.RoomCleared, handler);

            EventCenter.Broadcast(GameEventType.RoomCleared);

            Assert.IsFalse(called);
        }

        [Test]
        public void Broadcast_NoArg_MultipleSubscribers_AllInvoked()
        {
            int count = 0;
            EventCenter.AddListener(GameEventType.RoomCleared, () => count++);
            EventCenter.AddListener(GameEventType.RoomCleared, () => count++);

            EventCenter.Broadcast(GameEventType.RoomCleared);

            Assert.AreEqual(2, count);
        }

        #endregion

        #region 单参数泛型委托

        [Test]
        public void AddListener_OneArg_BroadcastPassesArgument()
        {
            string received = null;
            EventCenter.AddListener<string>(GameEventType.RunEnded, (s) => received = s);

            EventCenter.Broadcast(GameEventType.RunEnded, "test_run");

            Assert.AreEqual("test_run", received);
        }

        [Test]
        public void RemoveListener_OneArg_BroadcastDoesNotInvokeHandler()
        {
            string received = null;
            CallBack<string> handler = (s) => received = s;
            EventCenter.AddListener(GameEventType.RunEnded, handler);
            EventCenter.RemoveListener(GameEventType.RunEnded, handler);

            EventCenter.Broadcast(GameEventType.RunEnded, "test_run");

            Assert.IsNull(received);
        }

        [Test]
        public void Broadcast_OneArg_StructPayload()
        {
            GameStateChangedEvent received = default;
            EventCenter.AddListener<GameStateChangedEvent>(
                GameEventType.GameStateChanged,
                (evt) => received = evt);

            var sent = new GameStateChangedEvent
            {
                FromState = Core.GameState.Boot,
                ToState = Core.GameState.Hub,
                RunId = "r1"
            };
            EventCenter.Broadcast(GameEventType.GameStateChanged, sent);

            Assert.AreEqual(Core.GameState.Boot, received.FromState);
            Assert.AreEqual(Core.GameState.Hub, received.ToState);
            Assert.AreEqual("r1", received.RunId);
        }

        #endregion

        #region 双参数泛型委托

        [Test]
        public void AddListener_TwoArg_BroadcastPassesBothArguments()
        {
            int a = 0;
            string b = null;
            EventCenter.AddListener<int, string>(GameEventType.RoomCleared, (x, y) =>
            {
                a = x;
                b = y;
            });

            EventCenter.Broadcast(GameEventType.RoomCleared, 42, "hello");

            Assert.AreEqual(42, a);
            Assert.AreEqual("hello", b);
        }

        #endregion

        #region 类型安全校验

        [Test]
        public void AddListener_TypeConflict_ThrowsInvalidOperationException()
        {
            EventCenter.AddListener(GameEventType.RoomCleared, () => { });

            Assert.Throws<InvalidOperationException>(() =>
            {
                EventCenter.AddListener<int>(GameEventType.RoomCleared, (_) => { });
            });
        }

        [Test]
        public void AddListener_SameType_DoesNotThrow()
        {
            EventCenter.AddListener<int>(GameEventType.PlayerDamaged, (_) => { });

            Assert.DoesNotThrow(() =>
            {
                EventCenter.AddListener<int>(GameEventType.PlayerDamaged, (_) => { });
            });
        }

        #endregion

        #region Clear

        [Test]
        public void Clear_RemovesAllListeners()
        {
            bool called = false;
            EventCenter.AddListener(GameEventType.RoomCleared, () => called = true);

            EventCenter.Clear();
            EventCenter.Broadcast(GameEventType.RoomCleared);

            Assert.IsFalse(called);
        }

        #endregion

        #region 异常隔离

        [Test]
        public void Broadcast_HandlerThrows_OtherHandlersStillCalled()
        {
            bool secondCalled = false;
            EventCenter.AddListener(GameEventType.RoomCleared, () => throw new Exception("boom"));
            EventCenter.AddListener(GameEventType.RoomCleared, () => secondCalled = true);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("处理器异常.*boom"));

            Assert.DoesNotThrow(() => EventCenter.Broadcast(GameEventType.RoomCleared));
            Assert.IsTrue(secondCalled);
        }

        #endregion

        #region 空广播

        [Test]
        public void Broadcast_NoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventCenter.Broadcast(GameEventType.RoomCleared));
            Assert.DoesNotThrow(() => EventCenter.Broadcast(GameEventType.RunEnded, "x"));
            Assert.DoesNotThrow(() => EventCenter.Broadcast(GameEventType.PlayerDamaged, 1, "y"));
        }

        #endregion
    }
}
