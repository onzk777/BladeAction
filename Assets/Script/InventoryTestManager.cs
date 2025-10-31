using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BladeAction.Item;
using BladeAction.UI;

/// <summary>
/// 인벤토리 테스트를 위한 매니저
/// TestMode가 false이면 비활성화됩니다.
/// </summary>
public class InventoryTestManager : MonoBehaviour
{
    [Header("테스트 모드")]
    [Tooltip("테스트 모드 활성화 여부 (false면 이 스크립트는 아무것도 하지 않음)")]
    public bool testMode = false;
    
    [Header("테스트 설정")]
    public bool autoInitialize = true;
    public bool addTestItems = true;
    
    [Header("아이템 선택 테스트")]
    [Tooltip("추가할 아이템 선택 (드롭다운)")]
    public int selectedItemIndex = 0;
    
    [Tooltip("추가할 수량")]
    [Range(1, 99)]
    public int addQuantity = 1;
    
    [Tooltip("현재 인벤토리의 아이템 목록")]
    public List<string> currentInventoryItems = new List<string>();
    
    private CharacterInventory testInventory;
    private InventoryUI inventoryUI;
    private MainMenuManager mainMenuManager;
    
    // Editor에서 접근하기 위한 public 프로퍼티
    public CharacterInventory TestInventory => testInventory;
    public InventoryUI InventoryUI => inventoryUI;
    public MainMenuManager MainMenuManager => mainMenuManager;
    
    void Start()
    {
        // TestMode가 false면 아무것도 하지 않음
        if (!testMode)
        {
            Debug.Log("[InventoryTestManager] TestMode가 비활성화되어 있습니다. 스크립트 비활성화.");
            enabled = false;
            return;
        }
        
        Debug.Log("[InventoryTestManager] TestMode 활성화 - 테스트 인벤토리로 초기화");
        
        if (autoInitialize)
        {
            InitializeTest();
        }
    }
    
    
    void AddTestItems()
    {
        // 실제 데이터베이스에 있는 아이템으로 테스트
        testInventory.AddItem("itm_weapon_test1", 1);
        testInventory.AddItem("itm_weapon_test2", 1);
        
        Debug.Log("✅ 테스트 아이템 추가 완료");
        Debug.Log($"인벤토리 아이템 수: {testInventory.items.Count}");
        
        // 각 아이템 정보 출력
        foreach (var ownedItem in testInventory.items)
        {
            var itemData = ownedItem.GetItemData();
            if (itemData != null)
            {
                Debug.Log($"아이템: {itemData.itemName} (키: {ownedItem.itemKey}, 수량: {ownedItem.quantity})");
            }
            else
            {
                Debug.LogWarning($"아이템 데이터 없음: {ownedItem.itemKey}");
            }
        }
    }
    
    [ContextMenu("테스트 아이템 추가")]
    public void AddMoreTestItems()
    {
        // 실제 데이터베이스에 있는 아이템으로 추가
        testInventory.AddItem("itm_weapon_test1", 2); // 수량 증가
        testInventory.AddItem("itm_weapon_test2", 1); // 새로 추가
        
        // UI 갱신
        inventoryUI.RefreshAll();
        Debug.Log("✅ 추가 테스트 아이템 추가 완료");
        Debug.Log($"총 인벤토리 아이템 수: {testInventory.items.Count}");
    }
    
    [ContextMenu("선택한 아이템 추가")]
    public void AddSelectedItem()
    {
        if (testInventory == null)
        {
            Debug.LogError("[InventoryTestManager] 테스트 인벤토리가 초기화되지 않았습니다!");
            return;
        }
        
        // 사용 가능한 아이템 목록 가져오기
        var availableItems = GetAvailableItems();
        if (availableItems.Count == 0)
        {
            Debug.LogWarning("[InventoryTestManager] 사용 가능한 아이템이 없습니다!");
            return;
        }
        
        // 선택된 인덱스가 유효한지 확인
        if (selectedItemIndex < 0 || selectedItemIndex >= availableItems.Count)
        {
            Debug.LogWarning($"[InventoryTestManager] 잘못된 아이템 인덱스: {selectedItemIndex} (최대: {availableItems.Count - 1})");
            return;
        }
        
        // 아이템 추가
        string itemKey = availableItems[selectedItemIndex];
        testInventory.AddItem(itemKey, addQuantity);
        
        // UI 갱신
        if (inventoryUI != null)
        {
            inventoryUI.RefreshAll();
        }
        
        // 현재 인벤토리 목록 업데이트
        UpdateCurrentInventoryList();
        
        Debug.Log($"✅ 아이템 추가 완료: {itemKey} x{addQuantity}");
    }
    
    [ContextMenu("선택한 아이템 제거")]
    public void RemoveSelectedItem()
    {
        if (testInventory == null)
        {
            Debug.LogError("[InventoryTestManager] 테스트 인벤토리가 초기화되지 않았습니다!");
            return;
        }
        
        // 현재 인벤토리 아이템 목록 가져오기
        var currentItems = GetCurrentInventoryItems();
        if (currentItems.Count == 0)
        {
            Debug.LogWarning("[InventoryTestManager] 인벤토리가 비어있습니다!");
            return;
        }
        
        // 선택된 인덱스가 유효한지 확인
        if (selectedItemIndex < 0 || selectedItemIndex >= currentItems.Count)
        {
            Debug.LogWarning($"[InventoryTestManager] 잘못된 아이템 인덱스: {selectedItemIndex} (최대: {currentItems.Count - 1})");
            return;
        }
        
        // 아이템 제거
        string itemKey = currentItems[selectedItemIndex];
        testInventory.RemoveItem(itemKey, addQuantity);
        
        // UI 갱신
        if (inventoryUI != null)
        {
            inventoryUI.RefreshAll();
        }
        
        // 현재 인벤토리 목록 업데이트
        UpdateCurrentInventoryList();
        
        Debug.Log($"✅ 아이템 제거 완료: {itemKey} x{addQuantity}");
    }
    
