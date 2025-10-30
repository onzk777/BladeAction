using UnityEngine;
using System.Collections.Generic;   



[CreateAssetMenu(fileName = "ActionCommandData", menuName = "Combat/ActionCommandData", order = 1)]
public class ActionCommandData : ScriptableObject
{
    public ActionCommand commandType;
    public string commandName; // 커맨드 이름
    
    [Header("UI 표시")]
    [Tooltip("검술 아이콘")]
    public Sprite icon;
    
    [Tooltip("검술 설명")]
    [TextArea(3, 5)]
    public string description;

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
    
    [Header("발사체 설정")]
    [Tooltip("일반 발사체 프리팹")]
    public GameObject normalProjectilePrefab;
    
    [Tooltip("완벽 입력 성공 시 발사체 프리팹")]
    public GameObject perfectProjectilePrefab;
    
    [Tooltip("발사체 크기")]
    public float projectileScale = 1f;
    
    [Header("검술 태그")]
    [Tooltip("이 검술을 분류하는 태그들 (ActionCommandTagList에서 선택)")]
    public List<string> tags = new List<string>();

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
    
    /// <summary>
    /// 특정 태그를 포함하는지 확인
    /// </summary>
    public bool HasTag(string tag)
    {
        return tags != null && tags.Contains(tag);
    }
}
