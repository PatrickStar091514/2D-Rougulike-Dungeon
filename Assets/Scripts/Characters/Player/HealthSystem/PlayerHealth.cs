using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private static float health = 5.0f;
    public static float Health
    {
        get => health;
        set => health = Mathf.Clamp(value, 0f, 10f);
    }


}
