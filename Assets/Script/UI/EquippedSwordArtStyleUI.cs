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
    public class EquippedSwordArtStyleUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("인벤토리 참조")]
        [Tooltip("인벤토리 참조 (런타임에 설정)")]
        [SerializeField] private CharacterInventory inventory;
        
        [Tooltip("InventoryUI 참조 (ItemDetailPanel 연동용)")]
        private InventoryUI inventoryUI;
        
        [Header("UI 컴포넌트 - 유파 정보")]
        [Tooltip("유파 아이콘")]
        [SerializeField] private Image styleIcon;
        
        [Tooltip("유파 이름")]
        [SerializeField] private TextMeshProUGUI styleNameText;
        
        [Tooltip("유파 설명")]
        [SerializeField] private TextMeshProUGUI styleDescriptionText;
        
        [Tooltip("빈 슬롯 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI emptySlotText;
        
        [Header("UI 컴포넌트 - 검술 리스트")]
        [Tooltip("검술 리스트 컨테이너 (ActionCommandItemUI들이 생성될 부모)")]
        [SerializeField] private Transform commandListContainer;
        
        [Tooltip("검술 아이템 프리팹")]
        [SerializeField] private GameObject ActionCommandItemUIPrefab;
        
        [Header("기본 아이콘")]
        [Tooltip("빈 슬롯 아이콘")]
        [SerializeField] private Sprite emptyStyleIcon;
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        // UI 아이템 리스트
        private List<ActionCommandItemUI> commandItems = new List<ActionCommandItemUI>();
        
        // 선택 상태 관리
        private SelectableSlotUI selectableSlot;
        private bool isSelected = false;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // SelectableSlotUI 컴포넌트 가져오기 또는 추가
            selectableSlot = GetComponent<SelectableSlotUI>();
            if (selectableSlot == null)
            {
                selectableSlot = gameObject.AddComponent<SelectableSlotUI>();
                // FrameColor나 BackgroundColor 모드 사용 가능 (Inspector에서 설정)
                Debug.Log($"[EquippedSwordArtStyleUI] SelectableSlotUI 컴포넌트 자동 추가");
            }
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
            // 유파 아이콘
            if (styleIcon != null)
            {
                styleIcon.sprite = itemData.icon;
                styleIcon.enabled = itemData.icon != null;
            }
            
            // 유파 이름
            if (styleNameText != null)
            {
                styleNameText.text = styleData.styleName;
                if (enableDebugLog)
                {
                    Debug.Log($"[EquippedSwordArtStyleUI] 유파 이름 설정: {styleData.styleName}");
                    Debug.Log($"[EquippedSwordArtStyleUI] styleNameText GameObject: {styleNameText.gameObject.name}, Active: {styleNameText.gameObject.activeInHierarchy}, Text: '{styleNameText.text}'");
                }
            }
            else
            {
                Debug.LogWarning("[EquippedSwordArtStyleUI] styleNameText가 null입니다! Inspector에서 참조를 연결해주세요.");
            }
            
            // 유파 설명
            if (styleDescriptionText != null)
            {
                styleDescriptionText.text = styleData.description;
            }
            
            // 빈 슬롯 텍스트 숨김
            if (emptySlotText != null)
            {
                emptySlotText.text = "";
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
            if (enableDebugLog)
            {
                Debug.Log("[EquippedSwordArtStyleUI] ShowEmptySlot 호출");
                Debug.Log($"[EquippedSwordArtStyleUI] 호출 스택:\n{System.Environment.StackTrace}");
            }
            
            // 유파 아이콘
            if (styleIcon != null)
            {
                styleIcon.sprite = emptyStyleIcon;
                styleIcon.enabled = emptyStyleIcon != null;
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
            
            // 빈 슬롯 텍스트 표시
            if (emptySlotText != null)
            {
                emptySlotText.text = "유파 미장착";
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
        
        #region 클릭 이벤트 처리
        
        /// <summary>
        /// 클릭 이벤트 처리 (유파 아이템 상세 정보 표시)
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventory == null)
            {
                Debug.LogWarning("[EquippedSwordArtStyleUI] Inventory가 null입니다!");
                return;
            }
            
            // 유파 슬롯 찾기
            var styleSlot = inventory.equipmentSlots.Find(s => s.slotType == EquipmentSlotType.SwordArtStyle);
            if (styleSlot == null || styleSlot.IsEmpty())
            {
                if (enableDebugLog)
                    Debug.Log("[EquippedSwordArtStyleUI] 빈 유파 슬롯 클릭 - 아무 동작 없음");
                return;
            }
            
            // 같은 슬롯 재클릭 시 토글 처리
            bool alreadySelected = isSelected;
            
            if (alreadySelected)
            {
                // 선택 해제
                SetSelected(false);
                
                // 상세 정보 패널 숨기기
                if (inventoryUI != null && inventoryUI.ItemDetailPanel != null)
                {
                    inventoryUI.ItemDetailPanel.gameObject.SetActive(false);
                }
                
                if (enableDebugLog)
                    Debug.Log("[EquippedSwordArtStyleUI] 유파 슬롯 선택 해제 (토글)");
                return;
            }
            
            // 기존 선택 해제
            if (inventoryUI != null)
            {
                inventoryUI.ClearAllSelections();
            }
            
            // 선택 상태 설정
            SetSelected(true);
            
            // InventoryUI와 ItemDetailPanel 연동
            if (inventoryUI != null && inventoryUI.ItemDetailPanel != null)
            {
                // 장착된 유파 아이템을 ItemDetailPanel에 표시
                var tempOwnedItem = new OwnedItem(styleSlot.equippedItemKey, 1);
                tempOwnedItem.isEquipped = true;
                
                inventoryUI.ItemDetailPanel.ShowItem(tempOwnedItem);
                
                if (enableDebugLog)
                    Debug.Log($"[EquippedSwordArtStyleUI] 유파 슬롯 클릭: {styleSlot.equippedItemKey}");
            }
            else
            {
                if (enableDebugLog)
                    Debug.LogWarning("[EquippedSwordArtStyleUI] InventoryUI 또는 ItemDetailPanel이 연결되지 않았습니다");
            }
        }
        
        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(selected);
            }
        }
        
        /// <summary>
        /// 선택 해제 (외부에서 호출용)
        /// </summary>
        public void ClearSelection()
        {
            SetSelected(false);
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

