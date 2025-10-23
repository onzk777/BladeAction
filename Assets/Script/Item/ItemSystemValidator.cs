using UnityEngine;
using System.Collections.Generic;
using BladeAction.Item;

/// <summary>
/// 아이템 시스템 Phase 4 검증 도구
/// 순차적이고 체계적인 검증을 위한 전용 테스트 클래스
/// </summary>
public class ItemSystemValidator : MonoBehaviour
{
    [Header("검증 설정")]
    // [SerializeField] private bool enableDetailLogs = true;  // 향후 사용 예정
    // [SerializeField] private float testDelay = 0.5f;        // 향후 사용 예정
    
    [Header("테스트 데이터")]
    [SerializeField] private string testItemKey = "test_sword_001";
    [SerializeField] private int testQuantity = 5;
    
    private CombatantInventory testInventory;
    
    // 검증 로그 구분자
    private const string LOG_PREFIX = "🔍 [ITEM_VALIDATOR]";
    private const string LOG_SUCCESS = "✅";
    private const string LOG_ERROR = "❌";
    private const string LOG_WARNING = "⚠️";
    private const string LOG_INFO = "ℹ️";
    
    private void Start()
    {
        LogSeparator("아이템 시스템 Phase 4 검증 시작");
    }
    
    /// <summary>
    /// 검증 로그 구분자 (다른 로그와 섞이지 않도록)
    /// </summary>
    private void LogSeparator(string message)
    {
        Debug.Log($"{LOG_PREFIX} ===== {message} =====");
    }
    
    private void LogSuccess(string message)
    {
        Debug.Log($"{LOG_PREFIX} {LOG_SUCCESS} {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"{LOG_PREFIX} {LOG_ERROR} {message}");
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"{LOG_PREFIX} {LOG_WARNING} {message}");
    }
    
    private void LogInfo(string message)
    {
        Debug.Log($"{LOG_PREFIX} {LOG_INFO} {message}");
    }
    
