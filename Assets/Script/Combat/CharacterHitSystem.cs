using UnityEngine;

public class CharacterHitSystem : MonoBehaviour
{
    [Header("Collider 설정")]
    [SerializeField] private Collider2D perfectInputArea;  // 완벽 입력 가능 구간
    [SerializeField] private Collider2D characterHitBox;   // 실제 피격 판정 구간
    
    [Header("상태 관리")]
    public bool IsPerfectInputAvailable { get; private set; }
    public bool IsHitTiming { get; private set; }
    
    // 이벤트
    public event System.Action<Projectile> OnProjectileEnterPerfectZone;
    public event System.Action<Projectile> OnProjectileEnterHitZone;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 발사체인지 확인
        if (!other.CompareTag("Projectile")) return;
        
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile == null) return;
        
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
        Debug.Log($"[CharacterHitSystem] PerfectInputArea 진입 - 완벽 입력 가능");
    }
    
    private void HandlePerfectInputAreaExit(Projectile projectile)
    {
        IsPerfectInputAvailable = false;
        Debug.Log($"[CharacterHitSystem] PerfectInputArea 이탈 - 완벽 입력 불가");
    }
    
    private void HandleCharacterHitBoxEnter(Projectile projectile)
    {
        IsHitTiming = true;
        OnProjectileEnterHitZone?.Invoke(projectile);
        Debug.Log($"[CharacterHitSystem] CharacterHitBox 진입 - 피격 판정 발생");
    }
    
    public void ResetHitState()
    {
        IsPerfectInputAvailable = false;
        IsHitTiming = false;
        Debug.Log($"[CharacterHitSystem] 히트 상태 초기화");
    }
}
