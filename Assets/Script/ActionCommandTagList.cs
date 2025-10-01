using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 검술 태그 중앙 관리 ScriptableObject
/// Resources/ActionCommandTagList.asset 으로 생성하여 사용
/// </summary>
[CreateAssetMenu(fileName = "ActionCommandTagList", menuName = "Combat/Tag List", order = 0)]
public class ActionCommandTagList : ScriptableObject
{
    [System.Serializable]
    public class TagEntry
    {
        [Tooltip("태그 이름")]
        public string tagName;
        
        [Tooltip("Inspector 표시용 색상 (선택 사항)")]
        public Color displayColor = Color.white;
    }
    
    [Tooltip("사용 가능한 검술 태그 리스트")]
    public List<TagEntry> tags = new List<TagEntry>();
    
    /// <summary>
    /// 모든 태그 이름 리스트 반환
    /// </summary>
    public List<string> GetAllTagNames()
    {
        List<string> names = new List<string>();
        foreach (var tag in tags)
        {
            if (!string.IsNullOrEmpty(tag.tagName))
                names.Add(tag.tagName);
        }
        return names;
    }
    
    /// <summary>
    /// 태그 이름이 존재하는지 확인
    /// </summary>
    public bool IsValidTag(string tagName)
    {
        return tags.Exists(t => t.tagName == tagName);
    }
    
    /// <summary>
    /// 싱글톤 인스턴스 (Resources에서 로드)
    /// </summary>
    private static ActionCommandTagList _instance;
    public static ActionCommandTagList Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ActionCommandTagList>("ActionCommandTagList");
                if (_instance == null)
                {
                    Debug.LogWarning("[ActionCommandTagList] Resources/ActionCommandTagList.asset을 찾을 수 없습니다.");
                }
            }
            return _instance;
        }
    }
}


