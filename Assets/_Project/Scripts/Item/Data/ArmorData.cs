using UnityEngine;
using RPGSystem.Equipment;
using RPGSystem.Stat;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 갑옷(방어구) 아이템 데이터.
    /// 투구, 갑옷, 바지, 신발 모두 이 클래스를 사용한다.
    /// equipSlot 필드로 어떤 슬롯에 장착되는지 구분한다.
    /// </summary>
    [CreateAssetMenu(fileName = "New Armor", menuName = "RPG System/Items/Armor")]
    public class ArmorData : ItemData
    {
        [Header("장비 슬롯")]
        [Tooltip("이 방어구가 장착되는 슬롯 (Helmet, Chest, Legs, Boots, Ring1, Ring2)")]
        public EquipSlotType equipSlot;

        [Header("방어구 스탯")]
        [Tooltip("기본 방어력")]
        public float defense;

        [Header("추가 스탯")]
        [Tooltip("이 방어구가 제공하는 추가 스탯 보너스")]
        public StatModifier[] statModifiers;

        private void OnValidate()
        {
            itemType = ItemType.Armor;
            isStackable = false;
            maxStack = 1;
        }
    }
}
