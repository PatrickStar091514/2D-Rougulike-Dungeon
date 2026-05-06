using RogueDungeon.Data.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 地牢场景的暂停输入处理，按 ESC 加载暂停场景。
/// </summary>
public class PauseInputHandler : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausePanel != null && RunManager.Instance.CurrentRun != null) // 只有在 Run 进行中才允许暂停
            {
                bool isActive = pausePanel.activeSelf;
                pausePanel.SetActive(!isActive);
                Time.timeScale = isActive ? 1 : 0; // 切换时间流动状态
            }
        }
    }
}
