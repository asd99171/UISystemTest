using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPGSystem.Equipment;
using RPGSystem.Item;
using RPGSystem.Item.Data;
using RPGSystem.Skill;
using RPGSystem.Stat;

namespace RPGSystem.UI
{
    /// <summary>
    /// 아이템/스킬 툴팁 UI.
    /// 마우스 호버 시 아이템 정보를 리치 텍스트로 표시하고,
    /// 장비 아이템이면 현재 장착 아이템과 비교 수치를 표시한다.
    ///
    /// 구조:
    /// - 메인 툴팁 (hovering 아이템 정보)
    /// - 비교 툴팁 (현재 장착 아이템 정보, 장비에만 표시)
    ///
    /// 위치:
    /// - 마우스 우하단에 기본 배치
    /// - 화면 밖 튀는 경우 자동 보정 (4방향)
    /// - PointerMove로 실시간 추적
    ///
    /// [GameObject] Canvas 아래 "Tooltip" 오브젝트에 부착.
    /// 최상위 렌더를 위해 Sort Order를 높게 설정하거나 별도 Canvas 사용.
    /// 기본 상태: SetActive(false)
    /// </summary>
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        // ──────────────────────────────────────
        // 메인 툴팁
        // ──────────────────────────────────────

        [Header("메인 툴팁 패널")]
        [Tooltip("메인 툴팁 전체 RectTransform (위치 이동용)")]
        [SerializeField] private RectTransform mainTooltipRect;

        [Tooltip("아이템 이름")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("아이템 타입 + 등급 라인")]
        [SerializeField] private TMP_Text typeText;

        [Tooltip("아이콘 이미지")]
        [SerializeField] private Image iconImage;

        [Tooltip("장착 부위 라인 (장비 전용, 비장비면 숨김)")]
        [SerializeField] private TMP_Text equipSlotText;

        [Tooltip("핵심 스탯 영역 (ATK, DEF 등 주요 수치)")]
        [SerializeField] private TMP_Text primaryStatsText;

        [Tooltip("추가 스탯 모디파이어 영역")]
        [SerializeField] private TMP_Text modifierStatsText;

        [Tooltip("특수 효과 / 잼 효과 영역")]
        [SerializeField] private TMP_Text specialEffectText;

        [Tooltip("짧은 설명")]
        [SerializeField] private TMP_Text descriptionText;

        [Tooltip("상세 설명 (이탤릭 풍)")]
        [SerializeField] private TMP_Text detailDescText;

        [Tooltip("판매 가격")]
        [SerializeField] private TMP_Text sellPriceText;

        // ──────────────────────────────────────
        // 비교 툴팁 (장비 비교용)
        // ──────────────────────────────────────

        [Header("비교 툴팁 패널")]
        [Tooltip("비교 툴팁 전체 GameObject (장비가 아니면 비활성)")]
        [SerializeField] private GameObject comparePanel;

        [Tooltip("비교 툴팁 RectTransform")]
        [SerializeField] private RectTransform compareRect;

        [Tooltip("비교 대상 이름 (현재 장착 중)")]
        [SerializeField] private TMP_Text compareNameText;

        [Tooltip("비교 스탯 차이 텍스트")]
        [SerializeField] private TMP_Text compareStatsText;

        [Tooltip("비교 라벨 (\"현재 장착 중\")")]
        [SerializeField] private TMP_Text compareLabelText;

        // ──────────────────────────────────────
        // 설정
        // ──────────────────────────────────────

        [Header("위치 설정")]
        [Tooltip("마우스로부터의 기본 오프셋 (우하단)")]
        [SerializeField] private Vector2 offset = new Vector2(20f, -20f);

        [Tooltip("화면 가장자리 여백 (px)")]
        [SerializeField] private float screenPadding = 8f;

        // ──────────────────────────────────────
        // 내부 상태
        // ──────────────────────────────────────

