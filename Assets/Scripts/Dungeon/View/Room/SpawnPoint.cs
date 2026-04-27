using UnityEngine;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// 生成点标记组件，挂载在 Prefab 内的指定位置以标记实体生成点
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnType type; // 生成点类型

        /// <summary>此生成点的类型</summary>
        public SpawnType Type => type;
    }
}
