# ActionCommand 장착 시스템 구현 명세서

## 1. 시스템 개요

### 1.1 목적
캐릭터가 전투에서 사용할 검술(ActionCommand)을 선택하고 장착할 수 있는 시스템을 구현합니다. **기존 인벤토리 시스템을 활용**하여 검술도 아이템의 일종으로 관리합니다.

### 1.2 설계 철학

#### 검술 = 아이템
- 검술도 "획득 → 보유 → 장착 → 사용" 흐름을 따름
- 일반 아이템과 본질적으로 동일하며, 타입만 다를 뿐
- **기존 인벤토리 시스템(CharacterInventory)을 재사용**하여 코드 중복 제거

#### 통합의 이점
- ✅ 코드 재사용 (CRUD, 장착, 이벤트, 저장/로드)
- ✅ 시스템 일관성 (모든 소유물을 하나의 방식으로 관리)
- ✅ UI 통합 (하나의 인벤토리 UI에서 탭 필터링)
- ✅ 유지보수 용이 (하나의 시스템만 관리)

### 1.3 핵심 개념

#### 용어 정의
- **습득 검술(Acquired Actions)**: 인벤토리에 보관된 ItemType.ActionCommand 아이템들
- **유파(SwordArtStyle)**: 장비 슬롯에 장착하는 ItemType.SwordArtStyle 아이템
- **장착 검술(Equipped Actions)**: 4개의 검술 장비 슬롯(ActionSlot1~4)에 장착된 검술들
- **유파 검술**: 장착된 유파 아이템의 SwordArtStyleData가 제공하는 검술 목록

#### 시스템 구조
```
Character.Inventory (CharacterInventory)
│
├── items (List<OwnedItem>)
│   ├── ItemType.ActionCommand (습득 검술들)
│   ├── ItemType.Weapon
│   ├── ItemType.Armor
│   └── ItemType.SwordArtStyle (유파 아이템)
│
└── equipmentSlots (List<EquipmentSlot>)
    ├── Weapon (1개)
    ├── Armor (1개)
    ├── Accessory (3개)
    ├── SwordArtStyle (1개) ← 유파 장착
    └── ActionSlot1~4 (4개) ← 검술 장착
```

---

## 2. 데이터 구조 확장

### 2.1 ItemType 열거형 확장

#### 수정 전
```csharp
public enum ItemType
{
    Weapon,
    Armor,
    Accessory,
    SwordArtStyle
}
```

#### 수정 후
```csharp
public enum ItemType
{
    Weapon,
    Armor,
    Accessory,
    SwordArtStyle,
    ActionCommand,   // 추가: 검술 아이템 타입
    Consumable       // 향후 확장: 소모품
}
```

### 2.2 EquipmentSlotType 열거형 확장

#### 수정 전
```csharp
public enum EquipmentSlotType
{
    Weapon,
    Armor,
    Accessory,
    SwordArtStyle
}
```

#### 수정 후
```csharp
public enum EquipmentSlotType
{
    Weapon,
    Armor,
    Accessory,
    SwordArtStyle,
    ActionSlot1,     // 추가: 검술 슬롯 1
    ActionSlot2,     // 추가: 검술 슬롯 2
    ActionSlot3,     // 추가: 검술 슬롯 3
    ActionSlot4      // 추가: 검술 슬롯 4
}
```

### 2.3 Item 클래스 확장

기존 Item 클래스에 검술 데이터 참조 추가
```csharp
public class Item : ScriptableObject
{
    [Header("기본 정보")]
    public string itemKey;
    public string itemName;
    public string description;
    public ItemType itemType;
    
    // ... 기존 필드들 ...
    
    [Header("검술 데이터")]
    [Tooltip("이 아이템이 검술(ActionCommand)인 경우 ActionCommandData 참조")]
    public ActionCommandData actionCommandData;
    
    [Header("검술 유파 데이터")]
    [Tooltip("이 아이템이 유파(SwordArtStyle)인 경우 SwordArtStyleData 참조")]
    public SwordArtStyleData swordArtStyleData;
    
    // ... 기존 필드들 ...
}
```

### 2.4 EquipmentSlot 검증 로직 확장

