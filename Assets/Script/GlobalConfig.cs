// GlobalConfig.cs (전체 리팩터링본)

using UnityEngine;

[CreateAssetMenu(fileName = "GlobalConfig", menuName = "Combat/GlobalConfig", order = 0)]
public class GlobalConfig : ScriptableObject
{
    private static GlobalConfig _instance;

    public static GlobalConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                // Resources 폴더에 GlobalConfig.asset 이 있어야 함!
                _instance = Resources.Load<GlobalConfig>("GlobalConfig");
                if (_instance == null)
                {
                    Debug.LogError("[GlobalConfig] Resources/GlobalConfig.asset 을 찾을 수 없습니다!");
                }
            }
            return _instance;
        }
    }
    

    [Header("Timing Settings")]
    [SerializeField] private float inputBufferStartSeconds = 0.1f;
    public float InputBufferStartSeconds => inputBufferStartSeconds;
    [SerializeField] private float inputBufferEndSeconds = 0.1f;
    public float InputBufferEndSeconds => inputBufferEndSeconds;
    [SerializeField] private float additionalTurnDuration = 0f;
    [Tooltip("마지막 히트 완료 후 추가 턴 지속 시간 (초) - 빠른 템포 테스트용")]
    public float AdditionalTurnDuration => additionalTurnDuration;
    
    [Tooltip("전투 시작 후 첫 턴 시작 전 대기 시간 (초)")]
    [SerializeField] private float combatStartDelay = 0.2f;
    public float CombatStartDelay => combatStartDelay;
    
    [Tooltip("턴 전환 시 대기 시간")]
    [SerializeField] private float turnEndBuffer = 0.1f;
    public float TurnEndBuffer => turnEndBuffer;
    
    [Tooltip("피격 애니메이션 완료 대기 시간")]
    [SerializeField] private float animationWaitTime = 0.5f;
    public float AnimationWaitTime => animationWaitTime;

    [Header("AI Settings")]
    [Tooltip("AI가 완벽 입력 타이밍을 성공할 확률(0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float npcAttackPerfectRate = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float npcParryPerfectRate = 0.5f;
    [Range(0f, 1f)]
    [Tooltip("AI가 막기를 시도할 확률(0~1)")]
    [SerializeField] private float npcGuardAttemptRate = 0.3f;
    [Tooltip("AI가 막기 중에도 쳐내기를 시도할지 여부")]
    [SerializeField] private bool npcParryWhileGuarding = true;
    public float NpcAttackPerfectRate => npcAttackPerfectRate;
    public float NpcParryPerfectRate => npcParryPerfectRate;
    public float NpcGuardAttemptRate => npcGuardAttemptRate;
    public bool NpcParryWhileGuarding => npcParryWhileGuarding;

    [Header("자세 포인트 시스템")]
    [Tooltip("플레이어와 AI가 보유할 수 있는 최대 자세 포인트")]
    [SerializeField] private float posturePointsMax = 100f;
    [Tooltip("쳐내기 당했을 때 감소하는 자세 포인트")]
    [SerializeField] private float posturePointsLossOnParry = 25f;
    [Tooltip("중단 발생 후 대기 시간(초)")]
    [SerializeField] private float interruptWaitSec = 1.5f;
    public float PosturePointsMax => posturePointsMax;
    public float PosturePointsLossOnParry => posturePointsLossOnParry;
    public float InterruptWaitSec => interruptWaitSec;

    [Header("ActionInputCooldown")]
    [Tooltip("완벽 입력이 아닌 입력을 하게 되면 이 시간(초)동안 입력이 막힌다.")]
    [SerializeField] private float actionInputCooldown_Default = 0.8f;
    public float ActionInputCooldown_Default => actionInputCooldown_Default; // 플레이어가 턴 행동 중 완벽 타격에 실패한 입력을 하면 이 시간 동안 입력이 막힘
    [Tooltip("완벽 입력을 성공하면 이 시간(초)동안 입력이 막힌다.")]
    [SerializeField] private float actionInputCooldown_Perfect = 0.25f;
    public float ActionInputCooldown_Perfect => actionInputCooldown_Perfect; // 플레이어가 턴 행동 중 완벽 타격에 성공한 입력을 하면 이 시간 동안 입력이 막힘

    // 향후 필요한 설정이 있으면 여기에 추가
}
