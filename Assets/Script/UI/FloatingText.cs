using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections

public class FloatingText : MonoBehaviour
{
    [Header("플로팅 설정")]
    [SerializeField] private float lifetime = 0.8f;        // 텍스트가 살아있는 시간
    [SerializeField] private float riseSpeed = 1.2f;       // 위로 상승하는 속도
    [SerializeField] private Vector2 startOffset = new Vector2(0, 1.2f); // 시작 위치 오프셋
    
    [Header("참조")]
    [SerializeField] private TextMeshProUGUI textComponent; // 텍스트 컴포넌트
    [SerializeField] private CanvasGroup canvasGroup;       // 알파 페이드용
    
    private Vector3 startPosition; // 시작 위치 저장
    private float elapsedTime;     // 경과 시간
    private void Awake()
    {
        // 컴포넌트 자동 찾기
        if (textComponent == null)
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        // 필수 컴포넌트 확인
        if (textComponent == null)
        {
            Debug.LogError("FloatingText: TextMeshProUGUI 컴포넌트를 찾을 수 없습니다!");
            enabled = false; // 스크립트 비활성화
            return;
        }
        if (canvasGroup == null)
        {
            Debug.LogError("FloatingText: CanvasGroup 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
    }

    public void Initialize(string text, Vector3 worldPosition)
    {
        // 기존 애니메이션 중단
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        // 컴포넌트 상태 확인
        if (textComponent == null || canvasGroup == null)
        {
            Debug.LogError("FloatingText: 필수 컴포넌트가 없습니다!");
            return;
        }

        // 텍스트 설정
        textComponent.text = text;
        
        // 시작 위치 계산 (월드 포지션 + 오프셋)
        startPosition = worldPosition + new Vector3(startOffset.x, startOffset.y, 0);
        transform.position = startPosition;
        
        // 초기 상태 설정
        canvasGroup.alpha = 1f;
        elapsedTime = 0f;
        
        // 플로팅 애니메이션 시작
        currentAnimation = StartCoroutine(FloatingAnimation());
    }

    private IEnumerator FloatingAnimation()
    {
        // 코루틴이 실행되는 동안 계속 반복
        while (elapsedTime < lifetime)
        {
            elapsedTime += Time.deltaTime;
            
            // 진행률 계산 (0~1)
            float progress = elapsedTime / lifetime;
            
            // 위로 상승
            float newY = startPosition.y + (riseSpeed * elapsedTime);
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            
            // 알파 페이드 아웃
            canvasGroup.alpha = 1f - progress;
            
            yield return null; // 다음 프레임까지 대기
        }
        
        // 애니메이션 완료 후 비활성화
        gameObject.SetActive(false);
    }
}