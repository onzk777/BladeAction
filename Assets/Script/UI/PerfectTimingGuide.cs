using UnityEngine;

/// <summary>
/// 턴 타이머 게이지 바 위에 Perfect Timing 구간을 시각적으로 표시하는 UI 컴포넌트
/// width만 동적으로 설정하고, 나머지 시각적 속성(높이, 색상, 투명도 등)은 Prefab에서 정의됩니다.
/// Guide(대기 상태)와 Already(완료 상태) 두 세트를 가지며, 완벽 입력 성공 시 전환됩니다.
/// </summary>
public class PerfectTimingGuide : MonoBehaviour
{
    [Header("Guide Set - 대기 상태 (아직 입력하지 않음)")]
    [Tooltip("대기 상태 가이드 컨테이너")]
    [SerializeField] private GameObject guideContainer;
    
    [Tooltip("Guide - Perfect 시작 시점 마커")]
    [SerializeField] private RectTransform guideStartMarker;
    
    [Tooltip("Guide - Perfect 종료 시점 마커")]
    [SerializeField] private RectTransform guideEndMarker;
    
    [Tooltip("Guide - Start~End 구간 채우기")]
    [SerializeField] private RectTransform guideFillRect;
    
    [Header("Already Set - 완료 상태 (완벽 입력 성공)")]
    [Tooltip("완료 상태 가이드 컨테이너")]
    [SerializeField] private GameObject alreadyContainer;
    
    [Tooltip("Already - Perfect 시작 시점 마커")]
    [SerializeField] private RectTransform alreadyStartMarker;
    
    [Tooltip("Already - Perfect 종료 시점 마커")]
    [SerializeField] private RectTransform alreadyEndMarker;
    
    [Tooltip("Already - Start~End 구간 채우기")]
    [SerializeField] private RectTransform alreadyFillRect;
    
    private void Awake()
    {
        // 초기 상태: Guide 활성화, Already 비활성화
        if (guideContainer != null) guideContainer.SetActive(true);
        if (alreadyContainer != null) alreadyContainer.SetActive(false);
    }
    
    /// <summary>
    /// Perfect Timing 구간의 width를 설정합니다 (시간 정보 기반)
    /// Guide와 Already 두 세트 모두에 동일하게 적용됩니다.
    /// </summary>
    /// <param name="width">게이지 바 상에서의 픽셀 width</param>
    public void SetGuideWidth(float width)
    {
        // Guide 세트 설정
        SetupMarkerSet(guideFillRect, guideStartMarker, guideEndMarker, width);
        
        // Already 세트 설정
        SetupMarkerSet(alreadyFillRect, alreadyStartMarker, alreadyEndMarker, width);
    }
    
    /// <summary>
    /// 마커 세트(FillRect, StartMarker, EndMarker)를 설정합니다
    /// </summary>
    private void SetupMarkerSet(RectTransform fillRect, RectTransform startMarker, RectTransform endMarker, float width)
    {
        if (fillRect == null) return;
        
        // FillRect 설정
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(0, 0);
        fillRect.sizeDelta = new Vector2(width, fillRect.sizeDelta.y);
        
        // StartMarker 설정
        if (startMarker != null)
        {
            startMarker.anchorMin = new Vector2(0f, 0.5f);
            startMarker.anchorMax = new Vector2(0f, 0.5f);
            startMarker.pivot = new Vector2(0.5f, 0.5f);
            startMarker.anchoredPosition = new Vector2(0, 0);
        }
        
        // EndMarker 설정
        if (endMarker != null)
        {
            endMarker.anchorMin = new Vector2(0f, 0.5f);
            endMarker.anchorMax = new Vector2(0f, 0.5f);
            endMarker.pivot = new Vector2(0.5f, 0.5f);
            endMarker.anchoredPosition = new Vector2(width, 0);
        }
    }
    
    /// <summary>
    /// 완벽 입력 성공 시 호출: Guide를 비활성화하고 Already를 활성화합니다
    /// </summary>
    public void MarkAsCompleted()
    {
        if (guideContainer != null) guideContainer.SetActive(false);
        if (alreadyContainer != null) alreadyContainer.SetActive(true);
        
        Debug.Log("[PerfectTimingGuide] 완벽 입력 성공 - Guide → Already 전환");
    }
    
    /// <summary>
    /// 가이드를 제거합니다
    /// </summary>
    public void Cleanup()
    {
        Destroy(gameObject);
    }
}

