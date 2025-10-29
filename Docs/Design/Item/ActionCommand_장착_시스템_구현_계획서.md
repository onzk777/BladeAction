# ActionCommand 장착 시스템 구현 계획서

## 1. 문서 개요

### 1.1 목적
ActionCommand 장착 시스템의 구현 순서, 일정, 작업 내용을 정의하여 체계적인 개발을 진행합니다.

### 1.2 관련 문서
- `ActionCommand_장착_시스템_구현_명세서.md` (시스템 설계)
- `아이템_시스템_명세서.md` (기존 인벤토리 시스템)

### 1.3 구현 방식
**인벤토리 시스템 통합 방식** - 검술을 ItemType.ActionCommand로 아이템화하여 기존 CharacterInventory 시스템 활용

---

## 2. 전체 일정 개요

### 2.1 총 예상 소요 시간
**약 12~16시간** (UI 구현 포함)

### 2.2 단계별 일정
| Phase | 작업 내용 | 예상 소요 시간 | 우선순위 |
|-------|-----------|----------------|----------|
| Phase 1 | 데이터 구조 확장 | 1~2시간 | 필수 |
| Phase 2 | 인벤토리 시스템 확장 | 2~3시간 | 필수 |
| Phase 3 | Character 클래스 정리 | 1시간 | 필수 |
| Phase 4 | 검술 아이템 생성 도구 | 1~2시간 | 필수 |
| Phase 5 | UI 구현 | 4~6시간 | 필수 |
| Phase 6 | CSV 통합 | 1~2시간 | 선택 |
| Phase 7 | 테스트 및 디버깅 | 2~3시간 | 필수 |

---

## 3. Phase 1: 데이터 구조 확장 (1~2시간)

### 3.1 작업 목표
기본 데이터 타입 및 열거형 확장

### 3.2 세부 작업

#### 3.2.1 ItemType 열거형 확장
**파일**: `Assets/Script/Item/ItemType.cs` (또는 Item.cs 내부)

**작업 내용**:
```csharp
public enum ItemType
{
    Weapon,
    Armor,
    Accessory,
    SwordArtStyle,
    ActionCommand,   // 추가
    Consumable       // 향후 확장용 (선택)
}
```

**예상 시간**: 5분

---

#### 3.2.2 EquipmentSlotType 열거형 확장
**파일**: `Assets/Script/Item/EquipmentSlot.cs` (또는 별도 파일)

**작업 내용**:
```csharp
public enum EquipmentSlotType
{
    Weapon,
    Armor,
    Accessory,
    SwordArtStyle,
    ActionSlot1,     // 추가
    ActionSlot2,     // 추가
    ActionSlot3,     // 추가
    ActionSlot4      // 추가
}
```

**예상 시간**: 5분

---

#### 3.2.3 Item 클래스 확장
**파일**: `Assets/Script/Item/Item.cs`

**작업 내용**:
```csharp
[Header("검술 데이터")]
[Tooltip("이 아이템이 검술(ActionCommand)인 경우 ActionCommandData 참조")]
public ActionCommandData actionCommandData;
```

**예상 시간**: 5분

---

#### 3.2.4 EquipmentSlot 검증 로직 확장
**파일**: `Assets/Script/Item/EquipmentSlot.cs`

**작업 내용**:
`CanEquipItem()` 메서드 수정
```csharp
case EquipmentSlotType.ActionSlot1:
case EquipmentSlotType.ActionSlot2:
case EquipmentSlotType.ActionSlot3:
case EquipmentSlotType.ActionSlot4:
    return item.itemType == ItemType.ActionCommand;
```

**예상 시간**: 10분

---

#### 3.2.5 검증 및 컴파일 테스트
- Unity 에디터에서 컴파일 오류 확인
- 기존 코드가 정상 작동하는지 확인

**예상 시간**: 30분

---

### 3.3 완료 기준
- [ ] ItemType.ActionCommand 추가
- [ ] EquipmentSlotType.ActionSlot1~4 추가
- [ ] Item.actionCommandData 필드 추가
- [ ] EquipmentSlot.CanEquipItem() 로직 확장
- [ ] 컴파일 오류 없음

---

## 4. Phase 2: 인벤토리 시스템 확장 (2~3시간)

### 4.1 작업 목표
CharacterInventory 클래스에 검술 관리 기능 추가

### 4.2 세부 작업

#### 4.2.1 InitializeDefaultEquipmentSlots() 수정
**파일**: `Assets/Script/Item/CharacterInventory.cs`

