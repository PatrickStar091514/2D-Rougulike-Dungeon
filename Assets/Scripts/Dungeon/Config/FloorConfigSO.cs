using System;
using UnityEngine;
using RogueDungeon.Dungeon.Types;

namespace RogueDungeon.Dungeon.Config
{
    /// <summary>
    /// 形状权重配置，用于指定各房间形状在合并时的选择权重
    /// </summary>
    [Serializable]
    public struct ShapeWeight
    {
        public RoomShape shape; // 形状类型
        public float weight;    // 权重 (≥0)
    }

    /// <summary>
    /// 楼层生成配置 ScriptableObject，定义一层地牢的生成参数
    /// </summary>
    [CreateAssetMenu(fileName = "NewFloorConfig", menuName = "Dungeon/Floor Config")]
    public class FloorConfigSO : ScriptableObject
    {
        [SerializeField] private int gridWidth = 5;          // 网格宽度
        [SerializeField] private int gridHeight = 4;         // 网格高度
        [SerializeField] private int targetRoomCount = 12;   // 目标房间总数
        [SerializeField] private int eliteCount = 1;         // 精英房数量
        [SerializeField] private int eventCount = 1;         // 事件房数量
        [SerializeField] private float mergeRate = 0.3f;     // 普通房合并率 [0,1]
        [SerializeField] private ShapeWeight[] shapeWeights; // 各形状的合并权重
        [SerializeField] private RoomTemplateSO[] templates;  // 可用房间模板列表

        /// <summary>网格宽度</summary>
        public int GridWidth => gridWidth;

        /// <summary>网格高度</summary>
        public int GridHeight => gridHeight;

        /// <summary>目标房间总数</summary>
        public int TargetRoomCount => targetRoomCount;

        /// <summary>精英房数量</summary>
        public int EliteCount => eliteCount;

        /// <summary>事件房数量</summary>
        public int EventCount => eventCount;

        /// <summary>普通房合并率 [0,1]</summary>
        public float MergeRate => mergeRate;

        /// <summary>各形状的合并权重</summary>
        public ShapeWeight[] ShapeWeights => shapeWeights;

        /// <summary>可用房间模板列表</summary>
        public RoomTemplateSO[] Templates => templates;

        /// <summary>
        /// 校验配置参数的合理性，检查所有规则并输出所有失败项的警告
        /// </summary>
        /// <returns>所有校验通过返回 true，任一失败返回 false</returns>
        public bool Validate()
        {
            bool valid = true;

            if (gridWidth < 3 || gridHeight < 3)
            {
                Debug.LogWarning($"[FloorConfigSO] 网格尺寸过小: {gridWidth}×{gridHeight}，最小要求 3×3");
                valid = false;
            }

            if (targetRoomCount > gridWidth * gridHeight)
            {
                Debug.LogWarning($"[FloorConfigSO] 目标房间数 ({targetRoomCount}) 超过网格容量 ({gridWidth * gridHeight})");
                valid = false;
            }

            if (targetRoomCount < 5)
            {
                Debug.LogWarning($"[FloorConfigSO] 目标房间数 ({targetRoomCount}) 不足，最少需要 5 间 (Start+Boss+3通道)");
                valid = false;
            }

            int specialTotal = eliteCount + eventCount;
            if (specialTotal >= targetRoomCount - 2)
            {
                Debug.LogWarning($"[FloorConfigSO] 特殊房间总数 ({specialTotal}) 过多，应小于 targetRoomCount-2 ({targetRoomCount - 2})");
                valid = false;
            }

            if (mergeRate < 0f || mergeRate > 1f)
            {
                Debug.LogWarning($"[FloorConfigSO] 合并率 ({mergeRate}) 越界，已 Clamp 到 [0,1]");
                mergeRate = Mathf.Clamp01(mergeRate);
                valid = false;
            }

            if (templates == null || templates.Length == 0)
            {
                Debug.LogWarning("[FloorConfigSO] 模板列表为空，无法进行生成");
                valid = false;
            }

            return valid;
        }
    }
}
