using System;
using System.Collections.Generic;
using UnityEngine;
using RPGSystem.Core;
using RPGSystem.Inventory;
using RPGSystem.Item;
using RPGSystem.Item.Data;

namespace RPGSystem.Equipment
{
    /// <summary>
    /// 장비 매니저.
    /// 8개 장비 슬롯을 관리하고, 장착/해제 로직을 처리한다.
    /// InventoryManager의 EquipRequestEvent를 구독하여 장착을 실행한다.
    ///
    /// [GameObject] "EquipmentManager" 빈 오브젝트 또는 InventoryManager와 같은 오브젝트에 부착.
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        /// <summary>장비 슬롯 딕셔너리</summary>
        private Dictionary<EquipSlotType, ItemInstance> _equippedItems;

        /// <summary>모든 슬롯 타입</summary>
        private static readonly EquipSlotType[] AllSlots = (EquipSlotType[])Enum.GetValues(typeof(EquipSlotType));

        /// <summary>특정 슬롯의 장착 아이템 조회 (없으면 null)</summary>
        public ItemInstance GetEquipped(EquipSlotType slot)
        {
            _equippedItems.TryGetValue(slot, out var item);
            return item;
        }

        /// <summary>특정 슬롯이 비어있는지</summary>
        public bool IsSlotEmpty(EquipSlotType slot)
        {
            return !_equippedItems.ContainsKey(slot) || _equippedItems[slot] == null;
        }

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

            _equippedItems = new Dictionary<EquipSlotType, ItemInstance>();
            foreach (var slot in AllSlots)
            {
                _equippedItems[slot] = null;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EquipRequestEvent>(OnEquipRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EquipRequestEvent>(OnEquipRequested);
        }

        // ──────────────────────────────────────
        // 장착 요청 처리
        // ──────────────────────────────────────

        private void OnEquipRequested(EquipRequestEvent evt)
        {
            var item = evt.item;
            if (item == null) return;

            EquipSlotType? targetSlot = ResolveSlot(item);
            if (!targetSlot.HasValue)
            {
                Debug.LogWarning($"[Equipment] Cannot determine slot for: {item.data.itemName}");
                return;
            }

            Equip(targetSlot.Value, item);
        }

        /// <summary>
        /// 아이템 데이터에서 장착할 슬롯 결정.
        /// </summary>
        private EquipSlotType? ResolveSlot(ItemInstance item)
        {
            switch (item.data)
            {
                case WeaponData:
                    return EquipSlotType.Weapon;

                case SubWeaponData:
                    return EquipSlotType.OffHand;

                case ArmorData armorData:
                    return armorData.equipSlot;

                default:
                    return null;
            }
        }

        // ──────────────────────────────────────
        // 장착 / 해제
        // ──────────────────────────────────────

        /// <summary>
        /// 아이템을 지정 슬롯에 장착.
        /// 기존 장비가 있으면 인벤토리로 반환한 후 장착.
        /// </summary>
        public bool Equip(EquipSlotType slot, ItemInstance item)
        {
            if (item == null) return false;

            var inventory = InventoryManager.Instance;
            if (inventory == null)
            {
                Debug.LogError("[Equipment] InventoryManager not found.");
                return false;
            }

            // 1) 인벤토리에서 해당 아이템 제거
            var (slotIndex, _) = inventory.FindItemByUid(item.uid);
            if (slotIndex >= 0)
            {
                inventory.RemoveItem(slotIndex);
            }

            // 2) 기존 장비가 있으면 인벤토리로 반환
            var currentEquip = _equippedItems[slot];
            if (currentEquip != null)
            {
                if (!inventory.AddItemInstance(currentEquip))
                {
                    // 인벤토리 가득 참 → 원래 아이템 복구
                    if (slotIndex >= 0)
                        inventory.AddItemInstance(item);
                    Debug.LogWarning("[Equipment] Inventory full, cannot unequip current item.");
                    return false;
                }
            }

            // 3) 장착
            _equippedItems[slot] = item;

            Debug.Log($"[Equipment] Equipped '{item.data.itemName}' to {slot}");
            EventBus.Publish(new EquipmentChangedEvent());
            return true;
        }

        /// <summary>
        /// 지정 슬롯의 장비 해제. 인벤토리로 반환.
        /// </summary>
        public bool Unequip(EquipSlotType slot)
        {
            if (IsSlotEmpty(slot))
                return false;

            var inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            var item = _equippedItems[slot];

            if (!inventory.AddItemInstance(item))
            {
                Debug.LogWarning("[Equipment] Inventory full, cannot unequip.");
                return false;
            }

            _equippedItems[slot] = null;

            Debug.Log($"[Equipment] Unequipped '{item.data.itemName}' from {slot}");
            EventBus.Publish(new EquipmentChangedEvent());
            return true;
        }

        // ──────────────────────────────────────
        // 디버그
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print Equipment")]
        public void DebugPrintEquipment()
        {
            Debug.Log("═══════ EQUIPMENT ═══════");
            foreach (var slot in AllSlots)
            {
                var item = _equippedItems[slot];
                string display = item != null ? $"{item.data.itemName} ({item.data.rarity})" : "(empty)";
                Debug.Log($"  {slot,-10} : {display}");
            }
            Debug.Log("═════════════════════════");
        }
    }
}
