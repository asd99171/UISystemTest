# RPG UI System - Architecture Design

Unity 기반 RPG 인벤토리 / 장비 / 스킬 / 잼 시스템 아키텍처 설계 문서

---

## 1. 시스템 개요

| 시스템 | 설명 |
|--------|------|
| 인벤토리 | 아이템 습득, 보관, 사용, 버리기 |
| 장비 UI | 장비 슬롯에 아이템 장착/해제 |
| 스킬 UI | 스킬 목록 확인, 스킬 선택 |
| 스킬 숙련도 | 스킬 사용 시 경험치 누적 → 레벨업 |
| 잼 시스템 | 스킬에 잼 장착 → 스킬 효과 변경/강화 |
| 아이템 툴팁 | 마우스 호버 시 아이템 정보 표시 |
| UI 상태 관리 | UI 열림 시 게임 일시정지 + 커서 해제 |

### 아이템 종류
`Consumable` · `Armor` · `Weapon` · `SubWeapon` · `Gem` · `KeyItem`

### 장비 슬롯
`Weapon` · `OffHand` · `Helmet` · `Chest` · `Legs` · `Boots` · `Ring1` · `Ring2`

---

## 2. 추천 폴더 구조

```
Assets/
├── _Project/
│   ├── Data/                          # ScriptableObject 에셋
│   │   ├── Items/
│   │   │   ├── Consumables/
│   │   │   ├── Weapons/
│   │   │   ├── Armors/
│   │   │   ├── SubWeapons/
│   │   │   ├── Gems/
│   │   │   └── KeyItems/
│   │   ├── Skills/
│   │   └── Database/                  # ItemDatabase, SkillDatabase SO
│   │
│   ├── Scripts/
│   │   ├── Core/                      # 핵심 인프라
│   │   │   ├── GameManager.cs
│   │   │   ├── UIManager.cs
│   │   │   └── EventBus.cs
│   │   │
│   │   ├── Item/                      # 아이템 데이터 모델
│   │   │   ├── Data/
│   │   │   │   ├── ItemData.cs            # SO 기반 베이스
│   │   │   │   ├── ConsumableData.cs
│   │   │   │   ├── WeaponData.cs
│   │   │   │   ├── ArmorData.cs
│   │   │   │   ├── SubWeaponData.cs
│   │   │   │   ├── GemData.cs
│   │   │   │   └── KeyItemData.cs
│   │   │   ├── ItemInstance.cs            # 런타임 아이템 인스턴스
│   │   │   ├── ItemDatabase.cs            # 전체 아이템 DB (SO)
│   │   │   └── ItemEnums.cs               # ItemType, Rarity 등
│   │   │
│   │   ├── Inventory/                 # 인벤토리 로직
│   │   │   ├── InventorySystem.cs         # 모델 (데이터)
│   │   │   └── InventoryService.cs        # 비즈니스 로직
│   │   │
│   │   ├── Equipment/                 # 장비 로직
│   │   │   ├── EquipmentSystem.cs         # 모델
│   │   │   ├── EquipmentService.cs        # 장착/해제 로직
│   │   │   └── EquipSlot.cs               # 슬롯 enum + 헬퍼
│   │   │
│   │   ├── Skill/                     # 스킬 로직
│   │   │   ├── Data/
│   │   │   │   ├── SkillData.cs           # SO 기반
│   │   │   │   └── SkillDatabase.cs
│   │   │   ├── SkillInstance.cs           # 런타임 스킬
│   │   │   ├── SkillSystem.cs             # 스킬 보유/관리
│   │   │   └── SkillProficiency.cs        # 숙련도 데이터+로직
│   │   │
│   │   ├── Gem/                       # 잼 시스템
│   │   │   ├── GemSocket.cs               # 잼 소켓 데이터
│   │   │   └── GemService.cs              # 장착/해제 로직
│   │   │
│   │   ├── Stat/                      # 스탯 계산
│   │   │   ├── StatType.cs
│   │   │   ├── StatModifier.cs
│   │   │   └── StatCalculator.cs
│   │   │
│   │   └── UI/                        # UI 전체
│   │       ├── Common/
│   │       │   ├── UIPanel.cs             # UI 패널 베이스
│   │       │   ├── DraggableItem.cs       # 드래그 앤 드롭
│   │       │   └── TooltipSystem.cs       # 툴팁 매니저
│   │       ├── Inventory/
│   │       │   ├── InventoryUI.cs         # 인벤토리 화면
│   │       │   └── InventorySlotUI.cs     # 개별 슬롯
│   │       ├── Equipment/
│   │       │   ├── EquipmentUI.cs         # 장비 화면
│   │       │   └── EquipSlotUI.cs         # 장비 슬롯 UI
│   │       ├── Skill/
│   │       │   ├── SkillUI.cs             # 스킬 화면
│   │       │   ├── SkillSlotUI.cs
│   │       │   └── GemSocketUI.cs         # 잼 소켓 UI
│   │       └── Tooltip/
│   │           ├── ItemTooltipUI.cs
│   │           └── SkillTooltipUI.cs
│   │
│   ├── Prefabs/
│   │   └── UI/
│   ├── Art/
│   │   └── UI/
│   └── Scenes/
```

