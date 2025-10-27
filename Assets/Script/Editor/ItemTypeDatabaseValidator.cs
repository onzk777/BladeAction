#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using BladeAction.Item;

public static class ItemTypeDatabaseValidator
{
    [MenuItem("Tools/Database/Validate ItemTypeDatabase Keys (Report)")]
    public static void ValidateKeys()
    {
        var db = FindItemTypeDatabase();
        if (db == null)
        {
            Debug.LogError("ItemTypeDatabase를 찾을 수 없습니다.");
            return;
        }

        ValidateList("Weapon", db.weaponTypes.Select(x => x?.typeKey));
        ValidateList("Armor", db.armorTypes.Select(x => x?.typeKey));
        ValidateList("Accessory", db.accessoryTypes.Select(x => x?.typeKey));
        Debug.Log("[ItemTypeDatabaseValidator] 검증 완료");
    }

    [MenuItem("Tools/Database/Fix Duplicate Keys (Append _N)")]
    public static void FixDuplicateKeys()
    {
        var db = FindItemTypeDatabase();
        if (db == null)
        {
            Debug.LogError("ItemTypeDatabase를 찾을 수 없습니다.");
            return;
        }

        Undo.RecordObject(db, "Fix Duplicate Keys in ItemTypeDatabase");

        FixList(db.weaponTypes);
        FixList(db.armorTypes);
        FixList(db.accessoryTypes);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("[ItemTypeDatabaseValidator] 중복 키 수정 완료 (필요 시 _N 접미사 추가)");
    }

    private static void ValidateList(string label, IEnumerable<string> keys)
    {
        var list = keys.Where(k => !string.IsNullOrEmpty(k)).ToList();
        var dup = list.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        var empty = keys.Count(k => string.IsNullOrEmpty(k));
        if (dup.Count == 0 && empty == 0)
        {
            Debug.Log($"[{label}] OK - {list.Count} keys");
        }
        else
        {
            if (dup.Count > 0)
                Debug.LogWarning($"[{label}] 중복 키: {string.Join(", ", dup)}");
            if (empty > 0)
                Debug.LogWarning($"[{label}] 빈 키 항목: {empty}개");
        }
    }

    private static void FixList<T>(List<T> entries) where T : class
    {
        if (entries == null) return;
        var seen = new HashSet<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null) continue;

            var keyField = typeof(T).GetField("typeKey");
            var nameField = typeof(T).GetField("typeName");
            if (keyField == null) continue;
            var key = keyField.GetValue(entry) as string;
            if (string.IsNullOrEmpty(key))
            {
                // 빈 키는 이름 기반으로 생성
                string baseKey = (nameField?.GetValue(entry) as string) ?? "type";
                key = SanitizeKey(baseKey);
            }

            string unique = key;
            int suffix = 1;
            while (seen.Contains(unique))
            {
                unique = key + "_" + suffix;
                suffix++;
            }
            seen.Add(unique);
            keyField.SetValue(entry, unique);
        }
    }

    private static string SanitizeKey(string s)
    {
        if (string.IsNullOrEmpty(s)) return "type";
        var chars = s.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        var result = new string(chars).Trim('_');
        return string.IsNullOrEmpty(result) ? "type" : result;
    }

    private static ItemTypeDatabase FindItemTypeDatabase()
    {
        var guids = AssetDatabase.FindAssets("t:ItemTypeDatabase");
        if (guids == null || guids.Length == 0) return null;
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<ItemTypeDatabase>(path);
    }
}
#endif


