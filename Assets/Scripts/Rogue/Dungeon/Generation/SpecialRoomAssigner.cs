using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueDungeon.Rogue.Dungeon.Data;

namespace RogueDungeon.Rogue.Dungeon.Generation
{
    /// <summary>
    /// 特殊房间分配器：根据规则为 Elite、Shop、Event 类型在生成树上找到合适位置
    /// </summary>
    internal static class SpecialRoomAssigner
    {
        /// <summary>
        /// 为生成树中的节点分配特殊房间类型
        /// </summary>
        /// <param name="nodes">节点字典（非合并的）</param>
        /// <param name="mainPath">主路径 Id 列表</param>
        /// <param name="eliteCount">精英房间数量</param>
        /// <param name="shopCount">商店数量</param>
        /// <param name="eventCount">事件数量</param>
        /// <param name="rng">确定性随机源</param>
        public static void Assign(
            Dictionary<string, GraphNode> nodes,
            List<string> mainPath,
            int eliteCount,
            int shopCount,
            int eventCount,
            SeededRandom rng,
            ISet<string> protectedNodeIds = null)
        {
            var mainPathSet = new HashSet<string>(mainPath);

            // Elite：主路径后半段 → 前半段 → 分支中 Normal 节点
            AssignElite(nodes, mainPath, mainPathSet, eliteCount, rng, protectedNodeIds);

            // Shop：叶子节点（度==1）→ 度<=2 的分支节点
            AssignShop(nodes, mainPathSet, shopCount, rng, protectedNodeIds);

            // Event：非主路径 Normal → 任意剩余 Normal
            AssignEvent(nodes, mainPathSet, eventCount, rng, protectedNodeIds);
        }

        private static void AssignElite(
            Dictionary<string, GraphNode> nodes,
            List<string> mainPath,
            HashSet<string> mainPathSet,
            int count,
            SeededRandom rng,
            ISet<string> protectedNodeIds)
        {
            if (count <= 0) return;
            int assigned = 0;

            // 主路径后半段 Normal 节点
            int half = mainPath.Count / 2;
            var secondHalf = mainPath
                .Skip(half)
                .Where(id => nodes.ContainsKey(id)
                    && nodes[id].RoomType == RoomType.Normal
                    && (protectedNodeIds == null || !protectedNodeIds.Contains(id)))
                .OrderBy(id => id).ToList();
            rng.Shuffle(secondHalf);
            assigned += PickAndAssign(secondHalf, nodes, RoomType.Elite, count - assigned);

            if (assigned >= count) return;

            // 主路径前半段 Normal 节点
            var firstHalf = mainPath
                .Take(half)
                .Where(id => nodes.ContainsKey(id)
                    && nodes[id].RoomType == RoomType.Normal
                    && (protectedNodeIds == null || !protectedNodeIds.Contains(id)))
                .OrderBy(id => id).ToList();
            rng.Shuffle(firstHalf);
            assigned += PickAndAssign(firstHalf, nodes, RoomType.Elite, count - assigned);

            if (assigned >= count) return;

            // 分支中的 Normal 节点
            var branchNormals = nodes.Keys
                .Where(id => !mainPathSet.Contains(id)
                    && nodes[id].RoomType == RoomType.Normal
                    && !nodes[id].IsMerged
                    && (protectedNodeIds == null || !protectedNodeIds.Contains(id)))
                .OrderBy(id => id).ToList();
            rng.Shuffle(branchNormals);
            assigned += PickAndAssign(branchNormals, nodes, RoomType.Elite, count - assigned);

            if (assigned < count)
                Debug.LogWarning($"[SpecialRoomAssigner] Elite 候选不足：需要 {count}，只分配了 {assigned}");
        }

        private static void AssignShop(
            Dictionary<string, GraphNode> nodes,
            HashSet<string> mainPathSet,
            int count,
            SeededRandom rng,
            ISet<string> protectedNodeIds)
        {
            if (count <= 0) return;
            int assigned = 0;

            // 叶子节点（度==1）中的 Normal
            var leaves = nodes.Keys
                .Where(id => !nodes[id].IsMerged
                    && nodes[id].RoomType == RoomType.Normal
                    && (protectedNodeIds == null || !protectedNodeIds.Contains(id))
                    && nodes[id].NeighborIds.Count(nid => nodes.ContainsKey(nid) && !nodes[nid].IsMerged) == 1)
                .OrderBy(id => id).ToList();
            rng.Shuffle(leaves);
            assigned += PickAndAssign(leaves, nodes, RoomType.Shop, count - assigned);

            if (assigned >= count) return;

            // 度<=2 的分支节点
            var lowDegree = nodes.Keys
                .Where(id => !nodes[id].IsMerged
                    && nodes[id].RoomType == RoomType.Normal
                    && !mainPathSet.Contains(id)
                    && (protectedNodeIds == null || !protectedNodeIds.Contains(id))
                    && nodes[id].NeighborIds.Count(nid => nodes.ContainsKey(nid) && !nodes[nid].IsMerged) <= 2)
                .OrderBy(id => id).ToList();
            rng.Shuffle(lowDegree);
            assigned += PickAndAssign(lowDegree, nodes, RoomType.Shop, count - assigned);

            if (assigned < count)
                Debug.LogWarning($"[SpecialRoomAssigner] Shop 候选不足：需要 {count}，只分配了 {assigned}");
        }

        private static void AssignEvent(
            Dictionary<string, GraphNode> nodes,
            HashSet<string> mainPathSet,
            int count,
            SeededRandom rng,
            ISet<string> protectedNodeIds)
        {
            if (count <= 0) return;
            int assigned = 0;

            // 非主路径 Normal
            var offMain = nodes.Keys
                .Where(id => !nodes[id].IsMerged
                    && nodes[id].RoomType == RoomType.Normal
                    && !mainPathSet.Contains(id)
                    && (protectedNodeIds == null || !protectedNodeIds.Contains(id)))
                .OrderBy(id => id).ToList();
            rng.Shuffle(offMain);
            assigned += PickAndAssign(offMain, nodes, RoomType.Event, count - assigned);

            if (assigned >= count) return;

            // 任意剩余 Normal
            var remaining = nodes.Keys
                .Where(id => !nodes[id].IsMerged
                    && nodes[id].RoomType == RoomType.Normal
                    && (protectedNodeIds == null || !protectedNodeIds.Contains(id)))
                .OrderBy(id => id).ToList();
            rng.Shuffle(remaining);
            assigned += PickAndAssign(remaining, nodes, RoomType.Event, count - assigned);

            if (assigned < count)
                Debug.LogWarning($"[SpecialRoomAssigner] Event 候选不足：需要 {count}，只分配了 {assigned}");
        }

        /// <summary>
        /// 从候选列表中选取指定数量的节点并赋予类型
        /// </summary>
        private static int PickAndAssign(
            List<string> candidates,
            Dictionary<string, GraphNode> nodes,
            RoomType type,
            int count)
        {
            int assigned = 0;
            foreach (var id in candidates)
            {
                if (assigned >= count) break;
                if (nodes[id].RoomType != RoomType.Normal) continue;
                nodes[id].RoomType = type;
                assigned++;
            }
            return assigned;
        }
    }
}
