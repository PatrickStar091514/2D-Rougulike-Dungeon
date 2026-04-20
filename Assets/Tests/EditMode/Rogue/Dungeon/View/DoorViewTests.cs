using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.View;

namespace RogueDungeon.Tests.Dungeon.View
{
    public class DoorViewTests
    {
        private readonly System.Collections.Generic.List<GameObject> _tempObjects = new();
        private GameObject _ownerGo;
        private RoomView _ownerRoom;
        private GameObject _doorGo;
        private DoorView _doorView;
        private Tilemap _doorTilemap;
        private MethodInfo _onTriggerEnter2D;

        [SetUp]
        public void SetUp()
        {
            _ownerGo = new GameObject("TestRoom");
            _ownerRoom = _ownerGo.AddComponent<RoomView>();

            _doorGo = new GameObject("DoorTrigger");
            _doorGo.AddComponent<BoxCollider2D>();
            _doorView = _doorGo.AddComponent<DoorView>();

            var tilemapGo = new GameObject("DoorTilemap");
            tilemapGo.transform.SetParent(_doorGo.transform);
            _doorTilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();

            typeof(DoorView).GetField("_doorTilemap", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_doorView, _doorTilemap);

            _onTriggerEnter2D = typeof(DoorView).GetMethod(
                "OnTriggerEnter2D",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(_onTriggerEnter2D);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _tempObjects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _tempObjects.Clear();
            Object.DestroyImmediate(_doorGo);
            Object.DestroyImmediate(_ownerGo);
        }

        [Test]
        public void Initialize_BindsOwnerSlotTarget_AndStartsLocked()
        {
            var slot = new DoorSlot(new Vector2Int(1, 0), Direction.East);
            var remoteSlot = new DoorSlot(new Vector2Int(0, 0), Direction.West);
            _doorView.Initialize(_ownerRoom, slot, "room_b", remoteSlot);

            Assert.AreEqual(_ownerRoom, _doorView.OwnerRoom);
            Assert.AreEqual(slot, _doorView.Slot);
            Assert.AreEqual("room_b", _doorView.ConnectedRoomId);
            Assert.AreEqual(remoteSlot, _doorView.ExpectedRemoteDoor);
            Assert.AreEqual(DoorState.Locked, _doorView.State);
        }

        [Test]
        public void StateTransitions_FollowExpectedPath()
        {
            InitDoor();
            _doorView.Unlock();
            Assert.AreEqual(DoorState.Unlocked, _doorView.State);

            _doorView.BeginTransit();
            Assert.AreEqual(DoorState.Transit, _doorView.State);

            _doorView.EndTransit();
            Assert.AreEqual(DoorState.Unlocked, _doorView.State);

            _doorView.Lock();
            Assert.AreEqual(DoorState.Locked, _doorView.State);
        }

        [Test]
        public void InvalidTransitions_KeepState_AndLogWarning()
        {
            InitDoor();
            Assert.AreEqual(DoorState.Locked, _doorView.State);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Lock\\(\\).*失败"));
            _doorView.Lock();
            Assert.AreEqual(DoorState.Locked, _doorView.State);

            _doorView.Unlock();
            Assert.AreEqual(DoorState.Unlocked, _doorView.State);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Unlock\\(\\).*失败"));
            _doorView.Unlock();
            Assert.AreEqual(DoorState.Unlocked, _doorView.State);
        }

        [Test]
        public void UpdateVisual_ChangesTilemapColorByState()
        {
            InitDoor();
            AssertColor(new Color(1f, 0.3f, 0.3f), _doorTilemap.color); // Locked 红

            _doorView.Unlock();
            AssertColor(new Color(0.3f, 1f, 0.3f), _doorTilemap.color); // Unlocked 绿

            _doorView.BeginTransit();
            AssertColor(new Color(0.5f, 0.5f, 0.5f), _doorTilemap.color); // Transit 灰
        }

        [Test]
        public void OnTriggerEnter2D_FiresOnlyWhenUnlockedAndPlayer()
        {
            InitDoor();
            int fired = 0;
            _doorView.OnPlayerEntered += _ => fired++;

            var playerCollider = CreateCollider("Player");
            var nonPlayerCollider = CreateCollider("Untagged");

            InvokeTrigger(nonPlayerCollider);
            Assert.AreEqual(0, fired);

            InvokeTrigger(playerCollider); // Locked，不触发
            Assert.AreEqual(0, fired);

            _doorView.Unlock();
            InvokeTrigger(playerCollider); // Unlocked，触发
            Assert.AreEqual(1, fired);

            _doorView.BeginTransit();
            InvokeTrigger(playerCollider); // Transit，不触发
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ConnectedDoor_SetAndGet_Works()
        {
            InitDoor();
            var otherGo = new GameObject("OtherDoor");
            var otherDoor = otherGo.AddComponent<DoorView>();
            otherDoor.Initialize(
                _ownerRoom,
                new DoorSlot(Vector2Int.zero, Direction.South),
                "room_a",
                new DoorSlot(Vector2Int.zero, Direction.North));

            _doorView.ConnectedDoor = otherDoor;
            Assert.AreEqual(otherDoor, _doorView.ConnectedDoor);

            Object.DestroyImmediate(otherGo);
        }

        private void InitDoor()
        {
            _doorView.Initialize(
                _ownerRoom,
                new DoorSlot(Vector2Int.zero, Direction.North),
                "room_target",
                new DoorSlot(Vector2Int.zero, Direction.South));
        }

        private Collider2D CreateCollider(string tag)
        {
            var go = new GameObject($"Collider_{tag}");
            go.tag = tag;
            _tempObjects.Add(go);
            return go.AddComponent<BoxCollider2D>();
        }

        private void InvokeTrigger(Collider2D collider)
        {
            _onTriggerEnter2D.Invoke(_doorView, new object[] { collider });
        }

        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.001f);
            Assert.AreEqual(expected.g, actual.g, 0.001f);
            Assert.AreEqual(expected.b, actual.b, 0.001f);
            Assert.AreEqual(expected.a, actual.a, 0.001f);
        }
    }
}
