# 세션 요약 - 검술 선택 UI 구현 (2025-01-06)

## 프로젝트 현재 상태

### Git 복구 상황
- 19일 전 (8월 29일) Git 커밋으로 CombatManager 복구 완료
- 인코딩 문제로 인해 3주간의 개발 작업 손실
- 현재 DevJournal 8월 26일부터의 개발 내용을 기반으로 재구현 진행 중

### 구현 완료된 핵심 시스템
1. **CharacterManager 시스템**
   - `CharacterData.cs`: 캐릭터 스탯 관리 (HP, ATK, DR, Crit, Poise 등)
   - `CharacterManager.cs`: 싱글톤으로 플레이어/에너미 데이터 관리
   - `CharacterStatsData.cs`: ScriptableObject로 기본 스탯 정의

2. **전투 시스템**
   - `CombatManager.cs`: 전투 로직, 턴 관리, 피해 계산
   - `Combatant.cs`: 전투 참가자 기본 클래스
   - `PlayerCombatant.cs`, `EnemyCombatant.cs`: 플레이어/에너미 전투 참가자
   - Poise 시스템: 방어력 기반 인터럽트 메커니즘

3. **UI 시스템**
   - `CombatStatusDisplay.cs`: 실시간 전투 스탯 표시
   - 이벤트 기반 UI 업데이트 (OnStatsChanged, OnHPChanged, OnPoiseChanged)

## 현재 진행 중인 작업: 검술 선택 UI

### 구현해야 할 파일들
1. **ActionCommandSelectionManager.cs** - 검술 선택 관리 싱글톤
2. **PlayerActionSelectUI.cs** - 플레이어용 검술 선택 UI
3. **EnemyActionSelectUI.cs** - 에너미용 검술 선택 UI
4. **ActionButton.cs** - 개별 검술 버튼 컴포넌트

### 핵심 구현 스펙

#### ActionCommandSelectionManager
```csharp
public class ActionCommandSelectionManager : MonoBehaviour
{
    public static ActionCommandSelectionManager Instance { get; private set; }
    
    [Header("UI References")]
    public PlayerActionSelectUI playerActionSelectUI;
    public EnemyActionSelectUI enemyActionSelectUI;
    
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
    
    public void InitializeUI()
    {
        // UI 초기화 로직
    }
    
    public static ActionCommandSelectionManager EnsureInstance()
    {
        // 자동 생성 로직
    }
}
```

#### PlayerActionSelectUI
```csharp
public class PlayerActionSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform actionButtonContainer;
    public GameObject actionButtonPrefab;
    public int maxButtons = 5;
    
    [Header("Player Reference")]
    public PlayerController playerController;
    
    private List<ActionButton> actionButtons = new List<ActionButton>();
    private int selectedIndex = 0;
    
    private void Start()
    {
        Initialize();
        SetupInput();
    }
    
    private void Initialize()
    {
        CreateActionButtons();
        SetInitialFocus();
    }
    
    private void CreateActionButtons()
    {
        // 플레이어의 검술 데이터를 참조하여 버튼 생성
        // 버튼 텍스트에 검술 이름 표시
        // 순서대로 생성하여 0번 인덱스가 맨 위
    }
    
    private void SetupInput()
    {
        // InputSystem "ActionSelect" 액션 구독
        // 위/아래 화살표로 포커스 이동
        // 엔터키로 선택 확인
    }
    
    private void ConfirmSelection()
    {
        // 선택된 검술을 PlayerController에 전달
        playerController.SetSelectedCommandIndex(selectedIndex);
    }
}
```

#### EnemyActionSelectUI
```csharp
public class EnemyActionSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform actionButtonContainer;
    public GameObject actionButtonPrefab;
    
    private List<ActionButton> actionButtons = new List<ActionButton>();
    
    private void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        CreateActionButtons();
        DisableButtonInteraction();
    }
    
    private void CreateActionButtons()
    {
        // 에너미의 검술 데이터를 참조하여 버튼 생성
        // 버튼은 비활성화 (interactable = false)
    }
    
    private void DisableButtonInteraction()
    {
        // 모든 버튼의 상호작용 비활성화
    }
    
    public void SimulateAISelection()
    {
        // AI 자동 선택 시뮬레이션
    }
}
```

#### ActionButton
```csharp
public class ActionButton : MonoBehaviour
{
    [Header("UI References")]
    public Button button;
    public TextMeshProUGUI buttonText;
    
    private ActionCommandData commandData;
    private int buttonIndex;
    
    public void Initialize(ActionCommandData data, int index)
    {
        commandData = data;
        buttonIndex = index;
        buttonText.text = data.commandName;
    }
    
    public void OnButtonClicked()
    {
        // 버튼 클릭 시 선택 이벤트 발생
    }
}
```

### Unity 설정 요구사항

#### InputSystem 설정
- `InputSystem_Actions.inputactions` 파일에 "ActionSelect" 액션 추가
- 키보드 바인딩: W/S 또는 Up/Down 화살표
- 네이티브 플랫폼 지원

#### Scene 설정
1. **UI 오브젝트 생성**
   - Canvas 하위에 검술 선택 UI 패널 생성
   - PlayerActionSelectUI, EnemyActionSelectUI 컴포넌트 연결
   - ActionButtonContainer Transform 설정

2. **프리팹 생성**
   - ActionButton 프리팹 생성
   - Button, TextMeshProUGUI 컴포넌트 포함

3. **컴포넌트 연결**
   - ActionCommandSelectionManager 인스턴스 생성
   - PlayerController, EnemyController 참조 연결

### 구현 순서
1. ActionCommandSelectionManager.cs 생성
2. PlayerActionSelectUI.cs 생성
3. EnemyActionSelectUI.cs 생성
4. ActionButton.cs 생성
5. Unity Editor에서 InputSystem 액션 추가
6. Scene에서 UI 오브젝트 생성 및 연결
7. 테스트 및 디버깅

### 중요한 기술적 결정사항
- **싱글톤 패턴**: ActionCommandSelectionManager는 전역 관리
- **이벤트 기반**: UI 업데이트는 이벤트 구독 방식
- **자동 생성**: EnsureInstance() 메서드로 매니저 자동 생성
- **비활성화**: Enemy 버튼은 상호작용 비활성화
- **포커스 관리**: 키보드 네비게이션으로 버튼 간 이동

### 현재 문제점
- 파일들이 삭제되어 재구현 필요
- InputSystem에 "ActionSelect" 액션이 없어서 KeyNotFoundException 발생
- UI 오브젝트들이 Scene에 연결되지 않음

### 다음 단계
1. 위의 스펙에 따라 4개 파일 재생성
2. Unity Editor에서 InputSystem 설정
3. Scene에서 UI 오브젝트 생성 및 연결
4. 통합 테스트 진행

이 요약을 바탕으로 새 Agent가 검술 선택 UI를 완전히 재구현할 수 있습니다.
