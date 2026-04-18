using System.Collections;
using System.Collections.Generic;
using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using UnityEngine;

public class StartPanel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartButtonClicked()
    {
        // 通过 GameManager 统一切换到 Hub 状态（会触发 GameStateChanged 事件）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Hub);
            this.gameObject.SetActive(false); // 隐藏开始界面
        }
        else
        {
            Debug.LogWarning("[StartPanel] GameManager.Instance 为 null，无法切换状态");
        }
    }
}
