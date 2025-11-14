using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

/// <summary>
/// HP 패널 컨트롤러 - TeamA/TeamB HP Bar를 비율 기반으로 크기 조정
/// </summary>
public class HPPanelController : MonoBehaviour
{
    [Header("HP Bar References")]
    [FormerlySerializedAs("playerHPBar")]
    [Tooltip("TeamA HP 바 오브젝트 (좌측)")]
    public RectTransform teamAHPBar;
    
    [FormerlySerializedAs("enemyHPBar")]
    [Tooltip("TeamB HP 바 오브젝트 (우측)")]
    public RectTransform teamBHPBar;

    [Header("Animation Settings")]
    [Tooltip("크기 변화 애니메이션 속도")]
    [Range(0.1f, 5f)]
    public float animationSpeed = 2f;

    [Header("Debug")]
    [Tooltip("디버그 모드 활성화")]
    public bool debugMode = false;

    // 현재 HP 값들 (캐싱용)
    private int currentTeamAHP = 0;
    private int currentTeamBHP = 0;
    private int maxTeamAHP = 0;
    private int maxTeamBHP = 0;

    private Character teamACharacter;
    private Character teamBCharacter;
    private CombatCharacterManager.CombatantSlot teamASlot;
    private CombatCharacterManager.CombatantSlot teamBSlot;

    // 애니메이션 관련
    private Coroutine sizeAnimationCoroutine;

    private void Awake()
    {
        if (teamAHPBar == null)
        {
            Debug.LogError("[HPPanelController] TeamA HP Bar가 할당되지 않았습니다!");
        }
        if (teamBHPBar == null)
        {
            Debug.LogError("[HPPanelController] TeamB HP Bar가 할당되지 않았습니다!");
        }
    }

    private void Start()
    {
        StartCoroutine(WaitForCharacterManager());
    }

