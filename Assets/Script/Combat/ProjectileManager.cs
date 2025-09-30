using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }
    
    [Header("풀링 설정")]
    public GameObject projectilePrefab; // 안전장치용 (현재 사용되지 않음)
    public int poolSize = 20;
    
    [Header("발사체 부모 설정")]
    [SerializeField] private Transform projectileParent; // 발사체들이 생성될 부모 Transform
    
    [Header("발사체 생성 위치 설정")]
    [Tooltip("공격자 위치에서의 발사체 생성 오프셋")]
    public Vector3 spawnOffset = Vector3.zero;
    
    [Tooltip("발사체 생성 높이 오프셋 (Y축)")]
    public float heightOffset = 0.5f;
    
    [Tooltip("발사체 생성 앞쪽 오프셋 (Z축)")]
    public float forwardOffset = 1.0f;
    
    // 🆕 프리팹별 풀링 시스템
    private Dictionary<GameObject, Queue<Projectile>> projectilePools = new Dictionary<GameObject, Queue<Projectile>>();
    private List<Projectile> activeProjectiles;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePool()
    {
        activeProjectiles = new List<Projectile>();
        projectilePools = new Dictionary<GameObject, Queue<Projectile>>();
        
        Debug.Log($"[ProjectileManager] 프리팹별 풀링 시스템 초기화 완료");
    }
    
    /// <summary>
    /// 기본 발사체 프리팹으로 발사체를 가져옵니다 (안전장치용 - 현재 사용되지 않음)
    /// </summary>
    /// <returns>발사체 (projectilePrefab이 null이면 null 반환)</returns>
    public Projectile GetProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[ProjectileManager] projectilePrefab이 설정되지 않음 - null 반환");
            return null;
        }
        return GetProjectile(projectilePrefab);
    }
    
    /// <summary>
    /// 지정된 프리팹으로 발사체를 가져옵니다
    /// </summary>
    /// <param name="prefab">사용할 발사체 프리팹</param>
    /// <returns>활성화된 발사체</returns>
    public Projectile GetProjectile(GameObject prefab)
    {
        // 🆕 프리팹별 풀에서 발사체 가져오기
        if (!projectilePools.ContainsKey(prefab))
        {
            projectilePools[prefab] = new Queue<Projectile>();
        }
        
        Projectile projectile;
        if (projectilePools[prefab].Count > 0)
        {
            // 풀에서 발사체 가져오기
            projectile = projectilePools[prefab].Dequeue();
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            GameObject projectileObj = Instantiate(prefab, projectileParent);
            projectile = projectileObj.GetComponent<Projectile>();
        }
        
        // 발사체 활성화
        projectile.gameObject.SetActive(true);
        activeProjectiles.Add(projectile);
        
        // 🆕 디버그 로그 추가
        Debug.Log($"[PROJECTILE] 생성됨: {prefab.name} → {projectile.gameObject.name}");
        
        return projectile;
    }
    
    /// <summary>
    /// 발사체 생성 위치를 계산합니다
    /// </summary>
    /// <param name="attackerPos">공격자 위치</param>
    /// <param name="defenderPos">방어자 위치</param>
    /// <returns>계산된 발사체 생성 위치</returns>
    public Vector3 CalculateSpawnPosition(Vector3 attackerPos, Vector3 defenderPos)
    {
        // 공격자에서 방어자 방향 계산
        Vector3 direction = (defenderPos - attackerPos).normalized;
        
        // 기본 오프셋 + 방향 기반 오프셋 계산
        Vector3 finalOffset = spawnOffset;
        finalOffset.y += heightOffset; // 높이 오프셋 추가
        finalOffset += direction * forwardOffset; // 앞쪽 오프셋 추가
        
        return attackerPos + finalOffset;
    }
    
    public void ReturnProjectile(Projectile projectile)
    {
        if (projectile == null) return;
        
        // 발사체 비활성화
        projectile.gameObject.SetActive(false);
        
        // 활성 발사체 목록에서 제거
        activeProjectiles.Remove(projectile);
        
        // 🆕 원본 프리팹 찾기 (프리팹별 풀에 반환하기 위해)
        GameObject originalPrefab = FindOriginalPrefab(projectile);
        if (originalPrefab != null && projectilePools.ContainsKey(originalPrefab))
        {
            projectilePools[originalPrefab].Enqueue(projectile);
        }
        else
        {
            // 원본 프리팹을 찾을 수 없으면 제거
            Destroy(projectile.gameObject);
        }
    }
    
    /// <summary>
    /// 발사체의 원본 프리팹을 찾습니다
    /// </summary>
    private GameObject FindOriginalPrefab(Projectile projectile)
    {
        // 발사체 이름에서 원본 프리팹 이름 추출 (Clone 제거)
        string originalName = projectile.gameObject.name.Replace("(Clone)", "");
        
        // 모든 프리팹 중에서 이름이 일치하는 것 찾기
        foreach (var prefab in projectilePools.Keys)
        {
            if (prefab.name == originalName)
            {
                return prefab;
            }
        }
        
        return null;
    }
    
    public void ClearAllProjectiles()
    {
        // 모든 활성 발사체 반환
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            ReturnProjectile(activeProjectiles[i]);
        }
    }
}
