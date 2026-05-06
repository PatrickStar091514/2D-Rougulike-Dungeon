using System;
using System.Collections;
using System.Collections.Generic;
using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using UnityEngine;

public class ScorePanel : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text scoreText;
    [SerializeField] private int score = 0;

    void Awake()
    {
        EventCenter.AddListener<GetScoreEvent>(GameEventType.GetScore, OnGetScore);
        EventCenter.AddListener<RunEndedEvent>(GameEventType.RunEnded, OnRunEnded);
        score = 0;
        gameObject.SetActive(false);
    }

    void Start()
    {
        UpdateScoreText();
    }

    void OnDestroy()
    {
        EventCenter.RemoveListener<GetScoreEvent>(GameEventType.GetScore, OnGetScore);
        EventCenter.RemoveListener<RunEndedEvent>(GameEventType.RunEnded, OnRunEnded);
    }

    private void OnGetScore(GetScoreEvent eventData)
    {
        score += eventData.ScoreDelta;
    }

    private void OnRunEnded(RunEndedEvent eventData)
    {
        UpdateScoreText();
        this.gameObject.SetActive(true);
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void ResetGame()
    {
        GameManager.Instance.ResetGame();
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
