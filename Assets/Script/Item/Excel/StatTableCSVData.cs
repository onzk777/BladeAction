namespace BladeAction.Item.Excel
{
    /// <summary>
    /// CSV에서 읽은 StatTable 한 행의 데이터
    /// </summary>
    [System.Serializable]
    public class StatTableCSVData
    {
        public string TableKey;
        public string Description;
        
        // 공격 관련
        public float AttackPower;
        
        // 막기 관련
        public float BlockEff;              // BlockEfficiency
        public float BlockPoiseCost;        // BlockPoiseConsumption
        
        // 쳐내기 관련
        public float ParryEff;              // ParryEfficiency
        public float ParryPoiseCost;        // ParryPoiseConsumption
        public float ParryPoiseAtk;         // ParryPoiseAttackPower
        
        // 생존 관련
        public float MaxHP;
        public float DamageReduction;
        public float Poise;
    }
}