**작업 내용**:
검술 슬롯 4개 추가
```csharp
// 검술 슬롯 4개 추가
equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot1, "검술 슬롯 1"));
equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot2, "검술 슬롯 2"));
equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot3, "검술 슬롯 3"));
equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot4, "검술 슬롯 4"));
```

**예상 시간**: 10분

---

#### 4.2.2 UnequipAllStyleActions() 구현
**파일**: `Assets/Script/Item/CharacterInventory.cs`

**작업 내용**:
유파 해제 시 유파 검술 자동 해제 로직 구현 (명세서 참조)

**핵심 로직**:
1. 유파 아이템에서 SwordArtStyleData 가져오기
2. ActionSlot1~4 순회
3. 장착된 검술이 유파 검술인지 확인
4. 유파 검술이면 해제 및 인벤토리 복귀

**예상 시간**: 40분

---

#### 4.2.3 UnequipItem() 수정
**파일**: `Assets/Script/Item/CharacterInventory.cs`

**작업 내용**:
유파 슬롯 해제 시 `UnequipAllStyleActions()` 호출 추가
```csharp
// 유파 해제 시 유파 검술 자동 해제
if (slotType == EquipmentSlotType.SwordArtStyle)
{
    UnequipAllStyleActions(unequippedKey);
}
```

**예상 시간**: 10분

---

#### 4.2.4 편의 메서드 추가
**파일**: `Assets/Script/Item/CharacterInventory.cs`

**작업 내용**:
- `GetAcquiredActions()` - 습득 검술 목록 반환
- `GetEquippedStyleActions()` - 유파 검술 목록 반환
- `GetEquippedActions()` - 장착된 검술 4개 반환

**예상 시간**: 30분

---

#### 4.2.5 단위 테스트 (에디터 테스트)
- 검술 슬롯 초기화 확인
- 유파 해제 시 검술 자동 해제 확인
- 편의 메서드 반환값 확인

**예상 시간**: 30~60분

---

### 4.3 완료 기준
- [ ] 검술 슬롯 4개 초기화
- [ ] UnequipAllStyleActions() 구현
- [ ] UnequipItem() 수정 (유파 콜백)
- [ ] GetAcquiredActions() 구현
- [ ] GetEquippedStyleActions() 구현
- [ ] GetEquippedActions() 구현
- [ ] 단위 테스트 통과

---

## 5. Phase 3: Character 클래스 정리 (1시간)

### 5.1 작업 목표
기존 Character 클래스의 검술 관리 코드 정리 및 리팩토링

### 5.2 세부 작업

#### 5.2.1 불필요한 필드/메서드 제거
**파일**: `Assets/Script/Character.cs`

**제거 대상**:
```csharp
// 제거
private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

// 제거 또는 수정
public void EquipSwordArtStyle(SwordArtStyleData styleData)
{
    _availableCommands.Clear();
    if (styleData != null)
    {
        _availableCommands.AddRange(styleData.GetActionCommands());
    }
    OnStyleEquipped?.Invoke(styleData);
}
```

**예상 시간**: 10분

---

#### 5.2.2 AvailableCommands 프로퍼티 수정
**파일**: `Assets/Script/Character.cs`

**수정 내용**:
```csharp
/// <summary>
/// 전투에서 사용 가능한 검술 목록 (장착된 4개)
/// </summary>
public List<ActionCommandData> AvailableCommands 
    => Inventory?.GetEquippedActions() ?? new List<ActionCommandData>();
```

**예상 시간**: 5분

---

#### 5.2.3 전투 시스템 호환성 확인
**파일**: `Assets/Script/Controller/PlayerController.cs`, `EnemyController.cs` 등

**확인 내용**:
- `Character.AvailableCommands` 사용 코드가 정상 작동하는지 확인
- `GetCurrentActionCommand(int index)` 메서드가 정상 작동하는지 확인

**예상 시간**: 20분

---

#### 5.2.4 컴파일 및 런타임 테스트
- Unity 에디터에서 실행하여 전투 시스템 정상 작동 확인

**예상 시간**: 25분

---

### 5.3 완료 기준
- [ ] `_availableCommands` 필드 제거
- [ ] `EquipSwordArtStyle()` 제거 또는 수정
- [ ] `AvailableCommands` 프로퍼티 수정
- [ ] 전투 시스템 정상 작동 확인
- [ ] 컴파일 오류 없음

---

## 6. Phase 4: 검술 아이템 생성 도구 (1~2시간)

### 6.1 작업 목표
기존 ActionCommandData에 대응하는 Item SO를 자동 생성하는 에디터 도구 작성

### 6.2 세부 작업

