# Character Instance ID 시스템 설계

**작성일**: 2025-11-03  
**목적**: Character 데이터 구조의 명확한 분리 (템플릿 vs 인스턴스)

---

## 📋 문제 정의

### 기존 구조의 문제점

```
CharacterData (ScriptableObject)
├── characterId: "goblin_warrior" 
└── 초기 스탯, 아이템 등

문제:
1. characterId가 템플릿 Key인지 Instance ID인지 불명확
2. 동일한 템플릿을 사용하는 여러 인스턴스 생성 불가
3. Character 인스턴스에 고유 식별자가 없음
```

### 필요한 개념 분리

| 개념 | 설명 | 예시 |
|------|------|------|
| **템플릿 Key** | 초기화 데이터의 식별자 (재사용 가능) | `"goblin_warrior"`, `"orc_shaman"` |
| **Instance ID** | 개별 Character의 고유 식별자 | `"enemy_goblin_01"`, `"enemy_goblin_02"` |

---

## 🏗️ 새로운 구조

### 1. CharacterInitData (ScriptableObject)

**역할**: 캐릭터 초기화를 위한 템플릿 데이터

**파일**: `Assets/Script/CharacterInitData.cs`

```
CharacterInitData
├── key: string (예: "goblin_warrior") ← 템플릿 Key
├── characterName: string
├── baseStats: CombatStats
├── initialItems: List<InitialItemEntry>
├── initialEquipment: List<InitialEquipmentEntry>
└── initialAcquiredActions: List<InitialActionEntry>
```

**특징**:
- 여러 Character 인스턴스가 공유 가능
- 읽기 전용 (ScriptableObject)
- Resources 폴더에 저장

---

### 2. CharacterInstanceEntry (데이터 클래스)

**역할**: 개별 Character 인스턴스 정의

**구조**:
```csharp
[System.Serializable]
public class CharacterInstanceEntry
{
    public string instanceId;        // "enemy_goblin_01" (고유)
    public string initDataKey;       // "goblin_warrior" (템플릿 참조)
    public CharacterType type;       // Player or Enemy
}
```

---

### 3. CharacterDatabase (ScriptableObject)

**역할**: 게임 내 모든 Character 인스턴스 정의 테이블

**파일**: `Assets/Script/CharacterDatabase.cs`

```
CharacterDatabase (ScriptableObject)
├── playerEntry: CharacterInstanceEntry
│   ├── instanceId: "player"
│   └── initDataKey: "player_default"
│
└── enemyEntries: List<CharacterInstanceEntry>
    ├── [0] instanceId: "enemy_goblin_01", initDataKey: "goblin_warrior"
    ├── [1] instanceId: "enemy_goblin_02", initDataKey: "goblin_warrior"
    └── [2] instanceId: "boss_orc_chief", initDataKey: "orc_chief"
```

**특징**:
- 게임 내 모든 Character 인스턴스를 정의
- Instance ID → InitData Key 매핑
- 하나의 InitData를 여러 인스턴스가 참조 가능

---

### 4. CharacterInitDataRegistry (매니저)

**역할**: CharacterInitData 에셋 로드 및 관리

**파일**: `Assets/Script/CharacterInitDataRegistry.cs`

```
CharacterInitDataRegistry
├── initDataAssets: List<CharacterInitData>
├── registry: Dictionary<string, CharacterInitData>
└── GetInitData(key): CharacterInitData
```

---

### 5. Character 클래스 수정

**추가 필드**:
```csharp
public class Character
{
    public string InstanceId { get; private set; }  // 고유 ID
    // ... 기존 필드들
    
    public Character(string instanceId, CharacterInitData initData)
    {
        this.InstanceId = instanceId;
        // initData로 초기화
    }
}
```

---

## 🔄 데이터 흐름

### 전투 시작 시

