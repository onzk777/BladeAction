using UnityEngine;

public class ActionCommandSelectionManager : MonoBehaviour
{
    public static ActionCommandSelectionManager Instance { get; private set; }
    
    [Header("UI References")]
    public PlayerActionSelectUI playerActionSelectUI;
    public EnemyActionSelectUI enemyActionSelectUI;
    
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
    
    private void Start()
    {
        // UI 컴포넌트들 자동 찾기
        if (playerActionSelectUI == null)
        {
            playerActionSelectUI = FindFirstObjectByType<PlayerActionSelectUI>();
        }
        
        if (enemyActionSelectUI == null)
        {
            enemyActionSelectUI = FindFirstObjectByType<EnemyActionSelectUI>();
        }
    }
    
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