---

## 3. 클래스 구조도

```
                        ┌──────────────┐
                        │  GameManager │  (싱글턴, 게임 상태)
                        └──────┬───────┘
                               │
                 ┌─────────────┼─────────────┐
                 ▼             ▼             ▼
          ┌───────────┐ ┌───────────┐ ┌───────────┐
          │ UIManager │ │ EventBus  │ │SaveManager│
          └─────┬─────┘ └───────────┘ └───────────┘
                │
    ┌───────────┼───────────┬───────────────┐
    ▼           ▼           ▼               ▼
┌────────┐ ┌──────────┐ ┌────────┐   ┌──────────┐
│Inv. UI │ │Equip. UI │ │Skill UI│   │Tooltip UI│
└───┬────┘ └────┬─────┘ └───┬────┘   └──────────┘
    │           │           │
    ▼           ▼           ▼
┌─────────┐ ┌──────────┐ ┌──────────┐
│Inventory│ │Equipment │ │  Skill   │
│ Service │ │ Service  │ │ System   │
└───┬─────┘ └────┬─────┘ └────┬─────┘
    │            │            │
    ▼            ▼            ▼
┌─────────┐ ┌──────────┐ ┌──────────┐  ┌────────────┐
│Inventory│ │Equipment │ │  Skill   │  │    Stat    │
│ System  │ │ System   │ │Proficiency│  │ Calculator │
└───┬─────┘ └────┬─────┘ └──────────┘  └────────────┘
    │            │
    └─────┬──────┘
          ▼
   ┌─────────────┐     ┌───────────┐
   │ ItemInstance │────▶│ ItemData  │  (ScriptableObject)
   └─────────────┘     └───────────┘
                             ▲
            ┌────────────────┼────────────────┐
            │                │                │
     ┌──────┴───┐    ┌──────┴───┐     ┌──────┴──┐
     │WeaponData│    │ArmorData │     │ GemData │  ...
     └──────────┘    └──────────┘     └─────────┘
```

---

## 4. 각 클래스의 책임

### Core

| 클래스 | 책임 |
|--------|------|
| **GameManager** | 게임 상태 관리 (Playing, Paused, UI). `Time.timeScale` 제어, 커서 lock/unlock |
| **UIManager** | UI 패널 열기/닫기 총괄. 패널 스택 관리. GameManager에 UI 상태 통지 |
| **EventBus** | 시스템 간 느슨한 결합을 위한 이벤트 중개. `OnItemAdded`, `OnEquipChanged`, `OnSkillLevelUp` 등 |

### Item (데이터 모델)

| 클래스 | 책임 |
|--------|------|
| **ItemData** (SO) | 아이템 기본 정보: ID, 이름, 아이콘, 설명, ItemType, 최대 스택, 등급 |
| **WeaponData** (SO) | 공격력, 공격속도, 잼소켓 수 등 무기 전용 필드 |
| **ArmorData** (SO) | 방어력, 장비 슬롯 타입 (Helmet/Chest/Legs/Boots) |
| **SubWeaponData** (SO) | 보조무기 전용 스탯 |
| **ConsumableData** (SO) | 사용 효과 (HP회복, 버프 등) |
| **GemData** (SO) | 잼 효과 정의 (StatModifier 리스트, 스킬 효과 변경) |
| **KeyItemData** (SO) | 퀘스트/스토리 아이템 플래그 |
| **ItemInstance** | 런타임 아이템 인스턴스. ItemData 참조 + 수량, 강화 수치 등 가변 상태 |
| **ItemDatabase** (SO) | 모든 ItemData를 ID로 조회 가능한 사전 |
| **ItemEnums** | `ItemType`, `Rarity`, `EquipSlotType` enum 정의 |