```csharp
public class EquipmentSlot
{
    // ... 기존 필드들 ...
    
    /// <summary>
    /// 아이템이 이 슬롯에 장착 가능한지 확인
    /// </summary>
    public bool CanEquipItem(string itemKey)
    {
        var item = ItemDatabase.GetItemSafe(itemKey);
        if (item == null)
            return false;
        
        // 슬롯 타입과 아이템 타입 매칭
        switch (slotType)
        {
            case EquipmentSlotType.Weapon:
                return item.itemType == ItemType.Weapon;
                
            case EquipmentSlotType.Armor:
                return item.itemType == ItemType.Armor;
                
            case EquipmentSlotType.Accessory:
                return item.itemType == ItemType.Accessory;
                
            case EquipmentSlotType.SwordArtStyle:
                return item.itemType == ItemType.SwordArtStyle;
                
            case EquipmentSlotType.ActionSlot1:
            case EquipmentSlotType.ActionSlot2:
            case EquipmentSlotType.ActionSlot3:
            case EquipmentSlotType.ActionSlot4:
                return item.itemType == ItemType.ActionCommand;
                
            default:
                return false;
        }
    }
}
```

---

## 3. CharacterInventory 확장

### 3.1 장비 슬롯 초기화 수정

```csharp
namespace BladeAction.Item
{
    public class CharacterInventory
    {
        // ... 기존 필드들 ...
        
        /// <summary>
        /// 기본 장비 슬롯 초기화
        /// 총 10개 슬롯: 무기 1, 갑옷 1, 장신구 3, 검술 유파 1, 검술 4
        /// </summary>
        private void InitializeDefaultEquipmentSlots()
        {
            equipmentSlots.Clear();
            
            // 기존 장비 슬롯
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Weapon, "주무기"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Armor, "갑옷"));
            
            // 장신구 슬롯 3개
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구1"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구2"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구3"));
            
            // 검술 유파 슬롯
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.SwordArtStyle, "검술 유파"));
            
            // 검술 슬롯 4개 추가
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot1, "검술 슬롯 1"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot2, "검술 슬롯 2"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot3, "검술 슬롯 3"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.ActionSlot4, "검술 슬롯 4"));
        }
    }
}
```

### 3.2 유파 해제 시 검술 자동 해제 로직

```csharp
namespace BladeAction.Item
{
    public class CharacterInventory
    {
        // ... 기존 메서드들 ...
        
        /// <summary>
        /// 아이템 해제
        /// </summary>
        public bool UnequipItem(EquipmentSlotType slotType)
        {
            if (isLocked)
                return false;
                
            var slot = equipmentSlots.FirstOrDefault(s => s.slotType == slotType);
            if (slot == null || slot.IsEmpty())
                return false;
                
            string unequippedKey = slot.UnequipItem();
            if (!string.IsNullOrEmpty(unequippedKey))
            {
                // 해제된 아이템을 인벤토리에 다시 추가
                bool success = AddItem(unequippedKey, 1);
                if (success)
                {
                    SafeTriggerEvent(events => events.TriggerItemUnequipped(unequippedKey, slotType, inventoryName));
                    
                    // 유파 해제 시 유파 검술 자동 해제
                    if (slotType == EquipmentSlotType.SwordArtStyle)
                    {
                        UnequipAllStyleActions(unequippedKey);
                    }
                    
                    // 스탯 재계산 트리거
                    TriggerStatsRecalculation();
                }
                return success;
            }
            
            return false;
        }
        
        /// <summary>
        /// 특정 유파의 모든 검술 해제 (유파 해제 시 자동 호출)
        /// </summary>
        private void UnequipAllStyleActions(string styleItemKey)
        {
            // 유파 아이템에서 SwordArtStyleData 가져오기
            var styleItem = ItemDatabase.GetItemSafe(styleItemKey);
            if (styleItem?.swordArtStyleData == null)
                return;
            
            var styleData = styleItem.swordArtStyleData;
            var styleActions = styleData.ActionCommands;
            
            if (styleActions == null || styleActions.Count == 0)
                return;
            
            // 검술 슬롯 순회하며 유파 검술 해제
            int unequippedCount = 0;
            foreach (var actionSlotType in new[] { 
                EquipmentSlotType.ActionSlot1, 
                EquipmentSlotType.ActionSlot2,
                EquipmentSlotType.ActionSlot3, 
                EquipmentSlotType.ActionSlot4 
            })
            {
                var actionSlot = equipmentSlots.FirstOrDefault(s => s.slotType == actionSlotType);
                if (actionSlot != null && !actionSlot.IsEmpty())
                {
                    var equippedItem = actionSlot.GetEquippedItem();
                    if (equippedItem?.actionCommandData != null && 
                        styleActions.Contains(equippedItem.actionCommandData))
                    {
                        // 이 검술은 유파 출처이므로 해제
                        string actionItemKey = actionSlot.UnequipItem();
                        if (!string.IsNullOrEmpty(actionItemKey))
                        {
                            // 인벤토리에 다시 추가
                            AddItem(actionItemKey, 1);
                            SafeTriggerEvent(events => events.TriggerItemUnequipped(actionItemKey, actionSlotType, inventoryName));
                            
                            unequippedCount++;
                            Debug.Log($"[CharacterInventory] 유파 해제로 인한 검술 자동 해제: {equippedItem.itemName} (슬롯: {actionSlotType})");
                        }
                    }
                }
            }
            
            if (unequippedCount > 0)
            {
                Debug.Log($"[CharacterInventory] 유파 '{styleItem.itemName}' 해제로 인해 {unequippedCount}개 검술 자동 해제");
            }
        }
        
        /// <summary>
        /// 습득 검술 목록 가져오기 (인벤토리에 있는 ActionCommand 타입 아이템들)
        /// </summary>
        public List<Item> GetAcquiredActions()
        {
            return items
                .Where(ownedItem => {
                    var item = ownedItem.GetItemData();
                    return item?.itemType == ItemType.ActionCommand;
                })
                .Select(ownedItem => ownedItem.GetItemData())
                .Where(item => item != null)
                .ToList();
        }
        
        /// <summary>
        /// 장착된 유파의 검술 목록 가져오기
        /// </summary>
        public List<ActionCommandData> GetEquippedStyleActions()
        {
            var styleSlot = equipmentSlots.FirstOrDefault(s => s.slotType == EquipmentSlotType.SwordArtStyle);
            if (styleSlot == null || styleSlot.IsEmpty())
                return new List<ActionCommandData>();
            
            var styleItem = styleSlot.GetEquippedItem();
            if (styleItem?.swordArtStyleData == null)
                return new List<ActionCommandData>();
            
            return styleItem.swordArtStyleData.GetActionCommands();
        }
        
        /// <summary>
        /// 장착된 검술 목록 가져오기 (4개 슬롯)
        /// </summary>
        public List<ActionCommandData> GetEquippedActions()
        {
            var equippedActions = new List<ActionCommandData>();
            
            foreach (var slotType in new[] { 
                EquipmentSlotType.ActionSlot1, 
                EquipmentSlotType.ActionSlot2,
                EquipmentSlotType.ActionSlot3, 
                EquipmentSlotType.ActionSlot4 
            })
            {
                var slot = equipmentSlots.FirstOrDefault(s => s.slotType == slotType);
                if (slot != null && !slot.IsEmpty())
                {
                    var item = slot.GetEquippedItem();
                    if (item?.actionCommandData != null)
                    {
                        equippedActions.Add(item.actionCommandData);
                    }
                }
            }
            
            return equippedActions;
        }
    }
}
```

