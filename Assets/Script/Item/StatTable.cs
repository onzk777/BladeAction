using UnityEngine;

namespace BladeAction.Item
{
    [System.Serializable]
    public class StatTable
    {
        [Tooltip("스탯 테이블 키")]
        public string tableKey;
        
        [Tooltip("장비 스탯")]
        public EquipmentStats stats = new EquipmentStats();
        
        [Tooltip("스탯 설명")]
        [TextArea(2, 4)]
        public string description;
    }
}

