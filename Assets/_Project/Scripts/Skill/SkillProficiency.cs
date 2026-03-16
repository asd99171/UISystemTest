using System;
using RPGSystem.Skill.Data;

namespace RPGSystem.Skill
{
    /// <summary>
    /// 스킬 숙련도 데이터.
    /// 현재 레벨, 경험치, 레벨업 판정을 관리한다.
    /// SkillInstance에 포함되어 각 스킬의 숙련도를 추적한다.
    /// </summary>
    [Serializable]
    public class SkillProficiency
    {
        /// <summary>현재 숙련도 레벨</summary>
        public int level;

        /// <summary>현재 레벨에서 누적된 경험치</summary>
        public int currentExp;

        /// <summary>참조하는 스킬 데이터 (레벨업 테이블용)</summary>
        private SkillData _skillData;

        public SkillProficiency(SkillData skillData)
        {
            _skillData = skillData;
            level = 0;
            currentExp = 0;
        }

        /// <summary>현재 레벨에서 다음 레벨까지 필요한 경험치</summary>
        public int RequiredExp => _skillData.GetRequiredExp(level);

        /// <summary>최대 레벨에 도달했는지</summary>
        public bool IsMaxLevel => level >= _skillData.maxProficiencyLevel;

        /// <summary>경험치 진행률 (0~1, UI 프로그레스바용)</summary>
        public float Progress => IsMaxLevel ? 1f : (float)currentExp / RequiredExp;

        /// <summary>현재 레벨에서 해금된 잼 소켓 수</summary>
        public int UnlockedSocketCount => _skillData.GetUnlockedSocketCount(level);

        /// <summary>
        /// 경험치 추가. 레벨업 발생 시 true 반환.
        /// 남은 경험치는 자동으로 다음 레벨에 이월된다.
        /// </summary>
        public bool AddExp(int amount)
        {
            if (IsMaxLevel)
                return false;

            currentExp += amount;
            bool leveledUp = false;

            while (!IsMaxLevel && currentExp >= RequiredExp)
            {
                currentExp -= RequiredExp;
                level++;
                leveledUp = true;
            }

            if (IsMaxLevel)
                currentExp = 0;

            return leveledUp;
        }

        /// <summary>SkillData 참조 복원 (저장/불러오기 후)</summary>
        public void SetSkillData(SkillData skillData)
        {
            _skillData = skillData;
        }
    }
}