        private Canvas _rootCanvas;
        private RectTransform _canvasRect;
        private Camera _uiCamera;
        private bool _isShowing;

        /// <summary>현재 마우스 위치 캐시 (PointerMove에서 갱신)</summary>
        private Vector2 _currentMousePos;

        // ──────────────────────────────────────
        // 색상 상수 (TMP Rich Text)
        // ──────────────────────────────────────

        private const string COLOR_POSITIVE = "#00FF88";   // 상승 (녹색)
        private const string COLOR_NEGATIVE = "#FF4444";   // 하락 (빨강)
        private const string COLOR_NEUTRAL  = "#AAAAAA";   // 보조 텍스트 (회색)
        private const string COLOR_EQUIP    = "#FFCC00";   // 장착 부위 (노랑)
        private const string COLOR_SPECIAL  = "#66CCFF";   // 특수 효과 (하늘)
        private const string COLOR_GOLD     = "#FFD700";   // 골드 (금색)

        // ══════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas != null)
            {
                _canvasRect = _rootCanvas.GetComponent<RectTransform>();
                // Overlay Canvas면 camera가 null
                if (_rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    _uiCamera = _rootCanvas.worldCamera;
            }

            Hide();
        }

        // ══════════════════════════════════════
        // Public API — 표시 / 숨기기
        // ══════════════════════════════════════

        /// <summary>
        /// 아이템 툴팁 표시.
        /// 장비 아이템이면 현재 장착 아이템과 비교 패널도 함께 표시한다.
        /// </summary>
        public void ShowItem(ItemInstance item)
        {
            if (item == null || item.data == null) return;

            BuildMainTooltip(item);
            BuildCompareTooltip(item);

            _isShowing = true;
            gameObject.SetActive(true);
            UpdatePosition(_currentMousePos);

            // LayoutRebuilder를 통해 Content Size Fitter 갱신 후 위치 재보정
            ForceRebuildLayout();
        }

        /// <summary>
        /// 스킬 툴팁 표시.
        /// 비교 패널은 숨긴다.
        /// </summary>
        public void ShowSkill(SkillInstance skill)
        {
            if (skill == null || skill.data == null) return;

            BuildSkillTooltip(skill);
            HideComparePanel();

            _isShowing = true;
            gameObject.SetActive(true);
            UpdatePosition(_currentMousePos);
            ForceRebuildLayout();
        }

