# 인벤토리 UI 오브젝트 구조

## 📋 문서 목적
Unity 에디터에서 인벤토리 UI를 구현하기 위한 오브젝트 구조와 설정 가이드를 제공합니다.

---

## 🎯 전체 오브젝트 계층 구조

```
Canvas (Screen Space - Overlay)
└── MainInventoryPanel (Panel)
    ├── TopNavigationBar (Panel + HorizontalLayoutGroup)
    │   ├── BagTab (Button)
    │   ├── SwordArtTab (Button)
    │   └── EmptyTabs (Button들)
    └── MainContentArea (Panel + HorizontalLayoutGroup)
        ├── EquipmentPanel (Panel + VerticalLayoutGroup)
        │   ├── CharacterEquipmentSlots (Panel)
        │   │   ├── WeaponSlot (HorizontalLayoutGroup)
        │   │   │   ├── WeaponIcon (Image)
        │   │   │   └── WeaponName (Text)
        │   │   ├── ArmorSlot (HorizontalLayoutGroup)
        │   │   │   ├── ArmorIcon (Image)
        │   │   │   └── ArmorName (Text)
        │   │   └── AccessorySlots (HorizontalLayoutGroup)
        │   │       ├── AccessorySlot1 (Image)
        │   │       ├── AccessorySlot2 (Image)
        │   │       └── AccessorySlot3 (Image)
        │   └── EquippedSwordArtStyle (Panel + VerticalLayoutGroup)
        │       ├── SwordArtStyleSlot (HorizontalLayoutGroup)
        │       │   ├── SwordArtStyleIcon (Image)
        │       │   └── SwordArtStyleName (Text)
        │       └── SwordsmanshipScrollView (ScrollRect)
        │           └── Viewport (Image + Mask)
        │               └── Content (RectTransform + VerticalLayoutGroup)
        ├── InventoryGridPanel (Panel + VerticalLayoutGroup)
        │   ├── InventoryTitle (Text)
        │   └── InventoryScrollView (ScrollRect)
        │       └── Viewport (Image + Mask)
        │           └── Content (RectTransform + GridLayoutGroup)  ← GridLayoutGroup 위치
        │               └── ItemSlots (ItemSlotUI Prefab들 - 동적 생성)
        └── ItemDetailsPanel (Panel + VerticalLayoutGroup)
            ├── SelectedItemDisplay (HorizontalLayoutGroup)
            │   ├── SelectedItemIcon (Image)
            │   └── SelectedItemName (Text)
            ├── ItemStatsInfo (VerticalLayoutGroup)
            │   ├── ATK_Stat (Text)
            │   ├── DEF_Stat (Text)
            │   └── SPD_Stat (Text)
            ├── ItemDescription (ScrollRect)
            │   └── Viewport (Image + Mask)
            │       └── Content (RectTransform)
            │           └── ItemDescriptionText (TextMeshProUGUI)
            └── ActionButtons (HorizontalLayoutGroup)
                ├── EquipButton (Button)
                ├── UseButton (Button)
                └── DropButton (Button)
```

---

## 📊 상세 오브젝트 구조

### 1. Canvas (최상위 컨테이너)

**오브젝트명**: `InventoryCanvas`
**컴포넌트**:
- `Canvas` (Render Mode: Screen Space - Overlay)
- `CanvasScaler` (UI Scale Mode: Scale With Screen Size, Reference Resolution: 1920x1080)
- `GraphicRaycaster`

**설정값**:
- Sort Order: 10 (전투 Canvas보다 위에 표시)
- Pixel Perfect: true

**⚠️ 중요**: 기존 전투 Canvas와 별도로 생성하여 UI 시스템 분리

---

### 2. MainInventoryPanel (메인 컨테이너)

**오브젝트명**: `MainInventoryPanel`
**컴포넌트**:
- `Image` (배경 이미지)
- `RectTransform` (전체 화면 크기)

**설정값**:
- Anchor: Stretch (전체 화면)
- Size: 1920x1080
- Background Color: 반투명 검정 (0, 0, 0, 0.8)

---

### 3. TopNavigationBar (상단 탭 바)

**오브젝트명**: `TopNavigationBar`
**컴포넌트**:
- `Image` (배경)
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**하위 오브젝트들**:

#### 3.1 가방 탭 (활성화)
**오브젝트명**: `BagTab`
**컴포넌트**:
- `Button`
- `Image` (배경 - 파란색)
- `Text` (텍스트: "가방")

#### 3.2 검술 탭 (비활성화)
**오브젝트명**: `SwordArtTab`
**컴포넌트**:
- `Button`
- `Image` (배경 - 회색)
- `Text` (텍스트: "검술")

