using System.Collections.Generic;

namespace RogueDungeon.Dungeon.Generation
{
    /// <summary>
    /// 封装 System.Random 的确定性随机数生成器，提供地牢生成所需的全部随机操作
    /// </summary>
    public class SeededRandom
    {
        private readonly System.Random _random; // 内部随机源

        /// <summary>
        /// 以指定种子创建确定性随机数生成器
        /// </summary>
        /// <param name="seed">随机种子</param>
        public SeededRandom(int seed)
        {
            _random = new System.Random(seed);
        }

        /// <summary>
        /// 返回 [min, max) 范围内的随机整数
        /// </summary>
        /// <param name="min">最小值（含）</param>
        /// <param name="max">最大值（不含）</param>
        /// <returns>范围内的随机整数</returns>
        public int Range(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// 返回 [0, 1) 范围内的随机浮点数
        /// </summary>
        public float Value => (float)_random.NextDouble();

        /// <summary>
        /// 使用 Fisher-Yates 算法就地随机打乱列表
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="list">要打乱的列表</param>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 按权重随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="items">候选元素列表</param>
        /// <param name="weights">对应权重列表，权重为 0 的元素不会被选中</param>
        /// <returns>按权重概率选中的元素</returns>
        public T WeightedSelect<T>(IList<T> items, IList<float> weights)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
                totalWeight += weights[i];

            if (totalWeight <= 0f)
                return items[_random.Next(0, items.Count)];

            float roll = (float)_random.NextDouble() * totalWeight;
            float cumulative = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return items[i];
            }
            return items[items.Count - 1];
        }

        /// <summary>
        /// 整数子种子哈希
        /// </summary>
        /// <param name="seed">基础种子</param>
        /// <param name="index">索引</param>
        /// <returns>组合后的哈希值</returns>
        public static int Hash(int seed, int index)
        {
            return seed * 31 + index;
        }

        /// <summary>
        /// 字符串子种子哈希（使用自定义确定性哈希，不依赖 string.GetHashCode）
        /// </summary>
        /// <param name="seed">基础种子</param>
        /// <param name="tag">标签字符串</param>
        /// <returns>组合后的哈希值</returns>
        public static int Hash(int seed, string tag)
        {
            return seed * 31 + DeterministicHash(tag);
        }

        /// <summary>
        /// 自定义确定性字符串哈希（FNV-1a），跨平台跨运行一致
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns>确定性哈希值</returns>
        public static int DeterministicHash(string str)
        {
            unchecked
            {
                int hash = (int)2166136261u;
                for (int i = 0; i < str.Length; i++)
                {
                    hash ^= str[i];
                    hash *= 16777619;
                }
                return hash;
            }
        }
    }
}
