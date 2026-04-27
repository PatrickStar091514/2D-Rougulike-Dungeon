using UnityEngine;
using UnityEngine.Tilemaps;

namespace RogueDungeon.Dungeon.View
{
    /// <summary>
    /// MVP 迷雾控制器，通过 SetActive 和 MaterialPropertyBlock 实现三态可见性。
    /// Hidden=隐藏 GameObject, Silhouette=暗色调, Revealed=原色。
    /// </summary>
    public class SimpleFogController : IRoomFogController
    {
        private static readonly Color DarkTint = new(0.15f, 0.15f, 0.2f, 1f); // 暗色轮廓 tint
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private readonly GameObject _rootGo;             // 房间根 GameObject
        private readonly TilemapRenderer[] _renderers;   // 缓存的 TilemapRenderer 引用
        private readonly MaterialPropertyBlock _mpb = new(); // 避免材质实例化

        /// <summary>
        /// 创建 SimpleFogController 实例
        /// </summary>
        /// <param name="rootGo">房间根 GameObject</param>
        public SimpleFogController(GameObject rootGo)
        {
            _rootGo = rootGo;
            _renderers = rootGo.GetComponentsInChildren<TilemapRenderer>(true);
        }

        /// <summary>
        /// 应用指定的可见性状态
        /// </summary>
        /// <param name="visibility">目标可见性状态</param>
        public void ApplyVisibility(RoomVisibility visibility)
        {
            switch (visibility)
            {
                case RoomVisibility.Hidden:
                    _rootGo.SetActive(false);
                    break;

                case RoomVisibility.Silhouette:
                    _rootGo.SetActive(true);
                    SetRenderersColor(DarkTint);
                    break;

                case RoomVisibility.Revealed:
                    _rootGo.SetActive(true);
                    SetRenderersColor(Color.white);
                    break;
            }
        }

        private void SetRenderersColor(Color color)
        {
            _mpb.SetColor(ColorProp, color);
            foreach (var renderer in _renderers)
            {
                if (renderer != null)
                    renderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
