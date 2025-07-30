using UnityEngine;

public static class TurnTimer
{
    private static float turnStartTime = -1f; // �ʱ�ȭ ������ ��Ȯ�� ����
    private static bool isInitialized => turnStartTime >= 0f;

    /// <summary>
    /// �� ���� �� ���� �ð����� ������ �ʱ�ȭ
    /// </summary>
    public static void Reset()
    {
        turnStartTime = Time.time;
        Debug.Log($"[TurnTimer] �� ���� �ð� �ʱ�ȭ: {turnStartTime:F5}");
    }

    /// <summary>
    /// ������� ����� �ð� (��)
    /// </summary>
    public static float ElapsedTime
    {
        get
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[TurnTimer] ���� Reset���� ���� �� ElapsedTime = 0");
                return 0f;
            }
            return Time.time - turnStartTime;
        }
    }

    /// <summary>
    /// ���� ���� �ð� ��ȯ (����׿�)
    /// </summary>
    public static float GetTurnStartTime()
    {
        return turnStartTime;
    }
}