#### 6.2.1 ActionCommandItemGenerator 스크립트 작성
**파일**: `Assets/Script/Editor/ActionCommandItemGenerator.cs` (새 파일)

**작업 내용**:
1. `Assets/Resources/Data/ActionData` 폴더의 모든 ActionCommandData 찾기
2. 각 ActionCommandData에 대응하는 Item SO 생성
3. Item 필드 자동 설정:
   - `itemKey = "action_" + actionData.name`
   - `itemName = actionData.commandName`
   - `itemType = ItemType.ActionCommand`
   - `actionCommandData = actionData`
   - `maxStack = 1`
4. `Assets/Resources/Data/Items/Actions/` 폴더에 저장

**예상 시간**: 60분

---

#### 6.2.2 에디터 메뉴 등록
**파일**: `Assets/Script/Editor/ActionCommandItemGenerator.cs`

**작업 내용**:
```csharp
[MenuItem("BladeAction/Generate Action Items")]
public static void GenerateActionItems() { ... }
```

**예상 시간**: 5분

---

#### 6.2.3 검술 아이템 일괄 생성
**실행 순서**:
1. Unity 에디터에서 `BladeAction > Generate Action Items` 메뉴 실행
2. 콘솔 로그에서 생성된 아이템 확인
3. `Assets/Resources/Data/Items/Actions/` 폴더 확인

**예상 시간**: 10분

---

#### 6.2.4 ItemDatabase 등록 (수동 또는 자동)
**작업 내용**:
- 생성된 Item SO를 ItemDatabase에 등록
- CSV Export 후 재Import로 자동 등록 가능

**예상 시간**: 15분

---

#### 6.2.5 검증
- 생성된 Item SO가 정상적으로 참조되는지 확인
- Inspector에서 필드가 올바르게 설정되었는지 확인

**예상 시간**: 10분

---

### 6.3 완료 기준
- [ ] ActionCommandItemGenerator 스크립트 작성
- [ ] 에디터 메뉴 등록
- [ ] 검술 아이템 일괄 생성
- [ ] ItemDatabase 등록
- [ ] 검증 완료

---

## 7. Phase 5: UI 구현 (4~6시간)

### 7.1 작업 목표
**ActionCommandEquipCanvas** Prefab 및 **ActionCommandEquipUI** 스크립트를 통해 검술 장착 전용 UI 시스템 구현

**참고**: Unity 에디터에서의 UI 오브젝트 생성, Canvas 구성, 참조 연결 등은 사용자가 직접 수행합니다.

### 7.2 UI 구조 개요

```
ActionCommandEquipCanvas (Prefab)
├── ActionCommandEquipPanel
│   ├── TopTabBar (가방/검술/...)
│   ├── LeftPanel
│   │   ├── EquippedStyleInfoPanel (유파 정보)
│   │   └── EquippedActionSlotsPanel (장착 검술 슬롯 4개)
│   ├── CenterPanel
│   │   ├── SubTabBar (습득 검술/유파 검술)
│   │   ├── ActionListScrollView (검술 목록)
│   │   └── ActionCommandDetailPanel (검술 상세 정보)
│   └── RightPanel
│       └── CharacterStatusPanel (캐릭터 스테이터스)
```

### 7.3 세부 작업 (스크립트 구현)

#### 7.3.1 ActionCommandEquipUI 메인 스크립트 작성
**파일**: `Assets/Script/UI/ActionCommandEquipUI.cs` (새 파일)

**작업 내용**:
```csharp
public class ActionCommandEquipUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject actionCommandEquipPanel;
    [SerializeField] private EquippedStyleInfoPanel equippedStyleInfoPanel;
    [SerializeField] private EquippedActionSlotsPanel equippedActionSlotsPanel;
    [SerializeField] private ActionListPanel actionListPanel;
    [SerializeField] private ActionCommandDetailPanel actionDetailPanel;
    [SerializeField] private CharacterStatusPanel characterStatusPanel;
    
    [Header("Tab References")]
    [SerializeField] private Button acquiredActionTabButton;
    [SerializeField] private Button styleActionTabButton;
    
    private CharacterInventory inventory;
    private ActionCommandData selectedAction;
    private ActionSubTab currentSubTab = ActionSubTab.Acquired;
    
    public enum ActionSubTab { Acquired, Style }
    
    // 초기화, 탭 전환, 검술 선택, 장착/해제 로직
}
```

**주요 메서드**:
- `Initialize()`: UI 초기화 및 이벤트 연결
- `OnTabChanged()`: 서브 탭 전환 (습득/유파)
- `OnActionSelected()`: 검술 선택 시 상세 정보 표시
- `OnEquipAction()`: 검술 장착
- `OnUnequipAction()`: 검술 해제
- `RefreshUI()`: 전체 UI 갱신

