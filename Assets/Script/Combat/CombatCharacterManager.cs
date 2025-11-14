using UnityEngine;
using System.Collections.Generic;
using BladeAction.Item;

/// <summary>
/// 현재 전투 중인 양측 캐릭터의 런타임 인스턴스 관리
/// PlayerCharacter와 EnemyCharacter(s)를 전투 시작 시 생성하고
/// 전투 종료 시 플레이어 상태를 동기화합니다.
/// CombatScene에 배치, DontDestroyOnLoad 적용 안함 (Scene 전용)
/// </summary>
public class CombatCharacterManager : MonoBehaviour
{
    public static CombatCharacterManager Instance { get; private set; }
    
    // === 전투 참가자 정보 (누가 싸우는가) ===
    public string PlayerInstanceId { get; private set; }
    public List<string> EnemyInstanceIds { get; private set; }
    public List<string> TeamAInstanceIds { get; private set; } = new List<string>();
    public List<string> TeamBInstanceIds { get; private set; } = new List<string>();
    
    // === 전투 중인 캐릭터 인스턴스 (참조만 보관, 소유 안 함) ===
    /// <summary>
    /// PlayerCharacterManager의 영속 PlayerCharacter 참조
    /// </summary>
    public PlayerCharacter PlayerCharacter => PlayerCharacterManager.Instance?.PlayerCharacter;
    
    /// <summary>
    /// NonPlayerCharacterManager의 Enemy 인스턴스 참조 목록
    /// </summary>
    public List<EnemyCharacter> EnemyCharacters
    {
        get
        {
            if (teamBSlots == null || teamBSlots.Count == 0)
                return null;

            var enemies = new List<EnemyCharacter>();
            foreach (var slot in teamBSlots)
            {
                if (slot?.Character is EnemyCharacter enemy)
                {
                    enemies.Add(enemy);
                }
            }
            return enemies;
        }
    }
    
    // === 편의 프로퍼티 ===
    public EnemyCharacter CurrentEnemy => EnemyCharacters != null && EnemyCharacters.Count > 0 ? EnemyCharacters[0] : null;

    // === 팀 슬롯 구조 ===
    [System.Serializable]
    public class CombatantSlot
    {
        public string InstanceId { get; }
        public Character Character { get; private set; }
        public ICombatController Controller { get; private set; }
        public bool IsLeader { get; }
        public CombatTeam Team { get; }
        public bool HasController => Controller != null;
        public bool HasCharacter => Character != null;
        public CharacterType? SlotCharacterType
        {
            get
            {
                if (Character is PlayerCharacter) return CharacterType.Player;
                if (Character is EnemyCharacter) return CharacterType.Enemy;
                return null;
            }
        }
        public bool IsPlayerSlot => SlotCharacterType == CharacterType.Player;
        public bool IsEnemySlot => SlotCharacterType == CharacterType.Enemy;

        public CombatantSlot(string instanceId, CombatTeam team, bool isLeader)
        {
            InstanceId = instanceId;
            Team = team;
            IsLeader = isLeader;
        }

        public void BindCharacter(Character character)
        {
            Character = character;
        }

        public void BindController(ICombatController controller)
        {
            Controller = controller;
        }

        public override string ToString()
        {
            string characterName = Character != null ? Character.Name : "null";
            string controllerName = Controller != null ? Controller.GetType().Name : "null";
            return $"Slot[{Team}:{InstanceId}] Character={characterName}, Controller={controllerName}, Leader={IsLeader}";
        }
    }

    public enum CombatTeam
    {
        TeamA = 0,
        TeamB = 1
    }

    private readonly List<CombatantSlot> teamASlots = new List<CombatantSlot>();
    private readonly List<CombatantSlot> teamBSlots = new List<CombatantSlot>();

    public IReadOnlyList<CombatantSlot> TeamA => teamASlots;
    public IReadOnlyList<CombatantSlot> TeamB => teamBSlots;

    public IEnumerable<CombatantSlot> EnumerateTeamSlots(CombatTeam team)
    {
        return GetTeamSlots(team);
    }

    public IEnumerable<CombatantSlot> EnumerateAllSlots()
    {
        foreach (var slot in teamASlots)
        {
            yield return slot;
        }
        foreach (var slot in teamBSlots)
        {
            yield return slot;
        }
    }
    
    // === 초기화 ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ⚠️ DontDestroyOnLoad 적용 안함 (Scene 전용)
            // Scene 언로드 시 자동으로 파괴됨
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // === 전투 초기화 ===
    /// <summary>
    /// 전투를 초기화합니다
    /// 전투 참가자 ID를 저장하고, 영속 Character 인스턴스를 참조합니다.
    /// </summary>
    /// <param name="playerInstanceId">플레이어 Character의 Instance ID</param>
    /// <param name="enemyInstanceIds">적 Character(s)의 Instance ID 배열</param>
    public void InitializeBattle(string playerInstanceId, params string[] enemyInstanceIds)
    {
        var teamAIds = new List<string>();
        if (!string.IsNullOrEmpty(playerInstanceId))
        {
            teamAIds.Add(playerInstanceId);
        }
        InitializeBattle(teamAIds, enemyInstanceIds);
    }

