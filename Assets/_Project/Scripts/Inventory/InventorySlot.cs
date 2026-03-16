using System;
using RPGSystem.Item;

namespace RPGSystem.Inventory
{
    /// <summary>
    /// 인벤토리의 슬롯 하나를 표현하는 데이터.
    /// 비어있을 수 있고(item == null), 아이템이 들어있을 수 있다.
    /// UI와 직접 1:1 매핑된다.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        /// <summary>이 슬롯에 들어있는 아이템 (비어있으면 null)</summary>
        public ItemInstance item;

        /// <summary>슬롯 인덱스 (인벤토리 내 위치)</summary>
        public int slotIndex;

        /// <summary>슬롯이 잠겨있는지 (확장 가능한 인벤토리용)</summary>
        public bool isLocked;

        public InventorySlot(int index)
        {
            slotIndex = index;
            item = null;
            isLocked = false;
        }

        /// <summary>슬롯이 비어있는지</summary>
        public bool IsEmpty => item == null;

        /// <summary>슬롯 비우기</summary>
        public void Clear()
        {
            item = null;
        }

        /// <summary>아이템 설정</summary>
        public void SetItem(ItemInstance newItem)
        {
            item = newItem;
        }
    }
}
