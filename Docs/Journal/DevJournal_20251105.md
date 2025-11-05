# 개발 일지 - 2025년 11월 5일

**작업 주제**: Scene 전환 시스템 구현 + Character 관리 구조 재설계  
**작업 시간**: 전일  
**상태**: ✅ **완료 및 검증 완료**

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

## 🔄 최종 상태

### ✅ 완료된 Scene 설정
- ✅ CoreSystemScene - SceneTransitionManager, SceneFlowController 추가
- ✅ PersistentUIScene - FadeCanvas, Main Camera 추가
- ✅ TitleScene - 생성 및 TitleSceneManager 설정 완료
- ✅ TestScene - TestSceneManager 추가 완료
- ✅ CombatScene - 전투 종료 후 ResultScene 전환 로직 추가
- ✅ ResultScene - 생성 및 ResultSceneManager 설정 완료

### ✅ 검증 완료
- ✅ Scene 전환 플로우 정상 동작
- ✅ Fade 효과 정상 작동
- ✅ 전투 결과 화면 표시
- ✅ Scene 로드/언로드 정상

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

## 🐛 해결된 이슈 (테스트 중 발견 및 해결)

### 이슈 #1: TestScene이 언로드되지 않음
- **원인**: CoreSystemInitializer가 SceneTransitionManager에 초기 Scene 등록 안 함
- **해결**: `SetCurrentContentScene()` 호출 추가

### 이슈 #2: CombatHUD 표시 안 됨
- **원인**: Canvas가 "Screen Space - Camera" 모드, Camera 참조 끊김
- **해결**: Canvas를 "Screen Space - Overlay"로 변경 필요 (Unity 작업)

### 이슈 #3: ResultScene 전환 안 됨
- **원인**: CombatManager에서 Scene 이름 하드코딩
- **해결**: SceneFlowController.GoToResultScene() 사용, 하드코딩 제거

### 이슈 #6: FadeController 찾을 수 없음
- **원인**: FadeImage 비활성화 방식 미구현
- **해결**: FadeImage GameObject 활성화/비활성화 제어 로직 추가

---

## 🔴 남은 이슈 (전투 시스템 - 별도 작업 필요)

- **이슈 #4**: 공격 애니메이션 중복 재생
- **이슈 #5**: HP Bar 비율 표시 오류
- **이슈 #7**: 턴 순환 오류 (한 쪽이 두 번 공격)

**비고**: Scene 전환 시스템과는 별개의 전투 시스템 버그

---

## 🎯 다음 작업 (2025-11-06 이후)

1. **전투 시스템 버그 수정**
   - 이슈 #4, #5, #7 해결

2. **Scene 전환 시스템 개선 (선택)**
   - Fade 속도 조정
   - 로딩 화면 추가 (선택)

3. **LobbyScene 구현 (추후)**
   - TestScene의 검증된 메커니즘을 LobbyScene에 적용
   - 정식 게임용 UI 구성

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

## 🔧 추가 작업: Character 관리 구조 재설계

### 문제 인식

**ActionCommandEquipUI 버그 원인:**
- TestScene과 CombatScene에서 Character 접근 경로가 달랐음
- CombatScene: `CombatCharacterManager.PlayerCharacter`
- TestScene: `PlayerController.Character` (없음!)

**근본 문제:**
- Character 인스턴스가 전투마다 새로 생성됨
- 영속 데이터와 임시 데이터가 혼재
- Single Source of Truth 부재

---

### 해결: Character 관리 아키텍처 재설계

#### 1. NonPlayerCharacterManager 생성 ⭐

**파일**: `Assets/Script/NonPlayerCharacterManager.cs` (신규)

**역할:**
- 모든 NPC/Enemy의 영속 인스턴스 관리
- Instance ID로 Character 조회
- Lazy 생성 방식 (메모리 효율)

**핵심 메서드:**
```csharp
public Character GetCharacter(string instanceId)
{
    // 캐시 확인 → 있으면 반환
    // 없으면 CharacterDatabaseManager.CreateCharacter() 호출 → 캐시에 저장
}
```

---

#### 2. PlayerCharacterManager 수정

**변경 사항:**
- `CharacterInventory Inventory` 제거
- `PlayerCharacter PlayerCharacter` 추가 (영속 인스턴스)
- `CreatePlayerCharacterForBattle()` 제거
- 게임 시작 시 1회 생성, 이후 재사용

---

#### 3. CharacterDatabaseManager 수정

**Factory 역할 추가:**
```csharp
public Character CreateCharacter(string instanceId)
{
    // Entry 조회 → InitData 로드 → Character 생성 → 반환
    // 관리는 안 함 (호출자가 관리)
}
```

---

#### 4. CombatCharacterManager 수정

