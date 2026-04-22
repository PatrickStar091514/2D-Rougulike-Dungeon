using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public float horizontal;
    public float vertical;
    bool readyToClear;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    //private void FixedUpdate()
    //{
    //    readyToClear = true;
    //}
    // Update is called once per frame
    void Update()
    {
        ProcessInput();
        ClearInput();
    }

    private void ClearInput()
    {
        if (!readyToClear)
            return;
        horizontal = 0;
        vertical = 0;
    }

    void ProcessInput()
    {
        //horizontal += Input.GetAxis("Horizontal");
        //horizontal = Mathf.Clamp(horizontal, -1, 1);

        //vertical += Input.GetAxis("Vertical");
        //vertical = Mathf.Clamp(vertical, -1, 1);

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

    }
}
