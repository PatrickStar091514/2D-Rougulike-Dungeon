using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponRotation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    private Vector3 mousePos;
    private Camera mainCamera;


    // Start is called before the first frame update
    void Start()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        //sprite = GetComponent<SpriteRenderer>();

    }

    // Update is called once per frame
    void Update()
    {
        WeaponRotate();
    }

    void WeaponRotate()
    {
        mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector3 rotation = mousePos - transform.position;
        float rotationZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;

        // check if rotationZ is outside the range(86, -86)
        // check the range by looking at the z value in the inspector

        if (rotationZ > 88f || rotationZ < -88f)
        {
            sprite.flipY = true;
        }
        else
        {
            sprite.flipY = false;
        }
        transform.rotation = Quaternion.Euler(0, 0, rotationZ);
    }

    void WeaponFlip()
    {

    }
}
