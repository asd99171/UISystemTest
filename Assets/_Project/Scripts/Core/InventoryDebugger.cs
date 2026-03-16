using UnityEngine;
using RPGSystem.Equipment;
using RPGSystem.Gem;
using RPGSystem.Inventory;
using RPGSystem.Item;
using RPGSystem.Item.Data;
using RPGSystem.Skill;
using RPGSystem.Skill.Data;
using RPGSystem.Stat;
using RPGSystem.UI;

namespace RPGSystem.Core
{
    /// <summary>
    /// 인벤토리 + 장비 + 스킬 + 잼 통합 테스트 컴포넌트.
    ///
    /// [GameObject] "InventoryDebugger" 빈 오브젝트에 부착.
    /// [Inspector] testItems, testSkills, testGems 배열 설정.
    /// </summary>
    public class InventoryDebugger : MonoBehaviour
    {
        [Header("테스트 아이템 (Inspector에서 SO 드래그)")]
        [SerializeField] private ItemData[] testItems;

        [Header("테스트 스킬 (Inspector에서 SkillData SO 드래그)")]
        [SerializeField] private SkillData[] testSkills;

        [Header("테스트 잼 (Inspector에서 GemData SO 드래그)")]
        [SerializeField] private GemData[] testGems;

        [Header("키 바인딩")]
        [SerializeField] private KeyCode addItemKey = KeyCode.F1;
        [SerializeField] private KeyCode removeItemKey = KeyCode.F2;
        [SerializeField] private KeyCode useItemKey = KeyCode.F3;
        [SerializeField] private KeyCode printKey = KeyCode.F4;
        [SerializeField] private KeyCode sortKey = KeyCode.F5;
        [SerializeField] private KeyCode printEquipKey = KeyCode.F6;
        [SerializeField] private KeyCode printStatsKey = KeyCode.F7;
        [SerializeField] private KeyCode printSkillsKey = KeyCode.F8;
        [SerializeField] private KeyCode useSkillKey = KeyCode.F9;
        [SerializeField] private KeyCode addSkillExpKey = KeyCode.F10;

        [Header("설정")]
        [SerializeField] private int addAmount = 1;
        [SerializeField] private int testSlotIndex = 0;
        [SerializeField] private int debugExpAmount = 50;

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
            if (Input.GetKeyDown(printSkillsKey)) DebugPrintSkills();
            if (Input.GetKeyDown(useSkillKey))   DebugUseFirstSkill();
            if (Input.GetKeyDown(addSkillExpKey)) DebugAddExpToFirstSkill();

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

        // ══════════════════════════════════════
        //  인벤토리 테스트
        // ══════════════════════════════════════

        [ContextMenu("Inventory/Add Current Test Item")]
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

        [ContextMenu("Inventory/Add All Test Items")]
        public void DebugAddAllItems()
        {
            if (testItems == null) return;
            foreach (var item in testItems)
            {
                if (item != null)
                    InventoryManager.Instance.AddItem(item, addAmount);
            }
        }

        [ContextMenu("Inventory/Remove From Test Slot")]
        public void DebugRemoveFromSlot()
        {
            InventoryManager.Instance.RemoveItem(testSlotIndex, 1);
        }

        [ContextMenu("Inventory/Use From Test Slot")]
        public void DebugUseFromSlot()
        {
            InventoryManager.Instance.UseItem(testSlotIndex);
        }

        [ContextMenu("Inventory/Print Inventory")]
        public void DebugPrint()
        {
            InventoryManager.Instance.DebugPrintInventory();
        }

        [ContextMenu("Inventory/Sort")]
        public void DebugSort()
        {
            InventoryManager.Instance.SortInventory();
        }

        [ContextMenu("Inventory/Swap Slots 0↔1")]
        public void DebugSwapSlots()
        {
            InventoryManager.Instance.SwapSlots(0, 1);
            Debug.Log("[Debug] Swapped slots 0 and 1");
        }

        [ContextMenu("Inventory/Fill Inventory")]
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

        // ══════════════════════════════════════
        //  장비 테스트
        // ══════════════════════════════════════

        [ContextMenu("Equipment/Print Equipment")]
        public void DebugPrintEquipment()
        {
            EquipmentManager.Instance?.DebugPrintEquipment();
        }

        [ContextMenu("Equipment/Unequip All")]
        public void DebugUnequipAll()
        {
            EquipmentManager.Instance?.UnequipAll();
        }

        [ContextMenu("Equipment/Equip Test - Add & Equip All")]
        public void DebugEquipTest()
        {
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
            DebugPrintEquipment();
            DebugPrintStats();
        }

        // ══════════════════════════════════════
        //  스탯 테스트
        // ══════════════════════════════════════

        [ContextMenu("Stats/Print Stats")]
        public void DebugPrintStats()
        {
            PlayerStatManager.Instance?.DebugPrintStats();
        }

