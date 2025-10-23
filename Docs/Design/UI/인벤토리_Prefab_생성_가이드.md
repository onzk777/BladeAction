# 인벤토리 Prefab 생성 가이드

## 📋 문서 목적
Unity 에디터에서 인벤토리 UI Prefab을 생성하는 방법을 단계별로 안내합니다.

---

## 🎯 생성할 Prefab 목록

### 필수 Prefab (4개)
1. **ItemSlotUI** - 아이템 슬롯
2. **EquipmentSlotUI** - 장비 슬롯  
3. **EquippedSwordArtStyleUI** - 검술 아이템 ⭐ NEW
4. **MainInventoryPanel** - 전체 인벤토리 UI

---

## 💡 RectTransform 설정 방법

### Inspector에서 보는 필드 설명

#### Anchors 설정
- **위치**: Inspector 좌측 상단의 십자 모양 사각형 아이콘
- **빠른 설정**: Alt+Shift를 누른 채로 프리셋 클릭하면 Position도 함께 맞춤

#### 자주 사용하는 Anchors 프리셋
1. **Stretch-Stretch** (4방향 늘림)
   - 부모 크기에 맞춰 늘어남
   - Inspector에서 보이는 필드: `Left`, `Top`, `Right`, `Bottom`
   - 예: (0, 0, 0, 0) = 부모 영역 꽉 채움

2. **Center-Middle** (중앙 고정)
   - 중앙에 고정된 크기
   - Inspector에서 보이는 필드: `Pos X`, `Pos Y`, `Width`, `Height`
   - 예: Pos X=0, Pos Y=0 = 정중앙

3. **Left-Middle** (좌측 고정)
   - 왼쪽 중앙에 고정
   - Inspector에서 보이는 필드: `Pos X`, `Pos Y`, `Width`, `Height`
   
4. **Bottom-Right** (우하단 고정)
   - 오른쪽 아래에 고정
   - Inspector에서 보이는 필드: `Pos X`, `Pos Y`, `Width`, `Height`

#### Pivot 설정
- **위치**: Inspector에서 Anchors 아래
- **의미**: 오브젝트의 기준점 (0,0)이 왼쪽 아래, (1,1)이 오른쪽 위
- **예시**: 
  - 중앙: X=0.5, Y=0.5
  - 좌하단: X=0, Y=0
  - 우하단: X=1, Y=0

---

## 1️⃣ ItemSlotUI Prefab 생성

### 📦 용도
인벤토리 그리드의 개별 아이템 슬롯

### 🏗️ 오브젝트 구조
```
ItemSlotUI (GameObject)
├── Background (Image) - 배경
├── IconImage (Image) - 아이템 아이콘 (100x100)
├── QuantityText (TextMeshProUGUI) - 수량
└── HighlightImage (Image) - 선택 하이라이트
```

### ✅ 생성 단계

#### 1. 기본 GameObject 생성
1. Hierarchy에서 우클릭 → `UI` → `Image`
2. 이름을 `ItemSlotUI`로 변경

#### 2. 컴포넌트 추가
- `Button` 컴포넌트 추가 (Add Component)
- `ItemSlotUI` 스크립트 추가 (Assets/Script/UI/ItemSlotUI.cs)

#### 3. 하위 오브젝트 생성

**Background (배경)**
- ItemSlotUI 우클릭 → `UI` → `Image`
- 이름: `Background`
- RectTransform 설정:
  - Anchors: **Stretch-Stretch** (Alt+Shift 클릭으로 한번에 설정)
  - Left: `0`, Top: `0`, Right: `0`, Bottom: `0`
- Color: 회색 (0.2, 0.2, 0.2, 1)

**IconImage (아이콘)**
- ItemSlotUI 우클릭 → `UI` → `Image`
- 이름: `IconImage`
- RectTransform 설정:
  - Anchors: **Center-Middle**
  - Pos X: `0`, Pos Y: `0`
  - Width: `100`, Height: `100`
- Raycast Target: **OFF** (클릭 이벤트 방해 방지)

**QuantityText (수량)**
- ItemSlotUI 우클릭 → `UI` → `TextMeshPro - Text`
- 이름: `QuantityText`
- RectTransform 설정:
  - Anchors: **Bottom-Right**
  - Pivot: X `1`, Y `0` (우하단)
  - Pos X: `-5`, Pos Y: `5`
  - Width: `50`, Height: `30`