        /// <summary>툴팁 숨기기</summary>
        public void Hide()
        {
            _isShowing = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 마우스 위치 업데이트 (UISlotBase의 OnPointerMove에서 호출).
        /// Update()에서 Input.mousePosition을 폴링하는 대신,
        /// EventSystem의 PointerMove 이벤트로 정확하게 추적한다.
        /// </summary>
        public void UpdateMousePosition(Vector2 screenPos)
        {
            _currentMousePos = screenPos;
            if (_isShowing)
                UpdatePosition(screenPos);
        }

        // ══════════════════════════════════════
        // 메인 툴팁 빌드
        // ══════════════════════════════════════

        private void BuildMainTooltip(ItemInstance item)
        {
            var data = item.data;

            // ─── 이름 (등급 색상) ───
            SetText(nameText, data.itemName);
            if (nameText != null)
                nameText.color = data.GetRarityColor();

            // ─── 타입 + 등급 ───
            SetText(typeText, $"{data.itemType} · {data.rarity}");

            // ─── 아이콘 ───
            SetIcon(data.icon);

            // ─── 장착 부위 (장비만) ───
            BuildEquipSlotLine(data);

            // ─── 핵심 스탯 ───
            SetText(primaryStatsText, BuildPrimaryStats(data));

            // ─── 추가 모디파이어 ───
            SetText(modifierStatsText, BuildModifierStats(data));

            // ─── 특수 효과 ───
            SetText(specialEffectText, BuildSpecialEffect(data));

            // ─── 설명 ───
            SetText(descriptionText, data.shortDescription);
            SetText(detailDescText, data.detailDescription);

            // ─── 판매 가격 ───
            if (data.sellPrice > 0)
                SetText(sellPriceText, $"<color={COLOR_GOLD}>Sell: {data.sellPrice}G</color>");
            else
                SetText(sellPriceText, null);
        }

        // ──────────────────────────────────────
        // 장착 부위 라인
        // ──────────────────────────────────────

        private void BuildEquipSlotLine(ItemData data)
        {
            string slotName = GetEquipSlotName(data);
            if (slotName != null)
                SetText(equipSlotText, $"<color={COLOR_EQUIP}>Equip: {slotName}</color>");
            else
                SetText(equipSlotText, null);
        }

        private static string GetEquipSlotName(ItemData data)
        {
            return data switch
            {
                WeaponData      => "Weapon",
                SubWeaponData   => "Off Hand",
                ArmorData armor => armor.equipSlot switch
                {
                    EquipSlotType.Helmet => "Helmet",
                    EquipSlotType.Chest  => "Chest",
                    EquipSlotType.Legs   => "Legs",
                    EquipSlotType.Boots  => "Boots",
                    EquipSlotType.Ring1  => "Ring",
                    EquipSlotType.Ring2  => "Ring",
                    _                    => armor.equipSlot.ToString()
                },
                _ => null
            };
        }

        // ──────────────────────────────────────
        // 핵심 스탯
        // ──────────────────────────────────────

        private string BuildPrimaryStats(ItemData data)
        {
            var sb = new StringBuilder();

            switch (data)
            {
                case WeaponData weapon:
                    sb.AppendLine($"ATK  <b>{weapon.attackPower}</b>");
                    sb.AppendLine($"Attack Speed  <b>{weapon.attackSpeed:0.##}</b>");
                    if (weapon.gemSocketCount > 0)
                        sb.AppendLine($"<color={COLOR_SPECIAL}>Gem Sockets: {weapon.gemSocketCount}</color>");
                    break;

                case ArmorData armor:
                    sb.AppendLine($"DEF  <b>{armor.defense}</b>");
                    break;

                case SubWeaponData sub:
                    if (sub.attackPower > 0)
                        sb.AppendLine($"ATK  <b>{sub.attackPower}</b>");
                    if (sub.defense > 0)
                        sb.AppendLine($"DEF  <b>{sub.defense}</b>");
                    break;

                case ConsumableData consumable:
                    sb.AppendLine($"Effect: <b>{FormatConsumableEffect(consumable)}</b>");
                    if (consumable.duration > 0)
                        sb.AppendLine($"Duration: <b>{consumable.duration}s</b>");
                    if (consumable.cooldown > 0)
                        sb.AppendLine($"<color={COLOR_NEUTRAL}>Cooldown: {consumable.cooldown}s</color>");
                    break;

                case GemData gem:
                    sb.AppendLine($"Tier: <b>{gem.gemTier}</b>");
                    if (gem.skillDamageBonus > 0)
                        sb.AppendLine($"<color={COLOR_SPECIAL}>Skill DMG +{gem.skillDamageBonus * 100:F0}%</color>");
                    if (gem.cooldownReduction > 0)
                        sb.AppendLine($"<color={COLOR_SPECIAL}>CD Reduction -{gem.cooldownReduction:F1}s</color>");
                    break;
            }

            return TrimResult(sb);
        }

        private static string FormatConsumableEffect(ConsumableData c)
        {
            return c.effectType switch
            {
                ConsumableEffect.RestoreHP => $"HP +{c.effectValue:0}",
                ConsumableEffect.RestoreMP => $"MP +{c.effectValue:0}",
                ConsumableEffect.BuffATK   => $"ATK +{c.effectValue:0.#}%",
                ConsumableEffect.BuffDEF   => $"DEF +{c.effectValue:0.#}%",
                ConsumableEffect.BuffSPD   => $"SPD +{c.effectValue:0.#}%",
                ConsumableEffect.Custom    => "Special",
                _                          => c.effectType.ToString()
            };
        }

        // ──────────────────────────────────────
        // 추가 스탯 모디파이어
        // ──────────────────────────────────────

        private string BuildModifierStats(ItemData data)
        {
            StatModifier[] modifiers = data switch
            {
                WeaponData weapon       => weapon.statModifiers,
                ArmorData armor         => armor.statModifiers,
                SubWeaponData sub       => sub.statModifiers,
                GemData gem             => gem.statModifiers,
                _                       => null
            };

            if (modifiers == null || modifiers.Length == 0)
                return null;

            var sb = new StringBuilder();
            foreach (var mod in modifiers)
            {
                string color = mod.value >= 0 ? COLOR_POSITIVE : COLOR_NEGATIVE;
                sb.AppendLine($"<color={color}>{mod.ToDisplayString()}</color>");
            }
            return TrimResult(sb);
        }

        // ──────────────────────────────────────
        // 특수 효과 (확장 영역)
        // ──────────────────────────────────────

        private string BuildSpecialEffect(ItemData data)
        {
            // GemData의 effectSummary 또는 향후 세트 효과 등
            if (data is GemData gem && !string.IsNullOrEmpty(gem.effectSummary))
                return $"<color={COLOR_SPECIAL}>{gem.effectSummary}</color>";

            // KeyItem 특수 표시
            if (data is KeyItemData key)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<color=#FFAAFF>Quest Item</color>");
                if (!key.isUsable)
                    sb.AppendLine($"<color={COLOR_NEUTRAL}>Cannot be used directly</color>");
                return TrimResult(sb);
            }

            return null;
        }

