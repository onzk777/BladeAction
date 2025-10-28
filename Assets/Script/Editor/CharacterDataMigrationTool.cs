using UnityEngine;
using UnityEditor;
using BladeAction.Combat;

namespace BladeAction.Editor
{
    /// <summary>
    /// CharacterData의 구형 필드를 baseStats(CombatStats)로 마이그레이션하는 도구
    /// </summary>
    public class CharacterDataMigrationTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool hasScanned = false;
        private System.Collections.Generic.List<CharacterData> targets = new System.Collections.Generic.List<CharacterData>();

        [MenuItem("Tools/Character/Migrate CharacterData to CombatStats")]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterDataMigrationTool>("CharacterData Migration");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("CharacterData 마이그레이션", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "구형 필드(baseMaxHP, baseATK 등)를 새로운 baseStats(CombatStats)로 이전합니다.\n" +
                "이 작업은 되돌릴 수 없으므로 백업을 권장합니다.",
                MessageType.Warning);

            EditorGUILayout.Space();

            if (GUILayout.Button("스캔 시작", GUILayout.Height(30)))
            {
                ScanAllCharacterData();
            }

            EditorGUILayout.Space();

            if (hasScanned)
            {
                EditorGUILayout.LabelField($"발견된 CharacterData: {targets.Count}개", EditorStyles.boldLabel);

                if (targets.Count > 0)
                {
                    if (GUILayout.Button($"마이그레이션 실행 ({targets.Count}개)", GUILayout.Height(25)))
                    {
                        MigrateAll();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("마이그레이션할 CharacterData가 없습니다.", MessageType.Info);
                }

                EditorGUILayout.Space();

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                foreach (var data in targets)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(data, typeof(CharacterData), false, GUILayout.Width(200));
#pragma warning disable CS0618
                    EditorGUILayout.LabelField($"HP:{data.baseMaxHP} ATK:{data.baseATK} DR:{data.baseDR}",
                        EditorStyles.wordWrappedLabel);
#pragma warning restore CS0618
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void ScanAllCharacterData()
        {
            targets.Clear();
            hasScanned = true;

            var allCharacterData = Resources.FindObjectsOfTypeAll<CharacterData>();
#pragma warning disable CS0618
            foreach (var data in allCharacterData)
            {
                // 기본값이 설정되어 있으면 마이그레이션 대상
                if (data.baseMaxHP != 0 || data.baseATK != 0 || data.baseDR != 0)
                {
                    targets.Add(data);
                }
            }
#pragma warning restore CS0618

            Debug.Log($"[CharacterDataMigration] 스캔 완료: {targets.Count}개 발견");
        }

        private void MigrateAll()
        {
            if (!EditorUtility.DisplayDialog("마이그레이션 확인",
                $"{targets.Count}개 CharacterData를 baseStats로 이전합니다.\n" +
                "이 작업은 되돌릴 수 없습니다. 계속하시겠습니까?",
                "실행", "취소"))
            {
                return;
            }

            int migrated = 0;

            foreach (var data in targets)
            {
                Undo.RecordObject(data, "Migrate to CombatStats");

#pragma warning disable CS0618 // Obsolete 경고 억제 (마이그레이션 도구이므로 의도적 사용)
                // 구형 필드 → baseStats로 복사
                data.baseStats.maxHP = data.baseMaxHP;
                data.baseStats.attack = data.baseATK;
                data.baseStats.defenseDR = data.baseDR;
                data.baseStats.critChance = data.baseCritChance;
                data.baseStats.critMultiplier = data.baseCritMultiplier;
                data.baseStats.maxPoise = data.baseMaxPoise;
                data.baseStats.parryPoiseDamage = data.baseParryPoiseDamage;
                data.baseStats.guardDamageReduction = data.guardDamageReduction;
                data.baseStats.guardDRBonus = data.guardDRBonus;

                // 구형 필드는 0으로 리셋 (deprecated이므로 사용 방지)
                data.baseMaxHP = 0;
                data.baseATK = 0;
                data.baseDR = 0;
                data.baseCritChance = 0f;
                data.baseCritMultiplier = 1f;
                data.baseMaxPoise = 0;
                data.baseParryPoiseDamage = 0;
                data.guardDamageReduction = 0f;
                data.guardDRBonus = 0;

                EditorUtility.SetDirty(data);
                migrated++;

                Debug.Log($"[Migration] {data.characterName} - " +
                          $"HP:{data.baseStats.maxHP} ATK:{data.baseStats.attack} DR:{data.baseStats.defenseDR}");
#pragma warning restore CS0618
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CharacterDataMigration] 마이그레이션 완료: {migrated}개");
            EditorUtility.DisplayDialog("완료", $"{migrated}개 CharacterData가 성공적으로 마이그레이션되었습니다.", "확인");

            // 재스캔
            ScanAllCharacterData();
        }
    }
}