    [ContextMenu("인벤토리 목록 새로고침")]
    public void RefreshInventoryList()
    {
        UpdateCurrentInventoryList();
        Debug.Log("✅ 인벤토리 목록 새로고침 완료");
    }
    
    [ContextMenu("인벤토리 토글")]
    public void ToggleInventory()
    {
        if (mainMenuManager != null)
        {
            // MainMenuCanvas가 활성화되어 있으면 닫고, 아니면 열기
            if (mainMenuManager.gameObject.activeSelf)
            {
                mainMenuManager.CloseMainMenu();
                Debug.Log("✅ 메인 메뉴 닫기");
            }
            else
            {
                mainMenuManager.OpenMainMenu();
                Debug.Log("✅ 메인 메뉴 열기 (인벤토리 탭)");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryTestManager] MainMenuManager를 찾을 수 없습니다!");
        }
    }
    
    [ContextMenu("소지품 탭 보기")]
    public void ShowInventoryTab()
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.ShowInventoryTab();
            Debug.Log("✅ 소지품 탭으로 전환");
        }
    }
    
    [ContextMenu("검술 탭 보기")]
    public void ShowActionCommandTab()
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.ShowActionCommandTab();
            Debug.Log("✅ 검술 탭으로 전환");
        }
    }
    
    [ContextMenu("인벤토리 새로고침")]
    public void RefreshInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.RefreshAll();
            Debug.Log("✅ 인벤토리 새로고침 완료");
        }
    }
    
    [ContextMenu("디버그 정보 출력")]
    public void PrintDebugInfo()
    {
        if (testInventory != null)
        {
            Debug.Log($"[InventoryTestManager] 인벤토리 정보:");
            Debug.Log($"  - 아이템 수: {testInventory.items.Count}");
            Debug.Log($"  - 장비 슬롯 수: {testInventory.equipmentSlots.Count}");
            
            foreach (var item in testInventory.items)
            {
                Debug.Log($"  - 아이템: {item.itemKey} x{item.quantity}");
            }
        }
    }
    
    #region 헬퍼 메서드
    
    /// <summary>
    /// 데이터베이스에서 사용 가능한 아이템 목록 가져오기
    /// </summary>
    private List<string> GetAvailableItems()
    {
        var itemDatabase = ItemDatabase.Instance;
        if (itemDatabase == null || itemDatabase.items == null)
        {
            Debug.LogWarning("[InventoryTestManager] ItemDatabase를 찾을 수 없습니다!");
            return new List<string>();
        }
        
        return itemDatabase.items.Select(item => item.itemKey).ToList();
    }
    
    
    /// <summary>
    /// 현재 인벤토리 목록 업데이트
    /// </summary>
    private void UpdateCurrentInventoryList()
    {
        currentInventoryItems = GetCurrentInventoryItems();
    }
    
    /// <summary>
    /// Editor에서 접근하기 위한 public 메서드
    /// </summary>
    public List<string> GetCurrentInventoryItems()
    {
        if (testInventory == null || testInventory.items == null)
        {
            return new List<string>();
        }
        
        return testInventory.items.Select(item => item.itemKey).ToList();
    }
    
    /// <summary>
    /// Editor에서 접근하기 위한 public 메서드
    /// </summary>
    public void InitializeTest()
    {
        // TestMode가 false면 절대 실행하지 않음
        if (!testMode)
        {
            Debug.LogWarning("[InventoryTestManager] TestMode가 false입니다. InitializeTest() 무시됨.");
            return;
        }
        
        if (testInventory == null)
        {
            Debug.Log("[InventoryTestManager] 테스트 초기화 시작");
            
            // 1. 테스트 인벤토리 생성 (생성자에서 자동 초기화됨)
            testInventory = new CharacterInventory();
            Debug.Log($"✅ 테스트 인벤토리 생성: {testInventory.items.Count}개 아이템");
            
            // 2. MainMenuManager 찾기
            mainMenuManager = FindFirstObjectByType<MainMenuManager>();
            if (mainMenuManager == null)
            {
                Debug.LogWarning("❌ MainMenuManager를 찾을 수 없습니다!");
            }
            else
            {
                Debug.Log("✅ MainMenuManager 찾기 완료");
            }
            
            // 3. InventoryUI 찾기 및 초기화
            inventoryUI = FindFirstObjectByType<InventoryUI>();
            if (inventoryUI != null)
            {
                #pragma warning disable CS0618 // Initialize는 deprecated이지만 테스트용으로 사용
                inventoryUI.Initialize(testInventory);
                #pragma warning restore CS0618
                Debug.Log("✅ InventoryUI 초기화 완료 (테스트 모드)");
            }
            else
            {
                Debug.LogError("❌ InventoryUI를 찾을 수 없습니다!");
            }
            
            // 4. 테스트 아이템 추가
            if (addTestItems)
            {
                AddTestItems();
            }
            
            Debug.Log("[InventoryTestManager] 테스트 초기화 완료");
        }
    }
    
    #endregion
}