#### 3.3 빈 탭들 (향후 확장용)
**오브젝트명**: `EmptyTab1`, `EmptyTab2`, `EmptyTab3`
**컴포넌트**:
- `Button`
- `Image` (투명)
- `Text` (빈 텍스트)

---

### 4. MainContentArea (메인 콘텐츠 영역)

**오브젝트명**: `MainContentArea`
**컴포넌트**:
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**설정값**:
- Spacing: 20
- Child Controls Size: true
- Child Force Expand: true (Width)

---

### 5. EquipmentPanel (왼쪽 - 장비 패널)

**오브젝트명**: `EquipmentPanel`
**컴포넌트**:
- `Image` (배경)
- `VerticalLayoutGroup`
- `ContentSizeFitter` (Vertical: Preferred Size)

**하위 오브젝트들**:

#### 5.1 캐릭터 장비 슬롯 영역
**오브젝트명**: `CharacterEquipmentSlots`
**컴포넌트**:
- `VerticalLayoutGroup`
- `ContentSizeFitter` (Vertical: Preferred Size)

##### 5.1.1 무기 슬롯
**오브젝트명**: `WeaponSlot`
**컴포넌트**:
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**하위 오브젝트**:
- `WeaponIcon` (Image, 64x64, 노란색 테두리)
- `WeaponName` (Text, "무기 이름")

##### 5.1.2 갑옷 슬롯
**오브젝트명**: `ArmorSlot`
**컴포넌트**:
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**하위 오브젝트**:
- `ArmorIcon` (Image, 64x64, 노란색 테두리)
- `ArmorName` (Text, "갑옷 이름")

##### 5.1.3 장신구 슬롯들
**오브젝트명**: `AccessorySlots`
**컴포넌트**:
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**하위 오브젝트**:
- `AccessorySlot1` (Image, 48x48, 회색 배경, 아이콘만)
- `AccessorySlot2` (Image, 48x48, 회색 배경, 아이콘만)
- `AccessorySlot3` (Image, 48x48, 회색 배경, 아이콘만)

**💡 참고**: 장신구 슬롯은 아이콘만 표시하고 텍스트는 없음

#### 5.2 장착 유파 + 검술 리스트 영역
**오브젝트명**: `EquippedSwordArtStyle`
**컴포넌트**:
- `VerticalLayoutGroup`
- `ContentSizeFitter` (Vertical: Preferred Size)

**💡 참고**: 이 영역에서 유파 슬롯 기능을 포함 (유파 아이콘 + 이름 + 검술 목록)

**하위 오브젝트**:

##### 5.2.1 장착된 유파 슬롯
**오브젝트명**: `SwordArtStyleSlot`
**컴포넌트**:
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**하위 오브젝트**:
- `SwordArtStyleIcon` (Image, 64x64, 노란색 테두리)
- `SwordArtStyleName` (Text, "유파 이름")

##### 5.2.2 검술 리스트
**오브젝트명**: `SwordsmanshipScrollView`
**컴포넌트**:
- `ScrollRect`

**하위 오브젝트**:
- `Viewport` (Image + Mask)
  - `Content` (RectTransform + VerticalLayoutGroup)
    - 검술 버튼들이 동적으로 생성됨

---

### 6. InventoryGridPanel (중앙 - 인벤토리 패널)

**오브젝트명**: `InventoryGridPanel`
**컴포넌트**:
- `Image` (배경)
- `VerticalLayoutGroup`
- `ContentSizeFitter` (Vertical: Preferred Size)

**하위 오브젝트들**:

#### 6.1 인벤토리 제목
**오브젝트명**: `InventoryTitle`
**컴포넌트**:
- `Text` (텍스트: "인벤토리")

#### 6.2 인벤토리 스크롤 뷰
**오브젝트명**: `InventoryScrollView`
**컴포넌트**:
- `ScrollRect`
- `Image` (배경)

**하위 오브젝트**:
- `Viewport` (Image + Mask)
  - `Content` (RectTransform + GridLayoutGroup)

**GridLayoutGroup 설정**:
- Cell Size: 80x80
- Spacing: 5x5
- Start Corner: Top Left
- Start Axis: Horizontal
- Child Alignment: Upper Left

---

### 7. ItemDetailsPanel (오른쪽 - 아이템 상세 패널)

**오브젝트명**: `ItemDetailsPanel`
**컴포넌트**:
- `Image` (배경)
- `VerticalLayoutGroup`
- `ContentSizeFitter` (Vertical: Preferred Size)

**하위 오브젝트들**:

#### 7.1 선택된 아이템 표시
**오브젝트명**: `ItemDetailInfo`
**컴포넌트**:
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**하위 오브젝트**:
- `SelectedItemIcon` (Image, 64x64)
- `SelectedItemName` (Text, "선택된 아이템")

