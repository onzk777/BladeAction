#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using BladeAction.Item;

/// <summary>
/// 기존 WeaponTypeData / ArmorTypeData / AccessoryTypeData ScriptableObject를
/// ItemTypeDatabase 내부 인라인 엔트리로 마이그레이션하는 에디터 유틸리티
/// </summary>
public static class ItemTypeMigrationTool
{
    [MenuItem("Tools/Database/Migrate Type SO -> ItemTypeDatabase Entries")]
    public static void MigrateTypeScriptableObjectsToEntries()
    {
        var db = FindItemTypeDatabase();
        if (db == null)
        {
            Debug.LogError("ItemTypeDatabase를 찾을 수 없습니다. 먼저 Create > Item > Item Type Database로 생성하세요.");
            return;
        }

        Undo.RecordObject(db, "Migrate Type ScriptableObjects to Entries");

        int weaponAdded = 0, weaponUpdated = 0;
        int armorAdded = 0, armorUpdated = 0;
        int accAdded = 0, accUpdated = 0;

        // WeaponTypeData → WeaponTypeEntry
        var weaponGuids = AssetDatabase.FindAssets("t:WeaponTypeData");
        foreach (var guid in weaponGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<WeaponTypeData>(path);
            if (so == null || string.IsNullOrEmpty(so.typeKey))
                continue;

            var existing = db.weaponTypes.FirstOrDefault(x => x.typeKey == so.typeKey);
            if (existing == null)
            {
                db.weaponTypes.Add(new WeaponTypeEntry
                {
                    typeKey = so.typeKey,
                    typeName = so.typeName,
                    typeIcon = so.typeIcon,
                    description = so.description,
                    category = so.category,
                    baseAttackSpeed = so.baseAttackSpeed,
                    baseRange = so.baseRange
                });
                weaponAdded++;
            }
            else
            {
                existing.typeName = so.typeName;
                existing.typeIcon = so.typeIcon;
                existing.description = so.description;
                existing.category = so.category;
                existing.baseAttackSpeed = so.baseAttackSpeed;
                existing.baseRange = so.baseRange;
                weaponUpdated++;
            }
        }

        // ArmorTypeData → ArmorTypeEntry
        var armorGuids = AssetDatabase.FindAssets("t:ArmorTypeData");
        foreach (var guid in armorGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ArmorTypeData>(path);
            if (so == null || string.IsNullOrEmpty(so.typeKey))
                continue;

            var existing = db.armorTypes.FirstOrDefault(x => x.typeKey == so.typeKey);
            if (existing == null)
            {
                db.armorTypes.Add(new ArmorTypeEntry
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
                armorAdded++;
            }
            else
            {
                existing.typeName = so.typeName;
                existing.typeIcon = so.typeIcon;
                existing.description = so.description;
                existing.category = so.category;
                existing.baseWeight = so.baseWeight;
                existing.baseMobility = so.baseMobility;
                existing.requiredLevel = so.requiredLevel;
                armorUpdated++;
            }
        }

        // AccessoryTypeData → AccessoryTypeEntry
        var accGuids = AssetDatabase.FindAssets("t:AccessoryTypeData");
        foreach (var guid in accGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<AccessoryTypeData>(path);
            if (so == null || string.IsNullOrEmpty(so.typeKey))
                continue;

            var existing = db.accessoryTypes.FirstOrDefault(x => x.typeKey == so.typeKey);
            if (existing == null)
            {
                db.accessoryTypes.Add(new AccessoryTypeEntry
                {
                    typeKey = so.typeKey,
                    typeName = so.typeName,
                    typeIcon = so.typeIcon,
                    description = so.description,
                    category = so.category,
                    maxEquipCount = so.maxEquipCount,
                    requiredLevel = so.requiredLevel
                });
                accAdded++;
            }
            else
            {
                existing.typeName = so.typeName;
                existing.typeIcon = so.typeIcon;
                existing.description = so.description;
                existing.category = so.category;
                existing.maxEquipCount = so.maxEquipCount;
                existing.requiredLevel = so.requiredLevel;
                accUpdated++;
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ItemTypeMigrationTool] 완료\n" +
                  $"- Weapon: +{weaponAdded}, updated {weaponUpdated}\n" +
                  $"- Armor: +{armorAdded}, updated {armorUpdated}\n" +
                  $"- Accessory: +{accAdded}, updated {accUpdated}");
    }

    private static ItemTypeDatabase FindItemTypeDatabase()
    {
        var guids = AssetDatabase.FindAssets("t:ItemTypeDatabase");
        if (guids == null || guids.Length == 0)
            return null;
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<ItemTypeDatabase>(path);
    }
}
#endif


