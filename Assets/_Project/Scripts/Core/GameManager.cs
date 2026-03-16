using UnityEngine;

namespace RPGSystem.Core
{
    /// <summary>
    /// 게임 상태 관리.
    /// UI가 열리면 게임을 일시정지하고 커서를 해제한다.
    ///
    /// [GameObject] "GameManager" 빈 오브젝트에 부착 (씬 최상위).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Playing,    // 게임 진행 중
            UI,         // UI 열려있음 (일시정지)
            Paused      // 일시정지 메뉴
        }

        [SerializeField] private GameState currentState = GameState.Playing;
        public GameState CurrentState => currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 게임 상태 변경. UI 열림 시 자동으로 일시정지 + 커서 해제.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;

                case GameState.UI:
                case GameState.Paused:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }

            Debug.Log($"[Game] State → {newState}");
        }
    }
}
