# Unity 셋업 가이드 - RPG UI 시스템

이 문서는 코드를 유니티에서 실제로 동작시키기 위한 **오브젝트 생성 → 스크립트 부착 → Inspector 연결** 가이드입니다.

---

## 1단계: ScriptableObject 에셋 생성

유니티 Project 창에서 우클릭 → `Create` 메뉴로 생성합니다.

### 1-1. 아이템 데이터 (Assets/_Project/Data/Items/)

| 생성 방법 | 설명 |
|---|---|
| `Create > Item > WeaponData` | 무기 (검, 활 등) |
| `Create > Item > ArmorData` | 방어구 (투구, 갑옷, 부츠 등) |
| `Create > Item > SubWeaponData` | 보조무기 (방패 등) |
| `Create > Item > ConsumableData` | 소모품 (포션 등) |
| `Create > Item > GemData` | 젬 (스킬 소켓용) |
| `Create > Item > KeyItemData` | 퀘스트 아이템 |

> **참고:** 각 ScriptableObject의 `CreateAssetMenu`가 코드에 정의되어 있어야 합니다. 없으면 우클릭 메뉴에 안 나타납니다.

### 1-2. 데이터베이스 (Assets/_Project/Data/)

| 에셋 | 설명 |
|---|---|
| **ItemDatabase** | `Create > Item > ItemDatabase` → 위에서 만든 아이템들을 배열에 등록 |
| **SkillDatabase** | `Create > Skill > SkillDatabase` → 스킬 데이터들을 배열에 등록 |

### 1-3. 스킬 데이터 (Assets/_Project/Data/Skills/)

| 생성 방법 | 설명 |
|---|---|
| `Create > Skill > SkillData` | 스킬 정의 (Active/Buff/Passive) |

---

## 2단계: Hierarchy 오브젝트 구조

아래와 같이 유니티 Hierarchy에 오브젝트를 만듭니다.

