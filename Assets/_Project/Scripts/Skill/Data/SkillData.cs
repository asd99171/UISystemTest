using UnityEngine;
using RPGSystem.Stat;

namespace RPGSystem.Skill.Data
{
    /// <summary>
    /// 스킬 기본 데이터 (ScriptableObject).
    /// 스킬의 불변 정보: 이름, 아이콘, 기본 데미지, 쿨다운 등.
    /// 숙련도 레벨별 잼 소켓 해금 테이블도 여기에 정의한다.
    /// </summary>
    [CreateAssetMenu(fileName = "New Skill", menuName = "RPG System/Skills/Skill")]
    public class SkillData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("고유 스킬 ID")]
        public string skillId;

        [Tooltip("스킬 이름")]
        public string skillName;

        [Tooltip("스킬 아이콘")]
        public Sprite icon;

        [Header("설명")]
        [Tooltip("짧은 설명 (툴팁 상단)")]
        public string shortDescription;

        [Tooltip("상세 설명")]
        [TextArea(2, 5)]
        public string detailDescription;

        [Header("스킬 스탯")]
        [Tooltip("기본 데미지")]
        public float baseDamage;

        [Tooltip("쿨다운 (초)")]
        public float cooldown;

        [Tooltip("마나 소모량")]
        public float mpCost;

        [Header("스킬 스탯 보너스")]
        [Tooltip("이 스킬이 기본으로 제공하는 스탯 효과")]
        public StatModifier[] baseStatEffects;

        [Header("잼 소켓 설정")]
        [Tooltip("최대 잼 소켓 수 (숙련도로 해금)")]
        [Range(0, 5)]
        public int maxGemSockets = 3;

        [Tooltip("각 소켓이 열리는 숙련도 레벨 (배열 크기 = maxGemSockets)")]
        public int[] socketUnlockLevels;

        [Header("숙련도 설정")]
        [Tooltip("최대 숙련도 레벨")]
        public int maxProficiencyLevel = 10;

        [Tooltip("레벨당 기본 필요 경험치 (레벨 * 이 값 = 필요 경험치)")]
        public int baseExpPerLevel = 100;

        [Tooltip("레벨당 데미지 증가율 (0.05 = 5%)")]
        public float damageScalePerLevel = 0.05f;

        /// <summary>
        /// 특정 숙련도 레벨에서 열리는 소켓 수 계산
        /// </summary>
        public int GetUnlockedSocketCount(int proficiencyLevel)
        {
            if (socketUnlockLevels == null || socketUnlockLevels.Length == 0)
                return 0;

            int count = 0;
            for (int i = 0; i < socketUnlockLevels.Length; i++)
            {
                if (proficiencyLevel >= socketUnlockLevels[i])
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 특정 레벨에서의 필요 경험치 계산
        /// </summary>
        public int GetRequiredExp(int level)
        {
            return baseExpPerLevel * (level + 1);
        }

        /// <summary>
        /// 특정 레벨에서의 실제 데미지 계산
        /// </summary>
        public float GetDamageAtLevel(int level)
        {
            return baseDamage * (1f + damageScalePerLevel * level);
        }
    }
}