#### 7.2 아이템 능력치 정보
**오브젝트명**: `ItemStatsInfo`
**컴포넌트**:
- `VerticalLayoutGroup`
- `ContentSizeFitter` (Vertical: Preferred Size)

**하위 오브젝트**:

##### 7.2.1 아이템 옵션 그리드
**오브젝트명**: `STATINFO_01` (GridLayoutGroup 컨테이너)
**컴포넌트**:
- `GridLayoutGroup`
- `ContentSizeFitter` (Vertical: Preferred Size)

**GridLayoutGroup 설정**:
- Cell Size: 390x50
- Spacing: 20x10
- Start Corner: Upper Left
- Start Axis: Horizontal
- Child Alignment: Upper Left
- Constraint: Fixed Column Count
- Constraint Count: 2

**하위 오브젝트** (2열 3행 배치):
- `STATINFO_01` (Text, "옵션1: +값")
- `STATINFO_02` (Text, "옵션2: +값")
- `STATINFO_03` (Text, "옵션3: +값")
- `STATINFO_04` (Text, "옵션4: +값")
- `STATINFO_05` (Text, "옵션5: +값")
- `STATINFO_06` (Text, "옵션6: +값")

**💡 참고**: 아이템 데이터에 따라 동적으로 옵션 표시 (최대 6개)

#### 7.3 아이템 설명
**오브젝트명**: `ItemDescription`
**컴포넌트**:
- `ScrollRect`
- `Image` (배경)

**하위 오브젝트**:
- `Viewport` (Image + Mask)
  - `Content` (RectTransform)
    - `ItemDescriptionText` (TextMeshProUGUI, "아이템 설명...")

#### 7.4 액션 버튼들
**오브젝트명**: `ItemActionButtons`
**컴포넌트**:
- `HorizontalLayoutGroup`
- `ContentSizeFitter` (Horizontal: Preferred Size)

**하위 오브젝트**:
- `EquipButton` (Button + Text, "장착/해제")
- `UseButton` (Button + Text, "사용")
- `DropButton` (Button + Text, "버리기")

---

## 🎨 프리팹 구조

### ItemSlotUI 프리팹
**용도**: 인벤토리 그리드의 개별 아이템 슬롯
**구조**:
```
ItemSlotUI (Button)
├── ItemIcon (Image)
├── ItemName (Text)
└── ItemQuantity (Text)
```

### EquipmentSlotUI 프리팹 (무기/갑옷/유파용)
**용도**: 무기, 갑옷, 유파 슬롯 (아이콘 + 이름)
**구조**:
```
EquipmentSlotUI (Button)
├── EquipmentIcon (Image, 64x64)
├── EquipmentName (Text)
└── EquipmentFrame (Image, 테두리)
```

### AccessorySlotUI 프리팹 (장신구용)
**용도**: 장신구 슬롯 (아이콘만, 텍스트 없음)
**구조**:
```
AccessorySlotUI (Button)
├── AccessoryIcon (Image, 48x48)
└── AccessoryFrame (Image, 테두리)
```

---

## ⚙️ 레이아웃 설정 가이드

### HorizontalLayoutGroup 공통 설정
- Child Controls Size: true
- Child Force Expand: true (Width)
- Spacing: 10-20

### VerticalLayoutGroup 공통 설정
- Child Controls Size: true
- Child Force Expand: true (Height)
- Spacing: 10-20

### ContentSizeFitter 설정
- Horizontal: Preferred Size (동적 크기 조정)
- Vertical: Preferred Size (동적 크기 조정)

### ScrollRect 공통 설정
- Horizontal: false (세로 스크롤만)
- Vertical: true
- Movement Type: Elastic
- Scroll Sensitivity: 20

---

## 🎯 생성 순서

1. **Canvas 생성** 및 기본 설정
2. **MainInventoryPanel** 생성
3. **TopNavigationBar** 및 탭들 생성
4. **MainContentArea** 생성
5. **EquipmentPanel** 및 하위 요소들 생성
6. **InventoryGridPanel** 및 스크롤 뷰 생성
7. **ItemDetailsPanel** 및 하위 요소들 생성
8. **프리팹 생성** (ItemSlotUI, EquipmentSlotUI, AccessorySlotUI)
9. **레이아웃 조정** 및 최종 테스트

---

## 📝 참고사항

- 모든 Text 컴포넌트는 TextMeshPro 사용 권장
- 색상은 프로젝트 테마에 맞게 조정
- 해상도 대응을 위해 Anchor와 Pivot 설정 주의
- 스크립트 연동을 위해 오브젝트명 정확히 유지
