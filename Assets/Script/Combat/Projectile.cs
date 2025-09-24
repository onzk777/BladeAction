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
        // 🆕 발사체 상태 로그 (디버깅용)
        if (Time.frameCount % 60 == 0) // 1초마다 로그
        {
            Debug.Log($"[Projectile] 상태: isLaunched={isLaunched}, isCompleted={isCompleted}, lifetime={currentLifetime}/{lifetime}");
        }
        
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
            
            // 발사체 이동 (디버깅 로그 제거)
            
            
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
        
        // 발사체 발사 (디버깅 로그 제거)
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Projectile] 🚨 OnTriggerEnter2D 호출됨: {other.name}, tag={other.tag}, isFromPlayer={isFromPlayer}");
        
        // 🆕 공격자 자기 자신과의 충돌 방지 (우선 처리)
        // 부모 오브젝트까지 확인
        Transform current = other.transform;
        while (current != null)
        {
            if (isFromPlayer && current.GetComponent<PlayerController>() != null)
            {
                Debug.Log($"[Projectile] 플레이어 발사체가 플레이어 계층과 충돌 - 무시");
                return;
            }
            if (!isFromPlayer && current.GetComponent<EnemyController>() != null)
            {
                Debug.Log($"[Projectile] 적 발사체가 적 계층과 충돌 - 무시");
                return;
            }
            current = current.parent;
        }
        
        // PerfectInputArea/CharacterHitBox는 항상 허용
        if (other.CompareTag("PerfectInputArea") || other.CompareTag("CharacterHitBox"))
        {
            Debug.Log($"[Projectile] PerfectInputArea/CharacterHitBox 충돌 허용: {other.tag}");
            // 태그 기반 충돌체 구분
            switch (other.tag)
            {
                case "PerfectInputArea":
                    HandlePerfectInputArea(other);
                    break;
                case "CharacterHitBox":
                    Debug.Log($"[Projectile] 🚨 CharacterHitBox 충돌 감지 - HandleCharacterHitBox 호출");
                    HandleCharacterHitBox(other);
                    break;
            }
        }
        else
        {
            Debug.Log($"[Projectile] 알 수 없는 충돌체: {other.name}, tag={other.tag}");
        }
    }
    
    private void HandlePerfectInputArea(Collider2D other)
    {
        // 🆕 방어자에게 완벽 입력 기회 제공
        OnProjectileEnterPerfectZone?.Invoke(this);
    }
    
    private void HandleCharacterHitBox(Collider2D other)
    {
        Debug.Log($"[Projectile] 🚨 HandleCharacterHitBox 호출됨");
        // 🆕 즉시 피격 판정 발생
        OnProjectileEnterHitZone?.Invoke(this);
        HandleHit(other);
    }
    
    private void HandleHit(Collider2D hitCollider)
    {
        Debug.Log($"[Projectile] 🚨 HandleHit 호출됨 - isCompleted: {isCompleted}");
        if (isCompleted) 
        {
            Debug.Log($"[Projectile] 🚨 이미 완료된 발사체 - HandleHit 무시");
            return;
        }
        
        Debug.Log($"[Projectile] 🚨 OnProjectileHit 이벤트 발생");
        isCompleted = true;
        
        // 🆕 발사체 즉시 소멸 (이벤트 발생 전에 소멸하여 중복 충돌 방지)
        DestroyProjectile();
        
        // 🆕 발사체 소멸 후 이벤트 발생 (중복 충돌 방지)
        OnProjectileHit?.Invoke(this);
    }
    
    private void DestroyProjectile()
    {
        OnProjectileCompleted?.Invoke(this);
        Destroy(gameObject);
    }
    
    // 디버깅 메서드들 제거됨
}
