# 개발 일지 - 2025년 10월 31일

## 📋 오늘의 목표
- UI 시스템 버그 수정 및 안정화
- 드래그 앤 드롭 시스템 완성도 향상
- 선택 상태 관리 일관성 확보

---

## ✅ 진행 상황

### 현재 상태
- **완료**: UI 선택 상태 관리 시스템 정리
- **완료**: 드래그 앤 드롭 시스템 버그 수정
- **완료**: 프리팹 색상 관리 일관성 확보
- **완료**: MainMenuManager 초기화 로직 개선

### 구현 완료 내역

#### 1. SelectableSlotUI와 EquipmentSlotUI 충돌 해결 ✅
**문제**: `SelectableSlotUI`가 `frameImage.enabled`를 제어하여 `EquipmentSlotUI`의 장착 테두리와 충돌

**해결**:
- `SelectableSlotUI`: `highlightImage` (배경) 전담 관리
- `EquipmentSlotUI`: `frameImage_equipped` (장착 테두리) 전담 관리
- 역할 분리로 충돌 제거

**파일**: 
- `Assets/Script/UI/SelectableSlotUI.cs`
- `Assets/Script/UI/EquipmentSlotUI.cs`

#### 2. EquipmentSlotUI 컬러 관리 정리 ✅
**문제**: 코드에서 UI 컬러를 강제 변경하여 프리팹 설정 무시

**해결**:
- `iconImage.color = Color.white` 제거 (3곳)
- `frameImage_equipped.color = EQUIPPED_FRAME_COLOR` 제거
- `NORMAL_FRAME_COLOR`, `SELECTED_FRAME_COLOR` 상수 제거
- 비활성화 표현만 코드 관리 (`DISABLED_COLOR`)

**파일**: `Assets/Script/UI/EquipmentSlotUI.cs`

#### 3. MainMenuManager 초기화 버그 수정 ✅
**문제**: 게임 시작 후 첫 메뉴 열기 시 모든 패널이 동시 표시

**원인**: 
- Unity 에디터에서 패널들이 활성화 상태로 저장됨
- `currentTabIndex = 0`으로 첫 `ShowTab(0)` 호출 시 스킵

**해결**:
- `Awake()`에서 모든 패널 명시적 비활성화
- `currentTabIndex = -1`로 초기화
- `ShowTab()` 스킵 조건에 `gameObject.activeSelf` 확인 추가

**파일**: `Assets/Script/UI/MainMenuManager.cs`

#### 4. 드래그 앤 드롭 장착 해제 로직 개선 ✅
**문제**: 장착 슬롯에서 그리드 아이템/검술 위로 드롭 시 교체 발생 (장착 해제 원함)

**해결**:
- `ISlotDropTarget.CanAcceptDrop(dragData, source)` 시그니처 확장
- 그리드 슬롯: 장착 슬롯에서 온 드래그 거부
- `GridAreaDropZone`: 장착 슬롯에서만 드롭 받음

**파일**:
- `Assets/Script/UI/ISlotDropTarget.cs`
- `Assets/Script/UI/ActionCommandSlotUI.cs`
- `Assets/Script/UI/ItemSlotUI.cs`
- `Assets/Script/UI/GridAreaDropZone.cs`
- `Assets/Script/UI/DraggableSlotUI.cs`

#### 5. ActionCommandDetailPanel 버튼 클릭 불가 해결 ✅
**문제**: 상세 패널의 장착/해제 버튼 클릭 불가

**원인**: `CanvasGroup.blocksRaycasts = false` 설정으로 모든 클릭 차단

**해결**: 불필요한 `CanvasGroup` 설정 제거

**파일**: `Assets/Script/UI/ActionCommandDetailPanel.cs`

#### 6. 드래그 후 포커스 유지 구현 ✅
**문제**: 드래그로 장착/해제 시 선택 상태가 해제됨

**해결**:
- `OnEquipAction`: 장착 후 해당 슬롯 자동 선택
- `SwapEquippedSlots`: 교체 후 드롭 대상 슬롯 자동 선택
- `OnUnequipAction`: 해제 후 그리드의 해당 검술 자동 선택
- `ActionCommandSlotUI.OnDropReceived`: 드래그 소스 선택 해제

**파일**: 
- `Assets/Script/UI/ActionCommandEquipUI.cs`
- `Assets/Script/UI/ActionCommandSlotUI.cs`

#### 7. 유파 슬롯 FrameImage 색상 문제 해결 ✅
**문제**: 유파 슬롯만 `frameImage_equipped`가 흰색으로 표시

