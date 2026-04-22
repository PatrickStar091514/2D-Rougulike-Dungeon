using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RogueDungeon.Debugging
{
    /// <summary>
    /// 运行时位移诊断：输出 TimeScale、当前输入方向、玩家移动后位置。
    /// </summary>
    public class RuntimeMovementDiagnostics : MonoBehaviour
    {
        [SerializeField] private Transform player; // 玩家 Transform（为空时自动查找 Tag=Player）
        [SerializeField] private float logInterval = 0.25f; // 日志输出间隔（秒）
        [SerializeField] private bool onlyLogWhenMoved = true; // 仅在发生位移时输出

        private Vector3 _lastPosition; // 上次记录的位置
        private float _nextLogTime; // 下一次日志时间点

        private void Awake()
        {
            if (player == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (player != null)
                _lastPosition = player.position;
        }

        private void Update()
        {
            if (player == null) return;
            if (Time.unscaledTime < _nextLogTime) return;

            var input = ReadMoveInput();
            var currentPosition = player.position;
            var delta = currentPosition - _lastPosition;
            bool moved = delta.sqrMagnitude > 0.000001f;

            if (!onlyLogWhenMoved || moved)
            {
                Debug.Log(
                    $"[RuntimeMovementDiagnostics] timeScale={Time.timeScale:F2}, " +
                    $"input=({input.x:F2},{input.y:F2}), " +
                    $"playerPos=({currentPosition.x:F3},{currentPosition.y:F3},{currentPosition.z:F3}), " +
                    $"delta=({delta.x:F3},{delta.y:F3},{delta.z:F3})");
            }

            _lastPosition = currentPosition;
            _nextLogTime = Time.unscaledTime + Mathf.Max(0.02f, logInterval);
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
    }
}
