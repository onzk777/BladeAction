using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; } // CombatManager의 싱글톤 인스턴스
    
    // === 다음 전투 설정 (Scene 전환 전 저장) ===
    private static List<string> pendingTeamAIds = new List<string> { "Player" };
    private static List<string> pendingTeamBIds = new List<string> { "Test_Enemy1" };
    private static string pendingPlayerId = "Player"; // 기본값 (역호환용)
    private static string pendingEnemyId = "Test_Enemy1"; // 기본값 (역호환용)
    
    /// <summary>
    /// 다음 전투의 참가자를 설정합니다 (Scene 전환 전 호출)
    /// TestScene 등에서 전투 시작 전에 호출
    /// </summary>
    public static void SetupNextBattle(string playerId, string enemyId)
    {
        SetupNextBattle(new[] { playerId }, new[] { enemyId });
    }

    public static void SetupNextBattle(IList<string> teamAIds, IList<string> teamBIds)
    {
        pendingTeamAIds = teamAIds != null ? new List<string>(teamAIds) : new List<string>();
        pendingTeamBIds = teamBIds != null ? new List<string>(teamBIds) : new List<string>();

        pendingPlayerId = pendingTeamAIds.Count > 0 ? pendingTeamAIds[0] : "Player";
        pendingEnemyId = pendingTeamBIds.Count > 0 ? pendingTeamBIds[0] : "Test_Enemy1";

        Debug.Log($"[CombatManager-Static] 다음 전투 설정: TeamA[{string.Join(", ", pendingTeamAIds)}] vs TeamB[{string.Join(", ", pendingTeamBIds)}]");
    }

