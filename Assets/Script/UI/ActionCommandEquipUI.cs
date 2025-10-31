using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DG.Tweening;

namespace BladeAction.UI
{
    /// <summary>
    /// 그리드 및 장착 슬롯 표시용 검술 정보
    /// </summary>
    public struct DisplayActionInfo
    {
        public ActionCommandData action;        // 검술 데이터 (Key 포함)
        public bool isAcquired;                 // 습득 검술 여부
        public bool isSwordArtStyleGetted;      // 유파 검술 여부
        
        public DisplayActionInfo(ActionCommandData action, bool isAcquired, bool isSwordArtStyleGetted)
        {
            this.action = action;
            this.isAcquired = isAcquired;
            this.isSwordArtStyleGetted = isSwordArtStyleGetted;
        }
        
        /// <summary>
        /// 강화된 검술인지 (습득 + 유파 모두 보유)
        /// </summary>
        public bool IsEnhanced => isAcquired && isSwordArtStyleGetted;
        
        /// <summary>
        /// 카테고리 텍스트 반환
        /// </summary>
        public string GetCategoryText()
        {
            if (IsEnhanced) return "강화";
            if (isAcquired) return "습득";
            if (isSwordArtStyleGetted) return "유파";
            return "";
        }
    }
    
    /// <summary>
    /// 검술(Action Command) 장착 UI 관리
    /// - 습득한 검술과 유파를 통해 획득한 검술을 선택하여 4개 슬롯에 장착
    /// - 전투 시 사용할 검술 구성
    /// </summary>
    public class ActionCommandEquipUI : MonoBehaviour
    {
        [Header("▣ 검술 그리드 (선택 가능한 검술 목록)")]
        [Tooltip("검술 그리드가 표시되는 패널 (보통 GridPanel 또는 ActionGrid)")]
        [SerializeField] private GameObject actionGridPanel;
        
        [Tooltip("검술 슬롯들이 동적으로 생성될 부모 Transform (보통 Content)")]
        [SerializeField] private Transform actionSlotsContainer;
        
        [Tooltip("검술 슬롯 프리팹 (ActionCommandSlot.prefab)")]
        [SerializeField] private GameObject actionSlotPrefab;
        
        [Tooltip("검술 그리드의 스크롤 영역 (ScrollRect 컴포넌트)")]
        [SerializeField] private ScrollRect actionScrollRect;
        
        [Tooltip("그리드 전체 영역 드롭 하이라이트용 Frame Image (선택사항)")]
        [SerializeField] private Image gridAreaFrameImage;
        
        [Tooltip("그리드 영역 드롭존 오브젝트 (GridAreaDropZone 컴포넌트 포함, 드래그 시에만 활성화)")]
        [SerializeField] private GameObject gridAreaDropZoneObject;
        
        [Header("▣ 장착 슬롯 (전투에서 사용할 검술 4개)")]
        [Tooltip("장착 슬롯들이 동적으로 생성될 부모 Transform (보통 EquippedSlotsContainer)")]
        [SerializeField] private Transform equippedActionSlotsContainer;
        
        [Tooltip("장착 슬롯 영역 드롭 하이라이트용 Frame Image (EquippedActionAreaFrameImage)")]
        [SerializeField] private Image equippedAreaFrameImage;
        
        [Header("▣ 상세 정보 패널")]
        [Tooltip("선택한 검술의 상세 정보를 표시하는 패널 (ActionCommandDetailPanel 컴포넌트)")]
        [SerializeField] private ActionCommandDetailPanel detailPanel;
        
        [Header("▣ 유파 슬롯 (장착된 유파 표시)")]
        [Tooltip("착용 중인 검술 유파를 표시하는 슬롯 (EquippedSwordArtStyleUI 컴포넌트)")]
        [SerializeField] private EquippedSwordArtStyleUI equippedSwordArtStyleUI;
        
        [Header("▣ 디버그")]
        [Tooltip("Console에 상세 로그를 출력할지 여부")]
        [SerializeField] private bool enableDebugLog = true;
        
