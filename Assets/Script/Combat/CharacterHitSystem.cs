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
    [Header("상태 관리")]
    public bool IsPerfectInputAvailable { get; private set; }
    public bool IsHitTiming { get; private set; }
    
    // 이벤트
    public event System.Action<Projectile> OnProjectileEnterPerfectZone;
    public event System.Action<Projectile> OnProjectileEnterHitZone;
    
    private void Start()
    {
        // Physics2D 설정 확인
        CheckPhysics2DSettings();
    }
    
    private void CheckPhysics2DSettings()
    {
        // Layer 0과 Layer 0 충돌 강제 활성화 (Unity 자동 충돌 감지 문제로 인한 임시 해결책)
        if (Physics2D.GetIgnoreLayerCollision(0, 0))
        {
            Physics2D.IgnoreLayerCollision(0, 0, false);
        }
    }
    
    private void HandlePerfectInputAreaEnter(Projectile projectile)
    {
        IsPerfectInputAvailable = true;
        OnProjectileEnterPerfectZone?.Invoke(projectile);
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
