# 개발 일지 - 2025년 11월 4일

**작업 주제**: Scene 계층 구조 구현 및 전투 중 UI 입력 차단 문제 해결  
**작업 시간**: 전일  
**상태**: Scene 구조 리팩토링 완료 / 입력 시스템 버그 해결 완료

---

## 📋 오늘의 목표
- Scene 계층 구조 실제 구현 (CoreSystemScene, PersistentUIScene, CombatScene)
- UI 시스템 분리 및 개선
- 전투 플로우 통합 테스트

---

## ✅ 완료된 작업

### 1. Scene 구조 리팩토링 완료 ⭐

#### 1-1. 3-Layer Scene 구조 구축
```
01.CoreSystemScene (영속 레이어)
├── EventSystem
├── GameInputManager (PlayerInput)
├── StatCalculationManager
└── CoreSystemInitializer (Scene 로딩 관리)

02.PersistentUIScene (공통 UI 레이어)
├── MainMenuManager (Canvas)
│   ├── TopNavigationBar
│   ├── InventoryUI
│   └── ActionEquipUI
├── Canvas_Debug
└── ItemEvents

03.CombatScene (컨텐츠 레이어)
├── Canvas_CombatHUD
├── CombatManager
├── Player / Enemy
└── 기타 전투 시스템
```

#### 1-2. 생성/수정된 파일
**새로 생성:**
- `CoreSystemInitializer.cs` - Additive Scene 로딩 관리

**수정:**
- `GameInputManager.cs` - DontDestroyOnLoad 제거, CoreSystemScene에 고정
- `CombatManager.cs` - DontDestroyOnLoad 제거
- `FloatingTextManager.cs` - DontDestroyOnLoad 제거
- `ItemEvents.cs` - DontDestroyOnLoad 제거
- `ActionCommandSelectionManager.cs` - DontDestroyOnLoad 제거

**이동:**
- `StatusUILayout.cs` → `Editor/` 폴더로 이동 (개발 전용 유틸리티)

---

### 2. UI 시스템 분리 및 개선

#### 2-1. 디버그 UI와 게임 HUD 분리
**CombatDebugDisplay.cs (디버그 전용):**
- `turnLabel` - 턴 타이머 정보
- `inputPromptText` - 입력 프롬프트
- `UpdateTurnInfo()`, `ShowInputPrompt()` 메서드

**CombatHUD.cs (게임 HUD):**
- `turnProgressBar` - 턴 진행 바
- `perfectTimingGuideContainer` - 퍼펙트 타이밍 가이드
- 디버그 요소 제거, 게임 플레이 UI만 관리

#### 2-2. MainMenuManager 개선
**추가된 기능:**
- TopNavigationBar 자동 표시/숨김
- `CancelMenu()` 메서드 (ESC/Cancel 키 지원)
- 패널 토글 로직 강화 및 디버그 로그

**수정사항:**
- GameObject 자체는 항상 활성화 유지
- 하위 패널들만 활성화/비활성화로 제어
- 모든 패널 닫힐 때 TopNavigationBar도 자동 숨김

---

### 3. 입력 시스템 수정

#### 3-1. GameInputManager 개선
**주요 변경:**
- DontDestroyOnLoad 제거 → CoreSystemScene에 고정
- `OnCancelUI()` 메서드 추가 (메뉴 취소 기능)
- 크로스 Scene 참조를 위한 중재자 역할 강화

#### 3-2. BaseInputHandler 버그 수정 ⭐ **핵심 수정**
**문제:**
```csharp
// BaseInputHandler.InitializePlayerInput()
playerInput.SwitchCurrentActionMap("Combat"); // ❌ 문제 코드
```
- `SwitchCurrentActionMap()`은 지정된 ActionMap만 활성화하고 **나머지는 모두 비활성화**
- UI ActionMap이 비활성화되어 전투 중 **모든 UI 입력 차단** (B/X 키, 마우스 클릭 등)