```
[Scene Root]
│
├── ===== MANAGERS =====          ← 빈 오브젝트 (정리용, 스크립트 없음)
│   ├── GameManager               ← GameManager.cs
│   ├── InventoryManager          ← InventoryManager.cs
│   ├── EquipmentManager          ← EquipmentManager.cs
│   ├── SkillManager              ← SkillManager.cs
│   ├── PlayerStatManager         ← PlayerStatManager.cs
│   ├── GemService                ← GemService.cs
│   └── InventoryDebugger         ← InventoryDebugger.cs (테스트용, 나중에 제거 가능)
│
├── ===== UI =====                ← 빈 오브젝트 (정리용)
│   ├── UICanvas                  ← Canvas + CanvasScaler + GraphicRaycaster
│   │   ├── UIManager             ← UIManager.cs (Canvas 자식으로)
│   │   │
│   │   ├── InventoryPanel        ← InventoryUIController.cs
│   │   │   ├── Title             ← TMP_Text ("인벤토리")
│   │   │   ├── SlotContainer     ← GridLayoutGroup + ContentSizeFitter
│   │   │   └── CloseButton       ← Button
│   │   │
│   │   ├── EquipmentPanel        ← EquipmentUIController.cs
│   │   │   ├── Title             ← TMP_Text ("장비")
│   │   │   ├── SlotArea
│   │   │   │   ├── WeaponSlot    ← EquipmentSlotUI.cs (slotType = Weapon)
│   │   │   │   ├── OffHandSlot   ← EquipmentSlotUI.cs (slotType = OffHand)
│   │   │   │   ├── HelmetSlot    ← EquipmentSlotUI.cs (slotType = Helmet)
│   │   │   │   ├── ChestSlot     ← EquipmentSlotUI.cs (slotType = Chest)
│   │   │   │   ├── LegsSlot      ← EquipmentSlotUI.cs (slotType = Legs)
│   │   │   │   ├── BootsSlot     ← EquipmentSlotUI.cs (slotType = Boots)
│   │   │   │   ├── Ring1Slot     ← EquipmentSlotUI.cs (slotType = Ring1)
│   │   │   │   └── Ring2Slot     ← EquipmentSlotUI.cs (slotType = Ring2)
│   │   │   ├── StatSummary       ← TMP_Text (스탯 요약 표시)
│   │   │   └── CloseButton       ← Button
│   │   │
│   │   ├── SkillPanel            ← SkillUIController.cs
│   │   │   ├── Title             ← TMP_Text ("스킬")
│   │   │   ├── SlotContainer     ← VerticalLayoutGroup
│   │   │   ├── DetailText        ← TMP_Text (선택한 스킬 상세정보)
│   │   │   └── CloseButton       ← Button
│   │   │
│   │   └── Tooltip               ← TooltipUI.cs
│   │       ├── MainTooltip       ← RectTransform (메인 패널)
│   │       │   ├── NameText      ← TMP_Text
│   │       │   ├── TypeText      ← TMP_Text
│   │       │   ├── IconImage     ← Image
│   │       │   ├── EquipSlotText ← TMP_Text
│   │       │   ├── PrimaryStats  ← TMP_Text
│   │       │   ├── ModifierStats ← TMP_Text
│   │       │   ├── SpecialEffect ← TMP_Text
│   │       │   ├── Description   ← TMP_Text
│   │       │   ├── DetailDesc    ← TMP_Text
│   │       │   └── SellPrice     ← TMP_Text
│   │       └── ComparePanel      ← GameObject (비교 패널)
│   │           ├── CompareLabel  ← TMP_Text ("현재 장착 중")
│   │           ├── CompareName   ← TMP_Text
│   │           └── CompareStats  ← TMP_Text
│   │
│   └── EventSystem               ← EventSystem + StandaloneInputModule
│
├── ===== PLAYER =====
│   └── Player                    ← 플레이어 오브젝트 + Rigidbody + Collider
│       ├── PlayerInputBlocker    ← PlayerInputBlocker.cs
│       └── ItemPickupSystem      ← ItemPickupSystem.cs (자동으로 SphereCollider 추가됨)
│
├── ===== WORLD ITEMS =====       ← 월드에 배치된 줍기 가능한 아이템들
│   ├── Item_Sword                ← WorldItem.cs + Collider(isTrigger=true) + 3D모델
│   ├── Item_Potion               ← WorldItem.cs + Collider(isTrigger=true) + 3D모델
│   └── Item_Gem                  ← WorldItem.cs + Collider(isTrigger=true) + 3D모델
│
└── (기타 게임 오브젝트들...)
```

---

## 3단계: 프리팹 만들기

아래 2개의 프리팹이 필요합니다. `Assets/_Project/Prefabs/UI/` 폴더에 저장하세요.

### 3-1. InventorySlotUI 프리팹

```
InventorySlot (프리팹)          ← InventorySlotUI.cs + Image (배경) + Button
├── EmptyBackground             ← Image (빈 슬롯 배경, 회색 등)
├── IconImage                   ← Image (아이템 아이콘, raycastTarget = false)
├── QuantityText                ← TMP_Text (수량, 우하단 정렬)
└── SelectionFrame              ← Image/GameObject (선택 테두리, 기본 비활성)
```

**크기 추천:** 80x80 또는 64x64

### 3-2. SkillSlotUI 프리팹

```
SkillSlot (프리팹)              ← SkillSlotUI.cs + Image (배경)
├── IconImage                   ← Image (스킬 아이콘)
├── SkillNameText               ← TMP_Text (스킬 이름)
├── LevelText                   ← TMP_Text ("Lv.1")
├── ProficiencyBar              ← Image (Fill 타입, 숙련도 바)
├── TypeLabel                   ← TMP_Text ("Active" / "Buff" / "Passive")
├── SocketText                  ← TMP_Text ("2/3")
├── QuantityText                ← TMP_Text (UISlotBase 용)
├── SelectionFrame              ← GameObject (선택 테두리)
└── EmptyBackground             ← Image (빈 슬롯 배경)
```

### 3-3. WorldItem 프리팹 (선택)

자주 사용하는 월드 아이템을 프리팹으로 만들어두면 편합니다. `Assets/_Project/Prefabs/World/` 폴더에 저장하세요.