        // ══════════════════════════════════════
        //  스킬 테스트
        // ══════════════════════════════════════

        [ContextMenu("Skill/Learn All Test Skills")]
        public void DebugLearnAllSkills()
        {
            if (testSkills == null || testSkills.Length == 0)
            {
                // 폴백: SkillManager의 데이터베이스에서
                SkillManager.Instance?.DebugLearnAllSkills();
                return;
            }
            foreach (var skill in testSkills)
            {
                if (skill != null)
                    SkillManager.Instance?.LearnSkill(skill);
            }
        }

        [ContextMenu("Skill/Print Skills")]
        public void DebugPrintSkills()
        {
            SkillManager.Instance?.DebugPrintSkills();
        }

        [ContextMenu("Skill/Use First Skill")]
        public void DebugUseFirstSkill()
        {
            SkillManager.Instance?.DebugUseFirstSkill();
        }

        [ContextMenu("Skill/Add 50 EXP to First Skill")]
        public void DebugAddExpToFirstSkill()
        {
            if (SkillManager.Instance == null || SkillManager.Instance.SkillCount == 0)
            {
                Debug.LogWarning("[Debug] No skills learned.");
                return;
            }
            SkillManager.Instance.AddExp(
                SkillManager.Instance.GetSkillByIndex(0),
                debugExpAmount);
        }

        [ContextMenu("Skill/Max Level First Skill")]
        public void DebugMaxLevelFirstSkill()
        {
            SkillManager.Instance?.DebugMaxLevelFirst();
        }

        [ContextMenu("Skill/Use First Skill x10 (Proficiency Test)")]
        public void DebugUseFirstSkillRepeat()
        {
            var mgr = SkillManager.Instance;
            if (mgr == null || mgr.SkillCount == 0) return;

            var skill = mgr.GetSkillByIndex(0);
            Debug.Log($"═══ Using '{skill.data.skillName}' 10 times ═══");
            for (int i = 0; i < 10; i++)
            {
                mgr.UseSkill(skill);
            }
            Debug.Log($"═══ After 10 uses: Lv.{skill.proficiency.level} " +
                      $"EXP:{skill.proficiency.currentExp}/{skill.proficiency.RequiredExp} ═══");
        }

        // ══════════════════════════════════════
        //  잼 테스트
        // ══════════════════════════════════════

        [ContextMenu("Gem/Add Test Gems to Inventory")]
        public void DebugAddGems()
        {
            if (testGems == null || testGems.Length == 0)
            {
                Debug.LogWarning("[Debug] No test gems configured!");
                return;
            }
            foreach (var gem in testGems)
            {
                if (gem != null)
                    InventoryManager.Instance.AddItem(gem, 3);
            }
            Debug.Log($"[Debug] Added {testGems.Length} gem types (x3 each) to inventory.");
        }

        [ContextMenu("Gem/Attach First Gem to First Skill Socket 0")]
        public void DebugAttachGem()
        {
            var skillMgr = SkillManager.Instance;
            var gemSvc = GemService.Instance;
            var inv = InventoryManager.Instance;
            if (skillMgr == null || gemSvc == null || inv == null) return;

            if (skillMgr.SkillCount == 0)
            {
                Debug.LogWarning("[Debug] No skills learned.");
                return;
            }

            // 인벤토리에서 첫 번째 잼 찾기
            ItemInstance gemItem = null;
            for (int i = 0; i < inv.SlotCount; i++)
            {
                if (!inv.Slots[i].IsEmpty && inv.Slots[i].item.data.itemType == ItemType.Gem)
                {
                    gemItem = inv.Slots[i].item;
                    break;
                }
            }

            if (gemItem == null)
            {
                Debug.LogWarning("[Debug] No gems in inventory. Use 'Add Test Gems' first.");
                return;
            }

            var skill = skillMgr.GetSkillByIndex(0);
            gemSvc.AttachGem(skill, 0, gemItem);
        }

        [ContextMenu("Gem/Detach All Gems from First Skill")]
        public void DebugDetachAllGems()
        {
            var skillMgr = SkillManager.Instance;
            var gemSvc = GemService.Instance;
            if (skillMgr == null || gemSvc == null) return;
            if (skillMgr.SkillCount == 0) return;

            gemSvc.DetachAllGems(skillMgr.GetSkillByIndex(0));
        }

        [ContextMenu("Gem/Print All Sockets")]
        public void DebugPrintSockets()
        {
            GemService.Instance?.DebugPrintAllSockets();
        }

        // ══════════════════════════════════════
        //  통합 테스트
        // ══════════════════════════════════════

        // ══════════════════════════════════════
        //  UI 상태 테스트
        // ══════════════════════════════════════

