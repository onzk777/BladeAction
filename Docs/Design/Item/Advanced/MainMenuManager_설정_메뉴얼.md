# MainMenuManager 설정 메뉴얼

## 개요

MainMenuManager는 인벤토리 UI와 액션 커맨드 장착 UI를 통합 관리하는 메인 메뉴 시스템입니다.

---

## 1. MainMenuManager 설정

### 필수 컴포넌트
- `MainMenuManager.cs`

### Inspector 설정

#### **탭 설정 (Tabs)**
| 필드 | 설명 | 설정 방법 |
|------|------|----------|
| **Menu Tabs (List)** | 메뉴 탭 목록 | Size = 2 (인벤토리, 액션 커맨드) |
| - Tab Button | 상단 네비게이션 버튼 | 각 탭 버튼 GameObject 할당 |
| - Panel | 탭에 해당하는 UI 패널 | InventoryUI, ActionCommandEquipUI 할당 |

#### **입력 설정 (Input)**
| 필드 | 설명 | 기본값 |
|------|------|--------|
| **Player Input** | PlayerInput 컴포넌트 | 자동 할당 (GetComponent) |
| **UI Action Map Name** | UI용 Input Action Map 이름 | "UI" |

### Hierarchy 구조
```
MainMenuCanvas
├─ TopNavigationBar
│  ├─ InventoryTabButton (Button)
│  └─ ActionCommandTabButton (Button)
├─ InventoryUI (GameObject + InventoryUI 컴포넌트)
└─ ActionCommandEquipUI (GameObject + ActionCommandEquipUI 컴포넌트)
```

### 입력 키 바인딩
| 키 | 기능 | 설명 |
|----|------|------|
| **B** | 인벤토리 토글 | 닫혀있으면 인벤토리 탭 열기, 인벤토리 탭 활성화 시 닫기, 다른 탭 활성화 시 인벤토리 탭으로 전환 |
| **X** | 액션 커맨드 토글 | 닫혀있으면 액션 커맨드 탭 열기, 액션 커맨드 탭 활성화 시 닫기, 다른 탭 활성화 시 액션 커맨드 탭으로 전환 |
| **ESC** | 메뉴 닫기 | 메뉴가 열려있으면 닫기 |

---

## 2. InventoryUI 설정

### 필수 컴포넌트
- `InventoryUI.cs`

### Inspector 설정

#### **아이템 그리드 (Item Grid)**
| 필드 | 설명 | 설정 방법 |
|------|------|----------|
| **Item Grid Container** | 아이템 슬롯들이 생성될 부모 | ScrollView → Viewport → Content |
| **Item Slot Prefab** | 아이템 슬롯 프리팹 | `ItemSlot.prefab` 할당 |
| **Item Scroll Rect** | 스크롤 영역 | ScrollView의 ScrollRect 컴포넌트 |
| **Grid Area Frame Image** | 드롭 하이라이트용 테두리 | 그리드 전체 영역 크기의 Image (초기 비활성화) |
| **Grid Area Drop Zone Object** | 드롭존 GameObject | GridAreaDropZone 컴포넌트 포함, 초기 비활성화 |

#### **장비 슬롯 (Equipment Slots)**
| 필드 | 설명 | 설정 방법 |
|------|------|----------|
| **Equipment Slot Container** | 장비 슬롯들이 생성될 부모 | EquipmentSlots GameObject |
| **Equipment Slot Prefab** | 장비 슬롯 프리팹 | `EquipmentSlot.prefab` 할당 |
| **Accessory Panel** | 장신구 슬롯 영역 | AccessoryPanel GameObject (가로 배치) |

#### **상세 정보 패널**
| 필드 | 설명 | 설정 방법 |
|------|------|----------|
| **Item Detail Panel** | 아이템 상세 정보 패널 | ItemDetailPanel 컴포넌트 할당 |
| **Equipped Sword Art Style UI** | 착용 중인 검술 유파 슬롯 | EquippedSwordArtStyleUI 컴포넌트 할당 |

### 드래그 앤 드롭 설정

#### **Grid Area Frame Image (테두리)**
1. 새 GameObject 생성: `ItemGridFrameImage`
2. 위치: ItemGrid와 동일한 크기로 배치
3. Image 컴포넌트 추가:
   - Color: 흰색 (1, 1, 1, 1)
   - Raycast Target: 체크 해제
4. RectTransform: ItemGrid와 완전히 겹치도록 설정
5. InventoryUI의 `gridAreaFrameImage`에 할당
6. 초기 상태: `enabled = false` (스크립트에서 자동 처리)

