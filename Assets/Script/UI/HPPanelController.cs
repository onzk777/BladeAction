using UnityEngine;
using System.Collections;

/// <summary>
/// HP 패널 컨트롤러 - Player와 Enemy의 HP Bar를 비율 기반으로 크기 조정
/// </summary>
public class HPPanelController : MonoBehaviour
{
    [Header("HP Bar References")]
    [Tooltip("플레이어 HP 바 오브젝트")]
    public RectTransform playerHPBar;
    
    [Tooltip("적 HP 바 오브젝트")]
    public RectTransform enemyHPBar;

    [Header("Animation Settings")]
    [Tooltip("크기 변화 애니메이션 속도")]
    [Range(0.1f, 5f)]
    public float animationSpeed = 2f;

    [Header("Debug")]
    [Tooltip("디버그 모드 활성화")]
    public bool debugMode = false;

    // 현재 HP 값들 (캐싱용)
    private int currentPlayerHP = 0;
    private int currentEnemyHP = 0;
    private int maxPlayerHP = 0;
    private int maxEnemyHP = 0;

    // 애니메이션 관련
    private Coroutine sizeAnimationCoroutine;

    private void Awake()
    {
        // HP Bar 참조 검증
        if (playerHPBar == null)
        {
            Debug.LogError("[HPPanelController] PlayerHPBar가 할당되지 않았습니다!");
        }
        else
        {
            Debug.Log($"[HPPanelController] PlayerHPBar 연결됨: {playerHPBar.name}");
        }
        
        if (enemyHPBar == null)
        {
            Debug.LogError("[HPPanelController] EnemyHPBar가 할당되지 않았습니다!");
        }
        else
        {
            Debug.Log($"[HPPanelController] EnemyHPBar 연결됨: {enemyHPBar.name}");
        }
    }

    private void Start()
    {
        // CharacterManager가 초기화될 때까지 대기
        StartCoroutine(WaitForCharacterManager());
    }

    private IEnumerator WaitForCharacterManager()
    {
        // CharacterManager가 초기화될 때까지 대기
        while (CombatCharacterManager.Instance == null)
        {
            yield return null;
        }

        // CharacterManager의 데이터가 준비될 때까지 대기
        while (CombatCharacterManager.Instance.PlayerCharacter == null || CombatCharacterManager.Instance.CurrentEnemy == null)
        {
            yield return null;
        }

        // 초기 HP 값 설정 및 이벤트 구독
        InitializeHPValues();
        SubscribeToHPEvents();
        
        // 초기 패널 크기 설정
        UpdatePanelSizes();
    }

