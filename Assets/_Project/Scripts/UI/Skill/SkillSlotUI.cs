using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPGSystem.Skill;
using RPGSystem.Skill.Data;

namespace RPGSystem.UI
{
    /// <summary>
    /// 스킬 슬롯 UI.
    /// SkillInstance 하나의 정보를 표시한다.
    /// 아이콘, 이름, 레벨, 숙련도 바, 잼 소켓 상태를 표시한다.
    ///
    /// 상호작용:
    /// - 좌클릭: 스킬 선택 (상세 정보 표시)
    /// - 우클릭: 스킬 사용 (Active/Buff만)
    /// - 더블클릭: 스킬 사용
    /// - 마우스 호버: 툴팁 표시
    ///
    /// [GameObject] 스킬 목록의 각 스킬 항목 Prefab에 부착.
    /// [Inspector] 하위 UI 요소 연결.
    /// </summary>
    public class SkillSlotUI : UISlotBase
    {
        [Header("스킬 슬롯 추가 요소")]
        [Tooltip("스킬 이름 텍스트")]
        [SerializeField] private TMP_Text skillNameText;

        [Tooltip("스킬 레벨 텍스트")]
        [SerializeField] private TMP_Text levelText;

        [Tooltip("숙련도 게이지 바 (fill Image)")]
        [SerializeField] private Image proficiencyBar;

        [Tooltip("스킬 타입 아이콘 또는 라벨")]
        [SerializeField] private TMP_Text typeLabel;

        [Tooltip("잼 소켓 상태 텍스트 (예: 2/3)")]
        [SerializeField] private TMP_Text socketText;

        /// <summary>현재 표시 중인 스킬 인스턴스 참조</summary>
        private SkillInstance _currentSkill;

        /// <summary>소속 컨트롤러</summary>
        private SkillUIController _controller;

        // ──────────────────────────────────────
        // 설정
        // ──────────────────────────────────────

        public void SetController(SkillUIController controller)
        {
            _controller = controller;
        }

        // ──────────────────────────────────────
        // 데이터 업데이트
        // ──────────────────────────────────────

        /// <summary>
        /// SkillInstance 데이터로 슬롯 UI 갱신.
        /// </summary>
        public void Refresh(SkillInstance skill)
        {
            if (skill == null)
            {
                Clear();
                return;
            }

            _currentSkill = skill;
            IsEmpty = false;

            // 아이콘
            SetIcon(skill.data.icon);

            // 이름
            if (skillNameText != null)
                skillNameText.text = skill.data.skillName;

            // 레벨
            if (levelText != null)
                levelText.text = $"Lv.{skill.proficiency.level}";

            // 숙련도 바
            if (proficiencyBar != null)
            {
                if (skill.proficiency.IsMaxLevel)
                    proficiencyBar.fillAmount = 1f;
                else
                    proficiencyBar.fillAmount = skill.proficiency.Progress;
            }

            // 타입 라벨
            if (typeLabel != null)
            {
                typeLabel.text = skill.data.skillType switch
                {
                    SkillType.Active  => "Active",
                    SkillType.Buff    => "Buff",
                    SkillType.Passive => "Passive",
                    _                 => ""
                };
            }

            // 소켓 상태
            if (socketText != null)
            {
                if (skill.data.maxGemSockets > 0)
                {
                    socketText.text = $"{skill.AttachedGemCount}/{skill.UnlockedSocketCount}";
                    socketText.enabled = true;
                }
                else
                {
                    socketText.enabled = false;
                }
            }

            // 수량 텍스트는 스킬에서 미사용
            SetQuantity(null);
        }

        public override void Clear()
        {
            base.Clear();
            _currentSkill = null;

            if (skillNameText != null) skillNameText.text = "";
            if (levelText != null) levelText.text = "";
            if (proficiencyBar != null) proficiencyBar.fillAmount = 0f;
            if (typeLabel != null) typeLabel.text = "";
            if (socketText != null) socketText.text = "";
        }

        // ──────────────────────────────────────
        // 상호작용
        // ──────────────────────────────────────

        protected override void OnLeftClick()
        {
            _controller?.OnSkillSlotSelected(SlotIndex);
        }

        protected override void OnRightClick()
        {
            if (_currentSkill == null) return;
            SkillManager.Instance?.UseSkill(_currentSkill);
        }

        protected override void OnDoubleClick()
        {
            if (_currentSkill == null) return;
            SkillManager.Instance?.UseSkill(_currentSkill);
        }

        protected override void OnHoverEnter()
        {
            if (_currentSkill == null) return;
            TooltipUI.Instance?.ShowSkill(_currentSkill);
        }

        protected override void OnHoverExit()
        {
            TooltipUI.Instance?.Hide();
        }
    }
}
