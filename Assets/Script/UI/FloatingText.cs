using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [Header("플로팅 설정")]
    [SerializeField] private float lifetime = 0.8f;        // 텍스트가 살아있는 시간
    [SerializeField] private float riseSpeed = 1.2f;       // 위로 상승하는 속도
    [SerializeField] private Vector2 startOffset = new Vector2(0, 0.5f); // 시작 위치 오프셋 (1.2f에서 0.5f로 감소)
    
    // Public 프로퍼티로 Lifetime 접근 허용
    public float Lifetime => lifetime;
    
    [Header("참조")]
    [SerializeField] private TextMeshProUGUI textComponent; // 텍스트 컴포넌트
    [SerializeField] private CanvasGroup canvasGroup;       // 알파 페이드용
    
    private Vector3 startPosition; // 시작 위치 저장
    private float elapsedTime;    // 경과 시간
    private Coroutine currentAnimation; // 현재 실행 중인 코루틴 추적

    private void Awake()
    {
        // 컴포넌트 자동 찾기 (null 체크 개선)
        textComponent ??= GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup ??= GetComponent<CanvasGroup>();
        
        // 필수 컴포넌트 확인
        if (textComponent == null)
        {
            Debug.LogError($"[{nameof(FloatingText)}] TextMeshProUGUI 컴포넌트를 찾을 수 없습니다!", this);
            enabled = false; // 스크립트 비활성화
            return;
        }
        if (canvasGroup == null)
        {
            Debug.LogError($"[{nameof(FloatingText)}] CanvasGroup 컴포넌트를 찾을 수 없습니다!", this);
            enabled = false;
            return;
        }
    }

    public void Initialize(string text, Vector3 canvasPosition, int id = 0)
    {
        string logPrefix = $"[FT#{id:000}]";
        
        // 입력 유효성 검사
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"{logPrefix} 빈 텍스트로 초기화를 시도했습니다.", this);
            return;
        }

        // 기존 애니메이션 중단
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        // 컴포넌트 상태 확인
        if (textComponent == null || canvasGroup == null)
        {
            Debug.LogError($"{logPrefix} 필수 컴포넌트가 없습니다!", this);
            return;
        }

        // 텍스트 설정
        textComponent.text = text;
        
        // Canvas UI 좌표계 사용
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // 시작 위치를 Canvas 좌표로 설정
            startPosition = canvasPosition + new Vector3(startOffset.x, startOffset.y, 0);
            rectTransform.anchoredPosition = new Vector2(startPosition.x, startPosition.y);
            Debug.Log($"{logPrefix} Canvas 위치 설정: {rectTransform.anchoredPosition}");
        }
        else
        {
            Debug.LogError($"{logPrefix} RectTransform을 찾을 수 없습니다!");
            return;
        }
        
        // 초기 상태 설정
        canvasGroup.alpha = 1f;
        elapsedTime = 0f;
        
        // 플로팅 애니메이션 시작
        Debug.Log($"{logPrefix} Initialize 완료, 애니메이션 코루틴 시작: text='{text}', 위치={canvasPosition}");
        currentAnimation = StartCoroutine(FloatingAnimation(id));
        
        if (currentAnimation == null)
        {
            Debug.LogError($"{logPrefix} 애니메이션 코루틴 시작 실패!");
        }
        else
        {
            Debug.Log($"{logPrefix} 애니메이션 코루틴 시작 성공!");
        }
    }

    private IEnumerator FloatingAnimation(int id)
    {
        string logPrefix = $"[FT#{id:000}]";
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"{logPrefix} RectTransform을 찾을 수 없습니다!");
            yield break;
        }
        
        Debug.Log($"{logPrefix} 애니메이션 시작: Lifetime={lifetime}초, GameObject: {gameObject.name}");
        Debug.Log($"{logPrefix} 시작 위치: {startPosition}, riseSpeed: {riseSpeed}");
        
        // 코루틴이 실행되는 동안 계속 반복
        while (elapsedTime < lifetime)
        {
            elapsedTime += Time.deltaTime;
            
            // 진행률 계산 (0~1)
            float progress = elapsedTime / lifetime;
            
            // 위로 상승 (Canvas UI 좌표계 사용)
            Vector2 currentPosition = rectTransform.anchoredPosition;
            float newY = startPosition.y + (riseSpeed * elapsedTime);
            currentPosition.y = newY;
            rectTransform.anchoredPosition = currentPosition;
            
            // 알파 페이드 아웃
            canvasGroup.alpha = 1f - progress;
            
            // 디버그: 위치 변화 추적
            if (elapsedTime % 0.1f < Time.deltaTime) // 0.1초마다 로그
            {
                Debug.Log($"{logPrefix} 애니메이션 진행: elapsed={elapsedTime:F2}, 위치=({currentPosition.x:F1}, {currentPosition.y:F1}), 알파={canvasGroup.alpha:F2}");
            }
            
            yield return null; // 다음 프레임까지 대기
        }
        
        Debug.Log($"{logPrefix} 애니메이션 완료, 자동 제거 시도: {gameObject.name}");
        
        // 방법 1: 일반 Destroy
        Destroy(gameObject);
        Debug.Log($"{logPrefix} Destroy 호출 완료: {gameObject.name}");
        
        // 방법 2: 다음 프레임에서 확인 후 강제 제거
        yield return null;
        if (gameObject != null)
        {
            Debug.LogWarning($"{logPrefix} Destroy 실패, DestroyImmediate 시도: {gameObject.name}");
            DestroyImmediate(gameObject);
        }
    }
    
    /// <summary>
    /// 텍스트 색상 설정
    /// </summary>
    /// <param name="color">설정할 색상</param>
    public void SetTextColor(Color color)
    {
        if (textComponent != null)
        {
            textComponent.color = color;
        }
    }
    
    private void OnDisable()
    {
        // 컴포넌트 비활성화 시 코루틴 정리
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
}
