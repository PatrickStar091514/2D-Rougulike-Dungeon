using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDungeon.Dungeon.Generation
{
    /// <summary>
    /// 生成树构建器：使用方向偏置的 DFS 在足迹上构建生成树并查找主路径
    /// </summary>
    internal static class SpanningTreeBuilder
    {
        /// <summary>
        /// 在已放置的足迹上构建方向偏置的 DFS 生成树
        /// </summary>
        /// <param name="nodes">所有节点（按 Id 可查找）</param>
        /// <param name="startId">起始节点 Id</param>
        /// <param name="bossAnchorId">Boss 锚点节点 Id</param>
        /// <param name="rng">确定性随机源</param>
        public static void Build(
            Dictionary<string, GraphNode> nodes,
            string startId,
            string bossAnchorId,
            SeededRandom rng)
        {
            // 构建位置 -> NodeId 查找
            var posToId = new Dictionary<Vector2Int, string>();
            foreach (var kvp in nodes)
                posToId[kvp.Value.Position] = kvp.Key;

            var bossNodeIds = BuildBossNodeIds(nodes, posToId, bossAnchorId);
            bool bossEntryUsed = false;
            var visited = new HashSet<string>();
            var stack = new Stack<string>();

            visited.Add(startId);
            stack.Push(startId);

            var bossPos = nodes[bossAnchorId].Position;

            while (stack.Count > 0)
            {
                var currentId = stack.Peek();
                var currentNode = nodes[currentId];

                // 获取未访问的相邻节点（sorted by Id for determinism — D5）
                var unvisitedNeighbors = GetAdjacentNodeIds(currentNode.Position, posToId, nodes)
                    .Where(id => !visited.Contains(id))
                    .OrderBy(id => id)
                    .ToList();

                // Boss 2x2 只允许一个主线路径入口：
                // 1) 非 Boss 节点优先走非 Boss 邻居，避免提前进入 Boss 区域；
                // 2) 进入 Boss 区域后，只在 Boss 区域内扩展，不再从 Boss 向外开分支。
                if (bossNodeIds.Contains(currentId))
                {
                    unvisitedNeighbors = unvisitedNeighbors
                        .Where(id => bossNodeIds.Contains(id))
                        .ToList();
                }
                else
                {
                    var nonBossNeighbors = unvisitedNeighbors
                        .Where(id => !bossNodeIds.Contains(id))
                        .ToList();

                    if (nonBossNeighbors.Count > 0)
                    {
                        unvisitedNeighbors = nonBossNeighbors;
                    }
                    else if (bossEntryUsed)
                    {
                        unvisitedNeighbors.Clear();
                    }
                    else
                    {
                        unvisitedNeighbors = unvisitedNeighbors
                            .Where(id => bossNodeIds.Contains(id))
                            .ToList();
                    }
                }

                if (unvisitedNeighbors.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                // 方向偏置：距离 Boss 更近的邻居权重 2.0×
                var weights = new List<float>();
                float currentDist = Vector2Int.Distance(currentNode.Position, bossPos);
                foreach (var neighborId in unvisitedNeighbors)
                {
                    float neighborDist = Vector2Int.Distance(nodes[neighborId].Position, bossPos);
                    weights.Add(neighborDist < currentDist ? 2.0f : 1.0f);
                }

                var selectedId = rng.WeightedSelect(unvisitedNeighbors, weights);
                if (!bossEntryUsed && !bossNodeIds.Contains(currentId) && bossNodeIds.Contains(selectedId))
                    bossEntryUsed = true;

                // 建立树边（双向邻居关系）
                currentNode.NeighborIds.Add(selectedId);
                nodes[selectedId].NeighborIds.Add(currentId);

                visited.Add(selectedId);
                stack.Push(selectedId);
            }
        }

        /// <summary>
        /// BFS 查找从 startId 到 bossId 的主路径
        /// </summary>
        /// <param name="nodes">节点字典</param>
        /// <param name="startId">起始节点 Id</param>
        /// <param name="bossId">Boss 节点 Id</param>
        /// <returns>从 startId 到 bossId 的路径（含两端），不可达时返回空列表</returns>
        public static List<string> FindMainPath(
            Dictionary<string, GraphNode> nodes,
            string startId,
            string bossId)
        {
            var prev = new Dictionary<string, string>();
            var visited = new HashSet<string>();
            var queue = new Queue<string>();

            visited.Add(startId);
            queue.Enqueue(startId);
            prev[startId] = null;

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                if (currentId == bossId)
                    break;

                var node = nodes[currentId];
                // 按 Id 排序邻居以保证确定性（D5）
                var sortedNeighbors = node.NeighborIds.OrderBy(id => id).ToList();
                foreach (var neighborId in sortedNeighbors)
                {
                    if (visited.Contains(neighborId)) continue;
                    if (!nodes.ContainsKey(neighborId)) continue;
                    visited.Add(neighborId);
                    prev[neighborId] = currentId;
                    queue.Enqueue(neighborId);
                }
            }

            if (!prev.ContainsKey(bossId))
                return new List<string>();

            // 回溯路径
            var path = new List<string>();
            var id = bossId;
            while (id != null)
            {
                path.Add(id);
                id = prev[id];
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// 获取一个位置四邻域的有效节点 Id
        /// </summary>
        private static List<string> GetAdjacentNodeIds(
            Vector2Int pos,
            Dictionary<Vector2Int, string> posToId,
            Dictionary<string, GraphNode> nodes)
        {
            var result = new List<string>();
            var dirs = new[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };
            foreach (var dir in dirs)
            {
                if (posToId.TryGetValue(pos + dir, out var id) && nodes.ContainsKey(id))
                    result.Add(id);
            }
            return result;
        }

        private static HashSet<string> BuildBossNodeIds(
            Dictionary<string, GraphNode> nodes,
            Dictionary<Vector2Int, string> posToId,
            string bossAnchorId)
        {
            var bossNodeIds = new HashSet<string>();
            if (!nodes.TryGetValue(bossAnchorId, out var bossAnchor))
                return bossNodeIds;

            var offsets = new[]
            {
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.right + Vector2Int.up
            };

            foreach (var offset in offsets)
            {
                if (posToId.TryGetValue(bossAnchor.Position + offset, out var nodeId))
                    bossNodeIds.Add(nodeId);
            }

            if (bossNodeIds.Count == 0)
                bossNodeIds.Add(bossAnchorId);

            return bossNodeIds;
        }
    }
}
