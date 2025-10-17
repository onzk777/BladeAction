using System;
using UnityEngine;

/// <summary>
/// 데이터베이스의 키 값을 드롭다운으로 선택할 수 있게 하는 범용 Attribute
/// 프로젝트 전체에서 사용 가능 (Item, Monster, Quest 등 모든 시스템)
/// 
/// 사용 예시:
/// [DatabaseKey(typeof(StatDatabase), "statTables", "tableKey")]
/// public string statTableKey;
/// 
/// [DatabaseKey(typeof(ItemDatabase), "items", "itemKey", "itemName")]
/// public string itemKey;
/// </summary>
public class DatabaseKeyAttribute : PropertyAttribute
{
    /// <summary>
    /// 데이터베이스 타입 (예: StatDatabase, ItemDatabase)
    /// </summary>
    public Type DatabaseType { get; private set; }
    
    /// <summary>
    /// 리스트 필드 이름 (예: "statTables", "items")
    /// </summary>
    public string ListFieldName { get; private set; }
    
    /// <summary>
    /// 키 필드 이름 (예: "tableKey", "itemKey")
    /// </summary>
    public string KeyFieldName { get; private set; }
    
    /// <summary>
    /// 선택적: 표시 이름 필드 (예: "itemName")
    /// null이면 KeyFieldName만 표시
    /// </summary>
    public string DisplayNameField { get; private set; }
    
    /// <summary>
    /// 선택적: 데이터베이스 Asset 경로
    /// null이면 자동 검색 (첫 번째 것 사용)
    /// </summary>
    public string DatabasePath { get; private set; }
    
    /// <summary>
    /// 데이터베이스 키 Attribute 생성자
    /// </summary>
    /// <param name="databaseType">데이터베이스 ScriptableObject 타입</param>
    /// <param name="listFieldName">리스트 필드 이름</param>
    /// <param name="keyFieldName">키 필드 이름</param>
    /// <param name="displayNameField">표시 이름 필드 (선택)</param>
    /// <param name="databasePath">데이터베이스 경로 (선택, 예: "Assets/Data/ItemDatabase.asset")</param>
    public DatabaseKeyAttribute(Type databaseType, string listFieldName, string keyFieldName, 
                               string displayNameField = null, string databasePath = null)
    {
        DatabaseType = databaseType;
        ListFieldName = listFieldName;
        KeyFieldName = keyFieldName;
        DisplayNameField = displayNameField;
        DatabasePath = databasePath;
    }
}

