using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;

namespace RogueDungeon.Rogue.Dungeon.Generation
{
    /// <summary>
    /// 房间合并器：将相邻的小房间合并为更大形状的房间
    /// </summary>
    internal static class RoomMerger
    {
        /// <summary>
        /// 合并 Boss 房间：将 Boss 锚点周围 2×2 的 4 个 cell 合并为 BigSquare
        /// </summary>
        /// <param name="nodes">节点字典</param>
        /// <param name="bossAnchorId">Boss 锚点节点 Id</param>
        /// <param name="bossCells">Boss 占据的 4 个 cell</param>
        public static void MergeBoss(
            Dictionary<string, GraphNode> nodes,
            string bossAnchorId,
            List<Vector2Int> bossCells)
        {
            var posToId = BuildPosLookup(nodes);
            var bossNode = nodes[bossAnchorId];

            // 找到要吸收的其他节点
            var absorbIds = new List<string>();
            foreach (var cell in bossCells)
            {
                if (posToId.TryGetValue(cell, out var id) && id != bossAnchorId)
                    absorbIds.Add(id);
            }

            // 应用合并
            ApplyMerge(nodes, bossNode, absorbIds, RoomShape.BigSquare, posToId);
            bossNode.RoomType = RoomType.Boss;
        }

        /// <summary>
        /// 按 mergeRate 概率对 Normal 房间尝试合并
        /// </summary>
        /// <param name="nodes">节点字典</param>
        /// <param name="mergeRate">合并概率 (0~1)</param>
        /// <param name="shapeWeights">各形状权重</param>
        /// <param name="rng">确定性随机源</param>
        public static void MergeNormal(
            Dictionary<string, GraphNode> nodes,
            float mergeRate,
            ShapeWeight[] shapeWeights,
            SeededRandom rng)
        {
            var posToId = BuildPosLookup(nodes);

            // 获取 Normal 且未合并的候选，排序后打乱（D5 + 随机性）
            var candidates = nodes.Keys
                .Where(id => !nodes[id].IsMerged && nodes[id].RoomType == RoomType.Normal)
                .OrderBy(id => id).ToList();
            rng.Shuffle(candidates);

            foreach (var id in candidates)
            {
                var node = nodes[id];
                if (node.IsMerged || node.RoomType != RoomType.Normal)
                    continue;
                if (rng.Value > mergeRate)
                    continue;

                TryMerge(nodes, node, shapeWeights, rng, posToId);
            }
        }

        private static void TryMerge(
            Dictionary<string, GraphNode> nodes,
            GraphNode anchor,
            ShapeWeight[] shapeWeights,
            SeededRandom rng,
            Dictionary<Vector2Int, string> posToId)
        {
            // 收集可用合并形状
            var validShapes = new List<RoomShape>();
            var validWeights = new List<float>();
            var validAbsorbLists = new List<List<string>>();

            if (shapeWeights == null) return;

            foreach (var sw in shapeWeights)
            {
                if (sw.shape == RoomShape.Single || sw.shape == RoomShape.BigSquare)
                    continue; // Single 不需要合并，BigSquare 仅限 Boss
                if (sw.weight <= 0f) continue;

                var extraCells = GetExtraCells(sw.shape);
                if (extraCells == null) continue;

                var absorbIds = CheckMergeValidity(anchor, extraCells, nodes, posToId);
                if (absorbIds != null)
                {
                    validShapes.Add(sw.shape);
                    validWeights.Add(sw.weight);
                    validAbsorbLists.Add(absorbIds);
                }
            }

            if (validShapes.Count == 0) return;

            // 加权随机选择形状
            var selectedShape = rng.WeightedSelect(validShapes, validWeights);
            int idx = validShapes.IndexOf(selectedShape);

            ApplyMerge(nodes, anchor, validAbsorbLists[idx], selectedShape, posToId);
        }

        /// <summary>
        /// 获取指定形状相对于锚点(0,0)的额外 cell 偏移量
        /// </summary>
        private static Vector2Int[] GetExtraCells(RoomShape shape)
        {
            var allCells = RoomShapeUtil.GetCells(shape);
            if (allCells == null || allCells.Length <= 1) return null;

            // 过滤掉 (0,0) 锚点
            return allCells.Where(c => c != Vector2Int.zero).ToArray();
        }

        /// <summary>
        /// 检查合并是否可行：额外 cell 对应的节点必须存在、未合并、类型为 Normal
        /// </summary>
        private static List<string> CheckMergeValidity(
            GraphNode anchor,
            Vector2Int[] extraCells,
            Dictionary<string, GraphNode> nodes,
            Dictionary<Vector2Int, string> posToId)
        {
            var absorbIds = new List<string>();
            foreach (var offset in extraCells)
            {
                var targetPos = anchor.Position + offset;
                if (!posToId.TryGetValue(targetPos, out var targetId))
                    return null;
                if (targetId == anchor.Id)
                    return null;
                var targetNode = nodes[targetId];
                if (targetNode.IsMerged || targetNode.RoomType != RoomType.Normal)
                    return null;
                absorbIds.Add(targetId);
            }
            return absorbIds;
        }

        /// <summary>
        /// 执行合并：将被吸收节点的 cell 合并到锚点节点，重定向外部边，标记已合并
        /// </summary>
        private static void ApplyMerge(
            Dictionary<string, GraphNode> nodes,
            GraphNode anchor,
            List<string> absorbIds,
            RoomShape shape,
            Dictionary<Vector2Int, string> posToId)
        {
            anchor.RoomShape = shape;

            foreach (var absorbId in absorbIds)
            {
                var absorbNode = nodes[absorbId];
                absorbNode.IsMerged = true;

                // 把被吸收的 cell 加入锚点
                foreach (var cell in absorbNode.Cells)
                {
                    if (!anchor.Cells.Contains(cell))
                        anchor.Cells.Add(cell);
                    posToId[cell] = anchor.Id;
                }

                // 重定向外部树边
                foreach (var neighborId in absorbNode.NeighborIds)
                {
                    if (neighborId == anchor.Id) continue;
                    if (absorbIds.Contains(neighborId)) continue; // 内部边，忽略

                    if (!nodes.ContainsKey(neighborId)) continue;
                    var neighbor = nodes[neighborId];

                    // 邻居端：将 absorbId 替换为 anchor.Id
                    neighbor.NeighborIds.Remove(absorbId);
                    if (!neighbor.NeighborIds.Contains(anchor.Id))
                        neighbor.NeighborIds.Add(anchor.Id);

                    // 锚点端：添加这个邻居
                    if (!anchor.NeighborIds.Contains(neighborId))
                        anchor.NeighborIds.Add(neighborId);
                }

                // 移除锚点对被吸收节点的邻居引用
                anchor.NeighborIds.Remove(absorbId);
            }
        }

        private static Dictionary<Vector2Int, string> BuildPosLookup(Dictionary<string, GraphNode> nodes)
        {
            var posToId = new Dictionary<Vector2Int, string>();
            foreach (var kvp in nodes)
            {
                if (!kvp.Value.IsMerged)
                    posToId[kvp.Value.Position] = kvp.Key;
            }
            return posToId;
        }
    }
}
