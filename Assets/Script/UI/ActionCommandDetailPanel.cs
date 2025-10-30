using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BladeAction.UI
{
    /// <summary>
    /// 검술 상세 정보 패널
    /// 선택된 검술의 상세 정보를 표시하고 액션 버튼을 제공합니다.
    /// </summary>
    public class ActionCommandDetailPanel : MonoBehaviour
    {
        [Header("UI 컴포넌트 - 기본 정보")]
        [Tooltip("선택된 검술 아이콘")]
        [SerializeField] private Image actionIcon;
        
        [Tooltip("선택된 검술 이름")]
        [SerializeField] private TextMeshProUGUI actionNameText;
        
        [Header("태그 동적 생성")]
        [Tooltip("태그 텍스트 프리팹 (SelectedActionCommandTag)")]
        [SerializeField] private GameObject tagPrefab;
        
        [Tooltip("태그가 생성될 컨테이너 (SelectedActionCommandTagContainer)")]
        [SerializeField] private Transform tagContainer;
        
        // 동적 생성된 태그 캐시
        private List<GameObject> spawnedTags = new List<GameObject>();
        
        [Header("UI 컨테이너")]
        [Tooltip("전투 정보를 감싸는 루트 오브젝트 (예: ActionCommandCombatInfo)")]
        [SerializeField] private GameObject combatInfoContainer;
        
        [Tooltip("설명(스크롤뷰 포함)을 감싸는 루트 오브젝트 (예: ActionCommandDescription)")]
        [SerializeField] private GameObject descriptionContainer;
        
        [Header("전투 정보 동적 생성")]
        [Tooltip("타격 정보 표시용 프리팹 (TextMeshProUGUI 포함)")]
        [SerializeField] private GameObject hitInfoPrefab;
        
        [Tooltip("타격 정보가 생성될 부모 Transform")]
        [SerializeField] private Transform hitInfoContainer;
        
        // 동적 생성된 타격 정보 텍스트 캐시
        private readonly List<TextMeshProUGUI> spawnedHitInfoTexts = new List<TextMeshProUGUI>();
        
        [Header("UI 컴포넌트 - 설명")]
        [Tooltip("검술 설명 텍스트")]
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
        
        [Header("기본 아이콘")]
        [Tooltip("검술 미선택 시 표시할 기본 아이콘")]
        [SerializeField] private Sprite defaultIcon;
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        // 현재 선택된 검술 및 Character
        private ActionCommandData currentAction;
        private Character targetCharacter;
        
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
            // 버튼 이벤트 연결
            if (equipButton != null)
                equipButton.onClick.AddListener(OnEquipButtonClicked);
            
            if (toggleButton != null)
                toggleButton.onClick.AddListener(OnToggleButtonClicked);
        }
        
        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (equipButton != null)
                equipButton.onClick.RemoveListener(OnEquipButtonClicked);
            
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(OnToggleButtonClicked);
        }
        
        #endregion
        
        #region 표시
        
        /// <summary>
        /// 검술 정보 표시
        /// </summary>
        public void Show(ActionCommandData action, Character character)
        {
            if (action == null || character == null)
            {
                Debug.LogWarning("[ActionCommandDetailPanel] 검술 또는 캐릭터가 null입니다!");
                return;
            }
            
            // 패널 활성화
            gameObject.SetActive(true);
            
            currentAction = action;
            targetCharacter = character;
            
            // 기본 정보 표시
            UpdateBasicInfo();
            
            // 태그 표시
            UpdateTags();
            
            // 전투 정보 표시
            UpdateCombatInfo();
            
            // 설명 표시
            UpdateDescription();
            
            // 토글 초기 상태 설정 (설명 표시)
            showDescription = true;
            UpdateToggleDisplay();
            
            // 버튼 상태 업데이트
            UpdateButtons();
            
            Log($"검술 상세 정보 표시: {action.commandName}");
        }
        
        /// <summary>
        /// 패널 숨김
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            Clear();
        }
        
        /// <summary>
        /// 패널 초기화
        /// </summary>
        public void Clear()
        {
            currentAction = null;
            targetCharacter = null;
            
            // UI 초기화
            if (actionIcon != null)
            {
                actionIcon.sprite = defaultIcon;
            }
            
            if (actionNameText != null)
            {
                actionNameText.text = "검술을 선택하세요";
            }
            
            if (descriptionText != null)
            {
                descriptionText.text = "";
            }
            
            ClearCombatInfo();
            ClearTags();
            
            // 버튼 비활성화
            if (equipButton != null)
            {
                equipButton.interactable = false;
            }
        }
        
        #endregion
        
        #region UI 업데이트
        
        /// <summary>
        /// 전체 UI 업데이트
        /// </summary>
        private void UpdateDisplay()
        {
            if (currentAction == null || targetCharacter == null) return;
            
            // 기본 정보
            UpdateBasicInfo();
            
            // 전투 정보
            UpdateCombatInfo();
            
            // 설명
            UpdateDescription();
            
            // 버튼 상태
            UpdateButtons();
        }
        
        /// <summary>
        /// 기본 정보 업데이트
        /// </summary>
        private void UpdateBasicInfo()
        {
            // 아이콘
            if (actionIcon != null)
            {
                actionIcon.sprite = currentAction.icon != null ? currentAction.icon : defaultIcon;
            }
            
            // 이름
            if (actionNameText != null)
            {
                actionNameText.text = currentAction.commandName;
            }
        }
        
        /// <summary>
        /// 태그 업데이트 (동적 생성)
        /// </summary>
        private void UpdateTags()
        {
            if (currentAction == null)
            {
                Log("UpdateTags: currentAction이 null입니다.");
                return;
            }
            
            // 기존 태그 제거
            ClearTags();
            
            Log($"UpdateTags 호출: {currentAction.commandName}, 태그 개수: {currentAction.tags?.Count ?? 0}");
            
            // 참조 확인
            if (tagPrefab == null)
            {
                Debug.LogWarning("[ActionCommandDetailPanel] tagPrefab이 설정되지 않았습니다!");
                return;
            }
            
            if (tagContainer == null)
            {
                Debug.LogWarning("[ActionCommandDetailPanel] tagContainer가 설정되지 않았습니다!");
                return;
            }
            
            // 태그가 없으면 종료
            if (currentAction.tags == null || currentAction.tags.Count == 0)
            {
                Log("태그 리스트가 비어있습니다.");
                return;
            }
            
            // 각 태그 생성
            foreach (var tag in currentAction.tags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                
                GameObject tagObj = Instantiate(tagPrefab, tagContainer);
                TextMeshProUGUI tagText = tagObj.GetComponent<TextMeshProUGUI>();
                
                // 직접 없으면 자식에서 찾기
                if (tagText == null)
                {
                    tagText = tagObj.GetComponentInChildren<TextMeshProUGUI>();
                }
                
                if (tagText != null)
                {
                    tagText.text = $"[{tag}]";
                    spawnedTags.Add(tagObj);
                    Log($"태그 생성: [{tag}]");
                }
                else
                {
                    Debug.LogWarning("[ActionCommandDetailPanel] tagPrefab에 TextMeshProUGUI 컴포넌트가 없습니다!");
                    Destroy(tagObj);
                }
            }
            
            Log($"태그 생성 완료: {spawnedTags.Count}개");
        }
        
        /// <summary>
        /// 태그 제거
        /// </summary>
        private void ClearTags()
        {
            foreach (var tag in spawnedTags)
            {
                if (tag != null)
                    Destroy(tag);
            }
            spawnedTags.Clear();
        }
        
        /// <summary>
        /// 전투 정보 업데이트 (동적 생성)
        /// </summary>
        private void UpdateCombatInfo()
        {
            if (currentAction == null) return;
            
            // 기존 정보 제거
            ClearCombatInfo();
            
            // Prefab이 없으면 생성 불가
            if (hitInfoPrefab == null || hitInfoContainer == null)
            {
                Debug.LogWarning("[ActionCommandDetailPanel] hitInfoPrefab 또는 hitInfoContainer가 설정되지 않았습니다!");
                return;
            }
            
            // 타격 정보가 없으면 종료
            if (currentAction.perfectTimings == null || currentAction.perfectTimings.Count == 0)
            {
                CreateHitInfoLine("타격 정보 없음");
                return;
            }
            
            // 각 타격별 정보 생성
            for (int i = 0; i < currentAction.perfectTimings.Count; i++)
            {
                var timing = currentAction.perfectTimings[i];
                float damageRatio = timing.damageRatio;
                
                string hitInfo = $"타격 {i + 1}: 피해량 {damageRatio:F1}x";
                CreateHitInfoLine(hitInfo);
            }
            
            Log($"전투 정보 생성 완료: {spawnedHitInfoTexts.Count}개 타격");
        }
        
        /// <summary>
        /// 타격 정보 라인 생성
        /// </summary>
        private void CreateHitInfoLine(string text)
        {
            GameObject lineObj = Instantiate(hitInfoPrefab, hitInfoContainer);
            TextMeshProUGUI textComponent = lineObj.GetComponent<TextMeshProUGUI>();
            
            if (textComponent != null)
            {
                textComponent.text = text;
                spawnedHitInfoTexts.Add(textComponent);
            }
            else
            {
                Debug.LogWarning("[ActionCommandDetailPanel] hitInfoPrefab에 TextMeshProUGUI 컴포넌트가 없습니다!");
                Destroy(lineObj);
            }
        }
        
        /// <summary>
        /// 전투 정보 초기화 (동적 생성된 오브젝트 제거)
        /// </summary>
        private void ClearCombatInfo()
        {
            foreach (var text in spawnedHitInfoTexts)
            {
                if (text != null)
                    Destroy(text.gameObject);
            }
            spawnedHitInfoTexts.Clear();
        }
        
        /// <summary>
        /// 설명 업데이트
        /// </summary>
        private void UpdateDescription()
        {
            if (descriptionText != null && currentAction != null)
            {
                descriptionText.text = currentAction.description;
            }
        }
        
        /// <summary>
        /// 버튼 상태 업데이트
        /// </summary>
        private void UpdateButtons()
        {
            if (currentAction == null || targetCharacter == null) return;
            
            // 장착/해제 버튼
            if (equipButton != null && equipButtonText != null)
            {
                bool isEquipped = IsActionEquipped();
                
                equipButton.interactable = true;
                equipButtonText.text = isEquipped ? "해제" : "장착";
            }
        }
        
        /// <summary>
        /// 현재 검술이 장착되어 있는지 확인
        /// </summary>
        private bool IsActionEquipped()
        {
            if (currentAction == null || targetCharacter == null) return false;
            
            for (int i = 0; i < 4; i++)
            {
                if (targetCharacter.GetEquippedAction(i) == currentAction)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 현재 검술이 장착된 슬롯 인덱스 찾기
        /// </summary>
        private int GetEquippedSlotIndex()
        {
            if (currentAction == null || targetCharacter == null) return -1;
            
            for (int i = 0; i < 4; i++)
            {
                if (targetCharacter.GetEquippedAction(i) == currentAction)
                {
                    return i;
                }
            }
            
            return -1;
        }
        
        #endregion
        
        #region 버튼 이벤트
        
        /// <summary>
        /// 장착/해제 버튼 클릭
        /// </summary>
        private void OnEquipButtonClicked()
        {
            if (currentAction == null || targetCharacter == null) return;
            
            bool isEquipped = IsActionEquipped();
            
            if (isEquipped)
            {
                // 해제
                int slotIndex = GetEquippedSlotIndex();
                if (slotIndex >= 0)
                {
                    targetCharacter.UnequipAction(slotIndex);
                    Log($"검술 해제: {currentAction.commandName} (슬롯 {slotIndex})");
                }
            }
            else
            {
                // 장착 (빈 슬롯 찾기)
                int emptySlot = -1;
                for (int i = 0; i < 4; i++)
                {
                    if (targetCharacter.GetEquippedAction(i) == null)
                    {
                        emptySlot = i;
                        break;
                    }
                }
                
                if (emptySlot >= 0)
                {
                    targetCharacter.EquipAction(currentAction, emptySlot);
                    Log($"검술 장착: {currentAction.commandName} → 슬롯 {emptySlot}");
                }
                else
                {
                    Debug.LogWarning("[ActionCommandDetailPanel] 빈 슬롯이 없습니다!");
                }
            }
            
            // UI 갱신
            UpdateDisplay();
            
            // 부모 UI에 갱신 요청 (이벤트 또는 직접 호출)
            var parentUI = GetComponentInParent<ActionCommandEquipUI>();
            if (parentUI != null)
            {
                parentUI.RefreshUI();
            }
        }
        
        /// <summary>
        /// 토글 버튼 클릭 (전투 정보/설명 전환)
        /// </summary>
        private void OnToggleButtonClicked()
        {
            // 토글 상태 전환
            showDescription = !showDescription;
            UpdateToggleDisplay();
            
            Log($"토글: {(showDescription ? "설명" : "전투 정보")} 표시");
        }
        
        /// <summary>
        /// 토글 표시 업데이트
        /// </summary>
        private void UpdateToggleDisplay()
        {
            // 컨테이너 표시 전환
            if (descriptionContainer != null)
                descriptionContainer.SetActive(showDescription);
            
            if (combatInfoContainer != null)
                combatInfoContainer.SetActive(!showDescription);
            
            // 버튼 텍스트 업데이트
            if (toggleButtonText != null)
            {
                toggleButtonText.text = showDescription ? "전투 정보" : "설명";
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
                Debug.Log($"[ActionCommandDetailPanel] {message}");
            }
        }
        
        #endregion
    }
}

