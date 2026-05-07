using UnityEngine;
using UnityEngine.SceneManagement;
using RogueDungeon.Core;
using UnityEngine.UI;
using System;

public class PauseUI : MonoBehaviour
{
    // [SerializeField] private Slider volumeSlider;

    // private void Start()
    // {
    //     volumeSlider.onValueChanged.AddListener(SetVolume);
    // }

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

    public void SetVolume(float volume)
    {
        // float volumePercent = volume / volumeSlider.maxValue;
        // RogueDungeon.Core.AudioManager.Instance?.SetMasterVolume(volumePercent);

        // if (volume <= 0.0f) volume = 0.0001f;
        // RogueDungeon.Core.AudioManager.Instance?.SetMasterVolume(Mathf.Log10(volume) * 20.0f);

        RogueDungeon.Core.AudioManager.Instance?.SetMasterVolume(Mathf.Pow(volume, 2));
    }
}