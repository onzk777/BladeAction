# Scene 구조 설계 가이드

## 문서 개요

이 문서는 BladeAction 프로젝트의 Unity Scene 구조 설계 및 마이그레이션 계획을 다룹니다.

**작성일**: 2025-10-22  
**상태**: 설계 단계 (구현은 핵심 시스템 개발 완료 후 진행)

---

## 목차

1. [Scene 관리 전략](#scene-관리-전략)
2. [최종 Scene 구조](#최종-scene-구조)
3. [Additive Scene Loading 가이드](#additive-scene-loading-가이드)
4. [공통 요소 관리 방법](#공통-요소-관리-방법)
5. [현재 개발 시 주의사항](#현재-개발-시-주의사항)
6. [마이그레이션 계획](#마이그레이션-계획)
7. [참고 코드 예시](#참고-코드-예시)

---

## Scene 관리 전략

### Unity의 3가지 Scene 관리 방식

#### 1. 단일 Scene + DontDestroyOnLoad 방식
- **장점**: 간단하고 직관적, Scene 전환 오버헤드 없음
- **단점**: Scene이 복잡해지면 관리 어려움, 메모리 관리 수동으로 해야 함
- **적용**: 프로토타이핑 단계 (현재 상태)

#### 2. 다중 Scene 분리 + Additive Loading 방식
- **장점**: 
  - 각 Scene의 역할이 명확하게 분리됨
  - 필요한 Scene만 로드/언로드하여 메모리 효율적
  - 팀 작업 시 Scene 충돌 최소화
  - 공통 요소를 별도 Scene으로 관리 가능
- **단점**: 초기 설정이 다소 복잡함
- **적용**: 프로덕션 단계

#### 3. 하이브리드 방식 ⭐ (프로젝트 채택 방식)
- Core 시스템은 `DontDestroyOnLoad`로 유지
- UI/환경/컨텐츠는 Additive Scene으로 관리
- **장점**: 두 방식의 장점을 모두 활용
- **적용**: 최종 구조

---

## 최종 Scene 구조

### Scene 목록 및 역할

```
BladeAction Scene 구조:

1. PersistentScene (항상 로드, 게임 시작 시 자동 로드)
   └─ 게임 내내 유지되는 핵심 시스템 (DontDestroyOnLoad)
      - CharacterManager
      - ItemManager
      - GameManager
      - AudioManager
      - SaveLoadManager
      - SceneLoader
      등 싱글톤 매니저들

2. MainMenuScene (시작 화면)
   └─ 타이틀 화면
   └─ 메인 메뉴 UI
   └─ 세이브 파일 선택 UI

3. WorldMapScene (월드맵)
   └─ 월드맵 UI
   └─ 지역 선택
   └─ 이동 경로 표시

4. TownScene (마을)
   └─ 마을 환경
   └─ NPC 배치
   └─ 마을 전용 UI

5. ExplorationScene (탐험, 미래 확장)
   └─ 던전 환경
   └─ 탐험 컨텐츠
   └─ 몬스터 인카운터

6. CombatScene (전투)
   └─ 전투 환경
   └─ 전투 전용 UI
   └─ CombatManager 활성화

7. CommonUIScene (공통 UI, Additive로 항상 로드)
   └─ 인벤토리 UI
   └─ 대화 UI (DialogueUI)
   └─ 시스템 메뉴
   └─ 알림/팝업
   └─ HUD (체력바, 골드, 경험치 등)
```

### Scene 로딩 구조도

```
[게임 시작]
  ↓
PersistentScene (항상 유지)
  ↓
MainMenuScene (단독 로드)
  ↓
[게임 시작 선택]
  ↓
WorldMapScene + CommonUIScene (Additive)
  ↓
[전투 발생]
  ↓
WorldMapScene 언로드 → CombatScene + CommonUIScene (Additive)
  ↓
[전투 종료]
  ↓
CombatScene 언로드 → WorldMapScene + CommonUIScene (Additive)
  ↓
[마을 진입]
  ↓
WorldMapScene 언로드 → TownScene + CommonUIScene (Additive)
```

---

## Additive Scene Loading 가이드

### Scene 로드/언로드 방법

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Scene 추가 로드 (현재 Scene을 유지하면서 추가)
SceneManager.LoadScene("CommonUIScene", LoadSceneMode.Additive);

// 비동기 Scene 로드 (로딩 진행도 추적 가능)
AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Additive);

// Scene 언로드
SceneManager.UnloadSceneAsync("WorldMapScene");

// 특정 Scene을 Active로 설정 (새로 생성되는 오브젝트가 이 Scene에 속함)
Scene scene = SceneManager.GetSceneByName("CombatScene");
SceneManager.SetActiveScene(scene);
```

### Scene 로딩 시 주의사항

1. **Active Scene 설정**: 새로운 GameObject가 어느 Scene에 속할지 결정
2. **메모리 정리**: Scene 언로드 후 리소스 정리
   ```csharp
   yield return Resources.UnloadUnusedAssets();
   System.GC.Collect();
   ```
3. **Build Settings**: 모든 Scene을 Build Settings에 추가 필수
4. **Scene 전환 중 입력 차단**: 로딩 중 플레이어 입력 무시

---

## 공통 요소 관리 방법

### 인벤토리/아이템 시스템 같은 공통 요소

#### 데이터 관리: DontDestroyOnLoad 싱글톤 매니저

```csharp
// ItemManager.cs - PersistentScene에 배치
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    
    // 인벤토리 데이터 (Scene 전환 시에도 유지)
    public Inventory PlayerInventory { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeInventory()
    {
        PlayerInventory = new Inventory(maxSlots: 50);
    }
}
```

#### UI 관리: CommonUIScene에 배치

```csharp
// InventoryUI.cs - CommonUIScene에 배치
public class InventoryUI : MonoBehaviour
{
    private Inventory inventory;
    
    private void Start()
    {
        // 매니저에서 데이터 참조
        inventory = ItemManager.Instance.PlayerInventory;
        RefreshUI();
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }
    
    private void RefreshUI()
    {
        // ItemManager의 데이터를 기반으로 UI 갱신
    }
}
```

### 대화 시스템 (Overlay 방식)

대화는 별도 Scene이 아닌 **UI Overlay**로 처리:

```csharp
// DialogueManager.cs - PersistentScene에 배치
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    
    private DialogueUI dialogueUI; // CommonUIScene의 UI 참조
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // CommonUIScene이 로드된 후 UI 참조
        dialogueUI = FindFirstObjectByType<DialogueUI>();
    }
    
    // 어느 Scene에서든 호출 가능
    public void ShowDialogue(DialogueData data)
    {
        if (dialogueUI != null)
        {
            dialogueUI.Show(data);
        }
    }
}
```

---

## 현재 개발 시 주의사항

> **중요**: 현재는 단일 Scene 프로토타이핑 단계이지만, 향후 Scene 구조 마이그레이션 시 리팩토링 비용을 최소화하기 위해 아래 원칙을 준수합니다.

### 1. 매니저 싱글톤 패턴 유지 ✅

```csharp
// ✅ 좋은 예: 표준 싱글톤 패턴
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

**이유**: Scene 구조로 전환해도 이 코드는 수정 없이 그대로 사용 가능

### 2. 직접 참조 대신 매니저를 통한 접근 ✅

```csharp
// ❌ 나쁜 예: UI가 직접 데이터 참조
public class InventoryUI : MonoBehaviour
{
    public Inventory inventory; // Inspector에서 직접 할당 - Scene 분리 시 참조 깨짐
}

// ✅ 좋은 예: 매니저를 통한 접근
public class InventoryUI : MonoBehaviour
{
    private void Start()
    {
        var inventory = ItemManager.Instance.PlayerInventory;
    }
}
```

**이유**: Scene이 분리되어도 매니저는 DontDestroyOnLoad로 유지되므로 문제없음

### 3. Scene 특정 요소와 공통 요소 구분 ✅

폴더 구조로 명확히 구분:

```
Assets/Script/
├── Combat/          (CombatScene 전용)
│   ├── CombatManager.cs
│   ├── CombatUI.cs
│   └── ...
├── WorldMap/        (WorldMapScene 전용)
│   ├── WorldMapController.cs
│   └── ...
├── Town/            (TownScene 전용)
│   └── ...
├── Common/          (모든 Scene에서 사용)
│   ├── UI/
│   │   ├── InventoryUI.cs
│   │   ├── DialogueUI.cs
│   │   └── ...
│   └── ...
└── Managers/        (DontDestroyOnLoad 싱글톤)
    ├── ItemManager.cs
    ├── CharacterManager.cs
    ├── GameManager.cs
    └── ...
```

### 4. 하드코딩된 GameObject.Find 지양 ✅

```csharp
// ❌ 나쁜 예: 이름으로 검색 (Scene 분리 시 찾지 못할 수 있음)
GameObject player = GameObject.Find("Player");

// ✅ 좋은 예: 싱글톤 또는 타입 기반 검색
PlayerController player = PlayerController.Instance;

// ✅ 또는
PlayerController player = FindFirstObjectByType<PlayerController>();
```

### 5. Scene 전환 이벤트 대비: 정리 로직 구현 ✅

```csharp
public class CombatUI : MonoBehaviour
{
    private void OnEnable()
    {
        // 이벤트 구독
        CombatManager.Instance.OnTurnStart += HandleTurnStart;
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제 (Scene 언로드 시 자동 호출됨)
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnTurnStart -= HandleTurnStart;
        }
    }
    
    private void OnDestroy()
    {
        // 추가 정리 작업
        StopAllCoroutines();
    }
}
```

### 6. Prefab 단위로 구성 ✅

- UI 요소들을 Prefab으로 미리 구성
- 나중에 Scene에 배치할 때 Prefab을 드래그하면 끝
- Scene 간 일관성 유지

---

## 마이그레이션 계획

### 작업 단계

#### [현재 단계] 프로토타이핑
1. ✅ 전투 시스템 구현 (완료)
2. 🔄 아이템/인벤토리 시스템 구현 (진행 중)
3. ⏳ 스탯 시스템 구현
4. ⏳ 장비 시스템 구현
5. ⏳ 각 시스템 단위 테스트

#### [중간 단계] 통합 테스트
6. 시스템 간 통합 테스트
7. 전투-아이템 연동 테스트
8. UI 흐름 테스트

#### [Scene 구조화 단계] 마이그레이션
9. Scene 구조 설계 재검토 (실제 요구사항 반영)
10. Scene 파일 생성 및 기본 구성
11. GameObject/Prefab을 적절한 Scene에 배치
12. SceneLoader 구현
13. Scene 전환 로직 구현
14. CommonUIScene 구성 및 Additive 로딩 구현
15. 통합 테스트 및 디버깅

### 마이그레이션 체크리스트

```markdown
## Scene 구조 마이그레이션 체크리스트

### 사전 준비
- [ ] 모든 핵심 시스템 구현 완료
- [ ] 단일 Scene에서 기능 검증 완료
- [ ] 현재 프로젝트 백업 완료

### Scene 생성
- [ ] PersistentScene 생성
- [ ] MainMenuScene 생성
- [ ] WorldMapScene 생성
- [ ] TownScene 생성
- [ ] CombatScene 생성
- [ ] CommonUIScene 생성
- [ ] Build Settings에 모든 Scene 추가

### 매니저 배치 (PersistentScene)
- [ ] CharacterManager
- [ ] ItemManager
- [ ] GameManager
- [ ] AudioManager
- [ ] SaveLoadManager
- [ ] SceneLoader (새로 구현)
- [ ] DialogueManager

### UI 배치 (CommonUIScene)
- [ ] InventoryUI
- [ ] DialogueUI
- [ ] SystemMenuUI
- [ ] HUD (체력바, 골드 등)
- [ ] 알림/팝업 UI

### Scene별 컨텐츠 배치
- [ ] CombatScene: 전투 환경, 전투 UI
- [ ] WorldMapScene: 월드맵 UI
- [ ] TownScene: 마을 환경, NPC

### SceneLoader 구현
- [ ] LoadCombatScene() 구현
- [ ] LoadWorldMapScene() 구현
- [ ] LoadTownScene() 구현
- [ ] 로딩 화면 구현
- [ ] Scene 전환 중 입력 차단 구현

### 테스트
- [ ] Scene 전환 테스트
- [ ] 데이터 유지 테스트 (인벤토리, 스탯 등)
- [ ] UI 유지 테스트 (CommonUIScene)
- [ ] 메모리 누수 테스트
- [ ] 전체 게임 플로우 테스트

### 최적화
- [ ] Scene 로딩 시간 측정
- [ ] 불필요한 리소스 제거
- [ ] Build 크기 확인
```

---

## 참고 코드 예시

### SceneLoader 구현 예시

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Scene 전환을 관리하는 싱글톤 매니저
/// PersistentScene에 배치
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    
    [Header("Scene 이름")]
    private const string SCENE_PERSISTENT = "PersistentScene";
    private const string SCENE_MAIN_MENU = "MainMenuScene";
    private const string SCENE_WORLD_MAP = "WorldMapScene";
    private const string SCENE_TOWN = "TownScene";
    private const string SCENE_COMBAT = "CombatScene";
    private const string SCENE_COMMON_UI = "CommonUIScene";
    
    private bool isLoading = false;
    private string currentMainScene = "";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 게임 시작 시 CommonUIScene 로드
        if (!SceneManager.GetSceneByName(SCENE_COMMON_UI).isLoaded)
        {
            SceneManager.LoadScene(SCENE_COMMON_UI, LoadSceneMode.Additive);
        }
    }
    
    /// <summary>
    /// 메인 메뉴로 이동
    /// </summary>
    public void LoadMainMenu()
    {
        if (isLoading) return;
        StartCoroutine(LoadMainMenuCoroutine());
    }
    
    private IEnumerator LoadMainMenuCoroutine()
    {
        isLoading = true;
        
        // 로딩 화면 표시
        // LoadingUI.Show();
        
        // 현재 메인 Scene 언로드
        if (!string.IsNullOrEmpty(currentMainScene))
        {
            yield return SceneManager.UnloadSceneAsync(currentMainScene);
        }
        
        // 메인 메뉴 Scene 로드
        yield return SceneManager.LoadSceneAsync(SCENE_MAIN_MENU, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SCENE_MAIN_MENU));
        currentMainScene = SCENE_MAIN_MENU;
        
        // 리소스 정리
        yield return Resources.UnloadUnusedAssets();
        
        // 로딩 화면 숨김
        // LoadingUI.Hide();
        
        isLoading = false;
    }
    
    /// <summary>
    /// 월드맵으로 이동
    /// </summary>
    public void LoadWorldMap()
    {
        if (isLoading) return;
        StartCoroutine(LoadWorldMapCoroutine());
    }
    
    private IEnumerator LoadWorldMapCoroutine()
    {
        isLoading = true;
        
        // 현재 메인 Scene 언로드
        if (!string.IsNullOrEmpty(currentMainScene))
        {
            yield return SceneManager.UnloadSceneAsync(currentMainScene);
        }
        
        // 월드맵 Scene 로드
        yield return SceneManager.LoadSceneAsync(SCENE_WORLD_MAP, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SCENE_WORLD_MAP));
        currentMainScene = SCENE_WORLD_MAP;
        
        yield return Resources.UnloadUnusedAssets();
        
        isLoading = false;
    }
    
    /// <summary>
    /// 전투 Scene으로 이동
    /// </summary>
    /// <param name="enemyId">전투할 적 ID</param>
    public void LoadCombatScene(string enemyId)
    {
        if (isLoading) return;
        StartCoroutine(LoadCombatSceneCoroutine(enemyId));
    }
    
    private IEnumerator LoadCombatSceneCoroutine(string enemyId)
    {
        isLoading = true;
        
        // 로딩 화면 표시
        // LoadingUI.Show();
        
        // 현재 메인 Scene 언로드
        if (!string.IsNullOrEmpty(currentMainScene))
        {
            yield return SceneManager.UnloadSceneAsync(currentMainScene);
        }
        
        // 전투 Scene 로드
        yield return SceneManager.LoadSceneAsync(SCENE_COMBAT, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SCENE_COMBAT));
        currentMainScene = SCENE_COMBAT;
        
        // 전투 초기화
        if (CombatManager.Instance != null)
        {
            // CombatManager.Instance.StartBattle(enemyId);
        }
        
        yield return Resources.UnloadUnusedAssets();
        
        // 로딩 화면 숨김
        // LoadingUI.Hide();
        
        isLoading = false;
    }
    
    /// <summary>
    /// 마을로 이동
    /// </summary>
    public void LoadTown()
    {
        if (isLoading) return;
        StartCoroutine(LoadTownCoroutine());
    }
    
    private IEnumerator LoadTownCoroutine()
    {
        isLoading = true;
        
        // 현재 메인 Scene 언로드
        if (!string.IsNullOrEmpty(currentMainScene))
        {
            yield return SceneManager.UnloadSceneAsync(currentMainScene);
        }
        
        // 마을 Scene 로드
        yield return SceneManager.LoadSceneAsync(SCENE_TOWN, LoadSceneMode.Additive);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SCENE_TOWN));
        currentMainScene = SCENE_TOWN;
        
        yield return Resources.UnloadUnusedAssets();
        
        isLoading = false;
    }
    
    /// <summary>
    /// 현재 로딩 중인지 확인
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }
}
```

### Scene 전환 중 입력 차단 예시

```csharp
using UnityEngine;

