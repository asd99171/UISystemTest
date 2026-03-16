using RPGSystem.Equipment;
using RPGSystem.Inventory;
using RPGSystem.Item;
using RPGSystem.Item.Data;
using RPGSystem.Skill;

namespace RPGSystem.Core
{
    /// <summary>
    /// 시스템 이벤트 정의.
    /// UI에서 이 이벤트들을 구독하여 화면을 갱신한다.
    /// </summary>

    // ──────────────── 인벤토리 이벤트 ────────────────

    /// <summary>인벤토리 슬롯 내용이 변경됨 (아이템 추가/제거/이동/수량변경)</summary>
    public struct InventoryChangedEvent
    {
        public int slotIndex;
        public InventorySlot slot;
    }

    /// <summary>인벤토리 전체가 갱신되어야 함 (정렬, 초기화 등)</summary>
    public struct InventoryRefreshEvent { }

    /// <summary>아이템 사용됨</summary>
    public struct ItemUsedEvent
    {
        public ItemInstance item;
    }

    /// <summary>아이템 추가 실패 (인벤토리 가득 참)</summary>
    public struct InventoryFullEvent
    {
        public ItemData attemptedItem;
        public int attemptedAmount;
    }

    // ──────────────── 장비 이벤트 ────────────────

    /// <summary>장비 아이템의 장착이 요청됨 (인벤토리 → 장비 시스템)</summary>
    public struct EquipRequestEvent
    {
        public ItemInstance item;
    }

    /// <summary>특정 슬롯에 직접 장착 요청 (드래그 앤 드롭 등)</summary>
    public struct EquipToSlotRequestEvent
    {
        public ItemInstance item;
        public EquipSlotType targetSlot;
    }

    /// <summary>장비가 장착/해제됨 → PlayerStatManager가 구독하여 재계산</summary>
    public struct EquipmentChangedEvent { }

    // ──────────────── 스탯 이벤트 ────────────────

    /// <summary>최종 스탯이 변경됨 (장비 변경, 버프 등) → UI가 구독하여 표시 갱신</summary>
    public struct StatChangedEvent { }

    // ──────────────── 스킬 이벤트 ────────────────

    /// <summary>새로운 스킬을 습득함</summary>
    public struct SkillLearnedEvent
    {
        public SkillInstance skill;
    }

    /// <summary>스킬 목록이 변경됨 (습득/삭제)</summary>
    public struct SkillListChangedEvent { }

    /// <summary>스킬이 사용됨 (전투 시스템이 구독하여 데미지/효과 적용)</summary>
    public struct SkillUsedEvent
    {
        public SkillInstance skill;
        public float damage;
        public float cooldown;
        public float mpCost;
    }

    /// <summary>스킬 숙련도 레벨업 발생</summary>
    public struct SkillLevelUpEvent
    {
        public SkillInstance skill;
        public int newLevel;
        public int newSocketsUnlocked;
    }

    // ──────────────── 잼 이벤트 ────────────────

    /// <summary>잼이 스킬 소켓에 장착됨</summary>
    public struct GemAttachedEvent
    {
        public SkillInstance skill;
        public int socketIndex;
        public GemData gem;
    }

    /// <summary>잼이 스킬 소켓에서 해제됨</summary>
    public struct GemDetachedEvent
    {
        public SkillInstance skill;
        public int socketIndex;
        public GemData gem;
    }

    // ──────────────── 게임 상태 / UI 이벤트 ────────────────

    /// <summary>게임 상태가 변경됨 (Playing/UI/Paused)</summary>
    public struct GameStateChangedEvent
    {
        public GameState previousState;
        public GameState newState;
    }

    /// <summary>UI 패널이 열림</summary>
    public struct UIPanelOpenedEvent
    {
        public string panelName;
    }

    /// <summary>UI 패널이 닫힘</summary>
    public struct UIPanelClosedEvent
    {
        public string panelName;
    }
}
