using UnityEngine;

public enum CommandType
{
    Slash_Horizontal,
    Slash_Vertical,
    Thrust,
    Defend,
    SecretArt // ���� �Է� �Ұ�
}

[CreateAssetMenu(fileName = "ActionCommandData", menuName = "Combat/ActionCommandData", order = 1)]
public class ActionCommandData : ScriptableObject
{
    public CommandType commandType;
    public string commandName;
    public string animationName;

    public bool isMultiHit; // ��Ÿ ����
    public int hitCount = 1; // �⺻�� 1ȸ Ÿ��

    [Range(0, 5)] public int instantTimingFactor = 1; // 0�̸� ���� �Ұ�, 1~5�� ���� �Է� �ð� ���

    public float perfectTimeStart = 0.4f;  // ��: 400 (�Է� ���� �� 400ms �ں��� ��ȿ)
    public float perfectTimeRange = 0.5f;  // ��: 200 (400ms~600ms ���������� perfect)
}
