using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using BladeAction.UI;

/// <summary>
/// InputSystem 상태를 진단하는 에디터 유틸리티
/// </summary>
public class InputSystemDiagnostics : MonoBehaviour
{
    [MenuItem("Tools/Input Diagnostics/Print Full Status")]
    public static void PrintFullStatus()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Play Mode에서만 실행 가능합니다.");
            return;
        }

        Debug.Log("========== INPUT SYSTEM 진단 시작 ==========");

        // 1. GameInputManager 확인
        if (GameInputManager.Instance != null)
        {
            Debug.Log("✅ GameInputManager.Instance 존재");
            
            var playerInput = GameInputManager.Instance.GetPlayerInput();
            if (playerInput != null)
            {
                Debug.Log($"✅ PlayerInput 존재: {playerInput.name}");
                Debug.Log($"   - Current Action Map: {playerInput.currentActionMap?.name ?? "NULL"}");
                Debug.Log($"   - Actions Asset: {(playerInput.actions != null ? "존재" : "NULL")}");
                
                if (playerInput.actions != null)
                {
                    // 모든 Action Map 상태 출력
                    Debug.Log("   === Action Maps 상태 ===");
                    foreach (var actionMap in playerInput.actions.actionMaps)
                    {
                        Debug.Log($"   - {actionMap.name}: {(actionMap.enabled ? "✅ Enabled" : "❌ Disabled")}");
                        
                        // 각 Action Map의 Action들 출력
                        foreach (var action in actionMap.actions)
                        {
                            Debug.Log($"      └─ {action.name}: {(action.enabled ? "✅" : "❌")}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("❌ PlayerInput이 NULL!");
            }
        }
        else
        {
            Debug.LogError("❌ GameInputManager.Instance가 NULL!");
        }

        // 2. EventSystem 확인
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            Debug.Log($"✅ EventSystem 존재: {eventSystem.name}");
            Debug.Log($"   - Enabled: {eventSystem.enabled}");
            Debug.Log($"   - Current Selected: {eventSystem.currentSelectedGameObject?.name ?? "NULL"}");
        }
        else
        {
            Debug.LogError("❌ EventSystem이 NULL!");
        }

        // 3. MainMenuManager 확인
        var mainMenuManager = FindAnyObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        if (mainMenuManager != null)
        {
            Debug.Log($"✅ MainMenuManager 존재: {mainMenuManager.name}");
            Debug.Log($"   - GameObject Active: {mainMenuManager.gameObject.activeSelf}");
            Debug.Log($"   - GameObject ActiveInHierarchy: {mainMenuManager.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("❌ MainMenuManager를 찾을 수 없음!");
        }

        // 4. Canvas 확인
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"Canvas 개수: {canvases.Length}");
        foreach (var canvas in canvases)
        {
            Debug.Log($"   - {canvas.name}: Active={canvas.gameObject.activeSelf}, RenderMode={canvas.renderMode}");
            
            // Graphic Raycaster 확인
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log($"      Graphic Raycaster: {(raycaster != null ? "✅" : "❌")}");
        }

        Debug.Log("========== INPUT SYSTEM 진단 종료 ==========");
    }
}

