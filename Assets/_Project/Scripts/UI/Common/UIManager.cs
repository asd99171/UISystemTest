using System.Collections.Generic;
using UnityEngine;
using RPGSystem.Core;

namespace RPGSystem.UI
{
    /// <summary>
    /// UI 매니저.
    /// 모든 UI 패널의 열기/닫기를 관리하고, 패널 스택으로 ESC 닫기를 처리한다.
    /// 패널이 하나라도 열리면 GameManager를 UI 상태로, 모두 닫히면 Playing 상태로 전환한다.
    ///
    /// [GameObject] "UIManager" 빈 오브젝트에 부착 (Canvas와 같은 레벨 또는 상위).
    /// [Inspector] panelRegistry에 씬의 모든 UIPanel을 등록.
    /// [Inspector] UI 토글 키를 설정.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("패널 등록")]
        [Tooltip("씬에 존재하는 모든 UIPanel을 여기에 등록")]
        [SerializeField] private UIPanel[] panelRegistry;

        [Header("키 바인딩")]
        [Tooltip("인벤토리 토글 키")]
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        [Tooltip("장비 토글 키")]
        [SerializeField] private KeyCode equipmentKey = KeyCode.E;
        [Tooltip("스킬 토글 키")]
        [SerializeField] private KeyCode skillKey = KeyCode.K;
        [Tooltip("ESC 키로 최상위 패널 닫기 또는 일시정지")]
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        /// <summary>열려있는 패널 스택 (LIFO: 마지막에 열린 패널이 먼저 닫힘)</summary>
        private readonly Stack<UIPanel> _panelStack = new Stack<UIPanel>();

        /// <summary>이름 → UIPanel 빠른 조회</summary>
        private Dictionary<string, UIPanel> _panelLookup;

        /// <summary>현재 열려있는 패널 수</summary>
        public int OpenPanelCount => _panelStack.Count;

        /// <summary>UI가 하나라도 열려있는지</summary>
        public bool HasOpenPanel => _panelStack.Count > 0;

