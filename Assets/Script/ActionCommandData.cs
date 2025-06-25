using UnityEngine;
using System.Collections.Generic;   

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
    public List<PerfectTimingWindow> perfectTimings     // 히트별 완벽 입력 타이밍 창
        = new List<PerfectTimingWindow>();
    public bool canInterruptTarget = false;   // 이 액션이 상대를 중단시킬 수 있는가
    public bool canBeInterrupted = true;      // 이 액션은 외부 요인에 의해 중단될 수 있는가


    public int hitCount => perfectTimings?.Count ?? 0;

    [Range(0, 5)] public int instantTimingFactor = 1; // 0이면 찰나 불가, 1~5는 찰나 입력 시간 계수

    [HideInInspector] public float perfectTimeStart = 1.0f;
    [HideInInspector] public float perfectTimeRange = 0.5f;

    private void OnEnable()
    {
        // 구버전 데이터일 때, 배열 → 리스트로 마이그레이션
        if (perfectTimings == null || perfectTimings.Count == 0)
        {
            perfectTimings = new List<PerfectTimingWindow>
                {
                    new PerfectTimingWindow { start = perfectTimeStart, duration = perfectTimeRange }
                };
            Debug.LogWarning($"[{commandName}] perfectTimings 비어 있어 기본 타이밍 창으로 초기화했습니다.");
        }
    }
}
