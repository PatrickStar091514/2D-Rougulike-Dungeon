using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Core;
using RogueDungeon.Core.Events;

namespace RogueDungeon.Tests
{
    /// <summary>
    /// GameManager 状态机的 EditMode 单元测试
    /// </summary>
    public class GameManagerTests
    {
        private GameObject _go;
        private GameManager _gm;

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            _go = new GameObject("TestGameManager");
            _gm = _go.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            EventCenter.Clear();
            if (_go != null) Object.DestroyImmediate(_go);
        }

        #region 初始状态

        [Test]
        public void InitialState_IsBoot()
        {
            Assert.AreEqual(GameState.Boot, _gm.CurrentState);
        }

        #endregion

        #region 合法迁移

        [Test]
        public void ChangeState_Boot_To_Hub_Succeeds()
        {
            _gm.ChangeState(GameState.Hub);
            Assert.AreEqual(GameState.Hub, _gm.CurrentState);
        }

        [Test]
        public void ChangeState_FullRunCycle()
        {
            _gm.ChangeState(GameState.Hub);
            _gm.ChangeState(GameState.RunInit);
            _gm.ChangeState(GameState.RoomPlaying);
            _gm.ChangeState(GameState.RoomClear);
            _gm.ChangeState(GameState.RewardSelect);
            _gm.ChangeState(GameState.RoomPlaying);
            _gm.ChangeState(GameState.BossPlaying);
            _gm.ChangeState(GameState.RunEnd);
            _gm.ChangeState(GameState.Hub);

            Assert.AreEqual(GameState.Hub, _gm.CurrentState);
        }

        [Test]
        public void ChangeState_RoomPlaying_To_RunEnd_DirectDeath()
        {
            _gm.ChangeState(GameState.Hub);
            _gm.ChangeState(GameState.RunInit);
            _gm.ChangeState(GameState.RoomPlaying);
            _gm.ChangeState(GameState.RunEnd);

            Assert.AreEqual(GameState.RunEnd, _gm.CurrentState);
        }

        #endregion

        #region 非法迁移

        [Test]
        public void ChangeState_IllegalTransition_StateUnchanged()
        {
            // Boot 只能迁移到 Hub
            _gm.ChangeState(GameState.RunInit);

            Assert.AreEqual(GameState.Boot, _gm.CurrentState);
        }

        [Test]
        public void ChangeState_Hub_To_RunEnd_IllegalTransition()
        {
            _gm.ChangeState(GameState.Hub);
            _gm.ChangeState(GameState.RunEnd);

            Assert.AreEqual(GameState.Hub, _gm.CurrentState);
        }

        #endregion

        #region 重复迁移

        [Test]
        public void ChangeState_SameState_Ignored()
        {
            _gm.ChangeState(GameState.Hub);

            int broadcastCount = 0;
            EventCenter.AddListener<GameStateChangedEvent>(
                GameEventType.GameStateChanged,
                (_) => broadcastCount++);

            // 重复迁移到 Hub，应被忽略
            _gm.ChangeState(GameState.Hub);

            Assert.AreEqual(GameState.Hub, _gm.CurrentState);
            Assert.AreEqual(0, broadcastCount);
        }

        #endregion

        #region 事件广播

        [Test]
        public void ChangeState_BroadcastsGameStateChangedEvent()
        {
            GameStateChangedEvent received = default;
            EventCenter.AddListener<GameStateChangedEvent>(
                GameEventType.GameStateChanged,
                (evt) => received = evt);

            _gm.ChangeState(GameState.Hub);

            Assert.AreEqual(GameState.Boot, received.FromState);
            Assert.AreEqual(GameState.Hub, received.ToState);
        }

        #endregion

        #region TimeScale

        [Test]
        public void TimeScale_Hub_Is1()
        {
            _gm.ChangeState(GameState.Hub);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void TimeScale_RoomPlaying_Is1()
        {
            _gm.ChangeState(GameState.Hub);
            _gm.ChangeState(GameState.RunInit);
            _gm.ChangeState(GameState.RoomPlaying);

            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void TimeScale_RunInit_Is0()
        {
            _gm.ChangeState(GameState.Hub);
            _gm.ChangeState(GameState.RunInit);

            Assert.AreEqual(0f, Time.timeScale);
        }

        [Test]
        public void TimeScale_RoomClear_Is0()
        {
            _gm.ChangeState(GameState.Hub);
            _gm.ChangeState(GameState.RunInit);
            _gm.ChangeState(GameState.RoomPlaying);
            _gm.ChangeState(GameState.RoomClear);

            Assert.AreEqual(0f, Time.timeScale);
        }

        #endregion
    }
}
