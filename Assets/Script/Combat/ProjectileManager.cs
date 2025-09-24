using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }
    
    [Header("풀링 설정")]
    public GameObject projectilePrefab;
    public int poolSize = 20;
    
    private Queue<Projectile> projectilePool;
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
        projectilePool = new Queue<Projectile>();
        activeProjectiles = new List<Projectile>();
        
        // 풀 크기만큼 발사체 미리 생성
        for (int i = 0; i < poolSize; i++)
        {
            GameObject projectileObj = Instantiate(projectilePrefab);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            
            // 발사체 비활성화
            projectileObj.SetActive(false);
            
            // 풀에 추가
            projectilePool.Enqueue(projectile);
        }
    }
    
    public Projectile GetProjectile()
    {
        Projectile projectile;
        
        if (projectilePool.Count > 0)
        {
            // 풀에서 발사체 가져오기
            projectile = projectilePool.Dequeue();
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            GameObject projectileObj = Instantiate(projectilePrefab);
            projectile = projectileObj.GetComponent<Projectile>();
        }
        
        // 발사체 활성화
        projectile.gameObject.SetActive(true);
        activeProjectiles.Add(projectile);
        
        // 🆕 디버그 로그 추가
        Debug.Log($"[ProjectileManager] 발사체 활성화: position={projectile.transform.position}, active={projectile.gameObject.activeInHierarchy}");
        
        return projectile;
    }
    
    public void ReturnProjectile(Projectile projectile)
    {
        if (projectile == null) return;
        
        // 발사체 비활성화
        projectile.gameObject.SetActive(false);
        
        // 활성 발사체 목록에서 제거
        activeProjectiles.Remove(projectile);
        
        // 풀에 반환
        projectilePool.Enqueue(projectile);
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
