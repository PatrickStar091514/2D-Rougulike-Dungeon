using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeedInputCtrl : MonoBehaviour
{
    // 这里直接拖【输入框本体】
    public TMP_InputField seedInputField;

    void Start()
    {
        // 运行时默认隐藏输入框
        if (seedInputField != null)
            seedInputField.gameObject.SetActive(false);
    }

    public void ShowInput()
    {
        if (seedInputField != null)
            seedInputField.gameObject.SetActive(true);
    }
}