**예상 시간**: 90분

---

#### 7.3.2 EquippedStyleInfoPanel 컴포넌트
**파일**: `Assets/Script/UI/EquippedStyleInfoPanel.cs` (새 파일)

**작업 내용**:
```csharp
public class EquippedStyleInfoPanel : MonoBehaviour
{
    [SerializeField] private Image styleIcon;
    [SerializeField] private Text styleName;
    [SerializeField] private Text styleDescription;
    [SerializeField] private GameObject styleEffectsContainer; // 패시브 효과 박스
    [SerializeField] private Text styleEffectsText;
    [SerializeField] private GameObject emptyStyleMessage; // "유파를 장착하세요"
    
    public void UpdateStyleInfo(Item styleItem)
    {
        if (styleItem == null || styleItem.swordArtStyleData == null)
        {
            ShowEmptyMessage();
            return;
        }
        
        HideEmptyMessage();
        styleIcon.sprite = styleItem.itemIcon;
        styleName.text = styleItem.itemName;
        styleDescription.text = styleItem.itemDescription;
        UpdateStyleEffects(styleItem.swordArtStyleData);
    }
    
    private void UpdateStyleEffects(SwordArtStyleData styleData)
    {
        // 유파 패시브 효과 표시 (향후 구현)
        // 현재는 placeholder 텍스트 표시
    }
}
```

**예상 시간**: 60분

---

#### 7.3.3 EquippedActionSlotsPanel 컴포넌트
**파일**: `Assets/Script/UI/EquippedActionSlotsPanel.cs` (새 파일)

**작업 내용**:
```csharp
public class EquippedActionSlotsPanel : MonoBehaviour
{
    [SerializeField] private EquippedActionSlotUI[] actionSlots; // 4개 슬롯
    
    private System.Action<int> onSlotClicked;
    
    public void Initialize(System.Action<int> slotClickCallback)
    {
        onSlotClicked = slotClickCallback;
        
        for (int i = 0; i < actionSlots.Length; i++)
        {
            int slotIndex = i; // 클로저 변수
            actionSlots[i].Initialize(() => onSlotClicked?.Invoke(slotIndex));
        }
    }
    
    public void UpdateSlots(List<ActionCommandData> equippedActions)
    {
        for (int i = 0; i < actionSlots.Length; i++)
        {
            if (i < equippedActions.Count && equippedActions[i] != null)
            {
                actionSlots[i].SetAction(equippedActions[i]);
            }
            else
            {
                actionSlots[i].SetEmpty();
            }
        }
    }
}
```

**예상 시간**: 45분

---

#### 7.3.4 EquippedActionSlotUI 컴포넌트
**파일**: `Assets/Script/UI/EquippedActionSlotUI.cs` (새 파일)

**작업 내용**:
```csharp
public class EquippedActionSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image actionIcon;
    [SerializeField] private Text actionName;
    [SerializeField] private GameObject emptySlotIndicator;
    
    private ActionCommandData currentAction;
    private System.Action onClicked;
    
    public void Initialize(System.Action clickCallback)
    {
        onClicked = clickCallback;
    }
    
    public void SetAction(ActionCommandData action)
    {
        currentAction = action;
        actionIcon.sprite = action.icon; // ActionCommandData에 icon 필드 필요
        actionName.text = action.commandName;
        emptySlotIndicator.SetActive(false);
    }
    
    public void SetEmpty()
    {
        currentAction = null;
        actionName.text = "(비어있음)";
        emptySlotIndicator.SetActive(true);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentAction != null)
        {
            onClicked?.Invoke();
        }
    }
}
```

**예상 시간**: 45분

---

#### 7.3.5 ActionListPanel 컴포넌트
**파일**: `Assets/Script/UI/ActionListPanel.cs` (새 파일)

**작업 내용**:
```csharp
public class ActionListPanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private ActionCommandSlotUI slotPrefab;
    
    private List<ActionCommandSlotUI> slotInstances = new List<ActionCommandSlotUI>();
    private System.Action<ActionCommandData> onActionSelected;
    
    public void Initialize(System.Action<ActionCommandData> selectionCallback)
    {
        onActionSelected = selectionCallback;
    }
    
    public void UpdateList(List<Item> actionItems)
    {
        ClearSlots();
        
        foreach (var item in actionItems)
        {
            if (item.actionCommandData == null)
                continue;
            
            var slot = Instantiate(slotPrefab, contentContainer);
            slot.SetAction(item.actionCommandData);
            slot.Initialize(() => onActionSelected?.Invoke(item.actionCommandData));
            slotInstances.Add(slot);
        }
    }
    
    public void HighlightAction(ActionCommandData action)
    {
        foreach (var slot in slotInstances)
        {
            slot.SetHighlight(slot.CurrentAction == action);
        }
    }
    
    private void ClearSlots()
    {
        foreach (var slot in slotInstances)
        {
            Destroy(slot.gameObject);
        }
        slotInstances.Clear();
    }
}
```

