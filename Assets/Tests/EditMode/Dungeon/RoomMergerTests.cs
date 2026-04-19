using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Generation;

namespace RogueDungeon.Tests.Dungeon
{
    public class RoomMergerTests
    {
        [Test]
        public void MergeBoss_CreatesBigSquare()
        {
            var nodes = new Dictionary<string, GraphNode>();
            var positions = new Vector2Int[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1),
                new Vector2Int(2, 0) // extra neighbor
            };

            foreach (var pos in positions)
            {
                var node = new GraphNode(pos);
                nodes[node.Id] = node;
            }

            // Setup tree edges manually
            nodes["room_0_0"].NeighborIds.Add("room_1_0");
            nodes["room_1_0"].NeighborIds.Add("room_0_0");
            nodes["room_0_0"].NeighborIds.Add("room_0_1");
            nodes["room_0_1"].NeighborIds.Add("room_0_0");
            nodes["room_0_1"].NeighborIds.Add("room_1_1");
            nodes["room_1_1"].NeighborIds.Add("room_0_1");
            nodes["room_1_0"].NeighborIds.Add("room_2_0");
            nodes["room_2_0"].NeighborIds.Add("room_1_0");

            var bossCells = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)
            };

            RoomMerger.MergeBoss(nodes, "room_0_0", bossCells);

            Assert.AreEqual(RoomShape.BigSquare, nodes["room_0_0"].RoomShape);
            Assert.AreEqual(4, nodes["room_0_0"].Cells.Count);
            Assert.AreEqual(RoomType.Boss, nodes["room_0_0"].RoomType);
        }

        [Test]
        public void MergeBoss_AbsorbedNodesMarkedMerged()
        {
            var nodes = new Dictionary<string, GraphNode>();
            var positions = new Vector2Int[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)
            };

            foreach (var pos in positions)
            {
                var node = new GraphNode(pos);
                nodes[node.Id] = node;
            }

            nodes["room_0_0"].NeighborIds.Add("room_1_0");
            nodes["room_1_0"].NeighborIds.Add("room_0_0");
            nodes["room_0_0"].NeighborIds.Add("room_0_1");
            nodes["room_0_1"].NeighborIds.Add("room_0_0");
            nodes["room_0_1"].NeighborIds.Add("room_1_1");
            nodes["room_1_1"].NeighborIds.Add("room_0_1");

            var bossCells = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)
            };

            RoomMerger.MergeBoss(nodes, "room_0_0", bossCells);

            Assert.IsTrue(nodes["room_1_0"].IsMerged);
            Assert.IsTrue(nodes["room_0_1"].IsMerged);
            Assert.IsTrue(nodes["room_1_1"].IsMerged);
            Assert.IsFalse(nodes["room_0_0"].IsMerged);
        }

        [Test]
        public void MergeBoss_ExternalNeighborsPreserved()
        {
            var nodes = new Dictionary<string, GraphNode>();
            var positions = new Vector2Int[]
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1),
                new Vector2Int(2, 0)
            };

            foreach (var pos in positions)
            {
                var node = new GraphNode(pos);
                nodes[node.Id] = node;
            }

            nodes["room_0_0"].NeighborIds.Add("room_1_0");
            nodes["room_1_0"].NeighborIds.Add("room_0_0");
            nodes["room_0_0"].NeighborIds.Add("room_0_1");
            nodes["room_0_1"].NeighborIds.Add("room_0_0");
            nodes["room_0_1"].NeighborIds.Add("room_1_1");
            nodes["room_1_1"].NeighborIds.Add("room_0_1");
            nodes["room_1_0"].NeighborIds.Add("room_2_0");
            nodes["room_2_0"].NeighborIds.Add("room_1_0");

            var bossCells = new List<Vector2Int>
            {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)
            };

            RoomMerger.MergeBoss(nodes, "room_0_0", bossCells);

            // room_2_0 should now be neighbor of boss anchor room_0_0
            Assert.IsTrue(nodes["room_0_0"].NeighborIds.Contains("room_2_0"),
                "Boss 锚点应保留外部邻居 room_2_0");
            Assert.IsTrue(nodes["room_2_0"].NeighborIds.Contains("room_0_0"),
                "外部邻居应指向 Boss 锚点");
        }

        [Test]
        public void MergeNormal_OnlyMergesNormalType()
        {
            var nodes = new Dictionary<string, GraphNode>();
            for (int x = 0; x < 3; x++)
            {
                var node = new GraphNode(new Vector2Int(x, 0));
                nodes[node.Id] = node;
            }

            nodes["room_0_0"].RoomType = RoomType.Start;
            nodes["room_1_0"].RoomType = RoomType.Normal;
            nodes["room_2_0"].RoomType = RoomType.Normal;

            nodes["room_0_0"].NeighborIds.Add("room_1_0");
            nodes["room_1_0"].NeighborIds.Add("room_0_0");
            nodes["room_1_0"].NeighborIds.Add("room_2_0");
            nodes["room_2_0"].NeighborIds.Add("room_1_0");

            var shapeWeights = new ShapeWeight[]
            {
                new ShapeWeight { shape = RoomShape.DoubleH, weight = 100f }
            };

            var rng = new SeededRandom(42);
            RoomMerger.MergeNormal(nodes, 1.0f, shapeWeights, rng);

            // Start should never be merged
            Assert.AreEqual(RoomType.Start, nodes["room_0_0"].RoomType);
            Assert.IsFalse(nodes["room_0_0"].IsMerged);
        }
    }
}
