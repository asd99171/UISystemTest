using System;
using UnityEngine;
using RPGSystem.Core;
using RPGSystem.Item;
using RPGSystem.Item.Data;

namespace RPGSystem.Inventory
{
    /// <summary>
    /// 인벤토리 핵심 매니저.
    /// 아이템 추가/제거/이동/사용/스택 처리 등 모든 인벤토리 로직을 담당한다.
    /// UI와 완전히 분리되어 있으며, EventBus로 변경 사항을 통지한다.
    ///
    /// [GameObject] "InventoryManager" 빈 오브젝트에 부착.
    /// [Inspector] slotCount, itemDatabase 설정.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("인벤토리 설정")]
        [Tooltip("인벤토리 슬롯 수")]
        [SerializeField] private int slotCount = 40;

        [Tooltip("아이템 데이터베이스")]
        [SerializeField] private ItemDatabase itemDatabase;

        /// <summary>인벤토리 슬롯 배열 (외부 읽기 전용)</summary>
        public InventorySlot[] Slots { get; private set; }

        /// <summary>현재 슬롯 수</summary>
        public int SlotCount => slotCount;

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

            InitializeSlots();
        }

        private void InitializeSlots()
        {
            Slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                Slots[i] = new InventorySlot(i);
            }
        }

        // ──────────────────────────────────────
        // 아이템 추가
        // ──────────────────────────────────────

        /// <summary>
        /// 아이템 추가. 스택 가능하면 기존 스택에 합치고, 아니면 빈 슬롯에 배치.
        /// 추가된 수량 반환. 0이면 인벤토리가 가득 찬 것.
        /// </summary>
        public int AddItem(ItemData itemData, int amount = 1)
        {
            if (itemData == null || amount <= 0)
                return 0;

            int remaining = amount;

            // 1) 스택 가능한 경우: 기존 동일 아이템 스택에 먼저 채우기
            if (itemData.isStackable)
            {
                remaining = FillExistingStacks(itemData, remaining);
            }

            // 2) 남은 수량을 빈 슬롯에 배치
            while (remaining > 0)
            {
                int emptyIndex = FindEmptySlotIndex();
                if (emptyIndex < 0)
                {
                    // 인벤토리 가득 참
                    EventBus.Publish(new InventoryFullEvent
                    {
                        attemptedItem = itemData,
                        attemptedAmount = remaining
                    });
                    break;
                }

                int toPlace = Mathf.Min(remaining, itemData.maxStack);
                var instance = new ItemInstance(itemData, toPlace);
                Slots[emptyIndex].SetItem(instance);
                remaining -= toPlace;

                NotifySlotChanged(emptyIndex);
            }

            int added = amount - remaining;
            if (added > 0)
            {
                Debug.Log($"[Inventory] Added {added}x {itemData.itemName}");
            }
            return added;
        }

        /// <summary>
        /// ItemInstance를 직접 특정 슬롯에 삽입 (장비 해제 시 사용).
        /// 성공 여부 반환.
        /// </summary>
        public bool AddItemInstance(ItemInstance item)
        {
            if (item == null) return false;

            // 스택 가능하면 기존 스택에 합치기 시도
            if (item.data.isStackable)
            {
                int remaining = FillExistingStacks(item.data, item.amount);
                if (remaining <= 0)
                    return true;
                item.amount = remaining;
            }

            int emptyIndex = FindEmptySlotIndex();
            if (emptyIndex < 0)
            {
                EventBus.Publish(new InventoryFullEvent
                {
                    attemptedItem = item.data,
                    attemptedAmount = item.amount
                });
                return false;
            }

            Slots[emptyIndex].SetItem(item);
            NotifySlotChanged(emptyIndex);
            return true;
        }

        /// <summary>기존 동일 아이템 스택에 수량 채우기. 남은 수량 반환.</summary>
        private int FillExistingStacks(ItemData itemData, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < slotCount && remaining > 0; i++)
            {
                var slot = Slots[i];
                if (slot.IsEmpty || slot.item.data != itemData || slot.item.IsStackFull)
                    continue;

                int canAdd = slot.item.StackableAmount;
                int toAdd = Mathf.Min(remaining, canAdd);
                slot.item.amount += toAdd;
                remaining -= toAdd;

                NotifySlotChanged(i);
            }
            return remaining;
        }

        // ──────────────────────────────────────
        // 아이템 제거
        // ──────────────────────────────────────

        /// <summary>
        /// 슬롯 인덱스로 아이템 제거. 제거된 아이템 반환.
        /// amount = 0이면 전체 제거, 양수이면 해당 수량만 제거.
        /// </summary>
        public ItemInstance RemoveItem(int slotIndex, int amount = 0)
        {
            if (!IsValidSlot(slotIndex) || Slots[slotIndex].IsEmpty)
                return null;

            var slot = Slots[slotIndex];
            var item = slot.item;

            // 중요 아이템은 버리기 불가
            if (item.data.itemType == ItemType.KeyItem)
            {
                Debug.LogWarning($"[Inventory] Cannot discard key item: {item.data.itemName}");
                return null;
            }

            // 전체 제거
            if (amount <= 0 || amount >= item.amount)
            {
                slot.Clear();
                NotifySlotChanged(slotIndex);
                Debug.Log($"[Inventory] Removed {item.amount}x {item.data.itemName}");
                return item;
            }

            // 부분 제거
            item.amount -= amount;
            var removed = item.Clone(amount);
            NotifySlotChanged(slotIndex);
            Debug.Log($"[Inventory] Removed {amount}x {item.data.itemName}");
            return removed;
        }

        /// <summary>
        /// ItemData 기준으로 제거. 여러 슬롯에 걸쳐 제거 가능.
        /// 실제 제거된 수량 반환.
        /// </summary>
        public int RemoveItemByData(ItemData itemData, int amount = 1)
        {
            int remaining = amount;
            for (int i = 0; i < slotCount && remaining > 0; i++)
            {
                var slot = Slots[i];
                if (slot.IsEmpty || slot.item.data != itemData)
                    continue;
                if (slot.item.data.itemType == ItemType.KeyItem)
                    continue;

                if (slot.item.amount <= remaining)
                {
                    remaining -= slot.item.amount;
                    slot.Clear();
                }
                else
                {
                    slot.item.amount -= remaining;
                    remaining = 0;
                }
                NotifySlotChanged(i);
            }
            return amount - remaining;
        }

        // ──────────────────────────────────────
        // 아이템 사용
        // ──────────────────────────────────────

        /// <summary>
        /// 슬롯 인덱스의 아이템 사용.
        /// - 소모품: 효과 적용 후 수량 감소
        /// - 장비류(무기/갑옷/보조무기): EquipRequestEvent 발행
        /// - 중요아이템: isUsable일 때만 이벤트 발행
        /// - 잼: 사용 불가 (스킬UI에서 장착)
        /// </summary>
        public bool UseItem(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || Slots[slotIndex].IsEmpty)
                return false;

            var slot = Slots[slotIndex];
            var item = slot.item;

            switch (item.data.itemType)
            {
                case ItemType.Consumable:
                    return UseConsumable(slotIndex);

                case ItemType.Weapon:
                case ItemType.Armor:
                case ItemType.SubWeapon:
                    RequestEquip(slotIndex);
                    return true;

                case ItemType.KeyItem:
                    return UseKeyItem(slotIndex);

                case ItemType.Gem:
                    Debug.Log($"[Inventory] Gem '{item.data.itemName}' must be socketed via Skill UI.");
                    return false;

                default:
                    return false;
            }
        }

        private bool UseConsumable(int slotIndex)
        {
            var item = Slots[slotIndex].item;

            // 사용 이벤트 발행 (실제 효과 적용은 구독자가 처리)
            EventBus.Publish(new ItemUsedEvent { item = item });

            Debug.Log($"[Inventory] Used consumable: {item.data.itemName}");

            // 수량 감소
            item.amount--;
            if (item.amount <= 0)
                Slots[slotIndex].Clear();

            NotifySlotChanged(slotIndex);
            return true;
        }

        private void RequestEquip(int slotIndex)
        {
            var item = Slots[slotIndex].item;
            Debug.Log($"[Inventory] Equip requested: {item.data.itemName}");
            EventBus.Publish(new EquipRequestEvent { item = item });
        }

        private bool UseKeyItem(int slotIndex)
        {
            var item = Slots[slotIndex].item;
            var keyData = item.data as KeyItemData;

            if (keyData == null || !keyData.isUsable)
            {
                Debug.Log($"[Inventory] Key item '{item.data.itemName}' is not usable.");
                return false;
            }

            EventBus.Publish(new ItemUsedEvent { item = item });
            Debug.Log($"[Inventory] Used key item: {item.data.itemName}");
            return true;
        }

        // ──────────────────────────────────────
        // 슬롯 이동 / 교환
        // ──────────────────────────────────────

        /// <summary>
        /// 두 슬롯의 아이템을 교환한다.
        /// 동일 아이템이면 스택을 합친다.
        /// </summary>
        public bool SwapSlots(int fromIndex, int toIndex)
        {
            if (!IsValidSlot(fromIndex) || !IsValidSlot(toIndex))
                return false;
            if (fromIndex == toIndex)
                return false;

            var fromSlot = Slots[fromIndex];
            var toSlot = Slots[toIndex];

            // 잠긴 슬롯 체크
            if (fromSlot.isLocked || toSlot.isLocked)
                return false;

            // 대상이 비어있으면 단순 이동
            if (toSlot.IsEmpty)
            {
                toSlot.SetItem(fromSlot.item);
                fromSlot.Clear();
                NotifySlotChanged(fromIndex);
                NotifySlotChanged(toIndex);
                return true;
            }

            // 동일 스택 가능 아이템이면 합치기
            if (!fromSlot.IsEmpty && !toSlot.IsEmpty
                && fromSlot.item.data == toSlot.item.data
                && fromSlot.item.data.isStackable
                && !toSlot.item.IsStackFull)
            {
                return MergeStacks(fromIndex, toIndex);
            }

            // 교환
            var temp = fromSlot.item;
            fromSlot.SetItem(toSlot.item);
            toSlot.SetItem(temp);
            NotifySlotChanged(fromIndex);
            NotifySlotChanged(toIndex);
            return true;
        }

        /// <summary>
        /// fromSlot의 아이템을 toSlot에 스택 합치기.
        /// 넘치면 from에 남은 수량이 남는다.
        /// </summary>
        private bool MergeStacks(int fromIndex, int toIndex)
        {
            var fromItem = Slots[fromIndex].item;
            var toItem = Slots[toIndex].item;

            int canAdd = toItem.StackableAmount;
            int toMove = Mathf.Min(fromItem.amount, canAdd);

            toItem.amount += toMove;
            fromItem.amount -= toMove;

            if (fromItem.amount <= 0)
                Slots[fromIndex].Clear();

            NotifySlotChanged(fromIndex);
            NotifySlotChanged(toIndex);
            return true;
        }

        // ──────────────────────────────────────
        // 조회
        // ──────────────────────────────────────

        /// <summary>첫 번째 빈 슬롯 인덱스 반환. 없으면 -1.</summary>
        public int FindEmptySlotIndex()
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (Slots[i].IsEmpty && !Slots[i].isLocked)
                    return i;
            }
            return -1;
        }

        /// <summary>특정 아이템의 총 보유 수량</summary>
        public int GetItemCount(ItemData itemData)
        {
            int count = 0;
            for (int i = 0; i < slotCount; i++)
            {
                if (!Slots[i].IsEmpty && Slots[i].item.data == itemData)
                    count += Slots[i].item.amount;
            }
            return count;
        }

        /// <summary>특정 아이템을 보유하고 있는지</summary>
        public bool HasItem(ItemData itemData, int requiredAmount = 1)
        {
            return GetItemCount(itemData) >= requiredAmount;
        }

        /// <summary>특정 아이템이 있는 첫 번째 슬롯 인덱스. 없으면 -1.</summary>
        public int FindItemSlotIndex(ItemData itemData)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (!Slots[i].IsEmpty && Slots[i].item.data == itemData)
                    return i;
            }
            return -1;
        }

        /// <summary>uid로 아이템 찾기. 없으면 (-1, null).</summary>
        public (int slotIndex, ItemInstance item) FindItemByUid(string uid)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (!Slots[i].IsEmpty && Slots[i].item.uid == uid)
                    return (i, Slots[i].item);
            }
            return (-1, null);
        }

        /// <summary>인벤토리가 가득 찼는지</summary>
        public bool IsFull => FindEmptySlotIndex() < 0;

        // ──────────────────────────────────────
        // 유틸리티
        // ──────────────────────────────────────

        private bool IsValidSlot(int index) => index >= 0 && index < slotCount;

        private void NotifySlotChanged(int slotIndex)
        {
            EventBus.Publish(new InventoryChangedEvent
            {
                slotIndex = slotIndex,
                slot = Slots[slotIndex]
            });
        }

        /// <summary>전체 인벤토리 갱신 이벤트 (정렬 후 등)</summary>
        private void NotifyFullRefresh()
        {
            EventBus.Publish(new InventoryRefreshEvent());
        }

        /// <summary>인벤토리 정렬 (타입 → 등급 → 이름)</summary>
        public void SortInventory()
        {
            // 아이템만 추출
            var items = new System.Collections.Generic.List<ItemInstance>();
            for (int i = 0; i < slotCount; i++)
            {
                if (!Slots[i].IsEmpty)
                {
                    items.Add(Slots[i].item);
                    Slots[i].Clear();
                }
            }

            // 정렬: 타입 → 등급(역순) → 이름
            items.Sort((a, b) =>
            {
                int typeCompare = a.data.itemType.CompareTo(b.data.itemType);
                if (typeCompare != 0) return typeCompare;
                int rarityCompare = b.data.rarity.CompareTo(a.data.rarity);
                if (rarityCompare != 0) return rarityCompare;
                return string.Compare(a.data.itemName, b.data.itemName, StringComparison.Ordinal);
            });

            // 재배치
            for (int i = 0; i < items.Count; i++)
            {
                Slots[i].SetItem(items[i]);
            }

            NotifyFullRefresh();
            Debug.Log("[Inventory] Sorted.");
        }

        // ──────────────────────────────────────
        // 디버그 / ContextMenu
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print Inventory")]
        public void DebugPrintInventory()
        {
            Debug.Log("═══════ INVENTORY ═══════");
            for (int i = 0; i < slotCount; i++)
            {
                var slot = Slots[i];
                if (!slot.IsEmpty)
                {
                    var item = slot.item;
                    Debug.Log($"  [{i:D2}] {item.data.itemName} x{item.amount} " +
                              $"({item.data.itemType}, {item.data.rarity}) uid:{item.uid[..8]}");
                }
            }
            Debug.Log("═════════════════════════");
        }

        [ContextMenu("Debug: Sort Inventory")]
        public void DebugSortInventory()
        {
            SortInventory();
        }
    }
}
