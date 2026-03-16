using System.Collections.Generic;
using UnityEngine;
using RPGSystem.Core;
using RPGSystem.Skill.Data;

namespace RPGSystem.Skill
{
    /// <summary>
    /// 스킬 매니저.
    /// 캐릭터가 보유한 스킬을 관리하고 스킬 사용/숙련도를 처리한다.
    ///
    /// 핵심 기능:
    /// - 스킬 습득/제거
    /// - 스킬 사용 → 숙련도 경험치 증가 → 레벨업 → 잼 소켓 해금
    /// - 보유 스킬 조회
    ///
    /// [GameObject] "SystemManager" 오브젝트에 부착.
    /// [Inspector] skillDatabase에 SkillDatabase SO 드래그.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        public static SkillManager Instance { get; private set; }

        [Header("스킬 데이터베이스")]
        [Tooltip("전체 스킬 DB (SO)")]
        [SerializeField] private SkillDatabase skillDatabase;

        [Header("설정")]
        [Tooltip("사용 시 기본 경험치 (SkillData.expPerUse가 0일 때 폴백)")]
        [SerializeField] private int defaultExpPerUse = 10;

        /// <summary>보유 스킬 목록</summary>
        private List<SkillInstance> _skills = new List<SkillInstance>();

        /// <summary>uid → SkillInstance 빠른 조회</summary>
        private Dictionary<string, SkillInstance> _skillLookup = new Dictionary<string, SkillInstance>();

        /// <summary>보유 스킬 목록 (읽기 전용)</summary>
        public IReadOnlyList<SkillInstance> Skills => _skills;

