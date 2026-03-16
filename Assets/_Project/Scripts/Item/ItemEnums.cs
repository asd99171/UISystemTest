namespace RPGSystem.Item
{
    /// <summary>
    /// 아이템 종류
    /// </summary>
    public enum ItemType
    {
        Consumable,     // 소모형
        Armor,          // 갑옷 (투구, 갑옷, 바지, 신발 포함)
        Weapon,         // 무기
        SubWeapon,      // 보조무기
        Gem,            // 잼
        KeyItem         // 중요아이템
    }

    /// <summary>
    /// 아이템 등급
    /// </summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