```
WorldItem (프리팹)              ← WorldItem.cs + SphereCollider(isTrigger=true)
├── Model                       ← 아이템 3D 모델 또는 Sprite
└── (선택) EffectParticle       ← ParticleSystem (반짝이는 효과 등)
```

**Collider 설정:**
- **종류:** SphereCollider 또는 BoxCollider
- **isTrigger:** 반드시 **true** (체크!)
- **크기:** 아이템 모델보다 약간 크게 (플레이어가 가까이 가면 감지되도록)

---

## 3.5단계: 줍기 안내 UI (선택)

플레이어가 아이템 근처에 갔을 때 `[F] 아이템이름 줍기` 같은 안내를 표시하려면:

```
UICanvas
└── PickupPrompt              ← GameObject (기본 비활성)
    └── PromptText            ← TMP_Text ("[F] 아이템 줍기", 화면 중앙 하단 배치)
```

이 오브젝트를 ItemPickupSystem의 Inspector에 연결합니다.

---

## 4단계: Inspector 연결 (가장 중요!)

### 4-1. GameManager

| 필드 | 값 |
|---|---|
| `lockCursorOnStart` | ✅ (FPS면 true, 아니면 false) |
| `pauseOnUI` | ✅ (UI 열면 게임 일시정지) |

### 4-2. InventoryManager

| 필드 | 연결 대상 |
|---|---|
| `slotCount` | `40` (원하는 슬롯 수) |
| `itemDatabase` | → ItemDatabase 에셋 드래그 |

### 4-3. SkillManager

| 필드 | 연결 대상 |
|---|---|
| `skillDatabase` | → SkillDatabase 에셋 드래그 |
| `defaultExpPerUse` | `10` |

### 4-4. PlayerStatManager

| 필드 | 값 |
|---|---|
| `baseStats` 배열 | 아래처럼 설정: |

```
[0] HP       = 100
[1] MP       = 50
[2] ATK      = 10
[3] DEF      = 5
[4] SPD      = 10
[5] AttackSpeed = 1.0
[6] CritRate = 0.05
[7] CritDamage = 1.5
```

### 4-5. UIManager

| 필드 | 연결 대상 |
|---|---|
| `panelRegistry` 배열 [3] | → InventoryPanel, EquipmentPanel, SkillPanel 드래그 |
| `inventoryKey` | `I` |
| `equipmentKey` | `E` |
| `skillKey` | `K` |
| `closeKey` | `Escape` |

### 4-6. InventoryUIController (InventoryPanel 오브젝트)

| 필드 | 연결 대상 |
|---|---|
| `slotPrefab` | → InventorySlotUI 프리팹 드래그 |
| `slotContainer` | → SlotContainer (GridLayoutGroup 있는 오브젝트) 드래그 |

### 4-7. EquipmentUIController (EquipmentPanel 오브젝트)

| 필드 | 연결 대상 |
|---|---|
| `equipmentSlots` 배열 [8] | → 8개의 EquipmentSlotUI 오브젝트를 순서대로 드래그 |
| `statSummaryText` | → StatSummary TMP_Text 드래그 |

### 4-8. 각 EquipmentSlotUI (8개 각각)

| 필드 | 연결 대상 |
|---|---|
| `slotType` | 드롭다운에서 선택 (Weapon, OffHand, Helmet 등) |
| `slotLabel` | → 자기 자신의 슬롯이름 TMP_Text |
| `iconImage` | → 자기 자신의 아이콘 Image |
| `selectionFrame` | → 자기 자신의 선택 프레임 GameObject |
| `emptyBackground` | → 자기 자신의 빈 배경 Image |

### 4-9. SkillUIController (SkillPanel 오브젝트)

| 필드 | 연결 대상 |
|---|---|
| `slotPrefab` | → SkillSlotUI 프리팹 드래그 |
| `slotContainer` | → SlotContainer (VerticalLayoutGroup 있는 오브젝트) 드래그 |
| `detailText` | → DetailText TMP_Text 드래그 |

### 4-10. TooltipUI (Tooltip 오브젝트)