        // ══════════════════════════════════════
        // 비교 툴팁 빌드 (장비 비교)
        // ══════════════════════════════════════

        /// <summary>
        /// 장비 아이템이면, 현재 장착 중인 같은 슬롯 아이템과 비교 패널을 표시한다.
        ///
        /// 비교 수치 예시:
        ///   ATK  45 → 52  (+7)    ← 녹색
        ///   DEF  30 → 25  (-5)    ← 빨강
        ///   CritRate +5% → +3% (-2%)
        /// </summary>
        private void BuildCompareTooltip(ItemInstance newItem)
        {
            var equipMgr = EquipmentManager.Instance;
            if (equipMgr == null)
            {
                HideComparePanel();
                return;
            }

            // 장비 아이템이 아니면 비교 불필요
            EquipSlotType? targetSlot = ResolveTargetSlot(newItem.data);
            if (!targetSlot.HasValue)
            {
                HideComparePanel();
                return;
            }

            // 현재 장착 아이템
            ItemInstance currentEquip = equipMgr.GetEquipped(targetSlot.Value);
            if (currentEquip == null)
            {
                HideComparePanel();
                return;
            }

            // 비교 패널 활성화
            if (comparePanel != null)
                comparePanel.SetActive(true);

            // 비교 헤더
            SetText(compareLabelText, $"<color={COLOR_NEUTRAL}>— Currently Equipped —</color>");
            if (compareNameText != null)
            {
                compareNameText.text = currentEquip.data.itemName;
                compareNameText.color = currentEquip.data.GetRarityColor();
            }

            // 비교 수치
            string diff = BuildComparisonDiff(newItem.data, currentEquip.data);
            SetText(compareStatsText, diff);
        }