### Inventory

| 클래스 | 책임 |
|--------|------|
| **InventorySystem** | 아이템 리스트 보유 (데이터 컨테이너). 슬롯 배열 관리 |
| **InventoryService** | 아이템 추가/제거/이동/스택합치기 비즈니스 로직. 이벤트 발행 |

### Equipment

| 클래스 | 책임 |
|--------|------|
| **EquipmentSystem** | 8개 장비 슬롯 상태 보유 (Dictionary<EquipSlotType, ItemInstance>) |
| **EquipmentService** | 장착 조건 검증, 장착/해제 실행, 인벤토리 연동, 스탯 재계산 트리거 |
| **EquipSlot** | `EquipSlotType` enum: Weapon, OffHand, Helmet, Chest, Legs, Boots, Ring1, Ring2 |

### Skill

| 클래스 | 책임 |
|--------|------|
| **SkillData** (SO) | 스킬 기본 정보: ID, 이름, 아이콘, 설명, 기본 데미지/쿨다운, 잼 소켓 수 |
| **SkillInstance** | 런타임 스킬 상태. SkillData 참조 + 숙련도 + 장착된 잼 목록 |
| **SkillSystem** | 보유 스킬 관리. 스킬 사용 시 숙련도 증가 트리거 |
| **SkillProficiency** | 숙련도 경험치 누적, 레벨 계산, 레벨업 시 스킬 강화 수치 적용 |
| **SkillDatabase** (SO) | 모든 SkillData ID 조회 |

### Gem

| 클래스 | 책임 |
|--------|------|
| **GemSocket** | 소켓 데이터: 장착된 GemData 참조, 소켓 타입(있다면) |
| **GemService** | 잼 장착/해제 로직. 스킬 효과 재계산 트리거. 인벤토리에서 잼 소모/반환 |

### Stat

| 클래스 | 책임 |
|--------|------|
| **StatType** | enum: ATK, DEF, HP, SPD, CritRate ... |
| **StatModifier** | 스탯 변경치: 타입, 값, 소스(장비/잼/버프), 연산방식(Flat/Percent) |
| **StatCalculator** | 기본 스탯 + 장비 보너스 + 잼 보너스 합산. 최종 스탯 산출 |

### UI

| 클래스 | 책임 |
|--------|------|
| **UIPanel** | 추상 베이스. Open/Close 애니메이션, UIManager 등록 |
| **InventoryUI** | 인벤토리 그리드 렌더링. 슬롯 생성/갱신 |
| **InventorySlotUI** | 개별 슬롯: 아이콘, 수량 표시. 클릭/드래그/우클릭 처리 |
| **EquipmentUI** | 캐릭터 모델 + 8개 장비 슬롯 배치 |
| **EquipSlotUI** | 장비 슬롯 하나: 드롭 수신, 장착 요청 |
| **SkillUI** | 스킬 목록 + 선택된 스킬 상세 + 숙련도 바 |
| **SkillSlotUI** | 스킬 하나 표시 + 잼 소켓 UI 포함 |
| **GemSocketUI** | 잼 소켓 하나. 잼 드래그 수신 |
| **TooltipSystem** | 툴팁 표시/숨기기 총괄. 마우스 따라다니기 |
| **ItemTooltipUI** | 아이템 정보 포맷팅 (이름, 스탯, 설명, 비교) |
| **SkillTooltipUI** | 스킬 정보 포맷팅 (이름, 레벨, 효과, 잼 효과) |
| **DraggableItem** | 드래그 앤 드롭 공용 컴포넌트 |

---

## 5. 데이터 흐름

### 아이템 획득 → 인벤토리
```
아이템 드롭/획득
  → InventoryService.AddItem(ItemData, count)
    → InventorySystem에 ItemInstance 생성/스택 증가
      → EventBus.Publish(OnInventoryChanged)
        → InventoryUI.Refresh()
```

### 장비 장착
```
InventorySlotUI 더블클릭 or 드래그→EquipSlotUI
  → EquipmentService.Equip(slot, itemInstance)
    → 조건 검증 (타입 매칭, 레벨 등)
    → 기존 장비 있으면 → InventoryService.AddItem(기존 장비)
    → EquipmentSystem[slot] = newItem
    → InventoryService.RemoveItem(newItem)
    → StatCalculator.Recalculate()
    → EventBus.Publish(OnEquipmentChanged)
      → EquipmentUI.Refresh()
      → InventoryUI.Refresh()
```

