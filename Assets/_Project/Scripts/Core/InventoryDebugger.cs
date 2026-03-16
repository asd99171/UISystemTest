using UnityEngine;
using RPGSystem.Equipment;
using RPGSystem.Inventory;
using RPGSystem.Item.Data;
using RPGSystem.Stat;

namespace RPGSystem.Core
{
    /// <summary>
    /// 인벤토리 + 장비 + 스탯 통합 테스트 컴포넌트.
    /// Inspector에서 테스트 아이템을 등록하고 ContextMenu / 키보드로 테스트한다.
    ///
    /// [GameObject] "InventoryDebugger" 빈 오브젝트에 부착.
    /// [Inspector] testItems 배열에 SO 아이템들 등록.
    /// </summary>
    public class InventoryDebugger : MonoBehaviour
    {
        [Header("테스트 아이템 (Inspector에서 SO 드래그)")]
        [SerializeField] private ItemData[] testItems;

        [Header("키 바인딩")]
        [SerializeField] private KeyCode addItemKey = KeyCode.F1;
        [SerializeField] private KeyCode removeItemKey = KeyCode.F2;
        [SerializeField] private KeyCode useItemKey = KeyCode.F3;
        [SerializeField] private KeyCode printKey = KeyCode.F4;
        [SerializeField] private KeyCode sortKey = KeyCode.F5;
        [SerializeField] private KeyCode printEquipKey = KeyCode.F6;
        [SerializeField] private KeyCode printStatsKey = KeyCode.F7;

        [Header("설정")]
        [SerializeField] private int addAmount = 1;
        [SerializeField] private int testSlotIndex = 0;

        private int _currentTestItemIndex;

        private void Update()
        {
            if (Input.GetKeyDown(addItemKey))    DebugAddCurrentItem();
            if (Input.GetKeyDown(removeItemKey)) DebugRemoveFromSlot();
            if (Input.GetKeyDown(useItemKey))    DebugUseFromSlot();
            if (Input.GetKeyDown(printKey))      DebugPrint();
            if (Input.GetKeyDown(sortKey))       DebugSort();
            if (Input.GetKeyDown(printEquipKey)) DebugPrintEquipment();
            if (Input.GetKeyDown(printStatsKey)) DebugPrintStats();

            // 숫자키 1~9로 테스트 아이템 선택
            for (int i = 0; i < 9 && i < testItems.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _currentTestItemIndex = i;
                    Debug.Log($"[Debug] Selected: {testItems[i].itemName} ({testItems[i].itemType})");
                }
            }
        }

        // ──────────────────────────────────────
        // 인벤토리 테스트
        // ──────────────────────────────────────

        [ContextMenu("Debug: Add Current Test Item")]
        public void DebugAddCurrentItem()
        {
            if (testItems == null || testItems.Length == 0)
            {
                Debug.LogWarning("[Debug] No test items configured!");
                return;
            }
            var item = testItems[_currentTestItemIndex % testItems.Length];
            InventoryManager.Instance.AddItem(item, addAmount);
        }

        [ContextMenu("Debug: Add All Test Items")]
        public void DebugAddAllItems()
        {
            if (testItems == null) return;
            foreach (var item in testItems)
            {
                if (item != null)
                    InventoryManager.Instance.AddItem(item, addAmount);
            }
        }

        [ContextMenu("Debug: Remove From Test Slot")]
        public void DebugRemoveFromSlot()
        {
            InventoryManager.Instance.RemoveItem(testSlotIndex, 1);
        }

        [ContextMenu("Debug: Use From Test Slot (Equip if Equipment)")]
        public void DebugUseFromSlot()
        {
            InventoryManager.Instance.UseItem(testSlotIndex);
        }

        [ContextMenu("Debug: Print Inventory")]
        public void DebugPrint()
        {
            InventoryManager.Instance.DebugPrintInventory();
        }

        [ContextMenu("Debug: Sort")]
        public void DebugSort()
        {
            InventoryManager.Instance.SortInventory();
        }

        [ContextMenu("Debug: Swap Slots 0↔1")]
        public void DebugSwapSlots()
        {
            InventoryManager.Instance.SwapSlots(0, 1);
            Debug.Log("[Debug] Swapped slots 0 and 1");
        }

        [ContextMenu("Debug: Fill Inventory")]
        public void DebugFillInventory()
        {
            if (testItems == null || testItems.Length == 0) return;
            var inv = InventoryManager.Instance;
            int idx = 0;
            while (!inv.IsFull)
            {
                var item = testItems[idx % testItems.Length];
                if (inv.AddItem(item, 1) == 0) break;
                idx++;
            }
            Debug.Log("[Debug] Inventory filled.");
        }

