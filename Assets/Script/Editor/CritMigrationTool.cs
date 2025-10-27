using UnityEditor;
using UnityEngine;

public static class CritMigrationTool
{
    [MenuItem("Tools/Migration/Convert Crit %% to Ratio (CharacterData)")]
    public static void ConvertCritPercentToRatio()
    {
        string[] guids = AssetDatabase.FindAssets("t:CharacterData");
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj == null) continue;

            var so = new SerializedObject(obj);
            var baseCritProp = so.FindProperty("baseCrit");                 // int (%) - deprecated
            var baseCritRatioProp = so.FindProperty("baseCritRatio");       // int (%) - deprecated
            var baseCritChanceProp = so.FindProperty("baseCritChance");     // float (0~1)
            var baseCritMultiplierProp = so.FindProperty("baseCritMultiplier"); // float (e.g., 1.5)

            if (baseCritChanceProp == null || baseCritMultiplierProp == null)
                continue; // 신규 필드가 아직 없으면 스킵

            bool dirty = false;

            if (baseCritProp != null)
            {
                float chance = Mathf.Clamp01(baseCritProp.intValue / 100f);
                baseCritChanceProp.floatValue = chance;
                dirty = true;
            }

            if (baseCritRatioProp != null)
            {
                float mult = Mathf.Max(0f, baseCritRatioProp.intValue / 100f);
                baseCritMultiplierProp.floatValue = mult <= 0f ? 1f : mult;
                dirty = true;
            }

            if (dirty)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(obj);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[CritMigrationTool] 변환 완료: {converted}개 CharacterData 업데이트");
    }
}