- Font Size: 16
- Alignment: Right Bottom
- Color: 흰색
- Raycast Target: **OFF**

**HighlightImage (하이라이트)**
- ItemSlotUI 우클릭 → `UI` → `Image`
- 이름: `HighlightImage`
- RectTransform 설정:
  - Anchors: **Stretch-Stretch**
  - Left: `2`, Top: `2`, Right: `-2`, Bottom: `-2`
- Color: 노란색 (1, 1, 0, 0.5) - 반투명
- Raycast Target: **OFF**

#### 4. 스크립트 참조 연결
ItemSlotUI 컴포넌트의 Inspector에서:
- `Icon Image` → IconImage 드래그
- `Quantity Text` → QuantityText 드래그
- `Background Image` → Background 드래그
- `Highlight Image` → HighlightImage 드래그
- `Empty Slot Icon` → (선택사항) 빈 슬롯 아이콘 Sprite

#### 5. Prefab 저장
- ItemSlotUI를 `Assets/Prefab/` 폴더로 드래그
- Hierarchy에서 삭제

---

## 2️⃣ EquipmentSlotUI Prefab 생성

### 📦 용도
장비 슬롯 (무기, 갑옷, 장신구, 검술 유파)

### 🏗️ 오브젝트 구조
```
EquipmentSlotUI (GameObject)
├── FrameImage (Image) - 테두리
├── IconImage (Image) - 아이템 아이콘 (100x100)
├── SlotNameText (TextMeshProUGUI) - 슬롯 이름
└── ItemNameText (TextMeshProUGUI) - 아이템 이름
```

### ✅ 생성 단계

#### 1. 기본 GameObject 생성
1. Hierarchy에서 우클릭 → `UI` → `Button`
2. 이름을 `EquipmentSlotUI`로 변경

#### 2. 컴포넌트 추가
- `EquipmentSlotUI` 스크립트 추가

#### 3. 하위 오브젝트 생성

**FrameImage (테두리)**
- EquipmentSlotUI 우클릭 → `UI` → `Image`
- 이름: `FrameImage`
- RectTransform 설정:
  - Anchors: **Stretch-Stretch**
  - Left: `0`, Top: `0`, Right: `0`, Bottom: `0`
- Color: 회색 (0.5, 0.5, 0.5, 1)
- Image Type: Sliced (테두리 이미지 사용 시)

**IconImage (아이콘)**
- EquipmentSlotUI 우클릭 → `UI` → `Image`
- 이름: `IconImage`
- RectTransform 설정:
  - Anchors: **Left-Middle**
  - Pivot: X `0`, Y `0.5` (좌측 중앙)
  - Pos X: `10`, Pos Y: `0`
  - Width: `100`, Height: `100`
- Raycast Target: **OFF**

**SlotNameText (슬롯 이름)**
- EquipmentSlotUI 우클릭 → `UI` → `TextMeshPro - Text`
- 이름: `SlotNameText`
- RectTransform 설정:
  - Anchors: **Left Stretch - Top** (좌우 늘림, 위쪽 고정)
  - Left: `120` (아이콘 오른쪽)
  - Top: `20` (위에서 20픽셀 아래)
  - Right: `-10` (오른쪽 여백 10)
  - Height: `20`
- Text: "무기"
- Font Size: 14
- Color: 회색
- Raycast Target: **OFF**

**ItemNameText (아이템 이름)**
- EquipmentSlotUI 우클릭 → `UI` → `TextMeshPro - Text`
- 이름: `ItemNameText`
- RectTransform 설정:
  - Anchors: **Left Stretch - Bottom** (좌우 늘림, 아래쪽 고정)
  - Left: `120` (아이콘 오른쪽)
  - Bottom: `20` (아래에서 20픽셀 위)
  - Right: `-10` (오른쪽 여백 10)
  - Height: `24`
- Text: ""
- Font Size: 16
- Color: 흰색
- Raycast Target: **OFF**

#### 4. 스크립트 참조 연결
- `Icon Image` → IconImage
- `Name Text` → ItemNameText
- `Slot Name Text` → SlotNameText
- `Frame Image` → FrameImage
- `Empty Slot Icon` → (선택사항)
- `Hide Text For Accessory` → ✓ (체크)

#### 5. Prefab 저장
- EquipmentSlotUI를 `Assets/Prefab/`로 드래그
- Hierarchy에서 삭제

