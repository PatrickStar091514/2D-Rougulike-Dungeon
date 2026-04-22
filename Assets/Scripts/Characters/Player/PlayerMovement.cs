using RogueDungeon.Rogue.Dungeon.Data;
using System;
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

    [Header("翻转属性")]
    [SerializeField] private bool useMouseToFlip = false;
    [SerializeField] private bool flipX = true;
    private bool isFacingRight = true;

    [SerializeField] SpriteRenderer sprite;
    PlayerInput input;
    [SerializeField] Rigidbody2D rb;
    BoxCollider2D boxCollider;

    // Start is called before the first frame update
    void Start()
    {
        input = GetComponent<PlayerInput>();
        if (input == null)
        {
            Debug.LogError("角色缺少PlayerInput组件！");
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Dynamic;

        boxCollider = GetComponent<BoxCollider2D>();

        sprite = GetComponent<SpriteRenderer>();

        originalX = transform.localScale.x;
        originalY = transform.localScale.y;


    }

    private void FixedUpdate()
    {
        Movement();
        FlipOnInput();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Movement()
    {
        float xVelocity = speed * input.horizontal;
        //if (xVelocity * direction < 0)
        //{
        //    FlipXDirection();
        //}
        float yVelocity = speed * input.vertical;
        //if (yVelocity * direction < 0)
        //{
        //    FlipYDirection();
        //}
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

    void FlipOnInput()
    {
        // Check if there is significant horizontal input
        if (Mathf.Abs(rb.velocity.x) > 0.1f)
        {
            bool mightFaceRight = rb.velocity.x > 0;
            if (mightFaceRight != isFacingRight)
            {
                FlipSprite(mightFaceRight);
            }
        }
    }

    private void FlipSprite(bool faceRight)
    {
        isFacingRight = faceRight;
        if (flipX)
        {
            // flip by scaling x
            sprite.flipX = !isFacingRight;
        }
        else
        {
            // flip by rotation
            transform.rotation = Quaternion.Euler(0, faceRight ? 0 : 180, 0);
        }
    }
}
