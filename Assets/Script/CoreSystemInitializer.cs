using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CoreSystemScene 초기화 및 Scene 로딩 관리
/// CoreSystemScene이 시작될 때 필요한 Scene들을 자동으로 로드합니다
/// </summary>
public class CoreSystemInitializer : MonoBehaviour
{
    [Header("Scene 설정")]
    [Tooltip("공통 UI Scene (항상 로드됨)")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset persistentUISceneAsset;
#endif
    [HideInInspector]
    [SerializeField] private string persistentUISceneName = "";
    
    [Tooltip("게임 시작 시 처음 보여줄 Scene (TitleScene=정식, TestScene=개발용, null=빈화면)")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset initialContentSceneAsset;
#endif
    [HideInInspector]
    [SerializeField] private string initialContentSceneName = "";
    
    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // SceneAsset이 변경되면 자동으로 Scene 이름 업데이트
        if (persistentUISceneAsset != null)
            persistentUISceneName = persistentUISceneAsset.name;
        
        if (initialContentSceneAsset != null)
            initialContentSceneName = initialContentSceneAsset.name;
        else
            initialContentSceneName = ""; // null이면 빈 문자열
    }
#endif

    private void Start()
    {
        Log("CoreSystemScene 초기화 시작");
        StartCoroutine(InitializeScenes());
    }

    private IEnumerator InitializeScenes()
    {
        // 1. Core Systems 초기화 대기
        yield return new WaitForSeconds(0.1f);
        Log("Core Systems 초기화 완료");

        // 2. PersistentUIScene Additive 로드
        if (!string.IsNullOrEmpty(persistentUISceneName))
        {
            if (!IsSceneLoaded(persistentUISceneName))
            {
                Log($"PersistentUIScene 로드 시작: {persistentUISceneName}");
                AsyncOperation loadPersistentUI = SceneManager.LoadSceneAsync(persistentUISceneName, LoadSceneMode.Additive);
                
                while (!loadPersistentUI.isDone)
                {
                    yield return null;
                }
                
                Log($"PersistentUIScene 로드 완료: {persistentUISceneName}");
            }
            else
            {
                Log($"PersistentUIScene 이미 로드됨: {persistentUISceneName}");
            }
        }

        // 3. 초기 Content Scene 로드
        if (!string.IsNullOrEmpty(initialContentSceneName))
        {
            if (!IsSceneLoaded(initialContentSceneName))
            {
                Log($"초기 Content Scene 로드 시작: {initialContentSceneName}");
                AsyncOperation loadContent = SceneManager.LoadSceneAsync(initialContentSceneName, LoadSceneMode.Additive);
                
                while (!loadContent.isDone)
                {
                    yield return null;
                }
                
                Log($"초기 Content Scene 로드 완료: {initialContentSceneName}");
                
                // Active Scene 설정 (선택사항)
                Scene contentScene = SceneManager.GetSceneByName(initialContentSceneName);
                if (contentScene.IsValid())
                {
                    SceneManager.SetActiveScene(contentScene);
                    Log($"Active Scene 설정: {initialContentSceneName}");
                }
                
                // SceneTransitionManager에 현재 Content Scene 등록
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.SetCurrentContentScene(initialContentSceneName);
                    Log($"SceneTransitionManager에 초기 Scene 등록: {initialContentSceneName}");
                }
            }
            else
            {
                Log($"초기 Content Scene 이미 로드됨: {initialContentSceneName}");
            }
        }

        Log("모든 Scene 초기화 완료");
    }

    /// <summary>
    /// Scene이 이미 로드되어 있는지 확인
    /// </summary>
    private bool IsSceneLoaded(string sceneName)
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

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[CoreSystemInitializer] {message}");
        }
    }
}