| 필드 | 연결 대상 |
|---|---|
| `mainTooltipRect` | → MainTooltip RectTransform |
| `nameText` | → NameText |
| `typeText` | → TypeText |
| `iconImage` | → IconImage |
| `equipSlotText` | → EquipSlotText |
| `primaryStatsText` | → PrimaryStats |
| `modifierStatsText` | → ModifierStats |
| `specialEffectText` | → SpecialEffect |
| `descriptionText` | → Description |
| `detailDescText` | → DetailDesc |
| `sellPriceText` | → SellPrice |
| `comparePanel` | → ComparePanel GameObject |
| `compareRect` | → ComparePanel RectTransform |
| `compareNameText` | → CompareName |
| `compareStatsText` | → CompareStats |
| `compareLabelText` | → CompareLabel |
| `offset` | `(20, -20)` |
| `screenPadding` | `8` |

### 4-11. PlayerInputBlocker (Player 오브젝트)

| 필드 | 연결 대상 |
|---|---|
| `playerController` | → 플레이어 이동 스크립트 (CharacterController 등) |
| `cameraController` | → 카메라 컨트롤 스크립트 |
| `additionalControllers` | → 기타 비활성화할 스크립트들 |

### 4-12. ItemPickupSystem (Player 오브젝트)

| 필드 | 연결 대상 / 값 |
|---|---|
| `pickupKey` | `F` (상호작용 키) |
| `pickupRange` | `3` (줍기 감지 범위, SphereCollider 자동 생성) |
| `pickupPromptUI` | → (선택) PickupPrompt GameObject 드래그 |
| `promptText` | → (선택) PromptText TMP_Text 드래그 |

> **참고:** Start()에서 SphereCollider(isTrigger)가 자동으로 추가됩니다. 이미 있으면 추가하지 않습니다.

### 4-13. WorldItem (월드 아이템 오브젝트 각각)

| 필드 | 연결 대상 / 값 |
|---|---|
| `itemData` | → ItemData SO 드래그 (이 오브젝트가 나타내는 아이템) |
| `amount` | `1` (줍을 때 획득할 수량) |
| `floatEffect` | ✅ (위아래 떠다니는 효과) |
| `floatAmplitude` | `0.2` (떠다니는 높이) |
| `floatSpeed` | `2` (떠다니는 속도) |
| `rotateSpeed` | `90` (회전 속도, 도/초) |

> **필수:** WorldItem 오브젝트의 **Collider를 isTrigger = true**로 설정하세요!

### 4-14. InventoryDebugger (테스트용)

| 필드 | 연결 대상 |
|---|---|
| `testItems` | → 테스트용 ItemData 에셋들 드래그 |
| `testSkills` | → 테스트용 SkillData 에셋들 드래그 |
| `testGems` | → 테스트용 GemData 에셋들 드래그 |

**디버거 키 바인딩 (UI 토글):**

| 키 | 기능 | 비고 |
|---|---|---|
| `I` | 인벤토리 토글 | UIManager 없을 때만 동작 |
| `E` | 장비 토글 | UIManager 없을 때만 동작 |
| `K` | 스킬 토글 | UIManager 없을 때만 동작 |
| `Backspace` | 모든 패널 닫기 | 항상 동작 |

---

## 5단계: Canvas 설정

### UICanvas 설정

| 컴포넌트 | 설정 |
|---|---|
| **Canvas** | Render Mode = `Screen Space - Overlay` |
| **CanvasScaler** | UI Scale Mode = `Scale With Screen Size`, Reference Resolution = `1920x1080`, Match = `0.5` |
| **GraphicRaycaster** | 기본값 |

### 패널 초기 상태

**중요:** 3개 패널은 시작 시 **비활성** 상태여야 합니다!

- `InventoryPanel` → Inspector에서 **GameObject 비활성** (체크 해제)
- `EquipmentPanel` → Inspector에서 **GameObject 비활성** (체크 해제)
- `SkillPanel` → Inspector에서 **GameObject 비활성** (체크 해제)
- `Tooltip` → Inspector에서 **GameObject 비활성** (체크 해제)

### GridLayoutGroup (인벤토리 SlotContainer)

| 설정 | 값 |
|---|---|
| Cell Size | `80 x 80` |
| Spacing | `4 x 4` |
| Constraint | `Fixed Column Count` = 8 (8x5 = 40칸) |
| Padding | `8, 8, 8, 8` |