---

## 4. 검술 아이템 생성 가이드

### 4.1 에디터에서 검술 아이템 생성

#### 방법 1: 수동 생성 (기본)
1. `Assets/Resources/Data/Items/` 폴더에서 우클릭
2. `Create > Item > Item` 선택
3. Inspector에서 설정:
   - `itemKey`: 고유 키 (예: "action_basic_slash")
   - `itemName`: 표시 이름 (예: "기본 베기")
   - `itemType`: `ActionCommand` 선택
   - `actionCommandData`: 해당하는 ActionCommandData SO 할당
   - `icon`: 검술 아이콘 스프라이트 할당
   - `maxStack`: 1 (검술은 스택 불가)

#### 방법 2: 자동 생성 스크립트 (권장)
```csharp
#if UNITY_EDITOR
public static class ActionCommandItemGenerator
{
    [MenuItem("BladeAction/Generate Action Items")]
    public static void GenerateActionItems()
    {
        // Assets/Resources/Data/ActionData 폴더의 모든 ActionCommandData 찾기
        string[] guids = AssetDatabase.FindAssets("t:ActionCommandData", new[] { "Assets/Resources/Data/ActionData" });
        
        int generatedCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var actionData = AssetDatabase.LoadAssetAtPath<ActionCommandData>(path);
            
            if (actionData == null)
                continue;
            
            // 이미 Item이 존재하는지 확인
            string itemKey = $"action_{actionData.name}";
            string itemPath = $"Assets/Resources/Data/Items/Actions/{actionData.name}_Item.asset";
            
            if (AssetDatabase.LoadAssetAtPath<Item>(itemPath) != null)
            {
                Debug.Log($"이미 존재하는 아이템: {itemPath}");
                continue;
            }
            
            // Item SO 생성
            var item = ScriptableObject.CreateInstance<Item>();
            item.itemKey = itemKey;
            item.itemName = actionData.commandName;
            item.description = $"{actionData.commandName} 검술";
            item.itemType = ItemType.ActionCommand;
            item.actionCommandData = actionData;
            item.maxStack = 1; // 검술은 스택 불가
            
            // 저장
            string directory = Path.GetDirectoryName(itemPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            AssetDatabase.CreateAsset(item, itemPath);
            generatedCount++;
            Debug.Log($"검술 아이템 생성: {itemPath}");
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"총 {generatedCount}개 검술 아이템 생성 완료!");
    }
}
#endif
```

