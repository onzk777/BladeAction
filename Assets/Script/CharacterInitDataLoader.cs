using UnityEngine;

/// <summary>
/// CharacterInitData를 Resources에서 로드하는 유틸리티
/// GameObject 없이 정적 메서드로 제공
/// 
/// 사용 방법:
/// - CharacterInitData 에셋을 "Resources/Data/CharacterData/CharacterInitData/" 폴더에 배치
/// - Key 필드가 파일명과 일치하도록 설정 (예: "Player.asset" → key="Player")
/// </summary>
public static class CharacterInitDataLoader
{
    private const string RESOURCE_PATH = "Data/CharacterData/CharacterInitData/";
    
    /// <summary>
    /// Key로 CharacterInitData 로드
    /// </summary>
    /// <param name="key">CharacterInitData의 key (파일명과 동일해야 함)</param>
    /// <returns>로드된 CharacterInitData, 실패 시 null</returns>
    public static CharacterInitData Load(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[CharacterInitDataLoader] key가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        // Resources 폴더에서 로드
        string path = RESOURCE_PATH + key;
        CharacterInitData initData = Resources.Load<CharacterInitData>(path);
        
        if (initData == null)
        {
            Debug.LogError($"[CharacterInitDataLoader] '{path}'에서 CharacterInitData를 찾을 수 없습니다! " +
                          $"파일이 Resources/{RESOURCE_PATH} 폴더에 있는지, 파일명이 key와 일치하는지 확인하세요.");
            return null;
        }
        
        // Key 검증 (파일명과 key 필드가 일치하는지 확인)
        if (initData.key != key)
        {
            Debug.LogWarning($"[CharacterInitDataLoader] 파일명({key})과 key 필드({initData.key})가 일치하지 않습니다! " +
                            $"일관성을 위해 key 필드를 '{key}'로 수정하는 것을 권장합니다.");
        }
        
        Debug.Log($"[CharacterInitDataLoader] ✅ 로드 성공: {key} ({initData.characterName})");
        return initData;
    }
    
    /// <summary>
    /// 모든 CharacterInitData 로드 (디버그/테스트용)
    /// </summary>
    public static CharacterInitData[] LoadAll()
    {
        CharacterInitData[] allData = Resources.LoadAll<CharacterInitData>(RESOURCE_PATH);
        Debug.Log($"[CharacterInitDataLoader] Resources/{RESOURCE_PATH}에서 {allData.Length}개 로드됨");
        return allData;
    }
}

