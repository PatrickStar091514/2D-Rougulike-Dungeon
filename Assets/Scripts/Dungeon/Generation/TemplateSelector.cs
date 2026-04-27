using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RogueDungeon.Dungeon.Types;
using RogueDungeon.Dungeon.Config;

namespace RogueDungeon.Dungeon.Generation
{
    /// <summary>
    /// 模板选择器：为每个节点从配置的 RoomTemplateSO 列表中按权重选择匹配模板
    /// </summary>
    internal static class TemplateSelector
    {
        /// <summary>
        /// 为所有非合并节点分配模板
        /// </summary>
        /// <param name="nodes">节点字典</param>
        /// <param name="config">楼层配置</param>
        /// <param name="rng">确定性随机源</param>
        public static void Assign(
            Dictionary<string, GraphNode> nodes,
            FloorConfigSO config,
            SeededRandom rng)
        {
            if (config.Templates == null || config.Templates.Length == 0)
            {
                Debug.LogWarning("[TemplateSelector] 配置中无模板");
                return;
            }

            // 按 Id 排序确保确定性（D5）
            var sortedNodes = nodes.Keys
                .Where(id => !nodes[id].IsMerged)
                .OrderBy(id => id).ToList();

            foreach (var id in sortedNodes)
            {
                var node = nodes[id];

                // 筛选匹配形状和类型的模板
                var candidates = new List<RoomTemplateSO>();
                var weights = new List<float>();

                foreach (var t in config.Templates)
                {
                    if (t == null) continue;
                    if (t.Shape != node.RoomShape) continue;
                    if (t.AllowedTypes == null || t.AllowedTypes.Length == 0 ||
                        !t.AllowedTypes.Contains(node.RoomType))
                        continue;

                    candidates.Add(t);
                    weights.Add(t.Weight > 0f ? t.Weight : 1f);
                }

                if (candidates.Count == 0)
                {
                    Debug.LogWarning($"[TemplateSelector] 节点 {id}（{node.RoomType}, {node.RoomShape}）找不到匹配模板");
                    node.TemplateId = null;
                    continue;
                }

                var selected = rng.WeightedSelect(candidates, weights);
                node.TemplateId = selected.TemplateId;
            }
        }
    }
}
