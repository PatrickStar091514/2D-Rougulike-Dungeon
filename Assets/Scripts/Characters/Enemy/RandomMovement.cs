using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMovement : MonoBehaviour
{
    [SerializeField] private float amplitudeX = 2f;
    [SerializeField] private float amplitudeY = 1f;
    [SerializeField] private float frequencyX = 1f;
    [SerializeField] private float frequencyY = 0.5f;
    [SerializeField] private Enemy enemy;
    [SerializeField] private float speed;

    [SerializeField] private Vector2 startPosition;
    private float timeOffset;
    [SerializeField] private Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        enemy = GetComponent<Enemy>();
        speed = enemy.data.MoveSpeed;
    }

    private void FixedUpdate()
    {
        // calculate sine-based offset
        float xOffset = Mathf.Sin(Time.fixedTime * frequencyX + timeOffset) * amplitudeX;
        float yOffset = Mathf.Cos(Time.fixedTime * frequencyY + timeOffset) * amplitudeY;
        Vector2 offset = new Vector2(xOffset, yOffset);

        // account for parent position if any
        Vector2 parentPos = transform.parent != null ? (Vector2)transform.parent.position : Vector2.zero;
        Vector2 targetPos = startPosition + offset + parentPos;

        rb.MovePosition(targetPos);

    }

    public void SetStartPosition(Vector2 position)
    {
        startPosition = position;
        rb.position = position; // ensure rigidbody2d matches
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
