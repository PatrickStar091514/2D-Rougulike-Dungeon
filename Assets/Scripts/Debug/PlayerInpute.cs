using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RogueDungeon.Debugging
{
    /// <summary>
    /// 测试用玩家输入组件。
    /// 读取 WASD/方向键并直接驱动位移。
    /// </summary>
    public class PlayerInpute : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f; // 移动速度（单位/秒）

        private Vector2 _currentInput; // 当前输入方向

        /// <summary>
        /// 当前输入方向
        /// </summary>
        public Vector2 CurrentInput => _currentInput;

        private void Update()
        {
            _currentInput = ReadMoveInput();
            var delta = _currentInput * moveSpeed * Time.deltaTime;
            if (delta.sqrMagnitude > 0f)
                transform.position += new Vector3(delta.x, delta.y, 0f);
        }

        private Vector2 ReadMoveInput()
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
    }
}