    /// <summary>
    /// 전투를 초기화합니다 (팀 단위 입력)
    /// </summary>
    /// <param name="teamAIds">엔트리 순서대로 구성된 A팀 캐릭터 ID 목록</param>
    /// <param name="teamBIds">엔트리 순서대로 구성된 B팀 캐릭터 ID 목록</param>
    public void InitializeBattle(IList<string> teamAIds, IList<string> teamBIds)
    {
        Debug.Log($"[CombatCharacterManager] === 전투 초기화 시작 ===");
        if (teamAIds == null || teamAIds.Count == 0)
        {
            Debug.LogError("[CombatCharacterManager] 팀 A에 전투자가 없습니다.");
            return;
        }

        if (teamBIds == null || teamBIds.Count == 0)
        {
            Debug.LogError("[CombatCharacterManager] 팀 B에 전투자가 없습니다.");
            return;
        }

        teamASlots.Clear();
        teamBSlots.Clear();

        TeamAInstanceIds = new List<string>(teamAIds);
        TeamBInstanceIds = new List<string>(teamBIds);

        PlayerInstanceId = teamAIds[0];
        EnemyInstanceIds = new List<string>(teamBIds);

        Debug.Log($"[CombatCharacterManager] 전투 참가자 (TeamA): [{string.Join(", ", TeamAInstanceIds)}]");
        Debug.Log($"[CombatCharacterManager] 전투 참가자 (TeamB): [{string.Join(", ", TeamBInstanceIds)}]");

        // Character 참조 확인 및 슬롯 생성
        for (int i = 0; i < teamAIds.Count; i++)
        {
            var slot = CreateSlot(teamAIds[i], CombatTeam.TeamA, i == 0);
            if (slot == null)
            {
                Debug.LogError($"[CombatCharacterManager] TeamA 슬롯 생성 실패 (index: {i})");
                return;
            }
            teamASlots.Add(slot);
        }

        for (int i = 0; i < teamBIds.Count; i++)
        {
            var slot = CreateSlot(teamBIds[i], CombatTeam.TeamB, i == 0);
            if (slot == null)
            {
                Debug.LogError($"[CombatCharacterManager] TeamB 슬롯 생성 실패 (index: {i})");
                return;
            }
            teamBSlots.Add(slot);
        }

        if (teamASlots.Count > 0 && teamBSlots.Count > 0)
        {
            var leaderA = teamASlots[0].Character?.Name ?? "Unknown";
            var leaderB = teamBSlots[0].Character?.Name ?? "Unknown";
            Debug.Log($"[CombatCharacterManager] === 전투 초기화 완료: {leaderA} vs {leaderB} (TeamA {teamASlots.Count}명, TeamB {teamBSlots.Count}명) ===");
        }
    }
    
    // === 전투 종료 ===
    /// <summary>
    /// 전투를 종료하고 플레이어 상태를 저장합니다
    /// </summary>
    /// <param name="result">전투 결과</param>
    public void FinalizeBattle(BattleResult result)
    {
        Debug.Log($"[CombatCharacterManager] 전투 종료: {(result.isVictory ? "승리" : "패배")}");
        
        if (PlayerCharacterManager.Instance == null)
        {
            Debug.LogError("[CombatCharacterManager] PlayerCharacterManager.Instance가 null입니다!");
            return;
        }
        
        // 1. 플레이어 상태 저장 (영속 인스턴스이므로 자동 동기화됨)
        PlayerCharacterManager.Instance.SyncPlayerStateAfterBattle();
        
        // 2. 보상 적용
        if (result.isVictory)
        {
            if (result.goldReward > 0)
            {
                PlayerCharacterManager.Instance.AddGold(result.goldReward);
            }
            
            if (result.expReward > 0)
            {
                PlayerCharacterManager.Instance.AddExperience(result.expReward);
            }
            
            Debug.Log($"[CombatCharacterManager] 보상 획득: 골드 +{result.goldReward}, 경험치 +{result.expReward}");
        }
        
        // 3. Character 인스턴스는 영속 관리자가 계속 보관함
        // 4. 전투 참가자 ID는 Scene 언로드 시 자동 소멸
        Debug.Log("[CombatCharacterManager] 전투 후처리 완료");
    }
    
    // === Controller 연결 ===
    /// <summary>
    /// Controller를 Character에 연결합니다
    /// </summary>
    /// <param name="type">캐릭터 타입 (Player/Enemy)</param>
    /// <param name="controller">연결할 Controller</param>
    public void ConnectController(CharacterType type, ICombatController controller)
    {
        if (type == CharacterType.Player)
        {
            ConnectController(CombatTeam.TeamA, 0, controller);
        }
        else if (type == CharacterType.Enemy)
        {
            ConnectController(CombatTeam.TeamB, 0, controller);
        }
    }

