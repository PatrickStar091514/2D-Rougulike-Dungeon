using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider hpBar;
    // Start is called before the first frame update
    void Start()
    {
        hpBar = GameObject.Find("Slider").GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        //hpBar.value = (int) PlayerHealth.Instance.maxHP - (PlayerHealth.Instance.maxHP - PlayerHealth.Instance.Health);
        hpBar.value = (float)PlayerHealth.Instance.Health / PlayerHealth.Instance.maxHP;
    }

}
