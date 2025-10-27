using UnityEditor;
using UnityEngine;
using BladeAction.Combat;

namespace BladeAction.EditorTools
{
	[CustomEditor(typeof(StatLimitRules))]
	public class StatLimitRulesEditor : UnityEditor.Editor
	{
		private SerializedProperty attack;
		private SerializedProperty defenseDR;
		private SerializedProperty maxHP;
		private SerializedProperty maxPoise;
		private SerializedProperty parryPoiseDamage;
		private SerializedProperty blockPoiseConsumption;
		private SerializedProperty parryPoiseConsumption;
		private SerializedProperty parryPoiseAttackPower;
		private SerializedProperty poiseGain;

		private SerializedProperty critChance;
		private SerializedProperty guardDamageReduction;
		private SerializedProperty damageReduction;
		private SerializedProperty blockEfficiency;
		private SerializedProperty parryEfficiency;

		private SerializedProperty critMultiplier;

		private void OnEnable()
		{
			attack = serializedObject.FindProperty("attack");
			defenseDR = serializedObject.FindProperty("defenseDR");
			maxHP = serializedObject.FindProperty("maxHP");
			maxPoise = serializedObject.FindProperty("maxPoise");
			parryPoiseDamage = serializedObject.FindProperty("parryPoiseDamage");
			blockPoiseConsumption = serializedObject.FindProperty("blockPoiseConsumption");
			parryPoiseConsumption = serializedObject.FindProperty("parryPoiseConsumption");
			parryPoiseAttackPower = serializedObject.FindProperty("parryPoiseAttackPower");
			poiseGain = serializedObject.FindProperty("poiseGain");

			critChance = serializedObject.FindProperty("critChance");
			guardDamageReduction = serializedObject.FindProperty("guardDamageReduction");
			damageReduction = serializedObject.FindProperty("damageReduction");
			blockEfficiency = serializedObject.FindProperty("blockEfficiency");
			parryEfficiency = serializedObject.FindProperty("parryEfficiency");

			critMultiplier = serializedObject.FindProperty("critMultiplier");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.LabelField("Stat Limit Rules (CombatStats 1:1)", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			EditorGUILayout.LabelField("- 비율형(0~1)은 우측에 % 미리보기가 표시됩니다.", EditorStyles.miniLabel);
			EditorGUILayout.Space(6);

			EditorGUILayout.LabelField("기본/가산형", EditorStyles.boldLabel);
			DrawMinMax("Attack", attack);
			DrawMinMax("Defense DR", defenseDR);
			DrawMinMax("Max HP", maxHP);
			DrawMinMax("Max Poise", maxPoise);
			DrawMinMax("Parry Poise Damage", parryPoiseDamage);
			DrawMinMax("Block Poise Consumption", blockPoiseConsumption);
			DrawMinMax("Parry Poise Consumption", parryPoiseConsumption);
			DrawMinMax("Parry Poise Attack Power", parryPoiseAttackPower);
			DrawMinMax("Poise Gain", poiseGain);

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("비율형 (0~1)", EditorStyles.boldLabel);
			DrawRatioMinMax("Crit Chance", critChance);
			DrawRatioMinMax("Guard Damage Reduction", guardDamageReduction);
			DrawRatioMinMax("Damage Reduction", damageReduction);
			DrawRatioMinMax("Block Efficiency", blockEfficiency);
			DrawRatioMinMax("Parry Efficiency", parryEfficiency);

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("배율(multiplier)", EditorStyles.boldLabel);
			DrawMultiplierMinMax("Crit Multiplier", critMultiplier);

			serializedObject.ApplyModifiedProperties();
		}

		private static void DrawMinMax(string label, SerializedProperty minMaxProp)
		{
			if (minMaxProp == null) return;
			var minProp = minMaxProp.FindPropertyRelative("min");
			var maxProp = minMaxProp.FindPropertyRelative("max");

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			EditorGUILayout.LabelField("Min", GUILayout.Width(30));
			minProp.floatValue = EditorGUILayout.FloatField(minProp.floatValue, GUILayout.MinWidth(60));
			EditorGUILayout.LabelField("Max", GUILayout.Width(34));
			maxProp.floatValue = EditorGUILayout.FloatField(maxProp.floatValue, GUILayout.MinWidth(60));
			EditorGUILayout.EndHorizontal();
		}

		private static void DrawRatioMinMax(string label, SerializedProperty minMaxProp)
		{
			if (minMaxProp == null) return;
			var minProp = minMaxProp.FindPropertyRelative("min");
			var maxProp = minMaxProp.FindPropertyRelative("max");

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			EditorGUILayout.LabelField("Min", GUILayout.Width(30));
			float min = EditorGUILayout.FloatField(minProp.floatValue, GUILayout.MinWidth(60));
			EditorGUILayout.LabelField($"{min * 100f:F1}%", GUILayout.Width(60));
			EditorGUILayout.LabelField("Max", GUILayout.Width(34));
			float max = EditorGUILayout.FloatField(maxProp.floatValue, GUILayout.MinWidth(60));
			EditorGUILayout.LabelField($"{max * 100f:F1}%", GUILayout.Width(60));
			minProp.floatValue = min;
			maxProp.floatValue = max;
			EditorGUILayout.EndHorizontal();
		}

		private static void DrawMultiplierMinMax(string label, SerializedProperty minMaxProp)
		{
			if (minMaxProp == null) return;
			var minProp = minMaxProp.FindPropertyRelative("min");
			var maxProp = minMaxProp.FindPropertyRelative("max");

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(label);
			EditorGUILayout.LabelField("Min", GUILayout.Width(30));
			float min = EditorGUILayout.FloatField(minProp.floatValue, GUILayout.MinWidth(60));
			EditorGUILayout.LabelField($"x{min:F2}", GUILayout.Width(60));
			EditorGUILayout.LabelField("Max", GUILayout.Width(34));
			float max = EditorGUILayout.FloatField(maxProp.floatValue, GUILayout.MinWidth(60));
			EditorGUILayout.LabelField($"x{max:F2}", GUILayout.Width(60));
			minProp.floatValue = min;
			maxProp.floatValue = max;
			EditorGUILayout.EndHorizontal();
		}
	}
}