### 스킬 숙련도 증가
```
스킬 사용 (전투 중)
  → SkillSystem.UseSkill(skillInstance)
    → SkillProficiency.AddExp(amount)
      → 레벨업 체크
        → if 레벨업: EventBus.Publish(OnSkillLevelUp)
          → SkillUI.Refresh() (숙련도 바 갱신)
```

### 잼 장착
```
GemSocketUI에 잼 드래그
  → GemService.AttachGem(skillInstance, socketIndex, gemItemInstance)
    → 소켓 빈 자리 검증
    → InventoryService.RemoveItem(gemItem)
    → SkillInstance.Sockets[index].Gem = gemData
    → 스킬 효과 재계산
    → EventBus.Publish(OnGemChanged)
      → SkillUI.Refresh()
```

### UI 열기/닫기 → 게임 상태
```
플레이어가 I키 누름
  → UIManager.Toggle<InventoryUI>()
    → 패널 열기
    → UIManager → GameManager.SetState(GameState.UI)
      → Time.timeScale = 0
      → Cursor.lockState = CursorLockMode.None
      → Cursor.visible = true
ESC키 or 닫기 버튼
  → UIManager.CloseTop()
    → 열린 패널 없으면 → GameManager.SetState(GameState.Playing)
      → Time.timeScale = 1
      → Cursor.lockState = CursorLockMode.Locked
```

---

## 6. 구현 순서 (단계별)

### Phase 1: 기반 시스템
1. `ItemEnums`, `StatType` — enum 정의
2. `ItemData` (SO 베이스) + 서브클래스들
3. `ItemInstance` 런타임 래퍼
4. `ItemDatabase` (SO)
5. `EventBus` 이벤트 시스템
6. `GameManager`, `UIManager` 코어

### Phase 2: 인벤토리
7. `InventorySystem` (데이터 컨테이너)
8. `InventoryService` (추가/제거/이동 로직)
9. `UIPanel` 베이스
10. `InventoryUI` + `InventorySlotUI`
11. `TooltipSystem` + `ItemTooltipUI`

### Phase 3: 장비 시스템
12. `EquipmentSystem` (슬롯 데이터)
13. `EquipmentService` (장착/해제)
14. `StatModifier` + `StatCalculator`
15. `EquipmentUI` + `EquipSlotUI`
16. 드래그 앤 드롭 (`DraggableItem`)

### Phase 4: 스킬 시스템
17. `SkillData` (SO) + `SkillDatabase`
18. `SkillInstance` + `SkillProficiency`
19. `SkillSystem`
20. `SkillUI` + `SkillSlotUI` + `SkillTooltipUI`

### Phase 5: 잼 시스템
21. `GemData` (SO) 확장
22. `GemSocket` + `GemService`
23. `GemSocketUI`
24. 스킬 효과 재계산 연동

### Phase 6: 폴리싱
25. UI 상태 관리 통합 테스트
26. 아이템 비교 툴팁
27. 사운드/애니메이션
28. 저장/불러오기 연동

---

## 7. Claude에게 단계별 요청하는 방법

각 단계별로 아래와 같이 요청하면 효율적입니다:

### Phase 1 요청 예시
```
"Phase 1을 구현해줘.
- ItemEnums.cs: ItemType(Consumable, Armor, Weapon, SubWeapon, Gem, KeyItem),
  EquipSlotType(Weapon, OffHand, Helmet, Chest, Legs, Boots, Ring1, Ring2),
  Rarity enum을 만들어줘.
- ItemData ScriptableObject 베이스 클래스와 서브클래스들을 만들어줘.
  무기는 공격력/공격속도, 방어구는 방어력/슬롯타입 필드 포함.
- ItemInstance 런타임 클래스를 만들어줘. ItemData 참조 + 수량 + 고유ID.
- ItemDatabase SO를 만들어줘. List<ItemData>를 Dictionary로 캐싱.
- EventBus를 만들어줘. 제네릭 이벤트 시스템으로.
- GameManager/UIManager 싱글턴을 만들어줘."
```

### Phase 2 요청 예시
```
"Phase 2를 구현해줘.
- InventorySystem: ItemInstance[] 슬롯 배열 (최대 40칸).
- InventoryService: AddItem, RemoveItem, MoveItem, StackItem 메서드.
  EventBus로 OnInventoryChanged 발행.
- UIPanel 추상 베이스: Open/Close virtual 메서드.
- InventoryUI: Grid Layout으로 슬롯 생성, InventoryService 구독.
- InventorySlotUI: 아이콘+수량 표시, 클릭/우클릭 처리.
- TooltipSystem: 마우스 위치 추적, Show/Hide.
- ItemTooltipUI: 아이템 이름(등급 색상), 타입, 스탯, 설명 표시."
```

