using System.Collections;
using UnityEngine;
using RogueDungeon.Core;

namespace RogueDungeon.Debugging
{
    /// <summary>
    /// 测试用自动启动脚本，PlayMode 下自动推进 Boot → Hub → RunInit
    /// </summary>
    public class DebugAutoStart : MonoBehaviour
    {
        [SerializeField] private bool autoStart = true; // 是否自动启动

        private IEnumerator Start()
        {
            if (!autoStart) yield break;

            yield return null; // 等待所有 Awake/OnEnable 完成

            if (GameManager.Instance == null)
            {
                UnityEngine.Debug.LogError("[DebugAutoStart] GameManager.Instance 为 null");
                yield break;
            }

            GameManager.Instance.ChangeState(GameState.Hub);
            GameManager.Instance.ChangeState(GameState.RunInit);
            UnityEngine.Debug.Log("[DebugAutoStart] 已自动推进到 RunInit");
        }
    }
}