    /// <summary>
    /// 1단계: CombatantInventory 기본 구조 검증
    /// </summary>
    [ContextMenu("1단계: 기본 구조 검증")]
    public void ValidateBasicStructure()
    {
        LogSeparator("1단계: CombatantInventory 기본 구조 검증 시작");
        
        try
        {
            // 테스트 인벤토리 생성
            testInventory = new CombatantInventory();
            LogSuccess("CombatantInventory 인스턴스 생성 성공");
            
            // ItemDatabase.Instance 접근 테스트
            LogInfo("ItemDatabase.Instance 접근 테스트 중...");
            var database = ItemDatabase.Instance;
            if (database != null)
            {
                LogSuccess("ItemDatabase.Instance 접근 성공 (캐싱 시스템 정상)");
            }
            else
            {
                LogWarning("ItemDatabase.Instance가 null입니다. ItemDatabase.asset 파일이 Resources 폴더에 있는지 확인하세요.");
            }
            
            // 기본 설정 확인
            if (testInventory.maxItemSlots == 5000)
                LogSuccess($"최대 슬롯 수: {testInventory.maxItemSlots}");
            else
                LogError($"최대 슬롯 수 오류: 예상값 5000, 실제값 {testInventory.maxItemSlots}");
            
            // 장비 슬롯 초기화 확인 (8개 고정)
            if (testInventory.equipmentSlots.Count == 8)
            {
                LogSuccess($"장비 슬롯 개수: {testInventory.equipmentSlots.Count}개");
                
                // 각 슬롯 타입 확인
                int weaponCount = 0, armorCount = 0, accessoryCount = 0, swordArtCount = 0;
                foreach (var slot in testInventory.equipmentSlots)
                {
                    switch (slot.slotType)
                    {
                        case EquipmentSlotType.Weapon: weaponCount++; break;
                        case EquipmentSlotType.Armor: armorCount++; break;
                        case EquipmentSlotType.Accessory: accessoryCount++; break;
                        case EquipmentSlotType.SwordArtStyle: swordArtCount++; break;
                    }
                }
                
                LogInfo($"슬롯 구성: 무기{weaponCount}개, 갑옷{armorCount}개, 장신구{accessoryCount}개, 검술유파{swordArtCount}개");
                
                if (weaponCount == 1 && armorCount == 1 && accessoryCount == 3 && swordArtCount == 1)
                    LogSuccess("장비 슬롯 구성이 올바름 (총 6개: 무기1, 갑옷1, 장신구3, 유파1)");
                else
                    LogError($"장비 슬롯 구성 오류: 무기{weaponCount}, 방어구{armorCount}, 장신구{accessoryCount}, 검술유파{swordArtCount}");
            }
            else
            {
                LogError($"장비 슬롯 개수 오류: 예상값 8개, 실제값 {testInventory.equipmentSlots.Count}개");
            }
            
            // 초기 상태 확인
            if (testInventory.items.Count == 0)
                LogSuccess("초기 아이템 리스트가 비어있음 (정상)");
            else
                LogWarning($"초기 아이템 리스트에 {testInventory.items.Count}개 아이템 존재");
                
            if (!testInventory.isLocked)
                LogSuccess("초기 잠금 상태: 해제됨 (정상)");
            else
                LogError("초기 잠금 상태: 잠겨있음 (오류)");
            
            LogSeparator("1단계 검증 완료");
        }
        catch (System.Exception ex)
        {
            LogError($"1단계 검증 중 예외 발생: {ex.Message}");
            LogError($"스택 트레이스: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 2단계: 아이템 추가/제거 기능 검증
    /// </summary>
    [ContextMenu("2단계: 아이템 관리 기능 검증")]
    public void ValidateItemManagement()
    {
        if (testInventory == null)
        {
            LogError("테스트 인벤토리가 초기화되지 않음. 1단계를 먼저 실행하세요.");
            return;
        }
        
        LogSeparator("2단계: 아이템 추가/제거 기능 검증 시작");
        
        try
        {
            // 잘못된 아이템 키로 테스트
            bool result1 = testInventory.AddItem("", 1);
            if (!result1)
                LogSuccess("빈 아이템 키 추가 시도 → 실패 (정상)");
            else
                LogError("빈 아이템 키 추가 시도 → 성공 (오류)");
                
            // 존재하지 않는 아이템 키로 테스트
            bool result2 = testInventory.AddItem("nonexistent_item_12345", 1);
            if (!result2)
                LogSuccess("존재하지 않는 아이템 키 추가 시도 → 실패 (정상)");
            else
                LogError("존재하지 않는 아이템 키 추가 시도 → 성공 (오류)");
            
            // 잘못된 수량으로 테스트
            bool result3 = testInventory.AddItem(testItemKey, 0);
            if (!result3)
                LogSuccess("수량 0으로 추가 시도 → 실패 (정상)");
            else
                LogError("수량 0으로 추가 시도 → 성공 (오류)");
            
            LogInfo("ItemDatabase 인스턴스 접근 테스트...");
            var database = ItemDatabase.Instance;
            if (database != null)
            {
                LogSuccess("ItemDatabase.Instance 접근 성공 (캐싱 시스템 정상)");
                var testItem = database.GetItem(testItemKey);
                if (testItem != null)
                {
                    LogInfo($"테스트 아이템 발견: {testItem.itemName} (키: {testItem.itemKey})");
                    
                    // 실제 아이템 추가 테스트
                    bool result4 = testInventory.AddItem(testItemKey, testQuantity);
                    if (result4)
                    {
                        LogSuccess($"아이템 추가 성공: {testItemKey} x{testQuantity}");
                        
                        // 수량 확인
                        int actualQuantity = testInventory.GetItemQuantity(testItemKey);
                        if (actualQuantity == testQuantity)
                            LogSuccess($"수량 확인 성공: 예상 {testQuantity}, 실제 {actualQuantity}");
                        else
                            LogError($"수량 확인 실패: 예상 {testQuantity}, 실제 {actualQuantity}");
                    }
                    else
                    {
                        LogError($"아이템 추가 실패: {testItemKey}");
                    }
                }
                else
                {
                    LogWarning($"테스트용 아이템을 찾을 수 없음: {testItemKey}");
                    LogInfo("ItemDatabase에 있는 첫 번째 아이템으로 테스트 진행...");
                    
                    if (database.items != null && database.items.Count > 0)
                    {
                        var firstItem = database.items[0];
                        LogInfo($"대체 테스트 아이템 사용: {firstItem.itemName} (키: {firstItem.itemKey})");
                        
                        bool result5 = testInventory.AddItem(firstItem.itemKey, 1);
                        if (result5)
                        {
                            LogSuccess($"대체 아이템 추가 성공: {firstItem.itemKey}");
                        }
                        else
                        {
                            LogError($"대체 아이템 추가 실패: {firstItem.itemKey}");
                        }
                    }
                }
            }
            else
            {
                LogError("ItemDatabase.Instance가 null입니다. Resources/ItemDatabase.asset을 확인하세요.");
            }
            
            LogSeparator("2단계 검증 완료");
        }
        catch (System.Exception ex)
        {
            LogError($"2단계 검증 중 예외 발생: {ex.Message}");
            LogError($"스택 트레이스: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 검증 상태 요약
    /// </summary>
    [ContextMenu("검증 상태 요약")]
    public void ShowValidationSummary()
    {
        LogSeparator("검증 상태 요약");
        
        if (testInventory == null)
        {
            LogError("테스트 인벤토리가 초기화되지 않음");
            return;
        }
        
        LogInfo($"인벤토리 이름: {testInventory.inventoryName}");
        LogInfo($"최대 슬롯: {testInventory.maxItemSlots}");
        LogInfo($"현재 아이템 수: {testInventory.items.Count}");
        LogInfo($"장비 슬롯 수: {testInventory.equipmentSlots.Count}");
        LogInfo($"잠금 상태: {(testInventory.isLocked ? "잠김" : "해제")}");
        
        LogInfo("현재 소유 아이템:");
        foreach (var item in testInventory.items)
        {
            if (item != null && !item.IsEmpty())
            {
                LogInfo($"  - {item.itemKey}: {item.quantity}개");
            }
        }
        
        LogInfo("장비 슬롯 상태:");
        foreach (var slot in testInventory.equipmentSlots)
        {
            if (slot != null)
            {
                string status = slot.IsEmpty() ? "비어있음" : $"장착됨: {slot.equippedItemKey}";
                LogInfo($"  - {slot.slotName} ({slot.slotType}): {status}");
            }
        }
        
        LogSeparator("요약 완료");
    }
}