**예상 시간**: 60분

---

#### 7.3.6 ActionCommandSlotUI 컴포넌트
**파일**: `Assets/Script/UI/ActionCommandSlotUI.cs` (새 파일)

**작업 내용**:
```csharp
public class ActionCommandSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Text actionName;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    
    public ActionCommandData CurrentAction { get; private set; }
    private System.Action onClicked;
    
    public void Initialize(System.Action clickCallback)
    {
        onClicked = clickCallback;
    }
    
    public void SetAction(ActionCommandData action)
    {
        CurrentAction = action;
        actionName.text = action.commandName;
    }
    
    public void SetHighlight(bool highlight)
    {
        background.color = highlight ? highlightColor : normalColor;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke();
    }
}
```

**예상 시간**: 30분

---

#### 7.3.7 ActionCommandDetailPanel 컴포넌트
**파일**: `Assets/Script/UI/ActionCommandDetailPanel.cs` (새 파일)

**작업 내용**:
```csharp
public class ActionCommandDetailPanel : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Text toggleButtonText;
    
    [Header("Content")]
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private Text descriptionText;
    [SerializeField] private GameObject combatInfoPanel;
    [SerializeField] private Text combatInfoText;
    
    [Header("Action Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Text equipButtonText;
    
    private ActionCommandData currentAction;
    private bool isShowingCombatInfo = true;
    private System.Action<ActionCommandData> onEquip;
    private System.Action<ActionCommandData> onUnequip;
    
    public void Initialize(
        System.Action<ActionCommandData> equipCallback,
        System.Action<ActionCommandData> unequipCallback)
    {
        onEquip = equipCallback;
        onUnequip = unequipCallback;
        
        toggleButton.onClick.AddListener(OnToggleButtonClicked);
        equipButton.onClick.AddListener(OnEquipButtonClicked);
    }
    
    public void ShowActionDetail(ActionCommandData action, bool isEquipped)
    {
        currentAction = action;
        gameObject.SetActive(true);
        
        UpdateContent();
        UpdateEquipButton(isEquipped);
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    private void UpdateContent()
    {
        if (isShowingCombatInfo)
        {
            ShowCombatInfo();
            toggleButtonText.text = "검술 설명 보기";
        }
        else
        {
            ShowDescription();
            toggleButtonText.text = "전투 정보 보기";
        }
    }
    
    private void ShowCombatInfo()
    {
        descriptionPanel.SetActive(false);
        combatInfoPanel.SetActive(true);
        
        // ActionCommandData에서 전투 정보 읽기
        string info = $"공격 횟수 : {currentAction.numberOfAttacks}번\n\n";
        
        for (int i = 0; i < currentAction.damageMultipliers.Count; i++)
        {
            float percentage = currentAction.damageMultipliers[i] * 100f;
            info += $"{i+1}타 공격 : {percentage}% 피해\n";
        }
        
        combatInfoText.text = info;
    }
    
    private void ShowDescription()
    {
        descriptionPanel.SetActive(true);
        combatInfoPanel.SetActive(false);
        descriptionText.text = currentAction.description;
    }
    
    private void UpdateEquipButton(bool isEquipped)
    {
        if (isEquipped)
        {
            equipButtonText.text = "장착 해제";
        }
        else
        {
            equipButtonText.text = "장착";
        }
    }
    
    private void OnToggleButtonClicked()
    {
        isShowingCombatInfo = !isShowingCombatInfo;
        UpdateContent();
    }
    
    private void OnEquipButtonClicked()
    {
        if (equipButtonText.text == "장착")
        {
            onEquip?.Invoke(currentAction);
        }
        else
        {
            onUnequip?.Invoke(currentAction);
        }
    }
}
```

**예상 시간**: 90분

---

#### 7.3.8 Prefab 생성 (Unity 에디터 작업 - 사용자 직접 수행)
**파일**: `Assets/Prefab/ActionCommandEquipCanvas.prefab`

**작업 내용** (사용자가 Unity 에디터에서 수행):
1. Canvas 생성 (Render Mode: Screen Space - Overlay)
2. 패널 계층 구조 생성 (LeftPanel, CenterPanel, RightPanel)
3. Layout Group 설정 (Horizontal Layout Group 등)
4. ActionCommandEquipUI 컴포넌트 추가 및 참조 연결
5. 각 하위 패널 컴포넌트 참조 연결

