// CombatHUD.cs
// 전투 게임 HUD (플레이어용 UI)
// 배치 위치: CombatScene > Canvas_HUD

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 전투 중 플레이어에게 보여지는 HUD 컴포넌트
/// CombatScene의 Canvas_HUD에 배치됨
/// </summary>
public class CombatHUD : MonoBehaviour
{
    public static CombatHUD Instance { get; private set; }

    [Header("Turn Timer")]
    [Tooltip("턴 타이머 진행률 바 (Image 컴포넌트, Type=Filled 권장)")]
    [SerializeField] private Image turnTimerProgressBar;
    
    [Tooltip("턴 타이머 진행률 바 배경 (선택 사항)")]
    [SerializeField] private Image turnTimerProgressBarBackground;

    [Header("Perfect Timing Guide")]
    [Tooltip("Perfect Timing 가이드 Prefab")]
    [SerializeField] private GameObject perfectTimingGuidePrefab;
    
    [Tooltip("Perfect Timing 가이드를 배치할 부모 Transform (turnTimerProgressBar의 부모가 적절)")]
    [SerializeField] private RectTransform perfectTimingGuideContainer;
    
    // 생성된 가이드들을 추적
    private List<PerfectTimingGuide> activeGuides = new List<PerfectTimingGuide>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[CombatHUD] 중복 인스턴스 감지! 기존 인스턴스 유지");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 턴 타이머 진행률 바 업데이트 (시각적 표시만)
    /// </summary>
    /// <param name="remainingTime">잔여 시간</param>
    /// <param name="totalTime">전체 턴 시간</param>
    public void UpdateTurnProgressBar(float remainingTime, float totalTime)
    {
        if (totalTime <= 0f) return;
        
        // 경과 시간 기준 진행률 계산
        float elapsedTime = totalTime - remainingTime;
        float progressNormalized = Mathf.Clamp01(elapsedTime / totalTime); // 0~1 범위
        
        // 프로그레스 바 업데이트
        UpdateProgressBar(progressNormalized);
    }
    
    /// <summary>
    /// 프로그레스 바를 업데이트합니다.
    /// </summary>
    /// <param name="normalizedProgress">진행률 (0~1)</param>
    private void UpdateProgressBar(float normalizedProgress)
    {
        if (turnTimerProgressBar == null) return;
        
        // Image의 타입에 따라 다르게 처리
        // Type=Filled인 경우: fillAmount 사용 (권장)
        if (turnTimerProgressBar.type == Image.Type.Filled)
        {
            turnTimerProgressBar.fillAmount = normalizedProgress;
        }
        // Type=Simple 등 다른 타입: Scale 조정 (0~1)
        else
        {
            RectTransform rectTransform = turnTimerProgressBar.rectTransform;
            if (rectTransform != null)
            {
                // Scale의 x값만 변경 (y, z는 유지)
                // normalizedProgress: 0 = 0%, 1 = 100%
                Vector3 scale = rectTransform.localScale;
                scale.x = normalizedProgress;
                rectTransform.localScale = scale;
            }
        }
        
        // 색상 변경 (선택 사항): 진행률에 따라 색상 그라데이션
        // UpdateProgressBarColor(normalizedProgress);
    }
    
    /// <summary>
    /// 진행률에 따라 프로그레스 바 색상을 변경합니다. (선택 사항)
    /// </summary>
    /// <param name="normalizedProgress">진행률 (0~1)</param>
    private void UpdateProgressBarColor(float normalizedProgress)
    {
        if (turnTimerProgressBar == null) return;
        
        // 0%: 초록색, 50%: 노란색, 100%: 빨간색
        Color barColor;
        if (normalizedProgress < 0.5f)
        {
            // 초록 → 노랑 (0 ~ 0.5)
            barColor = Color.Lerp(Color.green, Color.yellow, normalizedProgress * 2f);
        }
        else
        {
            // 노랑 → 빨강 (0.5 ~ 1.0)
            barColor = Color.Lerp(Color.yellow, Color.red, (normalizedProgress - 0.5f) * 2f);
        }
        
        turnTimerProgressBar.color = barColor;
    }

    /// <summary>
    /// Perfect Timing 가이드들을 생성합니다
    /// </summary>
    /// <param name="actionData">현재 공격자가 사용한 검술 데이터</param>
    /// <param name="totalTurnTime">전체 턴 시간</param>
    public void ShowPerfectTimingGuides(ActionCommandData actionData, float totalTurnTime)
    {
        // 이전 가이드들 제거
        ClearPerfectTimingGuides();
        
        if (actionData == null || actionData.perfectTimings == null || actionData.perfectTimings.Count == 0)
        {
            Debug.Log("[CombatHUD] Perfect Timing 데이터가 없습니다.");
            return;
        }
        
        if (perfectTimingGuidePrefab == null)
        {
            Debug.LogWarning("[CombatHUD] perfectTimingGuidePrefab이 할당되지 않았습니다!");
            return;
        }
        
        if (perfectTimingGuideContainer == null)
        {
            Debug.LogWarning("[CombatHUD] perfectTimingGuideContainer가 할당되지 않았습니다!");
            return;
        }
        
        if (totalTurnTime <= 0f)
        {
            Debug.LogWarning("[CombatHUD] totalTurnTime이 유효하지 않습니다!");
            return;
        }
        
        // 게이지 바의 실제 픽셀 width 가져오기
        float gaugeWidth = GetGaugeWidth();
        if (gaugeWidth <= 0f)
        {
            Debug.LogWarning("[CombatHUD] 게이지 바의 width를 가져올 수 없습니다!");
            return;
        }
        
        // 각 Hit의 Perfect Timing에 대해 가이드 생성
        for (int i = 0; i < actionData.perfectTimings.Count; i++)
        {
            PerfectTimingWindow timing = actionData.perfectTimings[i];
            CreatePerfectTimingGuide(timing, totalTurnTime, gaugeWidth, i);
        }
        
        Debug.Log($"[CombatHUD] {actionData.perfectTimings.Count}개의 Perfect Timing 가이드 생성 완료");
    }
    
