using UnityEngine;

[CreateAssetMenu(fileName = "GameRule", menuName = "Combat/GameRule", order = 2)]
public class GameRule : ScriptableObject
{
    private static GameRule _instance;
    
    public static GameRule Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameRule>("GameRule");
                if (_instance == null)
                {
                    Debug.LogError("[GameRule] Resources/GameRule.asset을 찾을 수 없습니다!");
                }
            }
            return _instance;
        }
    }

    [Header("피해량 감소 설정")]
    [Tooltip("막기 시 피해량 감소 비율 (0~1, 0.5 = 50% 감소)")]
    [Range(0f, 1f)]
    public float guardDamageReduction = 0.5f;
    
    [Tooltip("패리 시 피해량 감소 비율 (0~1, 0 = 100% 감소)")]
    [Range(0f, 1f)]
    public float parryDamageReduction = 1f;
    
    [Tooltip("하프패리 시 피해량 감소 비율 (0~1, 0.5 = 50% 감소)")]
    [Range(0f, 1f)]
    public float halfParryDamageReduction = 0.75f;
    
    [Tooltip("가드브레이크 시 피해량 감소 비율 (0.25 = 25% 감소) - 비율")]
    [Range(0f, 1f)]
    public float guardBreakDamageReduction = 0.25f;

    [Header("피해량 계산 설정")]
    [Tooltip("최소 피해량 (DR 적용 후에도 이 값은 보장됨)")]
    public int minimumDamage = 1;
    
    [Tooltip("치명타 피해 배율 (1.5 = 150% 피해)")]
    public float criticalDamageMultiplier = 1.5f;

    [Header("기본 스테이터스 설정")]
    [Tooltip("기본 패리 시 상대 Poise 감소량")]
    public int defaultParryPoiseDamage = 25;
    
    [Tooltip("Poise 회복 비율 (100 = 100% 회복)")]
    [Range(0, 100)]
    public int poiseRestorePercentage = 100;

    [Header("플레이어 기본 스테이터스")]
    public int playerDefaultMaxHP = 100;
    public int playerDefaultAttack = 20;
    public int playerDefaultDefense = 0;
    public int playerDefaultCriticalChance = 0;
    public int playerDefaultCriticalMultiplier = 150;
    public int playerDefaultMaxPoise = 100;

    [Header("적 기본 스테이터스")]
    public int enemyDefaultMaxHP = 100;
    public int enemyDefaultAttack = 20;
    public int enemyDefaultDefense = 0;
    public int enemyDefaultCriticalChance = 0;
    public int enemyDefaultCriticalMultiplier = 150;
    public int enemyDefaultMaxPoise = 100;

    // 피해량 감소 계산 메서드들
    public float CalculateGuardDamageReduction()
    {
        return 1f - guardDamageReduction;
    }
    
    public float CalculateParryDamageReduction()
    {
        return 1f - parryDamageReduction;
    }
    
    public float CalculateHalfParryDamageReduction()
    {
        return 1f - halfParryDamageReduction;
    }
    
    public float CalculateGuardBreakDamageReduction()
    {
        return 1f - guardBreakDamageReduction;
    }
    
    public int CalculateFinalDamage(int damage, int defenderDR)
    {
        int finalDamage = damage - defenderDR;
        return Mathf.Max(minimumDamage, finalDamage);
    }
    
    public int CalculateCriticalDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * criticalDamageMultiplier);
    }

    // 기본 스테이터스 구조체들
    [System.Serializable]
    public struct DefaultPlayerStats
    {
        public int maxHP;
        public int attack;
        public int defense;
        public int criticalChance;
        public int criticalMultiplier;
        public int maxPoise;
        public int parryPoiseDamage;
    }
    
    [System.Serializable]
    public struct DefaultEnemyStats
    {
        public int maxHP;
        public int attack;
        public int defense;
        public int criticalChance;
        public int criticalMultiplier;
        public int maxPoise;
        public int parryPoiseDamage;
    }

    public DefaultPlayerStats GetDefaultPlayerStats()
    {
        return new DefaultPlayerStats
        {
            maxHP = playerDefaultMaxHP,
            attack = playerDefaultAttack,
            defense = playerDefaultDefense,
            criticalChance = playerDefaultCriticalChance,
            criticalMultiplier = playerDefaultCriticalMultiplier,
            maxPoise = playerDefaultMaxPoise,
            parryPoiseDamage = defaultParryPoiseDamage
        };
    }
    
    public DefaultEnemyStats GetDefaultEnemyStats()
    {
        return new DefaultEnemyStats
        {
            maxHP = enemyDefaultMaxHP,
            attack = enemyDefaultAttack,
            defense = enemyDefaultDefense,
            criticalChance = enemyDefaultCriticalChance,
            criticalMultiplier = enemyDefaultCriticalMultiplier,
            maxPoise = enemyDefaultMaxPoise,
            parryPoiseDamage = defaultParryPoiseDamage
        };
    }
}
