using System;
using System.Collections.Generic;
using UnityEngine;
using RPGSystem.Core;
using RPGSystem.Inventory;
using RPGSystem.Item;
using RPGSystem.Item.Data;
using RPGSystem.Stat;

namespace RPGSystem.Equipment
{
    /// <summary>
    /// 장비 매니저.
    /// 8개 장비 슬롯(무기, 다른손, 투구, 갑옷, 바지, 신발, 반지1, 반지2)을 관리한다.
    ///
    /// 핵심 로직:
    /// - 슬롯 타입 검증 (무기→Weapon, 갑옷→equipSlot 매칭)
    /// - 반지 2슬롯 자동 배치 (Ring1 비었으면 Ring1, 아니면 Ring2)
    /// - 장착/해제 시 인벤토리 연동
    /// - 장비 변경 시 EquipmentChangedEvent 발행 → PlayerStatManager가 재계산
    ///
    /// [GameObject] "SystemManager" 오브젝트에 부착 (InventoryManager와 동일 오브젝트 가능).
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        public static EquipmentManager Instance { get; private set; }

        /// <summary>장비 슬롯 딕셔너리</summary>
        private Dictionary<EquipSlotType, ItemInstance> _equippedItems;

        /// <summary>모든 슬롯 타입 (순회용 캐시)</summary>
        public static readonly EquipSlotType[] AllSlots =
            (EquipSlotType[])Enum.GetValues(typeof(EquipSlotType));

        // ──────────────────────────────────────
        // Public 조회
        // ──────────────────────────────────────

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

        /// <summary>장착 중인 모든 아이템의 StatModifier 수집</summary>
        public List<StatModifier> GetAllEquipmentModifiers()
        {
            var modifiers = new List<StatModifier>();

            foreach (var kvp in _equippedItems)
            {
                var item = kvp.Value;
                if (item == null) continue;

                switch (item.data)
                {
                    case WeaponData weapon:
                        // 무기 기본 공격력을 ATK Flat으로 변환
                        modifiers.Add(new StatModifier(StatType.ATK, ModifierType.Flat, weapon.attackPower));
                        modifiers.Add(new StatModifier(StatType.AttackSpeed, ModifierType.Flat, weapon.attackSpeed));
                        AddModifiers(modifiers, weapon.statModifiers);
                        break;

                    case ArmorData armor:
                        // 방어구 기본 방어력을 DEF Flat으로 변환
                        modifiers.Add(new StatModifier(StatType.DEF, ModifierType.Flat, armor.defense));
                        AddModifiers(modifiers, armor.statModifiers);
                        break;

                    case SubWeaponData subWeapon:
                        if (subWeapon.defense > 0)
                            modifiers.Add(new StatModifier(StatType.DEF, ModifierType.Flat, subWeapon.defense));
                        if (subWeapon.attackPower > 0)
                            modifiers.Add(new StatModifier(StatType.ATK, ModifierType.Flat, subWeapon.attackPower));
                        AddModifiers(modifiers, subWeapon.statModifiers);
                        break;
                }
            }

            return modifiers;
        }

