# CharacterManager 분리 구현 계획서

**작성일**: 2025-11-03  
**목적**: 기존 CharacterManager를 3개의 매니저로 분리하는 구체적인 구현 계획  
**관련 문서**: `Scene_계층_구조_설계.md`

---

## 📋 목차

1. [개요](#개요)
2. [분리 전후 비교](#분리-전후-비교)
3. [단계별 구현 계획](#단계별-구현-계획)
4. [새 매니저 클래스 명세](#새-매니저-클래스-명세)
5. [기존 코드 마이그레이션 가이드](#기존-코드-마이그레이션-가이드)
6. [테스트 계획](#테스트-계획)

---

## 개요

### 배경

기존 `CharacterManager`는 다음과 같은 문제점을 가지고 있습니다:

1. **역할 혼재**: 영속 데이터(Player)와 임시 데이터(Enemy)를 동시에 관리
2. **확장성 부족**: Enemy가 1개만 존재, 다중 적 전투 대응 불가
3. **생명주기 불일치**: Player는 게임 내내 유지, Enemy는 전투마다 교체 필요
4. **저장/로드 복잡성**: 불필요한 Enemy 데이터까지 저장 고려 필요

### 목표

기존 CharacterManager를 **역할과 생명주기에 따라** 3개의 매니저로 분리:

| 매니저 | 역할 | 생명주기 | 소속 Scene |
|--------|------|----------|-----------|
| **PlayerCharacterManager** | 플레이어 진행도 (영속 데이터) | 게임 시작~종료 | CoreSystemScene |
| **CharacterDatabase** | 캐릭터 템플릿 관리 | 게임 시작~종료 | CoreSystemScene |
| **CombatCharacterManager** | 전투 인스턴스 관리 | 전투 시작~종료 | CombatScene |

---

## 분리 전후 비교

### Before (현재)

```
CharacterManager (DontDestroyOnLoad)
├── PlayerData (ScriptableObject 에셋)
├── EnemyData (ScriptableObject 에셋)
├── PlayerCharacter (런타임 인스턴스)
├── EnemyCharacter (런타임 인스턴스)
└── 메서드:
    ├── InitializeCharacterData()
    ├── InitializeInventory()
    ├── InitializeActions()
    └── ConnectController()
```

### After (목표)

```
PlayerCharacterManager (CoreSystemScene, DontDestroyOnLoad)
├── Level, Experience, Gold
├── Inventory (영속)
├── Equipment (영속)
├── Actions (영속)
└── CreatePlayerCharacterForBattle()
    SyncPlayerStateAfterBattle()

CharacterDatabase (CoreSystemScene, DontDestroyOnLoad)
├── playerTemplateAsset
├── enemyTemplateAssets[]
├── characterRegistry (Dictionary)
└── GetPlayerTemplate()
    CreateEnemy(enemyId)

CombatCharacterManager (CombatScene, Scene 전용)
├── PlayerCharacter (전투 인스턴스)
├── EnemyCharacters[] (전투 인스턴스)
└── InitializeBattle(enemyId)
    FinalizeBattle(result)
    ConnectController()
```

---

## 단계별 구현 계획

### Phase 1: 새 매니저 클래스 생성 (병행 개발)

#### Step 1-1: PlayerCharacterManager 생성

**파일 경로**: `Assets/Script/Manager/PlayerCharacterManager.cs`

**작업 내용:**
1. 싱글톤 패턴 구현
2. DontDestroyOnLoad 적용
3. 플레이어 진행도 데이터 필드 추가
4. CreatePlayerCharacterForBattle() 메서드 구현
5. SyncPlayerStateAfterBattle() 메서드 구현

**참고:**
- 기존 CharacterManager의 Player 관련 코드를 이관
- 인벤토리/장비/검술 초기화 로직 포함

---

#### Step 1-2: CharacterDatabase 생성

**파일 경로**: `Assets/Script/Database/CharacterDatabase.cs`

**작업 내용:**
1. 싱글톤 패턴 구현
2. DontDestroyOnLoad 적용
3. CharacterData 에셋 필드 추가 (Player + Enemy 템플릿)
4. InitializeRegistry() 메서드 구현 (Dictionary 생성)
5. CreateEnemy() 메서드 구현

**참고:**
- 기존 CharacterManager의 Enemy 관련 초기화 코드 이관
- ItemDatabase, ActionCommandDatabase와 유사한 구조

---

#### Step 1-3: CombatCharacterManager 생성

**파일 경로**: `Assets/Script/Combat/CombatCharacterManager.cs`

**작업 내용:**
1. 싱글톤 패턴 구현 (DontDestroyOnLoad ❌)
2. PlayerCharacter, EnemyCharacters 필드 추가
3. InitializeBattle() 메서드 구현
   - PlayerCharacterManager.CreatePlayerCharacterForBattle() 호출
   - CharacterDatabase.CreateEnemy() 호출
4. FinalizeBattle() 메서드 구현
   - PlayerCharacterManager.SyncPlayerStateAfterBattle() 호출
5. ConnectController() 메서드 구현

**참고:**
- 기존 CharacterManager의 ConnectController() 로직 이관
- CombatManager와 밀접하게 연동

---

### Phase 2: CombatManager 수정

#### Step 2-1: 참조 변경

**수정 대상**: `Assets/Script/Combat/CombatManager.cs`

**변경 사항:**
```csharp
// Before
private void ConnectControllers()
{
    CharacterManager.Instance.ConnectController(...);
}

// After
private void ConnectControllers()
{
    CombatCharacterManager.Instance.ConnectController(...);
}
```

**추가 변경:**
- `PlayerCharacter` 조회: `CharacterManager.Instance.PlayerCharacter` → `CombatCharacterManager.Instance.PlayerCharacter`
- `EnemyCharacter` 조회: `CharacterManager.Instance.EnemyCharacter` → `CombatCharacterManager.Instance.CurrentEnemy`

---

#### Step 2-2: 전투 초기화 로직 추가

**CombatManager.Start()** 또는 **StartBattle()** 수정:

```csharp
private void Start()
{
    // 전투 초기화
    CombatCharacterManager.Instance.InitializeBattle("goblin_warrior");
    
    // 기존 로직
    ConnectControllers();
    StartCoroutine(RunCombat());
}
```

---

#### Step 2-3: 전투 종료 로직 추가

**전투 종료 시**:

```csharp
private void EndBattle(BattleResult result)
{
    // 전투 결과 처리
    CombatCharacterManager.Instance.FinalizeBattle(result);
    
    // Scene 전환 등
    // ...
}
```

---

### Phase 3: 기존 CharacterManager 제거

#### Step 3-1: 모든 참조 확인

**검색할 패턴:**
```
CharacterManager.Instance
```

**확인 파일:**
- CombatManager.cs
- PlayerController.cs
- EnemyController.cs
- UI 스크립트들 (InventoryUI, ActionCommandEquipUI 등)

**변경 방침:**
- Player 진행도 관련 → `PlayerCharacterManager.Instance`
- 전투 중 Character 조회 → `CombatCharacterManager.Instance`
- 캐릭터 템플릿 조회 → `CharacterDatabase.Instance`

---

#### Step 3-2: CharacterManager 삭제

모든 참조가 제거되면:
1. `Assets/Script/CharacterManager.cs` 파일 삭제
2. ProtoType Scene에서 CharacterManager GameObject 삭제
3. 컴파일 에러 확인

---

### Phase 4: ProtoType Scene 구성

#### Step 4-1: CoreSystemScene 구성 (향후)

**현재는 ProtoType Scene에 배치:**
```
ProtoType Scene (Hierarchy)
├── [Managers - Persistent]
│   ├── PlayerCharacterManager 🆕
│   └── CharacterDatabase 🆕
├── [Managers - Combat]
│   ├── CombatCharacterManager 🆕
│   └── CombatManager
└── ...
```

**향후 Scene 분리 시:**
- PlayerCharacterManager, CharacterDatabase → CoreSystemScene으로 이동
- CombatCharacterManager → CombatScene으로 이동

---

## 새 매니저 클래스 명세

### 1. PlayerCharacterManager

```csharp
public class PlayerCharacterManager : MonoBehaviour
{
    public static PlayerCharacterManager Instance { get; private set; }
    
    // === 진행도 데이터 ===
    [Header("플레이어 진행도")]
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; } = 0;
    public int Gold { get; private set; } = 100;
    
    // === 영속 데이터 ===
    [Header("영속 데이터")]
    public CharacterInventory Inventory { get; private set; }
    // EquipmentState, ActionCommandState는 Character 내부에서 관리
    
    // === 템플릿 참조 ===
    private CharacterData playerTemplate;
    
    // === 초기화 ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            InitializePlayerData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePlayerData()
    {
        // 1. CharacterDatabase에서 Player 템플릿 가져오기
        playerTemplate = CharacterDatabase.Instance.GetPlayerTemplate();
        
        // 2. 인벤토리 생성 (영속)
        Inventory = new CharacterInventory(playerTemplate.initialAccessorySlots);
        
        // 3. 초기 아이템 추가
        InitializeInventory();
        
        Debug.Log("[PlayerCharacterManager] 플레이어 데이터 초기화 완료");
    }
    
    private void InitializeInventory()
    {
        // 기존 CharacterManager.InitializeInventory() 로직
        // ...
    }
    
    // === 전투용 인스턴스 생성 ===
    public PlayerCharacter CreatePlayerCharacterForBattle()
    {
        // 1. 템플릿 복사
        CharacterData battleData = Instantiate(playerTemplate);
        battleData.InstantiateBehaviorTrees();
        
        // 2. PlayerCharacter 생성
        var player = new PlayerCharacter(battleData, null);
        
        // 3. 영속 데이터 적용
        player.Inventory = this.Inventory; // 참조 공유
        player.SetLevel(this.Level);
        
        // 4. 장비/검술 복원
        // (현재는 Inventory에서 관리되므로 자동 반영)
        
        Debug.Log($"[PlayerCharacterManager] 전투용 Player 생성: Lv.{Level}, 골드 {Gold}");
        return player;
    }
    
    // === 전투 후 동기화 ===
    public void SyncPlayerStateAfterBattle(PlayerCharacter battleInstance)
    {
        // 전투 중 변경된 인벤토리는 이미 참조 공유로 반영됨
        // (필요시 추가 동기화 로직)
        
        Debug.Log("[PlayerCharacterManager] 전투 후 플레이어 상태 동기화 완료");
    }
    
    // === 진행도 관리 ===
    public void AddGold(int amount)
    {
        Gold += amount;
        Debug.Log($"[PlayerCharacterManager] 골드 획득: +{amount} (현재: {Gold})");
    }
    
    public void AddExperience(int amount)
    {
        Experience += amount;
        Debug.Log($"[PlayerCharacterManager] 경험치 획득: +{amount} (현재: {Experience})");
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        int requiredExp = Level * 100; // 임시 공식
        if (Experience >= requiredExp)
        {
            Level++;
            Experience -= requiredExp;
            Debug.Log($"[PlayerCharacterManager] 레벨업! Lv.{Level}");
        }
    }
}
```

---

### 2. CharacterDatabase

```csharp
public class CharacterDatabase : MonoBehaviour
{
    public static CharacterDatabase Instance { get; private set; }
    
    // === 템플릿 에셋 ===
    [Header("캐릭터 템플릿 에셋")]
    [SerializeField] private CharacterData playerTemplateAsset;
    [SerializeField] private List<CharacterData> enemyTemplateAssets;
    
    // === 런타임 레지스트리 ===
    private Dictionary<string, CharacterData> characterRegistry;
    
    // === 초기화 ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            InitializeRegistry();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeRegistry()
    {
        characterRegistry = new Dictionary<string, CharacterData>();
        
        // Enemy 템플릿 등록
        foreach (var enemyData in enemyTemplateAssets)
        {
            if (enemyData != null && !string.IsNullOrEmpty(enemyData.characterId))
            {
                characterRegistry[enemyData.characterId] = enemyData;
                Debug.Log($"[CharacterDatabase] Enemy 등록: {enemyData.characterId}");
            }
        }
        
        Debug.Log($"[CharacterDatabase] 레지스트리 초기화 완료: {characterRegistry.Count}개 캐릭터");
    }
    
    // === 템플릿 조회 ===
    public CharacterData GetPlayerTemplate()
    {
        if (playerTemplateAsset == null)
        {
            Debug.LogError("[CharacterDatabase] playerTemplateAsset이 할당되지 않았습니다!");
            return null;
        }
        return playerTemplateAsset;
    }
    
    public CharacterData GetCharacterTemplate(string characterId)
    {
        if (characterRegistry.TryGetValue(characterId, out CharacterData template))
        {
            return template;
        }
        Debug.LogError($"[CharacterDatabase] 캐릭터 '{characterId}'를 찾을 수 없습니다!");
        return null;
    }
    
    // === Enemy 인스턴스 생성 ===
    public EnemyCharacter CreateEnemy(string enemyId)
    {
        // 1. 템플릿 조회
        var template = GetCharacterTemplate(enemyId);
        if (template == null)
            return null;
        
        // 2. 템플릿 복사 (독립적인 인스턴스)
        CharacterData enemyData = Instantiate(template);
        enemyData.InstantiateBehaviorTrees();
        
        // 3. EnemyCharacter 생성
        var enemy = new EnemyCharacter(enemyData, null);
        
        // 4. 초기화 (인벤토리, 장비, 검술)
        InitializeEnemyDefaults(enemy, enemyData);
        
        Debug.Log($"[CharacterDatabase] Enemy 생성: {enemy.Name}");
        return enemy;
    }
    
    private void InitializeEnemyDefaults(EnemyCharacter enemy, CharacterData data)
    {
        // 기존 CharacterManager.InitializeInventory() 로직
        // 기존 CharacterManager.InitializeActions() 로직
        // ...
    }
}
```

---

### 3. CombatCharacterManager

```csharp
public class CombatCharacterManager : MonoBehaviour
{
    public static CombatCharacterManager Instance { get; private set; }
    
    // === 전투 중인 캐릭터들 ===
    public PlayerCharacter PlayerCharacter { get; private set; }
    public List<EnemyCharacter> EnemyCharacters { get; private set; }
    
    // === 편의 프로퍼티 ===
    public EnemyCharacter CurrentEnemy => EnemyCharacters?.Count > 0 ? EnemyCharacters[0] : null;
    
    // === 초기화 ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ⚠️ DontDestroyOnLoad 적용 안함 (Scene 전용)
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // === 전투 초기화 ===
    public void InitializeBattle(string enemyId)
    {
        Debug.Log($"[CombatCharacterManager] 전투 초기화: vs {enemyId}");
        
        // 1. 플레이어 생성
        PlayerCharacter = PlayerCharacterManager.Instance.CreatePlayerCharacterForBattle();
        
        // 2. 적 생성
        EnemyCharacters = new List<EnemyCharacter>();
        var enemy = CharacterDatabase.Instance.CreateEnemy(enemyId);
        if (enemy != null)
        {
            EnemyCharacters.Add(enemy);
        }
        
        Debug.Log($"[CombatCharacterManager] 전투 참여자: {PlayerCharacter.Name} vs {enemy?.Name}");
    }
    
    // === 다중 적 전투 (향후) ===
    public void InitializeBattle(List<string> enemyIds)
    {
        Debug.Log($"[CombatCharacterManager] 다중 적 전투 초기화: {enemyIds.Count}명");
        
        PlayerCharacter = PlayerCharacterManager.Instance.CreatePlayerCharacterForBattle();
        
        EnemyCharacters = new List<EnemyCharacter>();
        foreach (var enemyId in enemyIds)
        {
            var enemy = CharacterDatabase.Instance.CreateEnemy(enemyId);
            if (enemy != null)
            {
                EnemyCharacters.Add(enemy);
            }
        }
    }
    
    // === 전투 종료 ===
    public void FinalizeBattle(BattleResult result)
    {
        Debug.Log($"[CombatCharacterManager] 전투 종료: {(result.isVictory ? "승리" : "패배")}");
        
        // 1. 플레이어 상태 저장
        PlayerCharacterManager.Instance.SyncPlayerStateAfterBattle(PlayerCharacter);
        
        // 2. 보상 적용
        if (result.isVictory)
        {
            PlayerCharacterManager.Instance.AddGold(result.goldReward);
            PlayerCharacterManager.Instance.AddExperience(result.expReward);
        }
        
        // 3. 인스턴스들은 Scene 언로드 시 자동 파괴됨
        Debug.Log("[CombatCharacterManager] 전투 후처리 완료");
    }
    
    // === Controller 연결 ===
    public void ConnectController(CharacterType type, ICombatController controller)
    {
        if (type == CharacterType.Player)
        {
            if (PlayerCharacter is PlayerCharacter playerChar)
            {
                playerChar.SetController(controller as PlayerController);
                Debug.Log($"[CombatCharacterManager] PlayerController 연결: {PlayerCharacter.Name}");
            }
        }
        else if (type == CharacterType.Enemy)
        {
            if (CurrentEnemy is EnemyCharacter enemyChar)
            {
                enemyChar.SetController(controller as EnemyController);
                Debug.Log($"[CombatCharacterManager] EnemyController 연결: {CurrentEnemy.Name}");
            }
        }
    }
}
```

---

## 기존 코드 마이그레이션 가이드

### 코드 이동 매핑

| 기존 위치 (CharacterManager) | 새 위치 | 비고 |
|------------------------------|---------|------|
| `PlayerData` | `PlayerCharacterManager.playerTemplate` | private |
| `EnemyData` | `CharacterDatabase.characterRegistry` | Dictionary |
| `PlayerCharacter` | `CombatCharacterManager.PlayerCharacter` | 전투 인스턴스 |
| `EnemyCharacter` | `CombatCharacterManager.CurrentEnemy` | 전투 인스턴스 |
| `InitializeCharacterData()` | 분산: 각 매니저의 `Awake()` | |
| `InitializeInventory(Player)` | `PlayerCharacterManager.InitializeInventory()` | |
| `InitializeInventory(Enemy)` | `CharacterDatabase.InitializeEnemyDefaults()` | |
| `InitializeActions()` | 각 매니저로 분산 | |
| `ConnectController()` | `CombatCharacterManager.ConnectController()` | |

### 참조 변경 패턴

```csharp
// === Player 관련 ===

// Before
CharacterManager.Instance.PlayerCharacter

// After
CombatCharacterManager.Instance.PlayerCharacter


// === Enemy 관련 ===

// Before
CharacterManager.Instance.EnemyCharacter

// After
CombatCharacterManager.Instance.CurrentEnemy


// === Player 진행도 ===

// Before (기존에는 암묵적으로 CharacterManager가 관리)

// After
PlayerCharacterManager.Instance.Gold
PlayerCharacterManager.Instance.Level
```

---

## 테스트 계획

### Unit Test (개별 매니저)

#### Test 1: PlayerCharacterManager

```csharp
[Test]
public void PlayerCharacterManager_CreatePlayerForBattle_ReturnsValidInstance()
{
    // Arrange
    var manager = PlayerCharacterManager.Instance;
    
    // Act
    var player = manager.CreatePlayerCharacterForBattle();
    
    // Assert
    Assert.IsNotNull(player);
    Assert.AreEqual(manager.Level, player.Level);
    Assert.AreEqual(manager.Inventory, player.Inventory);
}
```

#### Test 2: CharacterDatabase

```csharp
[Test]
public void CharacterDatabase_CreateEnemy_ReturnsValidInstance()
{
    // Arrange
    var db = CharacterDatabase.Instance;
    
    // Act
    var enemy = db.CreateEnemy("goblin_warrior");
    
    // Assert
    Assert.IsNotNull(enemy);
    Assert.AreEqual("Goblin Warrior", enemy.Name);
}
```

#### Test 3: CombatCharacterManager

```csharp
[Test]
public void CombatCharacterManager_InitializeBattle_CreatesBothCharacters()
{
    // Arrange
    var manager = CombatCharacterManager.Instance;
    
    // Act
    manager.InitializeBattle("goblin_warrior");
    
    // Assert
    Assert.IsNotNull(manager.PlayerCharacter);
    Assert.IsNotNull(manager.CurrentEnemy);
}
```

---

### Integration Test (통합)

#### Test 4: 전투 흐름 전체

```csharp
[Test]
public void CombatFlow_StartToEnd_DataPersists()
{
    // Arrange
    var initialGold = PlayerCharacterManager.Instance.Gold;
    
    // Act
    CombatCharacterManager.Instance.InitializeBattle("goblin_warrior");
    // ... 전투 진행 ...
    var result = new BattleResult { isVictory = true, goldReward = 50 };
    CombatCharacterManager.Instance.FinalizeBattle(result);
    
    // Assert
    Assert.AreEqual(initialGold + 50, PlayerCharacterManager.Instance.Gold);
}
```

---

### Manual Test (수동 테스트)

1. **ProtoType Scene 실행**
   - [ ] 게임 시작 시 3개 매니저 모두 초기화되는지 확인
   - [ ] 콘솔 로그에 에러 없는지 확인

2. **전투 시작**
   - [ ] PlayerCharacter가 올바른 레벨/장비로 생성되는지 확인
   - [ ] EnemyCharacter가 올바르게 생성되는지 확인

3. **전투 진행**
   - [ ] 기존과 동일하게 전투가 진행되는지 확인
   - [ ] Controller 연결이 정상적으로 작동하는지 확인

4. **전투 종료**
   - [ ] 보상(골드, 경험치)이 정상적으로 반영되는지 확인
   - [ ] 다음 전투 시작 시 이전 진행도가 유지되는지 확인

---

## 완료 체크리스트

### Phase 1: 새 매니저 생성
- [ ] PlayerCharacterManager.cs 작성
- [ ] CharacterDatabase.cs 작성
- [ ] CombatCharacterManager.cs 작성
- [ ] ProtoType Scene에 3개 매니저 GameObject 배치
- [ ] Inspector에서 필수 필드 할당

### Phase 2: CombatManager 수정
- [ ] CombatManager.cs 참조 변경
- [ ] 전투 초기화 로직 추가
- [ ] 전투 종료 로직 추가
- [ ] 컴파일 에러 해결

### Phase 3: 기존 CharacterManager 제거
- [ ] 모든 참조 검색 및 변경
- [ ] CharacterManager.cs 파일 삭제
- [ ] ProtoType Scene에서 CharacterManager GameObject 제거
- [ ] 최종 컴파일 확인

### Phase 4: 테스트
- [ ] Unit Test 작성 및 통과
- [ ] Integration Test 통과
- [ ] Manual Test 체크리스트 완료
- [ ] 전투 흐름 전체 검증

---

## 예상 작업 시간

| Phase | 작업 | 예상 시간 |
|-------|------|----------|
| 1 | 새 매니저 생성 | 3~4시간 |
| 2 | CombatManager 수정 | 1~2시간 |
| 3 | 기존 코드 제거 | 1시간 |
| 4 | 테스트 및 검증 | 2~3시간 |
| **합계** | | **7~10시간** |

---

## 참고 문서

- `Scene_계층_구조_설계.md` - Scene 및 매니저 아키텍처 전체 구조
- `아키텍처.md` - 프로젝트 전체 아키텍처
- `코딩-컨벤션.md` - 코딩 규칙

---

**작성자**: AI Assistant  
**최종 검토**: (검토 후 기입)




