using System.Collections.Generic;
using UnityEngine;

namespace RPGSystem.Item.Data
{
    /// <summary>
    /// 전체 아이템 데이터베이스 (ScriptableObject).
    /// 모든 ItemData를 등록하고 ID로 빠르게 조회할 수 있다.
    /// 프로젝트에 하나만 존재하며, 게임 시작 시 Dictionary로 캐싱한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "RPG System/Database/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [Tooltip("등록된 모든 아이템")]
        public List<ItemData> allItems = new List<ItemData>();

        private Dictionary<string, ItemData> _lookup;

        /// <summary>
        /// ID로 아이템 데이터 조회. 첫 호출 시 자동으로 Dictionary 빌드.
        /// </summary>
        public ItemData GetById(string itemId)
        {
            if (_lookup == null)
                BuildLookup();

            _lookup.TryGetValue(itemId, out var item);
            return item;
        }

        /// <summary>
        /// 특정 타입의 아이템만 필터링
        /// </summary>
        public List<ItemData> GetByType(ItemType type)
        {
            return allItems.FindAll(item => item.itemType == type);
        }

        /// <summary>
        /// Dictionary 빌드 (게임 시작 시 또는 첫 조회 시)
        /// </summary>
        public void BuildLookup()
        {
            _lookup = new Dictionary<string, ItemData>();
            foreach (var item in allItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemId))
                {
                    _lookup[item.itemId] = item;
                }
            }
        }

        private void OnEnable()
        {
            BuildLookup();
        }
    }
}
