using UnityEngine;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 중요 아이템 데이터.
    /// 퀘스트, 스토리 진행에 필요한 특수 아이템.
    /// 판매/버리기 불가.
    /// </summary>
    [CreateAssetMenu(fileName = "New KeyItem", menuName = "RPG System/Items/Key Item")]
    public class KeyItemData : ItemData
    {
        [Header("중요 아이템")]
        [Tooltip("연관된 퀘스트 ID (없으면 빈 문자열)")]
        public string questId;

        [Tooltip("사용 가능 여부 (일부 중요 아이템은 사용 가능)")]
        public bool isUsable;

        private void OnValidate()
        {
            itemType = ItemType.KeyItem;
            isStackable = false;
            maxStack = 1;
            sellPrice = 0; // 중요 아이템은 판매 불가
        }
    }
}
