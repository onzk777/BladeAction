using UnityEngine;

/// <summary>
/// CharacterDatabase의 개별 항목
/// 게임에 존재하는 각 Character 인스턴스를 정의합니다.
/// </summary>
[System.Serializable]
public class CharacterDatabaseEntry
{
    [Tooltip("Character 인스턴스 고유 ID (수기 입력)")]
    public string instanceId;  // 예: "player", "enemy_goblin_01", "boss_dragon"
    
    [Tooltip("초기화 데이터 Key (콤보박스에서 선택)")]
    [CharacterInitDataKey]
    public string initDataKey;  // 예: "player_default", "goblin_warrior"
}

