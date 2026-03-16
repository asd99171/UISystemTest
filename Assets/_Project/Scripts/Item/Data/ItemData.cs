using UnityEngine;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 모든 아이템의 베이스 ScriptableObject.
    /// 공통 필드: ID, 이름, 아이콘, 설명, 타입, 등급, 스택 관련.
    /// 서브클래스에서 무기/방어구/잼 등 전용 필드를 추가한다.
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("고유 아이템 ID (전체 시스템에서 유일)")]
        public string itemId;

        [Tooltip("아이템 이름")]
        public string itemName;

        [Tooltip("인벤토리 아이콘")]
        public Sprite icon;

        [Tooltip("아이템 종류")]
        public ItemType itemType;

        [Tooltip("아이템 등급")]
        public Rarity rarity;

        [Header("스택")]
        [Tooltip("스택 가능 여부")]
        public bool isStackable;

        [Tooltip("최대 스택 수 (스택 불가 시 1)")]
        [Min(1)]
        public int maxStack = 1;

        [Header("설명")]
        [Tooltip("짧은 한 줄 설명 (툴팁 상단)")]
        public string shortDescription;

        [Tooltip("상세 설명 (툴팁 하단)")]
        [TextArea(2, 5)]
        public string detailDescription;

        [Header("기타")]
        [Tooltip("판매 가격 (0이면 판매 불가)")]
        public int sellPrice;

        /// <summary>
        /// 등급별 색상 반환 (툴팁용)
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                Rarity.Common    => Color.white,
                Rarity.Uncommon  => Color.green,
                Rarity.Rare      => new Color(0.2f, 0.6f, 1f),  // 파랑
                Rarity.Epic      => new Color(0.7f, 0.3f, 1f),  // 보라
                Rarity.Legendary => new Color(1f, 0.65f, 0f),   // 주황
                _ => Color.white
            };
        }
    }
}
