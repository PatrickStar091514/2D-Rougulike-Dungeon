using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RogueDungeon.Core.Events;
using RogueDungeon.Rogue.Dungeon;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Runtime;

namespace RogueDungeon.Tests.Dungeon
{
    public class DungeonManagerRoomClearedTests
    {
        private GameObject _managerGo;
        private DungeonManager _manager;

        [SetUp]
        public void SetUp()
        {
            EventCenter.Clear();
            _managerGo = new GameObject("DungeonManager_Test");
            _manager = _managerGo.AddComponent<DungeonManager>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("floorConfigs 未配置|floorConfigs 未配置或为空"));
            InvokePrivateNoArg(_manager, "Awake");
            InvokePrivateNoArg(_manager, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_managerGo);
            SetStaticInstance(null);
            EventCenter.Clear();
        }

        [Test]
        public void RoomClearedEvent_SetsRoomClearedTrue()
        {
            var room = new RoomInstance(
                "r1",
                RoomType.Normal,
                RoomShape.Single,
                Vector2Int.zero,
                new List<Vector2Int> { Vector2Int.zero },
                null,
                true,
                new List<DoorConnection>());

            var map = CreateMap(new List<RoomInstance> { room }, "r1", "r1");
            SetAutoProperty(_manager, "CurrentMap", map);

            Assert.IsFalse(room.Cleared);
            EventCenter.Broadcast(GameEventType.RoomCleared, new RoomClearedEvent { RoomId = "r1" });
            Assert.IsTrue(room.Cleared);
        }

        private static DungeonMap CreateMap(List<RoomInstance> rooms, string startId, string bossId)
        {
            var ctor = typeof(DungeonMap).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(string), typeof(List<RoomInstance>) },
                null);
            Assert.NotNull(ctor, "DungeonMap private ctor not found");
            return (DungeonMap)ctor.Invoke(new object[] { startId, bossId, rooms });
        }

        private static void SetAutoProperty(object instance, string propertyName, object value)
        {
            var field = instance.GetType().GetField($"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"{propertyName} backing field not found");
            field.SetValue(instance, value);
        }

        private static void SetStaticInstance(object value)
        {
            var field = typeof(DungeonManager).GetField("<Instance>k__BackingField",
                BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, value);
        }

        private static void InvokePrivateNoArg(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"{methodName} method not found");
            method.Invoke(instance, null);
        }
    }
}
