using UnityEngine;
using RPGSystem.Core;

namespace RPGSystem.UI
{
    /// <summary>
    /// 플레이어 입력 차단기.
    /// GameState에 따라 플레이어 컨트롤러의 활성화/비활성화를 자동 처리한다.
    ///
    /// ── 1인칭 컨트롤러 연결 방법 ──
    ///
    /// 방법 1: Inspector에서 직접 연결 (권장)
    ///   - playerController에 이동 스크립트 (CharacterController 등)
    ///   - cameraController에 카메라 회전 스크립트 (MouseLook 등)
    ///   → UI가 열리면 두 컴포넌트가 자동으로 enabled = false
    ///
    /// 방법 2: 플레이어 컨트롤러에서 GameManager.IsInputAllowed 확인
    ///   ```
    ///   void Update() {
    ///       if (!GameManager.Instance.IsInputAllowed) return;
    ///       // 이동/카메라 로직
    ///   }
    ///   ```
    ///
    /// 방법 3: GameStateChangedEvent 구독
    ///   ```
    ///   EventBus.Subscribe&lt;GameStateChangedEvent&gt;(OnStateChanged);
    ///   void OnStateChanged(GameStateChangedEvent e) {
    ///       enabled = e.newState == GameState.Playing;
    ///   }
    ///   ```
    ///
    /// [GameObject] 플레이어 오브젝트에 부착.
    /// [Inspector] playerController, cameraController에 해당 컴포넌트 드래그.
    /// </summary>
    public class PlayerInputBlocker : MonoBehaviour
    {
        [Header("비활성화할 컴포넌트 (Inspector에서 드래그)")]
        [Tooltip("플레이어 이동 스크립트 (CharacterController, Rigidbody 기반 등)")]
        [SerializeField] private MonoBehaviour playerController;

        [Tooltip("카메라 회전 스크립트 (MouseLook, CinemachineInputProvider 등)")]
        [SerializeField] private MonoBehaviour cameraController;

        [Header("추가 비활성화 컴포넌트")]
        [Tooltip("UI 모드에서 비활성화할 추가 컴포넌트들")]
        [SerializeField] private MonoBehaviour[] additionalControllers;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

            // 현재 상태에 즉시 반영
            if (GameManager.Instance != null)
                ApplyInputState(GameManager.Instance.IsInputAllowed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            bool inputAllowed = evt.newState == GameState.Playing;
            ApplyInputState(inputAllowed);
        }

        private void ApplyInputState(bool inputAllowed)
        {
            if (playerController != null)
                playerController.enabled = inputAllowed;

            if (cameraController != null)
                cameraController.enabled = inputAllowed;

            if (additionalControllers != null)
            {
                foreach (var ctrl in additionalControllers)
                {
                    if (ctrl != null)
                        ctrl.enabled = inputAllowed;
                }
            }

            Debug.Log($"[Input] Player input {(inputAllowed ? "ENABLED" : "DISABLED")}");
        }
    }
}