**원인**: `InventoryUI.HighlightEquipmentArea()`에서 드래그 하이라이트 종료 시 `frameImage.color = Color.white` 강제 설정

**해결**: 하이라이트 종료 시 색상 변경 제거, `enabled` 제어만 유지

**파일**: `Assets/Script/UI/InventoryUI.cs` (1349, 1367번 줄)

#### 8. DraggableSlotUI 드롭 타겟 탐색 개선 ✅
**문제**: 검술 그리드 위에 드롭 시 장착 해제가 안됨

**원인**: `FindDropTarget()`이 `CanAcceptDrop` 확인 없이 우선순위만으로 타겟 선택

**해결**: 
- 드롭 타겟 탐색 시 `CanAcceptDrop(dragData, dragSource)` 미리 확인
- 실제 받을 수 있는 슬롯만 반환
- 그리드 슬롯이 거부하면 `GridAreaDropZone`으로 fallback

**파일**: `Assets/Script/UI/DraggableSlotUI.cs`

---

## 🐛 해결된 버그

1. ✅ 선택 테두리와 장착 테두리 충돌
2. ✅ UI 컬러가 프리팹 설정 무시
3. ✅ 게임 시작 시 모든 메뉴 패널 동시 표시
4. ✅ 장착 슬롯 드래그 시 의도치 않은 교체 발생
5. ✅ 상세 패널 버튼 클릭 불가
6. ✅ 드래그 후 선택 상태 해제
7. ✅ 유파 슬롯 테두리 색상 오류
8. ✅ 검술 그리드 위 드롭 시 장착 해제 실패

---

## 📊 코드 품질 개선

### 일관성 확보
- ✅ UI 컬러 관리: 프리팹 우선, 코드는 비활성화만
- ✅ 선택 상태 관리: 드래그/클릭 동일 경로
- ✅ 드롭 타겟 탐색: `CanAcceptDrop` 기반 검증

### 역할 분리
- ✅ `SelectableSlotUI`: 배경 하이라이트 전담
- ✅ `EquipmentSlotUI`: 장착 테두리 전담
- ✅ `GridAreaDropZone`: 장착 해제 전담

### 코드 정리
- ✅ 불필요한 컬러 상수 제거
- ✅ 중복 로직 통합
- ✅ 명확한 책임 분리

---

## 📌 다음 작업 계획

### 완료된 핵심 기능
- ✅ 메인 메뉴 시스템 통합
- ✅ 드래그 앤 드롭 (인벤토리 + 검술)
- ✅ 선택 상태 관리
- ✅ 포커스 시스템
- ✅ 동적 장신구 슬롯
- ✅ 검술 중복 처리 (습득/유파)

### 추가 개선 가능 영역 (우선순위 낮음)
- 정렬/필터 기능
- 애니메이션 효과
- 성능 최적화
- 접근성 개선

---

## 💡 배운 점

1. **UI 컴포넌트 역할 분리의 중요성**
   - `SelectableSlotUI`와 `EquipmentSlotUI`가 같은 `frameImage`를 제어하여 충돌
   - 명확한 책임 분리로 해결

2. **프리팹 설정 존중**
   - 코드에서 컬러를 강제하면 디자이너 작업 무효화
   - 상태 표현은 `enabled` 제어로 충분

3. **드래그 앤 드롭의 복잡성**
   - 우선순위 + 가능 여부 검증 필요
   - 소스 정보 전달로 컨텍스트 기반 판단

4. **Unity 초기화 순서**
   - 에디터 설정이 런타임까지 영향
   - `Awake()`에서 명시적 초기화 필수

---

## 📊 진행률

```
[====================] 100% - UI 버그 수정
[====================] 100% - 드래그 앤 드롭 완성도
[====================] 100% - 선택 상태 관리
[====================] 100% - 코드 품질 개선
```

**UI 시스템**: ✅ 안정화 완료  
**드래그 앤 드롭**: ✅ 완성  
**선택/포커스**: ✅ 일관성 확보  
**코드 품질**: ✅ 정리 완료

---

## 💡 참고 문서
- `Docs/Design/Item/Advanced/메인_메뉴_시스템_통합_구현_계획서.md`
- `Docs/Design/Item/Advanced/MainMenuManager_설정_메뉴얼.md`

---

**작성 시각**: 2025-10-31  
**다음 단계**: 통합 테스트 및 추가 컨텐츠 구현