        private void AddModifiers(List<StatModifier> list, StatModifier[] modifiers)
        {
            if (modifiers != null)
                list.AddRange(modifiers);
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
        // 장착 요청 처리 (인벤토리에서 사용 시)
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

        // ──────────────────────────────────────
        // 슬롯 결정 로직
        // ──────────────────────────────────────

        /// <summary>
        /// 아이템 데이터에서 장착할 슬롯을 결정한다.
        ///
        /// 무기(WeaponData) → Weapon 슬롯
        /// 보조무기(SubWeaponData) → OffHand 슬롯
        /// 갑옷(ArmorData) → equipSlot 필드에 지정된 슬롯
        ///   - Helmet, Chest, Legs, Boots: 해당 슬롯 그대로
        ///   - Ring1/Ring2: 반지 자동 배치 로직 (빈 슬롯 우선)
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
                    return ResolveArmorSlot(armorData);

                default:
                    return null;
            }
        }

        /// <summary>
        /// 방어구의 슬롯 결정.
        /// 반지인 경우: Ring1이 비어있으면 Ring1, 아니면 Ring2.
        /// </summary>
        private EquipSlotType ResolveArmorSlot(ArmorData armorData)
        {
            // 반지 슬롯 특수 처리
            if (armorData.equipSlot == EquipSlotType.Ring1
                || armorData.equipSlot == EquipSlotType.Ring2)
            {
                return ResolveRingSlot();
            }

            return armorData.equipSlot;
        }

        /// <summary>
        /// 반지 슬롯 자동 결정.
        /// 1) Ring1이 비어있으면 → Ring1
        /// 2) Ring2가 비어있으면 → Ring2
        /// 3) 둘 다 차있으면 → Ring1 교체 (기존 Ring1은 인벤토리로)
        /// </summary>
        private EquipSlotType ResolveRingSlot()
        {
            if (IsSlotEmpty(EquipSlotType.Ring1))
                return EquipSlotType.Ring1;
            if (IsSlotEmpty(EquipSlotType.Ring2))
                return EquipSlotType.Ring2;
            return EquipSlotType.Ring1; // 둘 다 차있으면 Ring1 교체
        }

        // ──────────────────────────────────────
        // 슬롯 타입 검증
        // ──────────────────────────────────────

        /// <summary>
        /// 아이템이 지정 슬롯에 장착 가능한지 검증.
        /// </summary>
        public bool CanEquipToSlot(EquipSlotType slot, ItemInstance item)
        {
            if (item == null) return false;

            switch (item.data)
            {
                case WeaponData:
                    return slot == EquipSlotType.Weapon;

                case SubWeaponData:
                    return slot == EquipSlotType.OffHand;

                case ArmorData armorData:
                    return CanArmorFitSlot(slot, armorData);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 방어구가 특정 슬롯에 맞는지 검증.
        /// 반지는 Ring1, Ring2 둘 다 허용.
        /// </summary>
        private bool CanArmorFitSlot(EquipSlotType slot, ArmorData armorData)
        {
            var itemSlot = armorData.equipSlot;

            // 반지: Ring1 또는 Ring2 둘 다 가능
            if (itemSlot == EquipSlotType.Ring1 || itemSlot == EquipSlotType.Ring2)
                return slot == EquipSlotType.Ring1 || slot == EquipSlotType.Ring2;

            // 나머지: 정확히 매칭
            return slot == itemSlot;
        }

        // ──────────────────────────────────────
        // 장착
        // ──────────────────────────────────────

        /// <summary>
        /// 아이템을 지정 슬롯에 장착.
        ///
        /// 처리 순서:
        /// 1) 슬롯 타입 검증
        /// 2) 인벤토리에서 아이템 제거
        /// 3) 기존 장비 인벤토리로 반환
        /// 4) 새 아이템 장착
        /// 5) EquipmentChangedEvent 발행 → PlayerStatManager 재계산
        /// </summary>
        public bool Equip(EquipSlotType slot, ItemInstance item)
        {
            if (item == null) return false;

            // 1) 슬롯 타입 검증
            if (!CanEquipToSlot(slot, item))
            {
                Debug.LogWarning($"[Equipment] '{item.data.itemName}' cannot be equipped to {slot}");
                return false;
            }

            var inventory = InventoryManager.Instance;
            if (inventory == null)
            {
                Debug.LogError("[Equipment] InventoryManager not found.");
                return false;
            }

            // 2) 인벤토리에서 해당 아이템 제거
            var (invSlotIndex, _) = inventory.FindItemByUid(item.uid);
            if (invSlotIndex >= 0)
            {
                inventory.RemoveItem(invSlotIndex);
            }

            // 3) 기존 장비가 있으면 인벤토리로 반환
            var currentEquip = _equippedItems[slot];
            if (currentEquip != null)
            {
                if (!inventory.AddItemInstance(currentEquip))
                {
                    // 인벤토리 가득 참 → 원래 아이템 복구
                    if (invSlotIndex >= 0)
                        inventory.AddItemInstance(item);
                    Debug.LogWarning("[Equipment] Inventory full, cannot swap equipment.");
                    return false;
                }
                Debug.Log($"[Equipment] Returned '{currentEquip.data.itemName}' to inventory");
            }

            // 4) 장착
            _equippedItems[slot] = item;
            Debug.Log($"[Equipment] Equipped '{item.data.itemName}' → {slot}");

            // 5) 이벤트 발행 → PlayerStatManager가 구독하여 재계산
            EventBus.Publish(new EquipmentChangedEvent());
            return true;
        }

        // ──────────────────────────────────────
        // 해제
        // ──────────────────────────────────────

        /// <summary>
        /// 지정 슬롯의 장비 해제. 인벤토리로 반환.
        /// </summary>
        public bool Unequip(EquipSlotType slot)
        {
            if (IsSlotEmpty(slot))
            {
                Debug.Log($"[Equipment] {slot} is already empty.");
                return false;
            }

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

        /// <summary>
        /// 모든 장비 해제
        /// </summary>
        public void UnequipAll()
        {
            foreach (var slot in AllSlots)
            {
                if (!IsSlotEmpty(slot))
                    Unequip(slot);
            }
        }

        // ──────────────────────────────────────
        // 특정 슬롯에 직접 드래그 장착 (UI용)
        // ──────────────────────────────────────

        /// <summary>
        /// UI 드래그 앤 드롭 시 호출.
        /// 인벤토리 슬롯 → 장비 슬롯으로 직접 장착.
        /// </summary>
        public bool EquipFromInventorySlot(int inventorySlotIndex, EquipSlotType targetSlot)
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            var invSlot = inventory.Slots[inventorySlotIndex];
            if (invSlot.IsEmpty) return false;

            return Equip(targetSlot, invSlot.item);
        }

        // ──────────────────────────────────────
        // 디버그
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print Equipment")]
        public void DebugPrintEquipment()
        {
            Debug.Log("═══════════ EQUIPMENT ═══════════");
            foreach (var slot in AllSlots)
            {
                var item = _equippedItems[slot];
                if (item != null)
                {
                    string stats = GetSlotStatSummary(item);
                    Debug.Log($"  {slot,-10} │ {item.data.itemName} ({item.data.rarity}) {stats}");
                }
                else
                {
                    Debug.Log($"  {slot,-10} │ (empty)");
                }
            }
            Debug.Log("═════════════════════════════════");
        }

        private string GetSlotStatSummary(ItemInstance item)
        {
            switch (item.data)
            {
                case WeaponData w: return $"[ATK:{w.attackPower} SPD:{w.attackSpeed}]";
                case ArmorData a: return $"[DEF:{a.defense}]";
                case SubWeaponData s: return $"[ATK:{s.attackPower} DEF:{s.defense}]";
                default: return "";
            }
        }

        [ContextMenu("Debug: Print Equipment Modifiers")]
        public void DebugPrintModifiers()
        {
            var mods = GetAllEquipmentModifiers();
            Debug.Log($"═══════ EQUIPMENT MODIFIERS ({mods.Count}) ═══════");
            foreach (var mod in mods)
            {
                Debug.Log($"  {mod.ToDisplayString()}");
            }
            Debug.Log("═════════════════════════════════════");
        }

        [ContextMenu("Debug: Unequip All")]
        public void DebugUnequipAll()
        {
            UnequipAll();
        }
    }
}
