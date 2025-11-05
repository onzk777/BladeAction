using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Scene 전환 시 Fade In/Out 효과를 제공하는 컨트롤러
/// PersistentUIScene에 배치되어 모든 Scene 전환에서 사용됨
/// </summary>
public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [Header("Fade 설정")]
    [Tooltip("Fade 효과에 사용할 Image (검은색 전체 화면)")]
    [SerializeField] private Image fadeImage;
    [Tooltip("기본 Fade 지속 시간 (초)")]
    [SerializeField] private float defaultFadeDuration = 0.5f;
    [Tooltip("알파 제어용 CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;

    private Coroutine currentFadeCoroutine;
    private bool isFading = false;

    public bool IsFading => isFading;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Log("FadeController 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[FadeController] 중복된 인스턴스 발견, 제거합니다.");
            Destroy(gameObject);
            return;
        }

        // 컴포넌트 자동 찾기
        if (fadeImage == null)
        {
            fadeImage = GetComponentInChildren<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // 필수 컴포넌트 확인
        if (fadeImage == null)
        {
            Debug.LogError("[FadeController] Image 컴포넌트를 찾을 수 없습니다!");
        }

        if (canvasGroup == null)
        {
            Debug.LogError("[FadeController] CanvasGroup 컴포넌트를 찾을 수 없습니다!");
        }

        // 초기 상태: FadeImage 비활성화 (에디터에서 설정한 상태 유지)
        if (fadeImage != null && fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(false);
            Log("FadeImage 초기 비활성화");
        }
    }

    /// <summary>
    /// Fade Out (화면이 어두워짐)
    /// </summary>
    public void FadeOut(float duration = -1f)
    {
        if (duration < 0) duration = defaultFadeDuration;
        StartFade(0f, 1f, duration);
    }

    /// <summary>
    /// Fade In (화면이 밝아짐)
    /// </summary>
    public void FadeIn(float duration = -1f)
    {
        if (duration < 0) duration = defaultFadeDuration;
        StartFade(1f, 0f, duration);
    }

    /// <summary>
    /// Fade Out → Callback 실행 → Fade In
    /// </summary>
    public void FadeOutIn(System.Action callback, float fadeOutDuration = -1f, float fadeInDuration = -1f)
    {
        if (fadeOutDuration < 0) fadeOutDuration = defaultFadeDuration;
        if (fadeInDuration < 0) fadeInDuration = defaultFadeDuration;

        StartCoroutine(FadeOutInCoroutine(callback, fadeOutDuration, fadeInDuration));
    }

    /// <summary>
    /// Fade Out → Callback 실행 → Fade In (Coroutine 버전)
    /// </summary>
    public IEnumerator FadeOutInCoroutine(System.Action callback, float fadeOutDuration = -1f, float fadeInDuration = -1f)
    {
        if (fadeOutDuration < 0) fadeOutDuration = defaultFadeDuration;
        if (fadeInDuration < 0) fadeInDuration = defaultFadeDuration;

        // Fade Out
        yield return FadeCoroutine(0f, 1f, fadeOutDuration);

        // Callback 실행
        callback?.Invoke();

        // Fade In
        yield return FadeCoroutine(1f, 0f, fadeInDuration);
    }

    /// <summary>
    /// Fade 시작
    /// </summary>
    private void StartFade(float fromAlpha, float toAlpha, float duration)
    {
        // 기존 Fade 중단
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeCoroutine(fromAlpha, toAlpha, duration));
    }

    /// <summary>
    /// Fade Coroutine
    /// </summary>
    private IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration)
    {
        isFading = true;
        
        // FadeImage 활성화
        if (fadeImage != null && !fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(true);
            Log("FadeImage 활성화");
        }
        
        Log($"Fade 시작: {fromAlpha:F2} → {toAlpha:F2} (Duration: {duration}초)");

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
            SetAlpha(currentAlpha);
            yield return null;
        }

        // 최종 알파값 정확히 설정
        SetAlpha(toAlpha);
        
        // Fade In 완료 시 (alpha 0) FadeImage 비활성화
        if (toAlpha == 0f && fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            Log("FadeImage 비활성화 (Fade In 완료)");
        }
        
        isFading = false;
        Log($"Fade 완료: 최종 알파 = {toAlpha:F2}");
    }

    /// <summary>
    /// 알파값 설정
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }

    /// <summary>
    /// 즉시 투명하게 (Fade In 상태로)
    /// </summary>
    public void SetTransparent()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }
        SetAlpha(0f);
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
        }
        isFading = false;
    }

    /// <summary>
    /// 즉시 불투명하게 (Fade Out 상태로)
    /// </summary>
    public void SetOpaque()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }
        if (fadeImage != null && !fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(true);
        }
        SetAlpha(1f);
        isFading = false;
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[FadeController] {message}");
        }
    }
}

