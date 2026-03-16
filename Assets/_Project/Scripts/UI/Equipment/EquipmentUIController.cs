using UnityEngine;
using TMPro;
using RPGSystem.Core;
using RPGSystem.Equipment;
using RPGSystem.Stat;

namespace RPGSystem.UI
{
    /// <summary>
    /// 장비 UI 컨트롤러.
    /// 8개의 고정 장비 슬롯과 스탯 요약을 표시한다.
    /// EquipmentManager / PlayerStatManager의 데이터를 읽어와 표시만 한다.
    ///
    /// [GameObject] 장비 패널(UIPanel)과 동일 오브젝트에 부착.
    /// [Inspector] equipmentSlots 배열에 씬의 8개 EquipmentSlotUI를 등록.
    /// [Inspector] statTexts에 스탯 표시 TMP_Text들을 연결 (선택).
    /// </summary>
    public class EquipmentUIController : UIPanel
    {
        [Header("장비 슬롯")]
        [Tooltip("씬에 배치된 8개의 EquipmentSlotUI (Weapon, OffHand, Helmet, Chest, Legs, Boots, Ring1, Ring2)")]
        [SerializeField] private EquipmentSlotUI[] equipmentSlots;

        [Header("스탯 표시 (선택)")]
        [Tooltip("스탯 요약 텍스트 (전체 스탯을 한 텍스트에 표시)")]
        [SerializeField] private TMP_Text statSummaryText;

        /// <summary>현재 선택된 장비 슬롯 타입</summary>
        private EquipSlotType? _selectedSlotType;

        // ──────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────

        private void Start()
        {
            InitializeSlots();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);
            EventBus.Subscribe<StatChangedEvent>(OnStatChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EquipmentChangedEvent>(OnEquipmentChanged);
            EventBus.Unsubscribe<StatChangedEvent>(OnStatChanged);
        }

        // ──────────────────────────────────────
        // 초기화
        // ──────────────────────────────────────

        private void InitializeSlots()
        {
            if (equipmentSlots == null) return;

            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (equipmentSlots[i] != null)
                {
                    equipmentSlots[i].Initialize(i);
                    equipmentSlots[i].SetController(this);
                }
            }
        }

        // ──────────────────────────────────────
        // 패널 열기/닫기
        // ──────────────────────────────────────

        protected override void OnOpened()
        {
            RefreshAll();
            RefreshStats();
            Debug.Log("[EquipmentUI] Opened");
        }

        protected override void OnClosed()
        {
            ClearSelection();
            TooltipUI.Instance?.Hide();
            Debug.Log("[EquipmentUI] Closed");
        }

        // ──────────────────────────────────────
        // 갱신
        // ──────────────────────────────────────

        /// <summary>모든 장비 슬롯 UI 갱신</summary>
        public void RefreshAll()
        {
            if (equipmentSlots == null) return;

            foreach (var slot in equipmentSlots)
            {
                slot?.Refresh();
            }
        }

        /// <summary>스탯 요약 텍스트 갱신</summary>
        public void RefreshStats()
        {
            if (statSummaryText == null) return;

            var statMgr = PlayerStatManager.Instance;
            if (statMgr == null)
            {
                statSummaryText.text = "No stat data";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (StatType statType in System.Enum.GetValues(typeof(StatType)))
            {
                float baseVal = statMgr.GetBaseStat(statType);
                float finalVal = statMgr.GetStat(statType);
                float diff = finalVal - baseVal;

                sb.Append($"{statType}: {finalVal:0.##}");
                if (diff > 0)
                    sb.Append($" <color=#00FF00>(+{diff:0.##})</color>");
                else if (diff < 0)
                    sb.Append($" <color=#FF0000>({diff:0.##})</color>");
                sb.AppendLine();
            }

            statSummaryText.text = sb.ToString().TrimEnd();
        }

        // ──────────────────────────────────────
        // 이벤트 핸들러
        // ──────────────────────────────────────

        private void OnEquipmentChanged(EquipmentChangedEvent evt)
        {
            if (!IsOpen) return;
            RefreshAll();
        }

        private void OnStatChanged(StatChangedEvent evt)
        {
            if (!IsOpen) return;
            RefreshStats();
        }

        // ──────────────────────────────────────
        // 선택
        // ──────────────────────────────────────

        /// <summary>장비 슬롯이 선택됨 (EquipmentSlotUI가 호출)</summary>
        public void OnEquipSlotSelected(EquipSlotType slotType)
        {
            if (_selectedSlotType.HasValue && _selectedSlotType.Value == slotType)
            {
                ClearSelection();
                return;
            }

            ClearSelection();
            _selectedSlotType = slotType;

            // 해당 슬롯 하이라이트
            foreach (var slot in equipmentSlots)
            {
                if (slot != null && slot.SlotType == slotType)
                {
                    slot.SetSelected(true);
                    break;
                }
            }
        }

        /// <summary>선택 해제</summary>
        public void ClearSelection()
        {
            if (equipmentSlots == null) return;

            foreach (var slot in equipmentSlots)
            {
                slot?.SetSelected(false);
            }
            _selectedSlotType = null;
        }
    }
}
