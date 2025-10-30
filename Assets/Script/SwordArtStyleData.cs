using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Spine.Unity;

/// <summary>
/// 유파의 검술 항목 (드롭다운 선택용)
/// </summary>
[System.Serializable]
public class ActionCommandKeyEntry
{
    [Tooltip("검술 키 (ActionCommandDatabase에서 선택)")]
    [DatabaseKey(typeof(ActionCommandDatabase), "actions", "key")]
    public string actionKey;
}

[CreateAssetMenu(fileName = "NewSwordArtStyle", menuName = "Combat/SwordArtStyle")]
public class SwordArtStyleData : ScriptableObject
{
    [Header("검술 스타일 기본")]
    [Tooltip("에디터에 표시될 스타일 이름")]
    public string styleName = "New Style";
    
    [Tooltip("유파에 대한 간략한 설명")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("Spine 애니메이션")]
    [Tooltip("이 유파에서 사용할 Spine 애니메이션 애셋")]
    [SerializeField] private SkeletonDataAsset spineAnimationAsset;
    public SkeletonDataAsset SpineAnimationAsset => spineAnimationAsset;

    [Header("사용 가능한 검술 목록")]
    [Tooltip("이 유파에서 사용할 검술 리스트 (ActionCommandDatabase에서 드롭다운 선택)")]
    public List<ActionCommandKeyEntry> actionCommandKeys = new List<ActionCommandKeyEntry>();
    
    // 마이그레이션용: 이전 commandSet 필드 (ActionCommandData 직접 참조)
    [SerializeField]
    [HideInInspector]
    [FormerlySerializedAs("commandSet")]
    private List<ActionCommandData> legacyCommandSet = new List<ActionCommandData>();
    
    /// <summary>
    /// 런타임에 ActionCommandDatabase에서 검술 데이터 조회
    /// </summary>
    public List<ActionCommandData> GetActionCommands()
    {
        if (actionCommandKeys == null || actionCommandKeys.Count == 0)
            return new List<ActionCommandData>();
        
        var database = ActionCommandDatabase.Instance;
        if (database == null)
        {
            Debug.LogWarning($"[SwordArtStyleData] ActionCommandDatabase를 찾을 수 없습니다. (유파: {styleName})");
            return new List<ActionCommandData>();
        }
        
        // ActionCommandKeyEntry에서 actionKey 추출하여 조회
        var keys = actionCommandKeys
            .Where(entry => entry != null && !string.IsNullOrEmpty(entry.actionKey))
            .Select(entry => entry.actionKey)
            .ToList();
        
        return database.GetActions(keys);
    }
    
    /// <summary>
    /// 하위 호환용: CommandSet 프로퍼티 (GetActionCommands() 호출)
    /// </summary>
    public List<ActionCommandData> CommandSet
    {
        get => GetActionCommands();
        set
        {
            // 하위 호환: ActionCommandData 리스트를 받아서 Key로 변환
            // Key는 ActionCommandDatabase의 Entry.key 사용
            Debug.LogWarning("[SwordArtStyleData] CommandSet setter는 하위 호환용입니다. actionCommandKeys를 직접 설정하세요.");
        }
    }
    
    /// <summary>
    /// 하위 호환용: ActionCommands 프로퍼티 (읽기 전용)
    /// </summary>
    public IReadOnlyList<ActionCommandData> ActionCommands => GetActionCommands().AsReadOnly();

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 컨텍스트 메뉴: 마이그레이션 수동 실행
    /// </summary>
    [ContextMenu("Migrate from Legacy CommandSet")]
    private void MigrateLegacyData()
    {
        if (legacyCommandSet == null || legacyCommandSet.Count == 0)
        {
            Debug.Log($"[SwordArtStyleData] {styleName} - 마이그레이션할 레거시 데이터가 없습니다.");
            return;
        }
        
        actionCommandKeys = new List<ActionCommandKeyEntry>();
        var database = ActionCommandDatabase.Instance;
        
        if (database != null && database.actions != null)
        {
            foreach (var legacyAction in legacyCommandSet)
            {
                if (legacyAction != null)
                {
                    var entry = database.actions.Find(e => e.data == legacyAction);
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                    {
                        actionCommandKeys.Add(new ActionCommandKeyEntry { actionKey = entry.key });
                        Debug.Log($"[SwordArtStyleData] 마이그레이션: {styleName} - {legacyAction.name} → {entry.key}");
                    }
                }
            }
        }
        
        if (actionCommandKeys.Count > 0)
        {
            legacyCommandSet.Clear();
            Debug.Log($"[SwordArtStyleData] {styleName} 마이그레이션 완료: {actionCommandKeys.Count}개 검술");
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

}

