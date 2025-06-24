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
    public string commandName; // Ŀ�ǵ� �̸�
    public PerfectTimingWindow[] perfectTimings; // ��Ʈ �� �Ϻ� �Է� Ÿ�̹�
    public bool canInterruptTarget = false;   // �� �׼��� ��븦 �ߴܽ�ų �� �ִ°�
    public bool canBeInterrupted = true;      // �� �׼��� �ܺ� ���ο� ���� �ߴܵ� �� �ִ°�


    public int hitCount
    {
        get
        {
            // �Ϻ� �Է� Ÿ�̹��� ���� ���, ��Ʈ�� ���ٰ� ���� (�Է� ����)
            return (perfectTimings != null && perfectTimings.Length > 0) ? perfectTimings.Length : 0;
        }
    }

    [Range(0, 5)] public int instantTimingFactor = 1; // 0�̸� ���� �Ұ�, 1~5�� ���� �Է� �ð� ���

    public float perfectTimeStart = 1.0f;  
    public float perfectTimeRange = 0.5f;

    private void OnEnable()
    {
        /*
        if (perfectTimings == null || perfectTimings.Length == 0)
        {
            perfectTimings = new PerfectTimingWindow[1];
            perfectTimings[0] = new PerfectTimingWindow { start = perfectTimeStart, duration = perfectTimeRange };
            Debug.LogWarning($"[{commandName}] perfectTimings�� null �Ǵ� ��� �־� �⺻������ �ʱ�ȭ��.");
        }
        */
    }



}
