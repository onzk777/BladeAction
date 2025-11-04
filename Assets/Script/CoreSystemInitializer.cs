using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// CoreSystemScene 초기화 및 Scene 로딩 관리
/// CoreSystemScene이 시작될 때 필요한 Scene들을 자동으로 로드합니다
/// </summary>
public class CoreSystemInitializer : MonoBehaviour
{
    [Header("Scene 설정")]
    [Tooltip("PersistentUI Scene 이름")]
    [SerializeField] private string persistentUISceneName = "02.PersistentUIScene";
    
    [Tooltip("초기 로드할 Content Scene 이름 (비워두면 로드 안함)")]
    [SerializeField] private string initialContentSceneName = "";
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;

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

