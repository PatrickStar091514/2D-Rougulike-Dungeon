using RogueDungeon.Rogue.Dungeon.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动属性")]
    public float speed = 8f;
    int direction = 1;
    float originalX;
    float originalY;
    

    [Header("环境检测属性")]
    public LayerMask groundLayer;
    PlayerInput input;
    Rigidbody2D rb;
    BoxCollider2D boxCollider;

    // Start is called before the first frame update
    void Start()
    {
        input = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        originalX = transform.localScale.x;
        originalY = transform.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Movement()
    {
        float xVelocity = speed * input.horizontal;
        if (xVelocity * direction < 0)
        {
            FlipXDirection();
        }
        float yVelocity = speed * input.vertical;
        if (yVelocity * direction < 0)
        {
            FlipYDirection();
        }
        rb.velocity = new Vector2(xVelocity, yVelocity);
    }

    void FlipXDirection()
    {
        direction *= -1;
        Vector3 scale = transform.localScale;
        scale.x = direction * originalX;
        transform.localScale = scale;
    }
    void FlipYDirection()
    {
        direction *= -1;
        Vector3 scale = transform.localScale;
        scale.y = direction * originalY;
        transform.localScale = scale;
    }
}
