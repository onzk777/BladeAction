using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Scene 전환을 관리하는 매니저
/// CoreSystemScene에 배치되어 모든 Content Scene 전환을 제어
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene 관리")]
    [Tooltip("현재 로드된 Content Scene 이름 (읽기 전용)")]
    [SerializeField] private string currentContentScene = "";
    [Tooltip("Scene 전환 중 여부 (읽기 전용)")]
    [SerializeField] private bool isTransitioning = false;

    [Header("Fade 설정")]
    [Tooltip("Scene 전환 시 Fade Out 기본 지속 시간 (초)")]
    [SerializeField] private float defaultFadeOutDuration = 0.5f;
    [Tooltip("Scene 전환 시 Fade In 기본 지속 시간 (초)")]
    [SerializeField] private float defaultFadeInDuration = 0.5f;

    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;

    // 전환 중인지 확인
    public bool IsTransitioning => isTransitioning;
    public string CurrentContentScene => currentContentScene;

    // 전환 완료 이벤트
    public event System.Action<string> OnSceneTransitionComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Log("SceneTransitionManager 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[SceneTransitionManager] 중복된 인스턴스 발견, 제거합니다.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Scene 전환 (Fade 효과 포함)
    /// 현재 Content Scene을 언로드하고 새 Scene을 로드
    /// </summary>
    public void TransitionToScene(string targetSceneName, float fadeOutDuration = -1f, float fadeInDuration = -1f)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneTransitionManager] 이미 Scene 전환 중입니다! (현재: {currentContentScene} → {targetSceneName})");
            return;
        }

        if (fadeOutDuration < 0) fadeOutDuration = defaultFadeOutDuration;
        if (fadeInDuration < 0) fadeInDuration = defaultFadeInDuration;

        StartCoroutine(TransitionCoroutine(targetSceneName, fadeOutDuration, fadeInDuration));
    }

    /// <summary>
    /// Scene 전환 Coroutine
    /// </summary>
    private IEnumerator TransitionCoroutine(string targetSceneName, float fadeOutDuration, float fadeInDuration)
    {
        isTransitioning = true;
        Log($"Scene 전환 시작: {currentContentScene} → {targetSceneName}");

        // 1. Fade Out
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeOut(fadeOutDuration);
            yield return new WaitForSeconds(fadeOutDuration);
        }
        else
        {
            Debug.LogWarning("[SceneTransitionManager] FadeController를 찾을 수 없습니다. Fade 효과 없이 전환합니다.");
        }

        // 2. 현재 Content Scene 언로드
        if (!string.IsNullOrEmpty(currentContentScene))
        {
            Log($"기존 Scene 언로드 시작: {currentContentScene}");
            yield return UnloadSceneCoroutine(currentContentScene);
        }

        // 3. 새 Content Scene 로드
        Log($"새 Scene 로드 시작: {targetSceneName}");
        yield return LoadSceneCoroutine(targetSceneName);

        // 4. 현재 Scene 업데이트
        currentContentScene = targetSceneName;

        // 5. Fade In
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeIn(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }

        isTransitioning = false;
        Log($"Scene 전환 완료: {targetSceneName}");

        // 6. 전환 완료 이벤트 발생
        OnSceneTransitionComplete?.Invoke(targetSceneName);
    }

    /// <summary>
    /// Scene Additive 로드 (Fade 효과 없음)
    /// </summary>
    public void LoadSceneAdditive(string sceneName)
    {
        if (IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneTransitionManager] Scene이 이미 로드되어 있습니다: {sceneName}");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Scene 언로드 (Fade 효과 없음)
    /// </summary>
    public void UnloadScene(string sceneName)
    {
        if (!IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneTransitionManager] Scene이 로드되어 있지 않습니다: {sceneName}");
            return;
        }

        StartCoroutine(UnloadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Scene 로드 Coroutine
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        if (IsSceneLoaded(sceneName))
        {
            Log($"Scene이 이미 로드되어 있습니다: {sceneName}");
            yield break;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (asyncLoad == null)
        {
            Debug.LogError($"[SceneTransitionManager] Scene 로드 실패: {sceneName}");
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            float progress = asyncLoad.progress * 100f;
            // 필요시 로딩 UI 업데이트 가능
            yield return null;
        }

        Log($"Scene 로드 완료: {sceneName}");

        // Active Scene 설정 (Content Scene인 경우)
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && !sceneName.Contains("CoreSystem") && !sceneName.Contains("PersistentUI"))
        {
            SceneManager.SetActiveScene(loadedScene);
            Log($"Active Scene 설정: {sceneName}");
        }
    }

    /// <summary>
    /// Scene 언로드 Coroutine
    /// </summary>
    private IEnumerator UnloadSceneCoroutine(string sceneName)
    {
        if (!IsSceneLoaded(sceneName))
        {
            Log($"Scene이 로드되어 있지 않아 언로드를 건너뜁니다: {sceneName}");
            yield break;
        }

        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);

        if (asyncUnload == null)
        {
            Debug.LogError($"[SceneTransitionManager] Scene 언로드 실패: {sceneName}");
            yield break;
        }

        while (!asyncUnload.isDone)
        {
            yield return null;
        }

        Log($"Scene 언로드 완료: {sceneName}");

        // 메모리 정리
        yield return Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// Scene이 로드되어 있는지 확인
    /// </summary>
    public bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 로드된 모든 Scene 이름 반환 (디버그용)
    /// </summary>
    public List<string> GetLoadedScenes()
    {
        List<string> loadedScenes = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            loadedScenes.Add(scene.name);
        }
        return loadedScenes;
    }

    /// <summary>
    /// 현재 Content Scene 강제 설정 (초기화 시 사용)
    /// </summary>
    public void SetCurrentContentScene(string sceneName)
    {
        currentContentScene = sceneName;
        Log($"현재 Content Scene 설정됨: {sceneName}");
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SceneTransitionManager] {message}");
        }
    }

    // 디버그용: 현재 로드된 Scene 목록 출력
    [ContextMenu("로드된 Scene 목록 출력")]
    private void PrintLoadedScenes()
    {
        List<string> scenes = GetLoadedScenes();
        Debug.Log($"[SceneTransitionManager] 로드된 Scene 목록 ({scenes.Count}개):");
        foreach (string sceneName in scenes)
        {
            Debug.Log($"  - {sceneName}");
        }
    }
}

