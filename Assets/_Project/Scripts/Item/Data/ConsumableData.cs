using UnityEngine;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 소모 효과 종류
    /// </summary>
    public enum ConsumableEffect
    {
        RestoreHP,      // HP 회복
        RestoreMP,      // MP 회복
        BuffATK,        // 공격력 버프
        BuffDEF,        // 방어력 버프
        BuffSPD,        // 이동속도 버프
        Custom          // 커스텀 (코드로 처리)
    }

    /// <summary>
    /// 소모형 아이템 데이터.
    /// 물약, 음식, 버프 스크롤 등.
    /// </summary>
    [CreateAssetMenu(fileName = "New Consumable", menuName = "RPG System/Items/Consumable")]
    public class ConsumableData : ItemData
    {
        [Header("소모 효과")]
        [Tooltip("사용 시 효과 종류")]
        public ConsumableEffect effectType;

        [Tooltip("효과 수치 (HP 50 회복, ATK 10% 증가 등)")]
        public float effectValue;

        [Tooltip("버프 지속시간 (초). 즉시 효과면 0")]
        public float duration;

        [Header("쿨다운")]
        [Tooltip("재사용 대기시간 (초)")]
        public float cooldown;

        private void OnValidate()
        {
            itemType = ItemType.Consumable;
            isStackable = true;
            if (maxStack < 1) maxStack = 99;
        }
    }
}
