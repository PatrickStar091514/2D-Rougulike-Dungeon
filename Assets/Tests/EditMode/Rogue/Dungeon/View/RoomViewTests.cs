using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    public class RoomViewTests
    {
        private GameObject _roomGo;
        private RoomView _roomView;

        [SetUp]
        public void SetUp()
        {
            // 构建模拟 Prefab 结构: Root → DoorSlot00N → { DoorTrigger, DoorTilemap }
            _roomGo = new GameObject("TestRoom");
            _roomView = _roomGo.AddComponent<RoomView>();

            var doorSlotGo = new GameObject("DoorSlot00N");
            doorSlotGo.transform.SetParent(_roomGo.transform);
            doorSlotGo.SetActive(false); // 默认不激活

            var triggerGo = new GameObject("DoorTrigger");
            triggerGo.transform.SetParent(doorSlotGo.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_roomGo);
        }

        [Test]
        public void Initialize_BindsRoomData()
        {
            var room = CreateTestRoom("room_1", new List<DoorConnection>());
            _roomView.Initialize(room);

            Assert.AreEqual(room, _roomView.Room);
            Assert.AreEqual("room_1", _roomView.RoomId);
        }

        [Test]
        public void Initialize_DefaultVisibilityIsHidden()
        {
            var room = CreateTestRoom("room_1", new List<DoorConnection>());
            _roomView.Initialize(room);

            Assert.AreEqual(RoomVisibility.Hidden, _roomView.Visibility);
        }

        [Test]
        public void SetVisibility_ChangesState()
        {
            var room = CreateTestRoom("room_1", new List<DoorConnection>());
            _roomView.Initialize(room);

            _roomView.SetVisibility(RoomVisibility.Revealed);
            Assert.AreEqual(RoomVisibility.Revealed, _roomView.Visibility);
        }

        [Test]
        public void SetVisibility_RevealedDoesNotRevert()
        {
            var room = CreateTestRoom("room_1", new List<DoorConnection>());
            _roomView.Initialize(room);

            _roomView.SetVisibility(RoomVisibility.Revealed);
            _roomView.SetVisibility(RoomVisibility.Hidden);

            // Revealed 不回退
            Assert.AreEqual(RoomVisibility.Revealed, _roomView.Visibility);
        }

        [Test]
        public void Initialize_BindsDoorViews()
        {
            var doors = new List<DoorConnection>
            {
                new(new DoorSlot(new Vector2Int(0, 0), Direction.North), "room_2",
                    new DoorSlot(new Vector2Int(0, 0), Direction.South))
            };
            var room = CreateTestRoom("room_1", doors);
            _roomView.Initialize(room);

            Assert.AreEqual(1, _roomView.ActiveDoors.Count);
            Assert.AreEqual("room_2", _roomView.ActiveDoors[0].ConnectedRoomId);
        }

        [Test]
        public void Initialize_MissingDoorSlot_LogsWarningAndSkips()
        {
            var doors = new List<DoorConnection>
            {
                new(new DoorSlot(new Vector2Int(9, 9), Direction.North), "room_x",
                    new DoorSlot(new Vector2Int(0, 0), Direction.South))
            };
            var room = CreateTestRoom("room_1", doors);
            _roomView.Initialize(room);

            // 不存在 DoorSlot99N，应跳过
            Assert.AreEqual(0, _roomView.ActiveDoors.Count);
        }

        private static RoomInstance CreateTestRoom(string id, IReadOnlyList<DoorConnection> doors)
        {
            return new RoomInstance(
                id,
                RoomType.Normal,
                RoomShape.Single,
                Vector2Int.zero,
                new List<Vector2Int> { Vector2Int.zero },
                null,
                false,
                doors
            );
        }
    }
}
