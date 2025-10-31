using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BladeAction.Item;
using System.Collections.Generic;

namespace BladeAction.UI
{
    /// <summary>
    /// 아이템 상세 정보 패널
    /// 선택된 아이템의 상세 정보를 표시하고 액션 버튼을 제공합니다.
    /// </summary>
    public class ItemDetailPanel : MonoBehaviour
    {
        [Header("인벤토리 참조")]
        [Tooltip("인벤토리 참조 (런타임에 설정)")]
        [SerializeField] private CharacterInventory inventory;
        
        // InventoryUI 참조 (포커스 관리용)
        private InventoryUI inventoryUI;
        
        [Header("UI 컴포넌트 - 기본 정보")]
        [Tooltip("선택된 아이템 아이콘")]
        [SerializeField] private Image itemIcon;
        
        [Tooltip("선택된 아이템 이름")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        
        [Header("UI 컴포넌트 - 스탯 정보")]
        [Tooltip("스탯 정보 텍스트 배열 (최대 6개)")]
        [SerializeField] private TextMeshProUGUI[] statInfoTexts = new TextMeshProUGUI[6];
        
        [Header("UI 컨테이너")]
        [Tooltip("스탯 정보를 감싸는 루트 오브젝트 (예: ItemStatsInfo)")]
        [SerializeField] private GameObject statsContainer;
        
        [Tooltip("설명(스크롤뷰 포함)을 감싸는 루트 오브젝트 (예: ItemDescription)")]
        [SerializeField] private GameObject descriptionContainer;

        [Header("스탯 동적 생성")]
        [Tooltip("스탯 1줄 표시용 프리팹 (예: ItemDetail_StatInfo)")]
        [SerializeField] private GameObject statInfoItemPrefab;

        // 동적 생성된 스탯 라인 캐시
        private readonly List<TextMeshProUGUI> spawnedStatLines = new List<TextMeshProUGUI>();
        
        [Header("UI 컴포넌트 - 설명")]
        [Tooltip("아이템 설명 텍스트")]
        [SerializeField] private TextMeshProUGUI descriptionText;
        
        [Tooltip("설명/스탯 토글 버튼")]
        [SerializeField] private Button toggleButton;
        
        [Tooltip("토글 버튼 텍스트")]
        [SerializeField] private TextMeshProUGUI toggleButtonText;
        
        [Header("UI 컴포넌트 - 액션 버튼")]
        [Tooltip("장착/해제 버튼")]
        [SerializeField] private Button equipButton;
        
        [Tooltip("장착/해제 버튼 텍스트")]
        [SerializeField] private TextMeshProUGUI equipButtonText;
        
        [Tooltip("사용 버튼")]
        [SerializeField] private Button useButton;
        
        [Tooltip("버리기 버튼")]
        [SerializeField] private Button dropButton;
        
        [Header("기본 아이콘")]
        [Tooltip("아이템 미선택 시 표시할 기본 아이콘")]
        [SerializeField] private Sprite defaultIcon;
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        // 현재 선택된 아이템
        private OwnedItem currentItem;
        
        // 토글 상태 (true: 설명, false: 스탯)
        private bool showDescription = true;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // 초기 상태 설정
            Clear();
        }
        
        private void Start()
        {
            // 버튼 이벤트 연결 (Start에서 실행하여 GameObject가 활성화된 후 연결)
            if (equipButton != null)
                equipButton.onClick.AddListener(OnEquipButtonClicked);
            
            if (useButton != null)
                useButton.onClick.AddListener(OnUseButtonClicked);
            
            if (dropButton != null)
                dropButton.onClick.AddListener(OnDropButtonClicked);
            
            if (toggleButton != null)
                toggleButton.onClick.AddListener(OnToggleButtonClicked);
        }
        
        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (equipButton != null)
                equipButton.onClick.RemoveListener(OnEquipButtonClicked);
            
            if (useButton != null)
                useButton.onClick.RemoveListener(OnUseButtonClicked);
            