        /// <summary>
        /// 새 아이템 vs 현재 장착 아이템의 스탯 차이를 빌드.
        ///
        /// 출력 예시:
        ///   ATK     45 → 52   <color=#00FF88>(+7)</color>
        ///   DEF     30 → 25   <color=#FF4444>(-5)</color>
        ///   HP +5%  →  +3%   <color=#FF4444>(-2%)</color>
        /// </summary>
        private string BuildComparisonDiff(ItemData newData, ItemData currentData)
        {
            var sb = new StringBuilder();

            // 주요 스탯 비교 (ATK/DEF)
            BuildPrimaryComparison(sb, newData, currentData);

            // 모디파이어 비교
            var newMods = GetModifiers(newData);
            var curMods = GetModifiers(currentData);
            BuildModifierComparison(sb, newMods, curMods);

            return TrimResult(sb);
        }

        /// <summary>주요 스탯(ATK, DEF, AttackSpeed) 비교</summary>
        private void BuildPrimaryComparison(StringBuilder sb, ItemData newData, ItemData curData)
        {
            // ATK 비교
            float newAtk = GetPrimaryATK(newData);
            float curAtk = GetPrimaryATK(curData);
            if (newAtk != 0 || curAtk != 0)
                AppendDiffLine(sb, "ATK", curAtk, newAtk);

            // DEF 비교
            float newDef = GetPrimaryDEF(newData);
            float curDef = GetPrimaryDEF(curData);
            if (newDef != 0 || curDef != 0)
                AppendDiffLine(sb, "DEF", curDef, newDef);

            // Attack Speed 비교 (무기끼리)
            float newSpd = GetAttackSpeed(newData);
            float curSpd = GetAttackSpeed(curData);
            if (newSpd != 0 || curSpd != 0)
                AppendDiffLine(sb, "AtkSpd", curSpd, newSpd);
        }

        /// <summary>모디파이어 합산 비교</summary>
        private void BuildModifierComparison(StringBuilder sb,
            StatModifier[] newMods, StatModifier[] curMods)
        {
            // 등장하는 모든 StatType 수집
            var allTypes = new HashSet<StatType>();
            CollectTypes(allTypes, newMods);
            CollectTypes(allTypes, curMods);

            foreach (var statType in allTypes)
            {
                // Flat 비교
                float newFlat = SumModifiers(newMods, statType, ModifierType.Flat);
                float curFlat = SumModifiers(curMods, statType, ModifierType.Flat);
                if (newFlat != 0 || curFlat != 0)
                    AppendDiffLine(sb, $"{statType}", curFlat, newFlat);

                // Percent 비교
                float newPct = SumModifiers(newMods, statType, ModifierType.Percent);
                float curPct = SumModifiers(curMods, statType, ModifierType.Percent);
                if (newPct != 0 || curPct != 0)
                    AppendDiffLinePct(sb, $"{statType}%", curPct, newPct);
            }
        }

        /// <summary>diff 라인 출력: "ATK  30 → 45  (+15)"</summary>
        private void AppendDiffLine(StringBuilder sb, string label, float current, float next)
        {
            float diff = next - current;
            if (Mathf.Approximately(diff, 0f))
            {
                sb.AppendLine($"<color={COLOR_NEUTRAL}>{label,-10} {current:0.#} → {next:0.#}  (=)</color>");
                return;
            }
            string diffColor = diff > 0 ? COLOR_POSITIVE : COLOR_NEGATIVE;
            string sign = diff > 0 ? "+" : "";
            sb.AppendLine($"{label,-10} {current:0.#} → {next:0.#}  <color={diffColor}>({sign}{diff:0.#})</color>");
        }

        /// <summary>percent diff 라인: "CritRate%  5% → 8%  (+3%)"</summary>
        private void AppendDiffLinePct(StringBuilder sb, string label, float current, float next)
        {
            float diff = next - current;
            if (Mathf.Approximately(diff, 0f))
            {
                sb.AppendLine($"<color={COLOR_NEUTRAL}>{label,-10} {current * 100:0.#}% → {next * 100:0.#}%  (=)</color>");
                return;
            }
            string diffColor = diff > 0 ? COLOR_POSITIVE : COLOR_NEGATIVE;
            string sign = diff > 0 ? "+" : "";
            sb.AppendLine($"{label,-10} {current * 100:0.#}% → {next * 100:0.#}%  <color={diffColor}>({sign}{diff * 100:0.#}%)</color>");
        }

