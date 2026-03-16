using UnityEngine;
using RPGSystem.Core;
using RPGSystem.Inventory;
using RPGSystem.Item;
using RPGSystem.Item.Data;
using RPGSystem.Skill;

namespace RPGSystem.Gem
{
    /// <summary>
    /// 잼 서비스.
    /// 스킬의 잼 소켓에 잼을 장착/해제하는 로직을 처리한다.
    /// 인벤토리와 연동하여 잼 아이템을 소모/반환한다.
    ///
    /// [GameObject] "SystemManager" 오브젝트에 부착.
    /// </summary>
    public class GemService : MonoBehaviour
    {
        public static GemService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ──────────────────────────────────────
        // 잼 장착
        // ──────────────────────────────────────

        /// <summary>
        /// 인벤토리의 잼 아이템을 스킬의 특정 소켓에 장착.
        ///
        /// 1) 소켓 해금 상태 검증
        /// 2) 기존 잼이 있으면 먼저 해제 → 인벤토리로 반환
        /// 3) 인벤토리에서 잼 아이템 1개 제거
        /// 4) 소켓에 잼 장착
        /// 5) GemAttachedEvent 발행
        /// </summary>
        public bool AttachGem(SkillInstance skill, int socketIndex, ItemInstance gemItem)
        {
            if (skill == null || gemItem == null)
                return false;

            // 잼 타입 검증
            if (gemItem.data is not GemData gemData)
            {
                Debug.LogWarning($"[Gem] '{gemItem.data.itemName}' is not a gem.");
                return false;
            }

            // 소켓 인덱스 검증
            if (socketIndex < 0 || socketIndex >= skill.gemSockets.Length)
            {
                Debug.LogWarning($"[Gem] Invalid socket index: {socketIndex}");
                return false;
            }

            var socket = skill.gemSockets[socketIndex];

            // 소켓 해금 검증
            if (!socket.isUnlocked)
            {
                Debug.LogWarning($"[Gem] Socket[{socketIndex}] is locked. " +
                                 $"Required proficiency level not reached.");
                return false;
            }

            var inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            // 기존 잼이 있으면 먼저 해제
            if (socket.HasGem)
            {
                if (!DetachGem(skill, socketIndex))
                {
                    Debug.LogWarning("[Gem] Failed to detach existing gem.");
                    return false;
                }
            }

            // 인벤토리에서 잼 1개 제거
            var (slotIndex, _) = inventory.FindItemByUid(gemItem.uid);
            if (slotIndex >= 0)
            {
                inventory.RemoveItem(slotIndex, 1);
            }
            else
            {
                Debug.LogWarning("[Gem] Gem item not found in inventory.");
                return false;
            }

            // 소켓에 장착
            socket.AttachGem(gemData);

            Debug.Log($"[Gem] Attached '{gemData.itemName}' to " +
                      $"'{skill.data.skillName}' Socket[{socketIndex}]" +
                      $" | DMG Bonus: +{gemData.skillDamageBonus * 100:F0}%" +
                      $" | CD Reduction: -{gemData.cooldownReduction:F1}s");

            EventBus.Publish(new GemAttachedEvent
            {
                skill = skill,
                socketIndex = socketIndex,
                gem = gemData
            });

            return true;
        }

        /// <summary>
        /// 인벤토리 슬롯 인덱스로 잼 장착 (UI 드래그 앤 드롭용).
        /// </summary>
        public bool AttachGemFromInventory(SkillInstance skill, int socketIndex, int inventorySlotIndex)
        {
            var inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            var slot = inventory.Slots[inventorySlotIndex];
            if (slot.IsEmpty) return false;

            return AttachGem(skill, socketIndex, slot.item);
        }

        // ──────────────────────────────────────
        // 잼 해제
        // ──────────────────────────────────────

        /// <summary>
        /// 스킬 소켓에서 잼 해제. 인벤토리로 반환.
        /// 인벤토리가 가득 차면 해제 실패.
        /// </summary>
        public bool DetachGem(SkillInstance skill, int socketIndex)
        {
            if (skill == null) return false;

            if (socketIndex < 0 || socketIndex >= skill.gemSockets.Length)
                return false;

            var socket = skill.gemSockets[socketIndex];
            if (!socket.HasGem)
            {
                Debug.Log($"[Gem] Socket[{socketIndex}] has no gem.");
                return false;
            }

            var inventory = InventoryManager.Instance;
            if (inventory == null) return false;

            var gemData = socket.attachedGem;

            // 인벤토리에 공간 있는지 확인 후 반환
            int added = inventory.AddItem(gemData, 1);
            if (added <= 0)
            {
                Debug.LogWarning("[Gem] Inventory full, cannot detach gem.");
                return false;
            }

            // 소켓에서 제거
            socket.DetachGem();

            Debug.Log($"[Gem] Detached '{gemData.itemName}' from " +
                      $"'{skill.data.skillName}' Socket[{socketIndex}] → inventory");

            EventBus.Publish(new GemDetachedEvent
            {
                skill = skill,
                socketIndex = socketIndex,
                gem = gemData
            });

            return true;
        }

        /// <summary>
        /// 스킬의 모든 잼 해제.
        /// </summary>
        public void DetachAllGems(SkillInstance skill)
        {
            if (skill == null) return;

            for (int i = 0; i < skill.gemSockets.Length; i++)
            {
                if (skill.gemSockets[i].HasGem)
                    DetachGem(skill, i);
            }
        }

        // ──────────────────────────────────────
        // 디버그
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print All Gem Sockets")]
        public void DebugPrintAllSockets()
        {
            var skillMgr = SkillManager.Instance;
            if (skillMgr == null) return;

            Debug.Log("═══════════ GEM SOCKETS ═══════════");
            foreach (var skill in skillMgr.Skills)
            {
                Debug.Log($"  Skill: {skill.data.skillName} " +
                          $"(Gems: {skill.AttachedGemCount}/{skill.UnlockedSocketCount}/{skill.data.maxGemSockets})");
                for (int i = 0; i < skill.gemSockets.Length; i++)
                {
                    var gs = skill.gemSockets[i];
                    if (!gs.isUnlocked)
                        Debug.Log($"    [{i}] LOCKED (Unlock at Lv.{GetUnlockLevel(skill, i)})");
                    else if (gs.HasGem)
                        Debug.Log($"    [{i}] {gs.attachedGem.itemName} " +
                                  $"(DMG+{gs.attachedGem.skillDamageBonus * 100:F0}% " +
                                  $"CD-{gs.attachedGem.cooldownReduction:F1}s)");
                    else
                        Debug.Log($"    [{i}] (empty)");
                }
            }
            Debug.Log("════════════════════════════════════");
        }

        private int GetUnlockLevel(SkillInstance skill, int socketIndex)
        {
            if (skill.data.socketUnlockLevels != null
                && socketIndex < skill.data.socketUnlockLevels.Length)
                return skill.data.socketUnlockLevels[socketIndex];
            return -1;
        }
    }
}
