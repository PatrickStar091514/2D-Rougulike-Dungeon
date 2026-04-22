using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon.Reward;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.Reward
{
    public class RewardSpawnerTests
    {
        private GameObject _gameManagerGo;
        private GameObject _spawnerGo;
        private GameObject _dropGo;
        private GameObject _dropGo2;
        private GameObject _groupGo;
        private GameObject _viewManagerGo;
        private GameObject _roomViewGo;

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            SetStaticGameManagerInstance(null);
            SetStaticRewardSpawnerInstance(null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dropGo != null) Object.DestroyImmediate(_dropGo);
            if (_dropGo2 != null) Object.DestroyImmediate(_dropGo2);
            if (_groupGo != null) Object.DestroyImmediate(_groupGo);
            if (_roomViewGo != null) Object.DestroyImmediate(_roomViewGo);
            if (_viewManagerGo != null) Object.DestroyImmediate(_viewManagerGo);
            if (_spawnerGo != null) Object.DestroyImmediate(_spawnerGo);
            if (_gameManagerGo != null) Object.DestroyImmediate(_gameManagerGo);

            SetStaticGameManagerInstance(null);
            SetStaticRewardSpawnerInstance(null);
            EventCenter.Clear();
        }

        [Test]
        public void OnDropPicked_EventCompletion_TransitionsBackToRoomPlaying()
        {
            _gameManagerGo = new GameObject("GameManager_Test");
            var gameManager = _gameManagerGo.AddComponent<GameManager>();
            SetStaticGameManagerInstance(gameManager);
            InvokePrivateNoArg(gameManager, "OnEnable");

            gameManager.ChangeState(GameState.Hub);
            gameManager.ChangeState(GameState.RunInit);
            gameManager.ChangeState(GameState.RoomPlaying);
            gameManager.ChangeState(GameState.RoomClear);
            gameManager.ChangeState(GameState.RewardSelect);
            Assert.AreEqual(GameState.RewardSelect, gameManager.CurrentState);

            _spawnerGo = new GameObject("RewardSpawner_Test");
            var spawner = _spawnerGo.AddComponent<RewardSpawner>();
            SetStaticRewardSpawnerInstance(spawner);

            var completionStateType = typeof(RewardSpawner).GetNestedType(
                "CompletionState", BindingFlags.NonPublic);
            Assert.NotNull(completionStateType);
            object toRoomPlaying = System.Enum.Parse(completionStateType, "ToRoomPlaying");
            SetPrivateField(spawner, "_completionState", toRoomPlaying);
            SetPrivateField(spawner, "_rewardClaimed", false);

            _dropGo = new GameObject("BuffDrop_Test");
            var drop = _dropGo.AddComponent<BuffDrop>();
            drop.Init("buff_attack_flat", null, null, null);

            InvokePrivate(spawner, "OnDropPicked", drop);

            Assert.AreEqual(GameState.RoomPlaying, gameManager.CurrentState);
        }

        [Test]
        public void ResolveSpawnPositions_NoRewardPoints_UsesRoomBoundsCenter()
        {
            _spawnerGo = new GameObject("RewardSpawner_Test");
            var spawner = _spawnerGo.AddComponent<RewardSpawner>();

            _viewManagerGo = new GameObject("DungeonViewManager_Test");
            var viewManager = _viewManagerGo.AddComponent<DungeonViewManager>();

            var room = new RoomInstance(
                "room_1_0",
                RoomType.Normal,
                RoomShape.BigSquare,
                new Vector2Int(2, 1),
                new List<Vector2Int>(RoomShapeUtil.GetCells(RoomShape.BigSquare)),
                null,
                false,
                new List<DoorConnection>());

            _roomViewGo = new GameObject("RoomView_room_1_0");
            _roomViewGo.transform.position = new Vector3(-999f, -999f, 0f);
            var roomView = _roomViewGo.AddComponent<RoomView>();
            roomView.Initialize(room);

            var roomViewsField = typeof(DungeonViewManager).GetField("_roomViews",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(roomViewsField);
            var roomViews = roomViewsField.GetValue(viewManager) as Dictionary<string, RoomView>;
            Assert.NotNull(roomViews);
            roomViews[room.Id] = roomView;

            var positions = InvokePrivateReturn<List<Vector3>>(spawner, "ResolveSpawnPositions", room.Id, 1);
            Assert.NotNull(positions);
            Assert.AreEqual(1, positions.Count);

            var bounds = DungeonCamera.CalculateRoomBounds(room);
            Assert.AreEqual(bounds.center.x, positions[0].x, 0.01f);
            Assert.AreEqual(bounds.center.y, positions[0].y, 0.01f);
        }

        [Test]
        public void OnDropPicked_ClearsExclusiveGroupReference()
        {
            _spawnerGo = new GameObject("RewardSpawner_Test");
            var spawner = _spawnerGo.AddComponent<RewardSpawner>();

            _dropGo = new GameObject("BuffDrop_A");
            var dropA = _dropGo.AddComponent<BuffDrop>();
            dropA.Init("buff_a", null, null, null);

            _dropGo2 = new GameObject("BuffDrop_B");
            var dropB = _dropGo2.AddComponent<BuffDrop>();
            dropB.Init("buff_b", null, null, null);

            _groupGo = new GameObject("RewardGroup_Test");
            var group = _groupGo.AddComponent<ExclusivePickupGroup>();
            group.Init(new List<BuffDrop> { dropA, dropB });
            dropA.SetGroup(group);
            dropB.SetGroup(group);

            var activeDropsField = typeof(RewardSpawner).GetField("_activeDrops",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(activeDropsField);
            var activeDrops = activeDropsField.GetValue(spawner) as List<BuffDrop>;
            Assert.NotNull(activeDrops);
            activeDrops.Add(dropA);
            activeDrops.Add(dropB);

            SetPrivateField(spawner, "_activeGroup", group);
            SetPrivateField(spawner, "_rewardClaimed", false);

            InvokePrivate(spawner, "OnDropPicked", dropA);

            Assert.AreEqual(0, activeDrops.Count);
            var currentGroup = GetPrivateField<ExclusivePickupGroup>(spawner, "_activeGroup");
            Assert.NotNull(currentGroup);
            Assert.IsFalse(currentGroup.gameObject.activeSelf);
        }

        private static void InvokePrivate(object instance, string methodName, object arg)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} method not found");
            method.Invoke(instance, new[] { arg });
        }

        private static T InvokePrivateReturn<T>(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} method not found");
            return (T)method.Invoke(instance, args);
        }

        private static void InvokePrivateNoArg(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(instance, null);
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

        private static void SetStaticGameManagerInstance(object value)
        {
            var field = typeof(GameManager).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }

        private static void SetStaticRewardSpawnerInstance(object value)
        {
            var field = typeof(RewardSpawner).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }
    }
}