        // ──────────────────────────────────────
        // 비교용 헬퍼
        // ──────────────────────────────────────

        private static EquipSlotType? ResolveTargetSlot(ItemData data)
        {
            return data switch
            {
                WeaponData              => EquipSlotType.Weapon,
                SubWeaponData           => EquipSlotType.OffHand,
                ArmorData armor         => armor.equipSlot,
                _                       => null
            };
        }

        private static float GetPrimaryATK(ItemData data)
        {
            return data switch
            {
                WeaponData w    => w.attackPower,
                SubWeaponData s => s.attackPower,
                _               => 0f
            };
        }

        private static float GetPrimaryDEF(ItemData data)
        {
            return data switch
            {
                ArmorData a     => a.defense,
                SubWeaponData s => s.defense,
                _               => 0f
            };
        }

        private static float GetAttackSpeed(ItemData data)
        {
            return data switch
            {
                WeaponData w => w.attackSpeed,
                _            => 0f
            };
        }

        private static StatModifier[] GetModifiers(ItemData data)
        {
            return data switch
            {
                WeaponData w    => w.statModifiers,
                ArmorData a     => a.statModifiers,
                SubWeaponData s => s.statModifiers,
                _               => null
            };
        }

        private static void CollectTypes(HashSet<StatType> set, StatModifier[] mods)
        {
            if (mods == null) return;
            foreach (var m in mods)
                set.Add(m.statType);
        }

        private static float SumModifiers(StatModifier[] mods, StatType type, ModifierType modType)
        {
            if (mods == null) return 0f;
            float sum = 0f;
            foreach (var m in mods)
            {
                if (m.statType == type && m.modifierType == modType)
                    sum += m.value;
            }
            return sum;
        }

        private void HideComparePanel()
        {
            if (comparePanel != null)
                comparePanel.SetActive(false);
        }

        // ══════════════════════════════════════
        // 스킬 툴팁 빌드
        // ══════════════════════════════════════

        private void BuildSkillTooltip(SkillInstance skill)
        {
            var data = skill.data;

            // 이름
            SetText(nameText, data.skillName);
            if (nameText != null)
                nameText.color = Color.white;

            // 타입
            SetText(typeText, $"{data.skillType} Skill · Lv.{skill.proficiency.level}");

            // 아이콘
            SetIcon(data.icon);

            // 장착 부위 → 없음
            SetText(equipSlotText, null);

            // 핵심 스탯
            var sb = new StringBuilder();
            sb.AppendLine($"Damage  <b>{skill.TotalDamage:F1}</b>");
            sb.AppendLine($"Cooldown  <b>{skill.TotalCooldown:F1}s</b>");
            sb.AppendLine($"MP Cost  <b>{skill.TotalMpCost:F0}</b>");
            SetText(primaryStatsText, TrimResult(sb));

            // 숙련도 정보
            sb.Clear();
            if (skill.proficiency.IsMaxLevel)
                sb.AppendLine($"<color={COLOR_SPECIAL}>Proficiency: MAX</color>");
            else
                sb.AppendLine($"EXP: {skill.proficiency.currentExp}/{skill.proficiency.RequiredExp} ({skill.proficiency.Progress * 100:F0}%)");

            if (skill.data.maxGemSockets > 0)
                sb.AppendLine($"Gem Sockets: {skill.AttachedGemCount}/{skill.UnlockedSocketCount}/{skill.data.maxGemSockets}");
            SetText(modifierStatsText, TrimResult(sb));

            // 잼 상세
            sb.Clear();
            for (int i = 0; i < skill.gemSockets.Length; i++)
            {
                var gs = skill.gemSockets[i];
                if (!gs.isUnlocked)
                    sb.AppendLine($"<color={COLOR_NEUTRAL}>  [{i}] Locked</color>");
                else if (gs.HasGem)
                    sb.AppendLine($"<color={COLOR_SPECIAL}>  [{i}] {gs.attachedGem.itemName}</color>");
                else
                    sb.AppendLine($"  [{i}] Empty");
            }
            SetText(specialEffectText, skill.gemSockets.Length > 0 ? TrimResult(sb) : null);

            // 설명
            SetText(descriptionText, null);
            SetText(detailDescText, null);
            SetText(sellPriceText, null);
        }

