using System;
using System.Collections.Generic;
using UnityEngine;
using RPGSystem.Core;
using RPGSystem.Equipment;

namespace RPGSystem.Stat
{
    /// <summary>
    /// 플레이어 스탯 매니저.
    /// 기본 스탯 + 장비 보너스를 합산하여 최종 스탯을 계산한다.
    ///
    /// 계산 공식: 최종값 = (기본값 + Flat 합계) * (1 + Percent 합계)
    ///
    /// EquipmentChangedEvent를 구독하여 장비 변경 시 자동 재계산한다.
    /// 외부 시스템은 StatChangedEvent를 구독하여 UI 갱신 등을 처리한다.
    ///
    /// [GameObject] "SystemManager" 오브젝트에 부착 (EquipmentManager와 동일 가능).
    /// [Inspector] baseStats 배열에서 기본 스탯 설정.
    /// </summary>
    public class PlayerStatManager : MonoBehaviour
    {
        public static PlayerStatManager Instance { get; private set; }

        [Header("기본 스탯")]
        [Tooltip("캐릭터 기본 스탯 (레벨업 등으로 변경)")]
        [SerializeField] private BaseStatEntry[] baseStats = new BaseStatEntry[]
        {
            new BaseStatEntry { statType = StatType.HP,          value = 100 },
            new BaseStatEntry { statType = StatType.MP,          value = 50 },
            new BaseStatEntry { statType = StatType.ATK,         value = 10 },
            new BaseStatEntry { statType = StatType.DEF,         value = 5 },
            new BaseStatEntry { statType = StatType.SPD,         value = 5 },
            new BaseStatEntry { statType = StatType.AttackSpeed, value = 1 },
            new BaseStatEntry { statType = StatType.CritRate,    value = 0.05f },
            new BaseStatEntry { statType = StatType.CritDamage,  value = 1.5f },
        };

        /// <summary>기본 스탯 딕셔너리 (캐시)</summary>
        private Dictionary<StatType, float> _baseStatMap;

        /// <summary>최종 스탯 딕셔너리 (기본 + 장비 + 버프)</summary>
        private Dictionary<StatType, float> _finalStatMap;

        /// <summary>외부 버프/디버프 등 추가 모디파이어 (장비 외)</summary>
        private List<StatModifier> _externalModifiers = new List<StatModifier>();

        // ──────────────────────────────────────
        // Public 조회
        // ──────────────────────────────────────

        /// <summary>최종 스탯 값 조회</summary>
        public float GetStat(StatType type)
        {
            if (_finalStatMap != null && _finalStatMap.TryGetValue(type, out float val))
                return val;
            return GetBaseStat(type);
        }

        /// <summary>기본 스탯 값 조회</summary>
        public float GetBaseStat(StatType type)
        {
            if (_baseStatMap != null && _baseStatMap.TryGetValue(type, out float val))
                return val;
            return 0f;
        }

        /// <summary>최종 스탯 전체 딕셔너리 (읽기 전용)</summary>
        public IReadOnlyDictionary<StatType, float> FinalStats => _finalStatMap;

        // ──────────────────────────────────────
        // 기본 스탯 수정 (레벨업 등)
        // ──────────────────────────────────────

        /// <summary>기본 스탯 직접 설정</summary>
        public void SetBaseStat(StatType type, float value)
        {
            _baseStatMap[type] = value;
            Recalculate();
        }

        /// <summary>기본 스탯에 값 추가</summary>
        public void AddBaseStat(StatType type, float amount)
        {
            if (!_baseStatMap.ContainsKey(type))
                _baseStatMap[type] = 0f;
            _baseStatMap[type] += amount;
            Recalculate();
        }

        // ──────────────────────────────────────
        // 외부 모디파이어 (버프/디버프)
        // ──────────────────────────────────────

        /// <summary>외부 모디파이어 추가 (버프 등)</summary>
        public void AddExternalModifier(StatModifier modifier)
        {
            _externalModifiers.Add(modifier);
            Recalculate();
        }

        /// <summary>외부 모디파이어 제거</summary>
        public void RemoveExternalModifier(StatModifier modifier)
        {
            _externalModifiers.Remove(modifier);
            Recalculate();
        }