/// <summary>
/// Scene 전환 중 입력을 차단하는 컴포넌트
/// </summary>
public class InputBlocker : MonoBehaviour
{
    private void Update()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading())
        {
            // 모든 입력 차단
            return;
        }
        
        // 정상적인 입력 처리
        HandleInput();
    }
    
    private void HandleInput()
    {
        // 입력 처리 로직
    }
}
```

### 로딩 화면 UI 예시

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로딩 화면 UI (CommonUIScene에 배치)
/// </summary>
public class LoadingUI : MonoBehaviour
{
    public static LoadingUI Instance { get; private set; }
    
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text loadingText;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        Hide();
    }
    
    public void Show()
    {
        loadingPanel.SetActive(true);
        SetProgress(0f);
    }
    
    public void Hide()
    {
        loadingPanel.SetActive(false);
    }
    
    public void SetProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        
        if (loadingText != null)
        {
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
        }
    }
}
```

---

## 추가 참고 자료

### Unity 공식 문서
- [SceneManager API](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html)
- [Additive Scene Loading](https://docs.unity3d.com/Manual/MultiSceneEditing.html)

### 프로젝트 내 관련 문서
- `Docs/아키텍처.md` - 전체 시스템 아키텍처
- `Docs/코딩-컨벤션.md` - 코딩 규칙

---

## 변경 이력

| 날짜 | 작성자 | 변경 내용 |
|------|--------|-----------|
| 2025-10-22 | AI Assistant | 최초 작성 |

---

## 문의 및 피드백

Scene 구조 관련 문의사항이나 개선 제안은 팀 회의에서 논의합니다.

**중요**: 이 문서는 향후 구현 시 참고용이며, 현재는 단일 Scene 프로토타이핑에 집중합니다.

