using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public float horizontal;
    public float vertical;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInput();
    }

    void ProcessInput()
    {
        horizontal += Input.GetAxis("Horizontal");
        horizontal = Mathf.Clamp(horizontal, -1, 1);

        vertical += Input.GetAxis("Vertical");
        vertical = Mathf.Clamp(vertical, -1, 1);
        
    }
}
