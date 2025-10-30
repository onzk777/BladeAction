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
        
        // 그리드 표시 모드
        private enum GridDisplayMode
        {
            AcquiredActions,  // 습득 검술
            SwordArtActions   // 유파 검술
        }
        private GridDisplayMode currentMode = GridDisplayMode.AcquiredActions;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // Canvas는 MainMenuManager에서 활성화됨
            // 컴포넌트 유효성 검증
            ValidateComponents();
        }
        
        private void Start()
        {
            // 탭 버튼 이벤트 연결
            if (acquiredActionButton != null)
                acquiredActionButton.onClick.AddListener(ShowAcquiredActions);
            
            if (swordArtActionButton != null)
                swordArtActionButton.onClick.AddListener(ShowSwordArtActions);
            
            // 패널 초기 상태 (비활성화)
            if (panel != null)
            {
                panel.SetActive(false);
            }
            
            // 지연 초기화 (CharacterManager보다 늦게 실행될 수 있으므로)
            if (autoConnect)
            {
                StartCoroutine(DelayedAutoConnect());
            }
        }
        
        private void OnDestroy()
        {
            // 탭 버튼 이벤트 해제
            if (acquiredActionButton != null)
                acquiredActionButton.onClick.RemoveListener(ShowAcquiredActions);
            
            if (swordArtActionButton != null)
                swordArtActionButton.onClick.RemoveListener(ShowSwordArtActions);
            
            // Character 이벤트 구독 해제
            UnsubscribeFromCharacterEvents();
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
            
            // 초기 탭 모드 설정 (습득 검술)
            currentMode = GridDisplayMode.AcquiredActions;
            UpdateTabButtons();
            
            // 검술 그리드 초기화
            RefreshActionGrid();
            
            // 장착 슬롯 초기화
            RefreshEquippedSlots();
            
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
        
        #region 탭 전환
        
        /// <summary>
        /// 습득 검술 탭으로 전환
        /// </summary>
        public void ShowAcquiredActions()
        {
            currentMode = GridDisplayMode.AcquiredActions;
            UpdateTabButtons();
            RefreshActionGrid();
            
            Log("습득 검술 탭으로 전환");
        }
        
        /// <summary>
        /// 유파 검술 탭으로 전환
        /// </summary>
        public void ShowSwordArtActions()
        {
            currentMode = GridDisplayMode.SwordArtActions;
            UpdateTabButtons();
            RefreshActionGrid();
            
            Log("유파 검술 탭으로 전환");
        }
        
        /// <summary>
        /// 탭 버튼 상태 업데이트
        /// </summary>
        private void UpdateTabButtons()
        {
            // 선택된 탭은 비활성화, 다른 탭은 활성화
            if (acquiredActionButton != null)
            {
                acquiredActionButton.interactable = (currentMode != GridDisplayMode.AcquiredActions);
            }
            
            if (swordArtActionButton != null)
            {
                swordArtActionButton.interactable = (currentMode != GridDisplayMode.SwordArtActions);
            }
        }
        
        #endregion
        
        #region UI 갱신
        
        /// <summary>
        /// 검술 그리드 갱신 (현재 모드에 따라)
        /// </summary>
        private void RefreshActionGrid()
        {
            if (targetCharacter == null || actionCommandGridContainer == null) return;
            
            // 기존 슬롯 제거
            ClearActionSlots();
            
            // 현재 모드에 따라 검술 리스트 가져오기
            List<ActionCommandData> actionsToDisplay = null;
            bool isStyleMode = false;
            
            if (currentMode == GridDisplayMode.AcquiredActions)
            {
                // 습득한 검술
                actionsToDisplay = targetCharacter.GetAcquiredActions();
                isStyleMode = false;
            }
            else if (currentMode == GridDisplayMode.SwordArtActions)
            {
                // 유파 검술
                actionsToDisplay = targetCharacter.GetStyleActions();
                isStyleMode = true;
            }
            
            if (actionsToDisplay == null || actionsToDisplay.Count == 0)
            {
                string modeName = currentMode == GridDisplayMode.AcquiredActions ? "습득 검술" : "유파 검술";
                Log($"{modeName}이 없습니다.");
                return;
            }
            
            // 각 검술에 대해 슬롯 생성
            foreach (var action in actionsToDisplay)
            {
                if (action == null) continue;
                
                CreateActionSlot(action, isStyleMode);
            }
            
            Log($"검술 그리드 갱신 완료: {actionSlots.Count}개 ({currentMode})");
        }
        
        /// <summary>
        /// 장착 슬롯 갱신
        /// </summary>
        private void RefreshEquippedSlots()
        {
            if (targetCharacter == null || equippedActionSlotsContainer == null) return;
            
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
        private void CreateActionSlot(ActionCommandData action, bool isStyleAction = false)
        {
            if (actionCommandSlotPrefab == null || actionCommandGridContainer == null) return;
            
            GameObject slotObj = Instantiate(actionCommandSlotPrefab, actionCommandGridContainer);
            ActionCommandSlotUI slot = slotObj.GetComponent<ActionCommandSlotUI>();
            
            if (slot != null)
            {
                slot.Initialize(action, this, -1, false, isStyleAction);
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
                // 유파 검술인지 확인
                bool isStyleAction = IsActionFromStyle(action);
                
                slot.Initialize(action, this, slotIndex, true, isStyleAction);
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
        /// </summary>
        private bool IsActionFromStyle(ActionCommandData action)
        {
            if (action == null || targetCharacter == null)
                return false;
            
            var styleActions = targetCharacter.GetStyleActions();
            return styleActions != null && styleActions.Contains(action);
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
        /// 슬롯 선택 처리
        /// </summary>
        public void OnSlotSelected(ActionCommandSlotUI slot)
        {
            if (slot == null) return;
            
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
            
            Log($"검술 선택: {slot.ActionData?.name}");
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
            }
            
            if (slotIndex < 0 || slotIndex >= 4)
            {
                Debug.LogWarning("[ActionCommandEquipUI] 빈 슬롯이 없습니다!");
                return;
            }
            
            // 장착
            targetCharacter.EquipAction(action, slotIndex);
            
            // UI 갱신
            RefreshEquippedSlots();
            
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
            
            Log($"검술 해제: {action.commandName} (슬롯 {slotIndex})");
        }
        
        #endregion
        
        #region UI 토글
        
        /// <summary>
        /// 패널 표시
        /// </summary>
        public void Show()
        {
            if (panel == null) return;
            
            panel.SetActive(true);
            RefreshUI();
            
            Log("검술 장착 UI 열림");
        }
        
        /// <summary>
        /// 패널 숨김
        /// </summary>
        public void Hide()
        {
            if (panel == null) return;
            
            panel.SetActive(false);
            
            Log("검술 장착 UI 닫힘");
        }
        
        /// <summary>
        /// 패널 토글
        /// </summary>
        public void TogglePanel()
        {
            if (panel == null) return;
            
            if (panel.activeSelf)
                Hide();
            else
                Show();
        }
        
        /// <summary>
        /// 전체 UI 갱신
        /// </summary>
        public void RefreshUI()
        {
            RefreshActionGrid();
            RefreshEquippedSlots();
            
            if (styleUI != null && targetCharacter != null && targetCharacter.Inventory != null)
            {
                styleUI.Initialize(targetCharacter.Inventory);
            }
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

