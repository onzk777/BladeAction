using UnityEngine;
using Spine.Unity;
using Spine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Spine 애니메이션과 전투 시스템을 연동하는 테스트 어댑터
/// 애니메이션 재생, 이벤트 처리, 초 단위 시간 계산, 디버그 로그 출력
/// 완벽 입력 판정은 ActionCommandData의 수동 설정을 유지하고, Spine 이벤트는 연출 및 디버깅용
/// </summary>
public class SpineAttackTestAdapter : MonoBehaviour
{
    [Header("Spine 설정")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private string targetEventName = "perfect"; // 타겟 이벤트 이름
    
    [Header("디버그 UI")]
    [SerializeField] private TextMeshProUGUI debugText; // 디버그 로그 출력용
    
    [Header("재생 설정")]
    [SerializeField] private float timeScale = 1.0f; // 애니메이션 재생 속도
    
    // 현재 재생 중인 애니메이션 정보
    private string currentAnimationName;
    private float animationStartTime;
    private float currentTimeInSeconds; // 초 단위 시간으로 변경
    
    // 로그 관리
    private List<string> logMessages = new List<string>();
    private const int MAX_LOG_COUNT = 20; // 최대 로그 개수
    
    // 이벤트 수신 정보 (디버깅 및 수동 동기화 확인용)
    private List<EventLog> eventLogs = new List<EventLog>();
    
    [System.Serializable]
    public class EventLog
    {
        public string eventName;           // 이벤트 이름 (예: "perfect")
        public float eventTimeInSeconds;   // 이벤트 발생 시점 (초 단위)
        public float currentTimeInSeconds; // 현재 애니메이션 시간 (초 단위)
        public string stringValue;         // 이벤트 문자열 값 (예: "hit_1_on", "hit_1_off")
        public float realTime;             // 실제 게임 시간
        
        public EventLog(string name, float eventTime, float currentTime, string strVal)
        {
            eventName = name;
            eventTimeInSeconds = eventTime;
            currentTimeInSeconds = currentTime;
            stringValue = strVal;
            realTime = Time.time;
        }
    }
    
    private void Awake()
    {
        // SkeletonAnimation 자동 찾기
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
            
        if (skeletonAnimation == null)
        {
            Debug.LogError("[SpineAttackTestAdapter] SkeletonAnimation을 찾을 수 없습니다!");
            return;
        }
        
        // 이벤트 구독
        SubscribeToEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.Event += OnSpineEvent;
            Debug.Log($"[SpineAttackTestAdapter] 이벤트 구독 완료: {targetEventName}");
        }
    }
    
    private void OnDestroy()
    {
        if (skeletonAnimation?.AnimationState != null)
        {
            skeletonAnimation.AnimationState.Event -= OnSpineEvent;
        }
    }
    