        /// <summary>모든 외부 모디파이어 제거</summary>
        public void ClearExternalModifiers()
        {
            _externalModifiers.Clear();
            Recalculate();
        }

        // ──────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildBaseStatMap();
            Recalculate();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EquipmentChangedEvent>(OnEquipmentChanged);
        }

        private void OnEquipmentChanged(EquipmentChangedEvent evt)
        {
            Recalculate();
        }

        // ──────────────────────────────────────
        // 스탯 계산
        // ──────────────────────────────────────

        private void BuildBaseStatMap()
        {
            _baseStatMap = new Dictionary<StatType, float>();
            foreach (var entry in baseStats)
            {
                _baseStatMap[entry.statType] = entry.value;
            }
        }

        /// <summary>
        /// 최종 스탯 재계산.
        ///
        /// 공식: 최종값 = (기본값 + Flat 합계) * (1 + Percent 합계)
        ///
        /// 1) 모든 StatType에 대해 기본값으로 초기화
        /// 2) 장비 모디파이어 수집
        /// 3) 외부 모디파이어 포함
        /// 4) Flat 먼저 합산, 그 다음 Percent 적용
        /// </summary>
        public void Recalculate()
        {
            _finalStatMap = new Dictionary<StatType, float>();

            // 1) 기본값 복사
            foreach (var kvp in _baseStatMap)
            {
                _finalStatMap[kvp.Key] = kvp.Value;
            }

            // 2) 모든 모디파이어 수집
            var allModifiers = new List<StatModifier>();

            // 장비 모디파이어
            if (EquipmentManager.Instance != null)
            {
                allModifiers.AddRange(EquipmentManager.Instance.GetAllEquipmentModifiers());
            }

            // 외부 모디파이어 (버프 등)
            allModifiers.AddRange(_externalModifiers);

            // 3) Flat/Percent 분류 및 합산
            var flatSums = new Dictionary<StatType, float>();
            var percentSums = new Dictionary<StatType, float>();

            foreach (var mod in allModifiers)
            {
                if (mod.modifierType == ModifierType.Flat)
                {
                    if (!flatSums.ContainsKey(mod.statType))
                        flatSums[mod.statType] = 0f;
                    flatSums[mod.statType] += mod.value;
                }
                else // Percent
                {
                    if (!percentSums.ContainsKey(mod.statType))
                        percentSums[mod.statType] = 0f;
                    percentSums[mod.statType] += mod.value;
                }
            }

            // 4) 최종 계산: (base + flat) * (1 + percent)
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                float baseVal = _finalStatMap.ContainsKey(statType) ? _finalStatMap[statType] : 0f;
                float flat = flatSums.ContainsKey(statType) ? flatSums[statType] : 0f;
                float percent = percentSums.ContainsKey(statType) ? percentSums[statType] : 0f;

                _finalStatMap[statType] = (baseVal + flat) * (1f + percent);
            }

            // 5) 이벤트 발행
            EventBus.Publish(new StatChangedEvent());

            Debug.Log("[Stat] Stats recalculated.");
        }

        // ──────────────────────────────────────
        // 디버그
        // ──────────────────────────────────────

        [ContextMenu("Debug: Print Stats")]
        public void DebugPrintStats()
        {
            Debug.Log("═══════════════ PLAYER STATS ═══════════════");
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                float baseVal = GetBaseStat(statType);
                float finalVal = GetStat(statType);
                float diff = finalVal - baseVal;
                string diffStr = diff != 0 ? $" ({(diff >= 0 ? "+" : "")}{diff:0.##})" : "";
                Debug.Log($"  {statType,-14} │ Base: {baseVal,8:0.##} │ Final: {finalVal,8:0.##}{diffStr}");
            }
            Debug.Log("════════════════════════════════════════════");
        }

        [ContextMenu("Debug: Force Recalculate")]
        public void DebugForceRecalculate()
        {
            Recalculate();
            DebugPrintStats();
        }
    }

    /// <summary>
    /// Inspector에서 기본 스탯을 편집하기 위한 직렬화 구조체.
    /// </summary>
    [Serializable]
    public struct BaseStatEntry
    {
        public StatType statType;
        public float value;
    }
}
