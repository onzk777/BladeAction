using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using BladeAction.Item;

namespace BladeAction.UI
{
    /// <summary>
    /// 장착된 검술 유파와 사용 가능한 검술 목록을 표시하는 UI
    /// </summary>
    public class EquippedSwordArtStyleUI : MonoBehaviour
    {
        [Header("인벤토리 참조")]
        [Tooltip("인벤토리 참조 (런타임에 설정)")]
        [SerializeField] private CharacterInventory inventory;
        
        [Tooltip("InventoryUI 참조 (ItemDetailPanel 연동용)")]
        private InventoryUI inventoryUI;
        
        [Header("유파 슬롯 생성 (컨테이너 및 프리팹 연결)")]
        [Tooltip("EquipmentSlotUI가 동적 생성될 컨테이너 (SwordArtStyleSlot GameObject)")]
        [SerializeField] private Transform equipmentSlotContainer;
        
        [Tooltip("장비 슬롯 프리팹 (EquipmentSlot.prefab)")]
        [SerializeField] private GameObject equipmentSlotPrefab;
        
        [Header("UI 컴포넌트 - 유파 정보")]
        [Tooltip("유파 이름")]
        [SerializeField] private TextMeshProUGUI styleNameText;
        
        [Tooltip("유파 설명")]
        [SerializeField] private TextMeshProUGUI styleDescriptionText;
        
        [Header("UI 컴포넌트 - 검술 리스트")]
        [Tooltip("검술 리스트 컨테이너 (ActionCommandItemUI들이 생성될 부모)")]
        [SerializeField] private Transform commandListContainer;
        
        [Tooltip("검술 아이템 프리팹")]
        [SerializeField] private GameObject ActionCommandItemUIPrefab;
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        // UI 아이템 리스트
        private List<ActionCommandItemUI> commandItems = new List<ActionCommandItemUI>();
        
        // 자식 슬롯 UI (드래그 앤 드롭 및 선택 관리)
        private EquipmentSlotUI equipmentSlotUI;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // 자식 EquipmentSlotUI 찾기 또는 생성
            equipmentSlotUI = GetComponentInChildren<EquipmentSlotUI>();
            
            if (equipmentSlotUI == null)
            {
                // 컨테이너와 프리팹이 설정되어 있으면 동적 생성
                if (equipmentSlotContainer != null && equipmentSlotPrefab != null)
                {
                    CreateEquipmentSlot();
                }
                else
                {
                    Debug.LogWarning($"[EquippedSwordArtStyleUI] EquipmentSlotUI를 찾을 수 없고, 컨테이너/프리팹도 설정되지 않았습니다.");
                }
            }
        }
        