[Header("Actor Prefabs & Spawn Points")]
[SerializeField] private GameObject playerActorPrefab;
[SerializeField] private GameObject npcActorPrefab;
[SerializeField] private Transform teamASpawnPoint;
[SerializeField] private Transform teamBSpawnPoint;
[SerializeField] private Transform actorsRoot;
[SerializeField] private bool flipTeamBActors = true;

    internal PlayerController playerController;
    private AIController enemyController; // TeamB NonPlayer 컨트롤러 인스턴스
    private readonly List<GameObject> spawnedActors = new List<GameObject>();
    private readonly Dictionary<CombatCharacterManager.CombatantSlot, GameObject> spawnedActorMap = new Dictionary<CombatCharacterManager.CombatantSlot, GameObject>();
    private BattleState battle;
    private BattleExecutor battleExecutor;

    internal BattleState ActiveBattle => battle ?? throw new InvalidOperationException("[CombatManager] Battle is not initialized.");
    
    // UI에서 접근할 수 있도록 public 프로퍼티 추가
    public PlayerController PlayerController
    {
        get
        {
            var slot = CombatCharacterManager.Instance?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA);
            if (slot?.Controller is PlayerController playerSlotController)
            {
                return playerSlotController;
            }
            return playerController;
        }
    }
    public AIController NonPlayerController
    {
        get
        {
            var slot = CombatCharacterManager.Instance?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB);
            if (slot?.Controller is AIController aiSlotController)
            {
                return aiSlotController;
            }
            return enemyController;
        }
    }
    public CharacterHitSystem GetCharacterHitSystemForDefender()
    {
        return defenderInputHandler != null ? defenderInputHandler.CharacterHitSystem : null;
    }
    
    internal AttackerInputHandler attackerInputHandler; // 공격자 타이밍 입력 핸들러
    internal DefenderInputHandler defenderInputHandler; // 방어자 타이밍 입력 핸들러
    internal CombatCharacterManager.CombatantSlot currentAttackerSlot
    {
        get => ActiveBattle.CurrentAttackerSlot;
        set => ActiveBattle.CurrentAttackerSlot = value;
    }
    internal CombatCharacterManager.CombatantSlot currentDefenderSlot
    {
        get => ActiveBattle.CurrentDefenderSlot;
        set => ActiveBattle.CurrentDefenderSlot = value;
    }
    internal CombatTurnContext currentTurnContext
    {
        get => ActiveBattle.CurrentTurnContext;
        set => ActiveBattle.CurrentTurnContext = value;
    }
    internal ICombatController currentAttackerController
    {
        get => ActiveBattle.CurrentAttackerController;
        set => ActiveBattle.CurrentAttackerController = value;
    }
    internal ICombatController currentDefenderController
    {
        get => ActiveBattle.CurrentDefenderController;
        set => ActiveBattle.CurrentDefenderController = value;
    }
    internal bool? attackerPerfectInput
    {
        get => ActiveBattle.AttackerPerfectInput;
        set => ActiveBattle.AttackerPerfectInput = value;
    }
    internal bool? defenderPerfectInput
    {
        get => ActiveBattle.DefenderPerfectInput;
        set => ActiveBattle.DefenderPerfectInput = value;
    }
    internal float? attackerInputTime
    {
        get => ActiveBattle.AttackerInputTime;
        set => ActiveBattle.AttackerInputTime = value;
    }
    internal float? defenderInputTime
    {
        get => ActiveBattle.DefenderInputTime;
        set => ActiveBattle.DefenderInputTime = value;
    }
    public bool IsPlayerAttacker => GetCurrentAttackerCharacter() is PlayerCharacter;
    public bool AttackerPerfectInput
    {
        get { return attackerPerfectInput.HasValue ? attackerPerfectInput.Value : false; }
        set { attackerPerfectInput = value; }
    }
    public bool DefenderPerfectInput
    {
        get { return defenderPerfectInput.HasValue ? defenderPerfectInput.Value : false; }
        set { defenderPerfectInput = value; }
    }

    [Header("전역 설정")]
    [SerializeField] internal GlobalConfig globalConfig;
    

    
    // 발사체 발사 상태 추적
    internal bool[] projectileLaunched => ActiveBattle.ProjectileLaunched; // 각 히트별 발사 상태
    
    // 🆕 히트당 판정 한 번만 발생하도록 추적
    internal bool[] hitJudgmentCompleted => ActiveBattle.HitJudgmentCompleted; // 각 히트별 판정 완료 상태
    
    // 🆕 중복 판정 추적을 위한 카운터
    internal int[] hitJudgmentCount => ActiveBattle.HitJudgmentCount; // 각 히트별 판정 발생 횟수
    
    // ❌ 제거: 턴 종료 플래그들 (PerformTurn에서 직접 처리)
    // private bool turnEndRequested = false;
    // private bool isWaitingForTurnEnd = false;
    
    // 🆕 발사체 완료 카운팅
    // ❌ 제거: 발사체 완료 추적 변수 (시간 기반 턴 종료로 변경)
    // private int completedProjectiles = 0;
    // private int totalProjectiles = 0;

    // 현재 턴 지속 시간 (전역 접근 가능)
    public float CurrentTurnDuration
    {
        get => ActiveBattle.CurrentTurnDuration;
        internal set => ActiveBattle.CurrentTurnDuration = value;
    }
    
    // 현재 턴 번호 (BT에서 사용)
    public int CurrentTurnNumber
    {
        get => ActiveBattle.CurrentTurnNumber;
        internal set => ActiveBattle.CurrentTurnNumber = value;
    }
    
    // 공격 턴 여부 (BT에서 사용)
    public bool IsNPCAttackTurn => GetCurrentAttackerCharacter() is EnemyCharacter;
    public bool IsPlayerAttackTurn => IsPlayerAttacker;

    // CharacterManager를 통해 Character 인스턴스 접근

    // 현재 히트 컨텍스트 전역화
    public int CurrentHit
    {
        get => ActiveBattle.CurrentHit;
        internal set => ActiveBattle.CurrentHit = value;
    } // 현재 히트 인덱스. (연타 공격일 경우 체크용)
    public bool CurrentAttackResultShown
    {
        get => ActiveBattle.CurrentAttackResultShown;
        internal set => ActiveBattle.CurrentAttackResultShown = value;
    } // 히트 결과가 표시되었는지 여부
    public bool CurrentDefenseResultShown
    {
        get => ActiveBattle.CurrentDefenseResultShown;
        internal set => ActiveBattle.CurrentDefenseResultShown = value;
    } // 히트 결과가 표시되었는지 여부
    internal bool CurrentClashResultShown
    {
        get => ActiveBattle.CurrentClashResultShown;
        set => ActiveBattle.CurrentClashResultShown = value;
    } // 현재 클래시 결과가 표시되었는지 여부
    public bool windowPrompted
    {
        get => ActiveBattle.WindowPrompted;
        internal set => ActiveBattle.WindowPrompted = value;
    } // 히트 윈도우가 열렸는지 여부
    
    // 중단 상태 추적
    internal bool isInterrupted
    {
        get => ActiveBattle.IsInterrupted;
        set => ActiveBattle.IsInterrupted = value;
    } // 현재 턴에서 중단이 발생했는지 여부
    
    // 전투 종료 상태 추적
    internal bool isBattleEnded
    {
        get => ActiveBattle.IsBattleEnded;
        set => ActiveBattle.IsBattleEnded = value;
    } // 전투가 종료되었는지 여부
    private BattleResult battleResult => ActiveBattle.BattleResult; // 전투 결과
    public event System.Action<BattleResult> OnBattleEnded; // 전투 종료 이벤트
    
    // FloatingText 생성 상태 추적 (입력 처리 결과와 분리)
    internal bool floatingTextShown
    {
        get => ActiveBattle.FloatingTextShown;
        set => ActiveBattle.FloatingTextShown = value;
    } // 공격자 FloatingText 생성 여부
    public ICombatController CurrentController
    {
        get => ActiveBattle.CurrentController;
        internal set => ActiveBattle.CurrentController = value;
    } // player/enemy 컨트롤러의 인터페이스
    public CharacterCommandResult CurrentResult
    {
        get => ActiveBattle.CurrentResult;
        internal set => ActiveBattle.CurrentResult = value;
    } // 현재 커맨드 결과
    public static float CombatStartTime { get; internal set; } // 전투 시작 시간 (초 단위 f.)
    public CombatCharacterManager.CombatantSlot CurrentAttackerSlot => currentTurnContext?.AttackerSlot ?? currentAttackerSlot;
    public CombatCharacterManager.CombatantSlot CurrentDefenderSlot => currentTurnContext?.DefenderSlot ?? currentDefenderSlot;
    public float GetInputDeadline() // 입력 마감 시간 계산
    {
        return CombatStartTime + CurrentTurnDuration - GlobalConfig.Instance.InputBufferEndSeconds;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // CombatManager의 싱글톤 인스턴스 설정
            // CombatScene 전용이므로 DontDestroyOnLoad 적용 안함
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성 방지
            return;
        }
    }


    private void Start()
    {
        // EventSystem 상태 확인
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("[CombatManager] Start에서 EventSystem이 없습니다!");
        }
        else
        {
            Debug.Log($"[CombatManager] Start에서 EventSystem 활성화 상태: {eventSystem.enabled}");
        }
        
        // Combat ActionMap 활성화 (UI ActionMap은 유지)
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.EnableCombatMap();
            Debug.Log("[CombatManager] Combat ActionMap 활성화 (UI는 유지)");
        }
        
        // 전투 결과 초기화
        battle = new BattleState(this);
        battle.InitializeResult();
        battleExecutor = new BattleExecutor(this);
        
        Debug.Log($"[CombatManager] 초기화 완료. 외부에서 StartBattle() 호출 대기 중...");
    }
    
    /// <summary>
    /// 전투를 시작합니다 (SetupNextBattle()로 설정된 참가자 사용)
    /// </summary>
    public void StartBattle()
    {
        StartBattle(pendingTeamAIds, pendingTeamBIds);
    }
    
    /// <summary>
    /// 전투를 시작합니다
    /// 추후 상위 시스템(필드, 던전 등)에서 호출하게 될 진입점
    /// </summary>
    /// <param name="playerInstanceId">플레이어 Instance ID</param>
    /// <param name="enemyInstanceIds">적 Instance ID 배열</param>
    public void StartBattle(string playerInstanceId, params string[] enemyInstanceIds)
    {
        var teamAIds = new List<string> { playerInstanceId };
        var teamBIds = new List<string>(enemyInstanceIds ?? System.Array.Empty<string>());
        StartBattle(teamAIds, teamBIds);
    }

    public void StartBattle(IList<string> teamAIds, IList<string> teamBIds)
    {
        Debug.Log($"[CombatManager] === 전투 시작 명령 수신 ===");
        Debug.Log($"[CombatManager] 전투원: TeamA[{string.Join(", ", teamAIds ?? new List<string>())}] vs TeamB[{string.Join(", ", teamBIds ?? new List<string>())}]");

        CombatCharacterManager.Instance.InitializeBattle(teamAIds, teamBIds);

        // 🆕 슬롯 기반으로 모든 캐릭터 스테이터스 초기화
        var characterManager = CombatCharacterManager.Instance;
        foreach (var slot in characterManager.EnumerateAllSlots())
        {
            if (slot?.Character != null)
            {
                slot.Character.InitializeRuntimeStats();
                Debug.Log($"[CombatManager] 전투 시작 - 스테이터스 초기화 - Team:{slot.Team} {slot.Character.Name} - HP: {slot.Character.GetHPStatus()}, Poise: {slot.Character.GetPoiseStatus()}");
            }
        }

        SpawnTeamActors();
        EnsureInputHandlers();
        ConnectControllers();

        battle = new BattleState(this);
        battle.InitializeResult();
        battleExecutor = new BattleExecutor(this);

        StartCoroutine(RunCombat());
    }

    private void ConnectControllers() // Controller 연결
    {
        var manager = CombatCharacterManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[CombatManager] CombatCharacterManager.Instance가 null입니다!");
            return;
        }

        ConnectLeaderController(manager, CombatCharacterManager.CombatTeam.TeamA);
        ConnectLeaderController(manager, CombatCharacterManager.CombatTeam.TeamB);

        Debug.Log("[CombatManager] Controller 연결 완료 (슬롯 기반)");
    }

    private void SpawnTeamActors()
    {
        CleanupSpawnedActors();

        var manager = CombatCharacterManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[CombatManager] CombatCharacterManager.Instance가 null입니다!");
            return;
        }

        SpawnActorForSlot(CombatCharacterManager.CombatTeam.TeamA, manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA));
        SpawnActorForSlot(CombatCharacterManager.CombatTeam.TeamB, manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB));
    }

    private void CleanupSpawnedActors()
    {
        foreach (var actor in spawnedActors)
        {
            if (actor != null)
            {
                Destroy(actor);
            }
        }

        spawnedActors.Clear();
        spawnedActorMap.Clear();
        playerController = null;
        enemyController = null;
        attackerInputHandler = null;
        defenderInputHandler = null;
    }

    private void SpawnActorForSlot(CombatCharacterManager.CombatTeam team, CombatCharacterManager.CombatantSlot slot)
    {
        if (slot == null || slot.Character == null)
        {
            if (team == CombatCharacterManager.CombatTeam.TeamB)
            {
                enemyController = null;
            }
            return;
        }

        bool isTeamB = team == CombatCharacterManager.CombatTeam.TeamB;
        bool flip = flipTeamBActors && isTeamB;
        Transform spawnPoint = GetSpawnPoint(team);
        Transform parent = actorsRoot != null ? actorsRoot : transform;

        if (spawnedActorMap.TryGetValue(slot, out var existingActor) && existingActor != null)
        {
            Destroy(existingActor);
            spawnedActorMap.Remove(slot);
        }

        if (slot.Character is PlayerCharacter)
        {
            var actor = InstantiateActor(playerActorPrefab, spawnPoint, parent, flip);
            if (actor == null)
            {
                return;
            }

            var controller = actor.GetComponentInChildren<PlayerController>(true);
            if (controller == null)
            {
                Debug.LogError("[CombatManager] PlayerActorPrefab에 PlayerController가 포함되어 있지 않습니다.");
            }
            else
            {
                playerController = controller;
                slot.BindController(controller);
            }

            if (attackerInputHandler == null)
            {
                attackerInputHandler = actor.GetComponentInChildren<AttackerInputHandler>(true);
            }

            if (defenderInputHandler == null)
            {
                defenderInputHandler = actor.GetComponentInChildren<DefenderInputHandler>(true);
            }

            if (attackerInputHandler == null || defenderInputHandler == null)
            {
                Debug.LogError("[CombatManager] Player Actor에서 Attacker/DefenderInputHandler를 찾지 못했습니다. Player 프리팹에 핸들러 컴포넌트가 포함되어 있는지 확인하세요.");
            }

            if (team == CombatCharacterManager.CombatTeam.TeamB)
            {
                enemyController = null;
            }

            spawnedActorMap[slot] = actor;
        }
        else
        {
            var actor = InstantiateActor(npcActorPrefab, spawnPoint, parent, flip);
            if (actor == null)
            {
                return;
            }

            var controller = actor.GetComponentInChildren<AIController>(true);
            if (controller == null)
            {
                Debug.LogError("[CombatManager] NPCActorPrefab에 AIController가 포함되어 있지 않습니다.");
                return;
            }

            if (team == CombatCharacterManager.CombatTeam.TeamB)
            {
                enemyController = controller;
            }

            slot.BindController(controller);

            spawnedActorMap[slot] = actor;
        }
    }

    private GameObject InstantiateActor(GameObject prefab, Transform spawnPoint, Transform parent, bool flip)
    {
        if (prefab == null)
        {
            Debug.LogError("[CombatManager] Actor Prefab이 설정되지 않았습니다.");
            return null;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : parent.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : parent.rotation;

        var instance = Instantiate(prefab, position, rotation, parent);
        spawnedActors.Add(instance);

        if (flip)
        {
            FlipActor(instance.transform);
        }

        return instance;
    }

    public GameObject GetSpawnedActor(CombatCharacterManager.CombatantSlot slot)
    {
        if (slot == null)
        {
            return null;
        }

        return spawnedActorMap.TryGetValue(slot, out var actor) ? actor : null;
    }

    private Transform GetSpawnPoint(CombatCharacterManager.CombatTeam team)
    {
        if (team == CombatCharacterManager.CombatTeam.TeamA)
        {
            if (teamASpawnPoint != null)
                return teamASpawnPoint;
        }
        else
        {
            if (teamBSpawnPoint != null)
                return teamBSpawnPoint;
        }

        Debug.LogWarning($"[CombatManager] {team} SpawnPoint가 설정되지 않아 CombatManager 위치를 사용합니다.");
        return transform;
    }

    private void FlipActor(Transform target)
    {
        if (target == null)
            return;

        var scale = target.localScale;
        scale.x = -Mathf.Abs(scale.x);
        target.localScale = scale;
    }

    private ICombatController GetControllerForCharacter(Character character, CombatCharacterManager.CombatantSlot slot)
    {
        if (character == null)
        {
            return null;
        }

        var manager = CombatCharacterManager.Instance;
        var resolvedSlot = slot ?? manager?.FindSlotByCharacter(character);

        if (resolvedSlot?.Controller != null)
        {
            return resolvedSlot.Controller;
        }

        if (character is PlayerCharacter && playerController != null)
        {
            return playerController;
        }

        return null;
    }

    private Transform GetActorTransform(ICombatController controller)
    {
        switch (controller)
        {
            case PlayerController playerCtrl:
                return playerCtrl.transform;
            case AIController aiController:
                return aiController.transform;
            default:
                return null;
        }
    }

    internal Character GetCurrentAttackerCharacter()
    {
        if (currentTurnContext?.AttackerCharacter != null)
        {
            return currentTurnContext.AttackerCharacter;
        }

        if (CurrentAttackerSlot?.Character != null)
        {
            return CurrentAttackerSlot.Character;
        }

        return currentAttackerController?.Character;
    }

    internal Character GetCurrentDefenderCharacter()
    {
        if (currentTurnContext?.DefenderCharacter != null)
        {
            return currentTurnContext.DefenderCharacter;
        }

        if (CurrentDefenderSlot?.Character != null)
        {
            return CurrentDefenderSlot.Character;
        }

        return currentDefenderController?.Character;
    }

    private AIController GetCurrentAttackerAI()
    {
        if (currentAttackerController is AIController ai)
        {
            return ai;
        }

        if (currentAttackerSlot?.Controller is AIController slotAi)
        {
            return slotAi;
        }

        var manager = CombatCharacterManager.Instance;
        var slot = currentTurnContext?.AttackerSlot ?? manager?.FindSlotByCharacter(currentTurnContext?.AttackerCharacter ?? currentAttackerSlot?.Character);
        if (slot?.Controller is AIController resolvedAi)
        {
            return resolvedAi;
        }

        return null;
    }

    private AIController GetCurrentDefenderAI()
    {
        if (currentDefenderController is AIController ai)
        {
            return ai;
        }

        if (currentDefenderSlot?.Controller is AIController slotAi)
        {
            return slotAi;
        }

        var manager = CombatCharacterManager.Instance;
        var slot = currentTurnContext?.DefenderSlot ?? manager?.FindSlotByCharacter(currentTurnContext?.DefenderCharacter ?? currentDefenderSlot?.Character);
        if (slot?.Controller is AIController resolvedAi)
        {
            return resolvedAi;
        }

        return null;
    }

    private Transform GetCurrentAttackerTransform()
    {
        var transform = GetActorTransform(currentAttackerController);
        if (transform != null)
        {
            return transform;
        }

        if (GetCurrentAttackerCharacter() is PlayerCharacter && playerController != null)
        {
            return playerController.transform;
        }

        var ai = GetCurrentAttackerAI();
        return ai != null ? ai.transform : null;
    }

    private Transform GetCurrentDefenderTransform()
    {
        var transform = GetActorTransform(currentDefenderController);
        if (transform != null)
        {
            return transform;
        }

        if (GetCurrentDefenderCharacter() is PlayerCharacter && playerController != null)
        {
            return playerController.transform;
        }

        var ai = GetCurrentDefenderAI();
        return ai != null ? ai.transform : null;
    }

    private void InitializeInputHandlers()
    {
        if (attackerInputHandler == null)
        {
            attackerInputHandler = FindPreferredInputHandler<AttackerInputHandler>();
        }

        if (defenderInputHandler == null)
        {
            defenderInputHandler = FindPreferredInputHandler<DefenderInputHandler>();
        }
    }

    private T FindPreferredInputHandler<T>() where T : BaseInputHandler
    {
        var handlers = GetComponentsInChildren<T>(true);
        T fallback = null;
        foreach (var handler in handlers)
        {
            if (handler == null)
            {
                continue;
            }

            if (handler.GetComponentInParent<PlayerController>() != null)
            {
                return handler;
            }

            if (fallback == null)
            {
                fallback = handler;
            }
        }

        return fallback;
    }

    private void EnsureInputHandlers()
    {
        InitializeInputHandlers();

        if (attackerInputHandler == null)
        {
            attackerInputHandler = CreateFallbackAttackerInputHandler();
        }

        if (defenderInputHandler == null)
        {
            defenderInputHandler = CreateFallbackDefenderInputHandler();
        }
    }

    internal void BindInputHandler(BaseInputHandler handler, CombatCharacterManager.CombatantSlot slot)
    {
        if (handler == null)
        {
            return;
        }

        handler.BindToSlot(slot);

        if (handler is DefenderInputHandler defenderHandler)
        {
            EnsureCharacterHitSystem(defenderHandler, slot);
        }
    }

    private void EnsureCharacterHitSystem(DefenderInputHandler defenderHandler, CombatCharacterManager.CombatantSlot slot)
    {
        if (defenderHandler == null)
        {
            return;
        }

        if (defenderHandler.CharacterHitSystem != null)
        {
            return;
        }

        CharacterHitSystem hitSystem = null;

        if (slot?.Controller is Component controllerComponent)
        {
            hitSystem = controllerComponent.GetComponentInChildren<CharacterHitSystem>(true);
        }

        if (hitSystem == null && slot != null)
        {
            var actor = GetSpawnedActor(slot);
            if (actor != null)
            {
                hitSystem = actor.GetComponentInChildren<CharacterHitSystem>(true);
            }
        }

        if (hitSystem == null)
        {
            GameObject origin = slot != null ? GetSpawnedActor(slot) : null;
            origin ??= defenderHandler.gameObject;

            hitSystem = origin.GetComponent<CharacterHitSystem>();
            if (hitSystem == null)
            {
                hitSystem = origin.AddComponent<CharacterHitSystem>();
                Debug.Log($"[CombatManager] CharacterHitSystem 자동 추가 - origin:{origin.name}");
            }
        }

        defenderHandler.SetCharacterHitSystem(hitSystem);
    }

    private AttackerInputHandler CreateFallbackAttackerInputHandler()
    {
        var handlerObject = new GameObject("Fallback_AttackerInputHandler");
        handlerObject.transform.SetParent(transform, false);
        var handler = handlerObject.AddComponent<AttackerInputHandler>();
        Debug.Log("[CombatManager] Fallback AttackerInputHandler 생성 (NPC 전용)");
        return handler;
    }

    private DefenderInputHandler CreateFallbackDefenderInputHandler()
    {
        var handlerObject = new GameObject("Fallback_DefenderInputHandler");
        handlerObject.transform.SetParent(transform, false);
        handlerObject.AddComponent<CharacterHitSystem>();
        var handler = handlerObject.AddComponent<DefenderInputHandler>();
        Debug.Log("[CombatManager] Fallback DefenderInputHandler 생성 (NPC 전용)");
        return handler;
    }

    private void ConnectLeaderController(CombatCharacterManager manager, CombatCharacterManager.CombatTeam team)
    {
        var slot = manager.GetLeaderSlot(team);
        if (slot == null)
        {
            Debug.LogWarning($"[CombatManager] {team} 리더 슬롯을 찾을 수 없습니다.");
            return;
        }

        if (slot.Character == null)
        {
            Debug.LogWarning($"[CombatManager] {team} 리더 캐릭터가 null입니다.");
            return;
        }

        ICombatController controllerToUse = slot.Controller;

        if (controllerToUse == null && slot.Character is PlayerCharacter)
        {
            controllerToUse = playerController;
        }

        if (controllerToUse == null)
        {
            var actor = GetSpawnedActor(slot);
            if (actor != null)
            {
                var playerCtrl = actor.GetComponentInChildren<PlayerController>(true);
                if (playerCtrl != null)
                {
                    controllerToUse = playerCtrl;
                }
                else
                {
                    var aiCtrl = actor.GetComponentInChildren<AIController>(true);
                    controllerToUse = aiCtrl;
                }
            }
        }

        if (controllerToUse == null)
        {
            Debug.LogError($"[CombatManager] {team} 리더 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        manager.ConnectController(team, 0, controllerToUse);

        ActionCommandSelectionManager.Instance?.GetTeamActionUI(team)?.AssignController(controllerToUse);
    }
    
    /// <summary>
    /// 검술 기반 턴 지속 시간 계산
    /// </summary>
    /// <param name="command">사용할 검술 커맨드</param>
    /// <returns>계산된 턴 지속 시간 (초)</returns>
    internal float CalculateTurnDuration(ActionCommandData command)
    {
        if (command == null || command.perfectTimings == null || command.perfectTimings.Count == 0)
        {
            Debug.LogWarning("[CombatManager] 유효하지 않은 커맨드로 기본 턴 지속 시간 사용");
            return 1.0f; // 기본값 1초 사용 (마지막 히트 완료 시간이 없으므로)
        }
        
        // 마지막 히트의 완료 시간 계산
        var lastTiming = command.perfectTimings[command.perfectTimings.Count - 1];
        float lastHitEndTime = lastTiming.start + lastTiming.duration;
        
        // 추가 시간 (기본 0초, GlobalConfig에서 설정 가능)
        float additionalTime = GlobalConfig.Instance.AdditionalTurnDuration; // 기본값 0초
        
        float totalDuration = lastHitEndTime + additionalTime;
        
        Debug.Log($"[CombatManager] 턴 지속 시간 계산 - 마지막 히트 완료: {lastHitEndTime}초, 추가 시간: {additionalTime}초, 총 지속 시간: {totalDuration}초");
        
        return totalDuration;
    }
    
    /// <summary>
    /// 애니메이션 완료 대기 후 다음 턴 시작
    /// </summary>
    /// <param name="nextController">다음 턴을 수행할 컨트롤러</param>
    /// <returns></returns>
    private IEnumerator WaitForAnimationAndStartNextTurn(ICombatController nextController)
    {
        // 기본 턴 전환 대기 시간
        float baseWaitTime = GlobalConfig.Instance.TurnEndBuffer;
        
        // 애니메이션 완료를 위한 추가 대기 시간 (피격 애니메이션 등)
        float animationWaitTime = GlobalConfig.Instance.AnimationWaitTime;
        
        // 총 대기 시간
        float totalWaitTime = baseWaitTime + animationWaitTime;
        
        Debug.Log($"[CombatManager] 턴 전환 대기 - 기본: {baseWaitTime}초, 애니메이션: {animationWaitTime}초, 총: {totalWaitTime}초");
        
        yield return new WaitForSeconds(totalWaitTime);
        
        // 다음 턴 시작 (이제는 BattleExecutor가 처리하므로 여기서는 처리하지 않음)
        // var manager = CombatCharacterManager.Instance;
        // var slot = manager?.FindSlotByController(nextController);
        // var team = slot != null ? slot.Team : CombatCharacterManager.CombatTeam.TeamB;
        // var context = BuildTurnContext(team);
        // if (context != null)
        // {
        //     yield return StartCoroutine(PerformTurn(context));
        // }
        Debug.LogWarning("[CombatManager] WaitForAnimationAndStartNextTurn는 더 이상 사용되지 않습니다. BattleExecutor가 턴 관리를 처리합니다.");
    }


    private IEnumerator RunCombat()
    {
        if (battleExecutor == null)
        {
            battleExecutor = new BattleExecutor(this);
        }

        yield return battleExecutor.RunBattle();
    }

    private CombatTurnContext PrepareTeamTurn(CombatCharacterManager.CombatTeam team)
    {
        var context = BuildTurnContext(team);
        if (context == null || !context.IsValid)
        {
            Debug.LogError($"[RunCombat] {team} 턴 컨텍스트 생성 실패로 전투를 종료합니다.");
            isBattleEnded = true;
            return null;
        }

        switch (team)
        {
            case CombatCharacterManager.CombatTeam.TeamA:
                CombatStartTime = Time.time;
                CurrentTurnNumber++;
                Debug.Log($"[RunCombat] 턴 {CurrentTurnNumber} 시작 - TeamA 리더 턴 ({context.AttackerCharacter?.Name} vs {context.DefenderCharacter?.Name})");
                break;
            case CombatCharacterManager.CombatTeam.TeamB:
                Debug.Log($"[RunCombat] 턴 {CurrentTurnNumber} 계속 - TeamB 리더 턴 ({context.AttackerCharacter?.Name} vs {context.DefenderCharacter?.Name})");
                break;
        }

        return context;
    }
    
    /// <summary>
    /// 모든 캐릭터의 BT 실행 상태를 리셋 (새 전투 시작 시)
    /// 
    /// 블랙보드 패턴:
    /// - Combatant의 Blackboard.ResetCombat() 호출
    /// - BT 자체는 상태가 없으므로 리셋 불필요
    /// </summary>
    internal void ResetBehaviorTreeStates()
    {
        var characterManager = CombatCharacterManager.Instance;
        if (characterManager == null)
        {
            Debug.LogWarning("[CombatManager] CombatCharacterManager.Instance가 null입니다 - BT 상태 리셋 건너뜀");
            return;
        }

        foreach (var slot in characterManager.EnumerateAllSlots())
        {
            if (slot?.Character == null)
            {
                continue;
            }

            switch (slot.Character)
            {
                case PlayerCharacter playerCharacter:
                    playerCharacter.ResetBlackboard();
                    Debug.Log($"[CombatManager] 블랙보드 리셋 완료 - Team:{slot.Team} Player {playerCharacter.Name}");
                    break;
                case EnemyCharacter enemyCharacter:
                    enemyCharacter.ResetBlackboard();
                    Debug.Log($"[CombatManager] 블랙보드 리셋 완료 - Team:{slot.Team} Enemy {enemyCharacter.Name}");
                    break;
            }
        }

        Debug.Log("[CombatManager] 모든 BT 블랙보드 리셋 완료");
    }
    
    /// <summary>
    /// NPC의 런타임 확률을 원본으로 리셋합니다 (턴 종료 시 호출)
    /// 
    /// 역할:
    /// - BT가 해당 턴에 적용한 확률 조정을 모두 제거
    /// - CharacterData의 원본 확률로 복원
    /// 
    /// 호출 시점:
    /// - 각 턴 종료 후 (RunCombat에서 호출)
    /// - 다음 턴 시작 시 BT가 다시 새롭게 확률을 조정함
    /// </summary>
    internal void ResetNPCProbabilities()
    {
        var characterManager = CombatCharacterManager.Instance;
        if (characterManager == null)
        {
            Debug.LogWarning("[CombatManager] CombatCharacterManager.Instance가 null입니다 - NPC 확률 리셋 건너뜀");
            return;
        }

        foreach (var slot in characterManager.EnumerateAllSlots())
        {
            if (slot?.Character is EnemyCharacter enemyCharacter)
            {
                enemyCharacter.ResetProbabilities();
                Debug.Log($"[CombatManager] NPC 확률 리셋 완료 - Team:{slot.Team} {enemyCharacter.Name}");
            }
        }
    }

    internal bool AreAllHitJudgmentsCompleted(int hitCount)
    {
        if (hitJudgmentCompleted == null)
        {
            Debug.Log($"[CombatManager] 🆕 hitJudgmentCompleted 배열이 null - hitCount:{hitCount}");
            return false;
        }
        
        for (int i = 0; i < hitCount; i++)
        {
            if (i >= hitJudgmentCompleted.Length || !hitJudgmentCompleted[i])
            {
                Debug.Log($"[CombatManager] 🆕 Hit {i} 판정 미완료 - 배열길이:{hitJudgmentCompleted.Length}, 완료상태:{hitJudgmentCompleted[i]}");
                return false;
            }
        }
        
        Debug.Log($"[CombatManager] 🆕 모든 Hit 판정 완료 확인 - hitCount:{hitCount}");
        return true;
    }
    
    /// <summary>
    /// 남은 히트 판정을 강제로 완료 처리합니다.
    /// 
    /// 역할:
    /// - 중단 발생 시 더 이상 발사되지 않을 히트들을 "완료"로 표시
    /// - EnsureAllHitJudgmentsCompleted()의 무한 대기를 방지
    /// 
    /// 호출 시점:
    /// - isInterrupted = true일 때
    /// - CheckInterruptCondition()이 true일 때
    /// 
    /// 예시:
    /// - hitCount = 3, currentHit = 1 (2번째 히트 중단)
    /// - hitJudgmentCompleted[1], [2]를 강제로 true로 설정
    /// </summary>
    /// <param name="currentHit">현재 히트 인덱스</param>
    /// <param name="totalHits">총 히트 수</param>
    internal void ForceCompleteRemainingHits(int currentHit, int totalHits)
    {
        if (hitJudgmentCompleted == null)
        {
            Debug.LogWarning("[중단] hitJudgmentCompleted 배열이 null - 강제 완료 생략");
            return;
        }
        
        Debug.Log($"[중단] 남은 히트 판정 강제 완료 시작: Hit {currentHit} ~ {totalHits - 1}");
        
        int completedCount = 0;
        for (int i = currentHit; i < totalHits; i++)
        {
            if (i < hitJudgmentCompleted.Length)
            {
                if (!hitJudgmentCompleted[i])
                {
                    hitJudgmentCompleted[i] = true;
                    completedCount++;
                    Debug.Log($"  - Hit {i}: 강제 완료 처리");
                }
                else
                {
                    Debug.Log($"  - Hit {i}: 이미 완료됨");
                }
            }
        }
        
        Debug.Log($"[중단] 강제 완료 처리 완료 - {completedCount}개 히트 처리됨");
    }
    
    /// <summary>
    /// 공격자와 피격자의 애니메이션이 모두 완료될 때까지 대기
    /// </summary>
    internal IEnumerator WaitForAnimationsComplete(Character attacker, Character target)
    {
        Debug.Log($"[CombatManager] 애니메이션 완료 대기 시작 - 공격자: {attacker.Name}, 피격자: {target.Name}");
        
        // 공격자와 피격자의 컨트롤러 가져오기
        ICombatController attackerController = currentAttackerController ?? GetControllerForCharacter(attacker, currentAttackerSlot);
        ICombatController defenderController = currentDefenderController ?? GetControllerForCharacter(target, currentDefenderSlot);
        
        // 최대 대기 시간 (안전장치)
        float maxWaitTime = 10f;
        float startTime = Time.time;
        
        // 공격자 애니메이션 완료 대기
        yield return StartCoroutine(WaitForControllerAnimationComplete(attackerController, "공격자", maxWaitTime));
        
        // 피격자 애니메이션 완료 대기
        yield return StartCoroutine(WaitForControllerAnimationComplete(defenderController, "피격자", maxWaitTime));
        
        float totalWaitTime = Time.time - startTime;
        Debug.Log($"[CombatManager] 모든 애니메이션 완료 대기 완료 - 총 대기 시간: {totalWaitTime:F2}초");
    }
    
    /// <summary>
    /// 특정 컨트롤러의 애니메이션이 완료될 때까지 대기
    /// </summary>
    private IEnumerator WaitForControllerAnimationComplete(ICombatController controller, string role, float maxWaitTime)
    {
        if (controller == null)
        {
            Debug.LogWarning($"[CombatManager] {role} 컨트롤러가 null입니다. 애니메이션 대기를 건너뜁니다.");
            yield break;
        }
        
        float startTime = Time.time;
        
        // 기본 애니메이션 대기 시간 (GlobalConfig에서 설정)
        float baseWaitTime = GlobalConfig.Instance.AnimationWaitTime;
        
        // 최소 대기 시간 적용
        if (baseWaitTime > 0)
        {
            Debug.Log($"[CombatManager] {role} 기본 애니메이션 대기: {baseWaitTime}초");
            yield return new WaitForSeconds(baseWaitTime);
        }
        
        // 실제 애니메이션 상태 확인을 통한 완료 대기
        yield return StartCoroutine(WaitForActualAnimationComplete(controller, role, maxWaitTime));
        
        float totalWaitTime = Time.time - startTime;
        Debug.Log($"[CombatManager] {role} 애니메이션 대기 완료 - 총 대기 시간: {totalWaitTime:F2}초");
    }
    
    /// <summary>
    /// 실제 Animator 상태를 확인하여 애니메이션 완료를 감지
    /// </summary>
    private IEnumerator WaitForActualAnimationComplete(ICombatController controller, string role, float maxWaitTime)
    {
        // Controller에서 Animator 컴포넌트 가져오기
        Animator animator = GetControllerAnimator(controller);
        if (animator == null)
        {
            Debug.LogWarning($"[CombatManager] {role} Animator를 찾을 수 없습니다. 기본 대기 시간을 사용합니다.");
            yield return new WaitForSeconds(1f); // 기본 1초 대기
            yield break;
        }
        
        float startTime = Time.time;
        float lastAnimationTime = 0f;
        int stableFrameCount = 0;
        const int requiredStableFrames = 3; // 3프레임 동안 안정적이어야 완료로 간주
        
        Debug.Log($"[CombatManager] {role} 실제 애니메이션 상태 모니터링 시작");
        
        while (Time.time - startTime < maxWaitTime)
        {
            // 현재 애니메이션 상태 정보 가져오기
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float currentAnimationTime = stateInfo.normalizedTime;
            
            // 애니메이션이 루프되지 않는 경우 (normalizedTime이 1.0 이상이면 완료)
            if (stateInfo.normalizedTime >= 1.0f)
            {
                Debug.Log($"[CombatManager] {role} 애니메이션 완료 감지 (normalizedTime: {stateInfo.normalizedTime:F2})");
                break;
            }
            
            // 애니메이션 시간이 변하지 않는 경우 (안정 상태)
            if (Mathf.Approximately(currentAnimationTime, lastAnimationTime))
            {
                stableFrameCount++;
                if (stableFrameCount >= requiredStableFrames)
                {
                    Debug.Log($"[CombatManager] {role} 애니메이션 안정 상태 감지 (stableFrames: {stableFrameCount})");
                    break;
                }
            }
            else
            {
                stableFrameCount = 0; // 리셋
            }
            
            lastAnimationTime = currentAnimationTime;
            yield return null; // 다음 프레임까지 대기
        }
        
        // 추가 안전 대기 (애니메이션이 완전히 끝날 때까지)
        float additionalWaitTime = 0.2f;
        Debug.Log($"[CombatManager] {role} 추가 안전 대기: {additionalWaitTime}초");
        yield return new WaitForSeconds(additionalWaitTime);
    }
    
    /// <summary>
    /// Controller에서 Animator 컴포넌트를 가져옵니다
    /// </summary>
    private Animator GetControllerAnimator(ICombatController controller)
    {
        if (controller is PlayerController playerCtrl)
        {
            return playerCtrl.CombatAnimationObject?.GetComponent<Animator>();
        }
        else if (controller is AIController enemyCtrl)
        {
            return enemyCtrl.CombatAnimationObject?.GetComponent<Animator>();
        }
        
        return null;
    }
    
    /// <summary>
    /// FloatingText 위치 계산 (2D 프로젝트용)
    /// </summary>
    /// <param name="attackerIsPlayer">플레이어가 공격자인지 여부</param>
    /// <returns>FloatingText가 표시될 월드 위치</returns>
    internal Vector3 GetFloatingTextPosition(bool attackerIsPlayer)
    {
        Transform referenceTransform = GetActorTransform(currentTurnContext?.AttackerController ?? currentAttackerController);

        if (referenceTransform == null)
        {
            var attackerSlot = currentTurnContext?.AttackerSlot ?? currentAttackerSlot;
            if (attackerSlot?.Controller != null)
            {
                referenceTransform = GetActorTransform(attackerSlot.Controller);
            }
        }

        if (referenceTransform == null)
        {
            if (currentTurnContext?.AttackerController is PlayerController playerCtrl)
            {
                referenceTransform = playerCtrl.transform;
            }
            else if (currentTurnContext?.AttackerController is AIController aiCtrl)
            {
                referenceTransform = aiCtrl.transform;
            }
            else if (attackerIsPlayer && playerController != null)
            {
                referenceTransform = playerController.transform;
            }
        }

        if (referenceTransform == null)
        {
            Debug.LogWarning("[FloatingText 위치] 참조 컨트롤러를 찾을 수 없어 CombatManager 위치를 사용합니다.");
            referenceTransform = transform;
        }

        Vector3 basePosition = referenceTransform.position;
        Debug.Log($"[FloatingText 위치] 기준 위치: {basePosition}");

        // 2. 캐릭터 위쪽에 오프셋 추가 (기존 2f에서 1.5f로 감소)
        Vector3 finalPosition = basePosition + Vector3.up * 1.5f;
        Debug.Log($"[FloatingText 위치] 오프셋 적용 후: {finalPosition}");
        
        // 3. 화면 밖으로 나가지 않도록 제한
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(finalPosition);
            
            // 화면 경계 내로 제한 (여백 100픽셀)
            screenPos.x = Mathf.Clamp(screenPos.x, 100, Screen.width - 100);
            screenPos.y = Mathf.Clamp(screenPos.y, 100, Screen.height - 100);
            
            // 다시 월드 좌표로 변환
            finalPosition = Camera.main.ScreenToWorldPoint(screenPos);
            
            Debug.Log($"[FloatingText 위치] 화면 경계 제한 후: {finalPosition}");
        }
        
        Debug.Log($"[FloatingText 위치] 최종 위치: {finalPosition}");
        return finalPosition;
    }
    
    /// <summary>
    /// AI 위치를 가져오는 대체 방법
    /// </summary>
    /// <returns>AI의 대략적인 위치</returns>
    private Vector3 GetAIPositionFallback()
    {
        var attackerSlot = currentTurnContext?.AttackerSlot ?? currentAttackerSlot;
        if (attackerSlot?.Controller is AIController slotAi && slotAi != null)
        {
            Debug.Log($"[FloatingText 위치] AI 슬롯 컨트롤러 위치 사용: {slotAi.transform.position}");
            return slotAi.transform.position;
        }

        if (currentTurnContext?.AttackerController is AIController aiController && aiController != null)
        {
            Debug.Log($"[FloatingText 위치] AI 컨트롤러 위치 사용: {aiController.transform.position}");
            return aiController.transform.position;
        }
        
        if (playerController != null)
        {
            Vector3 playerPos = playerController.transform.position;
            Vector3 fallbackPos = new Vector3(-playerPos.x, playerPos.y, playerPos.z);
            Debug.Log($"[FloatingText 위치] 플레이어 반대편 대체 위치 사용: {fallbackPos}");
            return fallbackPos;
        }
        
        // 3. 최후의 수단: 화면 중앙
        Vector3 centerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        Debug.Log($"[FloatingText 위치] 화면 중앙 대체 위치 사용: {centerPos}");
        return centerPos;
    }

    public void OnInputReceivedFromHandler(BaseInputHandler handler)
    {
        // 모든 히트가 완료된 경우 입력 무시
        if (CurrentHit >= CurrentResult?.HitCount)
        {
            Debug.Log($"[CombatManager] 모든 히트 완료! CurrentHit={CurrentHit}, HitCount={CurrentResult?.HitCount} - 입력 무시");
            return;
        }

        bool isPerfect = handler.HasPerfectInput();
        Debug.Log($"[CombatManager] OnInputReceivedFromHandler: handler={handler.GetType().Name}, isPerfect={isPerfect}");

        if (handler == attackerInputHandler)
        {
            attackerPerfectInput = isPerfect; // 플레이어 공격자 입력 처리
            Debug.Log($"[CombatManager] 공격자 입력 수신: {isPerfect}");
            if (!CurrentAttackResultShown)
            {
                ResolveInput(handler, isPerfect);
            }
        }
        else if (handler == defenderInputHandler)
        {
            // 🆕 발사체 기반 방어자 입력 처리
            defenderPerfectInput = isPerfect; // 방어자 입력 처리
            Debug.Log($"[CombatManager] 발사체 기반 방어자 입력 수신: {isPerfect}");
            
            // 🆕 발사체 기반에서는 즉시 판정하지 않고, 발사체 충돌 시에만 판정
            // ResolveInput 호출 제거 (발사체 충돌 시에만 판정 발생)
            // 방어자 입력은 발사체 기반으로만 처리됨
        }
    }

    // 🆕 AI 입력 직접 처리 메서드 (InputHandler 없이)
    internal void ResolveAIInput(bool isAttacker, bool isPerfect)
    {
        Debug.Log($"[ResolveAIInput] 호출됨! isAttacker={isAttacker}, isPerfect={isPerfect}");

        if (!attackerPerfectInput.HasValue) attackerPerfectInput = false;
        if (!defenderPerfectInput.HasValue) defenderPerfectInput = false;

        // 안전한 범위 체크 추가
        if (CurrentResult != null && CurrentHit >= 0 && CurrentHit < CurrentResult.HitCount)
        {
            if (isAttacker)
            {
                CurrentResult.SetHitResult(CurrentHit, isPerfect);
            }
        }
        else
        {
            Debug.LogWarning($"[CombatManager] SetHitResult 실패: CurrentHit={CurrentHit}, HitCount={CurrentResult?.HitCount ?? 0}");
        }

        if (isAttacker) // 공격자 AI
        {
            if (CurrentAttackResultShown)
            {
                Debug.LogWarning("[ResolveAIInput] 공격자 입력 이미 처리됨 → 무시");
                return;
            }

            attackerPerfectInput = isPerfect;
            attackerInputTime = TurnTimer.ElapsedTime;

            var aiController = GetCurrentAttackerAI();
            if (aiController != null)
            {
                aiController.OnHitResult(CurrentHit, isPerfect);
            }

            CurrentAttackResultShown = true;

            // 완벽 입력 성공 시 가이드 완료 상태로 전환
            if (isPerfect && CombatHUD.Instance != null)
            {
                CombatHUD.Instance.MarkGuideAsCompleted(CurrentHit);
            }

            // 공격자 입력 처리 시 발사체 발사 (성공/실패 무관)
            Debug.Log($"[CombatManager] AI 공격자 입력 처리 완료 - 발사체 발사: 히트 {CurrentHit}, 완벽 입력: {isPerfect}");
            CreateProjectileForCurrentHit(isPerfect);
        }
        else // 방어자 AI
        {
            if (CurrentDefenseResultShown)
            {
                Debug.LogWarning("[ResolveAIInput] 방어자 입력 이미 처리됨 → 무시");
                return;
            }

            defenderPerfectInput = isPerfect;
            defenderInputTime = TurnTimer.ElapsedTime;

            var aiController = GetCurrentDefenderAI();
            if (aiController != null)
            {
                aiController.OnHitResult(CurrentHit, isPerfect);
            }

            CurrentDefenseResultShown = true;
        }
    }

    // 플레이어 입력을 처리할 때 호출 (InputHandler에서 호출됨)
    internal void ResolveInput(BaseInputHandler handler, bool isPerfect)
    {
        Debug.Log($"[ResolveInput] 호출됨! handler={handler}, isPerfect ={isPerfect}");

        if (!attackerPerfectInput.HasValue) attackerPerfectInput = false;
        if (!defenderPerfectInput.HasValue) defenderPerfectInput = false;

        bool atk = attackerPerfectInput.Value;
        bool def = defenderPerfectInput.Value;
        
        // 안전한 범위 체크 추가
        if (CurrentResult != null && CurrentHit >= 0 && CurrentHit < CurrentResult.HitCount)
        {
            CurrentResult.SetHitResult(CurrentHit, (bool)attackerPerfectInput);
        }
        else
        {
            Debug.LogWarning($"[CombatManager] SetHitResult 실패: CurrentHit={CurrentHit}, HitCount={CurrentResult?.HitCount ?? 0}");
        }

        // 컨트롤러에 결과 전달
        if (handler == attackerInputHandler) // 공격자 입장(핸들러)
        {
            if (CurrentAttackResultShown)
            {
                Debug.LogWarning("[ResolveInput] 공격자 입력 이미 처리됨 → 무시");
                return;
            }

            attackerInputTime = attackerInputHandler.GetLastInputTime();  // ✅ 공격자 입력 시간 저장
            if (attackerInputHandler.IsPlayer) // 공격자 : 플레이어
            {
                playerController.OnHitResult(CurrentHit, isPerfect);
            }
            else // 공격자 : AI
            {
                var aiController = GetCurrentAttackerAI();
                if (aiController != null)
                {
                    aiController.OnHitResult(CurrentHit, isPerfect);
                }
            }

            CurrentAttackResultShown = true; // 히트 결과가 표시되었음을 설정
            
            // 🆕 완벽 입력 성공 시 가이드 완료 상태로 전환
            if (isPerfect && CombatHUD.Instance != null)
            {
                CombatHUD.Instance.MarkGuideAsCompleted(CurrentHit);
            }
            
            // 🆕 공격자 입력 처리 시 발사체 발사 (성공/실패 무관)
            Debug.Log($"[CombatManager] 공격자 입력 처리 완료 - 발사체 발사: 히트 {CurrentHit}, 완벽 입력: {isPerfect}");
            CreateProjectileForCurrentHit(isPerfect);
        }

        if (handler == defenderInputHandler) // 방어자 입장(핸들러)
        {
            if (CurrentDefenseResultShown)
            {
                Debug.LogWarning("[ResolveInput] 방어자 입력 이미 처리됨 → 무시");
                return;
            }

            defenderInputTime = defenderInputHandler.GetLastInputTime();  // ✅ 방어자 입력 시간 저장
            Debug.Log($"[ResolveInput] defenderInputTime = {defenderInputTime}");
            if (defenderInputHandler.IsPlayer)
                playerController.OnHitResult(CurrentHit, isPerfect);
            else
            {
                var aiController = GetCurrentDefenderAI();
                if (aiController != null)
                {
                    aiController.OnHitResult(CurrentHit, isPerfect);
                }
            }

            CurrentDefenseResultShown = true; // 방어자 결과가 표시되었음을 설정 
        }
    }

    /// <summary>
    /// 전투 판정을 처리하는 메서드
    /// </summary>
    /// <param name="hitIndex">히트 인덱스 (발사체에서 전달받은 값)</param>
    private void EvaluateClashResult(int hitIndex)
    {
        bool atkPerfect = attackerPerfectInput ?? false;
        float atkTime = attackerInputTime ?? float.MaxValue;

        bool defPerfect = defenderPerfectInput ?? false;
        float defTime = defenderInputTime ?? float.MaxValue;

        // 방어 커맨드 여부 설정 - 실제 막기 상태 사용 (플레이어 + AI)
        bool guard = defenderInputHandler != null ? defenderInputHandler.IsGuardActive : false;
        if (!guard && battleExecutor != null)
        {
            guard = battleExecutor.IsAIGuardActive();
        }

        var ivr = new InputVersusResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성
        var resultVersus = ivr.GetResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성

        Debug.Log($"[CombatManager] 판정 결과: {resultVersus} (공격자 완벽: {atkPerfect}, 방어자 완벽: {defPerfect}, 막기: {guard}) - 히트 {hitIndex}");

        // 현재 공격자와 방어자 Character 찾기
        Character attacker = GetCurrentAttackerCharacter();
        Character defender = GetCurrentDefenderCharacter();

        if (attacker == null || defender == null)
        {
            Debug.LogError("[CombatManager] EvaluateClashResult에서 공격자 또는 방어자가 null입니다.");
            return;
        }
        
        // 현재 사용된 검술 커맨드 가져오기
        var currentCommand = CurrentResult?.Command;
        if (currentCommand == null)
        {
            Debug.LogError("[CombatManager] 현재 커맨드를 찾을 수 없습니다!");
            return;
        }
        
        // 피해량 계산 및 적용 - 올바른 히트 인덱스 사용
        if (battleExecutor == null)
        {
            battleExecutor = new BattleExecutor(this);
        }
        battleExecutor.ProcessDamageCalculation(attacker, defender, currentCommand, resultVersus, hitIndex);
        
        // 쳐내기 판정 시 공격자 자세 포인트 감소
        if (resultVersus == InputVersusResult.ResultType.Parry || resultVersus == InputVersusResult.ResultType.HalfParry)
        {
            int poiseDamage = defender.CharacterInitData.ParryPoiseDamage;
            
            Debug.Log($"[CombatManager] {resultVersus} 판정! {attacker.Name}의 Poise 감소 시작 (현재: {attacker.GetPoiseStatus()}, {defender.Name}의 ParryPoiseDamage: {poiseDamage})");
            
            attacker.LosePoise(poiseDamage); // 쳐내기 당했을 때 Poise 감소
            
            Debug.Log($"[CombatManager] {attacker.Name}의 Poise 감소 완료 (감소 후: {attacker.GetPoiseStatus()})");
            
            // 🆕 쳐내기 성공 시 AI 막기 해제 (방어자가 AI인 경우)
            if (defender is EnemyCharacter && battleExecutor != null)
            {
                battleExecutor.StopAIGuardOnParry(currentTurnContext);
            }
            
            // 중단 발생 확인
            if (attacker.IsInterrupted)
            {
                Debug.LogWarning($"[CombatManager] {attacker.Name}의 공격이 중단되었습니다!");
                TriggerInterrupt();
            }
        }
        else
        {
            Debug.Log($"[CombatManager] {resultVersus} 판정 - Poise 감소 없음");
        }

        ivr.OnHitVersusResult(hitIndex, resultVersus); // 히트 결과 UI에 표시 - 올바른 히트 인덱스 사용
        
        // 판정 결과에 따른 애니메이션 호출
        HandleClashResultAnimation(resultVersus);
        
        //////////////////////// 판정 구간 종료 /////////////////////////
        Debug.Log("<==[판정 정보]==>");
        Debug.Log($"[판정 정보]공격자 완벽 입력: {atkPerfect}, 입력 시간: {atkTime} / 방어자 완벽 입력: {defPerfect}, 입력 시간: {defTime}, 방어 커맨드: {guard}");
        Debug.Log($"[판정 정보]InputVersusResult 생성됨: new InputVersusResult({atkPerfect}, {atkTime}, {defPerfect}, {defTime}, {guard})");

        CurrentClashResultShown = true; // 판정 결과가 표시되었음을 설정
        
        // 초기화
        attackerPerfectInput = null;
        defenderPerfectInput = null;
        Debug.Log("[판정 구간 종료] 판정 결과 표시 및 초기화 완료");
    }
    
    /// <summary>
    /// 중단 발생 시 처리
    /// </summary>
    private void TriggerInterrupt()
    {
        // 중단 상태 설정
        isInterrupted = true;
        
        // 중단 애니메이션 재생
        var attackerController = currentTurnContext?.AttackerController ?? currentAttackerController;
        switch (attackerController)
        {
            case PlayerController playerCtrl:
                playerCtrl.OnInterrupted();
                break;
            case AIController aiCtrl:
                aiCtrl.OnInterrupted();
                break;
            default:
                if (IsPlayerAttacker && playerController != null)
                {
                    playerController.OnInterrupted();
                }
                else
                {
                    var fallbackAi = GetCurrentAttackerAI();
                    fallbackAi?.OnInterrupted();
                }
                break;
        }
        
        Debug.LogWarning("[CombatManager] 중단 발생! 턴이 조기 종료됩니다.");
    }
    
    /// <summary>
    /// 현재 히트에 대한 발사체 생성 및 발사
    /// </summary>
    /// <param name="isPerfect">완벽 입력 여부</param>
    internal void CreateProjectileForCurrentHit(bool isPerfect = false)
    {
        Debug.Log($"[CombatManager] 발사체 생성 시도 - 히트 {CurrentHit}, 이미 발사됨: {projectileLaunched[CurrentHit]}");
        
        if (projectileLaunched[CurrentHit]) 
        {
            Debug.Log($"[CombatManager] 히트 {CurrentHit}는 이미 발사됨 - 중복 발사 방지");
            return; // 중복 발사 방지
        }
        
        // ActionCommandData에서 발사체 정보 가져오기
        var command = CurrentResult.Command;
        
        // 🆕 완벽 입력 여부에 따른 발사체 프리팹 선택
        Debug.Log($"[PROJECTILE] 조건 확인: isPerfect={isPerfect}, perfectProjectilePrefab={command.perfectProjectilePrefab != null}");

        GameObject projectilePrefab = null;
        if (isPerfect && command.perfectProjectilePrefab != null)
        {
            projectilePrefab = command.perfectProjectilePrefab;
            Debug.Log($"[PROJECTILE] ✅ Perfect 발사체 선택: {projectilePrefab.name}");
        }

        if (projectilePrefab == null)
        {
            projectilePrefab = command.normalProjectilePrefab;
            if (projectilePrefab != null)
            {
                Debug.Log($"[PROJECTILE] ❌ 일반 발사체 선택: {projectilePrefab.name}");
            }
        }

        if (projectilePrefab == null)
        {
            Debug.LogError($"[CombatManager] {command.commandName}에 발사체 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        // 🆕 ProjectileManager 싱글톤을 통해 발사체 가져오기
        if (ProjectileManager.Instance == null)
        {
            Debug.LogError("[CombatManager] ProjectileManager.Instance가 null입니다!");
            return;
        }
        
        global::Projectile projectile = global::ProjectileManager.Instance.GetProjectile(projectilePrefab);
        projectile.Initialize(command, CurrentHit, IsPlayerAttacker, isPerfect, this);
        projectile.SetOwner(CurrentAttackerSlot, currentAttackerController, GetSpawnedActor(CurrentAttackerSlot));
        
        // ❌ 제거: 중복된 hitIndex 설정 (Initialize에서 이미 설정됨)
        // projectile.hitIndex = CurrentHit;
        
        // Controller 기반으로 위치 가져오기
        Transform attackerTransform = GetCurrentAttackerTransform();
        Transform defenderTransform = GetCurrentDefenderTransform();

        Vector3 attackerPos = attackerTransform != null ? attackerTransform.position : transform.position;
        Vector3 defenderPos = defenderTransform != null ? defenderTransform.position : transform.position;
        
        // 🆕 ProjectileManager를 통해 발사체 생성 위치 계산
        Vector3 spawnPosition = ProjectileManager.Instance.CalculateSpawnPosition(attackerPos, defenderPos);
        projectile.transform.position = spawnPosition;
        
        // 발사 방향 계산
        Vector3 direction = (defenderPos - attackerPos).normalized;
        
        // 🆕 디버그 로그 추가
        Debug.Log($"[CombatManager] 발사체 생성: attackerPos={attackerPos}, defenderPos={defenderPos}, spawnPos={spawnPosition}, direction={direction}");
        
        // 🆕 발사체 이벤트 구독
        projectile.OnProjectileHit += OnProjectileHit;
        
        // 발사체 발사 필드 설정
        projectile.direction = direction.normalized;
        projectile.isLaunched = true;
        
        // 발사 상태 기록
        projectileLaunched[CurrentHit] = true;
        
        Debug.Log($"[CombatManager] 히트 {CurrentHit} 발사체 발사 완료");
    }
    
    /// <summary>
    /// 발사체 충돌 시 호출되는 메서드 (기존 방식 - 호환성 유지)
    /// </summary>
    public void OnProjectileHit(Projectile projectile)
    {
        // 🆕 발사체의 히트 인덱스 사용
        int hitIdx = projectile.hitIndex;
        
        // 🆕 히트 인덱스 범위 체크
        if (hitIdx < 0 || hitIdx >= hitJudgmentCount.Length)
        {
            Debug.LogError($"[CombatManager] 🚨 히트 인덱스 범위 초과! hitIdx={hitIdx}, 배열 길이={hitJudgmentCount.Length} - 판정 무시");
            return;
        }
        
        // 🆕 판정 발생 횟수 카운트
        hitJudgmentCount[hitIdx]++;
        int currentCount = hitJudgmentCount[hitIdx];
        
        Debug.Log($"[CombatManager] 🚨 OnProjectileHit 호출 - 히트 {hitIdx}, 호출 횟수: {currentCount}, 배열 길이: {hitJudgmentCompleted.Length}");
        
        // 🆕 중복 판정 방지: 이미 판정이 완료된 히트는 무시
        if (hitJudgmentCompleted[hitIdx])
        {
            Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 이미 판정 완료됨 - 중복 판정 방지 (총 {currentCount}번 호출됨)");
            return;
        }
        
        Debug.Log($"[CombatManager] 🚨 발사체 충돌 - 즉시 판정 발생 (히트 {hitIdx}, {currentCount}번째 호출)");
        
        // 발사체 충돌 시 즉시 판정 발생 - 올바른 히트 인덱스 전달
        EvaluateClashResult(hitIdx);
        
        // 🆕 히트 판정 완료 상태 기록
        hitJudgmentCompleted[hitIdx] = true;
        Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 판정 완료 상태 설정됨");
    }
    
    /// <summary>
    /// 발사체 기반 최종 판정을 처리합니다
    /// </summary>
    /// <param name="projectile">충돌한 발사체</param>
    /// <param name="defenderPerfectSuccess">방어자 완벽 입력 성공 여부</param>
    public void TriggerProjectileBasedFinalJudgment(Projectile projectile, bool defenderPerfectSuccess)
    {
        Debug.Log($"[CombatManager] 🚨 발사체 기반 최종 판정 시작 - 히트 {projectile.hitIndex}, 공격자 완벽: {projectile.attackerPerfectInput}, 방어자 완벽: {defenderPerfectSuccess}");
        
        // 🆕 발사체의 히트 인덱스 사용
        int hitIdx = projectile.hitIndex;
        
        // 🆕 히트 인덱스 범위 체크
        if (hitIdx < 0 || hitIdx >= hitJudgmentCount.Length)
        {
            Debug.LogError($"[CombatManager] 🚨 히트 인덱스 범위 초과! hitIdx={hitIdx}, 배열 길이={hitJudgmentCount.Length} - 판정 무시");
            return;
        }
        
        // 🆕 중복 판정 방지: 이미 판정이 완료된 히트는 무시
        if (hitJudgmentCompleted[hitIdx])
        {
            Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 이미 판정 완료됨 - 중복 판정 방지");
            return;
        }
        
        // 🆕 공격자와 방어자 입력 판정 설정
        attackerPerfectInput = projectile.attackerPerfectInput;
        defenderPerfectInput = defenderPerfectSuccess;
        
        // 🆕 입력 시간 설정 (현재 시간 사용)
        attackerInputTime = Time.time;
        defenderInputTime = Time.time;
        
        Debug.Log($"[CombatManager] 🚨 발사체 기반 판정 정보 설정 - 공격자: {attackerPerfectInput}, 방어자: {defenderPerfectInput}");
        
        // 🆕 최종 판정 실행 - 올바른 히트 인덱스 전달
        EvaluateClashResult(hitIdx);
        
        // 🆕 방어자 완벽 입력 성공 시 발사체 제거 (연출 개선)
        if (defenderPerfectSuccess)
        {
            Debug.Log($"[CombatManager] 🚨 방어자 완벽 입력 성공 - 발사체 제거 (히트 {hitIdx})");
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
        
        // 🆕 히트 판정 완료 상태 기록
        hitJudgmentCompleted[hitIdx] = true;
        Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 발사체 기반 판정 완료");
        
        // 🆕 AI 막기 해제는 턴 종료 버퍼 이후에 DisableInput()에서 처리됨
    }
    
    // ❌ 제거: WaitForTurnEnd 코루틴 (PerformTurn에서 직접 처리)
    // private IEnumerator WaitForTurnEnd()
    // {
    //     Debug.Log($"[CombatManager] 턴 종료 대기 시작 - TurnEndBuffer: {GlobalConfig.Instance.TurnEndBuffer}초");
    //     isWaitingForTurnEnd = true;
    //     
    //     yield return new WaitForSeconds(GlobalConfig.Instance.TurnEndBuffer);
    //     
    //     Debug.Log($"[CombatManager] 턴 종료 대기 완료 - 턴 종료");
    //     // 🆕 턴 종료 플래그 설정
    //     turnEndRequested = true;
    //     isWaitingForTurnEnd = false;
    // }
    
    
    /// <summary>
    /// 클래시 결과에 따른 애니메이션 처리
    /// </summary>
    /// <param name="resultType">판정 결과 타입</param>
    private void HandleClashResultAnimation(InputVersusResult.ResultType resultType)
    {
        var defenderAI = GetCurrentDefenderAI();
        bool defenderIsPlayer = GetCurrentDefenderCharacter() is PlayerCharacter;

        switch (resultType)
        {
            case InputVersusResult.ResultType.Hit:
            case InputVersusResult.ResultType.PerfectAttack:
            case InputVersusResult.ResultType.GuardBreak:
                if (defenderIsPlayer)
                {
                    playerController?.OnBeHitted();
                }
                else
                {
                    defenderAI?.OnBeHitted();
                }
                break;
                
            case InputVersusResult.ResultType.Parry:
            case InputVersusResult.ResultType.HalfParry:
                if (defenderIsPlayer)
                {
                    playerController?.OnSuccessParry();
                }
                else
                {
                    defenderAI?.OnSuccessParry();
                }
                break;
                
            case InputVersusResult.ResultType.Guard:
                if (defenderIsPlayer)
                {
                    playerController?.OnPlayDefence();
                }
                else
                {
                    defenderAI?.OnPlayDefence();
                }
                break;
        }
    }
    
    internal bool CheckInterruptCondition()
    {
        return InterruptManager.IsInterrupted();        
    }

    
    /// <summary>
    /// 판정 결과에 따른 피해량 감소 비율 반환
    /// </summary>
    internal float GetDamageReduction(InputVersusResult.ResultType resultType)
    {
        float reduction;
        
        if (GameRule.Instance == null)
        {
            // GameRule이 없으면 기본값 사용
            switch (resultType)
            {
                case InputVersusResult.ResultType.Parry:
                    reduction = 0f; // 패리: 100% 감소 (완전 무효화)
                    break;
                case InputVersusResult.ResultType.HalfParry:
                    reduction = 0.5f; // 하프패리: 50% 감소
                    break;
                case InputVersusResult.ResultType.Guard:
                    reduction = 0.5f; // 막기: 50% 감소
                    break;
                case InputVersusResult.ResultType.GuardBreak:
                case InputVersusResult.ResultType.PerfectAttack:
                case InputVersusResult.ResultType.Hit:
                default:
                    reduction = 1f; // 일반 명중: 감소 없음
                    break;
            }
            Debug.Log($"[피해량 계산] 기본값 사용 - {resultType}: {reduction} (GameRule 없음)");
        }
        else
        {
            // GameRule에서 피해량 감소 비율 가져오기
            switch (resultType)
            {
                case InputVersusResult.ResultType.Parry:
                    reduction = GameRule.Instance.CalculateParryDamageReduction();
                    break;
                case InputVersusResult.ResultType.HalfParry:
                    reduction = GameRule.Instance.CalculateHalfParryDamageReduction();
                    break;
                case InputVersusResult.ResultType.Guard:
                    reduction = GameRule.Instance.CalculateGuardDamageReduction();
                    break;
                case InputVersusResult.ResultType.GuardBreak:
                    reduction = GameRule.Instance.CalculateGuardBreakDamageReduction();
                    break;
                case InputVersusResult.ResultType.PerfectAttack:
                case InputVersusResult.ResultType.Hit:
                default:
                    reduction = 1f; // 일반 명중: 감소 없음
                    break;
            }
            Debug.Log($"[피해량 계산] GameRule 사용 - {resultType}: {reduction}");
        }
        
        return reduction;
    }
    
    /// <summary>
    /// 막기 시 피해량 감소 비율 반환
    /// </summary>
    private float GetGuardDamageReduction()
    {
        if (GameRule.Instance != null)
        {
            return GameRule.Instance.CalculateGuardDamageReduction();
        }
        
        // GameRule이 없으면 기본값 사용
        return 0.5f; // 50% 감소
    }
    
    /// <summary>
    /// 방어력(DR) 적용하여 최종 피해량 계산
    /// </summary>
    internal int ApplyDefenseReduction(int damage, int defenderDR)
    {
        int finalDamage;
        
        if (GameRule.Instance != null)
        {
            finalDamage = GameRule.Instance.CalculateFinalDamage(damage, defenderDR);
            Debug.Log($"[피해량 계산] GameRule DR 적용: {damage} - {defenderDR} = {finalDamage}");
        }
        else
        {
            // GameRule이 없으면 기본 계산
            int minimumDamage = 1; // 기본 최소 피해량
            finalDamage = Mathf.Max(minimumDamage, damage - defenderDR);
            Debug.Log($"[피해량 계산] 기본 DR 적용: {damage} - {defenderDR} = {finalDamage} (최소: {minimumDamage})");
        }
        
        return finalDamage;
    }
    
    /// <summary>
    /// 전투 종료 처리 (새로운 방식)
    /// </summary>
    internal void EndBattle(BattleResult.BattleEndReason reason)
    {
        if (isBattleEnded) return; // 이미 전투가 종료된 경우 무시
        
        isBattleEnded = true;
        battleResult.EndReason = reason;
        battleResult.EndTime = Time.time;
        
        // 승리자와 패배자 결정
        Character winner = null;
        Character loser = null;
        string winnerName = "";
        
        if (reason == BattleResult.BattleEndReason.PlayerDefeated)
        {
            winner = CombatCharacterManager.Instance.CurrentEnemy;
            loser = CombatCharacterManager.Instance.PlayerCharacter;
            winnerName = "적";
        }
        else if (reason == BattleResult.BattleEndReason.EnemyDefeated)
        {
            winner = CombatCharacterManager.Instance.PlayerCharacter;
            loser = CombatCharacterManager.Instance.CurrentEnemy;
            winnerName = "플레이어";
        }
        
        // 전투 결과 저장
        battleResult.winner = winner;
        battleResult.loser = loser;
        
        // 캐릭터 비활성화 처리
        DisableCharacters();
        
        // UI에 전투 종료 및 승리자 표시 (디버그 패널)
        string resultMessage = "승리!"; // 승리자에게는 항상 승리 메시지
        if (CombatDebugDisplay.Instance != null)
        {
            if (CombatDebugDisplay.Instance.actionProgress != null)
            {
                CombatDebugDisplay.Instance.actionProgress.text = $"전투 종료! {winnerName} {resultMessage}";
            }
            CombatDebugDisplay.Instance.ShowInputPrompt("Restart 버튼을 눌러 다시 시작하세요");
        }
        
        // UI ActionMap 활성화 (GameInputManager 사용)
        if (GameInputManager.Instance != null)
        {
            GameInputManager.Instance.EnableUIActionMap();
        }
        
        // 🆕 전투 종료 후처리 (플레이어 상태 저장 및 보상 적용)
        if (CombatCharacterManager.Instance != null)
        {
            CombatCharacterManager.Instance.FinalizeBattle(battleResult);
        }
        
        // 전투 종료 이벤트 발생
        OnBattleEnded?.Invoke(battleResult);
        
        Debug.Log($"[CombatManager] 전투 종료 - 사유: {reason}, 승리자: {winnerName}, 결과: {resultMessage}");
        
        // 전투 결과를 ResultScene에 전달하고 Scene 전환
        TransitionToResultScene();
    }
    
    /// <summary>
    /// 전투 종료 시 캐릭터들 비활성화
    /// </summary>
    private void DisableCharacters()
    {
        // 플레이어 Controller 비활성화
        foreach (var actor in spawnedActors)
        {
            if (actor != null)
            {
                actor.SetActive(false);
            }
        }
        Debug.Log("[CombatManager] 모든 전투 Actor 비활성화");
        
        // 입력 핸들러 비활성화
        if (attackerInputHandler != null)
        {
            attackerInputHandler.DisableInput();
        }
        
        if (defenderInputHandler != null)
        {
            defenderInputHandler.DisableInput();
        }
    }
    
    /// <summary>
    /// 전투 재시작 시 캐릭터들 재활성화
    /// </summary>
    private void EnableCharacters()
    {
        foreach (var actor in spawnedActors)
        {
            if (actor != null)
            {
                actor.SetActive(true);
            }
        }
        Debug.Log("[CombatManager] 모든 전투 Actor 재활성화");
    }
    
    /// <summary>
    /// 전투 처음부터 다시 시작 (UI 버튼에서 호출)
    /// </summary>
    public void RestartBattle()
    {
        Debug.Log("[CombatManager] 재시작 버튼 클릭됨!");
        Debug.Log("[CombatManager] 전투 다시 시작 요청");
        
        // 🆕 모든 코루틴 중지 (DelayedSceneTransition 포함, 재시작 시 자동 Scene 전환 취소)
        StopAllCoroutines();
        
        // EventSystem 상태 확인
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("[CombatManager] EventSystem이 없습니다!");
        }
        else
        {
            Debug.Log($"[CombatManager] EventSystem 활성화 상태: {eventSystem.enabled}");
        }
        
        // 1. 전투 상태 초기화
        isBattleEnded = false;
        isInterrupted = false;
        CurrentHit = 0;
        CurrentAttackResultShown = false;
        CurrentDefenseResultShown = false;
        CurrentClashResultShown = false;
        windowPrompted = false;
        floatingTextShown = false;
        
        // 2. 입력 핸들러 초기화
        attackerInputHandler?.ResetInputState();
        defenderInputHandler?.ResetInputState();
        attackerInputHandler?.DisableInput();
        defenderInputHandler?.DisableInput();
        
        // 3. 캐릭터 재활성화 및 스테이터스 초기화
        EnableCharacters();
        
        // 🆕 슬롯 기반으로 모든 캐릭터 스테이터스 초기화
        if (CombatCharacterManager.Instance != null)
        {
            var characterManager = CombatCharacterManager.Instance;
            
            // TeamA 모든 슬롯의 캐릭터 초기화
            foreach (var slot in characterManager.EnumerateAllSlots())
            {
                if (slot?.Character != null)
                {
                    slot.Character.InitializeRuntimeStats();
                    Debug.Log($"[CombatManager] 스테이터스 초기화 - Team:{slot.Team} {slot.Character.Name} - HP: {slot.Character.GetHPStatus()}, Poise: {slot.Character.GetPoiseStatus()}");
                }
            }
        }
        
        // 4. UI 초기화
        CombatHUD.Instance?.ClearHUD();
        CombatDebugDisplay.Instance?.ClearDebugResults();
        CombatDebugDisplay.Instance?.ShowInputPrompt("전투 다시 시작!");
        
        // 5. 전투 결과 초기화
        battle = new BattleState(this);
        battle.InitializeResult();
        battleExecutor = new BattleExecutor(this);
        
        // 6. 전투 재시작
        StopAllCoroutines();
        StartCoroutine(RunCombat());
        
        Debug.Log("[CombatManager] 전투 다시 시작 완료");
    }

    public void Update()
    {
        if (attackerInputHandler != null)
        {
            CombatDebugDisplay.Instance?.SetPlayerActionInputCooldown(attackerInputHandler.NextAllowedInputTime - Time.time);
        }
    }
    
    internal CombatTurnContext BuildTurnContext(CombatCharacterManager.CombatTeam attackerTeam)
    {
        var manager = CombatCharacterManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[CombatManager] BuildTurnContext 실패 - CombatCharacterManager가 없습니다.");
            return null;
        }

        var attackerSlot = manager.GetLeaderSlot(attackerTeam);
        if (attackerSlot == null || attackerSlot.Character == null)
        {
            Debug.LogError($"[CombatManager] BuildTurnContext 실패 - {attackerTeam} 공격자 슬롯이 비어 있습니다.");
            return null;
        }

        var defenderSlot = manager.GetOpponentLeaderSlot(attackerTeam);
        if (defenderSlot == null || defenderSlot.Character == null)
        {
            Debug.LogError($"[CombatManager] BuildTurnContext 실패 - {attackerTeam} 상대 슬롯이 비어 있습니다.");
            return null;
        }

        attackerInputHandler?.BindToSlot(attackerSlot);
        defenderInputHandler?.BindToSlot(defenderSlot);

        var context = new CombatTurnContext(attackerSlot, defenderSlot, attackerInputHandler, defenderInputHandler);
        return context;
    }

    /// <summary>
    /// 플레이어 컨트롤러를 반환합니다
    /// </summary>
    public PlayerController GetPlayerController()
    {
        var slot = CombatCharacterManager.Instance?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA);
        if (slot?.Controller is PlayerController controller)
        {
            return controller;
        }

        return playerController;
    }
    
    /// <summary>
    /// 적 컨트롤러를 반환합니다
    /// </summary>
    public AIController GetNonPlayerController()
    {
        var slot = CombatCharacterManager.Instance?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB);
        if (slot?.Controller is AIController aiController)
        {
            return aiController;
        }

        return enemyController;
    }
    
    /// <summary>
    /// 전투 종료 후 ResultScene으로 전환
    /// </summary>
    private void TransitionToResultScene()
    {
        // 약간의 지연 후 Scene 전환 (결과 확인 시간)
        StartCoroutine(DelayedSceneTransition());
    }
    
    /// <summary>
    /// 지연된 Scene 전환 Coroutine
    /// </summary>
    private System.Collections.IEnumerator DelayedSceneTransition()
    {
        // 2초 대기 (플레이어가 전투 종료 결과를 확인할 시간)
        yield return new WaitForSeconds(2f);
        
        if (SceneFlowController.Instance != null)
        {
            Debug.Log("[CombatManager] ResultScene으로 전환합니다.");
            // Flow Controller가 데이터 전달 + Scene 전환 담당
            SceneFlowController.Instance.ShowResultFlow(battleResult);
        }
        else
        {
            Debug.LogError("[CombatManager] SceneFlowController를 찾을 수 없습니다!");
        }
    }


    internal class BattleState
    {
        public CombatManager Owner { get; }

        public CombatTurnContext CurrentTurnContext { get; set; }
        public CombatCharacterManager.CombatantSlot CurrentAttackerSlot { get; set; }
        public CombatCharacterManager.CombatantSlot CurrentDefenderSlot { get; set; }
        public ICombatController CurrentAttackerController { get; set; }
        public ICombatController CurrentDefenderController { get; set; }

        public bool? AttackerPerfectInput { get; set; }
        public bool? DefenderPerfectInput { get; set; }
        public float? AttackerInputTime { get; set; }
        public float? DefenderInputTime { get; set; }

        public float CurrentTurnDuration { get; set; }
        public int CurrentTurnNumber { get; set; } = 1;
        public int CurrentHit { get; set; }

        public bool CurrentAttackResultShown { get; set; }
        public bool CurrentDefenseResultShown { get; set; }
        public bool CurrentClashResultShown { get; set; }
        public bool WindowPrompted { get; set; }
        public bool FloatingTextShown { get; set; }

        public bool IsInterrupted { get; set; }
        public bool IsBattleEnded { get; set; }

        public BattleResult BattleResult { get; }

        public bool[] ProjectileLaunched { get; private set; }
        public bool[] HitJudgmentCompleted { get; private set; }
        public int[] HitJudgmentCount { get; private set; }

        public ICombatController CurrentController { get; set; }
        public CharacterCommandResult CurrentResult { get; set; }

        public BattleState(CombatManager owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            BattleResult = new BattleResult();
        }

        public void InitializeResult()
        {
            BattleResult.InitializeBattle();
        }

        public void ResetTurnState()
        {
            AttackerPerfectInput = null;
            DefenderPerfectInput = null;
            AttackerInputTime = null;
            DefenderInputTime = null;
            CurrentAttackResultShown = false;
            CurrentDefenseResultShown = false;
            CurrentClashResultShown = false;
            WindowPrompted = false;
            FloatingTextShown = false;
        }

        public void EnsureHitArrays(int hitCount)
        {
            if (hitCount <= 0)
            {
                ProjectileLaunched = Array.Empty<bool>();
                HitJudgmentCompleted = Array.Empty<bool>();
                HitJudgmentCount = Array.Empty<int>();
                return;
            }

            if (ProjectileLaunched == null || ProjectileLaunched.Length != hitCount)
            {
                ProjectileLaunched = new bool[hitCount];
            }

            if (HitJudgmentCompleted == null || HitJudgmentCompleted.Length != hitCount)
            {
                HitJudgmentCompleted = new bool[hitCount];
            }

            if (HitJudgmentCount == null || HitJudgmentCount.Length != hitCount)
            {
                HitJudgmentCount = new int[hitCount];
            }
            else
            {
                Array.Clear(HitJudgmentCount, 0, HitJudgmentCount.Length);
            }

            Array.Clear(ProjectileLaunched, 0, ProjectileLaunched.Length);
            Array.Clear(HitJudgmentCompleted, 0, HitJudgmentCompleted.Length);
        }
    }

}

