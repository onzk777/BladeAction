using UnityEditor;
using UnityEngine;
using BladeAction.Combat;
using System.Collections.Generic;

namespace BladeAction.EditorTools
{
    [CustomPropertyDrawer(typeof(BladeAction.StatLimitAttribute))]
    public class StatLimitDrawer : PropertyDrawer
    {
        private const double UPDATE_INTERVAL = 5.0; // 5초 TTL
        private static StatLimitRules cachedRules;
        private static double lastRulesUpdateTime = -1;
        private static readonly Dictionary<string, CachedRange> rangeCache = new Dictionary<string, CachedRange>();

        private struct CachedRange
        {
            public float min;
            public float max;
            public bool has;
            public double t;
        }

        [MenuItem("Tools/Stats/Clear StatLimit Cache")]
        private static void ClearCache()
        {
            cachedRules = null;
            lastRulesUpdateTime = -1;
            rangeCache.Clear();
            Debug.Log("✅ StatLimitDrawer 캐시 초기화 완료");
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (BladeAction.StatLimitAttribute)attribute;

            float min = float.NegativeInfinity;
            float max = float.PositiveInfinity;
            bool hasRule = TryGetRange(attr.statKey, out min, out max);

            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Float)
            {
                float value = property.floatValue;
                if (attr.showAsSlider && hasRule && !float.IsInfinity(min) && !float.IsInfinity(max))
                {
                    // 왼쪽: 슬라이더, 오른쪽: 퍼센트 미리보기(비율형일 때)
                    if (IsRatioKey(attr.statKey))
                    {
                        var sliderRect = new Rect(position.x, position.y, position.width - 60f, position.height);
                        var percentRect = new Rect(position.x + position.width - 58f, position.y, 58f, position.height);
                        value = EditorGUI.Slider(sliderRect, label, value, min, max);
                        EditorGUI.LabelField(percentRect, $"{value * 100f:F1}%");
                    }
                    else
                    {
                        value = EditorGUI.Slider(position, label, value, min, max);
                    }
                }
                else
                {
                    if (IsRatioKey(attr.statKey))
                    {
                        var fieldRect = new Rect(position.x, position.y, position.width - 60f, position.height);
                        var percentRect = new Rect(position.x + position.width - 58f, position.y, 58f, position.height);
                        value = EditorGUI.FloatField(fieldRect, label, value);
                        EditorGUI.LabelField(percentRect, $"{value * 100f:F1}%");
                    }
                    else
                    {
                        value = EditorGUI.FloatField(position, label, value);
                    }
                    if (hasRule)
                        value = Mathf.Clamp(value, min, max);
                }
                property.floatValue = value;
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                int value = property.intValue;
                if (attr.showAsSlider && hasRule && !float.IsInfinity(min) && !float.IsInfinity(max))
                {
                    value = EditorGUI.IntSlider(position, label, value, Mathf.RoundToInt(min), Mathf.RoundToInt(max));
                }
                else
                {
                    value = EditorGUI.IntField(position, label, value);
                    if (hasRule)
                        value = Mathf.Clamp(value, Mathf.RoundToInt(min), Mathf.RoundToInt(max));
                }
                property.intValue = value;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }

            EditorGUI.EndProperty();

            if (!hasRule)
            {
                var helpRect = new Rect(position.x, position.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.HelpBox(helpRect, $"StatLimitRules: '{attr.statKey}' 규칙이 없습니다.", MessageType.Info);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (BladeAction.StatLimitAttribute)attribute;
            bool hasRule = TryGetRange(attr.statKey, out _, out _);
            float h = EditorGUI.GetPropertyHeight(property, label, true);
            if (!hasRule) h += EditorGUIUtility.singleLineHeight + 2f;
            return h;
        }

        private bool TryGetRange(string statKey, out float min, out float max)
        {
            // 1) per-key 캐시 우선
            CachedRange cr;
            if (rangeCache.TryGetValue(statKey, out cr))
            {
                if (EditorApplication.timeSinceStartup - cr.t <= UPDATE_INTERVAL)
                {
                    min = cr.min; max = cr.max; return cr.has;
                }
            }

            // 2) 룰 확보 (TTL)
            var rules = EnsureRules();
            if (rules == null) { min = float.NegativeInfinity; max = float.PositiveInfinity; return false; }

            // 3) 룰 조회 및 캐시 갱신
            bool has = rules.TryGetRange(statKey, out min, out max);
            rangeCache[statKey] = new CachedRange { min = min, max = max, has = has, t = EditorApplication.timeSinceStartup };
            return has;
        }

        private StatLimitRules EnsureRules()
        {
            if (cachedRules != null && (EditorApplication.timeSinceStartup - lastRulesUpdateTime) <= UPDATE_INTERVAL)
                return cachedRules;

            StatLimitRules rules = null;

            // 우선 씬 내 StatsCalculationManager에서 참조
            if (StatsCalculationManager.Instance != null)
            {
                rules = StatsCalculationManager.Instance.statLimitRules;
            }

            // 없으면 Resources에서 로드 시도
            if (rules == null)
            {
                rules = Resources.Load<StatLimitRules>("Data/Stat/StatLimitRules");
            }

            // 에디터에서는 AssetDatabase 검색도 시도(최후)
            if (rules == null)
            {
                var guids = AssetDatabase.FindAssets("t:StatLimitRules");
                if (guids != null && guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    rules = AssetDatabase.LoadAssetAtPath<StatLimitRules>(path);
                }
            }

            cachedRules = rules;
            lastRulesUpdateTime = EditorApplication.timeSinceStartup;
            return cachedRules;
        }

        private bool IsRatioKey(string statKey)
        {
            // 0~1로 관리하는 비율형 키 표시
            return statKey == "critChance"
                || statKey == "guardDamageReduction"
                || statKey == "damageReduction"
                || statKey == "blockEfficiency"
                || statKey == "parryEfficiency";
        }
    }
}


