// GlobalConfig.cs (��ü �����͸���)

using UnityEngine;

[CreateAssetMenu(fileName = "GlobalConfig", menuName = "Combat/GlobalConfig", order = 0)]
public class GlobalConfig : ScriptableObject
{
    [Header("Timing Settings")]
    [Tooltip("�÷��̾�/AI�� �Է� ������ ���� ��ٸ��� �ð�(��)")]
    [SerializeField] private float inputWindowSeconds = 3f;
    public float InputWindowSeconds => inputWindowSeconds;

    [Header("AI Settings")]
    [Tooltip("AI�� �Ϻ� �Է� Ÿ�̹��� ������ Ȯ��(0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float npcActionPerfectRate = 0.3f;
    public float NpcActionPerfectRate => npcActionPerfectRate;
    public float ActionInputCooldown = 0.5f; // �÷��̾ �� �ൿ �� �Ϻ� Ÿ�ݿ� ������ �Է��� �ϸ� �� �ð� ���� �Է��� ����

    // ���� �ʿ��� ������ ������ ���⿡ �߰�
}
