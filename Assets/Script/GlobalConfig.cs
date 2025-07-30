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
    [Tooltip("플레이어/AI가 입력 윈도우 동안 기다리는 시간(초)")]
    [SerializeField] private float turnDurationSeconds = 3f;
    public float TurnDurationSeconds => turnDurationSeconds;
    [SerializeField] private float inputBufferStartSeconds = 3f;
    public float InputBufferStartSeconds => inputBufferStartSeconds;
    [SerializeField] private float inputBufferEndSeconds = 3f;
    public float InputBufferEndSeconds => inputBufferEndSeconds;

    [Header("AI Settings")]
    [Tooltip("AI가 완벽 입력 타이밍을 성공할 확률(0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float npcActionPerfectRate = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float npcDefensePerfectRate = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float npcInputDifficulty = 0.5f;
    public float NpcActionPerfectRate => npcActionPerfectRate;
    public float NpcDefensePerfectRate => npcDefensePerfectRate;
    public float NpcInputDifficulty => npcInputDifficulty;

    [Header("ActionInputCooldown")]
    [Tooltip("완벽 입력이 아닌 입력을 하게 되면 이 시간(초)동안 입력이 막힌다.")]
    [SerializeField] private float actionInputCooldown_Default = 0.8f;
    public float ActionInputCooldown_Default => actionInputCooldown_Default; // 플레이어가 턴 행동 중 완벽 타격에 실패한 입력을 하면 이 시간 동안 입력이 막힘
    [SerializeField] private float actionInputCooldown_Perfect = 0.25f;
    public float ActionInputCooldown_Perfect => actionInputCooldown_Perfect; // 플레이어가 턴 행동 중 완벽 타격에 실패한 입력을 하면 이 시간 동안 입력이 막힘

    // 향후 필요한 설정이 있으면 여기에 추가
}