### 4.2 CSV를 통한 검술 아이템 관리

기존 ItemTable CSV에 검술 아이템 추가 가능
```csv
itemKey,itemName,itemType,description,maxStack,actionCommandKey,iconPath
action_basic_slash,기본 베기,ActionCommand,기본적인 베기 공격,1,BasicSlash,icon_action_slash
action_heavy_strike,강타,ActionCommand,강력한 일격,1,HeavyStrike,icon_action_heavy
action_quick_thrust,빠른 찌르기,ActionCommand,신속한 찌르기 공격,1,QuickThrust,icon_action_thrust
```

---

## 5. Character 클래스 통합

### 5.1 기존 코드 정리

#### 제거할 코드
```csharp
// Character.cs에서 제거
public IReadOnlyList<ActionCommandData> AvailableCommands => _availableCommands;
private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

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

#### 대체 코드
```csharp
// Character.cs 수정
public abstract class Character
{
    // ... 기존 필드들 ...
    
    /// <summary>
    /// 전투에서 사용 가능한 검술 목록 (장착된 4개)
    /// </summary>
    public List<ActionCommandData> AvailableCommands => Inventory?.GetEquippedActions() ?? new List<ActionCommandData>();
    
    // ... 기존 메서드들 ...
}
```

### 5.2 전투 시스템 수정

```csharp
// CombatManager.cs, PlayerController.cs, EnemyController.cs 등
public class PlayerController : MonoBehaviour, ICombatController
{
    // 기존: Character.AvailableCommands 사용
    // 변경: 동일하게 사용 (내부 구현만 변경됨)
    
    public int CommandCount => Character?.AvailableCommands.Count ?? 0;
    
    public ActionCommandData GetCurrentActionCommand(int commandIndex)
    {
        var commands = Character?.AvailableCommands;
        if (commands == null || commandIndex < 0 || commandIndex >= commands.Count)
            return null;
        
        return commands[commandIndex];
    }
}
```

---

## 6. UI 시스템 설계 (ActionCommandEquip)

### 6.1 전체 화면 구조

```
┌────────────────────────────────────────────────────────────────────────┐
│  [가방]  [검술]  [ ]  [ ]  [ ]  [ ]  [ ]  [ ]  ← 상단 탭 메뉴           │
├─────────────────┬──────────────────────────┬───────────────────────────┤
│                 │                          │                           │
│  좌측 패널      │    중앙 패널             │   우측 패널               │
│                 │                          │                           │
│  - 유파 정보    │  - 검술 목록 (서브탭)    │  - 캐릭터 스테이터스      │
│  - 장착 슬롯    │  - 검술 상세 정보        │                           │
│                 │    (선택 시 표시)        │                           │
│                 │                          │                           │
└─────────────────┴──────────────────────────┴───────────────────────────┘
```

### 6.2 "검술" 탭 상세 레이아웃

#### 6.2.1 좌측 패널: 유파 정보 + 장착 검술 슬롯

```
┌─────────────────────────────┐
│ [좌측 패널]                 │
│                             │
│ ┌─────────────────────────┐ │
│ │ 유파 정보 영역          │ │
│ │                         │ │
│ │ [Icon]  유파 이름       │ │
│ │                         │ │
│ │ 유파 설명 텍스트...     │ │
│ │                         │ │
│ │ ┌─────────────────────┐ │ │
│ │ │ 유파 패시브 효과    │ │ │
│ │ │ (붉은색 박스)       │ │ │
│ │ │                     │ │ │
│ │ │ Normal Attack +10%  │ │ │
│ │ │ 막기 효율 +10%      │ │ │
│ │ │ 쳐내기 효율 -15%    │ │ │
│ │ │ 쳐내기 자세감소 +5  │ │ │
│ │ └─────────────────────┘ │ │
│ └─────────────────────────┘ │
│                             │
│ ┌─────────────────────────┐ │
│ │ 장착 검술 슬롯          │ │
│ │                         │ │
│ │ [검술 1] 제국군-내려베기│ │
│ │ [검술 2] 검술 이름      │ │
│ │ [검술 3] (비어있음)     │ │
│ │ [검술 4] (비어있음)     │ │
│ └─────────────────────────┘ │
└─────────────────────────────┘
```

**구성 요소**:
- **상단: 유파 정보 영역**
  - 유파 아이콘
  - 유파 이름
  - 유파 설명
  - 유파 패시브 효과 (붉은색 박스)
    - 유파 아이템 자체가 제공하는 패시브 효과
    - 검술과 무관한 별도 기능 (향후 구현)
  - **유파 미장착 시**: "유파를 장착하세요" 안내 메시지 표시

- **하단: 장착 검술 슬롯 (4개)**
  - 각 슬롯에 장착된 검술 이름 표시
  - 빈 슬롯은 "(비어있음)" 표시
  - 클릭 시 해당 검술이 중앙 패널에서 선택됨

#### 6.2.2 중앙 패널: 검술 목록 + 검술 상세 정보

```
┌─────────────────────────────────────┐
│ [중앙 패널]                         │
│                                     │
│ [습득 검술 (n개)] [유파 검술 (n개)]│ ← 서브 탭
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ 검술 목록 (스크롤 가능)         │ │
│ │                                 │ │
│ │ ┌─────────────────────────────┐ │ │
│ │ │ 제국군 정규 검술 - 내려베기 │ │ │ ← 선택됨 (노란색)
│ │ └─────────────────────────────┘ │ │
│ │ [ 검술 이름 ]                   │ │
│ │ [ 검술 이름 ]                   │ │
│ │ [ 검술 이름 ]                   │ │
│ │ [ 검술 이름 ]                   │ │
│ │ [ 검술 이름 ]                   │ │
│ └─────────────────────────────────┘ │
│                                     │
│ ─────────────────────────────────── │ ← 선택 시에만 하단 표시
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ 선택된 검술 상세 정보           │ │
│ │ (ActionCommandDetailPanel)      │ │
│ │                                 │ │
│ │ [검술 설명] [전투 정보] ← 토글  │ │
│ │                                 │ │
│ │ ┌─────────────────────────────┐ │ │
│ │ │ 전투 정보 (주황색 박스)     │ │ │
│ │ │                             │ │ │
│ │ │ 공격 횟수 : 2번             │ │ │
│ │ │ 1타 공격 : 80% 피해         │ │ │
│ │ │ 2타 공격 : 120% 피해        │ │ │
│ │ └─────────────────────────────┘ │ │
│ │                                 │ │
│ │ [검술 설명 보기] [장착 해제]    │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

