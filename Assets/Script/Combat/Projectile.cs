using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("발사체 설정")]
    // ❌ 제거: 글로벌 변수들
    // public ActionCommandData sourceCommand;
    // public int hitIndex;
    // public bool isFromPlayer;
    
    // ✅ 내부 변수로 변경
    private ActionCommandData sourceCommand;
    private int hitIndex;
    private bool isFromPlayer;
    
    [Header("물리 설정")]
    public float baseSpeed = 10f;
    public AnimationCurve speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f); // 시간에 따른 속도 배율
    public Vector3 direction;
    public float lifetime = 5f;
    
    [Header("시각적 설정")]
    // ❌ 제거: 프리팹에서 자체적으로 컴포넌트를 들고 있음
    // public SpriteRenderer projectileRenderer;
    // public Collider2D projectileCollider;
    
    // 상태 관리
    private bool isLaunched = false;
    private bool isCompleted = false;
    private float currentLifetime = 0f;
    private float currentSpeed; // 현재 속도
    
    // 이벤트
    public event System.Action<Projectile> OnProjectileHit;
    public event System.Action<Projectile> OnProjectileCompleted;
    public event System.Action<Projectile> OnProjectileEnterPerfectZone;
    public event System.Action<Projectile> OnProjectileEnterHitZone;
    
    private void Update()
    {
        if (isLaunched && !isCompleted)
        {
            // 수명 관리
            currentLifetime += Time.deltaTime;
            
            // Curve 기반 속도 계산
            float normalizedTime = currentLifetime / lifetime;
            float speedMultiplier = speedCurve.Evaluate(normalizedTime);
            currentSpeed = baseSpeed * speedMultiplier;
            
            // 발사체 이동
            transform.Translate(direction * currentSpeed * Time.deltaTime);
            
            if (currentLifetime >= lifetime)
            {
                DestroyProjectile();
            }
        }
    }
    
    public void Initialize(ActionCommandData command, int hit, bool fromPlayer)
    {
        sourceCommand = command;
        hitIndex = hit;
        isFromPlayer = fromPlayer; // 🆕 Controller 기반 식별
        
        // 🆕 발사체 크기 적용
        if (command.projectileScale != 1f)
        {
            transform.localScale = Vector3.one * command.projectileScale;
        }
        
        // 초기 상태 설정
        isLaunched = false;
        isCompleted = false;
        currentLifetime = 0f;
    }
    
    public void Launch(Vector3 direction, float speed)
    {
        this.direction = direction.normalized;
        this.baseSpeed = speed;
        isLaunched = true;
        
        // 🆕 디버그 로그 추가
        Debug.Log($"[Projectile] 발사체 발사: position={transform.position}, direction={this.direction}, speed={speed}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Projectile] 충돌 감지: {other.name}, tag={other.tag}, isFromPlayer={isFromPlayer}");
        
        // 🆕 Controller 기반 발사자 충돌 방지
        if (isFromPlayer && other.GetComponent<PlayerController>() != null)
        {
            Debug.Log($"[Projectile] 플레이어 발사체가 플레이어와 충돌 - 무시");
            return;
        }
        if (!isFromPlayer && other.GetComponent<EnemyController>() != null)
        {
            Debug.Log($"[Projectile] 적 발사체가 적과 충돌 - 무시");
            return;
        }
        
        // 🆕 태그 기반 충돌체 구분
        switch (other.tag)
        {
            case "PerfectInputArea":
                Debug.Log($"[Projectile] PerfectInputArea 충돌");
                HandlePerfectInputArea(other);
                break;
            case "CharacterHitBox":
                Debug.Log($"[Projectile] CharacterHitBox 충돌");
                HandleCharacterHitBox(other);
                break;
            default:
                Debug.Log($"[Projectile] 알 수 없는 태그 충돌: {other.tag}");
                break;
        }
    }
    
    private void HandlePerfectInputArea(Collider2D other)
    {
        // 🆕 방어자에게 완벽 입력 기회 제공
        OnProjectileEnterPerfectZone?.Invoke(this);
    }
    
    private void HandleCharacterHitBox(Collider2D other)
    {
        // 🆕 즉시 피격 판정 발생
        OnProjectileEnterHitZone?.Invoke(this);
        HandleHit(other);
    }
    
    private void HandleHit(Collider2D hitCollider)
    {
        if (isCompleted) return;
        
        isCompleted = true;
        OnProjectileHit?.Invoke(this);
        
        // ❌ 제거: OnProjectileCompleted?.Invoke(this); // 중복 호출 제거
        
        // 발사체 파괴 (OnProjectileCompleted는 DestroyProjectile에서 호출)
        DestroyProjectile();
    }
    
    private void DestroyProjectile()
    {
        OnProjectileCompleted?.Invoke(this);
        Destroy(gameObject);
    }
}
