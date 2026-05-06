using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    // 关闭暂停，回到游戏
    public void ClosePause()
    {
        Time.timeScale = 1;
        SceneManager.UnloadSceneAsync(2);
    }

    // 返回主菜单
    public void BackToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }

    // 退出游戏
    public void QuitGame()
    {
        Application.Quit();
    }
}