**참고 문서**: 
- `Docs/Design/UI/인벤토리_Prefab_생성_가이드.md`
- `Docs/Design/UI/UI기획_인벤토리_오브젝트_구조.md`

**예상 시간**: 사용자가 직접 수행 (스크립트 구현 시간에서 제외)

---

#### 7.3.9 테스트 도구 구현 (선택사항)
**파일**: `Assets/Script/ActionCommandEquipTestManager.cs`

**작업 내용**:
```csharp
public class ActionCommandEquipTestManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ActionCommandEquipUI actionCommandEquipUI;
    
    [Header("Test Data")]
    [SerializeField] private List<Item> testActionItems;
    [SerializeField] private Item testStyleItem;
    
    [ContextMenu("Open Equip UI")]
    public void OpenEquipUI()
    {
        actionCommandEquipUI.gameObject.SetActive(true);
        actionCommandEquipUI.Initialize();
    }
    
    [ContextMenu("Add Test Action")]
    public void AddTestAction()
    {
        if (testActionItems.Count > 0)
        {
            var inventory = CharacterManager.Instance.PlayerCharacter.Inventory;
            inventory.AddItem(testActionItems[0].itemKey, 1);
        }
    }
    
    [ContextMenu("Equip Test Style")]
    public void EquipTestStyle()
    {
        if (testStyleItem != null)
        {
            var inventory = CharacterManager.Instance.PlayerCharacter.Inventory;
            inventory.EquipItem(testStyleItem.itemKey, EquipmentSlotType.SwordArtStyle);
        }
    }
}
```

**예상 시간**: 45분

---

#### 7.3.10 UI 통합 및 테스트
**작업 내용**:
1. Scene에 ActionCommandEquipCanvas Prefab 배치 (사용자 수행)
2. CharacterManager 연동 확인
3. 탭 전환 동작 확인
4. 검술 선택 및 상세 정보 표시 확인
5. 장착/해제 기능 확인
6. 유파 해제 시 자동 해제 확인
7. Layout 자동 조정 확인 (선택 시/미선택 시)

**예상 시간**: 60분

---

### 7.4 완료 기준 (스크립트 구현)
- [ ] ActionCommandEquipUI 메인 스크립트 구현
- [ ] EquippedStyleInfoPanel 구현
- [ ] EquippedActionSlotsPanel + EquippedActionSlotUI 구현
- [ ] ActionListPanel + ActionCommandSlotUI 구현
- [ ] ActionCommandDetailPanel 구현
- [ ] 테스트 도구 구현 (선택)
- [ ] 통합 테스트 통과

**참고**: Unity UI Prefab 생성 및 Inspector 참조 연결은 사용자가 직접 수행

### 7.5 참고 자료
- `Docs/Design/UI/인벤토리_Prefab_생성_가이드.md`: Prefab 생성 방법 참고
- `Docs/Design/UI/UI기획_인벤토리_오브젝트_구조.md`: UI 계층 구조 참고
- `Assets/Script/UI/InventoryUI.cs`: 기존 인벤토리 UI 스크립트 참고

---

## 8. Phase 6: CSV 통합 (1~2시간, 선택사항)

### 8.1 작업 목표
검술 아이템을 ItemTable CSV에 통합하여 데이터 관리 일원화

### 8.2 세부 작업

#### 8.2.1 ItemTable CSV 스키마 확장
**파일**: `Docs/Templates/ItemTable_Template.csv`

**작업 내용**:
`actionCommandKey` 컬럼 추가
```csv
itemKey,itemName,itemType,description,maxStack,actionCommandKey,iconPath
action_basic_slash,기본 베기,ActionCommand,기본적인 베기 공격,1,BasicSlash,icon_action_slash
```

**예상 시간**: 15분

---

#### 8.2.2 CSV Import 로직 수정
**파일**: `Assets/Script/Editor/ItemImportExportEditor.cs` (또는 해당 파일)

**작업 내용**:
1. `actionCommandKey` 컬럼 읽기
2. Resources.Load로 ActionCommandData 찾기
3. Item.actionCommandData 자동 매핑

**예상 시간**: 45분

---

#### 8.2.3 CSV Export 로직 수정
**파일**: `Assets/Script/Editor/ItemImportExportEditor.cs`

**작업 내용**:
1. Item.actionCommandData가 있으면 `actionCommandKey` 컬럼에 기록
2. CSV 생성 시 검술 아이템 포함

