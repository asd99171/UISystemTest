using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPGSystem.Item;
using RPGSystem.Item.Data;
using RPGSystem.Skill;
using RPGSystem.Stat;

namespace RPGSystem.UI
{
    /// <summary>
    /// 툴팁 UI.
    /// 아이템/스킬 정보를 마우스 위치에 팝업으로 표시한다.
    ///
    /// [GameObject] Canvas 아래 "Tooltip" 오브젝트에 부착.
    /// 최상위 렌더 순서를 위해 별도 Canvas 또는 높은 Sort Order 권장.
    /// [Inspector] 하위 TMP_Text들과 Image를 연결.
    /// 기본 상태: SetActive(false)
    /// </summary>
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        [Header("레이아웃")]
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("아이템 정보")]
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemTypeText;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statsText;

        [Header("설정")]
        [Tooltip("마우스로부터의 오프셋")]
        [SerializeField] private Vector2 offset = new Vector2(16f, -16f);

        private Canvas _parentCanvas;
        private RectTransform _canvasRect;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas != null)
                _canvasRect = _parentCanvas.GetComponent<RectTransform>();

            Hide();
        }

        // ──────────────────────────────────────
        // 아이템 툴팁
        // ──────────────────────────────────────

        /// <summary>아이템 툴팁 표시</summary>
        public void ShowItem(ItemInstance item)
        {
            if (item == null || item.data == null) return;

            var data = item.data;

            SetName(data.itemName, data.GetRarityColor());
            SetType($"{data.itemType} · {data.rarity}");
            SetIcon(data.icon);
            SetDescription(data.shortDescription);
            SetStats(BuildItemStats(data));

            gameObject.SetActive(true);
        }

        /// <summary>스킬 툴팁 표시</summary>
        public void ShowSkill(SkillInstance skill)
        {
            if (skill == null || skill.data == null) return;

            var data = skill.data;

            SetName(data.skillName, Color.white);
            SetType($"{data.skillType} Skill · Lv.{skill.proficiency.level}");
            SetIcon(data.icon);

            string desc = $"Damage: {skill.TotalDamage:F1}\n" +
                          $"Cooldown: {skill.TotalCooldown:F1}s\n" +
                          $"MP Cost: {skill.TotalMpCost:F0}";
            SetDescription(desc);

            string expInfo = skill.proficiency.IsMaxLevel
                ? "Proficiency: MAX"
                : $"EXP: {skill.proficiency.currentExp}/{skill.proficiency.RequiredExp}\n" +
                  $"Sockets: {skill.AttachedGemCount}/{skill.UnlockedSocketCount}/{skill.data.maxGemSockets}";
            SetStats(expInfo);

            gameObject.SetActive(true);
        }

        /// <summary>툴팁 숨기기</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ──────────────────────────────────────
        // 위치 업데이트
        // ──────────────────────────────────────

        private void Update()
        {
            if (tooltipRect == null) return;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 targetPos = mousePos + offset;

            // 화면 밖으로 나가지 않도록 클램프
            if (_canvasRect != null)
            {
                float maxX = Screen.width - tooltipRect.sizeDelta.x;
                float minY = tooltipRect.sizeDelta.y;

                if (targetPos.x > maxX)
                    targetPos.x = mousePos.x - offset.x - tooltipRect.sizeDelta.x;
                if (targetPos.y < minY)
                    targetPos.y = mousePos.y - offset.y + tooltipRect.sizeDelta.y;
            }

            tooltipRect.position = targetPos;
        }

        // ──────────────────────────────────────
        // 내부 헬퍼
        // ──────────────────────────────────────

        private void SetName(string name, Color color)
        {
            if (itemNameText == null) return;
            itemNameText.text = name;
            itemNameText.color = color;
        }

        private void SetType(string type)
        {
            if (itemTypeText == null) return;
            itemTypeText.text = type;
        }

        private void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        private void SetDescription(string desc)
        {
            if (descriptionText == null) return;
            descriptionText.text = desc;
            descriptionText.enabled = !string.IsNullOrEmpty(desc);
        }

        private void SetStats(string stats)
        {
            if (statsText == null) return;
            statsText.text = stats;
            statsText.enabled = !string.IsNullOrEmpty(stats);
        }

        private string BuildItemStats(ItemData data)
        {
            var sb = new System.Text.StringBuilder();

            switch (data)
            {
                case WeaponData weapon:
                    sb.AppendLine($"ATK: {weapon.attackPower}");
                    sb.AppendLine($"Attack Speed: {weapon.attackSpeed}");
                    AppendModifiers(sb, weapon.statModifiers);
                    if (weapon.gemSocketCount > 0)
                        sb.AppendLine($"Gem Sockets: {weapon.gemSocketCount}");
                    break;

                case ArmorData armor:
                    sb.AppendLine($"DEF: {armor.defense}");
                    sb.AppendLine($"Slot: {armor.equipSlot}");
                    AppendModifiers(sb, armor.statModifiers);
                    break;

                case SubWeaponData sub:
                    if (sub.attackPower > 0) sb.AppendLine($"ATK: {sub.attackPower}");
                    if (sub.defense > 0) sb.AppendLine($"DEF: {sub.defense}");
                    AppendModifiers(sb, sub.statModifiers);
                    break;

                case ConsumableData consumable:
                    sb.AppendLine($"Effect: {consumable.effectType}");
                    sb.AppendLine($"Value: {consumable.effectValue}");
                    if (consumable.duration > 0) sb.AppendLine($"Duration: {consumable.duration}s");
                    break;

                case GemData gem:
                    sb.AppendLine($"Tier: {gem.gemTier}");
                    if (gem.skillDamageBonus > 0) sb.AppendLine($"Skill DMG: +{gem.skillDamageBonus * 100:F0}%");
                    if (gem.cooldownReduction > 0) sb.AppendLine($"CD Reduction: -{gem.cooldownReduction:F1}s");
                    AppendModifiers(sb, gem.statModifiers);
                    break;
            }

            if (data.sellPrice > 0)
                sb.AppendLine($"Sell: {data.sellPrice}G");

            return sb.ToString().TrimEnd();
        }

        private void AppendModifiers(System.Text.StringBuilder sb, StatModifier[] modifiers)
        {
            if (modifiers == null) return;
            foreach (var mod in modifiers)
            {
                sb.AppendLine(mod.ToDisplayString());
            }
        }
    }
}
