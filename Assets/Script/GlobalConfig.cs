// GlobalConfig.cs (��ü �����͸���)

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
                // Resources ������ GlobalConfig.asset �� �־�� ��!
                _instance = Resources.Load<GlobalConfig>("GlobalConfig");
                if (_instance == null)
                {
                    Debug.LogError("[GlobalConfig] Resources/GlobalConfig.asset �� ã�� �� �����ϴ�!");
                }
            }
            return _instance;
        }
    }
    

    [Header("Timing Settings")]
    [Tooltip("�÷��̾�/AI�� �Է� ������ ���� ��ٸ��� �ð�(��)")]
    [SerializeField] private float turnDurationSeconds = 3f;
    public float TurnDurationSeconds => turnDurationSeconds;
    [SerializeField] private float inputBufferStartSeconds = 3f;
    public float InputBufferStartSeconds => inputBufferStartSeconds;
    [SerializeField] private float inputBufferEndSeconds = 3f;
    public float InputBufferEndSeconds => inputBufferEndSeconds;

    [Header("AI Settings")]
    [Tooltip("AI�� �Ϻ� �Է� Ÿ�̹��� ������ Ȯ��(0~1)")]
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
    [Tooltip("�Ϻ� �Է��� �ƴ� �Է��� �ϰ� �Ǹ� �� �ð�(��)���� �Է��� ������.")]
    [SerializeField] private float actionInputCooldown_Default = 0.8f;
    public float ActionInputCooldown_Default => actionInputCooldown_Default; // �÷��̾ �� �ൿ �� �Ϻ� Ÿ�ݿ� ������ �Է��� �ϸ� �� �ð� ���� �Է��� ����
    [SerializeField] private float actionInputCooldown_Perfect = 0.25f;
    public float ActionInputCooldown_Perfect => actionInputCooldown_Perfect; // �÷��̾ �� �ൿ �� �Ϻ� Ÿ�ݿ� ������ �Է��� �ϸ� �� �ð� ���� �Է��� ����

    // ���� �ʿ��� ������ ������ ���⿡ �߰�
}