**구성 요소**:
- **상단: 서브 탭**
  - "습득 검술 (n개)": 인벤토리의 ActionCommand 아이템 목록
  - "유파 검술 (n개)": 장착된 유파가 제공하는 검술 목록

- **중단: 검술 목록**
  - 서브 탭에 따라 필터링된 검술 리스트
  - 선택된 검술은 노란색 하이라이트
  - 클릭 시 하단 상세 정보 패널 표시

- **하단: 선택된 검술 상세 정보 (ActionCommandDetailPanel)**
  - **검술 미선택 시**: 이 패널이 숨겨지고 상단 목록이 하단까지 확장됨
  - **검술 선택 시**: 패널 표시
  
  - **토글 버튼**: "검술 설명 보기" ↔ "전투 정보 보기"
    - **검술 설명**: ActionCommandData.description (텍스트)
    - **전투 정보**: ActionCommandData 기반 (주황색 박스)
      - 공격 횟수 (numberOfAttacks)
      - 각 타수별 피해량 배율 (damageMultipliers)
      - 기타 전투 수치들
  
  - **액션 버튼**:
    - **"장착" 버튼**: 선택된 검술이 장착되지 않은 경우
      - 클릭 시 빈 슬롯 또는 4번 슬롯에 장착
      - 4개 슬롯이 모두 찬 경우 → 4번 슬롯 대체
    - **"장착 해제" 버튼**: 선택된 검술이 이미 장착된 경우
      - 클릭 시 해당 슬롯에서 해제

#### 6.2.3 우측 패널: 캐릭터 스테이터스

```
┌─────────────────────────┐
│ [우측 패널]             │
│                         │
│ ┌─────────────────────┐ │
│ │                     │ │
│ │  캐릭터             │ │
│ │  스테이터스         │ │
│ │  정보               │ │
│ │                     │ │
│ │  (파란색 배경)      │ │
│ │                     │ │
│ └─────────────────────┘ │
└─────────────────────────┘
```

**구성 요소**:
- 캐릭터의 현재 스탯 정보 표시
- 다른 탭에서도 유지되는 고정 UI 요소

### 6.3 UI 상호작용 로직