**Before:**
```csharp
public PlayerCharacter PlayerCharacter { get; private set; }
public EnemyCharacter CurrentEnemy { get; private set; }

// 전투 시작 시 생성
private PlayerCharacter CreatePlayer(...) { ... }
private EnemyCharacter CreateEnemy(...) { ... }
```

**After:**
```csharp
// 참조만 보관 (소유 안 함)
public PlayerCharacter PlayerCharacter 
    => PlayerCharacterManager.Instance?.PlayerCharacter;

public List<EnemyCharacter> EnemyCharacters
{
    get
    {
        // NonPlayerCharacterManager에서 가져오기
    }
}

// 생성 로직 제거, ID만 저장
public void InitializeBattle(string playerId, params string[] enemyIds)
{
    PlayerInstanceId = playerId;
    EnemyInstanceIds = new List<string>(enemyIds);
}
```

---

#### 5. Controller 수정

**PlayerController.cs:**
```csharp
// Before
public Character Character => CombatCharacterManager.Instance?.PlayerCharacter;

// After
public Character Character => PlayerCharacterManager.Instance?.PlayerCharacter;
```

**변경 이유:** 모든 Scene에서 통일된 접근

---

#### 6. UI 수정

**ActionCommandEquipUI.cs, InventoryUI.cs:**
```csharp
// Before: Scene별로 다른 접근
if (CombatScene) → CombatCharacterManager.PlayerCharacter
if (TestScene)   → PlayerController.Character (없음!)

// After: 모든 Scene에서 동일
PlayerCharacterManager.Instance.PlayerCharacter
```

---

### 결과: Single Source of Truth 달성 ✅

```
모든 Scene에서:
- Player: PlayerCharacterManager.Instance.PlayerCharacter
- NPC/Enemy: NonPlayerCharacterManager.Instance.GetCharacter(id)

→ 통일된 접근 경로
→ 영속성 보장
→ TestScene의 ActionCommandEquipUI 정상 동작!
```

---

## 🎯 Scene 전환 시스템 최종 개선

### SceneFlowController 역할 명확화

**Flow 관리자로 확정:**
- `GoToTitle()`: Scene 전환만
- `GoToTestScene()`: Scene 전환만
- `StartCombatFlow(playerId, enemyId)`: 데이터 전달 + Scene 전환 + 전투 트리거
- `ShowResultFlow(result)`: 데이터 전달 + Scene 전환

**설계 철학:**
- 각 Scene 진입 시 필요한 데이터 전달
- Scene 로드 후 초기화 트리거
- 일관된 인터페이스 제공

---

### TestScene Enemy 선택 기능 추가

**파일**: `Assets/Script/Scene/TestSceneManager.cs`

**추가 기능:**
- Enemy 선택 Dropdown
- CharacterDatabaseManager에서 Enemy 목록 자동 로드
- 선택한 Enemy로 전투 시작

**Unity 설정:**
```
TestScene
└─ EnemySelectionDropdown (TMP_Dropdown)
   └─ TestSceneManager에 연결
```

---

## ✅ 최종 검증

### Scene 전환 Flow 테스트

```
TitleScene
  ↓ "게임 시작"
TestScene
  ↓ Enemy 선택 + "전투 시작"
CombatScene (선택한 Enemy와 전투)
  ↓ 전투 종료
ResultScene (결과 표시)
  ↓ "계속하기"
TestScene (복귀)

✅ 모든 전환 정상 동작
✅ Fade In/Out 효과 정상
✅ Character 데이터 유지 확인
```

---

### Character 영속성 테스트

```
1. TestScene: 검술 장착
   → PlayerCharacterManager.PlayerCharacter.EquipAction()
   
2. CombatScene 전환
   → 장착된 검술 정상 표시 ✅
   
3. 전투 중 HP 감소
   → PlayerCharacterManager.PlayerCharacter.CurrentHP 변경
   
4. ResultScene → TestScene 복귀
   → HP 변경사항 유지 ✅

✅ Single Source of Truth 동작 확인
```

---

## 📚 생성된 문서

### Scene 전환 시스템 관련

1. `Docs/Design/Scene/Scene_전환_시스템_Unity_설정_가이드.md` (업데이트)
   - NonPlayerCharacterManager 추가 가이드
   - Enemy 선택 Dropdown 추가 가이드

2. `Docs/TroubleShoot/Scene_전환_시스템_TroubleShooting.md`
   - 디버깅 과정 및 해결책 문서화

---

### 전투 세션 시스템 (신규 기획)

**폴더**: `Docs/CombatSessionSystem/`

1. **전투_세션_시스템_디자인.md**
   - 다대다 전투 비전
   - 시나리오 기반 설계
   - 감정적 설계 (긴장감, 성취감)
   - "재밌겠다! 만들고 싶다!" 느낌

2. **전투_세션_시스템_구현_명세서.md**
   - CombatSessionManager 클래스 명세
   - Battle 클래스 명세
   - BattleExecutor 클래스 명세
   - 매칭 알고리즘 상세