**해결:**
```csharp
// ActionMap 전환 대신 직접 액션 참조
perfectAction = playerInput.actions["Combat/PerfectInput"];
```
- CombatManager에서 `EnableCombatMap()`으로 이미 활성화하므로 중복 불필요
- UI ActionMap과 Combat ActionMap 동시 활성화 유지

---

### 4. 해결된 버그

✅ **디버그 패널 버튼 클릭 안되는 문제**
- 원인: CoreSystemScene 단독 실행 시 EventSystem 없음
- 해결: Scene 로딩 구조 정립으로 자동 해결

✅ **인벤토리/검술설정 키 입력 안되는 문제**
- 원인: MainMenuManager GameObject가 비활성화되어 `FindFirstObjectByType` 실패
- 해결: MainMenuManager GameObject 항상 활성화, 하위 패널만 토글

✅ **전투 중 모든 UI 입력 차단 문제** ⭐ **가장 중요**
- 원인: `BaseInputHandler.SwitchCurrentActionMap("Combat")`이 UI ActionMap 비활성화
- 해결: ActionMap 전환 제거, 직접 액션 참조로 변경

✅ **검술 선택 UI 입력 문제**
- 전투 중 UI 입력 문제와 함께 해결됨

---

## 🔍 문제 해결 프로세스 포스트모템

### 핵심 교훈: 문제 해결의 두 가지 철칙

#### 철칙 1: 범위를 먼저 좁혀라
**❌ 잘못된 접근:**
```
EventSystem 있나? → Canvas 설정은? → Raycast Target은? → 
Canvas Group은? → Sort Order는? → ...
```
→ 개별 설정을 하나하나 체크하면 원인을 찾을 수 없음

**✅ 올바른 접근:**
```
증상: "전투 중에만" 클릭 + 키보드 모두 차단
→ 전투 관련 코드가 입력을 차단하고 있음

Binary Search:
CombatScene 제거 → 동작함 ✓
Canvas_CombatHUD 제거 → 여전히 안됨
Player/Enemy 제거 → 동작함! ✓

→ Player/Enemy 스크립트 중 하나가 원인
→ BaseInputHandler 발견!
```

#### 철칙 2: 최근 변경 사항을 우선 확인하라
**핵심:**
- 이전에 동작하던 것이 안 된다 = 최근 작업과 관련 있음
- 직접적 원인이 아니더라도 범위를 좁히는 데 도움됨

**적용:**
- Scene 구조 리팩토링 후 문제 발생
- InputSystem 관리 방식이 바뀌었을 가능성
- BaseInputHandler의 ActionMap 전환 코드 확인 → 원인 발견!

### 실제 적용 과정
```
1. 증상 파악: "전투 중에만 모든 UI 입력 차단"
   → UI 설정 문제 ❌ 전투 코드 문제 ✓

2. 범위 좁히기 (Binary Search):
   CombatScene 비활성화 → Player/Enemy 비활성화 → 원인 범위 좁힘

3. 최근 변경 확인:
   Scene 구조 변경 → InputSystem 이동 → BaseInputHandler 확인

4. 원인 발견:
   SwitchCurrentActionMap()이 UI ActionMap 비활성화
```

**소요 시간:**
- 잘못된 접근: 약 2시간 소요
- 올바른 접근으로 전환 후: 10분 만에 해결

---

## 📊 현재 상태

### ✅ 완료
- Scene 구조 리팩토링 100% 완료
- 입력 시스템 버그 해결
- UI 시스템 분리 및 개선
- 전투 플로우 정상 동작 확인

### 🔄 남은 작업
- Scene 전환 시스템 구현 (SceneTransitionManager)
- 타이틀 화면 구현
- 전투 결과 화면 개선
- 카메라 설정 확인 (배경 없어서 UI 닫힘이 보이지 않음)

---

## 🎯 다음 작업 (2025-11-05)

### 우선순위 1: Scene 전환 시스템 구현
1. **SceneTransitionManager 생성**
   - Scene 로드/언로드 관리
   - Fade In/Out 트랜지션 효과

2. **타이틀 화면 구현**
   - TitleScene 생성
   - 게임 시작 → MainMenuScene 전환

