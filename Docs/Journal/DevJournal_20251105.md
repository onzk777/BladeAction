# 개발 일지 - 2025년 11월 5일

**작업 주제**: Scene 전환 시스템 구현  
**작업 시간**: 전일  
**상태**: 진행 중 (ResultScene 작업 중)

---

## 📋 오늘의 목표

Scene 전환 시스템 구현 (SceneTransitionManager, FadeController) 및 필요한 Scene들(TitleScene, TestScene, ResultScene) 설정하여 전체 게임 플로우 완성

---

## ✅ 완료된 작업

### 1. 핵심 시스템 구현

#### 1-1. FadeController 구현
- **위치**: `Assets/Script/UI/FadeController.cs`
- **역할**: Scene 전환 시 Fade In/Out 효과 제공
- **주요 기능**:
  - `FadeIn()`, `FadeOut()` 메서드
  - `FadeOutIn()` - Callback 지원
  - CanvasGroup을 이용한 알파 제어
- **소속**: PersistentUIScene (FadeCanvas)

#### 1-2. SceneTransitionManager 구현
- **위치**: `Assets/Script/Manager/SceneTransitionManager.cs`
- **역할**: Scene 로드/언로드 관리 (저수준)
- **주요 기능**:
  - `TransitionToScene()` - Fade 효과와 함께 Scene 전환
  - `LoadSceneAdditive()`, `UnloadScene()` - Additive 로드/언로드
  - 현재 Content Scene 추적
  - 전환 중 상태 관리 (isTransitioning)
- **소속**: CoreSystemScene

#### 1-3. SceneFlowController 구현
- **위치**: `Assets/Script/Manager/SceneFlowController.cs`
- **역할**: Scene 흐름 제어 (고수준)
- **이름 변경 이력**: MainMenuSceneManager → SceneFlowController
- **주요 기능**:
  - `StartCombat()` - 전투 시작 및 자동 CombatScene 전환
  - `ReturnToTitle()` - 타이틀 화면 복귀
  - `GoToTestScene()` - TestScene 전환
  - Scene 참조를 한 곳에서 통합 관리
- **소속**: CoreSystemScene
- **설계 결정**: 
  - 당초 MainMenuScene 전용으로 설계했으나, Scene 전환 로직이 범용적이므로 CoreSystemScene으로 이동
  - LobbyScene은 정식 게임에서 구현 예정, 현재는 TestScene이 Lobby 역할 대신함

---

### 2. Scene 관리자 구현

#### 2-1. TitleSceneManager 구현
- **위치**: `Assets/Script/Scene/TitleSceneManager.cs`
- **역할**: 타이틀 화면 버튼 이벤트 처리
- **주요 기능**:
  - 게임 시작 → TestScene 전환
  - 게임 종료
  - 버전 정보 표시

#### 2-2. TestSceneManager 구현
- **위치**: `Assets/Script/Scene/TestSceneManager.cs`
- **역할**: TestScene 버튼 이벤트 처리
- **주요 기능**:
  - 전투 시작 → SceneFlowController.StartCombat() 호출
  - 타이틀로 복귀 → SceneFlowController.ReturnToTitle() 호출
- **중요**: 다른 Scene(CoreSystemScene)에 있는 SceneFlowController를 코드로 접근 (Inspector 드래그 불가)

#### 2-3. ResultSceneManager 구현
- **위치**: `Assets/Script/Scene/ResultSceneManager.cs`
- **역할**: 전투 결과 화면 관리
- **주요 기능**:
  - Static 변수로 BattleResult 수신
  - 승리/패배 표시
  - 골드/경험치 보상 표시 및 PlayerCharacterManager에 적용
  - 계속 → TestScene 복귀
  - 타이틀로 → TitleScene 전환

---

### 3. 기존 시스템 수정

#### 3-1. CombatManager 수정
- **파일**: `Assets/Script/Combat/CombatManager.cs`
- **추가 기능**:
  - `TransitionToResultScene()` - 전투 종료 후 ResultScene 전환
  - `DelayedSceneTransition()` - 2초 대기 후 Scene 전환 (결과 확인 시간)
  - `ResultSceneManager.LastBattleResult`에 전투 결과 전달

#### 3-2. CoreSystemInitializer 수정
- **파일**: `Assets/Script/CoreSystemInitializer.cs`
- **변경 사항**:
  - `initialContentSceneName` 기본값을 `"05.TitleScene"`으로 변경
  - 게임 시작 시 타이틀 화면부터 시작

---

### 4. Scene Asset 참조 시스템 개선

#### 문제점
- Scene 이름을 string으로 수기 입력 → 오타 위험, Scene 이름 변경 시 반영 안됨

