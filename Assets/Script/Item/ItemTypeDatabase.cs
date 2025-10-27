using UnityEngine;
using System.Collections.Generic;

namespace BladeAction.Item
{
    // 인라인 타입 엔트리 정의 (ScriptableObject 불필요)
    [System.Serializable]
    public class WeaponTypeEntry
    {
        [Header("기본 정보")]
        public string typeName;
        public string typeKey;
        public Sprite typeIcon;
        [TextArea(3, 5)] public string description;

        [Header("무기 특성")]
        public WeaponCategory category = WeaponCategory.Melee;
        public float baseAttackSpeed = 1.0f;
        public float baseRange = 1.0f;
    }

    [System.Serializable]
    public class ArmorTypeEntry
    {
        [Header("기본 정보")]
        public string typeName;
        public string typeKey;
        public Sprite typeIcon;
        [TextArea(3, 5)] public string description;

        [Header("방어구 특성")]
        public ArmorCategory category = ArmorCategory.Light;
        public float baseWeight = 1.0f;
        [Range(0f, 1f)] public float baseMobility = 1.0f;
        public int requiredLevel = 1;
    }

    [System.Serializable]
    public class AccessoryTypeEntry
    {
        [Header("기본 정보")]
        public string typeName;
        public string typeKey;
        public Sprite typeIcon;
        [TextArea(3, 5)] public string description;

        [Header("보조장비 특성")]
        public AccessoryCategory category = AccessoryCategory.Ring;
        public int maxEquipCount = 1;
        public int requiredLevel = 1;
    }

    /// <summary>
    /// 아이템 타입 데이터베이스 (게임 룰/시스템 데이터)
    /// 게임에 존재하는 모든 타입 정의를 중앙 관리
    /// </summary>
    [CreateAssetMenu(fileName = "ItemTypeDatabase", menuName = "Item/Item Type Database", order = 8)]
    public class ItemTypeDatabase : ScriptableObject
    {
        [Header("무기 타입 (게임 룰)")]
        [Tooltip("게임에서 사용 가능한 모든 무기 타입")]
        public List<WeaponTypeEntry> weaponTypes = new List<WeaponTypeEntry>();
        
        [Header("방어구 타입 (게임 룰)")]
        [Tooltip("게임에서 사용 가능한 모든 방어구 타입")]
        public List<ArmorTypeEntry> armorTypes = new List<ArmorTypeEntry>();
        
        [Header("보조장비 타입 (게임 룰)")]
        [Tooltip("게임에서 사용 가능한 모든 보조장비 타입")]
        public List<AccessoryTypeEntry> accessoryTypes = new List<AccessoryTypeEntry>();
        
        #region 타입 조회 메서드
        
        /// <summary>
        /// typeKey로 무기 타입 검색
        /// </summary>
        public WeaponTypeEntry GetWeaponType(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;
            
            return weaponTypes.Find(t => t.typeKey == typeKey);
        }
        
        /// <summary>
        /// typeKey로 방어구 타입 검색
        /// </summary>
        public ArmorTypeEntry GetArmorType(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;
            
            return armorTypes.Find(t => t.typeKey == typeKey);
        }
        
        /// <summary>
        /// typeKey로 보조장비 타입 검색
        /// </summary>
        public AccessoryTypeEntry GetAccessoryType(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;
            
            return accessoryTypes.Find(t => t.typeKey == typeKey);
        }
        
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 리스트가 비어 있고 기존 ScriptableObject 타입 데이터가 존재하면 자동 마이그레이션 수행
            TryAutoMigrateTypeScriptableObjects();
        }

        private void TryAutoMigrateTypeScriptableObjects()
        {
            // 이미 데이터가 있으면 스킵
            bool hasAny = (weaponTypes != null && weaponTypes.Count > 0) ||
                          (armorTypes != null && armorTypes.Count > 0) ||
                          (accessoryTypes != null && accessoryTypes.Count > 0);
            if (hasAny)
                return;

            var dbPathGuids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ItemTypeDatabase)}");
            if (dbPathGuids == null || dbPathGuids.Length == 0)
                return;

            int wAdd = 0, aAdd = 0, acAdd = 0;

            // WeaponTypeData → WeaponTypeEntry
            var weaponGuids = UnityEditor.AssetDatabase.FindAssets("t:WeaponTypeData");
            foreach (var guid in weaponGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponTypeData>(path);
                if (so == null || string.IsNullOrEmpty(so.typeKey))
                    continue;
                if (weaponTypes.Exists(x => x.typeKey == so.typeKey))
                    continue;
                weaponTypes.Add(new WeaponTypeEntry
                {
                    typeKey = so.typeKey,
                    typeName = so.typeName,
                    typeIcon = so.typeIcon,
                    description = so.description,
                    category = so.category,
                    baseAttackSpeed = so.baseAttackSpeed,
                    baseRange = so.baseRange
                });
                wAdd++;
            }

            // ArmorTypeData → ArmorTypeEntry
            var armorGuids = UnityEditor.AssetDatabase.FindAssets("t:ArmorTypeData");
            foreach (var guid in armorGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<ArmorTypeData>(path);
                if (so == null || string.IsNullOrEmpty(so.typeKey))
                    continue;
                if (armorTypes.Exists(x => x.typeKey == so.typeKey))
                    continue;
                armorTypes.Add(new ArmorTypeEntry
                {
                    typeKey = so.typeKey,
                    typeName = so.typeName,
                    typeIcon = so.typeIcon,
                    description = so.description,
                    category = so.category,
                    baseWeight = so.baseWeight,
                    baseMobility = so.baseMobility,
                    requiredLevel = so.requiredLevel
                });
                aAdd++;
            }

            // AccessoryTypeData → AccessoryTypeEntry
            var accGuids = UnityEditor.AssetDatabase.FindAssets("t:AccessoryTypeData");
            foreach (var guid in accGuids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var so = UnityEditor.AssetDatabase.LoadAssetAtPath<AccessoryTypeData>(path);
                if (so == null || string.IsNullOrEmpty(so.typeKey))
                    continue;
                if (accessoryTypes.Exists(x => x.typeKey == so.typeKey))
                    continue;
                accessoryTypes.Add(new AccessoryTypeEntry
                {
                    typeKey = so.typeKey,
                    typeName = so.typeName,
                    typeIcon = so.typeIcon,
                    description = so.description,
                    category = so.category,
                    maxEquipCount = so.maxEquipCount,
                    requiredLevel = so.requiredLevel
                });
                acAdd++;
            }

            if (wAdd + aAdd + acAdd > 0)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"[ItemTypeDatabase] 자동 마이그레이션: Weapon +{wAdd}, Armor +{aAdd}, Accessory +{acAdd}");
            }
        }
#endif
    }
}

