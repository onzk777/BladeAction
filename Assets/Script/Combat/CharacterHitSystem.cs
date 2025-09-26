using UnityEngine;

public class CharacterHitSystem : MonoBehaviour
{
    private readonly System.Collections.Generic.HashSet<Projectile> trackedProjectiles = new System.Collections.Generic.HashSet<Projectile>();
    public void RegisterProjectile(Projectile projectile)
    {
        if (projectile == null) return;
        if (trackedProjectiles.Contains(projectile)) return;
        trackedProjectiles.Add(projectile);
        projectile.OnProjectileEnterPerfectZone += HandleProjectileEnterPerfectZone;
        projectile.OnProjectileEnterHitZone += HandleProjectileEnterHitZone;
        projectile.OnProjectileHit += HandleProjectileHit;
    }

    public void UnregisterProjectile(Projectile projectile)
    {
        if (projectile == null) return;
        if (!trackedProjectiles.Remove(projectile)) return;
        projectile.OnProjectileEnterPerfectZone -= HandleProjectileEnterPerfectZone;
        projectile.OnProjectileEnterHitZone -= HandleProjectileEnterHitZone;
        projectile.OnProjectileHit -= HandleProjectileHit;
    }
    [Header("Collider 설정")]
    [SerializeField] private Collider2D perfectInputArea;  // 완벽 입력 가능 구간
    [SerializeField] private Collider2D characterHitBox;   // 실제 피격 판정 구간
    
    [Header("상태 관리")]
    public bool IsPerfectInputAvailable { get; private set; }
    public bool IsHitTiming { get; private set; }
    
    // 이벤트
    public event System.Action<Projectile> OnProjectileEnterPerfectZone;
    public event System.Action<Projectile> OnProjectileEnterHitZone;
    
    private void Start()
    {
        // 충돌체 위치 확인
        if (perfectInputArea != null)
        {
            Debug.Log($"[CharacterHitSystem] PerfectInputArea 위치: {perfectInputArea.transform.position}, 태그: {perfectInputArea.tag}");
        }
        if (characterHitBox != null)
        {
            Debug.Log($"[CharacterHitSystem] CharacterHitBox 위치: {characterHitBox.transform.position}, 태그: {characterHitBox.tag}");
        }
        
        // 🆕 충돌체 정보 상세 로그
        LogColliderInfo();
        
        // Physics2D 설정 확인
        CheckPhysics2DSettings();
    }
    
    // 디버깅 메서드 제거됨
    
    private void LogColliderInfo()
    {
        // 충돌체 정보 로그 (디버깅 로그 제거됨)
    }
    
    private void CheckPhysics2DSettings()
    {
        // Layer 0과 Layer 0 충돌 강제 활성화 (Unity 자동 충돌 감지 문제로 인한 임시 해결책)
        if (Physics2D.GetIgnoreLayerCollision(0, 0))
        {
            Physics2D.IgnoreLayerCollision(0, 0, false);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 발사체인지 확인
        if (!other.CompareTag("Projectile")) 
        {
            return;
        }
        
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile == null) 
        {
            return;
        }
        
        // Projectile 이벤트 구독
        projectile.OnProjectileEnterPerfectZone += HandleProjectileEnterPerfectZone;
        projectile.OnProjectileEnterHitZone += HandleProjectileEnterHitZone;
        projectile.OnProjectileHit += HandleProjectileHit;
        
        // PerfectInputArea 충돌 감지
        if (other == perfectInputArea)
        {
            HandlePerfectInputAreaEnter(projectile);
        }
        // CharacterHitBox 충돌 감지
        else if (other == characterHitBox)
        {
            HandleCharacterHitBoxEnter(projectile);
        }
    }
    
    // 강제 충돌 감지 (Unity 자동 충돌 감지 문제로 인한 임시 해결책)
    private void Update()
    {
        // 매 0.5초마다 발사체와의 충돌을 강제로 확인
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[CharacterHitSystem] Update 호출 - 프레임: {Time.frameCount}");
            CheckForProjectiles();
        }
    }
    
    private void CheckForProjectiles()
    {
        GameObject[] projectiles = GameObject.FindGameObjectsWithTag("Projectile");
        Debug.Log($"[CharacterHitSystem] 발사체 검색: {projectiles.Length}개 발견");
        
        foreach (GameObject projectileObj in projectiles)
        {
            if (projectileObj.activeInHierarchy)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    Debug.Log($"[CharacterHitSystem] 활성화된 발사체 확인: {projectileObj.name}");
                    CheckProjectileOverlap(projectileObj);
                }
            }
        }
    }
    
    private void CheckProjectileOverlap(GameObject projectileObj)
    {
        // PerfectInputArea와의 겹침 확인
        if (perfectInputArea != null)
        {
            Collider2D perfectCollider = perfectInputArea.GetComponent<Collider2D>();
            if (perfectCollider != null)
            {
                bool isOverlapping = perfectCollider.OverlapPoint(projectileObj.transform.position);
                if (isOverlapping)
                {
                    OnTriggerEnter2D(projectileObj.GetComponent<Collider2D>());
                }
            }
        }
        
        // CharacterHitBox와의 겹침 확인
        if (characterHitBox != null)
        {
            Collider2D hitCollider = characterHitBox.GetComponent<Collider2D>();
            if (hitCollider != null)
            {
                bool isOverlapping = hitCollider.OverlapPoint(projectileObj.transform.position);
                if (isOverlapping)
                {
                    OnTriggerEnter2D(projectileObj.GetComponent<Collider2D>());
                }
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // 발사체인지 확인
        if (!other.CompareTag("Projectile")) return;
        
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile == null) return;
        
        // PerfectInputArea에서 나갈 때
        if (other == perfectInputArea)
        {
            HandlePerfectInputAreaExit(projectile);
        }
    }
    
    private void HandlePerfectInputAreaEnter(Projectile projectile)
    {
        IsPerfectInputAvailable = true;
        OnProjectileEnterPerfectZone?.Invoke(projectile);
    }
    
    private void HandlePerfectInputAreaExit(Projectile projectile)
    {
        IsPerfectInputAvailable = false;
    }
    
    private void HandleCharacterHitBoxEnter(Projectile projectile)
    {
        IsHitTiming = true;
        OnProjectileEnterHitZone?.Invoke(projectile);
    }
    
    public void ResetHitState()
    {
        IsPerfectInputAvailable = false;
        IsHitTiming = false;
    }
    
    // Projectile 이벤트 핸들러
    private void HandleProjectileEnterPerfectZone(Projectile projectile)
    {
        HandlePerfectInputAreaEnter(projectile);
    }
    
    private void HandleProjectileEnterHitZone(Projectile projectile)
    {
        HandleCharacterHitBoxEnter(projectile);
    }
    
    private void HandleProjectileHit(Projectile projectile)
    {
        Debug.Log($"[CharacterHitSystem] 🚨 HandleProjectileHit 호출 - 발사체: {projectile.name}");
        // CombatManager의 OnProjectileHit 이벤트 호출
        CombatManager.Instance.OnProjectileHit(projectile);
    }
}
