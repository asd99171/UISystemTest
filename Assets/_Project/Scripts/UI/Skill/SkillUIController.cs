using UnityEngine;
using TMPro;
using RPGSystem.Core;
using RPGSystem.Skill;

namespace RPGSystem.UI
{
    /// <summary>
    /// 스킬 UI 컨트롤러.
    /// SkillManager의 보유 스킬 목록을 UI로 표시한다.
    /// 스킬은 동적 리스트이므로, 패널 열릴 때마다 슬롯을 재생성한다.
    ///
    /// [GameObject] 스킬 패널(UIPanel)과 동일 오브젝트에 부착.
    /// [Inspector] slotContainer에 Vertical/Grid Layout Group이 있는 Transform 연결.
    /// [Inspector] slotPrefab에 SkillSlotUI가 부착된 Prefab 연결.
    /// </summary>
    public class SkillUIController : UIPanel
    {
        [Header("스킬 UI")]
        [Tooltip("스킬 슬롯 Prefab (SkillSlotUI 포함)")]
        [SerializeField] private SkillSlotUI slotPrefab;

        [Tooltip("슬롯이 배치될 컨테이너 (Vertical Layout Group 등)")]
        [SerializeField] private Transform slotContainer;

        [Header("스킬 상세 (선택)")]
        [Tooltip("선택된 스킬의 상세 정보 텍스트")]
        [SerializeField] private TMP_Text detailText;

        /// <summary>현재 생성된 슬롯 UI 목록</summary>
        private SkillSlotUI[] _slotUIs;

        /// <summary>현재 선택된 슬롯 인덱스</summary>
        private int _selectedIndex = -1;

        // ──────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Subscribe<SkillListChangedEvent>(OnSkillListChanged);
            EventBus.Subscribe<SkillLevelUpEvent>(OnSkillLevelUp);
            EventBus.Subscribe<SkillUsedEvent>(OnSkillUsed);
            EventBus.Subscribe<GemAttachedEvent>(OnGemChanged);
            EventBus.Subscribe<GemDetachedEvent>(OnGemDetached);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SkillListChangedEvent>(OnSkillListChanged);
            EventBus.Unsubscribe<SkillLevelUpEvent>(OnSkillLevelUp);
            EventBus.Unsubscribe<SkillUsedEvent>(OnSkillUsed);
            EventBus.Unsubscribe<GemAttachedEvent>(OnGemChanged);
            EventBus.Unsubscribe<GemDetachedEvent>(OnGemDetached);
        }

        // ──────────────────────────────────────
        // 패널 열기/닫기
        // ──────────────────────────────────────

        protected override void OnOpened()
        {
            RebuildSlots();
            Debug.Log("[SkillUI] Opened");
        }

        protected override void OnClosed()
        {
            ClearSelection();
            ClearDetail();
            TooltipUI.Instance?.Hide();
            Debug.Log("[SkillUI] Closed");
        }

        // ──────────────────────────────────────
        // 슬롯 생성/갱신
        // ──────────────────────────────────────

        /// <summary>
        /// 스킬 목록에 맞춰 슬롯 UI를 재생성.
        /// 동적 리스트이므로 매번 파괴 후 재생성한다.
        /// </summary>
        private void RebuildSlots()
        {
            ClearSlots();

            var skillMgr = SkillManager.Instance;
            if (skillMgr == null || slotPrefab == null || slotContainer == null)
                return;

            int count = skillMgr.SkillCount;
            _slotUIs = new SkillSlotUI[count];

            for (int i = 0; i < count; i++)
            {
                var slotObj = Instantiate(slotPrefab, slotContainer);
                slotObj.gameObject.name = $"SkillSlot_{i:D2}";
                slotObj.Initialize(i);
                slotObj.SetController(this);
                slotObj.Refresh(skillMgr.GetSkillByIndex(i));
                _slotUIs[i] = slotObj;
            }
        }

        /// <summary>기존 슬롯 UI 모두 제거</summary>
        private void ClearSlots()
        {
            if (_slotUIs != null)
            {
                foreach (var slot in _slotUIs)
                {
                    if (slot != null)
                        Destroy(slot.gameObject);
                }
                _slotUIs = null;
            }

            // 컨테이너에 남은 자식도 정리
            if (slotContainer != null)
            {
                for (int i = slotContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(slotContainer.GetChild(i).gameObject);
                }
            }
        }