---

## 3️⃣ ActionCommandItemUI Prefab 생성 ⭐ NEW

### 📦 용도
검술 유파의 검술 목록에 표시되는 개별 검술 아이템

### 🏗️ 오브젝트 구조
```
ActionCommandItemUI (GameObject)
├── BackgroundImage (Image) - 배경
├── CommandNameText (TextMeshProUGUI) - 검술 이름
└── CommandTagText (TextMeshProUGUI) - 검술 태그 (선택사항)
```

### ✅ 생성 단계

#### 1. 기본 GameObject 생성
1. Hierarchy에서 우클릭 → `UI` → `Image`
2. 이름을 `ActionCommandItemUI`로 변경
3. **이 Image가 BackgroundImage 역할을 담당**

#### 2. 컴포넌트 추가
- `ActionCommandItemUI` 스크립트 추가 (Assets/Script/UI/ActionCommandItemUI.cs)

#### 3. 하위 오브젝트 생성

**BackgroundImage (배경)**
- 최상위 Image를 BackgroundImage로 사용
- 이름: `BackgroundImage`
- RectTransform 설정:
  - Width: `250`, Height: `30`
- Color: 회색 (0.2, 0.2, 0.2, 1)

**CommandNameText (검술 이름)**
- ActionCommandItemUI 우클릭 → `UI` → `TextMeshPro - Text`
- 이름: `CommandNameText`
- RectTransform 설정:
  - Anchors: **Stretch-Stretch**
  - Left: `10`, Top: `5`, Right: `-10`, Bottom: `-5`
- Text: "검술 이름"
- Font Size: 18
- Alignment: Left Center
- Color: 흰색
- Raycast Target: **OFF**

**CommandTagText (검술 태그)**
- ActionCommandItemUI 우클릭 → `UI` → `TextMeshPro - Text`
- 이름: `CommandTagText`
- RectTransform 설정:
  - Anchors: **Right-Middle**
  - Pivot: X `1`, Y `0.5`
  - Pos X: `-10`, Pos Y: `0`
  - Width: `80`, Height: `24`
- Text: "[태그]"
- Font Size: 14
- Alignment: Right Center
- Color: 노란색 (1, 1, 0.5, 1)
- Raycast Target: **OFF**

#### 4. 스크립트 참조 연결
EquippedSwordArtStyleUI 컴포넌트의 Inspector에서:
- `Command Name Text` → CommandNameText 드래그
- `Command Tag Text` → CommandTagText 드래그
- `Background Image` → BackgroundImage 드래그

#### 5. Prefab 저장
- EquippedSwordArtStyleUI를 `Assets/Prefab/Inventory`로 드래그
- Hierarchy에서 삭제

---

## 4️⃣ MainInventoryPanel Prefab 생성

### 📦 용도
전체 인벤토리 UI (어제 작성한 UI 애셋 구조 활용)

### 🏗️ 오브젝트 구조
```
MainInventoryPanel (GameObject)
├── TopNavigationBar
├── MainContentArea
│   ├── EquipmentPanel
│   │   └── EquipmentSlotContainer (장비 슬롯들이 동적 생성될 컨테이너)
│   ├── InventoryGridPanel
│   │   └── InventoryScrollView
│   │       └── Viewport
│   │           └── Content (아이템 슬롯들이 동적 생성될 컨테이너)
│   └── ItemDetailsPanel
│       ├── ItemDetailInfo (아이콘 + 이름)
│       ├── ItemStatsInfo (스탯 정보)
│       ├── ItemDescription (설명)
│       └── ItemActionButtons (버튼들)
```

### ✅ 생성 단계

#### 1. 기존 UI 애셋 활용
어제 작성한 `MainInventoryPanel` 오브젝트를 그대로 활용합니다.

**확인 사항**:
- `MainInventoryPanel` 최상위 GameObject 존재
- 내부 구조가 UI 오브젝트 구조 문서와 일치
- ScrollRect 구조가 올바른지 확인 (Viewport → Content)

#### 2. 컴포넌트 추가
MainInventoryPanel에 `InventoryUI` 스크립트 추가

#### 3. 추가 컴포넌트 설정

**ItemDetailPanel 컴포넌트 추가**:
- ItemDetailsPanel GameObject에 `ItemDetailPanel` 스크립트 추가