#### **Grid Area Drop Zone (드롭 영역)**
1. 새 GameObject 생성: `ItemGridDropZone`
2. 위치: ItemGrid와 동일한 크기로 배치
3. `GridAreaDropZone.cs` 컴포넌트 추가
4. Image 컴포넌트 자동 추가됨:
   - Color: 투명 (1, 1, 1, 0)
   - Raycast Target: 자동 관리 (평소 false, 드래그 시 true)
5. InventoryUI의 `gridAreaDropZoneObject`에 할당
6. 초기 상태: `SetActive(false)` (스크립트에서 자동 처리)

### 슬롯 Prefab 설정

#### **ItemSlot.prefab**
**필수 컴포넌트:**
- `ItemSlotUI.cs`
- `SelectableSlotUI.cs`
- `DraggableSlotUI.cs` ⭐ 드래그 앤 드롭

**DraggableSlotUI 설정:**
- Drag Alpha: 0.6 (드래그 복사본 투명도)
- Source Alpha: 0.3 (드래그 중 원본 투명도)
- Drag Copy Width: 280 (드래그 복사본 가로 크기)

#### **EquipmentSlot.prefab**
**필수 컴포넌트:**
- `EquipmentSlotUI.cs`
- `SelectableSlotUI.cs`
- `DraggableSlotUI.cs` ⭐ 드래그 앤 드롭

**Frame Image 설정:**
- 장착 슬롯 하위에 `FrameImage` GameObject 추가
- Image 컴포넌트: 테두리 스프라이트, Color: 흰색
- RectTransform: 슬롯 전체 크기
- 초기 상태: `enabled = false`

---

## 3. ActionCommandEquipUI 설정

### 필수 컴포넌트
- `ActionCommandEquipUI.cs`

### Inspector 설정

#### **액션 그리드 (Action Grid)**
| 필드 | 설명 | 설정 방법 |
|------|------|----------|
| **Action Grid Container** | 검술 슬롯들이 생성될 부모 | ScrollView → Viewport → Content |
| **Action Slot Prefab** | 검술 슬롯 프리팹 | `ActionCommandSlot.prefab` 할당 |
| **Action Scroll Rect** | 스크롤 영역 | ScrollView의 ScrollRect 컴포넌트 |
| **Grid Area Frame Image** | 드롭 하이라이트용 테두리 | 그리드 전체 영역 크기의 Image (초기 비활성화) |
| **Grid Area Drop Zone Object** | 드롭존 GameObject | GridAreaDropZone 컴포넌트 포함, 초기 비활성화 |

#### **장착 슬롯 (Equipped Slots)**
| 필드 | 설명 | 설정 방법 |
|------|------|----------|
| **Equipped Action Slots Container** | 장착 슬롯들이 생성될 부모 | EquippedActionSlots GameObject |
| **Equipped Area Frame Image** | 드롭 하이라이트용 테두리 | 장착 영역 전체 크기의 Image (초기 비활성화) |

#### **상세 정보 패널**
| 필드 | 설명 | 설정 방법 |
|------|------|----------|
| **Action Command Detail Panel** | 검술 상세 정보 패널 | ActionCommandDetailPanel 컴포넌트 할당 |
| **Equipped Sword Art Style UI** | 장착 중인 검술 유파 표시 | EquippedSwordArtStyleUI 컴포넌트 할당 |

### 드래그 앤 드롭 설정

#### **Grid Area Frame Image (그리드 테두리)**
설정 방법은 InventoryUI와 동일 (ActionGrid 기준)

#### **Equipped Area Frame Image (장착 영역 테두리)**
1. 새 GameObject 생성: `EquippedAreaFrameImage`
2. 위치: EquippedActionSlots 전체 크기로 배치
3. Image 컴포넌트 추가:
   - Color: 흰색 (1, 1, 1, 1)
   - Raycast Target: 체크 해제
4. ActionCommandEquipUI의 `equippedAreaFrameImage`에 할당
5. 초기 상태: `enabled = false`

### 슬롯 Prefab 설정

#### **ActionCommandSlot.prefab**
**필수 컴포넌트:**
- `ActionCommandSlotUI.cs`
- `SelectableSlotUI.cs`
- `DraggableSlotUI.cs` ⭐ 드래그 앤 드롭

**DraggableSlotUI 설정:**
- Drag Alpha: 0.6
- Source Alpha: 0.3
- Drag Copy Width: 280

---

## 4. 공통 컴포넌트 설정

### SelectableSlotUI
**목적:** 슬롯 선택 상태 관리 (클릭 하이라이트)

**Inspector 설정:**
| 필드 | 설명 |
|------|------|
| **Highlight Image** | 선택 시 표시할 하이라이트 이미지 |
| **Frame Image** | 선택 시 표시할 테두리 이미지 (선택사항) |
| **Enable Click Toggle** | 재클릭 시 선택 해제 여부 (기본: true) |