```
1. CombatManager → CombatCharacterManager.InitializeBattle("enemy_goblin_01")

2. CombatCharacterManager:
   ├─ CharacterDatabase에서 "enemy_goblin_01" 조회
   │  → instanceId: "enemy_goblin_01"
   │  → initDataKey: "goblin_warrior"
   │
   ├─ CharacterInitDataRegistry에서 "goblin_warrior" 조회
   │  → CharacterInitData 반환
   │
   └─ Character 인스턴스 생성
      → new EnemyCharacter("enemy_goblin_01", initData)

3. Character 인스턴스:
   ├── InstanceId: "enemy_goblin_01"
   ├── Name: "Goblin Warrior"
   ├── HP, 스탯 등 (initData 기반)
   └── Inventory, Equipment (initData 기반 초기화)
```

---

## 📊 파일 구조

### Before (기존)

```
Assets/Script/
├── CharacterData.cs (혼란스러움)
├── CharacterManager.cs (역할 과다)
└── Character.cs
```

### After (새 구조)

```
Assets/Script/
├── Character.cs (instanceId 추가)
├── CharacterInitData.cs (템플릿)
├── CharacterInstanceEntry.cs (인스턴스 정의)
├── CharacterDatabase.cs (인스턴스 테이블)
├── CharacterInitDataRegistry.cs (템플릿 레지스트리)
├── PlayerCharacterManager.cs
└── Combat/
    └── CombatCharacterManager.cs
```

---

## 🎯 사용 예시

### Case 1: 플레이어 생성

```csharp
// CharacterDatabase
playerEntry = { instanceId: "player", initDataKey: "player_default" }

// 생성
CharacterInitData initData = CharacterInitDataRegistry.GetInitData("player_default");
PlayerCharacter player = new PlayerCharacter("player", initData);

// 접근
player.InstanceId // "player"
```

### Case 2: 동일 템플릿, 다른 인스턴스

```csharp
// CharacterDatabase
enemyEntries = [
    { instanceId: "enemy_goblin_01", initDataKey: "goblin_warrior" },
    { instanceId: "enemy_goblin_02", initDataKey: "goblin_warrior" }
]

// 생성
CharacterInitData goblinTemplate = CharacterInitDataRegistry.GetInitData("goblin_warrior");
EnemyCharacter goblin1 = new EnemyCharacter("enemy_goblin_01", goblinTemplate);
EnemyCharacter goblin2 = new EnemyCharacter("enemy_goblin_02", goblinTemplate);

// 두 인스턴스는 다른 객체
goblin1.InstanceId // "enemy_goblin_01"
goblin2.InstanceId // "enemy_goblin_02"
goblin1 != goblin2  // true
```

---

## 🔧 마이그레이션 계획

### Step 1: 리네이밍

| Before | After |
|--------|-------|
| `CharacterData.cs` | `CharacterInitData.cs` |
| `CharacterData.characterId` | `CharacterInitData.key` |
| `CharacterDatabase.cs` (기존) | `CharacterInitDataRegistry.cs` |

### Step 2: 새 파일 생성

- `CharacterInstanceEntry.cs`
- `CharacterDatabase.cs` (새 버전)

### Step 3: Character 클래스 수정

- `instanceId` 필드 추가
- 생성자 수정

### Step 4: 매니저 수정

- `PlayerCharacterManager`
- `CombatCharacterManager`
- `CombatManager`

### Step 5: 에셋 재설정

- CharacterData → CharacterInitData 리네이밍
- CharacterDatabase (새) 에셋 생성

---

## ✅ 개선 효과

| 항목 | Before | After |
|------|--------|-------|
| **템플릿 재사용** | ❌ 불가능 | ✅ 가능 (여러 인스턴스가 공유) |
| **인스턴스 식별** | ❌ 불명확 | ✅ 명확 (고유 ID) |
| **확장성** | ❌ 제한적 | ✅ 다중 적, 보스전 대응 |
| **명명 일관성** | ❌ 혼란 | ✅ 명확 (Key vs ID) |

---

**작성자**: AI Assistant  
**검토자**: (검토 후 기입)