    private void InitializeHPValues()
    {
        if (CombatCharacterManager.Instance?.PlayerCharacter != null)
        {
            currentPlayerHP = CombatCharacterManager.Instance.PlayerCharacter.currentHP;
            maxPlayerHP = (int)CombatCharacterManager.Instance.PlayerCharacter.MaxHP;
        }

        if (CombatCharacterManager.Instance?.CurrentEnemy != null)
        {
            currentEnemyHP = CombatCharacterManager.Instance.CurrentEnemy.currentHP;
            maxEnemyHP = (int)CombatCharacterManager.Instance.CurrentEnemy.MaxHP;
        }

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] 초기 HP 설정 - Player: {currentPlayerHP}/{maxPlayerHP}, Enemy: {currentEnemyHP}/{maxEnemyHP}");
        }
    }

    private void SubscribeToHPEvents()
    {
        // 플레이어 HP 이벤트 구독
        if (CombatCharacterManager.Instance?.PlayerCharacter != null)
        {
            CombatCharacterManager.Instance.PlayerCharacter.OnHPChanged += OnPlayerHPChanged;
        }

        // 적 HP 이벤트 구독
        if (CombatCharacterManager.Instance?.CurrentEnemy != null)
        {
            CombatCharacterManager.Instance.CurrentEnemy.OnHPChanged += OnEnemyHPChanged;
        }
    }

    private void OnPlayerHPChanged(int oldHP, int newHP)
    {
        currentPlayerHP = newHP;
        if (debugMode)
        {
            Debug.Log($"[HPPanelController] 플레이어 HP 변경: {oldHP} → {newHP}");
        }
        UpdatePanelSizes();
    }

    private void OnEnemyHPChanged(int oldHP, int newHP)
    {
        currentEnemyHP = newHP;
        if (debugMode)
        {
            Debug.Log($"[HPPanelController] 적 HP 변경: {oldHP} → {newHP}");
        }
        UpdatePanelSizes();
    }

    /// <summary>
    /// HP 패널 크기를 업데이트합니다
    /// </summary>
    public void UpdatePanelSizes()
    {
        if (playerHPBar == null || enemyHPBar == null)
        {
            Debug.LogWarning("[HPPanelController] HP Bar 참조가 없어 크기 업데이트를 건너뜁니다.");
            Debug.LogWarning($"[HPPanelController] PlayerHPBar: {playerHPBar != null}, EnemyHPBar: {enemyHPBar != null}");
            return;
        }

        // HP 비율 계산
        float playerRatio = CalculatePlayerRatio();
        float enemyRatio = CalculateEnemyRatio();

        Debug.Log($"[HPPanelController] HP 비율 계산 - Player: {playerRatio:F3}, Enemy: {enemyRatio:F3}");
        Debug.Log($"[HPPanelController] 현재 HP - Player: {currentPlayerHP}/{maxPlayerHP}, Enemy: {currentEnemyHP}/{maxEnemyHP}");

        // 애니메이션으로 크기 변경
        if (sizeAnimationCoroutine != null)
        {
            StopCoroutine(sizeAnimationCoroutine);
        }
        sizeAnimationCoroutine = StartCoroutine(AnimatePanelSizes(playerRatio, enemyRatio));
    }

    private float CalculatePlayerRatio()
    {
        int totalCurrentHP = currentPlayerHP + currentEnemyHP;
        if (totalCurrentHP <= 0) return 0.5f; // 둘 다 현재 HP가 0이면 50:50

        // 현재 HP 기준으로 상대적 비율 계산
        float ratio = (float)currentPlayerHP / totalCurrentHP;
        return Mathf.Clamp01(ratio); // 0~1 범위로 제한
    }

    private float CalculateEnemyRatio()
    {
        int totalCurrentHP = currentPlayerHP + currentEnemyHP;
        if (totalCurrentHP <= 0) return 0.5f; // 둘 다 현재 HP가 0이면 50:50

        // 현재 HP 기준으로 상대적 비율 계산
        float ratio = (float)currentEnemyHP / totalCurrentHP;
        return Mathf.Clamp01(ratio); // 0~1 범위로 제한
    }

    private IEnumerator AnimatePanelSizes(float targetPlayerRatio, float targetEnemyRatio)
    {
        // 현재 Scale 가져오기
        Vector3 currentPlayerScale = playerHPBar.localScale;
        Vector3 currentEnemyScale = enemyHPBar.localScale;

        // 목표 Scale 설정
        Vector3 targetPlayerScale = new Vector3(targetPlayerRatio, currentPlayerScale.y, currentPlayerScale.z);
        Vector3 targetEnemyScale = new Vector3(targetEnemyRatio, currentEnemyScale.y, currentEnemyScale.z);

        Debug.Log($"[HPPanelController] 애니메이션 시작 - Player Scale: {currentPlayerScale} → {targetPlayerScale}, Enemy Scale: {currentEnemyScale} → {targetEnemyScale}");

        float elapsedTime = 0f;
        float duration = 1f / animationSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // 부드러운 애니메이션

            // Player HP Bar 크기 조정
            playerHPBar.localScale = Vector3.Lerp(currentPlayerScale, targetPlayerScale, t);
            
            // Enemy HP Bar 크기 조정
            enemyHPBar.localScale = Vector3.Lerp(currentEnemyScale, targetEnemyScale, t);

            yield return null;
        }

        // 최종 크기 설정
        playerHPBar.localScale = targetPlayerScale;
        enemyHPBar.localScale = targetEnemyScale;

        Debug.Log($"[HPPanelController] 애니메이션 완료 - Player Scale: {playerHPBar.localScale}, Enemy Scale: {enemyHPBar.localScale}");

        sizeAnimationCoroutine = null;
    }

    /// <summary>
    /// 즉시 패널 크기를 설정합니다 (애니메이션 없음)
    /// </summary>
    public void SetPanelSizesImmediate()
    {
        if (playerHPBar == null || enemyHPBar == null) return;

        float playerRatio = CalculatePlayerRatio();
        float enemyRatio = CalculateEnemyRatio();

        // Player HP Bar 설정
        playerHPBar.localScale = new Vector3(playerRatio, playerHPBar.localScale.y, playerHPBar.localScale.z);
        
        // Enemy HP Bar 설정
        enemyHPBar.localScale = new Vector3(enemyRatio, enemyHPBar.localScale.y, enemyHPBar.localScale.z);

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] 즉시 크기 설정 완료 - Player Scale: {playerRatio:F3}, Enemy Scale: {enemyRatio:F3}");
        }
    }

    /// <summary>
    /// 강제로 HP 패널을 업데이트합니다 (외부에서 호출용)
    /// </summary>
    [ContextMenu("Force Update HP Panels")]
    public void ForceUpdatePanels()
    {
        InitializeHPValues();
        UpdatePanelSizes();
        Debug.Log("[HPPanelController] HP 패널 강제 업데이트 완료");
    }

    /// <summary>
    /// 테스트용 HP 변경 (디버그용)
    /// </summary>
    [ContextMenu("Test Player Take Damage")]
    public void TestPlayerTakeDamage()
    {
        if (CombatCharacterManager.Instance?.PlayerCharacter != null)
        {
            CombatCharacterManager.Instance.PlayerCharacter.TakeDamage(10);
        }
    }

    [ContextMenu("Test Enemy Take Damage")]
    public void TestEnemyTakeDamage()
    {
        if (CombatCharacterManager.Instance?.CurrentEnemy != null)
        {
            CombatCharacterManager.Instance.CurrentEnemy.TakeDamage(10);
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CombatCharacterManager.Instance?.PlayerCharacter != null)
        {
            CombatCharacterManager.Instance.PlayerCharacter.OnHPChanged -= OnPlayerHPChanged;
        }

        if (CombatCharacterManager.Instance?.CurrentEnemy != null)
        {
            CombatCharacterManager.Instance.CurrentEnemy.OnHPChanged -= OnEnemyHPChanged;
        }

        // 코루틴 정리
        if (sizeAnimationCoroutine != null)
        {
            StopCoroutine(sizeAnimationCoroutine);
        }
    }
}