        // ══════════════════════════════════════
        // 위치 계산 (화면 밖 보정)
        // ══════════════════════════════════════

        /// <summary>
        /// 마우스 위치를 기준으로 툴팁 위치를 설정한다.
        /// 4방향(우하→좌하→우상→좌상) 순서로 화면 안에 들어오는 위치를 탐색한다.
        /// </summary>
        private void UpdatePosition(Vector2 mouseScreenPos)
        {
            if (mainTooltipRect == null || _canvasRect == null) return;

            // Canvas Scaler 반영한 실제 크기 계산
            float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
            Vector2 tooltipSize = mainTooltipRect.sizeDelta * scaleFactor;

            // 비교 패널이 활성이면 전체 너비 확장 (오른쪽에 비교 패널 배치)
            float totalWidth = tooltipSize.x;
            if (comparePanel != null && comparePanel.activeSelf && compareRect != null)
                totalWidth += compareRect.sizeDelta.x * scaleFactor + 4f;

            // 4방향 후보 (우하, 좌하, 우상, 좌상)
            Vector2[] candidates = new Vector2[]
            {
                mouseScreenPos + new Vector2(offset.x, offset.y),                                           // 우하
                mouseScreenPos + new Vector2(-offset.x - totalWidth, offset.y),                              // 좌하
                mouseScreenPos + new Vector2(offset.x, -offset.y + tooltipSize.y),                           // 우상
                mouseScreenPos + new Vector2(-offset.x - totalWidth, -offset.y + tooltipSize.y),             // 좌상
            };

            Vector2 chosen = candidates[0];
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2 c = candidates[i];
                // 화면 범위 체크
                if (c.x >= screenPadding
                    && c.x + totalWidth <= Screen.width - screenPadding
                    && c.y - tooltipSize.y >= screenPadding
                    && c.y <= Screen.height - screenPadding)
                {
                    chosen = c;
                    break;
                }
            }

            // 최종 클램프 (어떤 후보도 완전히 들어가지 않을 경우 대비)
            chosen.x = Mathf.Clamp(chosen.x, screenPadding, Screen.width - totalWidth - screenPadding);
            chosen.y = Mathf.Clamp(chosen.y, tooltipSize.y + screenPadding, Screen.height - screenPadding);

            mainTooltipRect.position = chosen;
        }

        /// <summary>Content Size Fitter 등의 레이아웃을 강제 갱신</summary>
        private void ForceRebuildLayout()
        {
            if (mainTooltipRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainTooltipRect);
            if (compareRect != null && comparePanel != null && comparePanel.activeSelf)
                LayoutRebuilder.ForceRebuildLayoutImmediate(compareRect);

            // 레이아웃 갱신 후 위치 재계산
            UpdatePosition(_currentMousePos);
        }

        // ══════════════════════════════════════
        // 내부 유틸리티
        // ══════════════════════════════════════

        private void SetText(TMP_Text textComp, string content)
        {
            if (textComp == null) return;

            if (string.IsNullOrEmpty(content))
            {
                textComp.text = "";
                textComp.gameObject.SetActive(false);
            }
            else
            {
                textComp.text = content;
                textComp.gameObject.SetActive(true);
            }
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

        private static string TrimResult(StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return null;
            return sb.ToString().TrimEnd('\n', '\r', ' ');
        }
    }
}
