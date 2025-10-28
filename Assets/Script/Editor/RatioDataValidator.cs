using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using BladeAction.Item;

namespace BladeAction.Editor
{
    /// <summary>
    /// 비율형 데이터(0~1 저장 규칙)를 검증하고 0~100으로 잘못 저장된 케이스를 탐지/변환하는 도구
    /// </summary>
    public class RatioDataValidator : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<ValidationResult> results = new List<ValidationResult>();
        private bool hasScanned = false;

        private struct ValidationResult
        {
            public Object target;
            public string targetName;
            public string fieldName;
            public float currentValue;
            public bool needsConversion;
        }

        [MenuItem("Tools/Stats/Validate Ratio Data (0~1)")]
        public static void ShowWindow()
        {
            var window = GetWindow<RatioDataValidator>("Ratio Data Validator");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("비율형 데이터 검증 (0~1 범위)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "비율형 필드(blockEfficiency, parryEfficiency, damageReduction 등)가 0~100으로 잘못 저장된 케이스를 탐지합니다.\n" +
                "변환 대상: 1.0 초과 값 → 0.01 곱셈으로 0~1 범위로 변환",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("검증 시작", GUILayout.Height(30)))
            {
                ScanAllData();
            }

            EditorGUILayout.Space();

            if (hasScanned)
            {
                EditorGUILayout.LabelField($"검증 결과: 총 {results.Count}개 필드 확인", EditorStyles.boldLabel);

                var needsConversion = results.Where(r => r.needsConversion).ToList();
                if (needsConversion.Count > 0)
                {
                    EditorGUILayout.HelpBox($"{needsConversion.Count}개 필드가 변환이 필요합니다.", MessageType.Warning);

                    if (GUILayout.Button($"변환 실행 ({needsConversion.Count}개)", GUILayout.Height(25)))
                    {
                        ConvertAll(needsConversion);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("모든 데이터가 올바른 범위(0~1)에 있습니다.", MessageType.Info);
                }

                EditorGUILayout.Space();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                foreach (var result in results)
                {
                    if (result.needsConversion)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(result.target, typeof(Object), false, GUILayout.Width(150));
                        EditorGUILayout.LabelField($"{result.fieldName}: {result.currentValue:F2} → {result.currentValue * 0.01f:F4}",
                            EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ScanAllData()
        {
            results.Clear();
            hasScanned = true;

            // 1. StatDatabase 검증
            ScanStatDatabase();

            // 2. CharacterData 검증
            ScanCharacterData();

            Debug.Log($"[RatioDataValidator] 검증 완료: {results.Count}개 필드 확인, " +
                      $"{results.Count(r => r.needsConversion)}개 변환 필요");
        }

        private void ScanStatDatabase()
        {
            var statDBs = Resources.FindObjectsOfTypeAll<StatDatabase>();
            foreach (var db in statDBs)
            {
                if (db.statTables == null) continue;

                foreach (var statTable in db.statTables)
                {
                    if (statTable == null) continue;

                    var stats = statTable.stats;
                    CheckRatioField(db, $"StatDB[{statTable.tableKey}].blockEfficiency", stats.blockEfficiency);
                    CheckRatioField(db, $"StatDB[{statTable.tableKey}].parryEfficiency", stats.parryEfficiency);
                    CheckRatioField(db, $"StatDB[{statTable.tableKey}].damageReduction", stats.damageReduction);
                }
            }
        }

        private void ScanCharacterData()
        {
            var characterDatas = Resources.FindObjectsOfTypeAll<CharacterData>();
            foreach (var cd in characterDatas)
            {
#pragma warning disable CS0618 // Obsolete 경고 억제 (검증 도구이므로 의도적 사용)
                CheckRatioField(cd, $"CharacterData[{cd.characterName}].guardDamageReduction", cd.guardDamageReduction);
                // 크리티컬 확률도 0~1 범위
                CheckRatioField(cd, $"CharacterData[{cd.characterName}].baseCritChance", cd.baseCritChance);
#pragma warning restore CS0618
            }
        }

        private void CheckRatioField(Object target, string fieldName, float value)
        {
            results.Add(new ValidationResult
            {
                target = target,
                targetName = target.name,
                fieldName = fieldName,
                currentValue = value,
                needsConversion = value > 1.0f // 1.0 초과면 변환 필요
            });
        }

        private void ConvertAll(List<ValidationResult> targets)
        {
            if (!EditorUtility.DisplayDialog("변환 확인",
                $"{targets.Count}개 필드를 0~1 범위로 변환합니다.\n" +
                "이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?",
                "변환 실행", "취소"))
            {
                return;
            }

            int converted = 0;

            foreach (var result in targets)
            {
                if (result.target is StatDatabase statDB)
                {
                    ConvertStatDatabase(statDB, result.fieldName);
                    converted++;
                }
                else if (result.target is CharacterData cd)
                {
                    ConvertCharacterData(cd, result.fieldName);
                    converted++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RatioDataValidator] 변환 완료: {converted}개 필드");
            EditorUtility.DisplayDialog("변환 완료", $"{converted}개 필드가 성공적으로 변환되었습니다.", "확인");

            // 재검증
            ScanAllData();
        }

        private void ConvertStatDatabase(StatDatabase db, string fieldName)
        {
            if (db.statTables == null) return;

            Undo.RecordObject(db, "Convert Ratio Data");

            foreach (var statTable in db.statTables)
            {
                if (statTable == null) continue;

                var stats = statTable.stats;

                if (fieldName.Contains("blockEfficiency") && stats.blockEfficiency > 1.0f)
                    stats.blockEfficiency *= 0.01f;

                if (fieldName.Contains("parryEfficiency") && stats.parryEfficiency > 1.0f)
                    stats.parryEfficiency *= 0.01f;

                if (fieldName.Contains("damageReduction") && stats.damageReduction > 1.0f)
                    stats.damageReduction *= 0.01f;
            }

            EditorUtility.SetDirty(db);
        }

        private void ConvertCharacterData(CharacterData cd, string fieldName)
        {
            Undo.RecordObject(cd, "Convert Ratio Data");

#pragma warning disable CS0618 // Obsolete 경고 억제 (변환 도구이므로 의도적 사용)
            if (fieldName.Contains("guardDamageReduction") && cd.guardDamageReduction > 1.0f)
            {
                cd.guardDamageReduction *= 0.01f;
            }

            if (fieldName.Contains("baseCritChance") && cd.baseCritChance > 1.0f)
            {
                cd.baseCritChance *= 0.01f;
            }
#pragma warning restore CS0618

            EditorUtility.SetDirty(cd);
        }
    }
}

