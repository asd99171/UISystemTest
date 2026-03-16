using System.Collections.Generic;
using UnityEngine;

namespace RPGSystem.Skill.Data
{
    /// <summary>
    /// 전체 스킬 데이터베이스 (ScriptableObject).
    /// 모든 SkillData를 등록하고 ID로 빠르게 조회할 수 있다.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "RPG System/Database/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        [Tooltip("등록된 모든 스킬")]
        public List<SkillData> allSkills = new List<SkillData>();

        private Dictionary<string, SkillData> _lookup;

        /// <summary>
        /// ID로 스킬 데이터 조회
        /// </summary>
        public SkillData GetById(string skillId)
        {
            if (_lookup == null)
                BuildLookup();

            _lookup.TryGetValue(skillId, out var skill);
            return skill;
        }

        public void BuildLookup()
        {
            _lookup = new Dictionary<string, SkillData>();
            foreach (var skill in allSkills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.skillId))
                {
                    _lookup[skill.skillId] = skill;
                }
            }
        }

        private void OnEnable()
        {
            BuildLookup();
        }
    }
}
