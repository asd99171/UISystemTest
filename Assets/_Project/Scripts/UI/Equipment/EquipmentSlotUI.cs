using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPGSystem.Equipment;
using RPGSystem.Item;

namespace RPGSystem.UI
{
    /// <summary>
    /// 장비 슬롯 UI.
    /// 특정 EquipSlotType에 장착된 아이템을 표시한다.
    /// 각 슬롯은 Inspector에서 slotType을 설정하여 어느 장비 부위인지 지정한다.
    ///
    /// 상호작용:
    /// - 좌클릭: 슬롯 선택 (인벤토리에서 드래그할 대상 표시)
    /// - 우클릭: 장비 해제 (인벤토리로 반환)
    /// - 더블클릭: 장비 해제
    /// - 마우스 호버: 툴팁 표시
    ///
    /// [GameObject] 장비 패널 내 각 장비 슬롯 오브젝트에 부착.
    /// [Inspector] slotType으로 이 슬롯의 부위 설정 (Weapon, Helmet 등).
    /// </summary>
    public class EquipmentSlotUI : UISlotBase
    {
        [Header("장비 슬롯 설정")]
        [Tooltip("이 슬롯의 장비 부위 타입")]
        [SerializeField] private EquipSlotType slotType;

        [Tooltip("슬롯 타입 라벨 텍스트")]
        [SerializeField] private TMP_Text slotLabel;

        /// <summary>이 슬롯의 장비 타입</summary>
        public EquipSlotType SlotType => slotType;

        /// <summary>현재 장착된 아이템 참조</summary>
        private ItemInstance _equippedItem;

        /// <summary>소속 컨트롤러</summary>
        private EquipmentUIController _controller;

        // ──────────────────────────────────────
        // 설정
        // ──────────────────────────────────────

        public void SetController(EquipmentUIController controller)
        {
            _controller = controller;
        }

        private void Start()
        {
            if (slotLabel != null)
                slotLabel.text = GetSlotDisplayName(slotType);
        }

        // ──────────────────────────────────────
        // 데이터 업데이트
        // ──────────────────────────────────────

        /// <summary>
        /// EquipmentManager에서 현재 슬롯 데이터를 읽어와 표시.
        /// </summary>
        public void Refresh()
        {
            var equipment = EquipmentManager.Instance;
            if (equipment == null)
            {
                Clear();
                return;
            }

            var item = equipment.GetEquipped(slotType);
            if (item == null)
            {
                Clear();
                return;
            }

            _equippedItem = item;
            IsEmpty = false;
            SetIcon(item.data.icon);
            SetQuantity(null); // 장비는 수량 표시 없음
        }

        public override void Clear()
        {
            base.Clear();
            _equippedItem = null;
        }

        // ──────────────────────────────────────
        // 상호작용
        // ──────────────────────────────────────

        protected override void OnLeftClick()
        {
            _controller?.OnEquipSlotSelected(slotType);
        }

        protected override void OnRightClick()
        {
            if (IsEmpty) return;
            EquipmentManager.Instance?.Unequip(slotType);
        }

        protected override void OnDoubleClick()
        {
            if (IsEmpty) return;
            EquipmentManager.Instance?.Unequip(slotType);
        }

        protected override void OnHoverEnter()
        {
            if (IsEmpty || _equippedItem == null) return;
            TooltipUI.Instance?.ShowItem(_equippedItem);
        }

        protected override void OnHoverExit()
        {
            TooltipUI.Instance?.Hide();
        }

        // ──────────────────────────────────────
        // 유틸리티
        // ──────────────────────────────────────

        private static string GetSlotDisplayName(EquipSlotType type)
        {
            return type switch
            {
                EquipSlotType.Weapon  => "Weapon",
                EquipSlotType.OffHand => "Off Hand",
                EquipSlotType.Helmet  => "Helmet",
                EquipSlotType.Chest   => "Chest",
                EquipSlotType.Legs    => "Legs",
                EquipSlotType.Boots   => "Boots",
                EquipSlotType.Ring1   => "Ring 1",
                EquipSlotType.Ring2   => "Ring 2",
                _                     => type.ToString()
            };
        }
    }
}
