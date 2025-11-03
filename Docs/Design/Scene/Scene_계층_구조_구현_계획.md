# Scene 계층 구조 구현 계획

**작성일**: 2025-11-03  
**작업 시작 예정일**: 2025-11-04  
**목적**: Scene_계층_구조_설계.md의 설계를 실제로 구현  
**범위**: 최소 Scene 구조부터 점진적 확장

---

## 📋 목차

1. [현재 상태](#현재-상태)
2. [구현 우선순위](#구현-우선순위)
3. [Phase 1: 최소 Scene 구조](#phase-1-최소-scene-구조)
4. [Phase 2: PersistentUI 구현](#phase-2-persistentui-구현)
5. [Phase 3: 전투 결과 처리](#phase-3-전투-결과-처리)
6. [Phase 4: 추가 Scene 확장](#phase-4-추가-scene-확장)

---

## 현재 상태

### ✅ 완료된 작업

**설계:**
- `Scene_계층_구조_설계.md` 작성 완료 (996줄)
- 3-Layer Scene 구조 설계
- Scene별 역할 및 생명주기 정의
- 매니저 아키텍처 정의

**코드:**
- CharacterManager 리팩토링 완료
- 매니저 3개 분리 (PlayerCharacterManager, CharacterDatabaseManager, CombatCharacterManager)
- 모든 전투 시스템 정상 동작

**Unity 에셋:**
- CharacterDatabase.asset 생성
- CharacterInitData 에셋 설정
- ProtoType Scene에 새 매니저들 배치 완료

---

### 🔄 현재 Scene 상태

```
Assets/Scenes/
├─ ProtoType.unity
│  ├─ 모든 시스템이 여기 포함됨 (영속 + UI + 전투)
│  ├─ CharacterDatabaseManager ✅
│  ├─ PlayerCharacterManager ✅
│  ├─ CombatCharacterManager ✅
│  └─ CombatManager, Controllers, UI 등 모두 포함
│
└─ SampleScene.unity (미사용)
```

**문제점:**
- 영속 매니저와 전투 시스템이 하나의 Scene에 혼재
- Scene 전환 불가 (모든 것이 ProtoType Scene에 있음)
- 전투 종료 개념 없음 (Scene이 계속 유지됨)

---

## 구현 우선순위

### 🎯 Phase 1: 최소 Scene 구조 (필수, 내일 목표)

**목표:** TestScene ↔ CombatScene 전환 플로우 구현

```
CoreSystemScene (영속 매니저)
    ↓ Additive
TestScene (테스트 시작점)
    ↓ 전투 시작 버튼
CombatScene (전투 진행) - Additive
    ↓ 전투 종료
TestScene (복귀) - CombatScene 언로드
```

**구현 Scene:**
- CoreSystemScene ← 새로 생성 🆕
- TestScene ← 새로 생성 🆕
- CombatScene ← ProtoType 기반 정리 🔧

**예상 소요 시간: 3시간**

---

### 🎯 Phase 2: PersistentUI 구현 (중요)

**목표:** 공통 UI를 별도 Scene으로 분리

```
PersistentUIScene (Overlay)
├─ 디버그 패널 (BTMonitorUI, CombatStatusDisplay)
├─ 설정 버튼
└─ 로딩 인디케이터
```

**구현 Scene:**
- PersistentUIScene ← 새로 생성 🆕

**예상 소요 시간: 1시간**

---

### 🎯 Phase 3: 전투 결과 처리 (중요)

**목표:** 전투 종료 시 결과 표시 및 보상 처리

```
전투 종료
    ↓
BattleResult 생성 (승패, 통계, 보상)
    ↓
CombatCharacterManager.FinalizeBattle()
    ├─ 플레이어 상태 동기화
    └─ 보상 적용 (Gold, Exp)
    ↓
OnBattleEnd 이벤트 발생
    ↓
TestScene 복귀 (또는 ResultScene 로드)
```

**예상 소요 시간: 2시간**

---

### 🎯 Phase 4: 추가 Scene 확장 (추후)

**목표:** 게임 전체 플로우 구현

```
TitleScene → MainMenuScene → CombatScene/InventoryScene/etc
```

**예상 소요 시간: 5-10시간**

---

## Phase 1: 최소 Scene 구조

### Step 1-1: CoreSystemScene 생성

#### 작업 내용

**1. Scene 파일 생성**
```
위치: Assets/Scenes/Core/CoreSystemScene.unity
```

**2. GameObject 배치**
```
Hierarchy:
├─ CoreSystemManager (Empty GameObject)
│  └─ Component: CoreSystemManager (새로 생성)
│
├─ CharacterDatabaseManager
│  └─ Component: CharacterDatabaseManager
│     └─ Database Asset: [CharacterDatabase]
│
├─ PlayerCharacterManager
│  └─ Component: PlayerCharacterManager
│
└─ (필요시 다른 영속 매니저들)
```

**3. CoreSystemManager.cs 작성**
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CoreSystemScene 초기화 및 Scene 로드 순서 관리
/// </summary>
public class CoreSystemManager : MonoBehaviour
{
    [Header("로드할 Scene 설정")]
    [SerializeField] private string persistentUISceneName = "PersistentUIScene";
    [SerializeField] private string initialContentSceneName = "TestScene";
    
    [SerializeField] private bool loadPersistentUI = true;
    [SerializeField] private bool loadInitialContent = true;
    
    private void Start()
    {
        Debug.Log("[CoreSystemManager] CoreSystemScene 초기화 시작");
        
        // 영속 매니저들 초기화 대기
        StartCoroutine(InitializeCoreSystemsAndLoadScenes());
    }
    
    private System.Collections.IEnumerator InitializeCoreSystemsAndLoadScenes()
    {
        // CharacterDatabaseManager, PlayerCharacterManager 초기화 대기
        while (CharacterDatabaseManager.Instance == null || 
               PlayerCharacterManager.Instance == null)
        {
            yield return null;
        }
        
        Debug.Log("[CoreSystemManager] 영속 매니저 초기화 완료");
        
        // 1. PersistentUIScene Additive 로드
        if (loadPersistentUI)
        {
            yield return SceneManager.LoadSceneAsync(persistentUISceneName, LoadSceneMode.Additive);
            Debug.Log($"[CoreSystemManager] {persistentUISceneName} 로드 완료");
        }
        
        // 2. 초기 Content Scene Additive 로드
        if (loadInitialContent)
        {
            yield return SceneManager.LoadSceneAsync(initialContentSceneName, LoadSceneMode.Additive);
            Debug.Log($"[CoreSystemManager] {initialContentSceneName} 로드 완료");
        }
        
        Debug.Log("[CoreSystemManager] 모든 Scene 로드 완료");
    }
}
```

**예상 소요 시간: 30분**

---

### Step 1-2: TestScene 생성

#### 작업 내용

**1. Scene 파일 생성**
```
위치: Assets/Scenes/Content/TestScene.unity
```

**2. UI 구성**
```
Hierarchy:
├─ Canvas (Screen Space - Overlay)
│  └─ TestPanel
│     ├─ Title: "전투 테스트"
│     ├─ Player ID Dropdown
│     ├─ Enemy ID Dropdown
│     ├─ Start Battle Button
│     └─ Status Text
│
└─ TestSceneManager (Empty GameObject)
   └─ Component: TestSceneManager (새로 생성)
```

**3. TestSceneManager.cs 작성**
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// TestScene 관리 및 전투 시작/종료 처리
/// </summary>
public class TestSceneManager : MonoBehaviour
{
    [Header("Scene 설정")]
    [SerializeField] private string combatSceneName = "CombatScene";
    
    [Header("UI 참조")]
    [SerializeField] private TMP_Dropdown playerIdDropdown;
    [SerializeField] private TMP_Dropdown enemyIdDropdown;
    [SerializeField] private UnityEngine.UI.Button startBattleButton;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("테스트 기본값")]
    [SerializeField] private string defaultPlayerId = "Player";
    [SerializeField] private string defaultEnemyId = "Test_Enemy1";
    
    private bool isBattleActive = false;
    
    private void Start()
    {
        // UI 초기화
        InitializeUI();
        
        // 버튼 이벤트 연결
        if (startBattleButton != null)
        {
            startBattleButton.onClick.AddListener(OnStartBattleClicked);
        }
        
        UpdateStatus("테스트 준비 완료");
    }
    
    private void InitializeUI()
    {
        // CharacterDatabase에서 캐릭터 목록 가져와서 드롭다운 채우기
        if (CharacterDatabaseManager.Instance != null)
        {
            // Player 드롭다운
            if (playerIdDropdown != null)
            {
                playerIdDropdown.ClearOptions();
                var playerEntry = CharacterDatabaseManager.Instance.GetPlayerEntry();
                if (playerEntry != null)
                {
                    playerIdDropdown.AddOptions(new System.Collections.Generic.List<string> { playerEntry.instanceId });
                    playerIdDropdown.value = 0;
                }
            }
            
            // Enemy 드롭다운 (나중에 확장)
            if (enemyIdDropdown != null)
            {
                enemyIdDropdown.ClearOptions();
                var enemyEntry = CharacterDatabaseManager.Instance.GetFirstEnemyEntry();
                if (enemyEntry != null)
                {
                    enemyIdDropdown.AddOptions(new System.Collections.Generic.List<string> { enemyEntry.instanceId });
                    enemyIdDropdown.value = 0;
                }
            }
        }
    }
    
    private void OnStartBattleClicked()
    {
        if (isBattleActive)
        {
            Debug.LogWarning("[TestSceneManager] 이미 전투가 진행 중입니다!");
            return;
        }
        
        // 선택된 전투원 정보 가져오기
        string playerId = playerIdDropdown != null && playerIdDropdown.options.Count > 0
            ? playerIdDropdown.options[playerIdDropdown.value].text
            : defaultPlayerId;
            
        string enemyId = enemyIdDropdown != null && enemyIdDropdown.options.Count > 0
            ? enemyIdDropdown.options[enemyIdDropdown.value].text
            : defaultEnemyId;
        
        StartBattle(playerId, enemyId);
    }
    
    private void StartBattle(string playerId, string enemyId)
    {
        Debug.Log($"[TestSceneManager] 전투 시작 요청: {playerId} vs {enemyId}");
        UpdateStatus($"전투 준비 중... ({playerId} vs {enemyId})");
        
        StartCoroutine(LoadCombatSceneAndStartBattle(playerId, enemyId));
    }
    
    private System.Collections.IEnumerator LoadCombatSceneAndStartBattle(string playerId, string enemyId)
    {
        // 1. CombatScene Additive 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(combatSceneName, LoadSceneMode.Additive);
        
        while (!asyncLoad.isDone)
        {
            float progress = asyncLoad.progress * 100f;
            UpdateStatus($"전투 Scene 로딩 중... {progress:F0}%");
            yield return null;
        }
        
        Debug.Log("[TestSceneManager] CombatScene 로드 완료");
        UpdateStatus("전투 시작!");
        
        // 2. CombatManager 찾기 및 전투 시작
        yield return null; // Scene 로드 완료 대기
        
        var combatManager = CombatManager.Instance;
        if (combatManager == null)
        {
            Debug.LogError("[TestSceneManager] CombatManager를 찾을 수 없습니다!");
            UpdateStatus("오류: CombatManager 없음");
            yield break;
        }
        
        // 3. 전투 시작
        combatManager.StartBattle(playerId, enemyId);
        isBattleActive = true;
        
        // 4. 전투 종료 대기 (추후 이벤트 기반으로 변경)
        // 현재는 임시로 CombatManager의 전투 상태를 체크
        // TODO: CombatManager.OnBattleEnd 이벤트 구독으로 변경
    }
    
    /// <summary>
    /// 전투 종료 처리 (외부에서 호출 또는 이벤트로 트리거)
    /// </summary>
    public void OnBattleEnded(bool victory)
    {
        if (!isBattleActive)
        {
            Debug.LogWarning("[TestSceneManager] 전투가 진행 중이 아닙니다!");
            return;
        }
        
        Debug.Log($"[TestSceneManager] 전투 종료: {(victory ? "승리" : "패배")}");
        UpdateStatus($"전투 종료! {(victory ? "승리!" : "패배...")}");
        
        StartCoroutine(UnloadCombatScene());
    }
    
    private System.Collections.IEnumerator UnloadCombatScene()
    {
        yield return new WaitForSeconds(2f); // 결과 확인을 위한 대기
        
        UpdateStatus("전투 Scene 언로드 중...");
        
        // CombatScene 언로드
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(combatSceneName);
        
        while (!asyncUnload.isDone)
        {
            yield return null;
        }
        
        Debug.Log("[TestSceneManager] CombatScene 언로드 완료");
        UpdateStatus("테스트 준비 완료 (Scene 복귀 완료)");
        
        isBattleActive = false;
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[TestSceneManager] Status: {message}");
    }
}
```

**예상 소요 시간: 1시간**

---

### Step 1-3: CombatScene 정리

#### 작업 내용

**1. ProtoType Scene → CombatScene 복사**
```
작업:
1. ProtoType.unity 복사 → CombatScene.unity
2. 위치: Assets/Scenes/Content/CombatScene.unity
```

**2. GameObject 정리**
```
제거할 GameObject (CoreSystemScene으로 이동):
❌ CharacterDatabaseManager
❌ PlayerCharacterManager

유지할 GameObject:
✅ CombatCharacterManager
✅ CombatManager
✅ PlayerController
✅ EnemyController
✅ 모든 UI (일단 유지, 추후 PersistentUIScene으로 이동)
✅ Camera, Lighting, etc
```

**3. CombatManager 수정**
```csharp
// Start()에서 자동 전투 시작 제거
// StartBattle()은 외부(TestSceneManager)에서 호출하도록 변경

// 추가: 전투 종료 이벤트
public event System.Action<bool> OnBattleEnd; // bool: victory

// RunCombat() 종료 시 이벤트 발생
private IEnumerator RunCombat()
{
    // ... 전투 로직 ...
    
    // 전투 종료
    bool victory = /* 승패 판정 */;
    OnBattleEnd?.Invoke(victory);
}
```

**예상 소요 시간: 1시간**

---

### Step 1-4: Scene 전환 플로우 테스트

#### 테스트 시나리오

**1. 게임 시작**
```
Play 버튼 클릭 (CoreSystemScene 실행)
    ↓
CoreSystemManager가 영속 매니저 초기화
    ↓
TestScene Additive 로드
    ↓
TestScene UI 표시
```

**2. 전투 시작**
```
TestScene에서 "전투 시작" 버튼 클릭
    ↓
CombatScene Additive 로드
    ↓
CombatManager.StartBattle("Player", "Test_Enemy1")
    ↓
전투 진행
```

**3. 전투 종료**
```
전투 승패 결정
    ↓
CombatManager.OnBattleEnd 이벤트 발생
    ↓
TestSceneManager.OnBattleEnded() 호출
    ↓
CombatScene 언로드
    ↓
TestScene 복귀 (플레이어 상태 유지됨)
```

**예상 소요 시간: 30분**

---

## Phase 2: PersistentUI 구현

### Step 2-1: PersistentUIScene 생성

#### 작업 내용

**1. Scene 파일 생성**
```
위치: Assets/Scenes/UI/PersistentUIScene.unity
```

**2. Canvas 구성**
```
Hierarchy:
├─ PersistentUICanvas
│  ├─ Render Mode: Screen Space - Overlay
│  ├─ Sort Order: 100 (Content Scene 위에 표시)
│  │
│  ├─ DebugPanel (CombatScene에서 이동)
│  │  ├─ BTMonitorUI
│  │  └─ CombatStatusDisplay
│  │
│  ├─ SettingsButton
│  └─ LoadingIndicator
│
└─ PersistentUIManager (Empty GameObject)
   └─ Component: PersistentUIManager (새로 생성)
```

**3. PersistentUIManager.cs 작성**
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PersistentUI 가시성 제어 및 Scene별 표시 정책 관리
/// </summary>
public class PersistentUIManager : MonoBehaviour
{
    [Header("UI 요소 참조")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private GameObject settingsButton;
    [SerializeField] private GameObject loadingIndicator;
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateUIVisibility(scene.name, true);
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        UpdateUIVisibility(scene.name, false);
    }
    
    private void UpdateUIVisibility(string sceneName, bool loaded)
    {
        switch (sceneName)
        {
            case "CombatScene":
                if (debugPanel != null) debugPanel.SetActive(loaded);
                break;
                
            case "TestScene":
                if (debugPanel != null) debugPanel.SetActive(true); // 테스트는 항상 표시
                break;
        }
    }
}
```

**예상 소요 시간: 1시간**

---

## Phase 3: 전투 결과 처리

### Step 3-1: CombatManager 전투 종료 이벤트

#### 작업 내용

**1. BattleResult 확장**
```csharp
// BattleResult.cs
public class BattleResult
{
    // 기존
    public bool isVictory;
    public int goldReward;
    public int expReward;
    
    // 추가
    public int totalTurns;            // 전투 턴 수
    public int totalDamageDealt;      // 플레이어가 입힌 총 데미지
    public int totalDamageTaken;      // 플레이어가 받은 총 데미지
    public int criticalHitCount;      // 크리티컬 횟수
    public int perfectParryCount;     // 완벽한 쳐내기 횟수
}
```

**2. CombatManager 수정**
```csharp
// CombatManager.cs

// 이벤트 선언
public event System.Action<BattleResult> OnBattleEnd;

// Start() 수정 - 자동 전투 시작 제거
private void Start()
{
    // 전투 결과 초기화만
    battleResult = new BattleResult();
    battleResult.InitializeBattle();
    
    // 자동 시작 제거 - 외부에서 StartBattle() 호출하도록 변경
}

// RunCombat() 종료 부분 수정
private IEnumerator RunCombat()
{
    // ... 전투 로직 ...
    
    // 전투 종료
    bool victory = DetermineVictory();
    battleResult.isVictory = victory;
    battleResult.goldReward = victory ? 100 : 0;
    battleResult.expReward = victory ? 50 : 0;
    
    // CombatCharacterManager에 전투 종료 통보
    CombatCharacterManager.Instance.FinalizeBattle(battleResult);
    
    // 이벤트 발생
    OnBattleEnd?.Invoke(battleResult);
    
    Debug.Log($"[CombatManager] 전투 종료: {(victory ? "승리" : "패배")}");
}

private bool DetermineVictory()
{
    var player = CombatCharacterManager.Instance.PlayerCharacter;
    var enemy = CombatCharacterManager.Instance.CurrentEnemy;
    
    if (player == null || enemy == null)
        return false;
    
    // 승리 조건: 적의 HP가 0
    return enemy.CurrentHP <= 0 && player.CurrentHP > 0;
}
```

**예상 소요 시간: 1시간**

---

### Step 3-2: TestSceneManager와 CombatManager 연동

**TestSceneManager 수정:**
```csharp
private System.Collections.IEnumerator LoadCombatSceneAndStartBattle(string playerId, string enemyId)
{
    // ... Scene 로드 ...
    
    var combatManager = CombatManager.Instance;
    
    // 전투 종료 이벤트 구독
    combatManager.OnBattleEnd += OnBattleEnded;
    
    // 전투 시작
    combatManager.StartBattle(playerId, enemyId);
    isBattleActive = true;
}

private void OnBattleEnded(BattleResult result)
{
    // 이벤트 구독 해제
    if (CombatManager.Instance != null)
    {
        CombatManager.Instance.OnBattleEnd -= OnBattleEnded;
    }
    
    Debug.Log($"[TestSceneManager] 전투 종료 이벤트 수신: {(result.isVictory ? "승리" : "패배")}");
    UpdateStatus($"전투 종료! {(result.isVictory ? "승리!" : "패배...")}");
    
    StartCoroutine(UnloadCombatScene());
}
```

**예상 소요 시간: 30분**

---

## 📋 내일 작업 체크리스트 (2025-11-04)

### Phase 1: 최소 Scene 구조 (필수)

- [ ] **Step 1-1: CoreSystemScene 생성 (30분)**
  - [ ] Scene 파일 생성
  - [ ] GameObject 배치 (CharacterDatabaseManager, PlayerCharacterManager)
  - [ ] CoreSystemManager.cs 작성
  - [ ] Scene 자동 로드 테스트

- [ ] **Step 1-2: TestScene 생성 (1시간)**
  - [ ] Scene 파일 생성
  - [ ] UI 구성 (드롭다운, 버튼, 상태 텍스트)
  - [ ] TestSceneManager.cs 작성
  - [ ] 전투 시작 기능 테스트

- [ ] **Step 1-3: CombatScene 정리 (1시간)**
  - [ ] ProtoType Scene 복사 → CombatScene
  - [ ] 영속 매니저 제거
  - [ ] CombatManager.Start() 수정 (자동 시작 제거)
  - [ ] OnBattleEnd 이벤트 추가

- [ ] **Step 1-4: Scene 전환 플로우 테스트 (30분)**
  - [ ] CoreSystemScene 실행
  - [ ] TestScene 자동 로드 확인
  - [ ] "전투 시작" → CombatScene 로드 확인
  - [ ] 전투 종료 → CombatScene 언로드 확인
  - [ ] TestScene 복귀 확인

**예상 소요 시간: 3시간**

---

### Phase 2: PersistentUI 구현 (선택)

- [ ] **Step 2-1: PersistentUIScene 생성 (1시간)**
  - [ ] Scene 파일 생성
  - [ ] Canvas 구성
  - [ ] 디버그 패널 이동
  - [ ] PersistentUIManager.cs 작성

**예상 소요 시간: 1시간**

---

### Phase 3: 전투 결과 처리 (선택)

- [ ] **Step 3-1: 전투 종료 이벤트 (30분)**
  - [ ] BattleResult 확장
  - [ ] CombatManager.OnBattleEnd 이벤트 구현
  - [ ] 승패 판정 로직 명확화

- [ ] **Step 3-2: 이벤트 연동 (30분)**
  - [ ] TestSceneManager에서 OnBattleEnd 구독
  - [ ] 전투 종료 → Scene 언로드 자동 처리

**예상 소요 시간: 1시간**

---

## 🎯 내일의 최소 목표

**반드시 완료:**
1. ✅ CoreSystemScene 생성 및 동작
2. ✅ TestScene 생성 및 전투 시작 기능
3. ✅ TestScene ↔ CombatScene 전환 플로우 동작

**목표 달성 시:**
- Scene 계층 구조의 기본 골격 완성
- 전투 시작 → 진행 → 종료 → 복귀 플로우 완성
- 이후 추가 Scene 및 기능 확장 가능한 기반 완성

---

## 📈 진행률 추정

```
Scene 계층 구조 구현

[Phase 1: 최소 구조]     ═══════════════════════>    (내일 목표)
[Phase 2: PersistentUI]  ════════>                   (내일 추가)
[Phase 3: 전투 결과]     ═════>                       (내일 추가)
[Phase 4: 추가 Scene]    >                            (추후)

전체 진행률: 설계 100% / 구현 0% → 내일 목표 50% 달성
```

---

## 🔑 핵심 포인트

### 이번 작업의 의미

**CharacterManager 리팩토링 완료로:**
- 영속 데이터 (PlayerCharacterManager) ↔ 전투 데이터 (CombatCharacterManager) 분리 완료
- Scene 계층 구조 구현의 기반 마련

**Scene 계층 구조 구현으로:**
- 전투 "시작"과 "종료" 개념 명확화
- Scene 로드/언로드를 통한 리소스 관리
- 상태 동기화가 의미를 갖게 됨 (전투 종료 → 복귀)

### 주의사항

1. **CoreSystemScene은 단 한 번만 로드**
   - 게임 시작 시 로드, 종료까지 유지
   - 영속 매니저들은 여기에만 배치

2. **Content Scene은 Additive 로드**
   - CoreSystemScene 위에 추가로 로드
   - 필요 없을 때 언로드

3. **전투 종료 처리는 Scene 전환 후 구현**
   - 현재는 Scene이 계속 유지되어 "종료" 개념 없음
   - TestScene ↔ CombatScene 전환이 구현되면 자연스럽게 가능

---

## 📝 추가 메모

### 의사결정 기록

**Q: 전투 종료 후 상태 동기화를 지금 구현할까?**
A: Scene 전환 플로우가 없어서 의미 없음. Scene 구현 후 진행.

**Q: InventoryTestManager는?**
A: TestScene에서 전투원 선택 기능으로 대체 가능. 추후 판단.

**Q: CharacterInitDataProvider가 필요한가?**
A: 불필요. Resources.Load로 충분. GameObject 제거.

---

## 🔗 관련 문서
- `Docs/Design/Scene/Scene_계층_구조_설계.md` (설계 문서)
- `Docs/Design/Scene/Scene_계층_구조_구현_계획.md` (이 문서)
- `Docs/Design/CharacterManager_분리_구현_계획서.md` (리팩토링 문서)

---

## 다음 일지 작성 시 확인할 사항
- [ ] CoreSystemScene 생성 완료 여부
- [ ] TestScene 생성 완료 여부
- [ ] Scene 전환 플로우 동작 여부
- [ ] 발생한 문제 및 해결 방법

