using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    public class TransitTests
    {
        private readonly List<GameObject> _tempObjects = new();
        private GameObject _viewManagerGo;
        private DungeonViewManager _viewManager;
        private GameObject _coordinatorGo;
        private DoorTransitCoordinator _coordinator;

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            SetStaticDungeonManagerInstance(null);
            SetStaticInputEnabled(true);

            _viewManagerGo = new GameObject("ViewManager");
            _viewManager = _viewManagerGo.AddComponent<DungeonViewManager>();
            _tempObjects.Add(_viewManagerGo);

            _coordinatorGo = new GameObject("Coordinator");
            _coordinator = _coordinatorGo.AddComponent<DoorTransitCoordinator>();
            _tempObjects.Add(_coordinatorGo);

            typeof(DoorTransitCoordinator).GetField("_viewManager", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, _viewManager);
            _coordinator.enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _tempObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _tempObjects.Clear();

            SetStaticDungeonManagerInstance(null);
            SetStaticInputEnabled(true);
            EventCenter.Clear();
        }

        [Test]
        public void CalculateEntryOffset_North_Returns_0_Neg1_5()
        {
            var offset = DoorTransitCoordinator.CalculateEntryOffset(Direction.North, 1.5f);
            Assert.AreEqual(new Vector2(0f, -1.5f), offset);
        }

        [Test]
        public void CalculateEntryOffset_South_Returns_0_Pos1_5()
        {
            var offset = DoorTransitCoordinator.CalculateEntryOffset(Direction.South, 1.5f);
            Assert.AreEqual(new Vector2(0f, 1.5f), offset);
        }

        [Test]
        public void CalculateEntryOffset_East_Returns_Neg1_5_0()
        {
            var offset = DoorTransitCoordinator.CalculateEntryOffset(Direction.East, 1.5f);
            Assert.AreEqual(new Vector2(-1.5f, 0f), offset);
        }

        [Test]
        public void CalculateEntryOffset_West_Returns_Pos1_5_0()
        {
            var offset = DoorTransitCoordinator.CalculateEntryOffset(Direction.West, 1.5f);
            Assert.AreEqual(new Vector2(1.5f, 0f), offset);
        }

        [Test]
        public void CalculateEntryOffset_BelowMin_ClampsToPointOne()
        {
            var offset = DoorTransitCoordinator.CalculateEntryOffset(Direction.East, 0.02f);
            Assert.AreEqual(new Vector2(-0.1f, 0f), offset);
        }

        [Test]
        public void CalculateSpawnPosition_TargetWest_AnchorsAtInteriorEdgeThenOffsets()
        {
            var bounds = new Bounds(new Vector3(10f, 5f, 0f), new Vector3(1f, 2f, 0f));
            var spawnPos = DoorTransitCoordinator.CalculateSpawnPosition(bounds, Direction.West, 0.2f);
            Assert.AreEqual(new Vector2(10.7f, 5f), spawnPos);
        }

        [Test]
        public void RequestTransit_NullSourceDoor_LogsError()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("sourceDoor 为 null"));
            _coordinator.RequestTransit(null);
        }

        [Test]
        public void RequestTransit_LockedDoor_IsIgnored()
        {
            var room = CreateRoomView("room_a");
            var sourceDoor = CreateDoor(
                room,
                "room_b",
                new DoorSlot(Vector2Int.zero, Direction.North),
                new DoorSlot(Vector2Int.zero, Direction.South));

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("门状态为 Locked"));
            _coordinator.RequestTransit(sourceDoor);
        }

        [Test]
        public void RequestTransit_MissingConnectedDoor_LogsError()
        {
            var room = CreateRoomView("room_a");
            var sourceDoor = CreateDoor(
                room,
                "room_b",
                new DoorSlot(Vector2Int.zero, Direction.North),
                new DoorSlot(Vector2Int.zero, Direction.South));
            sourceDoor.Unlock();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ConnectedDoor 为 null"));
            _coordinator.RequestTransit(sourceDoor);
        }

        [Test]
        public void RequestTransit_WithoutDungeonManager_LogsError()
        {
            var roomA = CreateRoomView("room_a");
            var roomB = CreateRoomView("room_b");
            var sourceDoor = CreateDoor(
                roomA,
                "room_b",
                new DoorSlot(Vector2Int.zero, Direction.East),
                new DoorSlot(Vector2Int.zero, Direction.West));
            var targetDoor = CreateDoor(
                roomB,
                "room_a",
                new DoorSlot(Vector2Int.zero, Direction.West),
                new DoorSlot(Vector2Int.zero, Direction.East));
            sourceDoor.ConnectedDoor = targetDoor;
            targetDoor.ConnectedDoor = sourceDoor;
            sourceDoor.Unlock();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("DungeonManager.Instance 为 null"));
            _coordinator.RequestTransit(sourceDoor);
        }

        [Test]
        public void BuildDoorConnections_MultiDoor_PairsByExpectedRemoteSlot()
        {
            var roomA = CreateRoomView("room_a");
            var roomB = CreateRoomView("room_b");

            var sourceWestToEast = CreateDoor(
                roomA,
                "room_b",
                new DoorSlot(Vector2Int.zero, Direction.West),
                new DoorSlot(Vector2Int.zero, Direction.East));
            var sourceSouthToNorth = CreateDoor(
                roomA,
                "room_b",
                new DoorSlot(Vector2Int.zero, Direction.South),
                new DoorSlot(Vector2Int.zero, Direction.North));
            var targetEast = CreateDoor(
                roomB,
                "room_a",
                new DoorSlot(Vector2Int.zero, Direction.East),
                new DoorSlot(Vector2Int.zero, Direction.West));
            var targetNorth = CreateDoor(
                roomB,
                "room_a",
                new DoorSlot(Vector2Int.zero, Direction.North),
                new DoorSlot(Vector2Int.zero, Direction.South));

            AddDoorsToRoom(roomA, sourceWestToEast, sourceSouthToNorth);
            AddDoorsToRoom(roomB, targetEast, targetNorth);
            SetViewManagerRooms(roomA, roomB);

            InvokeBuildDoorConnections();

            Assert.AreEqual(targetEast, sourceWestToEast.ConnectedDoor);
            Assert.AreEqual(targetNorth, sourceSouthToNorth.ConnectedDoor);
            Assert.AreEqual(sourceWestToEast, targetEast.ConnectedDoor);
            Assert.AreEqual(sourceSouthToNorth, targetNorth.ConnectedDoor);
        }

        [Test]
        public void TeleportPlayer_TargetDoorEast_SpawnsToWestWithConfiguredOffset()
        {
            var player = new GameObject("Player");
            _tempObjects.Add(player);
            typeof(DoorTransitCoordinator).GetField("_playerTransform", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, player.transform);
            typeof(DoorTransitCoordinator).GetField("_entryOffsetDistance", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, 2f);

            var roomB = CreateRoomView("room_b");
            var targetDoor = CreateDoor(
                roomB,
                "room_a",
                new DoorSlot(Vector2Int.zero, Direction.East),
                new DoorSlot(Vector2Int.zero, Direction.West));
            targetDoor.transform.position = new Vector3(10f, 5f, 0f);

            _coordinator.TeleportPlayer(targetDoor);

            Assert.AreEqual(new Vector3(8f, 5f, 0f), player.transform.position);
        }

        [Test]
        public void TeleportPlayer_SourceEastToTargetWest_SpawnsInsideTargetRoomWithConfiguredOffset()
        {
            var player = new GameObject("Player");
            _tempObjects.Add(player);
            typeof(DoorTransitCoordinator).GetField("_playerTransform", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, player.transform);
            typeof(DoorTransitCoordinator).GetField("_entryOffsetDistance", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, 0.2f);

            var roomA = CreateRoomView("room_a");
            var roomB = CreateRoomView("room_b");
            var sourceDoor = CreateDoor(
                roomA,
                "room_b",
                new DoorSlot(Vector2Int.zero, Direction.East),
                new DoorSlot(Vector2Int.zero, Direction.West));
            var targetDoor = CreateDoor(
                roomB,
                "room_a",
                new DoorSlot(Vector2Int.zero, Direction.West),
                new DoorSlot(Vector2Int.zero, Direction.East));
            sourceDoor.ConnectedDoor = targetDoor;
            targetDoor.ConnectedDoor = sourceDoor;
            var triggerCollider = targetDoor.gameObject.AddComponent<BoxCollider2D>();
            triggerCollider.size = new Vector2(1f, 2f);
            var expectedSpawn = DoorTransitCoordinator.CalculateSpawnPosition(triggerCollider.bounds, Direction.West, 0.2f);

            _coordinator.TeleportPlayer(sourceDoor.ConnectedDoor);

            Assert.AreEqual(new Vector3(expectedSpawn.x, expectedSpawn.y, 0f), player.transform.position);
        }

        [Test]
        public void TeleportPlayer_OffsetBelowMin_UsesPointOneUnit()
        {
            var player = new GameObject("Player");
            _tempObjects.Add(player);
            typeof(DoorTransitCoordinator).GetField("_playerTransform", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, player.transform);
            typeof(DoorTransitCoordinator).GetField("_entryOffsetDistance", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, 0.04f);

            var roomB = CreateRoomView("room_b");
            var targetDoor = CreateDoor(
                roomB,
                "room_a",
                new DoorSlot(Vector2Int.zero, Direction.East),
                new DoorSlot(Vector2Int.zero, Direction.West));
            targetDoor.transform.position = new Vector3(10f, 5f, 0f);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("entryOffsetDistance"));
            _coordinator.TeleportPlayer(targetDoor);

            Assert.AreEqual(new Vector3(9.9f, 5f, 0f), player.transform.position);
        }

        [Test]
        public void OnDisable_AlwaysResetsTransitState()
        {
            SetStaticInputEnabled(false);
            typeof(DoorTransitCoordinator).GetField("_isTransiting", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_coordinator, true);

            var onDisable = typeof(DoorTransitCoordinator).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(onDisable);
            onDisable.Invoke(_coordinator, null);

            var isTransiting = (bool)(typeof(DoorTransitCoordinator).GetField("_isTransiting", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(_coordinator) ?? false);
            Assert.IsFalse(isTransiting);
            Assert.IsTrue(GetStaticInputEnabled());
        }

        private RoomView CreateRoomView(string id)
        {
            var go = new GameObject($"Room_{id}");
            _tempObjects.Add(go);
            var roomView = go.AddComponent<RoomView>();
            var room = new RoomInstance(
                id,
                RoomType.Normal,
                RoomShape.Single,
                Vector2Int.zero,
                new List<Vector2Int> { Vector2Int.zero },
                null,
                true,
                new List<DoorConnection>());
            roomView.Initialize(room);
            return roomView;
        }

        private DoorView CreateDoor(RoomView owner, string connectedRoomId, DoorSlot localSlot, DoorSlot remoteSlot)
        {
            var doorGo = new GameObject($"Door_{owner.RoomId}_{connectedRoomId}_{localSlot.Direction}");
            _tempObjects.Add(doorGo);
            var door = doorGo.AddComponent<DoorView>();
            door.Initialize(owner, localSlot, connectedRoomId, remoteSlot);
            return door;
        }

        private static void AddDoorsToRoom(RoomView roomView, params DoorView[] doors)
        {
            var field = typeof(RoomView).GetField("_activeDoors", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var list = field.GetValue(roomView) as List<DoorView>;
            Assert.NotNull(list);
            list.Clear();
            list.AddRange(doors);
        }

        private void SetViewManagerRooms(params RoomView[] roomViews)
        {
            var field = typeof(DungeonViewManager).GetField("_roomViews", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            var dict = field.GetValue(_viewManager) as Dictionary<string, RoomView>;
            Assert.NotNull(dict);
            dict.Clear();
            foreach (var roomView in roomViews)
            {
                dict[roomView.RoomId] = roomView;
            }
        }

        private void InvokeBuildDoorConnections()
        {
            var method = typeof(DoorTransitCoordinator).GetMethod("BuildDoorConnections", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(_coordinator, null);
        }

        private static void SetStaticDungeonManagerInstance(object value)
        {
            var field = typeof(DungeonManager).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }

        private static bool GetStaticInputEnabled()
        {
            var field = typeof(DoorTransitCoordinator).GetField("<InputEnabled>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            return (bool)(field?.GetValue(null) ?? false);
        }

        private static void SetStaticInputEnabled(bool value)
        {
            var field = typeof(DoorTransitCoordinator).GetField("<InputEnabled>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }
    }
}