    /// <summary>
    /// 팀/슬롯 인덱스 기반 Controller 연결
    /// </summary>
    public void ConnectController(CombatTeam team, int slotIndex, ICombatController controller)
    {
        var slots = GetTeamSlots(team);
        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            Debug.LogError($"[CombatCharacterManager] ConnectController 실패 - 잘못된 슬롯 인덱스: {slotIndex} (team: {team})");
            return;
        }

        var slot = slots[slotIndex];
        slot.BindController(controller);

        if (slot.Character is PlayerCharacter playerChar && controller is PlayerController playerController)
        {
            playerChar.SetController(playerController);
            Debug.Log($"[CombatCharacterManager] PlayerController 연결: {playerChar.Name} (ID: {playerChar.InstanceId})");
        }
        else if (slot.Character is EnemyCharacter enemyChar && controller is AIController aiController)
        {
            enemyChar.SetController(aiController);
            aiController.BindCombatantSlot(slot);
            Debug.Log($"[CombatCharacterManager] AIController 연결: {enemyChar.Name} (ID: {enemyChar.InstanceId}, 슬롯: {slotIndex})");
        }
        else if (slot.Character != null)
        {
            Debug.LogWarning($"[CombatCharacterManager] Controller 연결 - 타입 매칭이 필요합니다. Character: {slot.Character.GetType().Name}, Controller: {controller?.GetType().Name}");
        }
    }

    public CombatantSlot GetLeaderSlot(CombatTeam team)
    {
        var slots = GetTeamSlots(team);
        return slots.Count > 0 ? slots[0] : null;
    }

    public CombatantSlot GetOpponentLeaderSlot(CombatTeam team)
    {
        return team == CombatTeam.TeamA ? GetLeaderSlot(CombatTeam.TeamB) : GetLeaderSlot(CombatTeam.TeamA);
    }

    public CombatantSlot GetOpponentSlot(CombatantSlot slot)
    {
        if (slot == null)
        {
            return null;
        }

        return GetLeaderSlot(slot.Team == CombatTeam.TeamA ? CombatTeam.TeamB : CombatTeam.TeamA);
    }

    public CombatantSlot FindSlotByController(ICombatController controller)
    {
        if (controller == null)
        {
            return null;
        }

        foreach (var slot in teamASlots)
        {
            if (slot.Controller == controller)
            {
                return slot;
            }
        }

        foreach (var slot in teamBSlots)
        {
            if (slot.Controller == controller)
            {
                return slot;
            }
        }

        return null;
    }

    public bool TryFindSlot(ICombatController controller, out CombatantSlot slot)
    {
        slot = FindSlotByController(controller);
        return slot != null;
    }

    public CombatantSlot FindSlotByCharacter(Character character)
    {
        if (character == null)
        {
            return null;
        }

        foreach (var slot in teamASlots)
        {
            if (slot.Character == character)
            {
                return slot;
            }
        }

        foreach (var slot in teamBSlots)
        {
            if (slot.Character == character)
            {
                return slot;
            }
        }

        return null;
    }

    public bool TryFindSlot(Character character, out CombatantSlot slot)
    {
        slot = FindSlotByCharacter(character);
        return slot != null;
    }

    public CombatantSlot GetCombatantSlot(CombatTeam team, int slotIndex)
    {
        var slots = GetTeamSlots(team);
        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            return null;
        }
        return slots[slotIndex];
    }

    public Character GetCombatantCharacter(CombatTeam team, int slotIndex)
    {
        return GetCombatantSlot(team, slotIndex)?.Character;
    }

    private List<CombatantSlot> GetTeamSlots(CombatTeam team)
    {
        return team == CombatTeam.TeamA ? teamASlots : teamBSlots;
    }

    private CombatantSlot CreateSlot(string instanceId, CombatTeam team, bool isLeader)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("[CombatCharacterManager] InstanceId가 비어 있습니다.");
            return null;
        }

        var slot = new CombatantSlot(instanceId, team, isLeader);
        var character = ResolveCharacter(instanceId);
        if (character == null)
        {
            Debug.LogError($"[CombatCharacterManager] Character 참조 실패: {instanceId}");
            return null;
        }

        slot.BindCharacter(character);
        return slot;
    }

    private Character ResolveCharacter(string instanceId)
    {
        if (PlayerCharacterManager.Instance?.PlayerCharacter != null &&
            PlayerCharacterManager.Instance.PlayerCharacter.InstanceId == instanceId)
        {
            return PlayerCharacterManager.Instance.PlayerCharacter;
        }

        if (NonPlayerCharacterManager.Instance == null)
        {
            Debug.LogError("[CombatCharacterManager] NonPlayerCharacterManager.Instance가 null입니다!");
            return null;
        }

        return NonPlayerCharacterManager.Instance.GetCharacter(instanceId);
    }
    
    private void OnDestroy()
    {
        Debug.Log("[CombatCharacterManager] OnDestroy - Scene 언로드 시 자동 파괴됨");
    }
}
