using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 검술 유파 키 ↔ SwordArtStyleData 자산 매핑 테이블
/// </summary>
[CreateAssetMenu(fileName = "SwordArtStyleDatabase", menuName = "Item/SwordArtStyle Database", order = 11)]
public class SwordArtStyleDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string key; // 고유 키
        public string displayName; // 표시용 이름 (선택)
        public SwordArtStyleData asset; // 실제 SO
    }

    [Header("검술 유파 매핑 테이블")]
    public List<Entry> styles = new List<Entry>();

    /// <summary>
    /// 키로 스타일 SO 조회
    /// </summary>
    public SwordArtStyleData GetStyle(string key)
    {
        if (string.IsNullOrEmpty(key) || styles == null) return null;
        var e = styles.Find(x => x != null && x.key == key);
        return e != null ? e.asset : null;
    }

    /// <summary>
    /// 포함 여부
    /// </summary>
    public bool ContainsKey(string key)
    {
        return !string.IsNullOrEmpty(key) && styles != null && styles.Exists(x => x != null && x.key == key);
    }
}