    /// <summary>
    /// 개별 Perfect Timing 가이드를 생성합니다
    /// </summary>
    private void CreatePerfectTimingGuide(PerfectTimingWindow timing, float totalTurnTime, float gaugeWidth, int hitIndex)
    {
        // Prefab 인스턴스화
        GameObject guideObj = Instantiate(perfectTimingGuidePrefab, perfectTimingGuideContainer);
        PerfectTimingGuide guide = guideObj.GetComponent<PerfectTimingGuide>();
        
        if (guide == null)
        {
            Debug.LogError("[CombatHUD] Prefab에 PerfectTimingGuide 컴포넌트가 없습니다!");
            Destroy(guideObj);
            return;
        }
        
        // Start 시간 기준으로 상대적 위치 계산
        float startRatio = timing.start / totalTurnTime;
        float startPositionX = gaugeWidth * startRatio;
        
        // RectTransform 설정 (Anchor를 Left로)
        RectTransform guideRect = guideObj.GetComponent<RectTransform>();
        if (guideRect != null)
        {
            // Anchor를 Left-Center로 설정
            guideRect.anchorMin = new Vector2(0f, 0.5f);
            guideRect.anchorMax = new Vector2(0f, 0.5f);
            guideRect.pivot = new Vector2(0f, 0.5f);
            
            // 위치 설정
            guideRect.anchoredPosition = new Vector2(startPositionX, 0f);
        }
        
        // Perfect 구간의 width 계산
        float durationRatio = timing.duration / totalTurnTime;
        float guideWidth = gaugeWidth * durationRatio;
        
        // 가이드 width 설정 (색상, 크기 등은 Prefab에서 설정된 값 사용)
        guide.SetGuideWidth(guideWidth);
        
        // 리스트에 추가
        activeGuides.Add(guide);
        
        Debug.Log($"[CombatHUD] Hit {hitIndex + 1} 가이드 생성: Start={timing.start:F3}초, Duration={timing.duration:F3}초, X={startPositionX:F1}px, Width={guideWidth:F1}px");
    }
    
    /// <summary>
    /// 게이지 바의 실제 픽셀 width를 반환합니다
    /// </summary>
    private float GetGaugeWidth()
    {
        if (turnTimerProgressBar == null) return 0f;
        
        RectTransform gaugeRect = turnTimerProgressBar.rectTransform;
        if (gaugeRect == null) return 0f;
        
        return gaugeRect.rect.width;
    }
    
    /// <summary>
    /// 모든 Perfect Timing 가이드를 제거합니다
    /// </summary>
    public void ClearPerfectTimingGuides()
    {
        foreach (var guide in activeGuides)
        {
            if (guide != null)
            {
                guide.Cleanup();
            }
        }
        activeGuides.Clear();
    }
    
    /// <summary>
    /// 특정 Hit의 가이드를 완료 상태로 전환합니다 (완벽 입력 성공 시)
    /// </summary>
    /// <param name="hitIndex">Hit 인덱스 (0부터 시작)</param>
    public void MarkGuideAsCompleted(int hitIndex)
    {
        if (activeGuides == null || hitIndex < 0 || hitIndex >= activeGuides.Count)
        {
            Debug.LogWarning($"[CombatHUD] 유효하지 않은 hitIndex: {hitIndex} (activeGuides.Count: {activeGuides?.Count ?? 0})");
            return;
        }
        
        PerfectTimingGuide guide = activeGuides[hitIndex];
        if (guide != null)
        {
            guide.MarkAsCompleted();
            Debug.Log($"[CombatHUD] Hit {hitIndex + 1} 가이드 완료 상태로 전환");
        }
        else
        {
            Debug.LogWarning($"[CombatHUD] Hit {hitIndex + 1} 가이드가 null입니다");
        }
    }

    /// <summary>
    /// HUD 초기화 (턴 시작 시)
    /// </summary>
    public void ClearHUD()
    {
        // Perfect Timing 가이드 제거
        ClearPerfectTimingGuides();
    }

    /// <summary>
    /// Scene 전환 시 리소스 정리
    /// </summary>
    private void OnDestroy()
    {
        // 가이드 정리
        ClearPerfectTimingGuides();
    }
}

