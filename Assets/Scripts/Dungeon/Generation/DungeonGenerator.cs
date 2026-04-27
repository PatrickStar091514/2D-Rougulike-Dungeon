using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.Config;
using RogueDungeon.Dungeon.Map;

namespace RogueDungeon.Dungeon.Generation
{
    /// <summary>
    /// 地牢生成器：编排 10 步流水线，输入种子和楼层配置，输出 DungeonMap
    /// </summary>
    public static class DungeonGenerator
    {
        /// <summary>
        /// 生成地牢
        /// </summary>
        /// <param name="seed">楼层种子</param>
        /// <param name="config">楼层配置</param>
        /// <returns>生成的 DungeonMap，配置无效时返回 null</returns>
        public static DungeonMap Generate(int seed, FloorConfigSO config)
        {
            // Step 1: 验证配置
            if (!config.Validate())
            {
                Debug.LogError("[DungeonGenerator] 配置校验失败，终止生成");
                return null;
            }

            int gridW = config.GridWidth;
            int gridH = config.GridHeight;

            // Step 2: 创建子种子 RNG
            var layoutRng = new SeededRandom(SeededRandom.Hash(seed, "layout"));
            var contentRng = new SeededRandom(SeededRandom.Hash(seed, "content"));

            // Step 3: 规划锚点（4 种对角方案随机选一种）
            int scheme = layoutRng.Range(0, 4);
            PlanAnchors(scheme, gridW, gridH,
                out Vector2Int startPos, out Vector2Int bossAnchor, out List<Vector2Int> bossCells);

            // Step 4: 构建足迹（多源交替 BFS）
            var footprint = FootprintBuilder.Build(
                gridW, gridH, config.TargetRoomCount,
                startPos, bossCells, layoutRng);

            // 从足迹创建节点
            var nodes = new Dictionary<string, GraphNode>();
            foreach (var cell in footprint.OrderBy(c => (c.x, c.y)))
            {
                var node = new GraphNode(cell);
                nodes[node.Id] = node;
            }

            // 标记 Start 和 Boss
            string startId = $"room_{startPos.x}_{startPos.y}";
            string bossAnchorId = $"room_{bossAnchor.x}_{bossAnchor.y}";
            if (nodes.ContainsKey(startId))
                nodes[startId].RoomType = RoomType.Start;

            // Step 5: 构建生成树（方向偏置 DFS）
            SpanningTreeBuilder.Build(nodes, startId, bossAnchorId, layoutRng);

            // Step 6: 合并 Boss 房间
            RoomMerger.MergeBoss(nodes, bossAnchorId, bossCells);

            // Step 7: 查找主路径 + 分配特殊房间
            var mainPath = SpanningTreeBuilder.FindMainPath(nodes, startId, bossAnchorId);
            foreach (var id in mainPath)
            {
                if (nodes.ContainsKey(id) && !nodes[id].IsMerged)
                    nodes[id].IsOnMainPath = true;
            }

            var protectedNodeIds = new HashSet<string>();
            if (nodes.TryGetValue(bossAnchorId, out var bossNode))
            {
                foreach (var neighborId in bossNode.NeighborIds)
                {
                    if (nodes.ContainsKey(neighborId) && !nodes[neighborId].IsMerged)
                        protectedNodeIds.Add(neighborId);
                }
            }

            if (protectedNodeIds.Count == 0 && mainPath.Count >= 2)
            {
                var bossMainPathNeighborId = mainPath[mainPath.Count - 2];
                if (!string.IsNullOrEmpty(bossMainPathNeighborId))
                    protectedNodeIds.Add(bossMainPathNeighborId);
            }

            SpecialRoomAssigner.Assign(
                nodes, mainPath,
                config.EliteCount, config.EventCount,
                contentRng, protectedNodeIds);

            // Step 8: 普通房间合并
            RoomMerger.MergeNormal(
                nodes,
                config.MergeRate,
                config.ShapeWeights,
                contentRng,
                protectedNodeIds);

            // Step 9: 模板分配
            TemplateSelector.Assign(nodes, config, contentRng);

            // Step 10: 构建最终 DungeonMap
            var nodeList = nodes.Values.ToList();
            return DungeonMap.Build(nodeList, startId, bossAnchorId, config);
        }

        /// <summary>
        /// 规划起始点和 Boss 锚点的 4 种对角方案
        /// </summary>
        private static void PlanAnchors(
            int scheme, int gridW, int gridH,
            out Vector2Int startPos,
            out Vector2Int bossAnchor,
            out List<Vector2Int> bossCells)
        {
            // 四个角落
            var corners = new Vector2Int[]
            {
                new Vector2Int(0, gridH - 1),       // 左上 [0]
                new Vector2Int(gridW - 1, gridH - 1), // 右上 [1]
                new Vector2Int(0, 0),                 // 左下 [2]
                new Vector2Int(gridW - 1, 0)          // 右下 [3]
            };

            // Boss 锚点（内缩以容纳 2×2）
            var bossAnchors = new Vector2Int[]
            {
                new Vector2Int(0, gridH - 2),        // 左上内缩 [0]
                new Vector2Int(gridW - 2, gridH - 2), // 右上内缩 [1]
                new Vector2Int(0, 0),                  // 左下 [2]
                new Vector2Int(gridW - 2, 0)           // 右下 [3]
            };

            // 对角配对：(startCornerIdx, bossCornerIdx)
            var pairs = new (int s, int b)[]
            {
                (3, 0), // scheme 0: 起始右下，Boss左上
                (2, 1), // scheme 1: 起始左下，Boss右上
                (1, 2), // scheme 2: 起始右上，Boss左下
                (0, 3)  // scheme 3: 起始左上，Boss右下
            };

            var pair = pairs[scheme];
            startPos = corners[pair.s];
            bossAnchor = bossAnchors[pair.b];

            // Boss 2×2 cells
            bossCells = new List<Vector2Int>
            {
                bossAnchor,
                bossAnchor + Vector2Int.right,
                bossAnchor + Vector2Int.up,
                bossAnchor + Vector2Int.right + Vector2Int.up
            };
        }

    }
}
