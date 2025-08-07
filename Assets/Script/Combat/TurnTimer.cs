using UnityEngine;

public static class TurnTimer
{
    private static float turnStartTime = -1f; // 초기화 이전을 명확히 구분
    private static bool isInitialized => turnStartTime >= 0f;

    /// <summary>
    /// 턴 시작 시 현재 시간으로 기준점 초기화
    /// </summary>
    public static void Reset()
    {
        turnStartTime = Time.time;
        Debug.Log($"[TurnTimer] 턴 시작 시각 초기화: {turnStartTime:F5}");
    }

    /// <summary>
    /// 현재까지 경과된 시간 (초)
    /// </summary>
    public static float ElapsedTime
    {
        get
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[TurnTimer] 아직 Reset되지 않음 → ElapsedTime = 0");
                return 0f;
            }
            return Time.time - turnStartTime;
        }
    }

    /// <summary>
    /// 현재 기준 시각 반환 (디버그용)
    /// </summary>
    public static float GetTurnStartTime()
    {
        return turnStartTime;
    }
}