### Phase 3 요청 예시
```
"Phase 3를 구현해줘.
- EquipmentSystem: Dictionary<EquipSlotType, ItemInstance> 관리.
- EquipmentService: Equip/Unequip. 타입 검증, 기존 장비 인벤토리 반환.
- StatCalculator: 기본스탯 + 장비 StatModifier 합산.
- EquipmentUI: 8개 슬롯 고정 배치. 캐릭터 이미지 중앙.
- EquipSlotUI: 허용 타입 필터링, 드롭 수신.
- DraggableItem: IDragHandler/IDropHandler 구현."
```

### Phase 4 요청 예시
```
"Phase 4를 구현해줘.
- SkillData SO: 스킬명, 아이콘, 설명, 기본 데미지, 쿨다운, 잼소켓수.
- SkillInstance: SkillData 참조 + SkillProficiency + GemSocket[].
- SkillProficiency: currentExp, level, expToNextLevel 계산.
  사용 시 AddExp(), 레벨업 시 이벤트.
- SkillSystem: 보유 스킬 목록. UseSkill() 호출 시 숙련도 증가.
- SkillUI: 스킬 목록 + 선택 시 상세 패널. 숙련도 프로그레스 바."
```

### Phase 5 요청 예시
```
"Phase 5를 구현해줘.
- GemSocket: 소켓 데이터 클래스. 장착된 GemData 참조.
- GemService: AttachGem/DetachGem. 인벤토리 연동. 스킬 효과 재계산.
- GemSocketUI: 잼 드래그 수신. 잼 아이콘 표시. 우클릭 해제.
- 스킬 효과에 잼 StatModifier 합산 반영."
```

### 요청 시 팁
- **한 Phase씩** 요청 → 구현 → 테스트 → 다음 Phase
- **이전 코드 컨텍스트**를 Claude가 볼 수 있게 해당 파일을 참조
- **변경 사항이 있으면** "Phase 2인데 InventorySystem 슬롯을 60칸으로 바꿔줘" 식으로 구체적으로
- **통합 시점**에 "Phase 1~3 전체가 잘 연동되는지 확인하고 빠진 부분 채워줘" 요청

---

## 8. 핵심 설계 원칙

### 확장성
- **ScriptableObject 기반 데이터**: 새 아이템 타입 추가 시 SO 서브클래스만 생성
- **EventBus 패턴**: 시스템 간 직접 참조 없이 이벤트로 통신 → 새 시스템 추가 용이
- **제네릭 슬롯 구조**: `InventorySlotUI`를 재사용하여 상점/창고 등에 활용 가능

### 유지보수성
- **데이터/로직/UI 분리**: System(데이터) ↔ Service(로직) ↔ UI(표시) 3계층
- **단방향 데이터 흐름**: Service → System 변경 → Event 발행 → UI 갱신
- **enum 기반 타입 안전성**: 문자열 대신 enum으로 슬롯/아이템 타입 관리

### UI 연결 편의성
- **UIPanel 베이스 클래스**: 모든 UI 패널의 Open/Close/Toggle 통일
- **UIManager 스택**: ESC로 최상위 패널 닫기, 모두 닫히면 게임 재개
- **이벤트 기반 자동 갱신**: 데이터 변경 시 UI가 자동으로 Refresh

---

## 9. 기술 스택 참고

| 항목 | 추천 |
|------|------|
| UI 프레임워크 | Unity UI (uGUI) — Canvas + RectTransform |
| 레이아웃 | Grid Layout Group (인벤토리), 고정 배치 (장비) |
| 드래그 앤 드롭 | IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler |
| 툴팁 위치 | RectTransformUtility.ScreenPointToLocalPointInRectangle |
| 직렬화 | JsonUtility 또는 Newtonsoft.Json (저장/불러오기) |
| 이벤트 | C# event/Action 기반 EventBus (외부 에셋 불필요) |

---

## 10. 설치 및 설정 방법

### 씬 구성 (필수 GameObject)