3. **전투 종료 플로우 개선**
   - 전투 결과 화면 (ResultScene 또는 CombatScene 내 UI)
   - "다시 시작" / "메인 메뉴로" 버튼

### 우선순위 2: 남은 이슈 수정
1. **카메라 설정 확인**
   - 각 Scene별 카메라 구성
   - 배경 표시 문제 해결

2. **UI 미세 조정**
   - TopNavigationBar 타이밍 최종 검증
   - ESC/Cancel 키 동작 테스트

### 우선순위 3: 통합 테스트
1. **전체 게임 플로우 테스트**
   - 타이틀 → 메인 메뉴 → 전투 → 결과 → 반복
   - Scene 전환 시 메모리 누수 확인

---

## 📝 개발 노트

### 배운 점

1. **문제 해결은 범위 좁히기가 핵심**
   - 개별 설정 체크보다 논리적 추론과 Binary Search
   - "언제 발생하나?", "무엇이 안 되나?"에 집중

2. **최근 변경 사항이 가장 유력한 단서**
   - 동작하던 것이 안 되면 최근 작업 확인
   - 간접적이라도 반드시 범위를 좁히는 데 도움됨

3. **ActionMap 관리의 중요성**
   - `SwitchCurrentActionMap()`은 배타적 전환 (다른 것 모두 비활성화)
   - `Enable()`은 누적 활성화 (여러 ActionMap 동시 활성화 가능)
   - UI와 Combat ActionMap은 동시 활성화 필요

4. **Scene 분리 시 생명주기 관리**
   - DontDestroyOnLoad는 신중하게 사용
   - 각 Scene의 책임과 생명주기를 명확히 정의

### 어려웠던 점

1. **문제 원인 특정까지 시간 소요**
   - 초기에 잘못된 방향으로 접근 (UI 설정 체크)
   - 사용자 피드백("시야를 넓혀")으로 방법론 전환

2. **크로스 Scene 참조 문제**
   - GameObject가 다른 Scene에 있어 Inspector 연결 불가
   - GameInputManager를 중재자로 활용하여 해결

### 다음에 개선할 점

1. **문제 발생 시 즉시 철칙 적용**
   - 개별 설정 체크에 빠지지 말고 범위 좁히기부터
   - 최근 변경 사항 우선 확인

2. **ActionMap 설계 시 상호작용 고려**
   - 어떤 ActionMap이 동시에 활성화되어야 하는지 사전 정의
   - Enable/Disable vs Switch 차이 명확히 이해

---

## 🔗 관련 문서
- `Docs/Design/Scene/Scene_계층_구조_설계.md`
- `Docs/Design/Scene/Scene_계층_구조_구현_계획.md`

---

## 📊 프로젝트 진행률

```
[========================================>        ] 80%

완료:
- 전투 시스템 프로토타입 ✅
- CharacterManager 리팩토링 ✅
- Scene 계층 구조 설계 ✅
- Scene 계층 구조 구현 ✅
- 입력 시스템 버그 해결 ✅

진행 중:
- Scene 전환 시스템 구현 🔄

예정:
- 타이틀/메인 메뉴 구현
- 전투 결과 처리 개선
- 인벤토리/장비 Scene 분리
```

---

## 💡 오늘의 핵심 성과

**"문제 해결 철칙" 수립 및 실전 적용**

이전에 동작하던 UI가 Scene 리팩토링 후 전투 중에만 완전히 차단되는 심각한 버그를 발견했다. 초기에는 UI 설정 문제로 접근하여 시간을 낭비했지만, 문제 해결 방법론을 전환하여:

1. **범위 좁히기**: CombatScene → Player/Enemy → BaseInputHandler
2. **최근 변경 확인**: Scene 구조 변경 → InputSystem 관리 방식 검토

이 두 가지 철칙을 적용하여 10분 만에 근본 원인(SwitchCurrentActionMap)을 찾고 해결했다. 이 경험을 바탕으로 향후 모든 문제 해결 시 이 철칙을 최우선으로 적용할 것이다.







