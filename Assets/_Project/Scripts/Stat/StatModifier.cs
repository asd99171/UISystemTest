using System;
using UnityEngine;

namespace RPGSystem.Stat
{
    /// <summary>
    /// 스탯 변경치 하나를 표현하는 직렬화 가능한 구조체.
    /// 장비, 잼, 버프 등에서 제공하는 개별 스탯 보너스.
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("변경할 스탯 종류")]
        public StatType statType;

        [Tooltip("연산 방식 (고정값/퍼센트)")]
        public ModifierType modifierType;

        [Tooltip("값 (Flat: +10, Percent: 0.1 = 10%)")]
        public float value;

        public StatModifier(StatType statType, ModifierType modifierType, float value)
        {
            this.statType = statType;
            this.modifierType = modifierType;
            this.value = value;
        }

        /// <summary>
        /// 툴팁 표시용 문자열
        /// </summary>
        public string ToDisplayString()
        {
            string sign = value >= 0 ? "+" : "";
            if (modifierType == ModifierType.Percent)
                return $"{statType} {sign}{value * 100f:0.#}%";
            return $"{statType} {sign}{value:0.#}";
        }
    }
}