#### 해결 방법
```csharp
#if UNITY_EDITOR
[SerializeField] private SceneAsset combatSceneAsset; // 에디터에서 드래그
#endif
[HideInInspector]
[SerializeField] private string combatSceneName;      // 런타임에서 사용

private void OnValidate()
{
    if (combatSceneAsset != null)
        combatSceneName = combatSceneAsset.name; // 자동 동기화
}
```

#### 장점
- ✅ Scene 이름 변경 시 자동 반영
- ✅ 존재하지 않는 Scene 참조 방지
- ✅ 드래그 앤 드롭으로 편리하게 설정
- ✅ Inspector에서 Scene Name 필드 숨김 (HideInInspector)

#### 적용 파일
- CoreSystemInitializer.cs
- SceneFlowController.cs
- TitleSceneManager.cs (리팩토링 후 제거됨)
- ResultSceneManager.cs (리팩토링 후 제거됨)

---

### 5. 툴팁 추가

모든 SerializeField에 [Tooltip] 추가하여 Inspector에서 필드 역할을 쉽게 파악 가능하도록 개선

**적용 파일**:
- CoreSystemInitializer.cs
- SceneFlowController.cs
- TitleSceneManager.cs
- ResultSceneManager.cs
- TestSceneManager.cs
- FadeController.cs
- SceneTransitionManager.cs

---

### 6. 불필요한 기능 제거

#### 6-1. InventoryScene 제거
- **이유**: 인벤토리는 PersistentUIScene의 MainMenuManager가 이미 담당
- **작업**:
  - SceneFlowController에서 inventorySceneAsset 관련 필드 제거
  - OpenInventory(), CloseInventory() 메서드 제거
  - 설정 가이드에서 InventoryScene 관련 설명 제거

---

### 7. Scene 참조 중복 제거 리팩토링

#### 문제점
각 Scene Manager에서 Scene 참조가 중복됨:
```
TitleSceneManager → testSceneAsset
ResultSceneManager → testSceneAsset, titleSceneAsset
SceneFlowController → combatSceneAsset, titleSceneAsset, testSceneAsset
```

#### 해결 방법
**Scene 참조는 SceneFlowController에서만 관리**

**Before**:
```csharp
// ResultSceneManager
[SerializeField] private SceneAsset testSceneAsset;
SceneTransitionManager.Instance.TransitionToScene(testSceneName);
```

**After**:
```csharp
// ResultSceneManager - Scene 참조 제거
SceneFlowController.Instance.GoToTestScene(); // 메서드만 호출
```

#### 장점
- ✅ Scene 참조는 SceneFlowController에서만 관리
- ✅ Scene 이름 변경 시 한 곳만 수정
- ✅ Inspector 설정 간소화
- ✅ 중복 제거로 유지보수 용이

---

### 8. 문서 작성

#### Unity 설정 가이드 작성
- **파일**: `Docs/Design/Scene/Scene_전환_시스템_Unity_설정_가이드.md`
- **내용**:
  1. PersistentUIScene 설정 (FadeCanvas 추가)
  2. CoreSystemScene 설정 (SceneTransitionManager, SceneFlowController 추가)
  3. TitleScene 생성 및 설정
  4. TestScene 설정 (TestSceneManager 추가)
  5. ResultScene 생성 및 설정
  6. 통합 테스트 가이드

---

## 🔄 현재 상태

### 완료된 Scene 설정
- ✅ CoreSystemScene - SceneTransitionManager, SceneFlowController 추가
- ✅ PersistentUIScene - FadeCanvas 추가
- ✅ TitleScene - 생성 및 TitleSceneManager 설정 완료
- ✅ TestScene - TestSceneManager 추가 완료
- ✅ CombatScene - 전투 종료 후 ResultScene 전환 로직 추가

### 진행 중
- 🔄 **ResultScene - 생성 및 설정 작업 중**

---

## 📊 Scene 구조 (최종)

```
게임 시작 (CoreSystemScene)
    ↓ Fade In
TitleScene (타이틀 화면)
    ↓ "게임 시작" → Fade Out/In
TestScene (테스트용 Lobby 역할)
    ↓ "전투 시작" → Fade Out/In
CombatScene (전투 진행)
    ↓ 전투 종료 → 2초 대기 → Fade Out/In
ResultScene (결과 표시) ← 현재 작업 중
    ↓ "계속" → Fade Out/In
TestScene (복귀)
```

### Scene 역할 정리
- **TitleScene**: 게임 시작 화면
- **TestScene**: 개발/테스트용 Lobby Scene (정식 LobbyScene은 추후 구현 예정)
- **CombatScene**: 전투
- **ResultScene**: 전투 결과 및 보상

---

## 🎯 다음 작업 (2025-11-05 재개 시)