        // ──────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildLookup();
            CloseAllPanelsImmediate();
        }

        private void BuildLookup()
        {
            _panelLookup = new Dictionary<string, UIPanel>();
            if (panelRegistry == null) return;

            foreach (var panel in panelRegistry)
            {
                if (panel != null && !string.IsNullOrEmpty(panel.PanelName))
                {
                    _panelLookup[panel.PanelName] = panel;
                }
            }
        }

        /// <summary>
        /// Update에서 키 입력 처리.
        /// 주의: Time.timeScale=0이어도 Update는 호출되므로 Input 사용 가능.
        /// </summary>
        private void Update()
        {
            // ESC: 열린 패널이 있으면 최상위 닫기, 없으면 일시정지 토글
            if (Input.GetKeyDown(closeKey))
            {
                if (HasOpenPanel)
                    CloseTop();
                else
                    TogglePause();
            }

            // UI 토글 키 (Playing 또는 UI 상태에서만)
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState != GameState.Paused)
            {
                if (Input.GetKeyDown(inventoryKey))
                    TogglePanel("Inventory");
                if (Input.GetKeyDown(equipmentKey))
                    TogglePanel("Equipment");
                if (Input.GetKeyDown(skillKey))
                    TogglePanel("Skill");
            }
        }

        // ──────────────────────────────────────
        // 패널 열기 / 닫기
        // ──────────────────────────────────────

        /// <summary>
        /// 이름으로 패널 열기. 이미 열려있으면 무시.
        /// </summary>
        public bool OpenPanel(string panelName)
        {
            if (!_panelLookup.TryGetValue(panelName, out var panel))
            {
                Debug.LogWarning($"[UI] Panel not found: {panelName}");
                return false;
            }

            if (panel.IsOpen)
                return false;

            panel.Open();
            _panelStack.Push(panel);

            // 첫 패널이 열리면 → UI 상태
            if (_panelStack.Count == 1)
                EnterUIState();

            Debug.Log($"[UI] Opened: {panelName} (stack: {_panelStack.Count})");
            EventBus.Publish(new UIPanelOpenedEvent { panelName = panelName });
            return true;
        }

        /// <summary>
        /// 이름으로 패널 닫기.
        /// </summary>
        public bool ClosePanel(string panelName)
        {
            if (!_panelLookup.TryGetValue(panelName, out var panel))
                return false;

            if (!panel.IsOpen)
                return false;

            panel.Close();
            RebuildStack();

            if (_panelStack.Count == 0)
                ExitUIState();

            Debug.Log($"[UI] Closed: {panelName} (stack: {_panelStack.Count})");
            EventBus.Publish(new UIPanelClosedEvent { panelName = panelName });
            return true;
        }

        /// <summary>
        /// 패널 토글 (열려있으면 닫고, 닫혀있으면 열기).
        /// </summary>
        public void TogglePanel(string panelName)
        {
            if (!_panelLookup.TryGetValue(panelName, out var panel))
            {
                Debug.LogWarning($"[UI] Panel not found: {panelName}");
                return;
            }

            if (panel.IsOpen)
                ClosePanel(panelName);
            else
                OpenPanel(panelName);
        }

        /// <summary>
        /// 스택 최상위(마지막에 열린) 패널 닫기. ESC 키에서 호출.
        /// </summary>
        public void CloseTop()
        {
            if (_panelStack.Count == 0) return;

            var top = _panelStack.Pop();
            top.Close();

            if (_panelStack.Count == 0)
                ExitUIState();

            Debug.Log($"[UI] Closed top: {top.PanelName} (stack: {_panelStack.Count})");
            EventBus.Publish(new UIPanelClosedEvent { panelName = top.PanelName });
        }

        /// <summary>
        /// 모든 패널 닫기.
        /// </summary>
        public void CloseAll()
        {
            while (_panelStack.Count > 0)
            {
                var panel = _panelStack.Pop();
                panel.Close();
                EventBus.Publish(new UIPanelClosedEvent { panelName = panel.PanelName });
            }
            ExitUIState();
            Debug.Log("[UI] All panels closed.");
        }

        /// <summary>초기화 시 모든 패널 비활성화 (이벤트 발행 없이)</summary>
        private void CloseAllPanelsImmediate()
        {
            if (panelRegistry == null) return;
            foreach (var panel in panelRegistry)
            {
                if (panel != null)
                {
                    panel.Close();
                }
            }
        }

        // ──────────────────────────────────────
        // 일시정지 메뉴
        // ──────────────────────────────────────

        private void TogglePause()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.Paused)
                GameManager.Instance.SetState(GameState.Playing);
            else if (GameManager.Instance.CurrentState == GameState.Playing)
                GameManager.Instance.SetState(GameState.Paused);
        }

        // ──────────────────────────────────────
        // 상태 전환
        // ──────────────────────────────────────

        private void EnterUIState()
        {
            GameManager.Instance?.SetState(GameState.UI);
        }

        private void ExitUIState()
        {
            GameManager.Instance?.SetState(GameState.Playing);
        }

        /// <summary>스택에서 닫힌 패널 제거 (중간 패널이 닫힌 경우)</summary>
        private void RebuildStack()
        {
            var temp = new Stack<UIPanel>();
            while (_panelStack.Count > 0)
            {
                var p = _panelStack.Pop();
                if (p.IsOpen)
                    temp.Push(p);
            }
            while (temp.Count > 0)
            {
                _panelStack.Push(temp.Pop());
            }
        }

        // ──────────────────────────────────────
        // 조회
        // ──────────────────────────────────────

        /// <summary>이름으로 패널 조회</summary>
        public UIPanel GetPanel(string panelName)
        {
            _panelLookup.TryGetValue(panelName, out var panel);
            return panel;
        }

        /// <summary>특정 패널이 열려있는지</summary>
        public bool IsPanelOpen(string panelName)
        {
            if (_panelLookup.TryGetValue(panelName, out var panel))
                return panel.IsOpen;
            return false;
        }

        // ──────────────────────────────────────
        // 디버그
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print Panel Stack")]
        public void DebugPrintStack()
        {
            Debug.Log($"═══════ UI PANEL STACK ({_panelStack.Count}) ═══════");
            int idx = 0;
            foreach (var panel in _panelStack)
            {
                Debug.Log($"  [{idx++}] {panel.PanelName} (open: {panel.IsOpen})");
            }
            if (_panelStack.Count == 0)
                Debug.Log("  (empty)");
            Debug.Log("══════════════════════════════════════");
        }
    }
}
