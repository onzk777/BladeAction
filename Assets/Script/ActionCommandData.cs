using UnityEngine;
using System.Collections.Generic;   

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
    public List<PerfectTimingWindow> perfectTimings     // ��Ʈ�� �Ϻ� �Է� Ÿ�̹� â
        = new List<PerfectTimingWindow>();
    public bool canInterruptTarget = false;   // �� �׼��� ��븦 �ߴܽ�ų �� �ִ°�
    public bool canBeInterrupted = true;      // �� �׼��� �ܺ� ���ο� ���� �ߴܵ� �� �ִ°�


    public int hitCount => perfectTimings?.Count ?? 0;

    [Range(0, 5)] public int instantTimingFactor = 1; // 0�̸� ���� �Ұ�, 1~5�� ���� �Է� �ð� ���

    [HideInInspector] public float perfectTimeStart = 1.0f;
    [HideInInspector] public float perfectTimeRange = 0.5f;

    private void OnEnable()
    {
        // ������ �������� ��, �迭 �� ����Ʈ�� ���̱׷��̼�
        if (perfectTimings == null || perfectTimings.Count == 0)
        {
            perfectTimings = new List<PerfectTimingWindow>
                {
                    new PerfectTimingWindow { start = perfectTimeStart, duration = perfectTimeRange }
                };
            Debug.LogWarning($"[{commandName}] perfectTimings ��� �־� �⺻ Ÿ�̹� â���� �ʱ�ȭ�߽��ϴ�.");
        }
    }
}
