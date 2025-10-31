using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace BladeAction.UI
{
    /// <summary>
    /// 검술 장착 UI 메인 패널 컨트롤러
    /// Character의 습득/장착 검술을 표시하고 관리합니다.
    /// </summary>
    public class ActionCommandEquipUI : MonoBehaviour
    {
        [Header("Character 참조 (런타임 전용)")]
        [Tooltip("표시할 Character - 이 Character의 검술을 표시합니다")]
        private Character targetCharacter;
        
        [Header("UI 컨테이너 참조")]
        [Tooltip("메인 패널 GameObject")]
        [SerializeField] private GameObject panel;
        
        [Tooltip("검술 그리드 컨테이너 (ActionCommandSlotUI들이 생성될 부모)")]
        [SerializeField] private Transform actionCommandGridContainer;
        
        [Tooltip("검술 그리드가 들어있는 ScrollRect")]
        [SerializeField] private ScrollRect actionScrollRect;
        
        [Tooltip("장착된 검술 슬롯 컨테이너 (4개 슬롯)")]
        [SerializeField] private Transform equippedActionSlotsContainer;
        
        [Header("Prefab 참조")]
        [Tooltip("검술 슬롯 프리팹")]
        [SerializeField] private GameObject actionCommandSlotPrefab;
        
        [Header("패널 참조")]
        [Tooltip("검술 상세 정보 패널")]
        [SerializeField] private ActionCommandDetailPanel detailPanel;
        
        [Tooltip("장착된 유파 표시 패널")]
        [SerializeField] private EquippedSwordArtStyleUI styleUI;
        
        [Header("탭 버튼")]
        [Tooltip("습득 검술 표시 버튼")]
        [SerializeField] private Button acquiredActionButton;
        
        [Tooltip("유파 검술 표시 버튼")]
        [SerializeField] private Button swordArtActionButton;
        
        [Header("UI 설정")]
        [Tooltip("자동으로 Character 연결")]
        [SerializeField] private bool autoConnect = true;
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        // UI 슬롯 리스트
        private List<ActionCommandSlotUI> actionSlots = new List<ActionCommandSlotUI>();
        private List<ActionCommandSlotUI> equippedSlots = new List<ActionCommandSlotUI>();
        
        // 현재 선택된 슬롯
        private ActionCommandSlotUI selectedSlot;
        
        // 그리드 표시 모드 제거 (통합 리스트로 변경)
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // Canvas는 MainMenuManager에서 활성화됨
            // 컴포넌트 유효성 검증
            ValidateComponents();
        }
        
        private void Start()
        {
            // 지연 초기화 (CharacterManager보다 늦게 실행될 수 있으므로)
            if (autoConnect)
            {
                StartCoroutine(DelayedAutoConnect());
            }
        }
        
        private void OnEnable()
        {
            // GameObject가 활성화될 때 Character 연결 및 UI 갱신
            // Character가 아직 연결되지 않았으면 자동 연결 시도
            if (targetCharacter == null && autoConnect)
            {
                // CharacterManager 확인 및 연결
                if (CharacterManager.Instance != null && CharacterManager.Instance.PlayerCharacter != null)
                {
                    ConnectToCharacter(CharacterManager.Instance.PlayerCharacter);
                }
            }
            
            // UI 갱신
            if (targetCharacter != null)
            {
                RefreshUI();
                Log("ActionCommandEquipUI 활성화 - UI 갱신");
            }
        }
        
        private void OnDisable()
        {
            // GameObject가 비활성화될 때 정리 작업
            // 선택 상태 초기화
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
                selectedSlot = null;
            }
            
            // 상세 패널 닫기
            if (detailPanel != null)
            {
                detailPanel.Hide();
            }
            
            Log("ActionCommandEquipUI 비활성화 - 상태 정리");
        }
        
        private void OnDestroy()
        {
            // Character 이벤트 구독 해제
            UnsubscribeFromCharacterEvents();
        }
        
        #endregion
        
        #region 초기화 및 연결 관리
        
        /// <summary>
        /// Character 연결 상태 확인 및 자동 재연결
        /// </summary>
        /// <returns>Character가 유효하면 true, 아니면 false</returns>
        private bool EnsureCharacterConnection()
        {
            // 이미 연결되어 있으면 OK
            if (targetCharacter != null)
                return true;
            
            // 연결이 끊어졌으면 자동 재연결 시도
            if (enableDebugLog)
                Debug.LogWarning("[ActionCommandEquipUI] Character 연결이 끊어졌습니다. 자동 재연결 시도...");
            
            if (autoConnect && CharacterManager.Instance != null && CharacterManager.Instance.PlayerCharacter != null)
            {
                ConnectToCharacter(CharacterManager.Instance.PlayerCharacter);
            }
            
            // 재연결 결과 확인
            if (targetCharacter != null)
            {
                if (enableDebugLog)
                    Log("Character 자동 재연결 성공");
                return true;
            }
            
            // 재연결 실패
            Debug.LogError("[ActionCommandEquipUI] Character 연결 실패! UI를 표시할 수 없습니다.");
            return false;
        }
        
        #endregion
        
        #region 초기화
        
        /// <summary>
        /// 컴포넌트 유효성 검증
        /// </summary>
        private void ValidateComponents()
        {
            if (panel == null)
                Debug.LogWarning("[ActionCommandEquipUI] panel이 설정되지 않았습니다!");
            
            if (actionCommandGridContainer == null)
                Debug.LogWarning("[ActionCommandEquipUI] actionCommandGridContainer가 설정되지 않았습니다!");
            
            if (actionCommandSlotPrefab == null)
                Debug.LogError("[ActionCommandEquipUI] actionCommandSlotPrefab이 설정되지 않았습니다!");
            
            if (detailPanel == null)
                Debug.LogWarning("[ActionCommandEquipUI] detailPanel이 설정되지 않았습니다!");
        }
        
        /// <summary>
        /// 지연 자동 연결 (CharacterManager 초기화 대기)
        /// </summary>
        private IEnumerator DelayedAutoConnect()
        {
            // CharacterManager가 초기화될 때까지 대기
            yield return new WaitForSeconds(0.5f);
            
            // Player Character 자동 연결
            if (CharacterManager.Instance != null && CharacterManager.Instance.PlayerCharacter != null)
            {
                ConnectToCharacter(CharacterManager.Instance.PlayerCharacter);
                Log("Player Character 자동 연결 완료");
            }
            else
            {
                Debug.LogWarning("[ActionCommandEquipUI] CharacterManager 또는 PlayerCharacter를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// Character 연결
        /// </summary>
        public void ConnectToCharacter(Character character)
        {
            if (character == null)
            {
                Debug.LogError("[ActionCommandEquipUI] Character가 null입니다!");
                return;
            }
            
            // 이전 Character 구독 해제
            UnsubscribeFromCharacterEvents();
            
            // 새 Character 설정
            targetCharacter = character;
            
            // Character 이벤트 구독
            SubscribeToCharacterEvents();
            
            // UI 초기화
            InitializeUI();
            
            Log($"Character 연결 완료: {character.Name}");
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            if (targetCharacter == null) return;
            
            // 탭 모드 제거 (통합 리스트)
            
            // 장착 슬롯 초기화
            RefreshEquippedSlots();
            
            // 검술 그리드 초기화
            RefreshActionGrid();
            
            // 유파 UI 초기화
            if (styleUI != null && targetCharacter.Inventory != null)
            {
                styleUI.Initialize(targetCharacter.Inventory);
            }
            
            // 상세 패널 초기화 (숨김)
            if (detailPanel != null)
            {
                detailPanel.Hide();
            }
        }
        
        #endregion
        
        #region 이벤트 구독
        
        /// <summary>
        /// Character 이벤트 구독
        /// </summary>
        private void SubscribeToCharacterEvents()
        {
            if (targetCharacter == null) return;
            
            // TODO: Character의 검술 관련 이벤트 구독
            // targetCharacter.OnActionAcquired += RefreshActionGrid;
            // targetCharacter.OnActionEquipped += RefreshEquippedSlots;
            // targetCharacter.OnActionUnequipped += RefreshEquippedSlots;
        }
        
        /// <summary>
        /// Character 이벤트 구독 해제
        /// </summary>
        private void UnsubscribeFromCharacterEvents()
        {
            if (targetCharacter == null) return;
            
            // TODO: Character의 검술 관련 이벤트 구독 해제
            // targetCharacter.OnActionAcquired -= RefreshActionGrid;
            // targetCharacter.OnActionEquipped -= RefreshEquippedSlots;
            // targetCharacter.OnActionUnequipped -= RefreshEquippedSlots;
        }
        
        #endregion
        
        #region UI 갱신
        
        /// <summary>
        /// 검술 그리드 갱신 (통합 리스트: 유파 우선 정렬)
        /// </summary>
        private void RefreshActionGrid()
        {
            // Character 연결 상태 재확인
            if (!EnsureCharacterConnection() || actionCommandGridContainer == null) 
                return;
            
            // 기존 슬롯 제거
            ClearActionSlots();
            
            var acquiredActions = targetCharacter.GetAcquiredActions();
            var styleActions = targetCharacter.GetStyleActions();
            
            // 통합 리스트 생성
            List<ActionCommandData> actionsToDisplay = new List<ActionCommandData>();
            
            // 1. 유파 전용 검술 먼저 추가 (습득과 중복 아닌 것만)
            if (styleActions != null)
            {
                foreach (var styleAction in styleActions)
                {
                    bool isDuplicate = acquiredActions != null && 
                        acquiredActions.Any(a => a.commandName == styleAction.commandName);
                    
                    if (!isDuplicate)
                    {
                        actionsToDisplay.Add(styleAction);
                        Log($"유파 전용 검술 추가: {styleAction.commandName}");
                    }
                }
            }
            
            // 2. 습득 검술 추가 (★ 표시 대상 확인)
            if (acquiredActions != null)
            {
                actionsToDisplay.AddRange(acquiredActions);
            }
            
            if (actionsToDisplay.Count == 0)
            {
                Log($"표시할 검술이 없습니다.");
                return;
            }
            
            // 각 검술에 대해 슬롯 생성
            foreach (var action in actionsToDisplay)
            {
                if (action == null) continue;
                
                // 유파 전용 검술인지 확인
                bool isStyleOnly = styleActions != null && styleActions.Contains(action) &&
                    (acquiredActions == null || !acquiredActions.Any(a => a.commandName == action.commandName));
                
                // 습득 검술이 유파로 강화되었는지 확인
                bool isEnhancedByStyle = acquiredActions != null && acquiredActions.Contains(action) &&
                    styleActions != null && styleActions.Any(s => s.commandName == action.commandName);
                
                CreateActionSlot(action, isStyleOnly, isEnhancedByStyle);
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
            
            // 기존 슬롯 제거
            ClearEquippedSlots();
            
            // 4개 슬롯 생성
            for (int i = 0; i < 4; i++)
            {
                var action = targetCharacter.GetEquippedAction(i);
                CreateEquippedSlot(i, action);
            }
            
            Log($"장착 슬롯 갱신 완료: {equippedSlots.Count}개");
        }
        
        #endregion
        
        #region 슬롯 생성/제거
        
        /// <summary>
        /// 검술 슬롯 생성 (그리드용)
        /// </summary>
        private void CreateActionSlot(ActionCommandData action, bool isStyleAction = false, bool isEnhancedByStyle = false)
        {
            if (actionCommandSlotPrefab == null || actionCommandGridContainer == null) return;
            
            GameObject slotObj = Instantiate(actionCommandSlotPrefab, actionCommandGridContainer);
            ActionCommandSlotUI slot = slotObj.GetComponent<ActionCommandSlotUI>();
            
            if (slot != null)
            {
                slot.Initialize(action, this, -1, false, isStyleAction, isEnhanced: isEnhancedByStyle);
                actionSlots.Add(slot);
            }
            else
            {
                Debug.LogError("[ActionCommandEquipUI] ActionCommandSlotUI 컴포넌트를 찾을 수 없습니다!");
                Destroy(slotObj);
            }
        }
        
        /// <summary>
        /// 장착 슬롯 생성
        /// </summary>
        private void CreateEquippedSlot(int slotIndex, ActionCommandData action)
        {
            if (actionCommandSlotPrefab == null || equippedActionSlotsContainer == null) return;
            
            GameObject slotObj = Instantiate(actionCommandSlotPrefab, equippedActionSlotsContainer);
            ActionCommandSlotUI slot = slotObj.GetComponent<ActionCommandSlotUI>();
            
            if (slot != null)
            {
                // 중복 검술은 습득 우선
                bool isStyleAction = IsActionFromStyle(action);
                
                // 장착 슬롯에서도 강화 표시 (★ 표시)
                bool isEnhancedByStyle = false;
                if (action != null)
                {
                    var acquiredActions = targetCharacter.GetAcquiredActions();
                    var styleActions = targetCharacter.GetStyleActions();
                    
                    // 습득 검술이 유파로 강화되었는지 확인
                    isEnhancedByStyle = acquiredActions != null && acquiredActions.Contains(action) &&
                        styleActions != null && styleActions.Any(s => s.commandName == action.commandName);
                }
                
                slot.Initialize(action, this, slotIndex, true, isStyleAction, isEnhanced: isEnhancedByStyle);
                equippedSlots.Add(slot);
            }
            else
            {
                Debug.LogError("[ActionCommandEquipUI] ActionCommandSlotUI 컴포넌트를 찾을 수 없습니다!");
                Destroy(slotObj);
            }
        }
        
        /// <summary>
        /// 검술이 유파 검술인지 확인
        /// 중요: 습득 검술과 중복되는 경우 false 반환 (습득 우선)
        /// </summary>
        private bool IsActionFromStyle(ActionCommandData action)
        {
            if (action == null || targetCharacter == null)
                return false;
            
            var styleActions = targetCharacter.GetStyleActions();
            var acquiredActions = targetCharacter.GetAcquiredActions();
            
            // 유파 검술에 있는지 확인
            bool isInStyle = styleActions != null && styleActions.Contains(action);
            if (!isInStyle)
                return false;
            
            // 습득 검술에도 있는지 확인 (중복 체크)
            bool isInAcquired = acquiredActions != null && 
                acquiredActions.Any(a => a.commandName == action.commandName);
            
            // 중복이면 습득 우선 (false 반환)
            if (isInAcquired)
            {
                Log($"'{action.commandName}'은 습득/유파 중복 → 습득 취급");
                return false;
            }
            
            return true; // 유파 전용 검술
        }
        
        /// <summary>
        /// 모든 검술 슬롯 제거
        /// </summary>
        private void ClearActionSlots()
        {
            foreach (var slot in actionSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            actionSlots.Clear();
        }
        
        /// <summary>
        /// 모든 장착 슬롯 제거
        /// </summary>
        private void ClearEquippedSlots()
        {
            foreach (var slot in equippedSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            equippedSlots.Clear();
        }
        
        #endregion
        
        #region 슬롯 상호작용
        
        /// <summary>
        /// 슬롯 선택 처리 (토글 모드)
        /// </summary>
        public void OnSlotSelected(ActionCommandSlotUI slot)
        {
            if (slot == null) return;
            
            // 같은 슬롯 재클릭 시 토글 처리
            bool alreadySelected = (selectedSlot == slot);
            
            if (alreadySelected)
            {
                // 선택 해제
                selectedSlot.SetSelected(false);
                selectedSlot = null;
                
                // 상세 패널 숨기기
                if (detailPanel != null)
                {
                    detailPanel.Hide();
                }
                
                Log($"검술 선택 해제 (토글)");
                return;
            }
            
            // 이전 선택 해제
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }
            
            // 새 슬롯 선택
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
            
            // 상세 패널 업데이트
            if (detailPanel != null && slot.ActionData != null)
            {
                detailPanel.Show(slot.ActionData, targetCharacter);
            }
            
            Log($"검술 선택: {slot.ActionData?.commandName}");
        }
        
        /// <summary>
        /// 검술 장착 처리
        /// </summary>
        public void OnEquipAction(ActionCommandData action, int slotIndex = -1)
        {
            if (targetCharacter == null || action == null) return;
            
            // 비어있는 슬롯 찾기 (slotIndex가 지정되지 않은 경우)
            if (slotIndex < 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (targetCharacter.GetEquippedAction(i) == null)
                    {
                        slotIndex = i;
                        break;
                    }
                }
                
                // 모든 슬롯이 꽉 찼으면 4번 슬롯에 자동 장착 (교체)
                if (slotIndex < 0)
                {
                    slotIndex = 3; // 4번 슬롯 (인덱스 3)
                    Log($"모든 슬롯이 꽉 참 → 4번 슬롯에 자동 장착 (교체)");
                }
            }
            
            if (slotIndex < 0 || slotIndex >= 4)
            {
                Debug.LogWarning("[ActionCommandEquipUI] 잘못된 슬롯 인덱스입니다!");
                return;
            }
            
            // 장착
            targetCharacter.EquipAction(action, slotIndex);
            
            // UI 갱신
            RefreshEquippedSlots();
            
            // 포커스를 장착된 슬롯으로 이동
            SetFocusToEquippedSlot(slotIndex);
            
            Log($"검술 장착: {action.commandName} → 슬롯 {slotIndex}");
        }
        
        /// <summary>
        /// 검술 해제 처리
        /// </summary>
        public void OnUnequipAction(int slotIndex)
        {
            if (targetCharacter == null) return;
            
            var action = targetCharacter.GetEquippedAction(slotIndex);
            if (action == null)
            {
                Debug.LogWarning($"[ActionCommandEquipUI] 슬롯 {slotIndex}이(가) 비어있습니다!");
                return;
            }
            
            // 해제
            targetCharacter.UnequipAction(slotIndex);
            
            // UI 갱신
            RefreshEquippedSlots();
            
            // 포커스를 해제된 검술로 이동
            SetFocusToAction(action);
            
            Log($"검술 해제: {action.commandName} (슬롯 {slotIndex})");
        }
        
        #endregion
        
        #region UI 갱신
        
        /// <summary>
        /// 전체 UI 갱신
        /// </summary>
        public void RefreshUI()
        {
            // Character 연결 상태 확인
            if (!EnsureCharacterConnection())
            {
                Debug.LogWarning("[ActionCommandEquipUI] RefreshUI: Character가 연결되지 않아 UI를 갱신할 수 없습니다.");
                return;
            }
            
            RefreshActionGrid();
            RefreshEquippedSlots();
            
            if (styleUI != null && targetCharacter != null && targetCharacter.Inventory != null)
            {
                styleUI.Initialize(targetCharacter.Inventory);
            }
        }
        
        #endregion
        
        #region 포커스 관리
        
        /// <summary>
        /// 현재 그리드에서 검술 슬롯 찾기
        /// 중복 검술 대응: commandName으로 비교
        /// </summary>
        private ActionCommandSlotUI FindActionSlotInCurrentGrid(ActionCommandData action)
        {
            if (action == null || actionSlots == null || actionSlots.Count == 0)
                return null;
            
            foreach (var slot in actionSlots)
            {
                if (slot != null && slot.ActionData != null)
                {
                    // 같은 검술인지 확인 (인스턴스가 다를 수 있으므로 commandName으로 비교)
                    if (slot.ActionData.commandName == action.commandName)
                    {
                        return slot;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 특정 장착 슬롯으로 포커스 이동
        /// </summary>
        /// <param name="slotIndex">포커스를 이동할 슬롯 인덱스 (0~3)</param>
        public void SetFocusToEquippedSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedSlots.Count)
                return;
            
            var targetSlot = equippedSlots[slotIndex];
            if (targetSlot == null)
                return;
            
            // 기존 선택 해제
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }
            
            // 새 슬롯 선택
            selectedSlot = targetSlot;
            selectedSlot.SetSelected(true);
            
            // Unity EventSystem으로 선택 (파란색 하이라이트)
            var selectable = targetSlot.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable != null)
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(targetSlot.gameObject);
            }
            
            // 상세 패널 업데이트
            if (detailPanel != null && targetSlot.ActionData != null)
            {
                detailPanel.Show(targetSlot.ActionData, targetCharacter);
            }
            
            Log($"포커스 이동: 장착 슬롯 {slotIndex} (파란색 하이라이트)");
        }
        
        /// <summary>
        /// 특정 검술로 포커스 이동 및 스크롤 (탭 자동 전환 지원)
        /// </summary>
        /// <param name="action">포커스를 이동할 검술</param>
        public void SetFocusToAction(ActionCommandData action)
        {
            if (action == null)
                return;
            
            // 현재 그리드에서 검술 찾기
            ActionCommandSlotUI targetSlot = FindActionSlotInCurrentGrid(action);
            
            // 탭 전환 로직 제거 (통합 리스트)
            
            if (targetSlot != null)
            {
                // 기존 선택 해제
                if (selectedSlot != null)
                {
                    selectedSlot.SetSelected(false);
                }
                
                // 새 슬롯 선택
                selectedSlot = targetSlot;
                selectedSlot.SetSelected(true);
                
                // Unity EventSystem으로 선택 (파란색 하이라이트)
                var selectable = targetSlot.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null)
                {
                    UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(targetSlot.gameObject);
                }
                
                // 상세 패널 업데이트
                if (detailPanel != null)
                {
                    detailPanel.Show(action, targetCharacter);
                }
                
                // 자동 스크롤 (InventoryUI와 유사한 로직)
                if (actionScrollRect != null)
                {
                    StartCoroutine(ScrollToActionIfOutOfViewDelayed(targetSlot));
                }
                
                Log($"포커스 이동: {action.commandName} (파란색 하이라이트)");
            }
        }
        
        // DelayedFocusAfterTabSwitch 제거 (통합 리스트에서는 탭 전환 없음)
        
        /// <summary>
        /// 검술 슬롯으로 자동 스크롤 (뷰포트 밖일 때만)
        /// </summary>
        private System.Collections.IEnumerator ScrollToActionIfOutOfViewDelayed(ActionCommandSlotUI targetSlot)
        {
            // null 체크 (탭 전환 시 슬롯이 파괴될 수 있음)
            if (actionScrollRect == null || targetSlot == null)
                yield break;
            
            // GameObject가 파괴되었는지 확인
            if (targetSlot.gameObject == null)
            {
                Log("스크롤 대상 슬롯이 파괴되어 스크롤 취소");
                yield break;
            }
            
            var content = actionScrollRect.content;
            var viewport = actionScrollRect.viewport != null ? actionScrollRect.viewport : (RectTransform)actionScrollRect.transform;
            var target = targetSlot.GetComponent<RectTransform>();
            
            if (content == null || viewport == null || target == null)
                yield break;
            
            // 레이아웃 업데이트 대기
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            
            // 파괴 여부 재확인
            if (targetSlot == null || targetSlot.gameObject == null)
            {
                Log("스크롤 중 슬롯이 파괴됨");
                yield break;
            }
            
            // 뷰포트 및 타겟 위치 계산 (InventoryUI와 동일한 로직)
            Vector3[] viewWC = new Vector3[4];
            Vector3[] targetWC = new Vector3[4];
            viewport.GetWorldCorners(viewWC);
            target.GetWorldCorners(targetWC);
            
            Vector2 viewTopLocal, viewBottomLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, viewWC[1]),
                null,
                out viewTopLocal
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, viewWC[0]),
                null,
                out viewBottomLocal
            );
            
            Vector2 targetTopLocal, targetBottomLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, targetWC[1]),
                null,
                out targetTopLocal
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, targetWC[0]),
                null,
                out targetBottomLocal
            );
            
            const float tol = 2f;
            bool above = targetTopLocal.y > viewTopLocal.y - tol;
            bool below = targetBottomLocal.y < viewBottomLocal.y + tol;
            
            if (!above && !below)
                yield break;
            
            float viewportHeight = viewport.rect.height;
            float contentHeight = content.rect.height;
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);
            Vector2 anchored = content.anchoredPosition;
            
            if (above)
            {
                float delta = targetTopLocal.y - viewTopLocal.y;
                anchored.y = Mathf.Clamp(anchored.y - delta, 0f, maxY);
            }
            else // below
            {
                float delta = viewBottomLocal.y - targetBottomLocal.y;
                anchored.y = Mathf.Clamp(anchored.y + delta, 0f, maxY);
            }
            
            content.anchoredPosition = anchored;
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ActionCommandEquipUI] {message}");
            }
        }
        
        #endregion
    }
}

