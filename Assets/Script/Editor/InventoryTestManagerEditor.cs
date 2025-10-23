#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using BladeAction.Item;

/// <summary>
/// InventoryTestManager Custom Editor
/// Inspector에서 아이템 드롭다운 선택 기능 제공
/// </summary>
[CustomEditor(typeof(InventoryTestManager))]
public class InventoryTestManagerEditor : Editor
{
    private InventoryTestManager testManager;
    private ItemDatabase itemDatabase;
    private List<string> availableItems = new List<string>();
    private List<string> currentInventoryItems = new List<string>();
    private string[] availableItemNames;
    private string[] currentInventoryItemNames;
    
    void OnEnable()
    {
        testManager = (InventoryTestManager)target;
        
        // ItemDatabase 로드
        itemDatabase = ItemDatabase.Instance;
        
        // 아이템 목록 업데이트
        UpdateItemLists();
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 기본 필드들 표시
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("아이템 테스트 도구", EditorStyles.boldLabel);
        
        // ItemDatabase 상태 확인
        if (itemDatabase == null)
        {
            EditorGUILayout.HelpBox("ItemDatabase를 찾을 수 없습니다!\nResources 폴더에 ItemDatabase.asset이 있는지 확인하세요.", MessageType.Error);
            return;
        }
        
        if (availableItems.Count == 0)
        {
            EditorGUILayout.HelpBox("사용 가능한 아이템이 없습니다.\nItemDatabase에 아이템을 추가하세요.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.Space();
        
        // 아이템 추가 섹션
        EditorGUILayout.LabelField("아이템 추가", EditorStyles.boldLabel);
        
        // 사용 가능한 아이템 드롭다운
        int newSelectedIndex = EditorGUILayout.Popup("추가할 아이템", testManager.selectedItemIndex, availableItemNames);
        if (newSelectedIndex != testManager.selectedItemIndex)
        {
            testManager.selectedItemIndex = newSelectedIndex;
            EditorUtility.SetDirty(testManager);
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("선택한 아이템 추가"))
        {
            testManager.AddSelectedItem();
        }
        if (GUILayout.Button("모든 아이템 추가"))
        {
            AddAllItems();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 아이템 제거 섹션
        EditorGUILayout.LabelField("아이템 제거", EditorStyles.boldLabel);
        
        // 현재 인벤토리 아이템 드롭다운
        if (currentInventoryItems.Count > 0)
        {
            int newCurrentIndex = EditorGUILayout.Popup("제거할 아이템", testManager.selectedItemIndex, currentInventoryItemNames);
            if (newCurrentIndex != testManager.selectedItemIndex)
            {
                testManager.selectedItemIndex = newCurrentIndex;
                EditorUtility.SetDirty(testManager);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택한 아이템 제거"))
            {
                testManager.RemoveSelectedItem();
            }
            if (GUILayout.Button("모든 아이템 제거"))
            {
                RemoveAllItems();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("인벤토리가 비어있습니다.", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // 인벤토리 제어 버튼
        EditorGUILayout.LabelField("인벤토리 제어", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("인벤토리 열기/닫기"))
        {
            testManager.ToggleInventory();
        }
        if (GUILayout.Button("인벤토리 새로고침"))
        {
            testManager.RefreshInventory();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 새로고침 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("목록 새로고침"))
        {
            UpdateItemLists();
            testManager.RefreshInventoryList();
        }
        if (GUILayout.Button("인벤토리 초기화"))
        {
            InitializeTest();
        }
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// 아이템 목록 업데이트
    /// </summary>
    private void UpdateItemLists()
    {
        if (itemDatabase == null || itemDatabase.items == null)
        {
            availableItems.Clear();
            currentInventoryItems.Clear();
            availableItemNames = new string[0];
            currentInventoryItemNames = new string[0];
            return;
        }
        
        // 사용 가능한 아이템 목록
        availableItems = itemDatabase.items.Select(item => item.itemKey).ToList();
        availableItemNames = itemDatabase.items.Select(item => $"[{item.itemKey}] {item.itemName}").ToArray();
        
        // 현재 인벤토리 아이템 목록
        currentInventoryItems = testManager.GetCurrentInventoryItems();
        currentInventoryItemNames = currentInventoryItems.ToArray();
    }
    
    /// <summary>
    /// 모든 아이템 추가
    /// </summary>
    private void AddAllItems()
    {
        if (itemDatabase == null || itemDatabase.items == null)
            return;
        
        foreach (var item in itemDatabase.items)
        {
            testManager.TestInventory.AddItem(item.itemKey, 1);
        }
        
        if (testManager.InventoryUI != null)
        {
            testManager.InventoryUI.RefreshAll();
        }
        
        UpdateItemLists();
        Debug.Log($"✅ 모든 아이템 추가 완료 ({itemDatabase.items.Count}개)");
    }
    
    /// <summary>
    /// 모든 아이템 제거
    /// </summary>
    private void RemoveAllItems()
    {
        if (testManager.TestInventory == null)
            return;
        
        var itemsToRemove = testManager.TestInventory.items.ToList();
        foreach (var item in itemsToRemove)
        {
            testManager.TestInventory.RemoveItem(item.itemKey, item.quantity);
        }
        
        if (testManager.InventoryUI != null)
        {
            testManager.InventoryUI.RefreshAll();
        }
        
        UpdateItemLists();
        Debug.Log("✅ 모든 아이템 제거 완료");
    }
    
    /// <summary>
    /// 테스트 초기화
    /// </summary>
    private void InitializeTest()
    {
        testManager.InitializeTest();
        UpdateItemLists();
        Debug.Log("✅ 테스트 초기화 완료");
    }
}
#endif