#### 6.3.1 검술 선택
```csharp
// 좌측 패널: 장착 슬롯 클릭
OnEquippedActionSlotClick(int slotIndex)
{
    var equippedAction = inventory.GetEquippedActions()[slotIndex];
    if (equippedAction != null)
    {
        SelectAction(equippedAction);
        ShowActionDetailPanel(equippedAction);
    }
}

// 중앙 패널: 검술 목록 클릭
OnActionListItemClick(ActionCommandData actionData)
{
    SelectAction(actionData);
    ShowActionDetailPanel(actionData);
}
```

#### 6.3.2 검술 상세 정보 토글
```csharp
private bool isShowingCombatInfo = true; // 기본: 전투 정보 표시

void OnToggleDetailButton()
{
    isShowingCombatInfo = !isShowingCombatInfo;
    
    if (isShowingCombatInfo)
    {
        // 전투 정보 표시
        ShowCombatInfo(selectedAction);
        toggleButton.text = "검술 설명 보기";
    }
    else
    {
        // 검술 설명 표시
        ShowActionDescription(selectedAction);
        toggleButton.text = "전투 정보 보기";
    }
}

void ShowCombatInfo(ActionCommandData action)
{
    // ActionCommandData에서 전투 정보 읽기
    combatInfoText.text = $"공격 횟수 : {action.numberOfAttacks}번\n";
    
    for (int i = 0; i < action.damageMultipliers.Count; i++)
    {
        float percentage = action.damageMultipliers[i] * 100f;
        combatInfoText.text += $"{i+1}타 공격 : {percentage}% 피해\n";
    }
    
    // 기타 전투 수치 추가...
}

void ShowActionDescription(ActionCommandData action)
{
    descriptionText.text = action.description;
}
```

#### 6.3.3 검술 장착/해제
```csharp
void OnEquipButton()
{
    if (selectedAction == null)
        return;
    
    // 빈 슬롯 찾기
    int emptySlotIndex = FindEmptyActionSlot();
    
    if (emptySlotIndex != -1)
    {
        // 빈 슬롯에 장착
        inventory.EquipItem(selectedActionItem.itemKey, 
            (EquipmentSlotType)((int)EquipmentSlotType.ActionSlot1 + emptySlotIndex));
    }
    else
    {
        // 4개 슬롯이 모두 참 → 4번 슬롯 대체
        inventory.UnequipItem(EquipmentSlotType.ActionSlot4);
        inventory.EquipItem(selectedActionItem.itemKey, EquipmentSlotType.ActionSlot4);
    }
    
    RefreshUI();
}

void OnUnequipButton()
{
    if (selectedAction == null)
        return;
    
    // 선택된 검술이 어느 슬롯에 장착되어 있는지 찾기
    for (int i = 0; i < 4; i++)
    {
        var slotType = (EquipmentSlotType)((int)EquipmentSlotType.ActionSlot1 + i);
        var equippedAction = inventory.GetEquippedItem(slotType);
        
        if (equippedAction?.actionCommandData == selectedAction)
        {
            inventory.UnequipItem(slotType);
            break;
        }
    }
    
    RefreshUI();
}

int FindEmptyActionSlot()
{
    for (int i = 0; i < 4; i++)
    {
        var slotType = (EquipmentSlotType)((int)EquipmentSlotType.ActionSlot1 + i);
        if (inventory.GetEquippedItem(slotType) == null)
            return i;
    }
    return -1;
}
```

#### 6.3.4 검술 미선택 시 레이아웃
```csharp
void OnActionDeselected()
{
    // ActionCommandDetailPanel 숨김
    actionDetailPanel.SetActive(false);
    
    // 검술 목록 영역 확장 (Layout Group의 Flexible Height 활용)
    // Unity Layout System이 자동으로 처리
}

void OnActionSelected(ActionCommandData action)
{
    // ActionCommandDetailPanel 표시
    actionDetailPanel.SetActive(true);
    
    // 검술 목록 영역 축소 (Layout Group이 자동 처리)
}
```

### 6.4 UI 필터링 로직

