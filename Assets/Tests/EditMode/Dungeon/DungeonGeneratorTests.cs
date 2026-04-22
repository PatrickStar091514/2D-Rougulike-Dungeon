using NUnit.Framework;
using RogueDungeon.Rogue.Dungeon.Generation;
using RogueDungeon.Rogue.Dungeon.Data;

namespace RogueDungeon.Tests.Dungeon
{
    public class DungeonGeneratorTests
    {
        /// <summary>
        /// 辅助方法：创建一个最小可用的 FloorConfigSO 用于测试
        /// </summary>
        private FloorConfigSO CreateTestConfig()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<FloorConfigSO>();

            // 使用反射设置 private 字段（测试用）
            var type = typeof(FloorConfigSO);
            type.GetField("gridWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, 5);
            type.GetField("gridHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, 4);
            type.GetField("targetRoomCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, 12);
            type.GetField("eliteCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, 1);
            type.GetField("eventCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, 1);
            type.GetField("mergeRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, 0.3f);

            // 创建简单模板
            var templates = new RoomTemplateSO[2];

            var singleTemplate = UnityEngine.ScriptableObject.CreateInstance<RoomTemplateSO>();
            SetTemplateFields(singleTemplate, "single_default", RoomShape.Single,
                new RoomType[] { RoomType.Start, RoomType.Normal, RoomType.Elite, RoomType.Event },
                new DoorSlot[]
                {
                    new DoorSlot(UnityEngine.Vector2Int.zero, Direction.North),
                    new DoorSlot(UnityEngine.Vector2Int.zero, Direction.South),
                    new DoorSlot(UnityEngine.Vector2Int.zero, Direction.East),
                    new DoorSlot(UnityEngine.Vector2Int.zero, Direction.West)
                }, 1);
            templates[0] = singleTemplate;

            var bossTemplate = UnityEngine.ScriptableObject.CreateInstance<RoomTemplateSO>();
            SetTemplateFields(bossTemplate, "boss_default", RoomShape.BigSquare,
                new RoomType[] { RoomType.Boss },
                new DoorSlot[]
                {
                    new DoorSlot(UnityEngine.Vector2Int.zero, Direction.South),
                    new DoorSlot(new UnityEngine.Vector2Int(1, 0), Direction.South),
                    new DoorSlot(UnityEngine.Vector2Int.zero, Direction.West),
                    new DoorSlot(new UnityEngine.Vector2Int(0, 1), Direction.West),
                    new DoorSlot(new UnityEngine.Vector2Int(1, 0), Direction.East),
                    new DoorSlot(new UnityEngine.Vector2Int(1, 1), Direction.East),
                    new DoorSlot(new UnityEngine.Vector2Int(0, 1), Direction.North),
                    new DoorSlot(new UnityEngine.Vector2Int(1, 1), Direction.North)
                }, 1);
            templates[1] = bossTemplate;

            type.GetField("templates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, templates);

            var shapeWeights = new ShapeWeight[]
            {
                new ShapeWeight { shape = RoomShape.DoubleH, weight = 1f },
                new ShapeWeight { shape = RoomShape.DoubleV, weight = 1f }
            };
            type.GetField("shapeWeights", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(config, shapeWeights);

            return config;
        }

        private void SetTemplateFields(RoomTemplateSO template, string id, RoomShape shape,
            RoomType[] allowedTypes, DoorSlot[] doorSlots, int weight)
        {
            var type = typeof(RoomTemplateSO);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            type.GetField("templateId", flags)?.SetValue(template, id);
            type.GetField("shape", flags)?.SetValue(template, shape);
            type.GetField("allowedTypes", flags)?.SetValue(template, allowedTypes);
            type.GetField("doorSlots", flags)?.SetValue(template, doorSlots);
            type.GetField("weight", flags)?.SetValue(template, weight);
        }

        [Test]
        public void Generate_Deterministic_SameSeed_SameResult()
        {
            var config = CreateTestConfig();

            var map1 = DungeonGenerator.Generate(42, config);
            var map2 = DungeonGenerator.Generate(42, config);

            Assert.IsNotNull(map1);
            Assert.IsNotNull(map2);
            Assert.AreEqual(map1.AllRooms.Count, map2.AllRooms.Count);
            Assert.AreEqual(map1.StartRoomId, map2.StartRoomId);
            Assert.AreEqual(map1.BossRoomId, map2.BossRoomId);

            for (int i = 0; i < map1.AllRooms.Count; i++)
            {
                Assert.AreEqual(map1.AllRooms[i].Id, map2.AllRooms[i].Id);
                Assert.AreEqual(map1.AllRooms[i].Type, map2.AllRooms[i].Type);
                Assert.AreEqual(map1.AllRooms[i].Shape, map2.AllRooms[i].Shape);
            }
        }

        [Test]
        public void Generate_AllRoomsReachable()
        {
            var config = CreateTestConfig();
            var map = DungeonGenerator.Generate(42, config);

            Assert.IsNotNull(map);

            // BFS from start room
            var visited = new System.Collections.Generic.HashSet<string>();
            var queue = new System.Collections.Generic.Queue<string>();
            visited.Add(map.StartRoomId);
            queue.Enqueue(map.StartRoomId);

            while (queue.Count > 0)
            {
                var roomId = queue.Dequeue();
                var room = map.GetRoom(roomId);
                if (room == null) continue;

                foreach (var door in room.Doors)
                {
                    if (!visited.Contains(door.ConnectedRoomId))
                    {
                        visited.Add(door.ConnectedRoomId);
                        queue.Enqueue(door.ConnectedRoomId);
                    }
                }
            }

            Assert.AreEqual(map.AllRooms.Count, visited.Count,
                $"所有房间应可达：期望 {map.AllRooms.Count}，实际可达 {visited.Count}");
        }

        [Test]
        public void Generate_BossRoom_IsBigSquare()
        {
            var config = CreateTestConfig();
            var map = DungeonGenerator.Generate(42, config);

            Assert.IsNotNull(map);
            var bossRoom = map.GetRoom(map.BossRoomId);
            Assert.IsNotNull(bossRoom);
            Assert.AreEqual(RoomType.Boss, bossRoom.Type);
            Assert.AreEqual(RoomShape.BigSquare, bossRoom.Shape);
            Assert.AreEqual(4, bossRoom.Cells.Count);
        }

        [Test]
        public void Generate_StartRoom_IsSingle()
        {
            var config = CreateTestConfig();
            var map = DungeonGenerator.Generate(42, config);

            Assert.IsNotNull(map);
            var startRoom = map.GetRoom(map.StartRoomId);
            Assert.IsNotNull(startRoom);
            Assert.AreEqual(RoomType.Start, startRoom.Type);
        }

        [Test]
        public void Generate_HasCorrectSpecialRoomCounts()
        {
            var config = CreateTestConfig();
            var map = DungeonGenerator.Generate(42, config);

            Assert.IsNotNull(map);

            int eliteCount = 0, eventCount = 0;
            foreach (var room in map.AllRooms)
            {
                if (room.Type == RoomType.Elite) eliteCount++;
                else if (room.Type == RoomType.Event) eventCount++;
            }

            Assert.AreEqual(1, eliteCount, "应有 1 个 Elite 房间");
            Assert.AreEqual(1, eventCount, "应有 1 个 Event 房间");
        }
    }
}