```
[Hierarchy]
├── GameManager          ← GameManager.cs
├── SystemManager        ← InventoryManager.cs
│                          EquipmentManager.cs
│                          PlayerStatManager.cs
│                          SkillManager.cs
│                          GemService.cs
├── UIManager            ← UIManager.cs
├── Canvas (Screen Space - Overlay)
│   ├── InventoryPanel   ← InventoryPanelPlaceholder.cs (비활성 시작)
│   ├── EquipmentPanel   ← EquipmentPanelPlaceholder.cs (비활성 시작)
│   └── SkillPanel       ← SkillPanelPlaceholder.cs (비활성 시작)
├── Player (1인칭)       ← PlayerInputBlocker.cs
│   ├── (이동 스크립트)     Inspector에서 playerController에 드래그
│   └── (카메라 스크립트)   Inspector에서 cameraController에 드래그
└── InventoryDebugger    ← InventoryDebugger.cs (테스트용)
```

### 단계별 설정

1. **빈 씬**에서 Hierarchy를 위 구성대로 생성한다.
2. **GameManager 오브젝트**에 `GameManager.cs` 부착
   - Inspector: `Lock Cursor On Start` = true (1인칭), `Pause On UI` = true
3. **SystemManager 오브젝트**에 5개 컴포넌트 부착:
   - `InventoryManager.cs` — Inspector: `Slot Count` = 40, `Item Database`에 SO 드래그
   - `EquipmentManager.cs` — 추가 설정 없음
   - `PlayerStatManager.cs` — Inspector: `Base Stats` 배열의 기본 스탯값 조정
   - `SkillManager.cs` — Inspector: `Skill Database`에 SkillDatabase SO 드래그
   - `GemService.cs` — 추가 설정 없음
4. **UIManager 오브젝트**에 `UIManager.cs` 부착
   - Inspector: `Panel Registry` 배열에 3개 패널 드래그 (아래 5번 참조)
   - 키 설정: I=Inventory, E=Equipment, K=Skill, ESC=Close
5. **Canvas** 생성 → 아래에 3개 패널 GameObject 생성 (각각 비활성):
   - InventoryPanel에 `InventoryPanelPlaceholder.cs` 부착, panelName = "Inventory"
   - EquipmentPanel에 `EquipmentPanelPlaceholder.cs` 부착, panelName = "Equipment"
   - SkillPanel에 `SkillPanelPlaceholder.cs` 부착, panelName = "Skill"
6. **Player 오브젝트**에 `PlayerInputBlocker.cs` 부착
   - Inspector: `Player Controller`에 이동 스크립트 드래그
   - Inspector: `Camera Controller`에 카메라 스크립트 드래그
7. **InventoryDebugger 오브젝트**에 `InventoryDebugger.cs` 부착 (테스트용)
   - `Test Items`, `Test Skills`, `Test Gems` 배열 설정

### 테스트 키 바인딩

| 키 | 기능 |
|----|------|
| `1~9` | 테스트 아이템 선택 |
| `F1` | 선택한 아이템 추가 |
| `F2` | 테스트 슬롯에서 아이템 제거 |
| `F3` | 테스트 슬롯 아이템 사용 (장비면 장착) |
| `F4` | 인벤토리 콘솔 출력 |
| `F5` | 인벤토리 정렬 |
| `F6` | 장비 슬롯 콘솔 출력 |
| `F7` | 최종 스탯 콘솔 출력 |
| `F8` | 스킬 목록 콘솔 출력 |
| `F9` | 첫 번째 스킬 사용 (숙련도 +EXP) |
| `F10` | 첫 번째 스킬에 경험치 추가 |

### ContextMenu (Inspector 우클릭)

모든 매니저와 InventoryDebugger에서 ContextMenu 제공.
InventoryDebugger는 카테고리별로 정리:

