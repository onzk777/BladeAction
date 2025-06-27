// GlobalConfig.cs (��ü �����͸���)

using UnityEngine;

[CreateAssetMenu(fileName = "GlobalConfig", menuName = "Combat/GlobalConfig", order = 0)]
public class GlobalConfig : ScriptableObject
{
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
    [SerializeField] private float npcActionPerfectRate = 0.3f;
    public float NpcActionPerfectRate => npcActionPerfectRate;

    [Header("ActionInputCooldown")]
    [Tooltip("�Ϻ� �Է��� �ƴ� �Է��� �ϰ� �Ǹ� �� �ð�(��)���� �Է��� ������.")]
    [SerializeField] private float actionInputCooldown = 0.5f;
    public float ActionInputCooldown => actionInputCooldown; // �÷��̾ �� �ൿ �� �Ϻ� Ÿ�ݿ� ������ �Է��� �ϸ� �� �ð� ���� �Է��� ����

    // ���� �ʿ��� ������ ������ ���⿡ �߰�
}
