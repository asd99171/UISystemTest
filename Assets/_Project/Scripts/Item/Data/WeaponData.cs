using UnityEngine;
using RPGSystem.Stat;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 무기 아이템 데이터.
    /// 공격력, 공격속도, 추가 스탯 보너스, 잼 소켓 수를 정의한다.
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "RPG System/Items/Weapon")]
    public class WeaponData : ItemData
    {
        [Header("무기 스탯")]
        [Tooltip("기본 공격력")]
        public float attackPower;

        [Tooltip("공격속도 (초당 공격 횟수)")]
        public float attackSpeed = 1f;

        [Header("추가 스탯")]
        [Tooltip("이 무기가 제공하는 추가 스탯 보너스")]
        public StatModifier[] statModifiers;

        [Header("잼 소켓")]
        [Tooltip("잼 장착 가능 소켓 수")]
        [Range(0, 3)]
        public int gemSocketCount;

        private void OnValidate()
        {
            itemType = ItemType.Weapon;
            isStackable = false;
            maxStack = 1;
        }
    }
}