**예상 시간**: 30분

---

#### 8.2.4 테스트
- CSV Import 후 actionCommandData 자동 매핑 확인
- CSV Export 후 검술 아이템 포함 확인

**예상 시간**: 15분

---

### 8.3 완료 기준
- [ ] ItemTable CSV 스키마 확장
- [ ] CSV Import 로직 수정
- [ ] CSV Export 로직 수정
- [ ] Import/Export 테스트 통과

---

## 9. Phase 7: 테스트 및 디버깅 (2~3시간)

### 9.1 작업 목표
전체 시스템 통합 테스트 및 버그 수정

### 9.2 테스트 시나리오

#### 9.2.1 기본 기능 테스트
**시나리오 1: 검술 획득 및 장착**
1. 검술 아이템을 인벤토리에 추가
2. 검술 탭에서 검술 확인
3. 검술을 검술 슬롯에 장착
4. 장착된 검술이 전투 UI에 표시되는지 확인

**예상 시간**: 20분

---

**시나리오 2: 검술 해제**
1. 장착된 검술을 우클릭 → 해제
2. 인벤토리에 다시 추가되는지 확인

**예상 시간**: 10분

---

#### 9.2.2 유파 연동 테스트
**시나리오 3: 유파 장착 및 검술 접근**
1. 유파 아이템을 유파 슬롯에 장착
2. 검술 탭 → 유파 검술 서브 탭 선택
3. 유파 검술 목록이 표시되는지 확인
4. 유파 검술을 검술 슬롯에 장착

**예상 시간**: 20분

---

**시나리오 4: 유파 해제 시 검술 자동 해제**
1. 유파 검술과 습득 검술을 각각 장착 (총 4개 슬롯 중 2개씩)
2. 유파를 해제
3. 유파 검술만 자동 해제되고 습득 검술은 유지되는지 확인

**예상 시간**: 20분

---

#### 9.2.3 전투 시스템 통합 테스트
**시나리오 5: 전투 중 검술 사용**
1. 검술 4개 장착
2. 전투 진입
3. 전투 UI에 장착 검술 4개가 표시되는지 확인
4. 각 검술을 선택하여 사용 가능한지 확인

**예상 시간**: 30분

---

**시나리오 6: 검술 교체 후 전투 재진입**
1. 전투 종료 후 인벤토리에서 검술 교체
2. 전투 재진입
3. 변경된 검술이 반영되는지 확인

**예상 시간**: 20분

---

#### 9.2.4 경계 조건 테스트
**시나리오 7: 검술 슬롯 가득 참**
1. 검술 4개 모두 장착
2. 새로운 검술 장착 시도
3. 기존 검술이 인벤토리로 돌아가는지 확인

**예상 시간**: 15분

---

**시나리오 8: 유파 미장착 시 유파 검술 접근**
1. 유파를 장착하지 않은 상태
2. 검술 탭 → 유파 검술 서브 탭 선택
3. "유파를 장착하세요" 메시지 표시되는지 확인

**예상 시간**: 10분

---

#### 9.2.5 버그 수정 및 재테스트
- 발견된 버그 수정
- 회귀 테스트

**예상 시간**: 60분

---

### 9.3 완료 기준
- [ ] 모든 테스트 시나리오 통과
- [ ] 버그 수정 완료
- [ ] 회귀 테스트 통과
- [ ] 성능 이상 없음

---

## 10. 구현 순서 요약

### 10.1 Day 1 (4~6시간)
1. **Phase 1: 데이터 구조 확장** (1~2시간)
   - ItemType, EquipmentSlotType 확장
   - Item 클래스 확장
   - EquipmentSlot 검증 로직 확장

2. **Phase 2: 인벤토리 시스템 확장** (2~3시간)
   - CharacterInventory 슬롯 초기화 수정
   - UnequipAllStyleActions() 구현
   - 편의 메서드 추가

3. **Phase 3: Character 클래스 정리** (1시간)
   - 불필요한 코드 제거
   - AvailableCommands 프로퍼티 수정

---

### 10.2 Day 2 (5~8시간)
4. **Phase 4: 검술 아이템 생성 도구** (1~2시간)
   - ActionCommandItemGenerator 작성
   - 검술 아이템 일괄 생성

5. **Phase 5: UI 구현** (4~6시간)
   - UI 레이아웃 수정
   - InventoryUI 스크립트 수정
   - 드래그 앤 드롭 구현
   - 유파 검술 표시 로직

---

### 10.3 Day 3 (3~5시간)
6. **Phase 6: CSV 통합** (1~2시간, 선택)
   - CSV 스키마 확장
   - Import/Export 로직 수정