    /// <summary>
    /// 커맨드에 대한 애니메이션 재생
    /// </summary>
    /// <param name="cmd">재생할 액션 커맨드</param>
    /// <param name="playbackSpeed">재생 속도 (기본값: 1.0)</param>
    public void PlayForCommand(ActionCommandData cmd, float playbackSpeed = 1.0f)
    {
        if (skeletonAnimation == null || cmd == null)
        {
            Debug.LogWarning("[SpineAttackTestAdapter] 재생할 수 없습니다: SkeletonAnimation 또는 ActionCommandData가 null");
            return;
        }
        
        // ActionCommandData의 animationName 직접 사용
        string animName = cmd.animationName;
        
        if (string.IsNullOrEmpty(animName))
        {
            Debug.LogWarning($"[SpineAttackTestAdapter] 커맨드 '{cmd.commandName}'에 animationName이 설정되지 않았습니다!");
            return;
        }
        
        // 재생 시작
        PlayAnimation(animName, playbackSpeed);
        
        // 로그 추가
        AddLog($"재생 시작: {animName} (속도: {playbackSpeed:F2})");
        AddLog($"커맨드: {cmd.commandName}, 히트 수: {cmd.hitCount}");
        
        // ActionCommandData의 수동 설정된 PerfectTiming 정보 표시
        if (cmd.perfectTimings != null && cmd.perfectTimings.Count > 0)
        {
            AddLog("📋 수동 설정된 PerfectTiming:");
            for (int i = 0; i < cmd.perfectTimings.Count; i++)
            {
                var timing = cmd.perfectTimings[i];
                AddLog($"  Hit {i + 1}: {timing.start:F3}초 ~ {timing.End:F3}초 (Duration: {timing.duration:F3}초)");
            }
        }
        else
        {
            AddLog("⚠️ PerfectTiming이 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 애니메이션 재생
    /// </summary>
    private void PlayAnimation(string animationName, float speed)
    {
        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning("[SpineAttackTestAdapter] 애니메이션 이름이 비어있습니다");
            return;
        }
        
        // 애니메이션 재생
        skeletonAnimation.AnimationState.SetAnimation(0, animationName, false);
        
        // 재생 속도 설정
        skeletonAnimation.AnimationState.TimeScale = speed;
        
        // 현재 애니메이션 정보 저장
        currentAnimationName = animationName;
        animationStartTime = Time.time;
        
        // 이벤트 로그 초기화
        eventLogs.Clear();
        
        Debug.Log($"[SpineAttackTestAdapter] 애니메이션 재생: {animationName} (속도: {speed:F2})");
    }
    
    /// <summary>
    /// Spine 이벤트 수신 처리 (디버깅 및 수동 동기화 확인용)
    /// </summary>
    private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e == null) return;
        
        // 현재 시간을 초 단위로 계산 (Spine은 초 단위 제공)
        float currentTime = trackEntry.AnimationTime;        // 현재 애니메이션 시간 (초)
        float totalTime = trackEntry.AnimationEnd;           // 애니메이션 총 길이 (초)
        currentTimeInSeconds = currentTime;                  // 초 단위 시간 저장
        
        // 이벤트 로그 생성
        EventLog eventLog = new EventLog(
            e.Data.Name,
            currentTime,           // 이벤트 발생 시점 (초)
            currentTime,           // 현재 애니메이션 시간 (초)
            e.String
        );
        
        eventLogs.Add(eventLog);
        
        // 이벤트 로깅 (디버깅 및 수동 동기화 확인용)
        if (e.Data.Name == targetEventName)
        {
            AddLog($"🎯 타겟 이벤트 수신: {e.Data.Name} (시간: {currentTime:F3}초, 문자열: {e.String})");
        }
        else
        {
            AddLog($"📡 이벤트 수신: {e.Data.Name} (시간: {currentTime:F3}초, 문자열: {e.String})");
        }
        
        // 디버그 UI 업데이트
        UpdateDebugUI();
    }
    
    /// <summary>
    /// 로그 메시지 추가
    /// </summary>
    private void AddLog(string message)
    {
        string timestamp = $"[{Time.time:F2}s]";
        string logMessage = $"{timestamp} {message}";
        
        logMessages.Add(logMessage);
        
        // 최대 로그 개수 제한
        if (logMessages.Count > MAX_LOG_COUNT)
        {
            logMessages.RemoveAt(0);
        }
        
        Debug.Log(logMessage);
        UpdateDebugUI();
    }
    
    /// <summary>
    /// 디버그 UI 업데이트
    /// </summary>
    private void UpdateDebugUI()
    {
        if (debugText == null) return;
        
        string displayText = "";
        
        // 현재 애니메이션 정보
        if (!string.IsNullOrEmpty(currentAnimationName))
        {
            displayText += $"🎬 현재 애니메이션: {currentAnimationName}\n";
            displayText += $"⏱️ 현재 시간: {currentTimeInSeconds:F3}초\n";
            displayText += $"🚀 재생 속도: {timeScale:F2}\n\n";
        }
        
        // 이벤트 로그
        if (eventLogs.Count > 0)
        {
            displayText += "📡 이벤트 로그:\n";
            for (int i = Mathf.Max(0, eventLogs.Count - 8); i < eventLogs.Count; i++)
            {
                var log = eventLogs[i];
                displayText += $"  {log.stringValue}: {log.eventTimeInSeconds:F3}초\n";
            }
            displayText += "\n";
        }
        
        // 최근 로그 메시지
        displayText += "📝 최근 로그:\n";
        for (int i = Mathf.Max(0, logMessages.Count - 8); i < logMessages.Count; i++)
        {
            displayText += $"  {logMessages[i]}\n";
        }
        
        debugText.text = displayText;
    }
    
    /// <summary>
    /// 재생 속도 변경
    /// </summary>
    public void SetTimeScale(float newTimeScale)
    {
        timeScale = newTimeScale;
        if (skeletonAnimation?.AnimationState != null)
        {
            skeletonAnimation.AnimationState.TimeScale = timeScale;
            AddLog($"재생 속도 변경: {timeScale:F2}");
        }
    }
    
    /// <summary>
    /// 현재 이벤트 로그 반환 (수동 동기화 확인용)
    /// </summary>
    public List<EventLog> GetEventLogs()
    {
        return new List<EventLog>(eventLogs);
    }
    
    /// <summary>
    /// 로그 초기화
    /// </summary>
    public void ClearLogs()
    {
        logMessages.Clear();
        eventLogs.Clear();
        UpdateDebugUI();
    }
}