- **Inventory/** — 아이템 추가/제거/정렬
- **Equipment/** — 장비 장착/해제
- **Stats/** — 스탯 출력
- **Skill/** — 스킬 습득/사용/경험치/레벨업
- **Gem/** — 잼 추가/장착/해제
- **Full Test/** — 스킬+잼 전체 흐름 테스트

### 이벤트 구독 (UI 연동 시)

```csharp
// UI 스크립트에서
void OnEnable() {
    EventBus.Subscribe<InventoryChangedEvent>(OnSlotChanged);
    EventBus.Subscribe<InventoryRefreshEvent>(OnFullRefresh);
    EventBus.Subscribe<EquipmentChangedEvent>(OnEquipChanged);
    EventBus.Subscribe<StatChangedEvent>(OnStatChanged);
    EventBus.Subscribe<SkillListChangedEvent>(OnSkillListChanged);
    EventBus.Subscribe<SkillLevelUpEvent>(OnSkillLevelUp);
    EventBus.Subscribe<GemAttachedEvent>(OnGemAttached);
    EventBus.Subscribe<GemDetachedEvent>(OnGemDetached);
}
void OnDisable() {
    EventBus.Unsubscribe<InventoryChangedEvent>(OnSlotChanged);
    EventBus.Unsubscribe<InventoryRefreshEvent>(OnFullRefresh);
    EventBus.Unsubscribe<EquipmentChangedEvent>(OnEquipChanged);
    EventBus.Unsubscribe<StatChangedEvent>(OnStatChanged);
    EventBus.Unsubscribe<SkillListChangedEvent>(OnSkillListChanged);
    EventBus.Unsubscribe<SkillLevelUpEvent>(OnSkillLevelUp);
    EventBus.Unsubscribe<GemAttachedEvent>(OnGemAttached);
    EventBus.Unsubscribe<GemDetachedEvent>(OnGemDetached);
}
```

### 장비 슬롯 처리 방식

| 아이템 타입 | 장착 슬롯 | 결정 방식 |
|------------|----------|----------|
| WeaponData | Weapon | 자동 (타입 매칭) |
| SubWeaponData | OffHand | 자동 (타입 매칭) |
| ArmorData (equipSlot=Helmet) | Helmet | SO의 equipSlot 필드 |
| ArmorData (equipSlot=Chest) | Chest | SO의 equipSlot 필드 |
| ArmorData (equipSlot=Legs) | Legs | SO의 equipSlot 필드 |
| ArmorData (equipSlot=Boots) | Boots | SO의 equipSlot 필드 |
| ArmorData (equipSlot=Ring1/Ring2) | Ring1 or Ring2 | 자동 배치 (빈 슬롯 우선) |

### 스탯 계산 공식

```
최종값 = (기본값 + Flat 보너스 합계) × (1 + Percent 보너스 합계)
```

- 기본값: PlayerStatManager의 baseStats
- Flat 보너스: 장비의 attackPower, defense + StatModifier(Flat)
- Percent 보너스: StatModifier(Percent)
- 장비 변경 시 자동 재계산 (EquipmentChangedEvent → Recalculate)

### 스킬 시스템

#### 스킬 종류 (SkillType)

| 타입 | 설명 | 숙련도 경험치 |
|------|------|-------------|
| Active | 공격 스킬 (데미지 적용) | 사용 시 획득 |
| Buff | 버프 스킬 (스탯 효과) | 사용 시 획득 |
| Passive | 패시브 (항상 적용) | 사용 불가 (직접 AddExp) |

#### 숙련도 시스템

```
스킬 사용 → expPerUse 만큼 경험치 증가
  → currentExp >= requiredExp 이면 레벨업
    → 필요 경험치: baseExpPerLevel × (level + 1)
    → 레벨업 시 소켓 해금 체크
    → 남은 경험치 다음 레벨에 이월
```

#### 잼 소켓 해금

SkillData SO에서 `socketUnlockLevels` 배열로 설정:
- 예: `[1, 3, 5]` → Lv.1에서 Socket[0], Lv.3에서 Socket[1], Lv.5에서 Socket[2] 해금

#### 잼 효과 반영

```
최종 데미지 = baseDamage × (1 + damageScale × level) × (1 + 잼 damageBonus 합)
최종 쿨다운 = cooldown - 잼 cooldownReduction 합 (최소 0.1초)
버프 효과 = baseStatEffects + 잼 statModifiers (합산)
```

#### 스킬 사용 코드 예시

```csharp
// 전투 시스템에서
var skill = SkillManager.Instance.GetSkillByIndex(0);
SkillManager.Instance.UseSkill(skill);
// → 숙련도 EXP 증가
// → SkillUsedEvent 발행 (damage, cooldown, mpCost 포함)
// → 레벨업 시 SkillLevelUpEvent 발행 + 소켓 자동 해금

// 잼 장착
var gemItem = InventoryManager.Instance.Slots[3].item; // 인벤토리의 잼
GemService.Instance.AttachGem(skill, 0, gemItem);
// → 인벤토리에서 잼 1개 제거
// → 소켓에 잼 데이터 저장
// → 스킬 데미지/쿨다운에 잼 효과 반영

// 잼 해제
GemService.Instance.DetachGem(skill, 0);
// → 소켓에서 잼 제거
// → 인벤토리에 잼 1개 반환
```

### Unity 테스트 절차

1. **SO 생성**: Create → RPG System → Skills → Skill
   - `skillId`: "fireball", `skillName`: "파이어볼"
   - `skillType`: Active, `baseDamage`: 30, `cooldown`: 3, `mpCost`: 15
   - `maxGemSockets`: 3, `socketUnlockLevels`: [1, 3, 5]
   - `maxProficiencyLevel`: 10, `baseExpPerLevel`: 100, `expPerUse`: 10
2. **잼 SO 생성**: Create → RPG System → Items → Gem
   - "화염의 잼": `skillDamageBonus`: 0.1, `cooldownReduction`: 0.3
3. **SkillDatabase SO 생성**: 스킬 등록
4. **Inspector 설정**: InventoryDebugger에 testSkills, testGems 배열 설정
5. **Play → ContextMenu**:
   - `Skill/Learn All Test Skills` → 스킬 습득
   - `Skill/Use First Skill x10` → 10회 사용하여 숙련도 상승 확인
   - `Gem/Add Test Gems` → 인벤토리에 잼 추가
   - `Gem/Attach First Gem` → 잼 장착, 스킬 데미지 변화 확인
   - `Full Test/Skill + Gem Full Flow` → 전체 흐름 한번에 실행

### UI 모드 전환 시스템

#### 상태 전환 흐름

```
Playing (게임 중)
  │
  ├── I키 → UIManager.TogglePanel("Inventory")
  │          → UIManager.OpenPanel() → GameManager.SetState(UI)
  │            → Time.timeScale = 0
  │            → Cursor.lockState = None, visible = true
  │            → GameStateChangedEvent 발행
  │              → PlayerInputBlocker: 이동/카메라 스크립트 disabled
  │
  ├── E키 → 장비 패널 (위와 동일)
  ├── K키 → 스킬 패널 (위와 동일)
  │
  └── ESC → 열린 패널이 없으면 Paused 토글

UI (UI 열림)
  │
  ├── ESC → UIManager.CloseTop() (스택 최상위 닫기)
  │         → 스택이 비면 → GameManager.SetState(Playing)
  │           → Time.timeScale = 1
  │           → Cursor.lockState = Locked, visible = false
  │           → PlayerInputBlocker: 이동/카메라 스크립트 enabled
  │
  ├── I키 → 인벤토리가 열려있으면 닫기, 아니면 열기
  └── 여러 패널 동시 열기 가능 (스택 관리)
```

#### 패널 스택 동작

```
[1] I키 → Inventory 열림 (스택: [Inventory])      → GameState.UI
[2] E키 → Equipment 열림 (스택: [Inventory, Equipment]) → 유지 UI
[3] ESC → Equipment 닫힘 (스택: [Inventory])       → 유지 UI
[4] ESC → Inventory 닫힘 (스택: [])                → GameState.Playing
```

#### Time.timeScale 주의사항

- `Time.timeScale = 0`이면 `Update()`는 호출되지만 `FixedUpdate()`는 멈춤
- **UI 애니메이션**: `Animator.updateMode = AnimatorUpdateMode.UnscaledTime` 설정 필요
- **UI 입력**: `Input.GetKeyDown()`은 timeScale=0에서도 정상 동작
- **코루틴**: `WaitForSeconds`는 멈추지만 `WaitForSecondsRealtime`은 동작
- **DOTween 등**: `.SetUpdate(true)`로 unscaled time 사용 가능

#### 1인칭 컨트롤러 연결 방법

**방법 1: PlayerInputBlocker (권장)**
```csharp
// Player 오브젝트에 PlayerInputBlocker 부착
// Inspector에서 이동/카메라 스크립트 드래그
// → UI 열면 자동으로 enabled = false
```

**방법 2: GameManager.IsInputAllowed 확인**
```csharp
void Update() {
    if (!GameManager.Instance.IsInputAllowed) return;
    // 이동 로직
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");
    // ...
}
```

**방법 3: GameStateChangedEvent 구독**
```csharp
void OnEnable() {
    EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
}
void OnStateChanged(GameStateChangedEvent e) {
    enabled = e.newState == GameState.Playing;
}
```

#### UI 상태 테스트 (ContextMenu)

- `UI State/Print Game State` — 현재 상태, timeScale, 커서 상태 출력
- `UI State/Toggle Inventory/Equipment/Skill` — 패널 토글
- `UI State/UI Flow Test` — 열기→스택→ESC닫기 전체 흐름 자동 테스트