### DraggableSlotUI
**목적:** 드래그 앤 드롭 기능 제공

**Inspector 설정:**
| 필드 | 설명 | 기본값 |
|------|------|--------|
| **Drag Alpha** | 드래그 복사본 투명도 | 0.6 |
| **Source Alpha** | 드래그 중 원본 투명도 | 0.3 |
| **Drag Copy Width** | 드래그 복사본 가로 크기 | 280 |

**자동 처리:**
- Wrapper 생성 (Canvas + LayoutElement + RectMask2D)
- 복사본 데이터 동기화 (ItemSlotUI, EquipmentSlotUI, ActionCommandSlotUI)
- 부모 UI 하이라이트 알림 (InventoryUI, ActionCommandEquipUI)

### GridAreaDropZone
**목적:** 그리드 전체 영역을 드롭 대상으로 만듦 (장착 해제)

**자동 처리:**
- Image 컴포넌트 자동 추가 (투명 배경)
- Raycast Target 동적 관리 (드래그 시에만 true)
- 부모 UI 자동 감지 (InventoryUI / ActionCommandEquipUI)

---

## 5. 체크리스트

### MainMenuManager
- [ ] Menu Tabs 리스트 설정 (Size = 2)
- [ ] 각 탭의 Button과 Panel 할당
- [ ] PlayerInput 컴포넌트 확인
- [ ] UI Action Map 활성화 확인

### InventoryUI
- [ ] Item Grid Container 할당
- [ ] Item Slot Prefab 할당
- [ ] Item Scroll Rect 할당
- [ ] Grid Area Frame Image 설정 및 할당
- [ ] Grid Area Drop Zone Object 설정 및 할당
- [ ] Equipment Slot Container 할당
- [ ] Equipment Slot Prefab 할당
- [ ] Accessory Panel 할당
- [ ] Item Detail Panel 할당
- [ ] Equipped Sword Art Style UI 할당

### ActionCommandEquipUI
- [ ] Action Grid Container 할당
- [ ] Action Slot Prefab 할당
- [ ] Action Scroll Rect 할당
- [ ] Grid Area Frame Image 설정 및 할당
- [ ] Grid Area Drop Zone Object 설정 및 할당
- [ ] Equipped Action Slots Container 할당
- [ ] Equipped Area Frame Image 설정 및 할당
- [ ] Action Command Detail Panel 할당
- [ ] Equipped Sword Art Style UI 할당

### Prefabs
- [ ] ItemSlot: DraggableSlotUI 추가
- [ ] EquipmentSlot: DraggableSlotUI 추가, Frame Image 설정
- [ ] ActionCommandSlot: DraggableSlotUI 추가

---

## 6. 트러블슈팅

### 드래그가 작동하지 않음
1. DraggableSlotUI 컴포넌트가 Prefab에 추가되었는지 확인
2. CanvasGroup 컴포넌트가 자동 추가되었는지 확인 (RequireComponent)
3. 슬롯이 비어있지 않은지 확인 (`CanStartDrag()` 조건)

### 드롭이 작동하지 않음
1. GridAreaDropZone의 Image 컴포넌트가 있는지 확인
2. GridAreaDropZone이 올바른 부모 UI 하위에 있는지 확인
3. 드래그 시 GridAreaDropZone이 활성화되는지 확인 (로그 확인)

### 하이라이트가 표시되지 않음
1. Frame Image의 Color가 흰색인지 확인
2. Frame Image의 Raycast Target이 체크 해제되었는지 확인
3. Frame Image가 Inspector에서 `enabled = false` 상태인지 확인
4. DOTween이 프로젝트에 임포트되어 있는지 확인

### 복제 이미지 크기가 이상함
1. Drag Copy Width 값 조정 (기본 280)
2. Prefab의 Layout 컴포넌트 제거 (DraggableSlotUI가 자동 처리)
3. 원본 슬롯의 Anchor 설정 확인

---

## 7. 주의사항

1. **Frame Image는 항상 비활성화 상태로 시작**: 스크립트에서 드래그 시에만 활성화
2. **GridAreaDropZone은 항상 비활성화 상태로 시작**: 스크립트에서 드래그 시에만 활성화
3. **Prefab 수정 후 Scene에 이미 생성된 슬롯은 자동 업데이트 안 됨**: UI 갱신 필요
4. **DOTween 필수**: 하이라이트 애니메이션에 사용
5. **부모 UI의 Vertical Layout Group 주의**: Wrapper에 LayoutElement(ignoreLayout=true) 사용

---

**작성일:** 2025-10-31  
**버전:** 1.0

