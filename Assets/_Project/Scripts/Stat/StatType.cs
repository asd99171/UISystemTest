namespace RPGSystem.Stat
{
    /// <summary>
    /// 스탯 종류
    /// </summary>
    public enum StatType
    {
        ATK,            // 공격력
        DEF,            // 방어력
        HP,             // 체력
        MP,             // 마나
        SPD,            // 이동속도
        AttackSpeed,    // 공격속도
        CritRate,       // 치명타 확률
        CritDamage      // 치명타 데미지
    }

    /// <summary>
    /// 스탯 연산 방식
    /// </summary>
    public enum ModifierType
    {
        Flat,       // 고정값 (+10)
        Percent     // 퍼센트 (+10%)
    }
}
