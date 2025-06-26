using UnityEngine;
using System.Collections.Generic;   



[CreateAssetMenu(fileName = "ActionCommandData", menuName = "Combat/ActionCommandData", order = 1)]
public class ActionCommandData : ScriptableObject
{
    public ActionCommand commandType;
    public string commandName; // Ŀ�ǵ� �̸�

    [Header("�Ϻ� �Է� Ÿ�̹�")]
    [Tooltip("�Ϻ� �Է� Ÿ�̹� â ����Ʈ(�� ����Ʈ ����)")]
    public List<PerfectTimingWindow> perfectTimings     // ��Ʈ�� �Ϻ� �Է� Ÿ�̹� â
        = new List<PerfectTimingWindow>();

    [Header("���ͷ�Ʈ ����")]
    [Tooltip("�� �׼��� ��븦 �ߴܽ�ų �� �ִ���")]
    public bool canInterruptTarget = false;   // �� �׼��� ��븦 �ߴܽ�ų �� �ִ°�

    [Tooltip("�� �׼��� �ܺο� ���� �ߴܵ� �� �ִ���")]
    public bool canBeInterrupted = true;      // �� �׼��� �ܺ� ���ο� ���� �ߴܵ� �� �ִ°�

    /// <summary>
    /// ��Ʈ ���� (perfectTimings.Count)
    /// �� ����Ʈ���� 0�� ��ȯ�մϴ�.
    /// </summary>
    public int hitCount => perfectTimings?.Count ?? 0;

    [Range(0, 5)] public int instantTimingFactor = 1; // 0�̸� ���� �Ұ�, 1~5�� ���� �Է� �ð� ���


}
