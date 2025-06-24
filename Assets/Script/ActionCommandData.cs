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
    public string commandName;
    public string animationName;

    public bool isMultiHit; // 연타 여부
    public int hitCount = 1; // 기본은 1회 타격

    [Range(0, 5)] public int instantTimingFactor = 1; // 0이면 찰나 불가, 1~5는 찰나 입력 시간 계수

    public float perfectTimeStart = 0.4f;  // 예: 400 (입력 시작 후 400ms 뒤부터 유효)
    public float perfectTimeRange = 0.5f;  // 예: 200 (400ms~600ms 구간에서만 perfect)
}
