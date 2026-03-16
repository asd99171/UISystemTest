using UnityEngine;
using RPGSystem.Inventory;
using RPGSystem.Item;

namespace RPGSystem.UI
{
    /// <summary>
    /// 인벤토리 슬롯 UI.
    /// InventorySlot의 데이터를 표시하고 사용자 상호작용을 처리한다.
    ///
    /// 상호작용:
    /// - 좌클릭: 슬롯 선택
    /// - 우클릭: 아이템 사용 (소모품 사용 / 장비 장착 요청)
    /// - 더블클릭: 아이템 사용 (우클릭과 동일)
    /// - 마우스 호버: 툴팁 표시
    ///
    /// [GameObject] 인벤토리 슬롯 Prefab에 부착.
    /// UISlotBase의 iconImage, quantityText, selectionFrame을 Inspector에서 연결.
    /// </summary>
    public class InventorySlotUI : UISlotBase
    {
        /// <summary>현재 표시 중인 아이템 인스턴스 (참조만, 저장 안 함)</summary>
        private ItemInstance _currentItem;

        /// <summary>소속 컨트롤러 참조</summary>
        private InventoryUIController _controller;

        // ──────────────────────────────────────
        // 설정
        // ──────────────────────────────────────

        /// <summary>컨트롤러 참조 설정</summary>
        public void SetController(InventoryUIController controller)
        {
            _controller = controller;
        }

        // ──────────────────────────────────────
        // 데이터 업데이트
        // ──────────────────────────────────────

        /// <summary>
        /// InventorySlot 데이터로 슬롯 UI 갱신.
        /// Manager 데이터를 읽기만 하고 저장하지 않는다.
        /// </summary>
        public void Refresh(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
                Clear();
                return;
            }

            _currentItem = slot.item;
            IsEmpty = false;

            SetIcon(_currentItem.data.icon);

            // 스택 가능 + 2개 이상일 때만 수량 표시
            if (_currentItem.data.isStackable && _currentItem.amount > 1)
                SetQuantity(_currentItem.amount.ToString());
            else
                SetQuantity(null);
        }

        public override void Clear()
        {
            base.Clear();
            _currentItem = null;
        }

        // ──────────────────────────────────────
        // 상호작용
        // ──────────────────────────────────────

        protected override void OnLeftClick()
        {
            _controller?.OnSlotSelected(SlotIndex);
        }

        protected override void OnRightClick()
        {
            if (IsEmpty) return;
            InventoryManager.Instance?.UseItem(SlotIndex);
        }

        protected override void OnDoubleClick()
        {
            if (IsEmpty) return;
            InventoryManager.Instance?.UseItem(SlotIndex);
        }

        protected override void OnHoverEnter()
        {
            if (IsEmpty || _currentItem == null) return;
            TooltipUI.Instance?.ShowItem(_currentItem);
        }

        protected override void OnHoverExit()
        {
            TooltipUI.Instance?.Hide();
        }
    }
}
