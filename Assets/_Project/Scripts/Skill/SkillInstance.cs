using System;
using System.Collections.Generic;
using RPGSystem.Gem;
using RPGSystem.Item.Data;
using RPGSystem.Skill.Data;
using RPGSystem.Stat;

namespace RPGSystem.Skill
{
    /// <summary>
    /// 런타임 스킬 인스턴스.
    /// SkillData(불변)를 참조하고, 숙련도 + 잼 소켓 등 가변 상태를 가진다.
    /// 캐릭터가 보유한 실제 스킬 하나를 표현한다.
    /// </summary>
    [Serializable]
    public class SkillInstance
    {
        /// <summary>런타임 고유 ID</summary>
        public string uid;

        /// <summary>원본 스킬 데이터 (ScriptableObject 참조)</summary>
        public SkillData data;

        /// <summary>숙련도 데이터</summary>
        public SkillProficiency proficiency;

        /// <summary>잼 소켓 배열 (크기 = data.maxGemSockets)</summary>
        public GemSocket[] gemSockets;

        public SkillInstance(SkillData data)
        {
            this.uid = Guid.NewGuid().ToString();
            this.data = data;
            this.proficiency = new SkillProficiency(data);

            // 소켓 배열 초기화 (모두 잠김 상태)
            gemSockets = new GemSocket[data.maxGemSockets];
            for (int i = 0; i < data.maxGemSockets; i++)
            {
                gemSockets[i] = new GemSocket(i);
            }

            // 초기 해금 상태 반영
            RefreshSocketUnlocks();
        }

        // ──────────────────────────────────────
        // 데미지 / 쿨다운 계산
        // ──────────────────────────────────────

        /// <summary>현재 숙련도 레벨 기준 실제 데미지</summary>
        public float CurrentDamage => data.GetDamageAtLevel(proficiency.level);

        /// <summary>잼 효과 포함 최종 데미지</summary>
        public float TotalDamage
        {
            get
            {
                float damage = CurrentDamage;
                float bonusMultiplier = 0f;

                foreach (var socket in gemSockets)
                {
                    if (socket.HasGem)
                        bonusMultiplier += socket.attachedGem.skillDamageBonus;
                }

                return damage * (1f + bonusMultiplier);
            }
        }

        /// <summary>잼 효과 포함 최종 쿨다운</summary>
        public float TotalCooldown
        {
            get
            {
                float cd = data.cooldown;
                foreach (var socket in gemSockets)
                {
                    if (socket.HasGem)
                        cd -= socket.attachedGem.cooldownReduction;
                }
                return cd < 0.1f ? 0.1f : cd; // 최소 0.1초
            }
        }

        /// <summary>잼 효과 포함 최종 MP 소모량</summary>
        public float TotalMpCost
        {
            get
            {
                // 잼에 의한 MP 감소 확장 가능
                return data.mpCost;
            }
        }

        // ──────────────────────────────────────
        // 잼 스탯 수집 (스킬 효과에 반영)
        // ──────────────────────────────────────

        /// <summary>
        /// 장착된 모든 잼의 StatModifier를 수집.
        /// 버프/패시브 스킬의 효과 계산 시 사용.
        /// </summary>
        public List<StatModifier> GetGemStatModifiers()
        {
            var modifiers = new List<StatModifier>();
            foreach (var socket in gemSockets)
            {
                if (socket.HasGem && socket.attachedGem.statModifiers != null)
                    modifiers.AddRange(socket.attachedGem.statModifiers);
            }
            return modifiers;
        }

        /// <summary>
        /// 스킬 기본 스탯 효과 + 잼 스탯 효과 합산.
        /// Buff/Passive 스킬이 적용하는 전체 스탯 효과.
        /// </summary>
        public List<StatModifier> GetTotalStatEffects()
        {
            var effects = new List<StatModifier>();
            if (data.baseStatEffects != null)
                effects.AddRange(data.baseStatEffects);
            effects.AddRange(GetGemStatModifiers());
            return effects;
        }

        // ──────────────────────────────────────
        // 소켓 관리
        // ──────────────────────────────────────

        /// <summary>해금된 소켓 수</summary>
        public int UnlockedSocketCount
        {
            get
            {
                int count = 0;
                foreach (var socket in gemSockets)
                {
                    if (socket.isUnlocked) count++;
                }
                return count;
            }
        }

        /// <summary>장착된 잼 수</summary>
        public int AttachedGemCount
        {
            get
            {
                int count = 0;
                foreach (var socket in gemSockets)
                {
                    if (socket.HasGem) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 숙련도 레벨 변경 시 소켓 해금 상태 갱신.
        /// </summary>
        public void RefreshSocketUnlocks()
        {
            int unlockedCount = proficiency.UnlockedSocketCount;
            for (int i = 0; i < gemSockets.Length; i++)
            {
                gemSockets[i].isUnlocked = i < unlockedCount;
            }
        }

        /// <summary>
        /// 경험치 추가 + 레벨업 시 소켓 갱신.
        /// 레벨업 발생 여부 반환.
        /// </summary>
        public bool AddProficiencyExp(int amount)
        {
            bool leveledUp = proficiency.AddExp(amount);
            if (leveledUp)
                RefreshSocketUnlocks();
            return leveledUp;
        }
    }
}
