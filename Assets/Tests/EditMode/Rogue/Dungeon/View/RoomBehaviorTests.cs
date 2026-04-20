using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RogueDungeon.Rogue.Dungeon;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    public class RoomBehaviorTests
    {
        private readonly List<UnityEngine.Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            SetStaticDungeonManagerInstance(null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object obj in _created)
            {
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            }
            _created.Clear();
            SetStaticDungeonManagerInstance(null);
        }

        #region Factory mapping

        [Test]
        public void CreateBehavior_Start_ReturnsStartRoomBehavior()
        {
            Assert.IsInstanceOf<StartRoomBehavior>(RoomView.CreateBehavior(RoomType.Start));
        }

        [Test]
        public void CreateBehavior_NormalFamily_ReturnsNormalRoomBehavior()
        {
            Assert.IsInstanceOf<NormalRoomBehavior>(RoomView.CreateBehavior(RoomType.Normal));
            Assert.IsInstanceOf<NormalRoomBehavior>(RoomView.CreateBehavior(RoomType.Elite));
            Assert.IsInstanceOf<NormalRoomBehavior>(RoomView.CreateBehavior(RoomType.Shop));
            Assert.IsInstanceOf<NormalRoomBehavior>(RoomView.CreateBehavior(RoomType.Event));
        }

        [Test]
        public void CreateBehavior_Boss_ReturnsBossRoomBehavior()
        {
            Assert.IsInstanceOf<BossRoomBehavior>(RoomView.CreateBehavior(RoomType.Boss));
        }

        #endregion

        #region Behavior lifecycle

        [Test]
        public void NormalRoomBehavior_OnEnter_NotCleared_LocksUnlockedDoors()
        {
            var roomView = CreateRoomView("normal", RoomType.Normal, cleared: false);
            var doorA = CreateDoor(roomView, DoorState.Unlocked);
            var doorB = CreateDoor(roomView, DoorState.Unlocked);
            AddDoorToRoom(roomView, doorA, doorB);

            var behavior = new NormalRoomBehavior();
            behavior.OnEnter(roomView);

            Assert.AreEqual(DoorState.Locked, doorA.State);
            Assert.AreEqual(DoorState.Locked, doorB.State);
        }

        [Test]
        public void NormalRoomBehavior_OnEnter_Cleared_DoesNotRelockDoors()
        {
            var roomView = CreateRoomView("normal_cleared", RoomType.Normal, cleared: true);
            var door = CreateDoor(roomView, DoorState.Unlocked);
            AddDoorToRoom(roomView, door);

            var behavior = new NormalRoomBehavior();
            behavior.OnEnter(roomView);

            Assert.AreEqual(DoorState.Unlocked, door.State);
        }

        [Test]
        public void BossRoomBehavior_OnEnter_NotCleared_LocksUnlockedDoors()
        {
            var roomView = CreateRoomView("boss", RoomType.Boss, cleared: false);
            var door = CreateDoor(roomView, DoorState.Unlocked);
            AddDoorToRoom(roomView, door);

            var behavior = new BossRoomBehavior();
            behavior.OnEnter(roomView);

            Assert.AreEqual(DoorState.Locked, door.State);
        }

        [Test]
        public void BossRoomBehavior_OnEnter_Cleared_DoesNotRelockDoors()
        {
            var roomView = CreateRoomView("boss_cleared", RoomType.Boss, cleared: true);
            var door = CreateDoor(roomView, DoorState.Unlocked);
            AddDoorToRoom(roomView, door);

            var behavior = new BossRoomBehavior();
            behavior.OnEnter(roomView);

            Assert.AreEqual(DoorState.Unlocked, door.State);
        }

        [Test]
        public void NotifyEnter_WhenBehaviorThrows_PropagatesException()
        {
            var roomView = CreateRoomView("throw_case", RoomType.Normal, cleared: false);
            SetBehavior(roomView, new ThrowBehavior());

            Assert.Throws<InvalidOperationException>(() => roomView.NotifyEnter());
        }

        #endregion

        #region Orchestrator behavior

        [Test]
        public void Orchestrator_OnRoomEntered_CallsExitThenEnter()
        {
            var order = new List<string>();
            var oldView = CreateRoomView("old", RoomType.Normal, false);
            var newView = CreateRoomView("new", RoomType.Normal, false);

            var oldSpy = new SpyBehavior("old", order);
            var newSpy = new SpyBehavior("new", order);
            SetBehavior(oldView, oldSpy);
            SetBehavior(newView, newSpy);

            var viewManager = CreateViewManager(oldView, newView);
            var orchestrator = CreateOrchestrator(viewManager);
            SetPrivateField(orchestrator, "_previousRoomId", "old");

            InvokePrivate(orchestrator, "OnRoomEntered", new RoomEnteredEvent { Room = newView.Room });

            Assert.AreEqual(1, oldSpy.ExitCount);
            Assert.AreEqual(1, newSpy.EnterCount);
            CollectionAssert.AreEqual(new[] { "old-exit", "new-enter" }, order);
        }

        [Test]
        public void Orchestrator_OnRoomCleared_CallsNotifyClear()
        {
            var order = new List<string>();
            var roomView = CreateRoomView("clear_target", RoomType.Normal, false);
            var spy = new SpyBehavior("target", order);
            SetBehavior(roomView, spy);

            var viewManager = CreateViewManager(roomView);
            var orchestrator = CreateOrchestrator(viewManager);

            InvokePrivate(orchestrator, "OnRoomCleared", new RogueDungeon.Core.Events.RoomClearedEvent { RoomId = "clear_target" });

            Assert.AreEqual(1, spy.ClearCount);
        }

        [Test]
        public void Orchestrator_OnDungeonReady_EntersStartRoom_AndSetsPreviousRoom()
        {
            var order = new List<string>();
            var startView = CreateRoomView("start_room", RoomType.Start, false);
            var spy = new SpyBehavior("start", order);
            SetBehavior(startView, spy);

            var viewManager = CreateViewManager(startView);
            var orchestrator = CreateOrchestrator(viewManager);
            CreateDungeonManagerWithCurrentRoom(startView.Room);

            InvokePrivate(orchestrator, "OnDungeonReady", new RogueDungeon.Core.Events.DungeonReadyEvent());

            Assert.AreEqual(1, spy.EnterCount);
            var previous = GetPrivateField<string>(orchestrator, "_previousRoomId");
            Assert.AreEqual("start_room", previous);
        }

        #endregion

        #region Test helpers

        private RoomView CreateRoomView(string id, RoomType type, bool cleared)
        {
            var go = new GameObject($"Room_{id}");
            _created.Add(go);
            var view = go.AddComponent<RoomView>();

            var room = new RoomInstance(
                id,
                type,
                RoomShape.Single,
                Vector2Int.zero,
                new List<Vector2Int> { Vector2Int.zero },
                null,
                true,
                new List<DoorConnection>());
            room.Cleared = cleared;

            view.Initialize(room);
            return view;
        }

        private DoorView CreateDoor(RoomView owner, DoorState initialState)
        {
            var go = new GameObject($"Door_{owner.RoomId}_{initialState}");
            _created.Add(go);
            var door = go.AddComponent<DoorView>();
            door.Initialize(
                owner,
                new DoorSlot(Vector2Int.zero, Direction.North),
                "target",
                new DoorSlot(Vector2Int.zero, Direction.South));

            if (initialState == DoorState.Unlocked)
                door.Unlock();
            else if (initialState == DoorState.Transit)
            {
                door.Unlock();
                door.BeginTransit();
            }

            return door;
        }

        private static void AddDoorToRoom(RoomView roomView, params DoorView[] doors)
        {
            var field = typeof(RoomView).GetField("_activeDoors", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var list = field.GetValue(roomView) as List<DoorView>;
            Assert.NotNull(list);
            list.Clear();
            list.AddRange(doors);
        }

        private DungeonViewManager CreateViewManager(params RoomView[] roomViews)
        {
            var go = new GameObject("DungeonViewManager_Test");
            _created.Add(go);
            var manager = go.AddComponent<DungeonViewManager>();

            var field = typeof(DungeonViewManager).GetField("_roomViews", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var dict = field.GetValue(manager) as Dictionary<string, RoomView>;
            Assert.NotNull(dict);
            dict.Clear();
            foreach (var rv in roomViews)
            {
                dict[rv.RoomId] = rv;
            }

            return manager;
        }

        private RoomBehaviorOrchestrator CreateOrchestrator(DungeonViewManager manager)
        {
            var go = new GameObject("RoomBehaviorOrchestrator_Test");
            _created.Add(go);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("viewManager 未赋值"));
            var orchestrator = go.AddComponent<RoomBehaviorOrchestrator>();

            SetPrivateField(orchestrator, "_viewManager", manager);
            orchestrator.enabled = true;
            return orchestrator;
        }

        private void CreateDungeonManagerWithCurrentRoom(RoomInstance room)
        {
            var go = new GameObject("DungeonManager_Test");
            _created.Add(go);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("floorConfigs 未配置|floorConfigs 未配置或为空"));
            var manager = go.AddComponent<DungeonManager>();
            SetAutoProperty(manager, "CurrentRoom", room);
        }

        private static void SetBehavior(RoomView roomView, IRoomBehavior behavior)
        {
            SetAutoProperty(roomView, "Behavior", behavior);
        }

        private static void SetAutoProperty(object instance, string propertyName, object value)
        {
            var field = instance.GetType().GetField($"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{propertyName} backing field not found");
            field.SetValue(instance, value);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} field not found");
            field.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{fieldName} field not found");
            return (T)field.GetValue(instance);
        }

        private static void InvokePrivate(object instance, string methodName, object arg)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} method not found");
            method.Invoke(instance, new[] { arg });
        }

        private static void SetStaticDungeonManagerInstance(object value)
        {
            var field = typeof(DungeonManager).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }

        private sealed class SpyBehavior : IRoomBehavior
        {
            private readonly string _name;
            private readonly List<string> _order;

            public int EnterCount { get; private set; }
            public int ClearCount { get; private set; }
            public int ExitCount { get; private set; }

            public SpyBehavior(string name, List<string> order)
            {
                _name = name;
                _order = order;
            }

            public void OnEnter(RoomView room)
            {
                EnterCount++;
                _order.Add($"{_name}-enter");
            }

            public void OnClear(RoomView room)
            {
                ClearCount++;
                _order.Add($"{_name}-clear");
            }

            public void OnExit(RoomView room)
            {
                ExitCount++;
                _order.Add($"{_name}-exit");
            }
        }

        private sealed class ThrowBehavior : IRoomBehavior
        {
            public void OnEnter(RoomView room) => throw new InvalidOperationException("boom");
            public void OnClear(RoomView room) { }
            public void OnExit(RoomView room) { }
        }

        #endregion
    }
}