7. **Phase 7: 테스트 및 디버깅** (2~3시간)
   - 전체 테스트 시나리오 실행
   - 버그 수정

---

## 11. 리스크 및 대응 방안

### 11.1 리스크 1: 유파 검술 Item 매핑 문제
**문제**: 유파 검술은 인벤토리에 실제 Item이 없어 장착 시 참조 문제 발생 가능

**대응 방안**:
1. **방안 A**: 유파 검술도 미리 Item으로 생성하여 ItemDatabase에 등록 (권장)
2. **방안 B**: 런타임에 임시 Item 래퍼 생성 (복잡도 증가)

**선택**: 방안 A 채택

---

### 11.2 리스크 2: UI 복잡도 증가
**문제**: 검술 탭, 서브 탭, 유파 검술 표시 등으로 UI 로직 복잡도 증가

**대응 방안**:
1. UI 컴포넌트 모듈화 (ActionSlotUI, ActionTabUI 등)
2. 명확한 책임 분리
3. 주석 및 문서화 철저

---

### 11.3 리스크 3: 기존 전투 시스템 호환성
**문제**: AvailableCommands 변경으로 기존 전투 코드에 영향 가능

**대응 방안**:
1. 하위 호환성 유지 (프로퍼티 시그니처 동일)
2. Phase 3에서 철저히 테스트
3. 문제 발생 시 구 구현 병행 유지 (Fallback)

---

### 11.4 리스크 4: 성능 문제
**문제**: GetEquippedActions() 등이 매 프레임 호출 시 성능 저하 가능

**대응 방안**:
1. 캐싱 메커니즘 도입 (Dirty Flag)
2. 장착/해제 시에만 캐시 갱신
3. 프로파일링 후 최적화

---

## 12. 진행 상황 체크리스트

### Phase 1: 데이터 구조 확장
- [ ] ItemType.ActionCommand 추가
- [ ] EquipmentSlotType.ActionSlot1~4 추가
- [ ] Item.actionCommandData 필드 추가
- [ ] EquipmentSlot.CanEquipItem() 확장
- [ ] 컴파일 오류 없음

### Phase 2: 인벤토리 시스템 확장
- [ ] InitializeDefaultEquipmentSlots() 수정
- [ ] UnequipAllStyleActions() 구현
- [ ] UnequipItem() 수정
- [ ] GetAcquiredActions() 구현
- [ ] GetEquippedStyleActions() 구현
- [ ] GetEquippedActions() 구현
- [ ] 단위 테스트 통과

### Phase 3: Character 클래스 정리
- [ ] _availableCommands 제거
- [ ] EquipSwordArtStyle() 제거/수정
- [ ] AvailableCommands 프로퍼티 수정
- [ ] 전투 시스템 정상 작동 확인

### Phase 4: 검술 아이템 생성 도구
- [ ] ActionCommandItemGenerator 작성
- [ ] 에디터 메뉴 등록
- [ ] 검술 아이템 일괄 생성
- [ ] ItemDatabase 등록

### Phase 5: UI 구현
- [ ] UI 레이아웃 수정
- [ ] InventoryUI 필터링 로직
- [ ] 검술 슬롯 UI
- [ ] 드래그 앤 드롭 구현
- [ ] 유파 검술 표시 로직

### Phase 6: CSV 통합 (선택)
- [ ] ItemTable CSV 스키마 확장
- [ ] CSV Import 로직 수정
- [ ] CSV Export 로직 수정

### Phase 7: 테스트 및 디버깅
- [ ] 기본 기능 테스트
- [ ] 유파 연동 테스트
- [ ] 전투 시스템 통합 테스트
- [ ] 경계 조건 테스트
- [ ] 버그 수정 완료

---

## 13. 참고사항

### 13.1 개발 환경
- Unity 2021.3 이상
- C# 9.0 이상
- TextMeshPro (UI용)

### 13.2 의존성
- 기존 인벤토리 시스템 (CharacterInventory)
- ItemDatabase, ItemEvents
- ActionCommandData (기존)
- SwordArtStyleData (기존)

### 13.3 코딩 컨벤션
- 프로젝트의 기존 코딩 컨벤션 준수
- 주석 및 XML 문서화 철저
- 디버그 로그 적절히 활용

---

## 문서 정보

**문서 버전**: 1.0  
**작성일**: 2025년 10월 29일  
**상태**: 계획 수립 완료 (구현 대기)  
**예상 총 소요 시간**: 12~16시간  
**관련 문서**:
- `ActionCommand_장착_시스템_구현_명세서.md`
- `아이템_시스템_명세서.md`

