using UnityEngine;

public enum CommandType
{
    Slash_Horizontal,
    Slash_Vertical,
    Thrust,
    Defend,
    SecretArt // 찰나 입력 불가
}

[CreateAssetMenu(fileName = "ActionCommandData", menuName = "Combat/ActionCommandData", order = 1)]
public class ActionCommandData : ScriptableObject
{
    public CommandType commandType;
    public string commandName; // 커맨드 이름
    public PerfectTimingWindow[] perfectTimings; // 히트 별 완벽 입력 타이밍
    public bool canInterruptTarget = false;   // 이 액션이 상대를 중단시킬 수 있는가
    public bool canBeInterrupted = true;      // 이 액션은 외부 요인에 의해 중단될 수 있는가


    public int hitCount
    {
        get
        {
            // 완벽 입력 타이밍이 없는 경우, 히트가 없다고 간주 (입력 없음)
            return (perfectTimings != null && perfectTimings.Length > 0) ? perfectTimings.Length : 0;
        }
    }

    [Range(0, 5)] public int instantTimingFactor = 1; // 0이면 찰나 불가, 1~5는 찰나 입력 시간 계수

    public float perfectTimeStart = 1.0f;  
    public float perfectTimeRange = 0.5f;

    private void OnEnable()
    {
        /*
        if (perfectTimings == null || perfectTimings.Length == 0)
        {
            perfectTimings = new PerfectTimingWindow[1];
            perfectTimings[0] = new PerfectTimingWindow { start = perfectTimeStart, duration = perfectTimeRange };
            Debug.LogWarning($"[{commandName}] perfectTimings가 null 또는 비어 있어 기본값으로 초기화됨.");
        }
        */
    }



}
