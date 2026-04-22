using UnityEngine;
using RogueDungeon.Rogue.Dungeon.View;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RogueDungeon.Debugging
{
    /// <summary>
    /// 测试用 2D 角色控制器：WASD/方向键移动，并接入 DoorTransit 输入锁。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class DebugPlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f; // 移动速度（单位/秒）
        [SerializeField] private bool ensureVisible = true; // 自动补可视化（测试用）
        [SerializeField] private Color debugColor = Color.cyan; // 调试可视化颜色

        private Rigidbody2D _rb; // 玩家刚体引用
        private Vector2 _moveInput; // 当前输入方向

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (ensureVisible)
                EnsureDebugVisual();
        }

        private void Update()
        {
            if (!DoorTransitCoordinator.InputEnabled)
            {
                _moveInput = Vector2.zero;
                if (_rb != null) _rb.velocity = Vector2.zero;
                return;
            }

            _moveInput = ReadMoveInput();
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;
            var delta = _moveInput * moveSpeed * Time.fixedDeltaTime;
            if (delta.sqrMagnitude <= 0f)
            {
                _rb.velocity = Vector2.zero;
                return;
            }

            transform.position += new Vector3(delta.x, delta.y, 0f);
            _rb.position = new Vector2(transform.position.x, transform.position.y);
        }

        private void OnDisable()
        {
            if (_rb != null)
                _rb.velocity = Vector2.zero;
        }

        private static Vector2 ReadMoveInput()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y = -1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y = 1f;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y = -1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y = 1f;
            }
#endif
            return new Vector2(x, y).normalized;
        }

        private void EnsureDebugVisual()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            if (spriteRenderer.sprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();

                var sprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                spriteRenderer.sprite = sprite;
            }

            spriteRenderer.color = debugColor;
            transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
    }
}
