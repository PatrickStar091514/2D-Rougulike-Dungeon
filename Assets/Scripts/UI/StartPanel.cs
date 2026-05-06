using System.Collections;
using System.Collections.Generic;
using RogueDungeon.Core;
using RogueDungeon.Core.Events;
using TMPro;
using UnityEngine;

public class StartPanel : MonoBehaviour
{
    public TMP_InputField seedInput;

    // Start is called before the first frame update
    void Start()
    {
        seedInput.onValueChanged.AddListener(InputSeed);
        seedInput.onEndEdit.AddListener(SetSeed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Seed = int.Parse(seedInput.text == "" || seedInput.text == "-" ? "0" : seedInput.text);
            GameManager.Instance.StartNewGame();
            this.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[StartPanel] GameManager.Instance 为 null，无法切换状态");
        }
    }

    private void InputSeed(string seedString)
    {
        System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("^-?[0-9]+$");
        if (reg.IsMatch(seedString))
        {
            seedInput.text = seedString;
        }
        else
        {
            if (seedInput.text == "") seedInput.text = "";
            else if (seedInput.text == "-") seedInput.text = "-";
            else
            {
                seedInput.text = seedString.Substring(0, seedString.Length - 1);
            }
        }  
    }

    private void SetSeed(string seedString)
    {
        GameManager.Instance.Seed = int.Parse(seedString == "" || seedString == "-" ? "0" : seedString);
    }
}