3. **전투_세션_시스템_구현_계획서.md**
   - 4단계 구현 로드맵
   - 일정 계획 (4.5-5.5일 예상)
   - 마일스톤 정의

---

## 🔗 관련 문서
- `Docs/Design/Scene/Scene_계층_구조_설계.md` (설계 문서)
- `Docs/Design/Scene/Scene_계층_구조_구현_계획.md` (구현 계획 - 완료 상태 업데이트됨)
- `Docs/Design/Scene/Scene_전환_시스템_Unity_설정_가이드.md` (Unity 설정 가이드)
- `Docs/TroubleShoot/Scene_전환_시스템_TroubleShooting.md` (문제 해결 문서)
- `Docs/CombatSessionSystem/` (신규 - 다대다 전투 시스템 문서)

---

## 📊 오늘의 성과

### 1. Scene 전환 시스템 완성 ✅

- 6개 Scene 구현 및 연동 완료
- Fade 효과와 함께 부드러운 전환
- 데이터 전달 및 초기화 Flow 완성

---

### 2. Character 관리 아키텍처 확립 ✅

- Single Source of Truth 달성
- 모든 Scene에서 통일된 Character 접근
- 영속 데이터와 임시 데이터 명확히 분리

---

### 3. 다음 개발 방향 수립 ✅

- 다대다 전투 시스템 기획 완료
- 3개 문서 작성으로 구현 준비 완료
- 4.5일 로드맵 수립

---

## 🔧 추가 작업: CharacterInitData 개선

### Initial Equipment 구조 변경

**문제:**
- 기존: List<InitialEquipmentEntry> (동적 추가/삭제 가능)
- 문제점: 슬롯은 고정인데 List로 관리 (불필요한 유연성)

**해결:**
```csharp
// Before
public List<InitialEquipmentEntry> initialEquipment;
// 슬롯 타입과 아이템 ID를 Entry로 관리

// After
public string weaponSlot;           // 무기 슬롯
public string armorSlot;            // 갑옷 슬롯
public string swordArtStyleSlot;    // 유파 슬롯
public string[] accessorySlots;     // 장신구 슬롯 (가변)
```

**장신구 슬롯 자동 조정:**
```csharp
#if UNITY_EDITOR
private void OnValidate()
{
    // initialAccessorySlots 값 변경 시 배열 크기 자동 조정
    if (accessorySlots.Length != initialAccessorySlots)
    {
        Array.Resize(ref accessorySlots, initialAccessorySlots);
    }
}
#endif
```

**효과:**
- Inspector에서 Initial Accessory Slots 값 변경 → 배열 크기 자동 조정
- 슬롯별로 명확한 필드명
- 코드 가독성 향상

---

### 관련 코드 수정

**파일 수정:**
1. `CharacterInitData.cs`
   - InitialEquipmentEntry 구조체 제거
   - 슬롯별 필드 추가
   - OnValidate() 추가 (배열 자동 조정)

2. `PlayerCharacterManager.cs`
   - InitializeInventory() 수정 (슬롯 방식으로)
   - EquipItemIfValid() 헬퍼 메서드 추가

3. `CharacterDatabaseManager.cs`
   - InitializeInventory() 수정 (슬롯 방식으로)
   - EquipItemIfValid() 헬퍼 메서드 추가

4. `EnemyCharacter.cs`
   - 에러 메시지 업데이트

---

## 📚 문서 작업 완료

### 전투 세션 시스템 문서 3종 작성 완료

**폴더**: `Docs/CombatSessionSystem/`

#### 1. 전투_세션_시스템_디자인.md (764줄)

**내용:**
- 비전: "살아있는 전장"
- 게임플레이 시나리오 (숲 속의 매복 등)
- 감정적 설계 (긴장감, 성취감, 몰입감)
- 승패 판정 규칙 (선두 전투 불능 시 즉시 종료)
- 엔트리 순서 규칙 (플레이어/보스 고정)
- Reserve 패시브 능력 (향후 확장)

**목표:** "재밌겠다! 만들고 싶다!" 느낌

---

#### 2. 전투_세션_시스템_구현_명세서.md (750줄)

**내용:**
- 시스템 구성 요소 사양 (CombatSessionManager, Battle, BattleExecutor)
- 데이터 구조 사양 (SessionResult, BattleResult, Enum)
- 동작 원리 (흐름 다이어그램)
- 승패 판정 알고리즘 (즉시 종료 로직)
- 매칭 알고리즘 (엔트리 순서 기반)
- 구현 고려사항 (주의사항, 권장사항)
- Reserve 패시브 구현 명세 (향후)

**특징:**
- 코드 최소화, 구조와 사양 중심
- 전자 제품 사양서 같은 느낌
- 구현 가이드라인 제공

