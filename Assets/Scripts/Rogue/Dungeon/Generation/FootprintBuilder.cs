using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDungeon.Rogue.Dungeon.Generation
{
    /// <summary>
    /// 足迹构建器：使用多源交替 BFS 在网格上生成连通的房间布局
    /// </summary>
    internal static class FootprintBuilder
    {
        /// <summary>
        /// 构建房间足迹
        /// </summary>
        /// <param name="gridWidth">网格宽度</param>
        /// <param name="gridHeight">网格高度</param>
        /// <param name="targetCount">目标房间数</param>
        /// <param name="startPos">起始锚点</param>
        /// <param name="bossCells">Boss 占据的 cell 列表（2×2）</param>
        /// <param name="rng">确定性随机源</param>
        /// <returns>已放置的 cell 集合</returns>
        public static HashSet<Vector2Int> Build(
            int gridWidth, int gridHeight,
            int targetCount,
            Vector2Int startPos,
            List<Vector2Int> bossCells,
            SeededRandom rng)
        {
            var placed = new HashSet<Vector2Int>();

            // Place start cell
            placed.Add(startPos);

            // Place boss cells
            foreach (var cell in bossCells)
                placed.Add(cell);

            // Regions for connectivity tracking
            var startRegion = new HashSet<Vector2Int> { startPos };
            var bossRegion = new HashSet<Vector2Int>(bossCells);

            // Frontiers
            var startFrontier = new List<Vector2Int>();
            AddNeighborsToFrontier(startPos, gridWidth, gridHeight, placed, startFrontier);

            var bossFrontier = new List<Vector2Int>();
            foreach (var cell in bossCells)
                AddNeighborsToFrontier(cell, gridWidth, gridHeight, placed, bossFrontier);

            bool connected = false;
            List<Vector2Int> mergedFrontier = null;

            while (placed.Count < targetCount)
            {
                if (connected)
                {
                    // After connection: use merged frontier
                    if (mergedFrontier.Count == 0) break;

                    var nextCell = PopRandom(mergedFrontier, rng);
                    if (placed.Contains(nextCell)) continue;
                    placed.Add(nextCell);
                    AddNeighborsToFrontier(nextCell, gridWidth, gridHeight, placed, mergedFrontier);
                }
                else
                {
                    // Alternating BFS: start expands one, boss expands one
                    if (startFrontier.Count > 0)
                    {
                        var cell = PopRandom(startFrontier, rng);
                        if (!placed.Contains(cell))
                        {
                            placed.Add(cell);
                            startRegion.Add(cell);
                            AddNeighborsToFrontier(cell, gridWidth, gridHeight, placed, startFrontier);

                            // Check connectivity
                            if (IsAdjacentToRegion(cell, bossRegion))
                            {
                                connected = true;
                                mergedFrontier = MergeFrontiers(startFrontier, bossFrontier, placed);
                                continue;
                            }
                        }
                    }

                    if (bossFrontier.Count > 0)
                    {
                        var cell = PopRandom(bossFrontier, rng);
                        if (!placed.Contains(cell))
                        {
                            placed.Add(cell);
                            bossRegion.Add(cell);
                            AddNeighborsToFrontier(cell, gridWidth, gridHeight, placed, bossFrontier);

                            // Check connectivity
                            if (IsAdjacentToRegion(cell, startRegion))
                            {
                                connected = true;
                                mergedFrontier = MergeFrontiers(startFrontier, bossFrontier, placed);
                                continue;
                            }
                        }
                    }

                    // Both frontiers empty but not connected — can't grow further
                    if (startFrontier.Count == 0 && bossFrontier.Count == 0)
                        break;
                }
            }

            return placed;
        }

        private static readonly Vector2Int[] Dirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private static void AddNeighborsToFrontier(
            Vector2Int cell, int gridW, int gridH,
            HashSet<Vector2Int> placed, List<Vector2Int> frontier)
        {
            foreach (var dir in Dirs)
            {
                var neighbor = cell + dir;
                if (neighbor.x >= 0 && neighbor.x < gridW &&
                    neighbor.y >= 0 && neighbor.y < gridH &&
                    !placed.Contains(neighbor) &&
                    !frontier.Contains(neighbor))
                {
                    frontier.Add(neighbor);
                }
            }
        }

        private static bool IsAdjacentToRegion(Vector2Int cell, HashSet<Vector2Int> region)
        {
            foreach (var dir in Dirs)
            {
                if (region.Contains(cell + dir))
                    return true;
            }
            return false;
        }

        private static Vector2Int PopRandom(List<Vector2Int> frontier, SeededRandom rng)
        {
            int index = rng.Range(0, frontier.Count);
            var cell = frontier[index];
            frontier[index] = frontier[frontier.Count - 1];
            frontier.RemoveAt(frontier.Count - 1);
            return cell;
        }

        private static List<Vector2Int> MergeFrontiers(
            List<Vector2Int> a, List<Vector2Int> b, HashSet<Vector2Int> placed)
        {
            var merged = new List<Vector2Int>();
            foreach (var cell in a)
            {
                if (!placed.Contains(cell) && !merged.Contains(cell))
                    merged.Add(cell);
            }
            foreach (var cell in b)
            {
                if (!placed.Contains(cell) && !merged.Contains(cell))
                    merged.Add(cell);
            }
            return merged;
        }
    }
}