        /// <summary>
        /// EquipmentSlotUI 동적 생성
        /// </summary>
        private void CreateEquipmentSlot()
        {
            if (equipmentSlotContainer == null || equipmentSlotPrefab == null)
            {
                Debug.LogError("[EquippedSwordArtStyleUI] equipmentSlotContainer 또는 equipmentSlotPrefab이 null입니다!");
                return;
            }
            
            // 기존 슬롯이 있으면 제거
            foreach (Transform child in equipmentSlotContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 새 슬롯 생성
            GameObject slotObj = Instantiate(equipmentSlotPrefab, equipmentSlotContainer);
            slotObj.name = "SwordArtStyleSlot";
            
            equipmentSlotUI = slotObj.GetComponent<EquipmentSlotUI>();
            if (equipmentSlotUI == null)
            {
                Debug.LogError("[EquippedSwordArtStyleUI] 생성된 슬롯에 EquipmentSlotUI 컴포넌트가 없습니다!");
                return;
            }
            
            // 빈 슬롯으로 초기화
            var emptySlot = new EquipmentSlot(EquipmentSlotType.SwordArtStyle, "검술 유파");
            equipmentSlotUI.Setup(emptySlot, hideTextForAccessorySlot: true);
            
            // **클릭 이벤트는 InventoryUI가 처리하도록 위임 (다른 장비 슬롯과 동일하게)**
            // OnEquipmentSlotClicked는 사용하지 않음
            
            if (enableDebugLog)
                Debug.Log("[EquippedSwordArtStyleUI] EquipmentSlotUI 동적 생성 완료");
        }
        
        /// <summary>
        /// 생성된 EquipmentSlotUI 반환 (InventoryUI가 equipmentSlots 리스트에 추가하기 위함)
        /// </summary>
        public EquipmentSlotUI GetEquipmentSlotUI()
        {
            return equipmentSlotUI;
        }
        
        #endregion
        
        #region 초기화 및 설정
        
        /// <summary>
        /// 인벤토리 참조 설정
        /// </summary>
        public void Initialize(CharacterInventory inventory, InventoryUI inventoryUI = null)
        {
            this.inventory = inventory;
            this.inventoryUI = inventoryUI;
            Refresh();
        }
        
        #endregion
        
        #region UI 갱신
        
        /// <summary>
        /// 전체 UI 갱신
        /// </summary>
        public void Refresh()
        {
            if (enableDebugLog)
                Debug.Log("[EquippedSwordArtStyleUI] Refresh 시작");
            
            if (inventory == null)
            {
                if (enableDebugLog)
                    Debug.LogWarning("[EquippedSwordArtStyleUI] inventory가 null → ShowEmptySlot");
                Clear();
                return;
            }
            
            // 장착된 유파 슬롯 찾기
            var styleSlot = inventory.equipmentSlots.Find(s => s.slotType == EquipmentSlotType.SwordArtStyle);
            
            if (styleSlot == null || styleSlot.IsEmpty())
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[EquippedSwordArtStyleUI] 유파 슬롯 없거나 비어있음 (null:{styleSlot == null}, empty:{styleSlot?.IsEmpty()}) → ShowEmptySlot");
                ShowEmptySlot();
                return;
            }
            
            // 유파 아이템 데이터 가져오기
            var styleItemData = styleSlot.GetEquippedItem();
            if (styleItemData == null)
            {
                if (enableDebugLog)
                    Debug.LogWarning("[EquippedSwordArtStyleUI] styleItemData가 null → ShowEmptySlot");
                ShowEmptySlot();
                return;
            }
            
            if (enableDebugLog)
                Debug.Log($"[EquippedSwordArtStyleUI] 유파 아이템 발견: {styleItemData.itemName}, Key: {styleItemData.swordArtStyleKey}");
            
            // 유파 ScriptableObject 가져오기 (키 기반)
            var db = EnsureStyleDatabase(styleItemData.swordArtStyleKey);
            var swordArtStyle = styleItemData.GetSwordArtStyle(db);
            if (swordArtStyle == null)
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[EquippedSwordArtStyleUI] swordArtStyle이 null (Key: {styleItemData.swordArtStyleKey}) → ShowEmptySlot");
                ShowEmptySlot();
                return;
            }
            
            if (enableDebugLog)
                Debug.Log($"[EquippedSwordArtStyleUI] 유파 데이터 발견: {swordArtStyle.styleName} → ShowSwordArtStyle 호출");
            