        [ContextMenu("UI State/Print Game State")]
        public void DebugPrintGameState()
        {
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.CurrentState;
            Debug.Log($"═══ GAME STATE: {state} ═══");
            Debug.Log($"  IsPlaying: {GameManager.Instance.IsPlaying}");
            Debug.Log($"  IsInUI: {GameManager.Instance.IsInUI}");
            Debug.Log($"  IsInputAllowed: {GameManager.Instance.IsInputAllowed}");
            Debug.Log($"  Time.timeScale: {Time.timeScale}");
            Debug.Log($"  Cursor.lockState: {Cursor.lockState}");
            Debug.Log($"  Cursor.visible: {Cursor.visible}");
        }

        [ContextMenu("UI State/Toggle Inventory Panel")]
        public void DebugToggleInventory()
        {
            UIManager.Instance?.TogglePanel("Inventory");
            DebugPrintGameState();
        }

        [ContextMenu("UI State/Toggle Equipment Panel")]
        public void DebugToggleEquipment()
        {
            UIManager.Instance?.TogglePanel("Equipment");
            DebugPrintGameState();
        }

        [ContextMenu("UI State/Toggle Skill Panel")]
        public void DebugToggleSkill()
        {
            UIManager.Instance?.TogglePanel("Skill");
            DebugPrintGameState();
        }

        [ContextMenu("UI State/Close All Panels")]
        public void DebugCloseAllPanels()
        {
            UIManager.Instance?.CloseAll();
            DebugPrintGameState();
        }

        [ContextMenu("UI State/Print Panel Stack")]
        public void DebugPrintPanelStack()
        {
            UIManager.Instance?.DebugPrintStack();
        }

        [ContextMenu("UI State/UI Flow Test (Open → Stack → Close)")]
        public void DebugUIFlowTest()
        {
            Debug.Log("╔═══════════════════════════════════╗");
            Debug.Log("║     UI STATE FLOW TEST            ║");
            Debug.Log("╚═══════════════════════════════════╝");

            var uiMgr = UIManager.Instance;
            if (uiMgr == null)
            {
                Debug.LogWarning("[Debug] UIManager not found.");
                return;
            }

            Debug.Log("── [1] Initial state ──");
            DebugPrintGameState();

            Debug.Log("── [2] Open Inventory (I) ──");
            uiMgr.OpenPanel("Inventory");
            DebugPrintGameState();

            Debug.Log("── [3] Open Equipment (E) while Inventory is open ──");
            uiMgr.OpenPanel("Equipment");
            DebugPrintGameState();
            uiMgr.DebugPrintStack();

            Debug.Log("── [4] ESC: Close top (Equipment) ──");
            uiMgr.CloseTop();
            DebugPrintGameState();
            uiMgr.DebugPrintStack();

            Debug.Log("── [5] ESC: Close top (Inventory) → back to Playing ──");
            uiMgr.CloseTop();
            DebugPrintGameState();

            Debug.Log("╔═══════════════════════════════════╗");
            Debug.Log("║     UI FLOW TEST COMPLETE         ║");
            Debug.Log("╚═══════════════════════════════════╝");
        }

        // ══════════════════════════════════════
        //  통합 테스트
        // ══════════════════════════════════════

        [ContextMenu("Full Test/Skill + Gem Full Flow")]
        public void DebugSkillGemFullFlow()
        {
            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║     SKILL + GEM FULL FLOW TEST            ║");
            Debug.Log("╚═══════════════════════════════════════════╝");

            // 1) 스킬 습득
            Debug.Log("── [1] Learning skills ──");
            DebugLearnAllSkills();

            // 2) 스킬 사용 x5 (숙련도 쌓기)
            Debug.Log("── [2] Using first skill 5 times ──");
            var mgr = SkillManager.Instance;
            if (mgr != null && mgr.SkillCount > 0)
            {
                var skill = mgr.GetSkillByIndex(0);
                for (int i = 0; i < 5; i++)
                    mgr.UseSkill(skill);
            }

            // 3) 경험치 대량 추가 → 레벨업 + 소켓 해금
            Debug.Log("── [3] Adding 500 EXP (level up + socket unlock) ──");
            if (mgr != null && mgr.SkillCount > 0)
                mgr.AddExp(mgr.GetSkillByIndex(0), 500);

            // 4) 잼 추가
            Debug.Log("── [4] Adding gems to inventory ──");
            DebugAddGems();

            // 5) 잼 장착
            Debug.Log("── [5] Attaching gem ──");
            DebugAttachGem();

            // 6) 스킬 상태 확인 (잼 효과 반영)
            Debug.Log("── [6] Final skill status ──");
            DebugPrintSkills();
            DebugPrintSockets();

            // 7) 잼 해제
            Debug.Log("── [7] Detaching gems ──");
            DebugDetachAllGems();

            // 8) 잼 해제 후 스킬 상태
            Debug.Log("── [8] After gem detach ──");
            DebugPrintSkills();

            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║     FULL FLOW TEST COMPLETE               ║");
            Debug.Log("╚═══════════════════════════════════════════╝");
        }
    }
}