---

#### 3. 전투_세션_시스템_구현_계획서.md (527줄)

**내용:**
- 4단계 구현 로드맵
  - 1단계: 1:1 일반화 (NPC vs NPC) - 1일
  - 2단계: Battle 클래스 분리 - 1일
  - 3단계: CombatSessionManager (다대다 + 승패 규칙) - 1일
  - 4단계: View 시스템 - 1일
- 일정 계획 (Day 1~5)
- 마일스톤 및 검증 조건
- 테스트 시나리오 (즉시 승패 판정 포함)
- 위험 요소 및 대응

**예상 총 기간:** 4.5-5.5일

---

## 💡 내일 작업

**전투 세션 시스템 구현 시작**
- 1단계: 1:1 일반화 (NPC vs NPC 지원)
- CombatCharacterManager 수정
- AIController 생성
- 테스트 및 검증

---

## 🎯 오늘의 최종 성과

### 1. Scene 전환 시스템 완성 ✅
- 6개 Scene 구현 및 연동
- Fade 효과, 데이터 전달, 초기화 Flow
- 통합 테스트 완료

### 2. Character 아키텍처 확립 ✅
- NonPlayerCharacterManager 추가
- Single Source of Truth 달성
- 모든 Scene 통일된 접근

### 3. SceneFlowController 역할 명확화 ✅
- Flow 관리자로 확정
- 데이터 전달 + Scene 전환 + 트리거

### 4. 다대다 전투 시스템 기획 ✅
- 3개 문서 작성 (디자인, 명세서, 계획서)
- 승패 판정 규칙 정의
- 4.5일 로드맵 수립

### 5. CharacterInitData 개선 ✅
- Initial Equipment List → 슬롯 필드
- 장신구 배열 자동 조정 (OnValidate)
- 코드 간결화

### 6. poiseGain 스탯 구현 및 Poise 회복 시스템 개선 ✅

#### 문제 인식
- `poiseGain` 스탯이 `CombatStats`에 정의되어 있었으나 **실제로 사용되지 않음**
- Poise 회복이 무조건 100% 완전 회복으로 고정되어 전략적 다양성 부족

#### 구현 내용

**1. poiseGain 스탯 정의 변경**
- 기존: 정수 스탯 (0~200 범위, 미사용)
- 변경: **비율 스탯 (0~1 범위)** - Poise 회복률을 나타냄

**관련 파일:**
- `Assets/Script/Combat/CombatStats.cs`
  - Tooltip 추가: "Poise 회복률 (0~1, 1.0 = 100% 회복)"
  - Range(0f, 1f) 속성 추가
- `Assets/Resources/Data/StatLimitRules.asset`
  - poiseGain 범위: 0~200 → 0~1 변경

**2. Poise 회복 로직 수정**
- `Assets/Script/Character.cs` - `ResetPoise()` 메서드
- **기존**: `currentPoise = maxPoise` (무조건 100% 회복)
- **변경**: `currentPoise = min(currentPoise + maxPoise * poiseGain, maxPoise)` (비율 회복)
- 디버그 로그 개선: 회복량 및 회복률 표시

**3. StatsCalculationManager 버그 수정 및 개선**
- `Assets/Script/Combat/StatsCalculationManager.cs` - `ConvertToCombatStats()` 메서드
- **문제**: 수동으로 필드를 하나씩 복사하여 `poiseGain` 누락 → 런타임에 항상 0
- **개선**: `data.baseStats`를 직접 반환 (구조체 값 복사)
  - 14줄 → 3줄로 간소화
  - 신규 필드 추가 시 자동 반영
  - 누락 오류 방지

**4. 문서 업데이트**
- `Docs/Design/CombatStatus/CombatStats_System_Design.md`
  - `poiseGain`을 정수 스탯에서 **비율 스탯(0~1)**으로 재분류
  - 주석 추가: "Poise 회복률 (0~1, 1.0 = 100% 회복)"

#### 실제 효과

**캐릭터별 Poise 회복률 차별화:**
- Player (poiseGain: 0.0): 회복 없음 (하드 모드)
- Test_Enemy1 (poiseGain: 0.5): 매 턴 50% 회복
- Test_Enemy2 (poiseGain: 1.0): 매 턴 100% 완전 회복

**전략적 의미:**
- 장비/버프로 `poiseGain` 조절 가능
- 캐릭터마다 다른 Poise 관리 전략 필요
- 전투 난이도 조절 수단

#### 기술적 개선점
1. **코드 품질**: 수동 복사 제거로 유지보수성 향상
2. **자동 완전성**: 신규 필드 추가 시 자동 반영
3. **가독성**: 의도가 명확한 간결한 코드
4. **확장성**: 비율 기반으로 다양한 밸런싱 가능

---

**작업 완료**: 2025-11-05 오후