```csharp
public class InventoryUI : MonoBehaviour
{
    public enum InventoryTab
    {
        Equipment,   // 가방 (소지품)
        Actions      // 검술
    }
    
    public enum ActionSubTab
    {
        Acquired,    // 습득 검술
        Style        // 유파 검술
    }
    
    private InventoryTab currentTab = InventoryTab.Equipment;
    private ActionSubTab currentActionSubTab = ActionSubTab.Acquired;
    private ActionCommandData selectedAction = null;
    
    /// <summary>
    /// 현재 탭에 맞는 아이템 필터링
    /// </summary>
    private List<Item> GetFilteredItems()
    {
        var inventory = CharacterManager.Instance.PlayerCharacter.Inventory;
        
        if (currentTab == InventoryTab.Equipment)
        {
            // 가방 탭: ActionCommand 제외
            return inventory.GetItemsByType(ItemType.ActionCommand, exclude: true);
        }
        else // currentTab == InventoryTab.Actions
        {
            if (currentActionSubTab == ActionSubTab.Acquired)
            {
                // 습득 검술 탭: ActionCommand만
                return inventory.GetAcquiredActions();
            }
            else // currentActionSubTab == ActionSubTab.Style
            {
                // 유파 검술 탭: 장착된 유파의 검술
                var styleActions = inventory.GetEquippedStyleActions();
                return ConvertToItemList(styleActions); // ActionCommandData → Item 변환
            }
        }
    }
    
    /// <summary>
    /// 장착 버튼 텍스트 업데이트
    /// </summary>
    private void UpdateEquipButtonText()
    {
        if (selectedAction == null)
        {
            equipButton.gameObject.SetActive(false);
            return;
        }
        
        equipButton.gameObject.SetActive(true);
        
        // 선택된 검술이 장착되어 있는지 확인
        bool isEquipped = IsActionEquipped(selectedAction);
        
        if (isEquipped)
        {
            equipButton.GetComponentInChildren<Text>().text = "장착 해제";
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnUnequipButton);
        }
        else
        {
            equipButton.GetComponentInChildren<Text>().text = "장착";
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButton);
        }
    }
    
    private bool IsActionEquipped(ActionCommandData action)
    {
        var equippedActions = inventory.GetEquippedActions();
        return equippedActions.Any(ea => ea == action);
    }
}
```

---

## 7. 구현 체크리스트

### 7.1 Phase 1: 데이터 구조 확장 (필수)
- [ ] `ItemType`에 `ActionCommand` 추가
- [ ] `EquipmentSlotType`에 `ActionSlot1~4` 추가
- [ ] `Item` 클래스에 `actionCommandData` 필드 추가
- [ ] `EquipmentSlot.CanEquipItem()` 검증 로직 확장

### 7.2 Phase 2: 인벤토리 시스템 확장 (필수)
- [ ] `CharacterInventory.InitializeDefaultEquipmentSlots()` 수정 (검술 슬롯 4개 추가)
- [ ] `CharacterInventory.UnequipAllStyleActions()` 구현
- [ ] `CharacterInventory.UnequipItem()` 수정 (유파 해제 시 콜백)
- [ ] `CharacterInventory.GetAcquiredActions()` 구현
- [ ] `CharacterInventory.GetEquippedStyleActions()` 구현
- [ ] `CharacterInventory.GetEquippedActions()` 구현

### 7.3 Phase 3: Character 클래스 정리 (필수)
- [ ] `Character._availableCommands` 제거
- [ ] `Character.EquipSwordArtStyle()` 제거 또는 수정
- [ ] `Character.AvailableCommands` 프로퍼티 수정 (Inventory 기반)

### 7.4 Phase 4: 검술 아이템 생성 (필수)
- [ ] 에디터 도구: `ActionCommandItemGenerator` 스크립트 작성
- [ ] 기존 ActionCommandData에 대응하는 Item SO 생성
- [ ] ItemDatabase에 검술 아이템 등록

### 7.5 Phase 5: UI 구현 (필수)
- [ ] 인벤토리 UI에 "검술" 탭 추가
- [ ] 검술 탭 UI 레이아웃 구성
- [ ] 습득 검술 / 유파 검술 서브 탭
- [ ] 검술 슬롯 UI (4개)
- [ ] 드래그 앤 드롭 기능 (검술 → 슬롯)
- [ ] 필터링 로직 구현

### 7.6 Phase 6: CSV 통합 (선택)
- [ ] ItemTable CSV에 ActionCommand 항목 추가
- [ ] CSV Import 시 actionCommandData 자동 매핑
- [ ] CSV Export 시 검술 아이템 포함

### 7.7 Phase 7: 테스트 (필수)
- [ ] 검술 아이템 추가/제거 테스트
- [ ] 검술 장착/해제 테스트
- [ ] 유파 장착 시 유파 검술 접근 테스트
- [ ] 유파 해제 시 검술 자동 해제 테스트
- [ ] 전투 시스템에서 장착 검술 사용 테스트

---

## 8. 테스트 케이스

### 8.1 기본 기능 테스트
1. **검술 아이템 획득**: 인벤토리에 ActionCommand 아이템 추가
2. **검술 아이템 장착**: 검술 슬롯에 드래그 앤 드롭 또는 더블클릭
3. **검술 아이템 해제**: 장착 검술 우클릭 → 해제
4. **습득 검술 확인**: `Inventory.GetAcquiredActions()` 호출