        private List<ActionCommandSlotUI> actionSlots = new List<ActionCommandSlotUI>();
        private List<ActionCommandSlotUI> equippedSlots = new List<ActionCommandSlotUI>();
        private ActionCommandSlotUI selectedSlot;
        
        private Character targetCharacter;
        
        // 드롭 영역 하이라이트 관리
        private DG.Tweening.Sequence gridAreaBlinkSequence;
        private DG.Tweening.Sequence equippedAreaBlinkSequence;
        private Color originalGridFrameColor;
        private Color originalEquippedFrameColor;
        
        private void Start()
        {
            // 초기 상태: Frame Image들 비활성화 (평소에는 보이지 않음)
            if (equippedAreaFrameImage != null)
            {
                originalEquippedFrameColor = equippedAreaFrameImage.color;
                equippedAreaFrameImage.enabled = false;
            }
            
            if (gridAreaFrameImage != null)
            {
                originalGridFrameColor = gridAreaFrameImage.color;
                gridAreaFrameImage.enabled = false;
            }
            
            // GridAreaDropZone의 Raycast 비활성화 (드래그 시에만 활성화)
            SetGridDropZoneEnabled(false);
        }
        
        /// <summary>
        /// Character 연결
        /// </summary>
        public void ConnectToCharacter(Character character)
        {
            if (character == null)
            {
                Debug.LogWarning("[ActionCommandEquipUI] character가 null입니다.");
                return;
            }
            
            targetCharacter = character;
            
            // Detail Panel은 별도로 Character 연결 불필요 (Show 시에 전달)
            
            Log($"[ActionCommandEquipUI] {character.Name}의 검술 장착 UI에 연결되었습니다.");
            
            RefreshAll();
        }
        
        private void OnEnable()
        {
            // 활성화될 때 UI 갱신
            Log("[ActionCommandEquipUI] OnEnable - UI 갱신 시작");
            
            // Character가 연결되지 않은 경우 재시도
            if (targetCharacter == null)
            {
                AutoConnectToPlayerInventory();
            }
            
            RefreshAll();
        }
        
        private void OnDisable()
        {
            // 선택 상태 초기화
            ClearSelection();
            
            // 상세 패널 숨기기
            if (detailPanel != null)
            {
                detailPanel.gameObject.SetActive(false);
            }
            
            // 유파 슬롯 선택 해제
            if (equippedSwordArtStyleUI != null)
            {
                equippedSwordArtStyleUI.ClearSelection();
            }
            
            // 드래그 하이라이트 정리
            OnDragEnd();
        }
        
