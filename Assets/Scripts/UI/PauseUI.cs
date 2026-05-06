using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core;

public class PauseUI : MonoBehaviour
{

    // 返回主菜单
    public void BackToMenu()
    {
        this.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    // 退出游戏
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}