**SwordArtDisplayUI 컴포넌트 추가** ⭐ NEW:
- EquippedSwordArtStyle GameObject에 `SwordArtDisplayUI` 스크립트 추가

#### 4. InventoryUI 스크립트 참조 연결

**인벤토리 참조**:
- `Inventory` → (런타임에 설정, null로 둠)

**UI 컨테이너 참조**:
- `Panel` → MainInventoryPanel
- `Item Grid Container` → InventoryScrollView/Viewport/Content (GridLayoutGroup이 있는 것)
- `Equipment Slot Container` → EquipmentPanel/EquipmentSlotContainer (또는 적절한 부모)

**Prefab 참조**:
- `Item Slot Prefab` → ItemSlotUI Prefab
- `Equipment Slot Prefab` → EquipmentSlotUI Prefab

**패널 참조**:
- `Item Detail Panel` → ItemDetailsPanel (ItemDetailPanel 스크립트가 붙은 것)
- `Sword Art Display UI` → EquippedSwordArtStyle (SwordArtDisplayUI 스크립트가 붙은 것)

**UI 설정**:
- `Max Display Slots` → 36 (6x6 그리드)
- `Auto Subscribe Events` → ✓ (체크)
- `Enable Debug Log` → ✓ (체크, 테스트용)

#### 5. ItemDetailPanel 스크립트 참조 연결

ItemDetailsPanel의 ItemDetailPanel 컴포넌트에서:

**인벤토리 참조**:
- `Inventory` → (런타임에 설정, null로 둠)

**UI 컴포넌트 - 기본 정보**:
- `Item Icon` → SelectedItemIcon (Image)
- `Item Name Text` → SelectedItemName (TextMeshProUGUI)

**UI 컴포넌트 - 스탯 정보**:
- `Stat Info Texts` → Size 6으로 설정
  - Element 0 → STATINFO_01
  - Element 1 → STATINFO_02
  - Element 2 → STATINFO_03
  - Element 3 → STATINFO_04
  - Element 4 → STATINFO_05
  - Element 5 → STATINFO_06

**UI 컴포넌트 - 설명**:
- `Description Text` → ItemDescriptionText (ScrollRect 안의 TextMeshProUGUI)

**UI 컴포넌트 - 액션 버튼**:
- `Equip Button` → EquipButton (Button)
- `Equip Button Text` → EquipButton의 자식 Text (TextMeshProUGUI)
- `Use Button` → UseButton (Button)
- `Drop Button` → DropButton (Button)

**디버그**:
- `Enable Debug Log` → ✓ (체크, 테스트용)

#### 5-2. SwordArtDisplayUI 스크립트 참조 연결 ⭐ NEW

EquippedSwordArtStyle의 SwordArtDisplayUI 컴포넌트에서:

**인벤토리 참조**:
- `Inventory` → (런타임에 설정, null로 둠)

**UI 컴포넌트 - 유파 정보 (상단 영역)**:
- `Style Icon` → 유파아이콘 (Image) - 좌상단 2x2 그리드 크기
- `Style Name Text` → 유파이름 (TextMeshProUGUI) - 아이콘 우측
- `Style Description Text` → 유파설명 (TextMeshProUGUI) - 아이콘 하단 우측
- `Empty Slot Text` → (선택사항) 빈 슬롯 표시 텍스트

**UI 컴포넌트 - 검술 리스트 (하단 영역)**:
- `Command List Container` → 검술목록영역/Viewport/Content (VerticalLayoutGroup이 있는 것)
- `Command Item Prefab` → ActionCommandItemUI Prefab

**디버그**:
- `Enable Debug Log` → ✓ (체크, 테스트용)

#### 6. Prefab 저장
- MainInventoryPanel을 `Assets/Prefab/`로 드래그
- (Hierarchy에서 삭제하지 말고 유지)

---

## 🎨 추가 설정 (선택사항)

### GridLayoutGroup 설정
`InventoryScrollView/Viewport/Content`의 GridLayoutGroup:
- Cell Size: (120, 120)
- Spacing: (10, 10)
- Start Corner: Top Left
- Start Axis: Horizontal
- Child Alignment: Upper Left
- Constraint: Fixed Column Count → 6 (또는 전체 화면에 맞게 조정)

### ScrollRect 설정
InventoryScrollView의 ScrollRect:
- Content → Content GameObject
- Viewport → Viewport GameObject
- Horizontal Scrollbar → (없음)
- Vertical Scrollbar → (선택사항)
- Movement Type: Elastic
- Scroll Sensitivity: 20