        /// <summary>스킬 데이터베이스 접근</summary>
        public SkillDatabase Database => skillDatabase;

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
        }

        // ──────────────────────────────────────
        // 스킬 습득 / 제거
        // ──────────────────────────────────────

        /// <summary>
        /// 스킬 습득. SkillData로부터 SkillInstance를 생성하여 보유 목록에 추가.
        /// 이미 보유한 스킬이면 false 반환.
        /// </summary>
        public bool LearnSkill(SkillData skillData)
        {
            if (skillData == null) return false;

            // 중복 체크
            if (HasSkill(skillData.skillId))
            {
                Debug.LogWarning($"[Skill] Already learned: {skillData.skillName}");
                return false;
            }

            var instance = new SkillInstance(skillData);
            _skills.Add(instance);
            _skillLookup[instance.uid] = instance;

            Debug.Log($"[Skill] Learned: {skillData.skillName} " +
                      $"(Type: {skillData.skillType}, MaxSockets: {skillData.maxGemSockets})");

            EventBus.Publish(new SkillLearnedEvent { skill = instance });
            EventBus.Publish(new SkillListChangedEvent());
            return true;
        }

        /// <summary>
        /// skillId로 스킬 습득 (데이터베이스에서 조회)
        /// </summary>
        public bool LearnSkillById(string skillId)
        {
            if (skillDatabase == null)
            {
                Debug.LogError("[Skill] SkillDatabase not assigned.");
                return false;
            }

            var data = skillDatabase.GetById(skillId);
            if (data == null)
            {
                Debug.LogWarning($"[Skill] Skill not found in database: {skillId}");
                return false;
            }

            return LearnSkill(data);
        }

        /// <summary>
        /// 스킬 제거. 장착된 잼은 소멸한다 (인벤토리 반환은 GemService에서 처리).
        /// </summary>
        public bool ForgetSkill(string uid)
        {
            var skill = GetSkillByUid(uid);
            if (skill == null) return false;

            _skills.Remove(skill);
            _skillLookup.Remove(uid);

            Debug.Log($"[Skill] Forgot: {skill.data.skillName}");
            EventBus.Publish(new SkillListChangedEvent());
            return true;
        }

        // ──────────────────────────────────────
        // 스킬 사용
        // ──────────────────────────────────────

        /// <summary>
        /// 스킬 사용.
        ///
        /// 1) 패시브는 사용 불가 (항상 적용 중)
        /// 2) 숙련도 경험치 증가
        /// 3) 레벨업 시 소켓 해금 + 이벤트 발행
        /// 4) SkillUsedEvent 발행 (전투 시스템이 데미지/효과 적용)
        ///
        /// 반환: 사용 성공 여부
        /// </summary>
        public bool UseSkill(string uid)
        {
            var skill = GetSkillByUid(uid);
            if (skill == null)
            {
                Debug.LogWarning("[Skill] Skill not found.");
                return false;
            }

            return UseSkill(skill);
        }

        /// <summary>스킬 인스턴스로 직접 사용</summary>
        public bool UseSkill(SkillInstance skill)
        {
            if (skill == null) return false;

            // 패시브는 사용 불가
            if (skill.data.skillType == SkillType.Passive)
            {
                Debug.Log($"[Skill] '{skill.data.skillName}' is passive and always active.");
                return false;
            }

            // 경험치 증가
            int exp = skill.data.expPerUse > 0 ? skill.data.expPerUse : defaultExpPerUse;
            int prevLevel = skill.proficiency.level;
            bool leveledUp = skill.AddProficiencyExp(exp);

            Debug.Log($"[Skill] Used '{skill.data.skillName}' " +
                      $"| Damage: {skill.TotalDamage:F1} " +
                      $"| CD: {skill.TotalCooldown:F1}s " +
                      $"| MP: {skill.TotalMpCost:F0} " +
                      $"| EXP +{exp} ({skill.proficiency.currentExp}/{skill.proficiency.RequiredExp})");

            // 스킬 사용 이벤트
            EventBus.Publish(new SkillUsedEvent
            {
                skill = skill,
                damage = skill.TotalDamage,
                cooldown = skill.TotalCooldown,
                mpCost = skill.TotalMpCost
            });

            // 레벨업 이벤트
            if (leveledUp)
            {
                int newSockets = skill.UnlockedSocketCount;
                int prevSockets = skill.data.GetUnlockedSocketCount(prevLevel);
                int gained = newSockets - prevSockets;

                Debug.Log($"[Skill] ★ LEVEL UP! '{skill.data.skillName}' " +
                          $"Lv.{prevLevel} → Lv.{skill.proficiency.level} " +
                          $"| Damage: {skill.CurrentDamage:F1} → {skill.TotalDamage:F1}");

                if (gained > 0)
                {
                    Debug.Log($"[Skill] ★ {gained} new gem socket(s) unlocked! " +
                              $"(Total: {newSockets}/{skill.data.maxGemSockets})");
                }

                EventBus.Publish(new SkillLevelUpEvent
                {
                    skill = skill,
                    newLevel = skill.proficiency.level,
                    newSocketsUnlocked = gained
                });
            }

            return true;
        }

        // ──────────────────────────────────────
        // 경험치 직접 추가 (디버그/퀘스트 보상 등)
        // ──────────────────────────────────────

        /// <summary>
        /// 특정 스킬에 경험치 직접 추가.
        /// 스킬 사용과 달리 SkillUsedEvent는 발행하지 않는다.
        /// </summary>
        public bool AddExp(string uid, int amount)
        {
            var skill = GetSkillByUid(uid);
            if (skill == null) return false;

            return AddExp(skill, amount);
        }

        /// <summary>스킬 인스턴스에 경험치 직접 추가</summary>
        public bool AddExp(SkillInstance skill, int amount)
        {
            if (skill == null || amount <= 0) return false;

            int prevLevel = skill.proficiency.level;
            bool leveledUp = skill.AddProficiencyExp(amount);

            Debug.Log($"[Skill] Added {amount} EXP to '{skill.data.skillName}' " +
                      $"({skill.proficiency.currentExp}/{skill.proficiency.RequiredExp})");

            if (leveledUp)
            {
                int newSockets = skill.UnlockedSocketCount;
                int prevSockets = skill.data.GetUnlockedSocketCount(prevLevel);

                Debug.Log($"[Skill] ★ LEVEL UP! Lv.{prevLevel} → Lv.{skill.proficiency.level}");

                EventBus.Publish(new SkillLevelUpEvent
                {
                    skill = skill,
                    newLevel = skill.proficiency.level,
                    newSocketsUnlocked = newSockets - prevSockets
                });
            }

            return true;
        }

        // ──────────────────────────────────────
        // 조회
        // ──────────────────────────────────────

        /// <summary>uid로 스킬 조회</summary>
        public SkillInstance GetSkillByUid(string uid)
        {
            _skillLookup.TryGetValue(uid, out var skill);
            return skill;
        }

        /// <summary>skillId로 보유 스킬 조회 (첫 매칭)</summary>
        public SkillInstance GetSkillById(string skillId)
        {
            foreach (var skill in _skills)
            {
                if (skill.data.skillId == skillId)
                    return skill;
            }
            return null;
        }

        /// <summary>인덱스로 스킬 조회</summary>
        public SkillInstance GetSkillByIndex(int index)
        {
            if (index >= 0 && index < _skills.Count)
                return _skills[index];
            return null;
        }

        /// <summary>특정 skillId를 보유하고 있는지</summary>
        public bool HasSkill(string skillId)
        {
            return GetSkillById(skillId) != null;
        }

        /// <summary>보유 스킬 수</summary>
        public int SkillCount => _skills.Count;

        /// <summary>특정 타입의 스킬만 반환</summary>
        public List<SkillInstance> GetSkillsByType(SkillType type)
        {
            var result = new List<SkillInstance>();
            foreach (var skill in _skills)
            {
                if (skill.data.skillType == type)
                    result.Add(skill);
            }
            return result;
        }

        // ──────────────────────────────────────
        // 패시브 스킬 스탯 수집
        // ──────────────────────────────────────

        /// <summary>
        /// 모든 패시브 스킬의 스탯 효과를 수집.
        /// PlayerStatManager에서 호출하여 최종 스탯에 반영할 수 있다.
        /// </summary>
        public List<Stat.StatModifier> GetAllPassiveModifiers()
        {
            var modifiers = new List<Stat.StatModifier>();
            foreach (var skill in _skills)
            {
                if (skill.data.skillType == SkillType.Passive)
                {
                    modifiers.AddRange(skill.GetTotalStatEffects());
                }
            }
            return modifiers;
        }

        // ──────────────────────────────────────
        // 디버그
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print All Skills")]
        public void DebugPrintSkills()
        {
            Debug.Log($"═══════════ SKILLS ({_skills.Count}) ═══════════");
            for (int i = 0; i < _skills.Count; i++)
            {
                var s = _skills[i];
                var p = s.proficiency;
                string expBar = p.IsMaxLevel ? "MAX" : $"{p.currentExp}/{p.RequiredExp} ({p.Progress * 100:F0}%)";
                Debug.Log($"  [{i}] {s.data.skillName} ({s.data.skillType})" +
                          $" | Lv.{p.level} EXP:{expBar}" +
                          $" | DMG:{s.TotalDamage:F1} CD:{s.TotalCooldown:F1}s MP:{s.TotalMpCost:F0}" +
                          $" | Sockets:{s.AttachedGemCount}/{s.UnlockedSocketCount}/{s.data.maxGemSockets}");

                // 잼 소켓 상세
                for (int j = 0; j < s.gemSockets.Length; j++)
                {
                    var gs = s.gemSockets[j];
                    string status = !gs.isUnlocked ? "🔒 Locked"
                                  : gs.HasGem ? $"💎 {gs.attachedGem.itemName}"
                                  : "○ Empty";
                    Debug.Log($"       Socket[{j}]: {status}");
                }
            }
            Debug.Log("═══════════════════════════════════════");
        }

        [ContextMenu("Debug: Learn All Skills From Database")]
        public void DebugLearnAllSkills()
        {
            if (skillDatabase == null)
            {
                Debug.LogWarning("[Skill] No SkillDatabase assigned.");
                return;
            }
            foreach (var data in skillDatabase.allSkills)
            {
                if (data != null)
                    LearnSkill(data);
            }
        }

        [ContextMenu("Debug: Use First Skill")]
        public void DebugUseFirstSkill()
        {
            if (_skills.Count == 0)
            {
                Debug.LogWarning("[Skill] No skills learned.");
                return;
            }
            UseSkill(_skills[0]);
        }

        [ContextMenu("Debug: Add 50 EXP To First Skill")]
        public void DebugAddExpToFirst()
        {
            if (_skills.Count == 0)
            {
                Debug.LogWarning("[Skill] No skills learned.");
                return;
            }
            AddExp(_skills[0], 50);
        }

        [ContextMenu("Debug: Max Level First Skill")]
        public void DebugMaxLevelFirst()
        {
            if (_skills.Count == 0) return;
            AddExp(_skills[0], 99999);
        }
    }
}
