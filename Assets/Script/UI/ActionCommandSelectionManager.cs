using UnityEngine;

/// <summary>
/// Action Command UI들을 관리하는 싱글톤 매니저
/// 
/// 사용 방법:
/// 1. Inspector 할당 (기존 방식): SerializeField로 직접 할당
/// 2. 등록 방식 (Scene 분리 대비): UI가 자신을 등록
/// </summary>
public class ActionCommandSelectionManager : MonoBehaviour
{
    public static ActionCommandSelectionManager Instance { get; private set; }
    
    // UI References - 자동으로 찾아서 등록됨 (Inspector에 표시 안함)
    [HideInInspector] [SerializeField] private PlayerActionSelectUI _playerActionSelectUI;
    [HideInInspector] [SerializeField] private EnemyActionSelectUI _enemyActionSelectUI;
    
    // Public 프로퍼티로 읽기 전용 접근 제공
    public PlayerActionSelectUI playerActionSelectUI => _playerActionSelectUI;
    public EnemyActionSelectUI enemyActionSelectUI => _enemyActionSelectUI;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // root GameObject일 때만 DontDestroyOnLoad 적용
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ActionCommandSelectionManager] 싱글톤 인스턴스 생성 및 DontDestroyOnLoad 설정");
            }
            else
            {
                Debug.LogWarning("[ActionCommandSelectionManager] DontDestroyOnLoad는 root GameObject에만 적용됩니다. 부모에서 분리하거나 root로 이동하세요.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Inspector에서 할당되지 않은 경우 자동 찾기 (하위 호환성)
        if (_playerActionSelectUI == null)
        {
            _playerActionSelectUI = FindFirstObjectByType<PlayerActionSelectUI>();
            if (_playerActionSelectUI != null)
            {
                Debug.Log($"[ActionCommandSelectionManager] PlayerActionSelectUI 자동 찾기 완료: {_playerActionSelectUI.name}");
            }
        }
        
        if (_enemyActionSelectUI == null)
        {
            _enemyActionSelectUI = FindFirstObjectByType<EnemyActionSelectUI>();
            if (_enemyActionSelectUI != null)
            {
                Debug.Log($"[ActionCommandSelectionManager] EnemyActionSelectUI 자동 찾기 완료: {_enemyActionSelectUI.name}");
            }
        }
    }
    
    /// <summary>
    /// PlayerActionSelectUI를 등록합니다 (UI의 Awake/Start에서 호출)
    /// </summary>
    public void RegisterPlayerActionUI(PlayerActionSelectUI ui)
    {
        if (ui == null)
        {
            Debug.LogWarning("[ActionCommandSelectionManager] RegisterPlayerActionUI: null UI");
            return;
        }
        
        // 이미 다른 UI가 등록되어 있으면 경고
        if (_playerActionSelectUI != null && _playerActionSelectUI != ui)
        {
            Debug.LogWarning($"[ActionCommandSelectionManager] PlayerActionSelectUI 중복 등록: 기존 {_playerActionSelectUI.name} → 새로 {ui.name}");
        }
        
        _playerActionSelectUI = ui;
        Debug.Log($"[ActionCommandSelectionManager] PlayerActionSelectUI 등록 완료: {ui.name}");
    }
    
    /// <summary>
    /// EnemyActionSelectUI를 등록합니다 (UI의 Awake/Start에서 호출)
    /// </summary>
    public void RegisterEnemyActionUI(EnemyActionSelectUI ui)
    {
        if (ui == null)
        {
            Debug.LogWarning("[ActionCommandSelectionManager] RegisterEnemyActionUI: null UI");
            return;
        }
        
        // 이미 다른 UI가 등록되어 있으면 경고
        if (_enemyActionSelectUI != null && _enemyActionSelectUI != ui)
        {
            Debug.LogWarning($"[ActionCommandSelectionManager] EnemyActionSelectUI 중복 등록: 기존 {_enemyActionSelectUI.name} → 새로 {ui.name}");
        }
        
        _enemyActionSelectUI = ui;
        Debug.Log($"[ActionCommandSelectionManager] EnemyActionSelectUI 등록 완료: {ui.name}");
    }
    
    /// <summary>
    /// 인스턴스가 없으면 생성합니다
    /// </summary>
    public static ActionCommandSelectionManager EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject managerObject = new GameObject("ActionCommandSelectionManager");
            Instance = managerObject.AddComponent<ActionCommandSelectionManager>();
        }
        return Instance;
    }
}