        // ──────────────────────────────────────
        // 장비 테스트
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print Equipment")]
        public void DebugPrintEquipment()
        {
            if (EquipmentManager.Instance != null)
                EquipmentManager.Instance.DebugPrintEquipment();
        }

        [ContextMenu("Debug: Unequip All")]
        public void DebugUnequipAll()
        {
            if (EquipmentManager.Instance != null)
                EquipmentManager.Instance.UnequipAll();
        }

        [ContextMenu("Debug: Equip Test - Add Weapon + Armor + Use")]
        public void DebugEquipTest()
        {
            Debug.Log("═══════ EQUIP TEST START ═══════");

            var inv = InventoryManager.Instance;
            foreach (var item in testItems)
            {
                if (item == null) continue;
                if (item.itemType == ItemType.Weapon
                    || item.itemType == ItemType.Armor
                    || item.itemType == ItemType.SubWeapon)
                {
                    inv.AddItem(item, 1);
                }
            }

            // 인벤토리의 장비를 순서대로 사용(장착)
            for (int i = 0; i < inv.SlotCount; i++)
            {
                var slot = inv.Slots[i];
                if (slot.IsEmpty) continue;
                var data = slot.item.data;
                if (data.itemType == ItemType.Weapon
                    || data.itemType == ItemType.Armor
                    || data.itemType == ItemType.SubWeapon)
                {
                    inv.UseItem(i);
                }
            }

            Debug.Log("═══════ EQUIP TEST END ═══════");

            DebugPrintEquipment();
            DebugPrintStats();
        }

        [ContextMenu("Debug: Ring Slot Test - Add 3 Rings")]
        public void DebugRingTest()
        {
            Debug.Log("═══════ RING TEST START ═══════");
            Debug.Log("Adding 3 rings: Ring1 빈→Ring1, Ring2 빈→Ring2, 둘다 참→Ring1 교체");

            var inv = InventoryManager.Instance;
            int ringCount = 0;

            foreach (var item in testItems)
            {
                if (item == null) continue;
                if (item is ArmorData armor
                    && (armor.equipSlot == EquipSlotType.Ring1
                        || armor.equipSlot == EquipSlotType.Ring2))
                {
                    inv.AddItem(item, 1);
                    ringCount++;
                    if (ringCount >= 3) break;
                }
            }

            // 인벤토리의 반지를 순서대로 사용(장착)
            for (int i = 0; i < inv.SlotCount; i++)
            {
                var slot = inv.Slots[i];
                if (slot.IsEmpty) continue;
                if (slot.item.data is ArmorData a
                    && (a.equipSlot == EquipSlotType.Ring1
                        || a.equipSlot == EquipSlotType.Ring2))
                {
                    Debug.Log($"  Using ring: {slot.item.data.itemName}");
                    inv.UseItem(i);
                }
            }

            Debug.Log("═══════ RING TEST END ═══════");
            DebugPrintEquipment();
        }

        // ──────────────────────────────────────
        // 스탯 테스트
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print Stats")]
        public void DebugPrintStats()
        {
            if (PlayerStatManager.Instance != null)
                PlayerStatManager.Instance.DebugPrintStats();
        }

        [ContextMenu("Debug: Full Flow Test (Add → Equip → Stats)")]
        public void DebugFullFlowTest()
        {
            Debug.Log("╔═══════════════════════════════════════╗");
            Debug.Log("║       FULL FLOW TEST                  ║");
            Debug.Log("╚═══════════════════════════════════════╝");

            // 1) 스탯 before
            Debug.Log("── [1] BEFORE equipping ──");
            DebugPrintStats();

            // 2) 모든 장비 아이템 추가 + 장착
            Debug.Log("── [2] Adding & equipping all equipment ──");
            DebugEquipTest();

            // 3) 스탯 after
            Debug.Log("── [3] AFTER equipping ──");
            DebugPrintStats();

            // 4) 전체 해제
            Debug.Log("── [4] Unequipping all ──");
            DebugUnequipAll();

            // 5) 스탯 after unequip
            Debug.Log("── [5] AFTER unequipping ──");
            DebugPrintStats();

            Debug.Log("╔═══════════════════════════════════════╗");
            Debug.Log("║       FULL FLOW TEST COMPLETE         ║");
            Debug.Log("╚═══════════════════════════════════════╝");
        }
    }
}
