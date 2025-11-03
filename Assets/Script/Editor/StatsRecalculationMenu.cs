using UnityEngine;
using UnityEditor;
using BladeAction.Combat;

namespace BladeAction.Editor
{
    /// <summary>
    /// 전체 Combatant의 스탯을 재계산하고 검증하는 에디터 메뉴
    /// </summary>
    public class StatsRecalculationMenu
    {
        [MenuItem("Tools/Stats/Recalculate All Combatants (Scene)")]
        public static void RecalculateAllCombatantsInScene()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("경고", "이 메뉴는 플레이 모드에서만 사용할 수 있습니다.", "확인");
                return;
            }

            var manager = StatsCalculationManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("오류", "StatsCalculationManager가 Scene에 없습니다.", "확인");
                return;
            }

            // CharacterManager에서 Character 가져오기
            if (CombatCharacterManager.Instance == null)
            {
                EditorUtility.DisplayDialog("오류", "CharacterManager가 Scene에 없습니다.", "확인");
                return;
            }

            int recalculated = 0;
            System.Text.StringBuilder log = new System.Text.StringBuilder();
            log.AppendLine("[스탯 재계산 결과]");
            log.AppendLine("=====================================");

            // 플레이어
            if (CombatCharacterManager.Instance.PlayerCharacter != null)
            {
                var player = CombatCharacterManager.Instance.PlayerCharacter;
                if (player.Inventory != null)
                {
                    var stats = manager.RecalculateAndCommit(player);
                    log.AppendLine($"[플레이어] {player.Name}");
                    log.AppendLine($"  ATK: {stats.attack:F0}, MaxHP: {stats.maxHP:F0}, MaxPoise: {stats.maxPoise:F0}");
                    log.AppendLine($"  CritChance: {stats.critChance * 100f:F1}%, CritMultiplier: {stats.critMultiplier:F2}x");
                    log.AppendLine($"  DamageReduction: {stats.damageReduction * 100f:F0}%");
                    recalculated++;
                }
                else
                {
                    log.AppendLine($"[플레이어] {player.Name} - 인벤토리 없음 (스킵)");
                }
            }

            // 적
            if (CombatCharacterManager.Instance.CurrentEnemy != null)
            {
                var enemy = CombatCharacterManager.Instance.CurrentEnemy;
                if (enemy.Inventory != null)
                {
                    var stats = manager.RecalculateAndCommit(enemy);
                    log.AppendLine($"[적] {enemy.Name}");
                    log.AppendLine($"  ATK: {stats.attack:F0}, MaxHP: {stats.maxHP:F0}, MaxPoise: {stats.maxPoise:F0}");
                    log.AppendLine($"  CritChance: {stats.critChance * 100f:F1}%, CritMultiplier: {stats.critMultiplier:F2}x");
                    log.AppendLine($"  DamageReduction: {stats.damageReduction * 100f:F0}%");
                    recalculated++;
                }
                else
                {
                    log.AppendLine($"[적] {enemy.Name} - 인벤토리 없음 (스킵)");
                }
            }

            log.AppendLine("=====================================");
            log.AppendLine($"재계산 완료: {recalculated}명");

            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("완료", $"{recalculated}명의 Character 스탯이 재계산되었습니다.\n자세한 내용은 Console 로그를 확인하세요.", "확인");
        }

        [MenuItem("Tools/Stats/Force Update Combat UI")]
        public static void ForceUpdateCombatUI()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("경고", "이 메뉴는 플레이 모드에서만 사용할 수 있습니다.", "확인");
                return;
            }

            var display = CombatStatusDisplay.Instance;
            if (display != null)
            {
                display.ForceUpdateUI();
                EditorUtility.DisplayDialog("완료", "Combat UI가 강제로 업데이트되었습니다.", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("오류", "CombatStatusDisplay가 Scene에 없습니다.", "확인");
            }
        }
    }
}

