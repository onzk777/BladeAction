using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PerfectTiming 시점에 FloatingText를 생성하는 매니저
/// 테스트 용도로 설계되어 추후 제거 또는 독립 모듈로 분리 가능
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }
    
    [Header("FloatingText 설정")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform floatingTextParent; // 부모 Transform (선택사항)
    
    [Header("테스트 모드")]
    [SerializeField] private bool enableFloatingText = true; // FloatingText 활성화 여부
    [SerializeField] private bool showDebugLogs = true; // 디버그 로그 표시 여부
    
    [Header("타이밍 표시 설정")]
    [SerializeField] private string startText = "Start";
    [SerializeField] private string endText = "End";
    
    // 생성된 FloatingText들 추적 (메모리 관리용)
    private List<FloatingText> activeFloatingTexts = new List<FloatingText>();
    
    // 고유 ID 생성용 카운터
    private static int floatingTextCounter = 0;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// PerfectTiming 시작 시점에 FloatingText 생성
    /// </summary>
    /// <param name="worldPosition">월드 위치</param>
    /// <param name="hitIndex">히트 인덱스 (1부터 시작)</param>
    /// <param name="timing">PerfectTiming 정보</param>
    public void ShowPerfectTimingStart(Vector3 worldPosition, int hitIndex, PerfectTimingWindow timing)
    {
        if (!enableFloatingText) return;
        
        string displayText = $"Hit {hitIndex} {startText}";
        CreateFloatingText(worldPosition, displayText, Color.green);
        
        if (showDebugLogs)
        {
            Debug.Log($"[FloatingTextManager] PerfectTiming Start: Hit {hitIndex} at {timing.start:F3}초");
        }
    }
    
    /// <summary>
    /// PerfectTiming 종료 시점에 FloatingText 생성
    /// </summary>
    /// <param name="worldPosition">월드 위치</param>
    /// <param name="hitIndex">히트 인덱스 (1부터 시작)</param>
    /// <param name="timing">PerfectTiming 정보</param>
    public void ShowPerfectTimingEnd(Vector3 worldPosition, int hitIndex, PerfectTimingWindow timing)
    {
        if (!enableFloatingText) return;
        
        string displayText = $"Hit {hitIndex} {endText}";
        CreateFloatingText(worldPosition, displayText, Color.red);
        
        if (showDebugLogs)
        {
            Debug.Log($"[FloatingTextManager] PerfectTiming End: Hit {hitIndex} at {timing.End:F3}초");
        }
    }
    
    /// <summary>
    /// FloatingText 생성 및 관리 (Canvas UI 좌표계 사용)
    /// </summary>
    private void CreateFloatingText(Vector3 worldPosition, string text, Color color)
    {
        // 고유 ID 생성
        int currentId = ++floatingTextCounter;
        string logPrefix = $"[FT#{currentId:000}]";
        
        if (floatingTextPrefab == null)
        {
            Debug.LogWarning($"{logPrefix} FloatingText Prefab이 설정되지 않았습니다!");
            return;
        }
        
        Debug.Log($"{logPrefix} === FloatingText 생성 시작 ===");
        
        // Canvas 좌표로 변환
        Vector3 canvasPosition = ConvertWorldToCanvasPosition(worldPosition);
        Debug.Log($"{logPrefix} 월드 좌표: {worldPosition} → Canvas 좌표: {canvasPosition}");
        
        // FloatingText 생성 (Canvas 좌표 사용)
        GameObject floatingTextObj = Instantiate(floatingTextPrefab, Vector3.zero, Quaternion.identity);
        floatingTextObj.name = $"FloatingText_{currentId:000}";
        Debug.Log($"{logPrefix} GameObject 생성됨: {floatingTextObj.name}");
        
        // 부모 설정 (Canvas)
        if (floatingTextParent != null)
        {
            floatingTextObj.transform.SetParent(floatingTextParent);
            Debug.Log($"{logPrefix} 부모 설정 완료: {floatingTextParent.name}");
        }
        else
        {
            Debug.LogWarning($"{logPrefix} 부모가 설정되지 않음! Canvas를 찾아서 자동 설정 시도");
            
            // 자동으로 Canvas 찾기
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length > 0)
            {
                Canvas targetCanvas = canvases[0]; // 첫 번째 Canvas 사용
                floatingTextObj.transform.SetParent(targetCanvas.transform);
                Debug.Log($"{logPrefix} 자동으로 Canvas 찾음: {targetCanvas.name}");
            }
            else
            {
                Debug.LogError($"{logPrefix} Canvas를 찾을 수 없음! UI 요소가 제대로 렌더링되지 않을 수 있습니다.");
            }
        }
        
        // RectTransform을 사용하여 Canvas 좌표 설정 (해상도 독립적)
        RectTransform rectTransform = floatingTextObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(canvasPosition.x, canvasPosition.y);
            Debug.Log($"{logPrefix} RectTransform 위치 설정: {rectTransform.anchoredPosition}");
        }
        else
        {
            Debug.LogError($"{logPrefix} RectTransform을 찾을 수 없음! UI 요소로 동작하려면 RectTransform이 필요합니다.");
            Debug.LogError($"{logPrefix} Prefab에 RectTransform 컴포넌트를 추가해주세요.");
            return; // RectTransform이 없으면 생성 중단
        }
        
        // FloatingText 컴포넌트 설정
        Debug.Log($"{logPrefix} FloatingText 컴포넌트 검색 시작");
        
        FloatingText floatingText = floatingTextObj.GetComponent<FloatingText>();
        Debug.Log($"{logPrefix} GetComponent<FloatingText>() 결과: {(floatingText != null ? "성공" : "실패")}");
        
        if (floatingText != null)
        {
            Debug.Log($"{logPrefix} FloatingText 컴포넌트 발견, Initialize 호출 시작");
            
            try
            {
                floatingText.Initialize(text, canvasPosition, currentId);
                Debug.Log($"{logPrefix} Initialize 호출 완료");
                
                floatingText.SetTextColor(color);
                Debug.Log($"{logPrefix} SetTextColor 호출 완료");
                
                // 활성 리스트에 추가
                activeFloatingTexts.Add(floatingText);
                
                // FloatingText가 자체적으로 제거되므로 리스트 정리만 필요
                StartCoroutine(CleanupFromList(floatingText, currentId));
                
                Debug.Log($"{logPrefix} FloatingText 생성됨: {text}, Lifetime: {floatingText.Lifetime}초");
                Debug.Log($"{logPrefix} === FloatingText 생성 완료 ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{logPrefix} Initialize 과정에서 오류 발생: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"{logPrefix} FloatingText 컴포넌트를 찾을 수 없습니다! GameObject: {floatingTextObj.name}");
            
            // 추가 디버그: GameObject에 어떤 컴포넌트들이 있는지 확인
            Component[] components = floatingTextObj.GetComponents<Component>();
            Debug.Log($"{logPrefix} GameObject에 있는 컴포넌트들:");
            foreach (Component comp in components)
            {
                Debug.Log($"{logPrefix}   - {comp.GetType().Name}");
            }
        }
    }
    
    /// <summary>
    /// FloatingText가 자체적으로 제거된 후 리스트에서 정리
    /// </summary>
    private System.Collections.IEnumerator CleanupFromList(FloatingText floatingText, int id)
    {
        string logPrefix = $"[FT#{id:000}]";
        
        if (floatingText == null)
        {
            Debug.LogWarning($"{logPrefix} CleanupFromList: floatingText가 null");
            yield break;
        }
        
        // FloatingText의 Lifetime만큼 대기
        float lifetime = 0.8f;
        try
        {
            lifetime = floatingText.Lifetime;
        }
        catch
        {
            // 기본값 사용
        }
        
        Debug.Log($"{logPrefix} CleanupFromList 시작: {floatingText.gameObject.name}, Lifetime={lifetime}초");
        
        // Lifetime 완료까지 대기
        yield return new WaitForSeconds(lifetime + 0.3f); // 여유 시간 추가
        
        // GameObject가 여전히 존재하는지 확인하고 강제 제거
        if (floatingText != null && floatingText.gameObject != null)
        {
            Debug.LogWarning($"{logPrefix} FloatingText가 여전히 존재함, 강제 제거 시도: {floatingText.gameObject.name}");
            
            // 방법 1: 일반 Destroy
            Destroy(floatingText.gameObject);
            
            // 방법 2: 다음 프레임에서 확인 후 DestroyImmediate
            yield return null;
            if (floatingText != null && floatingText.gameObject != null)
            {
                Debug.LogError($"{logPrefix} Destroy 실패, DestroyImmediate로 강제 제거: {floatingText.gameObject.name}");
                DestroyImmediate(floatingText.gameObject);
            }
        }
        
        // 리스트에서 제거
        if (activeFloatingTexts.Contains(floatingText))
        {
            activeFloatingTexts.Remove(floatingText);
            // null 체크를 더 엄격하게
            string objectName = "Unknown";
            if (floatingText != null && floatingText.gameObject != null)
            {
                objectName = floatingText.gameObject.name;
            }
            Debug.Log($"{logPrefix} 리스트에서 정리됨: {objectName}");
        }
    }
    
    /// <summary>
    /// 모든 활성 FloatingText 정리
    /// </summary>
    public void ClearAllFloatingTexts()
    {
        foreach (var floatingText in activeFloatingTexts)
        {
            if (floatingText != null)
            {
                Destroy(floatingText.gameObject);
            }
        }
        activeFloatingTexts.Clear();
    }
    
    /// <summary>
    /// FloatingText 활성화/비활성화 토글
    /// </summary>
    public void ToggleFloatingText()
    {
        enableFloatingText = !enableFloatingText;
        Debug.Log($"[FloatingTextManager] FloatingText {(enableFloatingText ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 디버그 로그 활성화/비활성화 토글
    /// </summary>
    public void ToggleDebugLogs()
    {
        showDebugLogs = !showDebugLogs;
        Debug.Log($"[FloatingTextManager] 디버그 로그 {(showDebugLogs ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 월드 좌표를 Canvas 좌표로 변환 (해상도 독립적)
    /// </summary>
    /// <param name="worldPosition">월드 좌표</param>
    /// <returns>Canvas 좌표</returns>
    private Vector3 ConvertWorldToCanvasPosition(Vector3 worldPosition)
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("[FloatingTextManager] 메인 카메라를 찾을 수 없습니다!");
            return Vector3.zero;
        }
        
        if (floatingTextParent == null)
        {
            Debug.LogWarning("[FloatingTextManager] floatingTextParent가 설정되지 않았습니다!");
            return Vector3.zero;
        }
        
        Canvas canvas = floatingTextParent.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[FloatingTextManager] Canvas를 찾을 수 없습니다!");
            return Vector3.zero;
        }
        
        // 1. 월드 좌표를 화면 좌표로 변환
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        
        // 2. Canvas 좌표로 변환 (해상도 독립적)
        Vector2 canvasPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            floatingTextParent as RectTransform, 
            screenPosition, 
            canvas.worldCamera, 
            out canvasPosition))
        {
            return canvasPosition;
        }
        else
        {
            Debug.LogWarning("[FloatingTextManager] 좌표 변환 실패, 기본값 사용");
            // 기본값: 화면 중앙 (해상도 비율 고려)
            float screenRatio = (float)Screen.width / Screen.height;
            return new Vector3(0, 100, 0); // 중앙에서 약간 위
        }
    }
    
    private void OnDestroy()
    {
        ClearAllFloatingTexts();
    }
}