            if (dropButton != null)
                dropButton.onClick.RemoveListener(OnDropButtonClicked);
            
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(OnToggleButtonClicked);
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
            Clear();
        }
        
        #endregion
        
        #region 아이템 표시
        
        /// <summary>
        /// 아이템 상세 정보 표시
        /// </summary>
        /// <param name="item">표시할 OwnedItem</param>
        public void ShowItem(OwnedItem item)
        {
            if (item == null || item.IsEmpty())
            {
                HidePanel();
                return;
            }
            
            // 패널 활성화
            gameObject.SetActive(true);
            currentItem = item;
            
            // 아이템 데이터 가져오기
            BladeAction.Item.Item itemData = item.GetItemData();
            if (itemData == null)
            {
                Debug.LogWarning($"[ItemDetailPanel] 아이템 데이터를 찾을 수 없습니다: {item.itemKey}");
                Clear();
                return;
            }
            
            // 기본 정보 표시
            ShowBasicInfo(itemData);
            
            // 스탯 정보 표시
            ShowStatInfo(itemData);
            
            // 설명 표시
            ShowDescription(itemData);
            
            // 토글 표시 초기화
            UpdateToggleDisplay();
            
            // 버튼 상태 업데이트
            UpdateButtons(item, itemData);

            // 스탯 토글 버튼 가시성: StatTable 미연결 시 토글 기능 숨김
            bool canShowStats = itemData.useStatTable && !string.IsNullOrEmpty(itemData.statTableKey);
            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(canShowStats);
            }
            if (toggleButtonText != null)
            {
                toggleButtonText.gameObject.SetActive(canShowStats);
            }
            