            // 유파 정보 표시
            ShowSwordArtStyle(styleItemData, swordArtStyle);
        }
        
        /// <summary>
        /// 검술 유파 정보 표시
        /// </summary>
        private void ShowSwordArtStyle(BladeAction.Item.Item itemData, SwordArtStyleData styleData)
        {
            // 자식 EquipmentSlotUI 업데이트 (드래그 앤 드롭용)
            var styleSlot = inventory?.equipmentSlots.Find(s => s.slotType == EquipmentSlotType.SwordArtStyle);
            if (equipmentSlotUI != null && styleSlot != null)
            {
                equipmentSlotUI.Setup(styleSlot);
                
                if (enableDebugLog)
                    Debug.Log($"[EquippedSwordArtStyleUI] EquipmentSlotUI 업데이트: {styleSlot.equippedItemKey}");
            }
            
            // 유파 이름 표시
            if (styleNameText != null)
            {
                styleNameText.text = styleData.styleName;
            }
            
            // 유파 설명 표시
            if (styleDescriptionText != null)
            {
                styleDescriptionText.text = styleData.description;
            }
            
            // 검술 리스트 표시
            ShowCommandList(styleData);
            
            if (enableDebugLog)
            {
                int cmdCount = styleData != null && styleData.ActionCommands != null ? styleData.ActionCommands.Count : 0;
                Debug.Log($"[EquippedSwordArtStyleUI] 유파 표시: {styleData.styleName} ({cmdCount}개 검술)");
            }
        }
        
        /// <summary>
        /// 검술 리스트 표시
        /// </summary>
        private void ShowCommandList(SwordArtStyleData styleData)
        {
            if (commandListContainer == null || ActionCommandItemUIPrefab == null)
                return;
            
            // 기존 리스트 제거
            ClearCommandList();
            
            // 검술 목록이 없으면 종료
            var commands = styleData.ActionCommands;
            if (commands == null || commands.Count == 0)
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[EquippedSwordArtStyleUI] {styleData.styleName}에 검술이 없습니다.");
                return;
            }
            
            // 검술 아이템 UI 생성
            foreach (var command in commands)
            {
                if (command == null)
                    continue;
                
                GameObject itemObj = Instantiate(ActionCommandItemUIPrefab, commandListContainer);
                ActionCommandItemUI itemUI = itemObj.GetComponent<ActionCommandItemUI>();
                
                if (itemUI != null)
                {
                    itemUI.Setup(command);
                    commandItems.Add(itemUI);
                }
                else
                {
                    Debug.LogWarning("[EquippedSwordArtStyleUI] commandItemPrefab에 ActionCommandItemUI 컴포넌트가 없습니다!");
                }
            }
            
            if (enableDebugLog)
                Debug.Log($"[EquippedSwordArtStyleUI] {commandItems.Count}개 검술 UI 생성 완료");
        }
        
        /// <summary>
        /// 검술 리스트 제거
        /// </summary>
        private void ClearCommandList()
        {
            foreach (var item in commandItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            commandItems.Clear();
        }
        
        /// <summary>
        /// 빈 슬롯 표시
        /// </summary>
        private void ShowEmptySlot()
        {
            // 자식 EquipmentSlotUI 비우기
            var styleSlot = inventory?.equipmentSlots.Find(s => s.slotType == EquipmentSlotType.SwordArtStyle);
            if (equipmentSlotUI != null && styleSlot != null)
            {
                equipmentSlotUI.Setup(styleSlot);
            }
            
            // 유파 이름 비우기
            if (styleNameText != null)
            {
                styleNameText.text = "";
            }
            
            // 유파 설명 비우기
            if (styleDescriptionText != null)
            {
                styleDescriptionText.text = "";
            }
            
            // 검술 리스트 비움
            ClearCommandList();
            
            if (enableDebugLog)
                Debug.Log("[EquippedSwordArtStyleUI] 빈 슬롯 표시 완료");
        }
        
        /// <summary>
        /// 전체 UI 비우기
        /// </summary>
        public void Clear()
        {
            ShowEmptySlot();
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Print Debug Info")]
        private void PrintDebugInfo()
        {
            Debug.Log("[EquippedSwordArtStyleUI] 디버그 정보:");
            Debug.Log($"  - Inventory: {(inventory != null ? inventory.inventoryName : "null")}");
            Debug.Log($"  - Command Items: {commandItems.Count}");
        }
        
        /// <summary>
        /// 강제 갱신 (디버그용)
        /// </summary>
        [ContextMenu("Force Refresh")]
        private void ForceRefresh()
        {
            Refresh();
            Debug.Log("[EquippedSwordArtStyleUI] 강제 갱신 완료");
        }
        
        #endregion
        
        #region 선택 및 하이라이트 관리 (EquipmentSlotUI 위임)
        
        /// <summary>
        /// 선택 상태 설정 (외부에서 호출용)
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (equipmentSlotUI != null)
            {
                equipmentSlotUI.SetSelected(selected);
            }
        }
        
        /// <summary>
        /// 선택 해제 (외부에서 호출용)
        /// </summary>
        public void ClearSelection()
        {
            if (equipmentSlotUI != null)
            {
                equipmentSlotUI.SetSelected(false);
            }
        }
        
        /// <summary>
        /// Frame Image 반환 (드래그 앤 드롭 하이라이트용)
        /// </summary>
        public Image GetFrameImage()
        {
            return equipmentSlotUI?.GetFrameImage();
        }
        
        #endregion
        
        private static SwordArtStyleDatabase cachedStyleDatabase;
        private SwordArtStyleDatabase EnsureStyleDatabase(string preferredKey)
        {
            if (cachedStyleDatabase != null)
                return cachedStyleDatabase;

            // Resources에서 자동 검색 (파일명/경로 무관)
            var found = Resources.LoadAll<SwordArtStyleDatabase>(string.Empty);
            if (found != null && found.Length > 0)
            {
                if (!string.IsNullOrEmpty(preferredKey))
                {
                    foreach (var db in found)
                    {
                        if (db != null && db.ContainsKey(preferredKey))
                        {
                            cachedStyleDatabase = db;
                            return cachedStyleDatabase;
                        }
                    }
                }
                cachedStyleDatabase = found[0];
            }
            return cachedStyleDatabase;
        }
    }
}