### VerticalLayoutGroup (스킬 SlotContainer)

| 설정 | 값 |
|---|---|
| Spacing | `4` |
| Child Force Expand Width | ✅ |
| Child Force Expand Height | ❌ |

---

## 6단계: 월드 아이템 배치 방법

### 방법 A: 직접 배치

1. Hierarchy에서 **빈 오브젝트** 생성 (또는 3D 모델 드래그)
2. `WorldItem.cs` 스크립트 부착
3. `SphereCollider` 또는 `BoxCollider` 추가 → **isTrigger = true**
4. Inspector에서 `itemData`에 원하는 아이템 SO 드래그
5. 씬에 원하는 위치에 배치

### 방법 B: 프리팹으로 배치

1. 위 방법으로 하나 만든 뒤 Project 폴더로 드래그 → 프리팹화
2. 프리팹을 씬에 드래그하여 여러 개 배치
3. 각 인스턴스마다 `itemData`와 `amount`를 다르게 설정 가능

### 플레이어 설정 (줍기 시스템)

**필수 조건:**
- Player에 **Rigidbody** 필요 (OnTriggerEnter 감지를 위해)
  - `isKinematic = true` 권장 (CharacterController 사용 시)
  - 또는 이미 Rigidbody가 있다면 그대로 사용
- Player에 **Collider** 필요 (기존 플레이어 콜라이더)
- `ItemPickupSystem.cs` 부착 → 자동으로 SphereCollider(Trigger) 추가됨

---

## 7단계: 동작 확인 (테스트)

### UI 테스트

1. **Play** 누르기
2. `F1` → 테스트 아이템 추가 (InventoryDebugger)
3. `I` → 인벤토리 창 열기
4. `E` → 장비 창 열기
5. `K` → 스킬 창 열기
6. 인벤토리에서 장비 아이템 **우클릭** → 자동 장착
7. 장비 창에서 장비 **우클릭** → 해제
8. `ESC` → 창 닫기

### 아이템 줍기 테스트

1. 씬에 WorldItem 오브젝트 배치 (위 6단계 참조)
2. **Play** 누르기
3. 플레이어를 아이템 근처로 이동
4. `[F] 아이템이름 줍기` 안내가 표시됨 (PickupPromptUI 연결 시)
5. `F` 키 누르기 → 아이템이 인벤토리에 추가되고 월드에서 사라짐
6. `I` 키로 인벤토리 열어서 확인
7. 인벤토리 가득 찬 상태에서 줍기 시도 → 실패 메시지 (Console)

---

## 빠른 체크리스트

### 기본 시스템
- [ ] ScriptableObject 에셋 생성 (ItemDatabase, SkillDatabase, 아이템들, 스킬들)
- [ ] Manager 오브젝트 6개 생성 및 스크립트 부착
- [ ] Canvas 생성 및 UIManager 부착
- [ ] 3개 패널 오브젝트 생성 (Inventory, Equipment, Skill)
- [ ] Tooltip 오브젝트 생성
- [ ] 프리팹 2개 생성 (InventorySlotUI, SkillSlotUI)
- [ ] 장비 슬롯 8개 생성 및 각각 slotType 설정
- [ ] 모든 Inspector 필드 연결
- [ ] 패널들 초기 비활성 상태 확인
- [ ] EventSystem 존재 확인

### 아이템 줍기 시스템
- [ ] Player에 Rigidbody 확인 (없으면 추가, isKinematic 권장)
- [ ] Player에 ItemPickupSystem.cs 부착
- [ ] (선택) PickupPrompt UI 생성 및 연결
- [ ] WorldItem 오브젝트 생성 (3D모델 + Collider(isTrigger) + WorldItem.cs)
- [ ] 각 WorldItem의 itemData에 아이템 SO 연결
- [ ] WorldItem의 Collider가 isTrigger = true 인지 확인

### 테스트
- [ ] Play 후 F1/I/E/K 키로 UI 테스트
- [ ] 월드 아이템 근처에서 F 키로 줍기 테스트
- [ ] Backspace로 모든 패널 닫기 테스트