        /// <summary>
        /// 플레이어 캐릭터에 자동 연결 시도
        /// </summary>
        private void AutoConnectToPlayerInventory()
        {
            var playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null && playerController.Character != null)
            {
                ConnectToCharacter(playerController.Character);
                Log("[ActionCommandEquipUI] 플레이어 캐릭터에 자동 연결되었습니다.");
            }
            else
            {
                Debug.LogWarning("[ActionCommandEquipUI] 플레이어 컨트롤러를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// Character 연결 상태 확인 및 자동 재연결
        /// </summary>
        /// <returns>Character가 유효하면 true, 아니면 false</returns>
        private bool EnsureCharacterConnection()
        {
            if (targetCharacter == null)
            {
                Log("[ActionCommandEquipUI] Character 연결이 끊어졌습니다. 재연결 시도...");
                AutoConnectToPlayerInventory();
            }
            
            return targetCharacter != null;
        }
        
        /// <summary>
        /// 전체 UI 갱신
        /// </summary>
        public void RefreshAll()
        {
            if (!EnsureCharacterConnection()) return;
            
            Log("[ActionCommandEquipUI] 전체 UI 갱신 시작");
            RefreshUI();
        }
        
        /// <summary>
        /// UI 갱신 (Action Command Grid + Equipped Slots + 유파 슬롯)
        /// </summary>
        public void RefreshUI()
        {
            if (!EnsureCharacterConnection()) return;
            
            RefreshActionGrid();
            RefreshEquippedSlots();
            RefreshSwordArtStyleUI();
        }
        
        /// <summary>
        /// 유파 슬롯 UI 갱신
        /// </summary>
        private void RefreshSwordArtStyleUI()
        {
            if (equippedSwordArtStyleUI != null && targetCharacter != null)
            {
                equippedSwordArtStyleUI.Initialize(targetCharacter.Inventory, null);
                equippedSwordArtStyleUI.Refresh();
                Log("[ActionCommandEquipUI] 유파 슬롯 UI 갱신 완료");
            }
        }
        
        /// <summary>
        /// 검술의 표시 정보 생성 (습득/유파 여부 판정)
        /// </summary>
        private DisplayActionInfo CreateDisplayInfo(ActionCommandData action)
        {
            if (action == null || targetCharacter == null)
            {
                return new DisplayActionInfo(action, false, false);
            }
            
            var database = ActionCommandDatabase.Instance;
            if (database == null)
            {
                Debug.LogError("[ActionCommandEquipUI] ActionCommandDatabase를 찾을 수 없습니다!");
                return new DisplayActionInfo(action, false, false);
            }
            
            string actionKey = database.GetKey(action);
            if (string.IsNullOrEmpty(actionKey))
            {
                Debug.LogWarning($"[ActionCommandEquipUI] '{action.name}' 검술의 Key를 찾을 수 없습니다!");
                return new DisplayActionInfo(action, false, false);
            }
            
            var acquiredActions = targetCharacter.GetAcquiredActions();
            var styleActions = targetCharacter.GetStyleActions();
            
            // Key로 비교
            bool isAcquired = acquiredActions != null && acquiredActions.Any(a => 
            {
                if (a == null) return false;
                string aKey = database.GetKey(a);
                return !string.IsNullOrEmpty(aKey) && aKey == actionKey;
            });
            
            bool isStyle = styleActions != null && styleActions.Any(s => 
            {
                if (s == null) return false;
                string sKey = database.GetKey(s);
                return !string.IsNullOrEmpty(sKey) && sKey == actionKey;
            });
            
            return new DisplayActionInfo(action, isAcquired, isStyle);
        }
        
        /// <summary>
        /// 검술 그리드 갱신 (통합 리스트 방식)
        /// </summary>
        private void RefreshActionGrid()
        {
            // Character 연결 상태 재확인
            if (!EnsureCharacterConnection() || actionSlotsContainer == null) 
                return;
            
            Log("[ActionCommandEquipUI] 검술 그리드 갱신 시작");
            
            // 기존 슬롯 제거
            ClearActionSlots();
            
            var acquiredActions = targetCharacter.GetAcquiredActions();
            var styleActions = targetCharacter.GetStyleActions();
            
            Log($"습득 검술: {acquiredActions?.Count ?? 0}개, 유파 검술: {styleActions?.Count ?? 0}개");
            
            // 장착된 검술 Key 목록 (제외용)
            var database = ActionCommandDatabase.Instance;
            if (database == null)
            {
                Debug.LogError("[ActionCommandEquipUI] ActionCommandDatabase를 찾을 수 없습니다!");
                return;
            }
            
            var equippedKeys = new HashSet<string>();
            for (int i = 0; i < 4; i++)
            {
                var equipped = targetCharacter.GetEquippedAction(i);
                if (equipped != null)
                {
                    string key = database.GetKey(equipped);
                    if (!string.IsNullOrEmpty(key))
                    {
                        equippedKeys.Add(key);
                    }
                }
            }
            
            // 표시용 검술 Dictionary: Key = commandName, Value = DisplayActionInfo
            var displayActions = new Dictionary<string, DisplayActionInfo>();
            
            // 1. 유파 검술을 먼저 추가 (isSwordArtStyleGetted = true)
            if (styleActions != null)
            {
                foreach (var styleAction in styleActions)
                {
                    if (styleAction == null) continue;
                    
                    string key = database.GetKey(styleAction);
                    if (string.IsNullOrEmpty(key)) continue;
                    
                    displayActions[key] = new DisplayActionInfo(
                        action: styleAction,
                        isAcquired: false,
                        isSwordArtStyleGetted: true
                    );
                }
            }
            
            // 2. 습득 검술로 덮어쓰기 (같은 Key면 isAcquired = true, isSwordArtStyleGetted 유지)
            if (acquiredActions != null)
            {
                foreach (var acquiredAction in acquiredActions)
                {
                    if (acquiredAction == null) continue;
                    
                    string key = database.GetKey(acquiredAction);
                    if (string.IsNullOrEmpty(key)) continue;
                    
                    // 이미 유파 검술로 존재하면 isSwordArtStyleGetted 유지
                    bool hasStyle = displayActions.ContainsKey(key);
                    
                    displayActions[key] = new DisplayActionInfo(
                        action: acquiredAction,
                        isAcquired: true,
                        isSwordArtStyleGetted: hasStyle
                    );
                }
            }
            
            // 3. 장착된 검술 제거 (Key로 비교)
            foreach (var equippedKey in equippedKeys)
            {
                displayActions.Remove(equippedKey);
            }
            
            if (displayActions.Count == 0)
            {
                Log($"표시할 검술이 없습니다.");
                return;
            }
            
            // 4. UI 슬롯 생성
            foreach (var displayInfo in displayActions.Values)
            {
                Log($"Grid 슬롯 생성: '{displayInfo.action.commandName}' - 습득={displayInfo.isAcquired}, 유파={displayInfo.isSwordArtStyleGetted}, 강화={displayInfo.IsEnhanced}, 카테고리={displayInfo.GetCategoryText()}");
                
                CreateActionSlot(displayInfo);
            }
            
            Log($"검술 그리드 갱신 완료: {actionSlots.Count}개 (통합 리스트)");
        }
        
        /// <summary>
        /// 장착 슬롯 갱신
        /// </summary>
        private void RefreshEquippedSlots()
        {
            // Character 연결 상태 재확인
            if (!EnsureCharacterConnection() || equippedActionSlotsContainer == null) 
                return;
            
            Log($"[RefreshEquippedSlots] 시작 - 기존 슬롯 개수: {equippedSlots.Count}");
            
            // 기존 슬롯 제거
            ClearEquippedSlots();
            
            Log($"[RefreshEquippedSlots] 기존 슬롯 제거 완료 - 현재 슬롯 개수: {equippedSlots.Count}");
            
            // 4개의 장착 슬롯 생성
            for (int i = 0; i < 4; i++)
            {
                var action = targetCharacter.GetEquippedAction(i);
                CreateEquippedSlot(i, action);
            }
            
            Log($"[RefreshEquippedSlots] 완료 - 최종 슬롯 개수: {equippedSlots.Count}");
        }
        
        /// <summary>
        /// 검술 슬롯 생성 (구조체 버전)
        /// </summary>
        private void CreateActionSlot(DisplayActionInfo displayInfo)
        {
            // 유파 전용 = 유파만 true이고 습득은 false
            bool isStyleOnly = displayInfo.isSwordArtStyleGetted && !displayInfo.isAcquired;
            CreateActionSlot(displayInfo.action, isStyleOnly, displayInfo.IsEnhanced);
        }
        
        /// <summary>
        /// 검술 슬롯 생성 (레거시)
        /// </summary>
        private void CreateActionSlot(ActionCommandData action, bool isStyleAction, bool isEnhanced)
        {
            GameObject slotObj = Instantiate(actionSlotPrefab, actionSlotsContainer);
            ActionCommandSlotUI slotUI = slotObj.GetComponent<ActionCommandSlotUI>();
            
            if (slotUI != null)
            {
                slotUI.Initialize(action, this, -1, false, isStyleAction, isEnhanced);
                actionSlots.Add(slotUI);
            }
        }
        
        /// <summary>
        /// 장착 슬롯 생성
        /// </summary>
        private void CreateEquippedSlot(int slotIndex, ActionCommandData action)
        {
            GameObject slotObj = Instantiate(actionSlotPrefab, equippedActionSlotsContainer);
            ActionCommandSlotUI slot = slotObj.GetComponent<ActionCommandSlotUI>();
            
            if (slot != null)
            {
                if (action != null)
                {
                    // 검술이 있는 경우
                    bool isStyleAction = IsActionFromStyle(action);
                    
                    // 장착 슬롯에서도 표시 정보 생성
                    DisplayActionInfo displayInfo = CreateDisplayInfo(action);
                    
                    Log($"장착 슬롯 {slotIndex} 생성: '{action.commandName}' - 습득={displayInfo.isAcquired}, 유파={displayInfo.isSwordArtStyleGetted}, 강화={displayInfo.IsEnhanced}, 카테고리={displayInfo.GetCategoryText()}");
                    
                    slot.Initialize(action, this, slotIndex, true, displayInfo.isSwordArtStyleGetted && !displayInfo.isAcquired, isEnhanced: displayInfo.IsEnhanced);
                }
                else
                {
                    // 빈 슬롯
                    slot.Initialize(null, this, slotIndex, true, false, false);
                }
                
                equippedSlots.Add(slot);
            }
            else
            {
                Debug.LogError("[ActionCommandEquipUI] ActionCommandSlotUI 컴포넌트를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 특정 검술이 유파 검술인지 확인 (레거시)
        /// </summary>
        private bool IsActionFromStyle(ActionCommandData action)
        {
            if (action == null || targetCharacter == null) return false;
            
            var acquiredActions = targetCharacter.GetAcquiredActions();
            var styleActions = targetCharacter.GetStyleActions();
            
            // 습득 검술과 중복이면 습득 우선 (false 반환)
            bool isInAcquired = acquiredActions != null && acquiredActions.Any(a => a != null && a.commandName == action.commandName);
            if (isInAcquired) return false;
            
            // 유파 검술에만 있으면 true
            return styleActions != null && styleActions.Any(s => s != null && s.commandName == action.commandName);
        }
        
        /// <summary>
        /// 그리드에서 특정 검술 슬롯 찾기 (Key로 비교)
        /// </summary>
        private ActionCommandSlotUI FindActionSlotInCurrentGrid(ActionCommandData action)
        {
            if (action == null) return null;
            
            var database = ActionCommandDatabase.Instance;
            if (database == null) return null;
            
            string actionKey = database.GetKey(action);
            if (string.IsNullOrEmpty(actionKey)) return null;
            
            // Key로 비교 (인스턴스가 다를 수 있으므로)
            return actionSlots.FirstOrDefault(slot => 
            {
                if (slot.ActionData == null) return false;
                string slotKey = database.GetKey(slot.ActionData);
                return !string.IsNullOrEmpty(slotKey) && slotKey == actionKey;
            });
        }
        
        /// <summary>
        /// 슬롯 선택 처리
        /// </summary>
        public void OnSlotSelected(ActionCommandSlotUI slot)
        {
            if (slot == null) return;
            
            // 토글 동작: 이미 선택된 슬롯을 다시 클릭하면 선택 해제
            bool wasAlreadySelected = (selectedSlot == slot);
            
            // 기존 선택 해제
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }
            
            if (wasAlreadySelected)
            {
                // 선택 해제
                selectedSlot = null;
                if (detailPanel != null)
                {
                    detailPanel.Hide();
                }
                return;
            }
            
            // 새로운 슬롯 선택
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
            
            // 상세 정보 표시
            if (detailPanel != null && slot.ActionData != null)
            {
                detailPanel.Show(slot.ActionData, targetCharacter);
            }
        }
        
        /// <summary>
        /// 검술 장착
        /// </summary>
        public void OnEquipAction(ActionCommandData action, int targetSlotIndex)
        {
            if (action == null || targetCharacter == null)
            {
                Debug.LogWarning("[ActionCommandEquipUI] action 또는 targetCharacter가 null입니다.");
                return;
            }
            
            // targetSlotIndex가 -1이면 빈 슬롯 또는 4번째 슬롯 찾기
            if (targetSlotIndex < 0)
            {
                targetSlotIndex = FindEmptySlotIndex();
                
                // 빈 슬롯이 없으면 4번째 슬롯(인덱스 3)에 자동 장착
                if (targetSlotIndex < 0)
                {
                    targetSlotIndex = 3;
                    Log($"[ActionCommandEquipUI] 빈 슬롯이 없어 4번째 슬롯에 자동 장착합니다.");
                }
            }
            
            if (targetCharacter.EquipAction(action, targetSlotIndex))
            {
                Log($"[ActionCommandEquipUI] '{action.commandName}' 검술을 슬롯 {targetSlotIndex}에 장착했습니다.");
                RefreshUI();
            }
        }
        
        /// <summary>
        /// 장착 슬롯 위치 교체 (드래그 앤 드롭용)
        /// </summary>
        public void SwapEquippedSlots(int slotIndex1, int slotIndex2)
        {
            if (targetCharacter == null) return;
            
            if (targetCharacter.SwapEquippedActions(slotIndex1, slotIndex2))
            {
                Log($"[ActionCommandEquipUI] 슬롯 {slotIndex1} ↔ 슬롯 {slotIndex2} 교체 완료");
                RefreshUI();
            }
        }
        
        /// <summary>
        /// 검술 장착 해제
        /// </summary>
        public void OnUnequipAction(int slotIndex)
        {
            if (targetCharacter == null) return;
            
            if (targetCharacter.UnequipAction(slotIndex))
            {
                Log($"[ActionCommandEquipUI] 슬롯 {slotIndex}의 검술을 해제했습니다.");
                RefreshUI();
            }
        }
        
        /// <summary>
        /// 빈 슬롯 인덱스 찾기
        /// </summary>
        private int FindEmptySlotIndex()
        {
            for (int i = 0; i < 4; i++)
            {
                if (targetCharacter.GetEquippedAction(i) == null)
                {
                    return i;
                }
            }
            return -1; // 빈 슬롯 없음
        }
        
        /// <summary>
        /// 선택 해제
        /// </summary>
        public void ClearSelection()
        {
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
                selectedSlot = null;
            }
            
            // 유파 슬롯 선택 해제
            if (equippedSwordArtStyleUI != null)
            {
                equippedSwordArtStyleUI.ClearSelection();
            }
        }
        
        /// <summary>
        /// 검술 슬롯 제거
        /// </summary>
        private void ClearActionSlots()
        {
            foreach (var slot in actionSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            actionSlots.Clear();
        }
        
        /// <summary>
        /// 장착 슬롯 제거
        /// </summary>
        private void ClearEquippedSlots()
        {
            foreach (var slot in equippedSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            equippedSlots.Clear();
        }
        
        /// <summary>
        /// 특정 검술로 포커스 설정
        /// </summary>
        public void SetFocusToAction(ActionCommandData action)
        {
            if (action == null) return;
            
            StartCoroutine(DelayedFocusToAction(action));
        }
        
        private IEnumerator DelayedFocusToAction(ActionCommandData action)
        {
            yield return null; // UI 갱신 대기
            
            var targetSlot = FindActionSlotInCurrentGrid(action);
            if (targetSlot != null)
            {
                // 이전 선택 해제
                if (selectedSlot != null)
                {
                    selectedSlot.SetSelected(false);
                }
                
                // 새로운 슬롯 선택
                selectedSlot = targetSlot;
                selectedSlot.SetSelected(true);
                
                // 스크롤 뷰에서 해당 슬롯이 보이도록 스크롤
                yield return StartCoroutine(ScrollToActionIfOutOfViewDelayed(targetSlot));
            }
        }
        
        /// <summary>
        /// 특정 장착 슬롯으로 포커스 설정
        /// </summary>
        public void SetFocusToEquippedSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedSlots.Count) return;
            
            StartCoroutine(DelayedFocusToEquippedSlot(slotIndex));
        }
        
        private IEnumerator DelayedFocusToEquippedSlot(int slotIndex)
        {
            yield return null; // UI 갱신 대기
            
            if (slotIndex >= 0 && slotIndex < equippedSlots.Count)
            {
                var targetSlot = equippedSlots[slotIndex];
                if (targetSlot != null)
                {
                    // 이전 선택 해제
                    if (selectedSlot != null)
                    {
                        selectedSlot.SetSelected(false);
                    }
                    
                    // 새로운 슬롯 선택
                    selectedSlot = targetSlot;
                    selectedSlot.SetSelected(true);
                }
            }
        }
        
        /// <summary>
        /// 슬롯이 화면 밖에 있으면 스크롤
        /// </summary>
        private IEnumerator ScrollToActionIfOutOfViewDelayed(ActionCommandSlotUI slot)
        {
            yield return null; // 레이아웃 갱신 대기
            
            if (slot == null || actionScrollRect == null) yield break;
            
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            RectTransform contentRect = actionScrollRect.content;
            RectTransform viewportRect = actionScrollRect.viewport;
            
            if (slotRect == null || contentRect == null || viewportRect == null) yield break;
            
            // 슬롯의 로컬 위치 계산
            Vector2 slotLocalPos = (Vector2)contentRect.InverseTransformPoint(slotRect.position);
            Vector2 viewportSize = viewportRect.rect.size;
            Vector2 contentSize = contentRect.rect.size;
            
            // 정규화된 스크롤 위치 계산
            float normalizedPosition = Mathf.Clamp01(
                (slotLocalPos.y + contentSize.y / 2f - viewportSize.y / 2f) / (contentSize.y - viewportSize.y)
            );
            
            actionScrollRect.verticalNormalizedPosition = 1f - normalizedPosition;
        }
        
        #region 드래그 앤 드롭 하이라이트
        
        /// <summary>
        /// 드래그 시작 시 드롭 가능 영역 하이라이트
        /// </summary>
        public void OnDragStart(bool isDraggingFromEquipped)
        {
            if (isDraggingFromEquipped)
            {
                // 장착 슬롯에서 드래그 → 검술 목록 그리드 전체 영역 하이라이트
                HighlightGridArea(true);
            }
            else
            {
                // 검술 목록 그리드에서 드래그 → 장착 슬롯 영역 전체 하이라이트
                HighlightEquippedArea(true);
            }
        }
        
        /// <summary>
        /// 드래그 종료 시 하이라이트 종료
        /// </summary>
        public void OnDragEnd()
        {
            Log("[OnDragEnd] 호출됨 - 하이라이트 종료 시작");
            HighlightGridArea(false);
            HighlightEquippedArea(false);
            Log("[OnDragEnd] 완료");
        }
        
        /// <summary>
        /// 장착 슬롯 영역 전체 하이라이트
        /// </summary>
        private void HighlightEquippedArea(bool highlight)
        {
            if (equippedAreaFrameImage == null) return;
            
            if (highlight)
            {
                // Frame 활성화
                equippedAreaFrameImage.enabled = true;
                
                // Hierarchy 맨 아래로 이동 (가장 위에 렌더링)
                equippedAreaFrameImage.transform.SetAsLastSibling();
                
                // 반짝임 시작 (흰색 → 초록 → 흰색)
                Color visibleGreen = new Color(0f, 1f, 0f, 0.8f); // 불투명 초록
                
                equippedAreaBlinkSequence = DOTween.Sequence();
                equippedAreaBlinkSequence.Append(
                    DOTween.To(() => equippedAreaFrameImage.color, x => equippedAreaFrameImage.color = x, visibleGreen, 0.3f)
                        .SetEase(Ease.InOutQuad)
                );
                equippedAreaBlinkSequence.Append(
                    DOTween.To(() => equippedAreaFrameImage.color, x => equippedAreaFrameImage.color = x, originalEquippedFrameColor, 0.3f)
                        .SetEase(Ease.InOutQuad)
                );
                equippedAreaBlinkSequence.SetLoops(-1, LoopType.Restart);
            }
            else
            {
                // 반짝임 중지
                if (equippedAreaBlinkSequence != null)
                {
                    equippedAreaBlinkSequence.Kill(true);
                    equippedAreaBlinkSequence = null;
                }
                
                // Frame 비활성화
                if (equippedAreaFrameImage != null)
                {
                    equippedAreaFrameImage.color = originalEquippedFrameColor;
                    equippedAreaFrameImage.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// GridAreaDropZone의 드롭 받기 활성화/비활성화
        /// </summary>
        private void SetGridDropZoneEnabled(bool enabled)
        {
            if (gridAreaDropZoneObject == null)
            {
                Debug.LogError("[ActionCommandEquipUI] gridAreaDropZoneObject가 null입니다! Inspector에서 연결하세요.");
                return;
            }
            
            var dropZone = gridAreaDropZoneObject.GetComponent<GridAreaDropZone>();
            if (dropZone == null)
            {
                Debug.LogError($"[ActionCommandEquipUI] {gridAreaDropZoneObject.name}에 GridAreaDropZone 컴포넌트가 없습니다!");
                return;
            }
            
            dropZone.SetDropEnabled(enabled);
            Debug.Log($"[ActionCommandEquipUI] SetGridDropZoneEnabled({enabled}) 호출 완료");
        }
        
        /// <summary>
        /// 그리드 영역 전체 하이라이트 + 드롭존 활성화
        /// </summary>
        private void HighlightGridArea(bool highlight)
        {
            if (highlight)
            {
                // GridAreaDropZone Raycast 활성화 (드롭 받을 수 있도록)
                SetGridDropZoneEnabled(true);
                
                // Frame 활성화 (시각적 표시)
                if (gridAreaFrameImage != null)
                {
                    gridAreaFrameImage.enabled = true;
                    
                    // Hierarchy 맨 아래로 이동 (가장 위에 렌더링)
                    gridAreaFrameImage.transform.SetAsLastSibling();
                    
                    // 반짝임 시작 (흰색 → 초록 → 흰색)
                    Color visibleGreen = new Color(0f, 1f, 0f, 0.8f); // 불투명 초록
                    
                    gridAreaBlinkSequence = DOTween.Sequence();
                    gridAreaBlinkSequence.Append(
                        DOTween.To(() => gridAreaFrameImage.color, x => gridAreaFrameImage.color = x, visibleGreen, 0.3f)
                            .SetEase(Ease.InOutQuad)
                    );
                    gridAreaBlinkSequence.Append(
                        DOTween.To(() => gridAreaFrameImage.color, x => gridAreaFrameImage.color = x, originalGridFrameColor, 0.3f)
                            .SetEase(Ease.InOutQuad)
                    );
                    gridAreaBlinkSequence.SetLoops(-1, LoopType.Restart);
                }
            }
            else
            {
                // GridAreaDropZone Raycast 비활성화
                SetGridDropZoneEnabled(false);
                
                // 반짝임 중지
                if (gridAreaBlinkSequence != null)
                {
                    gridAreaBlinkSequence.Kill(true);
                    gridAreaBlinkSequence = null;
                }
                
                // Frame 비활성화
                if (gridAreaFrameImage != null)
                {
                    gridAreaFrameImage.color = originalGridFrameColor;
                    gridAreaFrameImage.enabled = false;
                }
            }
        }
        
        #endregion
        
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ActionCommandEquipUI] {message}");
            }
        }
    }
}

