using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;
using RogueDungeon.Rogue.Dungeon.Generation;

namespace RogueDungeon.Tests.Dungeon
{
    public class SpanningTreeBuilderTests
    {
        private Dictionary<string, GraphNode> CreateSimpleGrid()
        {
            // 3×3 grid of nodes
            var nodes = new Dictionary<string, GraphNode>();
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var node = new GraphNode(new Vector2Int(x, y));
                    nodes[node.Id] = node;
                }
            }
            return nodes;
        }

        [Test]
        public void Build_NoLoop_EdgeCountEqualsNodeCountMinusOne()
        {
            var nodes = CreateSimpleGrid();
            var rng = new SeededRandom(42);
            string startId = "room_0_0";
            string bossId = "room_2_2";

            SpanningTreeBuilder.Build(nodes, startId, bossId, rng);

            // 边数 = 所有 NeighborIds 之和 / 2（双向边）
            int totalEdges = nodes.Values.Sum(n => n.NeighborIds.Count) / 2;
            Assert.AreEqual(nodes.Count - 1, totalEdges, "生成树应无环：边数 == 节点数 - 1");
        }

        [Test]
        public void Build_CoversAllNodes()
        {
            var nodes = CreateSimpleGrid();
            var rng = new SeededRandom(42);
            string startId = "room_0_0";
            string bossId = "room_2_2";

            SpanningTreeBuilder.Build(nodes, startId, bossId, rng);

            // 全部节点至少有一个邻居（除非只有一个节点）
            foreach (var node in nodes.Values)
                Assert.IsTrue(node.NeighborIds.Count > 0, $"节点 {node.Id} 应在树中");
        }

        [Test]
        public void FindMainPath_StartsAndEndsCorrectly()
        {
            var nodes = CreateSimpleGrid();
            var rng = new SeededRandom(42);
            string startId = "room_0_0";
            string bossId = "room_2_2";

            SpanningTreeBuilder.Build(nodes, startId, bossId, rng);

            var mainPath = SpanningTreeBuilder.FindMainPath(nodes, startId, bossId);

            Assert.IsNotEmpty(mainPath, "主路径不应为空");
            Assert.AreEqual(startId, mainPath[0], "主路径应从 start 开始");
            Assert.AreEqual(bossId, mainPath[mainPath.Count - 1], "主路径应在 boss 结束");
        }

        [Test]
        public void FindMainPath_LengthIsReasonable()
        {
            var nodes = CreateSimpleGrid();
            var rng = new SeededRandom(42);
            string startId = "room_0_0";
            string bossId = "room_2_2";

            SpanningTreeBuilder.Build(nodes, startId, bossId, rng);

            var mainPath = SpanningTreeBuilder.FindMainPath(nodes, startId, bossId);

            // 最短路 >= Manhattan distance + 1
            Assert.GreaterOrEqual(mainPath.Count, 5, "3×3网格对角路径至少 5 个节点");
            Assert.LessOrEqual(mainPath.Count, nodes.Count, "路径不应超过总节点数");
        }

        [Test]
        public void Build_Deterministic()
        {
            var nodes1 = CreateSimpleGrid();
            var nodes2 = CreateSimpleGrid();
            var rng1 = new SeededRandom(42);
            var rng2 = new SeededRandom(42);

            SpanningTreeBuilder.Build(nodes1, "room_0_0", "room_2_2", rng1);
            SpanningTreeBuilder.Build(nodes2, "room_0_0", "room_2_2", rng2);

            foreach (var id in nodes1.Keys)
            {
                var sorted1 = nodes1[id].NeighborIds.OrderBy(x => x).ToList();
                var sorted2 = nodes2[id].NeighborIds.OrderBy(x => x).ToList();
                Assert.AreEqual(sorted1, sorted2, $"节点 {id} 的邻居应相同");
            }
        }
    }
}
