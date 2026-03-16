using UnityEngine;
using RPGSystem.Core;
using RPGSystem.Inventory;

namespace RPGSystem.UI
{
    /// <summary>
    /// 인벤토리 UI 컨트롤러.
    /// InventoryManager의 데이터를 UI 슬롯 그리드로 표시한다.
    /// EventBus를 구독하여 실시간 갱신한다.
    ///
    /// 핵심 원칙: UI는 데이터를 저장하지 않고, Manager에서 읽어와 표시만 한다.
    ///
    /// [GameObject] 인벤토리 패널(UIPanel)과 동일 오브젝트에 부착.
    /// [Inspector] slotContainer에 Grid Layout Group이 있는 Transform 연결.
    /// [Inspector] slotPrefab에 InventorySlotUI가 부착된 Prefab 연결.
    /// </summary>
    public class InventoryUIController : UIPanel
    {
        [Header("인벤토리 UI")]
        [Tooltip("슬롯 Prefab (InventorySlotUI 포함)")]
        [SerializeField] private InventorySlotUI slotPrefab;

        [Tooltip("슬롯이 배치될 컨테이너 (Grid Layout Group)")]
        [SerializeField] private Transform slotContainer;

        /// <summary>생성된 슬롯 UI 배열</summary>
        private InventorySlotUI[] _slotUIs;

        /// <summary>현재 선택된 슬롯 인덱스 (-1 = 선택 없음)</summary>
        private int _selectedIndex = -1;

        /// <summary>현재 선택된 슬롯 인덱스</summary>
        public int SelectedIndex => _selectedIndex;

        // ──────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────

        private void Start()
        {
            CreateSlots();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
            EventBus.Subscribe<InventoryRefreshEvent>(OnInventoryRefresh);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
            EventBus.Unsubscribe<InventoryRefreshEvent>(OnInventoryRefresh);
        }

        // ──────────────────────────────────────
        // 슬롯 생성
        // ──────────────────────────────────────

        /// <summary>
        /// InventoryManager.SlotCount만큼 슬롯 UI를 동적 생성.
        /// </summary>
        private void CreateSlots()
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null || slotPrefab == null || slotContainer == null)
            {
                Debug.LogWarning("[InventoryUI] Missing references.");
                return;
            }

            int count = inventory.SlotCount;
            _slotUIs = new InventorySlotUI[count];

            for (int i = 0; i < count; i++)
            {
                var slotObj = Instantiate(slotPrefab, slotContainer);
                slotObj.gameObject.name = $"InvSlot_{i:D2}";
                slotObj.Initialize(i);
                slotObj.SetController(this);
                _slotUIs[i] = slotObj;
            }
        }

        // ──────────────────────────────────────
        // 패널 열기/닫기
        // ──────────────────────────────────────

        protected override void OnOpened()
        {
            RefreshAll();
            Debug.Log("[InventoryUI] Opened");
        }

        protected override void OnClosed()
        {
            ClearSelection();
            TooltipUI.Instance?.Hide();
            Debug.Log("[InventoryUI] Closed");
        }

        // ──────────────────────────────────────
        // 갱신
        // ──────────────────────────────────────

        /// <summary>전체 슬롯 갱신</summary>
        public void RefreshAll()
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null || _slotUIs == null) return;

            for (int i = 0; i < _slotUIs.Length; i++)
            {
                _slotUIs[i].Refresh(inventory.Slots[i]);
            }
        }

        /// <summary>단일 슬롯 갱신</summary>
        private void RefreshSlot(int index)
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null || _slotUIs == null) return;
            if (index < 0 || index >= _slotUIs.Length) return;

            _slotUIs[index].Refresh(inventory.Slots[index]);
        }

        // ──────────────────────────────────────
        // 이벤트 핸들러
        // ──────────────────────────────────────

        private void OnInventoryChanged(InventoryChangedEvent evt)
        {
            if (!IsOpen) return;
            RefreshSlot(evt.slotIndex);
        }

        private void OnInventoryRefresh(InventoryRefreshEvent evt)
        {
            if (!IsOpen) return;
            RefreshAll();
        }

        // ──────────────────────────────────────
        // 선택
        // ──────────────────────────────────────

        /// <summary>슬롯이 클릭됨 (InventorySlotUI가 호출)</summary>
        public void OnSlotSelected(int index)
        {
            // 같은 슬롯 다시 클릭 → 선택 해제
            if (_selectedIndex == index)
            {
                ClearSelection();
                return;
            }

            // 이전 선택 슬롯이 있으면 두 슬롯 교환 시도
            if (_selectedIndex >= 0 && _selectedIndex != index)
            {
                var inventory = InventoryManager.Instance;
                if (inventory != null)
                {
                    inventory.SwapSlots(_selectedIndex, index);
                }
                ClearSelection();
                return;
            }

            // 새 선택
            ClearSelection();
            _selectedIndex = index;
            if (_slotUIs != null && index >= 0 && index < _slotUIs.Length)
                _slotUIs[index].SetSelected(true);
        }

        /// <summary>선택 해제</summary>
        public void ClearSelection()
        {
            if (_selectedIndex >= 0 && _slotUIs != null && _selectedIndex < _slotUIs.Length)
                _slotUIs[_selectedIndex].SetSelected(false);
            _selectedIndex = -1;
        }
    }
}
