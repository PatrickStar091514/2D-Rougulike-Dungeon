using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Generation;

namespace RogueDungeon.Tests.Dungeon
{
    public class FootprintBuilderTests
    {
        [Test]
        public void Footprint_ContainsStartAndBossCells()
        {
            var rng = new SeededRandom(42);
            var startPos = new Vector2Int(4, 0);
            var bossCells = new List<Vector2Int>
            {
                new Vector2Int(0, 2),
                new Vector2Int(1, 2),
                new Vector2Int(0, 3),
                new Vector2Int(1, 3)
            };

            var footprint = FootprintBuilder.Build(5, 4, 12, startPos, bossCells, rng);

            Assert.IsTrue(footprint.Contains(startPos), "足迹应包含起始点");
            foreach (var cell in bossCells)
                Assert.IsTrue(footprint.Contains(cell), $"足迹应包含 Boss cell {cell}");
        }

        [Test]
        public void Footprint_IsConnected()
        {
            var rng = new SeededRandom(42);
            var startPos = new Vector2Int(4, 0);
            var bossCells = new List<Vector2Int>
            {
                new Vector2Int(0, 2),
                new Vector2Int(1, 2),
                new Vector2Int(0, 3),
                new Vector2Int(1, 3)
            };

            var footprint = FootprintBuilder.Build(5, 4, 12, startPos, bossCells, rng);

            // FloodFill 验证连通性
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var first = footprint.First();
            queue.Enqueue(first);
            visited.Add(first);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var neighbor = cell + dir;
                    if (footprint.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            Assert.AreEqual(footprint.Count, visited.Count, "足迹应全连通");
        }

        [Test]
        public void Footprint_ReachesTargetCount()
        {
            var rng = new SeededRandom(42);
            var startPos = new Vector2Int(4, 0);
            var bossCells = new List<Vector2Int>
            {
                new Vector2Int(0, 2),
                new Vector2Int(1, 2),
                new Vector2Int(0, 3),
                new Vector2Int(1, 3)
            };

            var footprint = FootprintBuilder.Build(5, 4, 12, startPos, bossCells, rng);

            Assert.GreaterOrEqual(footprint.Count, 5, "足迹至少应包含 start+4 boss cells");
        }

        [Test]
        public void Footprint_AllWithinGrid()
        {
            var rng = new SeededRandom(42);
            var startPos = new Vector2Int(4, 0);
            var bossCells = new List<Vector2Int>
            {
                new Vector2Int(0, 2),
                new Vector2Int(1, 2),
                new Vector2Int(0, 3),
                new Vector2Int(1, 3)
            };

            var footprint = FootprintBuilder.Build(5, 4, 12, startPos, bossCells, rng);

            foreach (var cell in footprint)
            {
                Assert.IsTrue(cell.x >= 0 && cell.x < 5 && cell.y >= 0 && cell.y < 4,
                    $"Cell {cell} 越界网格 5×4");
            }
        }
    }
}