        /// <summary>전체 슬롯 데이터 갱신 (재생성 없이)</summary>
        private void RefreshAll()
        {
            var skillMgr = SkillManager.Instance;
            if (skillMgr == null || _slotUIs == null) return;

            for (int i = 0; i < _slotUIs.Length; i++)
            {
                var skill = skillMgr.GetSkillByIndex(i);
                _slotUIs[i].Refresh(skill);
            }
        }

        // ──────────────────────────────────────
        // 이벤트 핸들러
        // ──────────────────────────────────────

        private void OnSkillListChanged(SkillListChangedEvent evt)
        {
            if (!IsOpen) return;
            RebuildSlots();
        }

        private void OnSkillLevelUp(SkillLevelUpEvent evt)
        {
            if (!IsOpen) return;
            RefreshAll();
            RefreshDetail();
        }

        private void OnSkillUsed(SkillUsedEvent evt)
        {
            if (!IsOpen) return;
            RefreshAll();
            RefreshDetail();
        }

        private void OnGemChanged(GemAttachedEvent evt)
        {
            if (!IsOpen) return;
            RefreshAll();
            RefreshDetail();
        }

        private void OnGemDetached(GemDetachedEvent evt)
        {
            if (!IsOpen) return;
            RefreshAll();
            RefreshDetail();
        }

        // ──────────────────────────────────────
        // 선택 + 상세 정보
        // ──────────────────────────────────────

        /// <summary>스킬 슬롯이 선택됨 (SkillSlotUI가 호출)</summary>
        public void OnSkillSlotSelected(int index)
        {
            if (_selectedIndex == index)
            {
                ClearSelection();
                ClearDetail();
                return;
            }

            ClearSelection();
            _selectedIndex = index;

            if (_slotUIs != null && index >= 0 && index < _slotUIs.Length)
            {
                _slotUIs[index].SetSelected(true);
                ShowDetail(index);
            }
        }

        /// <summary>선택 해제</summary>
        public void ClearSelection()
        {
            if (_selectedIndex >= 0 && _slotUIs != null && _selectedIndex < _slotUIs.Length)
                _slotUIs[_selectedIndex].SetSelected(false);
            _selectedIndex = -1;
        }

        /// <summary>선택된 스킬 상세 정보 표시</summary>
        private void ShowDetail(int index)
        {
            if (detailText == null) return;

            var skillMgr = SkillManager.Instance;
            if (skillMgr == null) return;

            var skill = skillMgr.GetSkillByIndex(index);
            if (skill == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{skill.data.skillName}</b>  [{skill.data.skillType}]");
            sb.AppendLine($"Level: {skill.proficiency.level}");

            if (!skill.proficiency.IsMaxLevel)
                sb.AppendLine($"EXP: {skill.proficiency.currentExp}/{skill.proficiency.RequiredExp} ({skill.proficiency.Progress * 100:F0}%)");
            else
                sb.AppendLine("EXP: MAX");

            sb.AppendLine();
            sb.AppendLine($"Damage: {skill.TotalDamage:F1}");
            sb.AppendLine($"Cooldown: {skill.TotalCooldown:F1}s");
            sb.AppendLine($"MP Cost: {skill.TotalMpCost:F0}");

            // 잼 소켓 정보
            if (skill.data.maxGemSockets > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Gem Sockets ({skill.AttachedGemCount}/{skill.UnlockedSocketCount}/{skill.data.maxGemSockets}):");
                for (int i = 0; i < skill.gemSockets.Length; i++)
                {
                    var gs = skill.gemSockets[i];
                    if (!gs.isUnlocked)
                        sb.AppendLine($"  [{i}] Locked");
                    else if (gs.HasGem)
                        sb.AppendLine($"  [{i}] {gs.attachedGem.itemName}");
                    else
                        sb.AppendLine($"  [{i}] Empty");
                }
            }

            detailText.text = sb.ToString().TrimEnd();
        }

        /// <summary>상세 정보 갱신 (선택 중일 때)</summary>
        private void RefreshDetail()
        {
            if (_selectedIndex >= 0)
                ShowDetail(_selectedIndex);
        }

        /// <summary>상세 정보 지우기</summary>
        private void ClearDetail()
        {
            if (detailText != null)
                detailText.text = "";
        }
    }
}
