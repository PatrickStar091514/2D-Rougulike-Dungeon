using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private const int HEART_ROW_SIZE = 8;
    [SerializeField] private const int HEART_OFFSET = 32;

    [SerializeField] private Sprite[] heartSprites;
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private RectTransform healthBar;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
