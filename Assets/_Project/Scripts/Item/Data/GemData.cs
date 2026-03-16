using UnityEngine;
using RPGSystem.Stat;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 잼 등급 (소켓 호환성 확장용)
    /// </summary>
    public enum GemTier
    {
        Tier1,  // 기본
        Tier2,  // 중급
        Tier3   // 고급
    }

    /// <summary>
    /// 잼 아이템 데이터.
    /// 스킬에 장착하여 스킬 효과를 변경/강화한다.
    /// </summary>
    [CreateAssetMenu(fileName = "New Gem", menuName = "RPG System/Items/Gem")]
    public class GemData : ItemData
    {
        [Header("잼 정보")]
        [Tooltip("잼 등급")]
        public GemTier gemTier;

        [Header("잼 효과")]
        [Tooltip("이 잼이 장착된 스킬에 적용되는 스탯 보너스")]
        public StatModifier[] statModifiers;

        [Tooltip("스킬 데미지 배율 보너스 (0.1 = +10%)")]
        public float skillDamageBonus;

        [Tooltip("스킬 쿨다운 감소 (초)")]
        public float cooldownReduction;

        [Header("잼 설명")]
        [Tooltip("잼 효과 요약 (툴팁용)")]
        public string effectSummary;

        private void OnValidate()
        {
            itemType = ItemType.Gem;
            isStackable = true;
            if (maxStack < 1) maxStack = 99;
        }
    }
}
