using UnityEngine;
using RPGSystem.Inventory;
using RPGSystem.Item.Data;

namespace RPGSystem.Core
{
    /// <summary>
    /// 인벤토리 테스트용 디버그 컴포넌트.
    /// Inspector에서 테스트 아이템을 등록하고 ContextMenu / 키보드로 테스트한다.
    ///
    /// [GameObject] "InventoryDebugger" 빈 오브젝트에 부착.
    /// [Inspector] testItems 배열에 ScriptableObject 아이템들을 등록.
    /// </summary>
    public class InventoryDebugger : MonoBehaviour
    {
        [Header("테스트 아이템 (Inspector에서 SO 드래그)")]
        [Tooltip("테스트용으로 추가할 아이템들")]
        [SerializeField] private ItemData[] testItems;

        [Header("키 바인딩")]
        [SerializeField] private KeyCode addItemKey = KeyCode.F1;
        [SerializeField] private KeyCode removeItemKey = KeyCode.F2;
        [SerializeField] private KeyCode useItemKey = KeyCode.F3;
        [SerializeField] private KeyCode printKey = KeyCode.F4;
        [SerializeField] private KeyCode sortKey = KeyCode.F5;

        [Header("설정")]
        [SerializeField] private int addAmount = 1;
        [SerializeField] private int testSlotIndex = 0;

        private int _currentTestItemIndex;

        private void Update()
        {
            if (Input.GetKeyDown(addItemKey))
                DebugAddCurrentItem();
            if (Input.GetKeyDown(removeItemKey))
                DebugRemoveFromSlot();
            if (Input.GetKeyDown(useItemKey))
                DebugUseFromSlot();
            if (Input.GetKeyDown(printKey))
                DebugPrint();
            if (Input.GetKeyDown(sortKey))
                DebugSort();

            // 숫자키 1~9로 테스트 아이템 선택
            for (int i = 0; i < 9 && i < testItems.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _currentTestItemIndex = i;
                    Debug.Log($"[Debug] Selected test item: {testItems[i].itemName}");
                }
            }
        }

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

        [ContextMenu("Debug: Use From Test Slot")]
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

        [ContextMenu("Debug: Test Swap Slots 0↔1")]
        public void DebugSwapSlots()
        {
            InventoryManager.Instance.SwapSlots(0, 1);
            Debug.Log("[Debug] Swapped slots 0 and 1");
        }

        [ContextMenu("Debug: Test Stack Merge")]
        public void DebugTestStackMerge()
        {
            if (testItems == null || testItems.Length == 0) return;
            var item = testItems[0];
            if (!item.isStackable)
            {
                Debug.LogWarning("[Debug] First test item is not stackable.");
                return;
            }
            InventoryManager.Instance.AddItem(item, 3);
            InventoryManager.Instance.AddItem(item, 5);
            Debug.Log("[Debug] Added 3 + 5 stackable items. Check inventory.");
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
    }
}
