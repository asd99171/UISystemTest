using UnityEngine;
using RPGSystem.Stat;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 보조무기 아이템 데이터.
    /// OffHand 슬롯에 장착된다. 방패, 부적, 서브웨폰 등.
    /// </summary>
    [CreateAssetMenu(fileName = "New SubWeapon", menuName = "RPG System/Items/SubWeapon")]
    public class SubWeaponData : ItemData
    {
        [Header("보조무기 스탯")]
        [Tooltip("추가 방어력 (방패 등)")]
        public float defense;

        [Tooltip("추가 공격력 (서브웨폰 등)")]
        public float attackPower;

        [Header("추가 스탯")]
        [Tooltip("이 보조무기가 제공하는 추가 스탯 보너스")]
        public StatModifier[] statModifiers;

        private void OnValidate()
        {
            itemType = ItemType.SubWeapon;
            isStackable = false;
            maxStack = 1;
        }
    }
}
