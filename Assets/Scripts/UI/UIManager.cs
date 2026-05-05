using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 开始按钮调用：进游戏
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    // 退出游戏按钮调用
    public void ExitGame()
    {
        Application.Quit();
    }

    // 按ESC弹出暂停
    void LateUpdate()
    {
        // 只在游戏场景生效
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 叠加加载暂停场景，不卸载游戏
                SceneManager.LoadScene(2, LoadSceneMode.Additive);
                Time.timeScale = 0;
            }
        }
    }
}