    private IEnumerator WaitForCharacterManager()
    {
        while (CombatCharacterManager.Instance == null)
        {
            yield return null;
        }

        var manager = CombatCharacterManager.Instance;
        while (manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA)?.Character == null ||
               manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB)?.Character == null)
        {
            yield return null;
        }

        InitializeSlotsAndEvents();
    }

    private void InitializeSlotsAndEvents()
    {
        BindLeaderSlots();
        RefreshTeamAHPStats();
        RefreshTeamBHPStats();
        SubscribeToHPEvents();
        CombatCharacterManager.OnLeaderSlotChanged += HandleLeaderSlotChanged;
        UpdatePanelSizes();
    }

    private void BindLeaderSlots()
    {
        var manager = CombatCharacterManager.Instance;
        teamASlot = manager?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA);
        teamBSlot = manager?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB);
        teamACharacter = teamASlot?.Character;
        teamBCharacter = teamBSlot?.Character;
    }

    private void HandleLeaderSlotChanged(CombatCharacterManager.CombatTeam team, CombatCharacterManager.CombatantSlot previousSlot, CombatCharacterManager.CombatantSlot newSlot)
    {
        if (team == CombatCharacterManager.CombatTeam.TeamA)
        {
            UpdateTeamCharacter(ref teamACharacter, previousSlot, newSlot, OnTeamAHPChanged);
            teamASlot = newSlot;
            RefreshTeamAHPStats();
        }
        else if (team == CombatCharacterManager.CombatTeam.TeamB)
        {
            UpdateTeamCharacter(ref teamBCharacter, previousSlot, newSlot, OnTeamBHPChanged);
            teamBSlot = newSlot;
            RefreshTeamBHPStats();
        }

        UpdatePanelSizes();
    }

    private void UpdateTeamCharacter(ref Character cachedCharacter, CombatCharacterManager.CombatantSlot previousSlot, CombatCharacterManager.CombatantSlot newSlot, System.Action<int, int> hpChangedHandler)
    {
        if (cachedCharacter != null)
        {
            cachedCharacter.OnHPChanged -= hpChangedHandler;
        }

        cachedCharacter = newSlot?.Character;

        if (cachedCharacter != null)
        {
            cachedCharacter.OnHPChanged += hpChangedHandler;
        }
    }

    private void SubscribeToHPEvents()
    {
        UnsubscribeFromHPEvents();

        if (teamACharacter != null)
        {
            teamACharacter.OnHPChanged += OnTeamAHPChanged;
        }

        if (teamBCharacter != null)
        {
            teamBCharacter.OnHPChanged += OnTeamBHPChanged;
        }
    }

    private void UnsubscribeFromHPEvents()
    {
        if (teamACharacter != null)
        {
            teamACharacter.OnHPChanged -= OnTeamAHPChanged;
        }

        if (teamBCharacter != null)
        {
            teamBCharacter.OnHPChanged -= OnTeamBHPChanged;
        }
    }

    private void OnTeamAHPChanged(int oldHP, int newHP)
    {
        currentTeamAHP = newHP;
        if (debugMode)
        {
            Debug.Log($"[HPPanelController] TeamA HP 변경: {oldHP} → {newHP}");
        }
        UpdatePanelSizes();
    }

    private void OnTeamBHPChanged(int oldHP, int newHP)
    {
        currentTeamBHP = newHP;
        if (debugMode)
        {
            Debug.Log($"[HPPanelController] TeamB HP 변경: {oldHP} → {newHP}");
        }
        UpdatePanelSizes();
    }

    private void RefreshTeamAHPStats()
    {
        if (teamACharacter != null)
        {
            currentTeamAHP = teamACharacter.currentHP;
            maxTeamAHP = Mathf.RoundToInt(teamACharacter.MaxHP);
        }
        else
        {
            currentTeamAHP = 0;
            maxTeamAHP = 0;
        }

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] TeamA HP 설정 - {currentTeamAHP}/{maxTeamAHP}");
        }
    }

    private void RefreshTeamBHPStats()
    {
        if (teamBCharacter != null)
        {
            currentTeamBHP = teamBCharacter.currentHP;
            maxTeamBHP = Mathf.RoundToInt(teamBCharacter.MaxHP);
        }
        else
        {
            currentTeamBHP = 0;
            maxTeamBHP = 0;
        }

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] TeamB HP 설정 - {currentTeamBHP}/{maxTeamBHP}");
        }
    }

    /// <summary>
    /// HP 패널 크기를 업데이트합니다
    /// </summary>
    public void UpdatePanelSizes()
    {
        if (teamAHPBar == null || teamBHPBar == null)
        {
            if (debugMode)
            {
                Debug.LogWarning("[HPPanelController] HP Bar 참조가 없어 크기 업데이트를 건너뜁니다.");
            }
            return;
        }

        float teamARatio = CalculateTeamARatio();
        float teamBRatio = CalculateTeamBRatio();

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] HP 비율 계산 - TeamA: {teamARatio:F3}, TeamB: {teamBRatio:F3}");
            Debug.Log($"[HPPanelController] 현재 HP - TeamA: {currentTeamAHP}/{maxTeamAHP}, TeamB: {currentTeamBHP}/{maxTeamBHP}");
        }

        if (sizeAnimationCoroutine != null)
        {
            StopCoroutine(sizeAnimationCoroutine);
        }
        sizeAnimationCoroutine = StartCoroutine(AnimatePanelSizes(teamARatio, teamBRatio));
    }

    private float CalculateTeamARatio()
    {
        int totalCurrentHP = currentTeamAHP + currentTeamBHP;
        if (totalCurrentHP <= 0) return 0.5f;

        float ratio = (float)currentTeamAHP / totalCurrentHP;
        return Mathf.Clamp01(ratio);
    }

    private float CalculateTeamBRatio()
    {
        int totalCurrentHP = currentTeamAHP + currentTeamBHP;
        if (totalCurrentHP <= 0) return 0.5f;

        float ratio = (float)currentTeamBHP / totalCurrentHP;
        return Mathf.Clamp01(ratio);
    }

    private IEnumerator AnimatePanelSizes(float targetTeamARatio, float targetTeamBRatio)
    {
        Vector3 currentTeamAScale = teamAHPBar.localScale;
        Vector3 currentTeamBScale = teamBHPBar.localScale;

        Vector3 targetTeamAScale = new Vector3(targetTeamARatio, currentTeamAScale.y, currentTeamAScale.z);
        Vector3 targetTeamBScale = new Vector3(targetTeamBRatio, currentTeamBScale.y, currentTeamBScale.z);

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] 애니메이션 시작 - TeamA Scale: {currentTeamAScale} → {targetTeamAScale}, TeamB Scale: {currentTeamBScale} → {targetTeamBScale}");
        }

        float elapsedTime = 0f;
        float duration = 1f / animationSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            teamAHPBar.localScale = Vector3.Lerp(currentTeamAScale, targetTeamAScale, t);
            teamBHPBar.localScale = Vector3.Lerp(currentTeamBScale, targetTeamBScale, t);

            yield return null;
        }

        teamAHPBar.localScale = targetTeamAScale;
        teamBHPBar.localScale = targetTeamBScale;

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] 애니메이션 완료 - TeamA Scale: {teamAHPBar.localScale}, TeamB Scale: {teamBHPBar.localScale}");
        }

        sizeAnimationCoroutine = null;
    }

    /// <summary>
    /// 즉시 패널 크기를 설정합니다 (애니메이션 없음)
    /// </summary>
    public void SetPanelSizesImmediate()
    {
        if (teamAHPBar == null || teamBHPBar == null) return;

        float teamARatio = CalculateTeamARatio();
        float teamBRatio = CalculateTeamBRatio();

        teamAHPBar.localScale = new Vector3(teamARatio, teamAHPBar.localScale.y, teamAHPBar.localScale.z);
        teamBHPBar.localScale = new Vector3(teamBRatio, teamBHPBar.localScale.y, teamBHPBar.localScale.z);

        if (debugMode)
        {
            Debug.Log($"[HPPanelController] 즉시 크기 설정 완료 - TeamA Scale: {teamARatio:F3}, TeamB Scale: {teamBRatio:F3}");
        }
    }

    /// <summary>
    /// 강제로 HP 패널을 업데이트합니다 (외부에서 호출용)
    /// </summary>
    [ContextMenu("Force Update HP Panels")]
    public void ForceUpdatePanels()
    {
        BindLeaderSlots();
        RefreshTeamAHPStats();
        RefreshTeamBHPStats();
        UpdatePanelSizes();
        if (debugMode)
        {
            Debug.Log("[HPPanelController] HP 패널 강제 업데이트 완료");
        }
    }

    [ContextMenu("Test TeamA Take Damage")]
    public void TestTeamATakeDamage()
    {
        if (teamACharacter != null)
        {
            teamACharacter.TakeDamage(10);
        }
    }

    [ContextMenu("Test TeamB Take Damage")]
    public void TestTeamBTakeDamage()
    {
        if (teamBCharacter != null)
        {
            teamBCharacter.TakeDamage(10);
        }
    }

    private void OnDestroy()
    {
        CombatCharacterManager.OnLeaderSlotChanged -= HandleLeaderSlotChanged;
        UnsubscribeFromHPEvents();

        if (sizeAnimationCoroutine != null)
        {
            StopCoroutine(sizeAnimationCoroutine);
        }
    }
}