            // 스탯 정보가 없으면 강제로 설명 표시
            if (!canShowStats)
            {
                showDescription = true;
                UpdateToggleDisplay();
            }
        }
        
        /// <summary>
        /// 기본 정보 표시 (아이콘, 이름)
        /// </summary>
        private void ShowBasicInfo(BladeAction.Item.Item itemData)
        {
            // 아이콘
            if (itemIcon != null)
            {
                itemIcon.sprite = itemData.icon;
                itemIcon.enabled = itemData.icon != null;
            }
            
            // 이름
            if (itemNameText != null)
            {
                itemNameText.text = itemData.itemName;
            }
        }
        
        /// <summary>
        /// 스탯 정보 표시
        /// </summary>
        private void ShowStatInfo(BladeAction.Item.Item itemData)
        {
            // 기존 동적 라인 비활성화 (풀 유지)
            HideAllStatLines();

            // 장비 아이템인 경우 스탯 표시
            if (itemData.itemType == ItemType.Weapon || 
                itemData.itemType == ItemType.Armor || 
                itemData.itemType == ItemType.Accessory)
            {
                var stats = itemData.GetStats(ItemDatabase.Instance?.statDatabase);
                if (stats != null)
                {
                    List<string> statStrings = new List<string>();
                    
                    // 스탯 수집 (0 숨김, 음수/양수만 표시)
                    if (stats.attackPower != 0)
                        statStrings.Add($"공격력: {FormatSigned(stats.attackPower, 1)}");

                    if (stats.blockEfficiency != 0)
                        statStrings.Add($"막기 효율: {FormatSignedPercent(stats.blockEfficiency)}%");

                    if (stats.blockPoiseConsumption != 0)
                        statStrings.Add($"막기 Poise 소모량: {FormatSigned(stats.blockPoiseConsumption, 1)}");

                    if (stats.parryEfficiency != 0)
                        statStrings.Add($"쳐내기 효율: {FormatSignedPercent(stats.parryEfficiency)}%");

                    if (stats.parryPoiseConsumption != 0)
                        statStrings.Add($"쳐내기 Poise 소모량: {FormatSigned(stats.parryPoiseConsumption, 1)}");

                    if (stats.parryPoiseAttackPower != 0)
                        statStrings.Add($"쳐내기 Poise 공격력: {FormatSigned(stats.parryPoiseAttackPower, 1)}");

                    if (stats.maxHP != 0)
                        statStrings.Add($"최대 HP: {FormatSigned(stats.maxHP, 0)}");

                    if (stats.damageReduction != 0)
                        statStrings.Add($"피해 감소: {FormatSignedPercent(stats.damageReduction)}%");

                    if (stats.poise != 0)
                        statStrings.Add($"Poise: {FormatSigned(stats.poise, 0)}");
                    
                    // 필요 개수만큼 라인 확보 후 채우기
                    EnsureStatLineCount(statStrings.Count);
                    for (int i = 0; i < statStrings.Count; i++)
                    {
                        var line = spawnedStatLines[i];
                        if (line != null)
                        {
                            line.text = statStrings[i];
                            line.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 설명 표시
        /// </summary>
        private void ShowDescription(BladeAction.Item.Item itemData)
        {
            if (descriptionText != null)
            {
                descriptionText.text = itemData.description;
            }
        }
        
        /// <summary>
        /// 버튼 상태 업데이트
        /// </summary>
        private void UpdateButtons(OwnedItem item, BladeAction.Item.Item itemData)
        {
            // 장착/해제 버튼
            if (equipButton != null && equipButtonText != null)
            {
                // 장비 아이템인 경우만 활성화
                bool isEquipment = itemData.itemType == ItemType.Weapon || 
                                   itemData.itemType == ItemType.Armor || 
                                   itemData.itemType == ItemType.Accessory ||
                                   itemData.itemType == ItemType.SwordArtStyle;
                
                equipButton.interactable = isEquipment;
                
                if (isEquipment)
                {
                    // 장착 여부에 따라 버튼 텍스트 변경
                    equipButtonText.text = item.isEquipped ? "해제" : "장착";
                }
                else
                {
                    equipButtonText.text = "장착";
                }
            }
            
            // 사용 버튼 (현재는 비활성화, 추후 소모품 구현 시 활성화)
            if (useButton != null)
            {
                useButton.interactable = false;
            }
            
            // 버리기 버튼
            if (dropButton != null)
            {
                // 잠긴 아이템이나 장착 중인 아이템은 버리기 불가
                dropButton.interactable = !item.isLocked && !item.isEquipped;
            }
        }
        
        /// <summary>
        /// 패널 숨기기 (비활성화)
        /// </summary>
        public void HidePanel()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 패널 비우기
        /// </summary>
        public void Clear()
        {
            currentItem = null;
            
            // 아이콘 초기화
            if (itemIcon != null)
            {
                itemIcon.sprite = defaultIcon;
                itemIcon.enabled = defaultIcon != null;
            }
            
            // 이름 초기화
            if (itemNameText != null)
            {
                itemNameText.text = "아이템을 선택하세요";
            }
            
            // 스탯 텍스트 숨김
            HideAllStatLines();
            
            // 설명 초기화
            if (descriptionText != null)
            {
                descriptionText.text = "";
            }
            
            // 컨테이너 가시성 초기화 (기본: 설명 보임, 스탯 숨김)
            if (descriptionContainer != null)
                descriptionContainer.SetActive(true);
            if (statsContainer != null)
                statsContainer.SetActive(false);
            
            // 버튼 비활성화
            if (equipButton != null)
                equipButton.interactable = false;
            
            if (useButton != null)
                useButton.interactable = false;
            
            if (dropButton != null)
                dropButton.interactable = false;
        }
        
        #endregion
        
        #region 버튼 이벤트 처리
        
        /// <summary>
        /// 장착/해제 버튼 클릭
        /// </summary>
        private void OnEquipButtonClicked()
        {
            if (currentItem == null || inventory == null)
                return;
            
            BladeAction.Item.Item itemData = currentItem.GetItemData();
            if (itemData == null)
                return;
            
            // 장착 여부에 따라 처리
            if (currentItem.isEquipped)
            {
                // 해제
                UnequipItem(itemData);
            }
            else
            {
                // 장착
                EquipItem(itemData);
            }
        }
        
        /// <summary>
        /// 아이템 장착 (외부에서 호출 가능)
        /// </summary>
        public void EquipItem(BladeAction.Item.Item itemData)
        {
            // 아이템 타입에 맞는 슬롯 타입 결정
            EquipmentSlotType slotType = GetSlotTypeForItem(itemData.itemType);
            
            if (slotType == EquipmentSlotType.None)
            {
                Debug.LogWarning($"[ItemDetailPanel] 장착할 수 없는 아이템 타입: {itemData.itemType}");
                return;
            }
            
            // 인벤토리에 장착 요청
            bool success = inventory.EquipItem(currentItem.itemKey, slotType, out var equippedSlot);
            
            if (enableDebugLog)
                Debug.Log($"[ItemDetailPanel] 아이템 장착: {currentItem.itemKey} → {slotType} ({success})");
            
            if (success)
            {
                // UI 갱신 (ItemEvents에서 자동으로 갱신됨)
                ShowItem(currentItem); // 버튼 텍스트 업데이트
                
                // 포커스를 실제 장착된 슬롯으로 이동
                if (inventoryUI != null && equippedSlot != null)
                {
                    inventoryUI.SetFocusToEquipmentSlot(equippedSlot);
                }
            }
        }
        
        /// <summary>
        /// 아이템 해제 (외부에서 호출 가능)
        /// </summary>
        public void UnequipItem(BladeAction.Item.Item itemData)
        {
            if (currentItem == null || !currentItem.isEquipped)
            {
                Debug.LogWarning($"[ItemDetailPanel] 장착되지 않은 아이템을 해제하려고 시도함");
                return;
            }
            
            // 현재 아이템이 장착된 슬롯 찾기
            var equippedSlot = inventory.FindEquippedSlot(currentItem.itemKey);
            if (equippedSlot == null)
            {
                Debug.LogWarning($"[ItemDetailPanel] 아이템 '{currentItem.itemKey}'가 장착된 슬롯을 찾을 수 없습니다");
                return;
            }
            
            // 인벤토리에 해제 요청 (슬롯 인스턴스 전달)
            bool success = inventory.UnequipItem(equippedSlot);
            
            if (enableDebugLog)
                Debug.Log($"[ItemDetailPanel] 아이템 해제: {currentItem.itemKey} from {equippedSlot.slotName} ({success})");
            
            if (success)
            {
                // UI 갱신 (ItemEvents에서 자동으로 갱신됨)
                ShowItem(currentItem); // 버튼 텍스트 업데이트
                
                // 유파 슬롯 해제인 경우 유파 UI 선택 해제
                if (equippedSlot.slotType == EquipmentSlotType.SwordArtStyle && inventoryUI != null)
                {
                    var styleUI = inventoryUI.GetComponentInChildren<EquippedSwordArtStyleUI>();
                    if (styleUI != null)
                    {
                        styleUI.ClearSelection();
                    }
                }
                
                // 포커스를 해제된 아이템으로 이동
                if (inventoryUI != null)
                {
                    inventoryUI.SetFocusToItem(currentItem.itemKey);
                }
            }
        }
        
        /// <summary>
        /// 아이템 타입에 따른 슬롯 타입 반환
        /// </summary>
        private EquipmentSlotType GetSlotTypeForItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Weapon:
                    return EquipmentSlotType.Weapon;
                case ItemType.Armor:
                    return EquipmentSlotType.Armor;
                case ItemType.Accessory:
                    return EquipmentSlotType.Accessory;
                case ItemType.SwordArtStyle:
                    return EquipmentSlotType.SwordArtStyle;
                default:
                    return EquipmentSlotType.None;
            }
        }
        
        /// <summary>
        /// 사용 버튼 클릭
        /// </summary>
        private void OnUseButtonClicked()
        {
            if (currentItem == null)
                return;
            
            // TODO: 소모품 사용 로직 구현
            if (enableDebugLog)
                Debug.Log($"[ItemDetailPanel] 아이템 사용: {currentItem.itemKey} (미구현)");
        }
        
        /// <summary>
        /// 버리기 버튼 클릭
        /// </summary>
        private void OnDropButtonClicked()
        {
            if (currentItem == null || inventory == null)
                return;
            
            if (currentItem.isLocked)
            {
                if (enableDebugLog)
                    Debug.Log($"[ItemDetailPanel] 잠긴 아이템은 버릴 수 없습니다: {currentItem.itemKey}");
                return;
            }
            
            // TODO: 확인 팝업 추가
            bool success = inventory.RemoveItem(currentItem.itemKey, currentItem.quantity);
            
            if (enableDebugLog)
                Debug.Log($"[ItemDetailPanel] 아이템 버리기: {currentItem.itemKey} ({success})");
            
            if (success)
            {
                Clear();
            }
        }
        
        /// <summary>
        /// 토글 버튼 클릭
        /// </summary>
        private void OnToggleButtonClicked()
        {
            showDescription = !showDescription;
            UpdateToggleDisplay();
            
            if (enableDebugLog)
                Debug.Log($"[ItemDetailPanel] 토글: {(showDescription ? "설명" : "스탯")}");
        }
        
        /// <summary>
        /// 토글 표시 업데이트
        /// </summary>
        private void UpdateToggleDisplay()
        {
            // 상위 컨테이너 오브젝트 기준으로 가시성 전환
            if (descriptionContainer != null)
                descriptionContainer.SetActive(showDescription);
            if (statsContainer != null)
                statsContainer.SetActive(!showDescription);
            
            if (toggleButtonText != null)
            {
                toggleButtonText.text = showDescription ? "스탯 보기" : "설명 보기";
            }
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Print Debug Info")]
        private void PrintDebugInfo()
        {
            Debug.Log("[ItemDetailPanel] 디버그 정보:");
            Debug.Log($"  - Current Item: {(currentItem != null ? currentItem.ToString() : "null")}");
            Debug.Log($"  - Inventory: {(inventory != null ? inventory.inventoryName : "null")}");
        }
        
        #endregion

        #region 내부 유틸리티(동적 스탯)

        private string FormatSigned(float value, int decimals)
        {
            // +기호 포함, 소수점 자릿수 선택 (F0 / F1 등)
            string format = decimals <= 0 ? "+0" : "+0.".PadRight(3 + decimals, '0');
            // 음수도 동일 포맷 사용 (표기는 자동 부호)
            return value.ToString(format);
        }

        private string FormatSignedPercent(float ratio)
        {
            // 0.0~1.0f → 0~100, 정수 표기(F0), 부호 포함
            float percent = ratio * 100f;
            return percent.ToString("+0;−0"); // 양수 +0, 음수는 유니코드 마이너스 기호 사용
        }

        private void EnsureStatLineCount(int requiredCount)
        {
            if (statsContainer == null || statInfoItemPrefab == null)
                return;

            // 부족하면 생성
            while (spawnedStatLines.Count < requiredCount)
            {
                var go = Instantiate(statInfoItemPrefab, statsContainer.transform);
                var text = go.GetComponentInChildren<TextMeshProUGUI>();
                if (text == null)
                {
                    // 프리팹 구조가 TextMeshProUGUI 하나를 포함한다는 전제
                    text = go.AddComponent<TextMeshProUGUI>();
                }
                go.SetActive(false);
                spawnedStatLines.Add(text);
            }

            // 남는 라인은 비활성화
            for (int i = requiredCount; i < spawnedStatLines.Count; i++)
            {
                if (spawnedStatLines[i] != null)
                    spawnedStatLines[i].gameObject.SetActive(false);
            }
        }

        private void HideAllStatLines()
        {
            foreach (var line in spawnedStatLines)
            {
                if (line != null)
                {
                    line.text = string.Empty;
                    line.gameObject.SetActive(false);
                }
            }
        }

        #endregion
    }
}

