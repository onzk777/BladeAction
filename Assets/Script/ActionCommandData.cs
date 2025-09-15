using UnityEngine;
using System.Collections.Generic;   



[CreateAssetMenu(fileName = "ActionCommandData", menuName = "Combat/ActionCommandData", order = 1)]
public class ActionCommandData : ScriptableObject
{
    public ActionCommand commandType;
    public string commandName; // 커맨드 이름

    [Header("Spine 애니메이션 설정")]
    [Tooltip("이 커맨드에서 재생할 Spine 애니메이션 이름")]
    public string animationName = ""; // Spine 애니메이션 이름

    [Header("완벽 입력 타이밍")]
    [Tooltip("완벽 입력 타이밍 창 리스트(빈 리스트 가능)")]
    public List<PerfectTimingWindow> perfectTimings     // 히트별 완벽 입력 타이밍 창
        = new List<PerfectTimingWindow>();

    [Header("인터럽트 설정")]
    [Tooltip("이 액션이 상대를 중단시킬 수 있는지")]
    public bool canInterruptTarget = false;   // 이 액션이 상대를 중단시킬 수 있는가

    [Tooltip("이 액션이 외부에 의해 중단될 수 있는지")]
    public bool canBeInterrupted = true;      // 이 액션은 외부 요인에 의해 중단될 수 있는가

    /// <summary>
    /// 히트 개수 (perfectTimings.Count)
    /// 빈 리스트여도 0을 반환합니다.
    /// </summary>
    public int hitCount => perfectTimings?.Count ?? 0;

    [Range(0, 5)] public int instantTimingFactor = 1; // 0이면 찰나 불가, 1~5는 찰나 입력 시간 계수

    [Header("공격력 설정")]
    [Tooltip("공격력 배율 (1.0 = 기본 공격력, 2.0 = 2배 공격력)")]
    [Range(0.1f, 5.0f)]
    public float damageRatio = 1.0f;

    /// <summary>
    /// 특정 히트의 공격력 배율을 반환합니다
    /// </summary>
    /// <param name="hitIndex">히트 인덱스 (0부터 시작)</param>
    /// <returns>해당 히트의 damageRatio, 인덱스가 유효하지 않으면 1.0 반환</returns>
    public float GetDamageRatio(int hitIndex)
    {
        if (perfectTimings == null || hitIndex < 0 || hitIndex >= perfectTimings.Count)
        {
            return 1.0f; // 기본값
        }
        return perfectTimings[hitIndex].damageRatio;
    }
}
