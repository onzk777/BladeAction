using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임 내 모든 Character 인스턴스를 정의하는 데이터베이스 (인명등록부)
/// ScriptableObject로 정적 정의를 관리하며,
/// 런타임에는 CharacterDatabaseManager가 사본을 만들어 사용합니다.
/// </summary>
[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Character/CharacterDatabase", order = 0)]
public class CharacterDatabase : ScriptableObject
{
    [Header("플레이어 정의")]
    [Tooltip("플레이어 Character 인스턴스 정의")]
    public CharacterDatabaseEntry playerEntry = new CharacterDatabaseEntry
    {
        instanceId = "player",
        initDataKey = "player_default"
    };
    
    [Header("적 정의")]
    [Tooltip("게임에 등장하는 모든 Enemy Character 인스턴스들")]
    public List<CharacterDatabaseEntry> enemyEntries = new List<CharacterDatabaseEntry>();
    
    /// <summary>
    /// Instance ID로 Entry 조회 (에디터/디버그용)
    /// 런타임에는 CharacterDatabaseManager를 사용하세요.
    /// </summary>
    public CharacterDatabaseEntry FindEntry(string instanceId)
    {
        if (playerEntry.instanceId == instanceId)
        {
            return playerEntry;
        }
        
        foreach (var entry in enemyEntries)
        {
            if (entry.instanceId == instanceId)
            {
                return entry;
            }
        }
        
        return null;
    }
}