---

## ✅ 검증 체크리스트

### ItemSlotUI Prefab
- [ ] Button 컴포넌트 존재
- [ ] ItemSlotUI 스크립트 존재
- [ ] 모든 참조가 연결됨
- [ ] HighlightImage가 초기에 비활성화됨

### EquipmentSlotUI Prefab
- [ ] Button 컴포넌트 존재
- [ ] EquipmentSlotUI 스크립트 존재
- [ ] 모든 참조가 연결됨
- [ ] Hide Text For Accessory 체크됨

### ActionCommandItemUI Prefab ⭐ NEW
- [ ] Image 컴포넌트 존재 (BackgroundImage)
- [ ] ActionCommandItemUI 스크립트 존재
- [ ] 모든 참조가 연결됨 (CommandNameText, CommandTagText, BackgroundImage)

### MainInventoryPanel Prefab
- [ ] InventoryUI 스크립트 존재
- [ ] ItemDetailPanel 스크립트 존재
- [ ] 모든 컨테이너 참조 연결
- [ ] Prefab 참조 연결
- [ ] GridLayoutGroup 설정 완료
- [ ] ScrollRect 구조 올바름 (Viewport → Content)

---

## 🚀 테스트 방법

### 1. 씬에 배치
- MainInventoryPanel Prefab을 Hierarchy로 드래그

### 2. 인벤토리 생성 및 연결
```csharp
// 테스트 스크립트 예시
CombatantInventory testInventory = new CombatantInventory();
testInventory.Initialize();

InventoryUI inventoryUI = FindObjectOfType<InventoryUI>();
inventoryUI.Initialize(testInventory);
```

### 3. 아이템 추가 테스트
```csharp
testInventory.AddItem("test_item_01", 1);
inventoryUI.RefreshAll();
```

### 4. UI 동작 확인
- [ ] 아이템 슬롯이 동적 생성됨
- [ ] 장비 슬롯이 6개 생성됨 (무기/갑옷/장신구3개/유파)
- [ ] 아이템 클릭 시 선택 하이라이트 표시
- [ ] 아이템 선택 시 상세 정보 패널에 표시
- [ ] 장착 버튼 클릭 시 아이템 장착
- [ ] 해제 버튼 클릭 시 아이템 해제
- [ ] ItemEvents 이벤트 발생 시 UI 자동 갱신

---

## 📝 주의사항

1. **TextMeshPro 설정**: 첫 사용 시 Import TMP Essentials 필요
2. **Raycast Target**: 클릭 이벤트가 필요 없는 UI는 Raycast Target OFF
   - Background, Icon, Text 등은 모두 **OFF**로 설정
   - 클릭을 받는 건 최상위 Button만!
3. **Anchor 설정**: 
   - Inspector 좌측 상단 사각형 아이콘으로 Anchors 프리셋 선택
   - Alt+Shift 누르고 클릭하면 Position도 함께 설정됨
4. **참조 연결**: 모든 스크립트 참조가 연결되었는지 확인
   - 연결 안 된 참조는 Inspector에서 **None (Transform)** 같은 식으로 표시됨
5. **Prefab 오버라이드**: Scene에서 수정 후 Apply All로 Prefab 업데이트
6. **크기 확인**: 
   - ItemSlotUI 아이콘: 100x100
   - EquipmentSlotUI 아이콘: 100x100
   - Grid Cell: 120x120

---

## 🔗 관련 문서
- `Docs/Design/UI/UI기획_인벤토리_오브젝트_구조.md` - 상세 UI 구조
- `Assets/Script/UI/ItemSlotUI.cs` - 아이템 슬롯 스크립트
- `Assets/Script/UI/EquipmentSlotUI.cs` - 장비 슬롯 스크립트
- `Assets/Script/UI/InventoryUI.cs` - 인벤토리 UI 스크립트
- `Assets/Script/UI/ItemDetailPanel.cs` - 아이템 상세 정보 패널 스크립트
- `Assets/Script/UI/SwordArtDisplayUI.cs` - 검술 유파 표시 패널 스크립트
- `Assets/Script/UI/EquippedSwordArtStyleUI.cs` - 검술 아이템 UI 스크립트

---

*작성일: 2025년 10월 23일*  
*작성자: AI Assistant*

