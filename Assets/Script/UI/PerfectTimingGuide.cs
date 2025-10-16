using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 턴 타이머 게이지 바 위에 Perfect Timing 구간을 시각적으로 표시하는 UI 컴포넌트
/// </summary>
public class PerfectTimingGuide : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Perfect 시작 시점 표시용 원형 마커")]
    [SerializeField] private RectTransform startMarker;
    
    [Tooltip("Perfect 종료 시점 표시용 원형 마커")]
    [SerializeField] private RectTransform endMarker;
    
    [Tooltip("Start~End 구간을 채우는 사각형")]
    [SerializeField] private RectTransform fillRect;
    
    [Header("Visual Settings")]
    [Tooltip("마커의 크기 (지름)")]
    [SerializeField] private float markerSize = 10f;
    
    [Tooltip("Fill Rect의 높이")]
    [SerializeField] private float fillHeight = 20f;
    
    [Tooltip("가이드 색상")]
    [SerializeField] private Color guideColor = new Color(1f, 0.8f, 0f, 0.7f); // 반투명 노란색
    
    /// <summary>
    /// Perfect Timing 구간의 width를 설정합니다
    /// </summary>
    /// <param name="width">게이지 바 상에서의 픽셀 width</param>
    public void SetGuideWidth(float width)
    {
        if (fillRect == null)
        {
            Debug.LogError("[PerfectTimingGuide] fillRect가 할당되지 않았습니다!");
            return;
        }
        
        // FillRect의 width 설정
        fillRect.sizeDelta = new Vector2(width, fillHeight);
        
        // StartMarker는 fillRect의 왼쪽 끝에 배치 (로컬 위치 0)
        if (startMarker != null)
        {
            startMarker.anchoredPosition = new Vector2(0, 0);
            startMarker.sizeDelta = new Vector2(markerSize, markerSize);
        }
        
        // EndMarker는 fillRect의 오른쪽 끝에 배치 (로컬 위치 width)
        if (endMarker != null)
        {
            endMarker.anchoredPosition = new Vector2(width, 0);
            endMarker.sizeDelta = new Vector2(markerSize, markerSize);
        }
        
        // 색상 적용
        ApplyColors();
    }
    
    /// <summary>
    /// 가이드 색상을 설정합니다
    /// </summary>
    /// <param name="color">설정할 색상</param>
    public void SetGuideColor(Color color)
    {
        guideColor = color;
        ApplyColors();
    }
    
    /// <summary>
    /// 모든 컴포넌트에 색상 적용
    /// </summary>
    private void ApplyColors()
    {
        if (startMarker != null)
        {
            var startImage = startMarker.GetComponent<Image>();
            if (startImage != null) startImage.color = guideColor;
        }
        
        if (endMarker != null)
        {
            var endImage = endMarker.GetComponent<Image>();
            if (endImage != null) endImage.color = guideColor;
        }
        
        if (fillRect != null)
        {
            var fillImage = fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                // FillRect는 좀 더 투명하게
                Color fillColor = guideColor;
                fillColor.a *= 0.5f;
                fillImage.color = fillColor;
            }
        }
    }
    
    /// <summary>
    /// 가이드를 제거합니다
    /// </summary>
    public void Cleanup()
    {
        Destroy(gameObject);
    }
}

