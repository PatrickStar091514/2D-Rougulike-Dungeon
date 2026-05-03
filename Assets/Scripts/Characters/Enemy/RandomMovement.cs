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
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
        enemy = GetComponent<Enemy>();
        speed = enemy.data.MoveSpeed;
    }

    void OnEnable()
    {
        startPosition = transform.localPosition;
    }

    private void FixedUpdate()
    {
        // calculate sine-based offset
        float xOffset = Mathf.Sin(Time.fixedTime * frequencyX + timeOffset) * amplitudeX;
        float yOffset = Mathf.Cos(Time.fixedTime * frequencyY + timeOffset) * amplitudeY;
        Vector2 offset = new Vector2(xOffset, yOffset);

        // 本地坐标 + 偏移 → 通过父物体转换为世界坐标
        Vector2 targetLocalPos = startPosition + offset;
        Vector2 targetWorldPos = transform.parent != null
            ? (Vector2)transform.parent.TransformPoint(targetLocalPos)
            : targetLocalPos;

        rb.MovePosition(targetWorldPos);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