1. **ResultScene 완료**
   - UI 요소 배치 완료
   - ResultSceneManager 설정 완료

2. **통합 테스트**
   - 전체 Scene 전환 플로우 테스트
   - Fade 효과 동작 확인
   - 전투 결과 데이터 정상 표시 확인
   - Scene 전환 시 메모리 누수 확인

3. **Build Settings 설정**
   - 모든 Scene을 Build Settings에 추가
   - Scene 순서 확인 (CoreSystemScene이 Index 0)

---

## 💡 주요 설계 결정 및 교훈

### 1. MainMenuScene → LobbyScene 명명 이슈
**상황**: MainMenuScene이라는 이름이 기존 MainMenuManager(인벤토리 UI 관리)와 충돌

**결정**:
- 현재 만드는 Scene은 테스트용이므로 **TestScene**으로 사용
- 정식 게임에서는 **LobbyScene**으로 구현 예정
- TestScene에서 검증된 메커니즘을 추후 LobbyScene에 적용

### 2. Scene Manager의 소속 위치
**상황**: MainMenuSceneManager가 Scene 전환 로직을 담당하는데 특정 Scene에 종속될 필요가 있는가?

**결정**:
- Scene 전환은 범용 기능이므로 CoreSystemScene으로 이동
- **SceneFlowController**로 리네임
- 역할 중심 네이밍 (Scene Flow를 Control)

### 3. Scene 참조 관리
**문제**: 각 Scene Manager마다 Scene Asset을 참조 → 중복, 관리 어려움

**해결**:
- **SceneFlowController에서만 Scene 참조 관리**
- 다른 Manager들은 SceneFlowController의 메서드만 호출
- 단일 책임 원칙 준수

### 4. 크로스 Scene 참조 불가
**문제**: TestScene의 버튼 → CoreSystemScene의 SceneFlowController를 Inspector에서 드래그 불가

**해결**:
- 코드로 연결 (SceneFlowController.Instance)
- TestSceneManager 스크립트 생성하여 버튼 이벤트 처리
- 싱글톤 패턴 활용

### 5. Scene Asset vs String
**문제**: Scene 이름을 string으로 입력 → 오타 위험, 유지보수 어려움

**해결**:
- SceneAsset을 에디터에서 참조
- OnValidate()로 자동으로 string 동기화
- HideInInspector로 Inspector 간소화

---

## 📝 개발 노트

### 배운 점

1. **Scene 전환 아키텍처**
   - 저수준(SceneTransitionManager): Scene 로드/언로드 기술 처리
   - 고수준(SceneFlowController): 게임 흐름 제어
   - 명확한 책임 분리

2. **크로스 Scene 참조**
   - 다른 Scene에 있는 GameObject는 Inspector 드래그 불가
   - 싱글톤 패턴으로 코드 접근 필요
   - Scene Manager는 같은 Scene 내 UI만 참조

3. **설정 중복 제거의 중요성**
   - Scene 참조는 한 곳에서만 관리
   - 변경 시 한 곳만 수정하면 됨
   - 유지보수성 향상

4. **에디터 편의성**
   - SceneAsset 참조로 드래그 앤 드롭
   - Tooltip으로 필드 역할 명시
   - HideInInspector로 불필요한 필드 숨김

### 어려웠던 점

1. **Scene 역할 명확화**
   - MainMenuScene vs LobbyScene vs TestScene 혼란
   - 테스트용 vs 정식용 구분 필요

2. **참조 관리 구조**
   - 초기에 각 Scene Manager마다 Scene 참조 중복
   - 리팩토링으로 SceneFlowController로 통합

3. **InventoryScene 오판**
   - PersistentUIScene에 인벤토리가 이미 있는데 별도 Scene 설계
   - 기존 구조 파악 부족

### 다음에 개선할 점

1. **기존 코드 플로우 점검**
   - 새로운 기능 추가 전 기존 구조 충분히 파악
   - 중복 기능이 없는지 확인

2. **명명 규칙**
   - Scene 이름은 역할 중심으로
   - Manager 이름은 소속이 아닌 역할 중심으로

3. **참조 설계**
   - Scene 참조는 가능한 한 중앙화
   - 크로스 Scene 참조 방법 사전 고려

---

## 🔗 관련 문서
- `Docs/Design/Scene/Scene_계층_구조_설계.md` (설계 문서)
- `Docs/Design/Scene/Scene_계층_구조_구현_계획.md` (구현 계획)
- `Docs/Design/Scene/Scene_전환_시스템_Unity_설정_가이드.md` (Unity 설정 가이드)

---

**작업 중단 시점**: ResultScene 작업 중 (UI 구성 단계)  
**재개 시 할 일**: ResultScene UI 배치 완료 → 통합 테스트