### 8.2 유파 연동 테스트
1. **유파 장착**: 유파 아이템을 유파 슬롯에 장착
2. **유파 검술 접근**: `Inventory.GetEquippedStyleActions()` 호출하여 목록 확인
3. **유파 검술 장착**: 유파 검술을 검술 슬롯에 장착
4. **유파 해제 후 검술 확인**: 유파 해제 시 유파 검술만 자동 해제, 습득 검술은 유지

### 8.3 전투 시스템 통합 테스트
1. **AvailableCommands 확인**: `Character.AvailableCommands` 호출하여 장착 검술 4개 확인
2. **전투 중 검술 사용**: 장착된 검술이 전투 UI에 정상 표시되는지 확인
3. **검술 변경 후 전투**: 검술 재장착 후 전투 재진입 시 반영 확인

### 8.4 UI 테스트
1. **탭 전환**: 소지품 ↔ 검술 탭 전환 시 필터링 확인
2. **서브 탭 전환**: 습득 검술 ↔ 유파 검술 전환
3. **드래그 앤 드롭**: 검술 아이템을 검술 슬롯에 드래그
4. **슬롯 표시**: 4개 검술 슬롯이 올바르게 표시되는지 확인

---

## 9. 주의사항 및 고려사항

### 9.1 검술 아이템의 고유성
- 검술 아이템은 `maxStack = 1`로 설정 (스택 불가)
- 동일한 검술을 여러 개 보유할 수 없음
- 단, 동일 검술을 여러 슬롯에 장착하는 것은 허용할지 정책 결정 필요

### 9.2 유파 검술의 참조 방식
- 유파 검술은 인벤토리에 실제 아이템으로 존재하지 않음
- UI에서 표시 시 SwordArtStyleData에서 직접 가져와 임시 표시
- 장착 시 해당 ActionCommandData에 대응하는 Item이 필요함
  - **해결 방안 1**: 유파 검술도 미리 Item으로 생성하여 ItemDatabase에 등록
  - **해결 방안 2**: 런타임에 임시 Item 래퍼 생성 (권장하지 않음)

### 9.3 검술 슬롯 수 변경
- 기본 4개로 설정했지만, 향후 변경 가능성 고려
- EquipmentSlotType에 슬롯 수만큼 열거형 추가 필요
- 동적 슬롯 수 변경은 복잡도가 높아 초기 버전에서는 고정 권장

### 9.4 전투 시스템 호환성
- 기존 `Character.AvailableCommands`를 사용하는 모든 코드는 수정 없이 작동
- 내부 구현만 변경되므로 하위 호환성 유지

---

## 10. 향후 확장 가능성

### 10.1 검술 레벨 및 숙련도 시스템
- Item에 `level`, `experience` 필드 추가
- 전투 중 사용 시 경험치 획득
- 레벨업 시 성능 향상

### 10.2 검술 조합 시스템
- 특정 검술 조합 시 세트 효과 발동
- 유파 검술 + 습득 검술 조합 보너스

### 10.3 검술 커스터마이징
- 검술 강화 (공격력, 속도 등 증가)
- 검술 각인 (특수 효과 부여)
- 검술 변형 (모션 변경)

### 10.4 검술 거래 시스템
- NPC와 검술 아이템 거래
- 검술 도감 시스템

---

## 11. 기존 시스템과의 차이점 요약

| 항목 | 기존 시스템 | 새 시스템 (통합) |
|------|------------|------------------|
| 검술 관리 | `Character._availableCommands` | `CharacterInventory.items` |
| 검술 출처 | `SwordArtStyleData`만 | 습득 + 유파 |
| 장착 방식 | `EquipSwordArtStyle()` 자동 | 사용자가 수동 장착 |
| 장착 슬롯 | 없음 (유파 장착 = 검술 사용 가능) | 4개 검술 슬롯 |
| 데이터 타입 | `ActionCommandData` 직접 | `Item` (ActionCommand 타입) |
| UI | 전투 UI에만 표시 | 인벤토리 UI 통합 |

---

## 문서 정보

**문서 버전**: 2.0 (통합 방식)  
**작성일**: 2025년 10월 29일  
**최종 수정일**: 2025년 10월 29일  
**상태**: 설계 완료 (구현 대기)  
**관련 문서**:
- `아이템_시스템_명세서.md`
- `인벤토리_시스템_테스트_시나리오.md`

### 주요 변경사항 (v2.0)
- **별도 시스템에서 통합 시스템으로 전환**
- ActionCommandInventory 제거 → CharacterInventory 활용
- 검술을 ItemType.ActionCommand로 아이템화
- 검술 슬롯 4개 추가 (EquipmentSlotType)
- 유파 해제 시 검술 자동 해제 로직 추가
