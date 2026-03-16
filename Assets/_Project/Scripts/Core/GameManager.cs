using UnityEngine;
using RPGSystem.Core;

namespace RPGSystem.Core
{
    /// <summary>
    /// 게임 상태
    /// </summary>
    public enum GameState
    {
        Playing,    // 게임 진행 중 (커서 잠금, 플레이어 조작 가능)
        UI,         // UI 열려있음 (일시정지, 커서 해제, 조작 불가)
        Paused      // 일시정지 메뉴
    }

    /// <summary>
    /// 게임 상태 매니저.
    /// 게임 전체 상태(Playing/UI/Paused)를 관리하고,
    /// 상태 변경 시 Time.timeScale, 커서, 입력 차단을 일괄 처리한다.
    ///
    /// UIManager가 패널을 열고 닫을 때 이 매니저를 통해 상태를 전환한다.
    /// PlayerInputBlocker가 이 상태를 읽어 1인칭 컨트롤러를 활성화/비활성화한다.
    ///
    /// [GameObject] "GameManager" 빈 오브젝트에 부착 (씬 최상위).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("초기 설정")]
        [Tooltip("게임 시작 시 커서를 잠글지 (1인칭 게임이면 true)")]
        [SerializeField] private bool lockCursorOnStart = true;

        [Tooltip("UI 모드에서 Time.timeScale을 0으로 할지")]
        [SerializeField] private bool pauseOnUI = true;

        [Header("현재 상태 (읽기 전용)")]
        [SerializeField] private GameState currentState = GameState.Playing;

        /// <summary>현재 게임 상태</summary>
        public GameState CurrentState => currentState;

        /// <summary>현재 게임이 플레이 중인지</summary>
        public bool IsPlaying => currentState == GameState.Playing;

        /// <summary>현재 UI가 열려있는지</summary>
        public bool IsInUI => currentState == GameState.UI;

        /// <summary>플레이어 입력이 허용되는지 (Playing 상태에서만)</summary>
        public bool IsInputAllowed => currentState == GameState.Playing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 게임 시작 시 Playing 상태로 강제 적용
            ApplyState(GameState.Playing);
        }

        // ──────────────────────────────────────
        // 상태 전환
        // ──────────────────────────────────────

        /// <summary>
        /// 게임 상태 변경.
        /// 상태에 따라 Time.timeScale, 커서, 이벤트가 자동 처리된다.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            var prevState = currentState;
            currentState = newState;
            ApplyState(newState);

            Debug.Log($"[Game] State: {prevState} → {newState}");

            EventBus.Publish(new GameStateChangedEvent
            {
                previousState = prevState,
                newState = newState
            });
        }

        /// <summary>
        /// Playing ↔ UI 토글. 이미 UI면 Playing으로, Playing이면 UI로.
        /// </summary>
        public void ToggleUI()
        {
            SetState(currentState == GameState.Playing ? GameState.UI : GameState.Playing);
        }

        private void ApplyState(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    if (pauseOnUI) Time.timeScale = 1f;
                    if (lockCursorOnStart)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                    break;

                case GameState.UI:
                    if (pauseOnUI) Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }
    }
}
