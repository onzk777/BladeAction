# BehaviorTreeContext 사용 예시

## 개요
`BehaviorTreeContext`는 BT 실행 중 모든 정보를 담는 컨테이너입니다. 이 문서에서는 Context의 각 데이터가 어떻게 조합되어 사용되는지 상세한 예시를 제공합니다.

---

## Context 데이터 구조

```csharp
public class BehaviorTreeContext
{
    // 전투 참가자
    public Combatant self;           // NPC 자신
    public Combatant target;         // 상대방 (플레이어)
    
    // 턴 정보
    public int currentTurn;          // 현재 턴 번호
    public bool isAttackTurn;        // 공격 턴 여부
    
    // 실행 결과 저장
    public Dictionary<string, float> probabilityOverrides;  // 확률 Override
    public int? selectedCommandIndex;                       // 선택된 검술 인덱스
    public string selectedCommandTag;                       // 선택된 검술 태그
    public string forcedBehavior;                           // 강제 행동 타입
}
```

---

## 시나리오 1: 위험한 상황에서의 NPC 행동

### BT 구성
```
Entry 0 (최우선):
├── 조건: "자신 HP < 20% AND 공격 턴"
└── 액션들:
    ├── "공격 성공률 100% 설정" (priority: 10)
    ├── "'필살기' 태그 검술 선택" (priority: 5)
    └── "막기 중 쳐내기 비활성화" (priority: 1)

Entry 1:
├── 조건: "상대 자세 > 80%"
└── 액션들:
    ├── "쳐내기 성공률 +30% 증가" (priority: 8)
    └── "막기 시도 확률 +20% 증가" (priority: 3)

Entry 2 (기본):
├── 조건: "항상"
└── 액션: "기본 확률 유지"
```

### Context 데이터 변화 과정

#### 1. 턴 시작 시 초기 상태
```csharp
// 전투 상황
npcCombatant.HP = 15;      // 15/100 (15%)
npcCombatant.MaxHP = 100;
npcCombatant.Poise = 40;   // 40/100 (40%)
npcCombatant.MaxPoise = 100;

playerCombatant.HP = 80;   // 80/100 (80%)
playerCombatant.MaxHP = 100;
playerCombatant.Poise = 90; // 90/100 (90%)
playerCombatant.MaxPoise = 100;

// Context 초기화
context = new BehaviorTreeContext {
    self = npcCombatant,
    target = playerCombatant,
    currentTurn = 5,
    isAttackTurn = true,
    probabilityOverrides = {},        // 비어있음
    selectedCommandIndex = null,
    selectedCommandTag = null,
    forcedBehavior = null
}
```

#### 2. Entry 0 평가 (조건 만족: HP < 20% AND 공격 턴)
```csharp
// 조건 평가
bool hpCondition = (15 / 100f) < 0.2f;  // true
bool turnCondition = true;               // 공격 턴
bool result = hpCondition && turnCondition; // true

// 액션 실행 (Priority 순서)
// 1. "공격 성공률 100% 설정" (priority: 10)
context.probabilityOverrides["AttackPerfectRate"] = 1.0f;

// 2. "'필살기' 태그 검술 선택" (priority: 5)
context.selectedCommandTag = "필살기";
context.selectedCommandIndex = null; // 태그 선택이므로 인덱스는 null

// 3. "막기 중 쳐내기 비활성화" (priority: 1)
context.probabilityOverrides["ParryWhileGuardingRate"] = 0.0f;

// 최종 Context 상태
context = {
    // ... 기존 데이터 ...
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f,        // 공격 성공률 100%
        "ParryWhileGuardingRate": 0.0f    // 막기 중 쳐내기 비활성화
    },
    selectedCommandTag = "필살기",
    selectedCommandIndex = null,
    forcedBehavior = null
}
```

#### 3. Entry 1 평가 (조건 만족: 상대 자세 > 80%)
```csharp
// 조건 평가
bool poiseCondition = (90 / 100f) > 0.8f; // true

// 액션 실행 (Priority 순서)
// 1. "쳐내기 성공률 +30% 증가" (priority: 8)
float currentParryRate = context.GetProbabilityOverride("ParryPerfectRate", 0.5f); // 기본값 0.5
context.probabilityOverrides["ParryPerfectRate"] = Mathf.Clamp01(currentParryRate + 0.3f); // 0.8

// 2. "막기 시도 확률 +20% 증가" (priority: 3)
float currentGuardRate = context.GetProbabilityOverride("GuardAttemptRate", 0.5f); // 기본값 0.5
context.probabilityOverrides["GuardAttemptRate"] = Mathf.Clamp01(currentGuardRate + 0.2f); // 0.7

// 최종 Context 상태
context = {
    // ... 기존 데이터 ...
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f,        // 기존 값 유지 (우선순위 높음)
        "ParryPerfectRate": 0.8f,         // 쳐내기 성공률 +30% (0.5 + 0.3)
        "GuardAttemptRate": 0.7f,         // 막기 시도 확률 +20% (0.5 + 0.2)
        "ParryWhileGuardingRate": 0.0f    // 기존 값 유지
    },
    selectedCommandTag = "필살기",        // 기존 값 유지
    selectedCommandIndex = null,
    forcedBehavior = null
}
```

#### 4. Entry 2 평가 (조건 만족: 항상)
```csharp
// 기본 확률 유지 액션 (실제로는 아무것도 하지 않음)
// Context 상태 변화 없음
```

### 최종 결과를 EnemyController에서 사용

```csharp
public void ApplyBehaviorTreeResults(BehaviorTreeContext context)
{
    // 1. 확률 Override 적용
    var npcBehavior = Combatant.CharacterData.npcBehavior;
    
    // 원본 확률 백업 (필요시)
    var originalRates = new Dictionary<string, float> {
        ["AttackPerfectRate"] = npcBehavior.attackPerfectRate,
        ["ParryPerfectRate"] = npcBehavior.parryPerfectRate,
        ["GuardAttemptRate"] = npcBehavior.guardAttemptRate,
        ["ParryWhileGuardingRate"] = npcBehavior.parryWhileGuardingRate
    };
    
    // BT 결과 적용
    foreach (var kvp in context.probabilityOverrides)
    {
        switch (kvp.Key)
        {
            case "AttackPerfectRate":
                runtimeBehavior.SetAttackPerfectRate(kvp.Value); // 1.0f
                break;
            case "ParryPerfectRate":
                runtimeBehavior.SetParryPerfectRate(kvp.Value); // 0.8f
                break;
            case "GuardAttemptRate":
                runtimeBehavior.SetGuardAttemptRate(kvp.Value); // 0.7f
                break;
            case "ParryWhileGuardingRate":
                runtimeBehavior.SetParryWhileGuardingRate(kvp.Value); // 0.0f
                break;
        }
    }
    
    // 2. 검술 선택 적용
    ActionCommandData selectedCommand = null;
    
    if (context.selectedCommandIndex.HasValue)
    {
        // 인덱스로 검술 선택
        int index = Mathf.Clamp(context.selectedCommandIndex.Value, 0, AvailableCommands.Count - 1);
        selectedCommand = AvailableCommands[index];
    }
    else if (!string.IsNullOrEmpty(context.selectedCommandTag))
    {
        // 태그로 검술 선택
        var filteredCommands = AvailableCommands
            .Where(cmd => cmd.HasTag(context.selectedCommandTag))
            .ToList();
        
        if (filteredCommands.Count > 0)
        {
            selectedCommand = filteredCommands[Random.Range(0, filteredCommands.Count)];
        }
    }
    
    if (selectedCommand != null)
    {
        this.selectedCommand = selectedCommand;
        Debug.Log($"[BT] 선택된 검술: {selectedCommand.name} (태그: {context.selectedCommandTag})");
    }
    
    // 3. 강제 행동 적용
    if (!string.IsNullOrEmpty(context.forcedBehavior))
    {
        ApplyForcedBehavior(context.forcedBehavior);
    }
    
    // 4. 디버그 로그
    Debug.Log($"[BT] 최종 확률 - 공격: {runtimeBehavior.GetAttackPerfectRate()}, " +
              $"쳐내기: {runtimeBehavior.GetParryPerfectRate()}, " +
              $"막기: {runtimeBehavior.GetGuardAttemptRate()}");
}
```

---

## 시나리오 2: 복잡한 조건 조합

### BT 구성
```
Entry 0:
├── 조건: "(자신 HP < 30% OR 자신 자세 < 20%) AND 상대 HP > 50%"
└── 액션들:
    ├── "공격 성공률 +50% 증가" (priority: 10)
    └── "'강공격' 태그 검술 선택" (priority: 5)

Entry 1:
├── 조건: "현재 턴 > 5 AND 공격 턴"
└── 액션들:
    ├── "쳐내기 성공률 +20% 증가" (priority: 8)
    └── "막기 시도 확률 +15% 증가" (priority: 3)
```

### Context 데이터 변화 과정

#### 1. 턴 시작 시 초기 상태
```csharp
// 전투 상황
npcCombatant.HP = 25;      // 25/100 (25%)
npcCombatant.Poise = 30;   // 30/100 (30%)
playerCombatant.HP = 70;   // 70/100 (70%)
currentTurn = 6;
isAttackTurn = true;

// Context 초기화
context = new BehaviorTreeContext {
    self = npcCombatant,
    target = playerCombatant,
    currentTurn = 6,
    isAttackTurn = true,
    probabilityOverrides = {},
    selectedCommandIndex = null,
    selectedCommandTag = null,
    forcedBehavior = null
}
```

#### 2. Entry 0 평가 (조건 만족)
```csharp
// 복합 조건 평가
// (자신 HP < 30% OR 자신 자세 < 20%) AND 상대 HP > 50%

// OR 조건 평가
bool hpCondition = (25 / 100f) < 0.3f;  // true
bool poiseCondition = (30 / 100f) < 0.2f; // false
bool orResult = hpCondition || poiseCondition; // true

// AND 조건 평가
bool targetHpCondition = (70 / 100f) > 0.5f; // true
bool finalResult = orResult && targetHpCondition; // true

// 액션 실행
context.probabilityOverrides["AttackPerfectRate"] = 0.5f + 0.5f; // 1.0f
context.selectedCommandTag = "강공격";
```

#### 3. Entry 1 평가 (조건 만족)
```csharp
// 조건 평가
bool turnCondition = 6 > 5; // true
bool attackTurnCondition = true; // 공격 턴
bool result = turnCondition && attackTurnCondition; // true

// 액션 실행
context.probabilityOverrides["ParryPerfectRate"] = 0.5f + 0.2f; // 0.7f
context.probabilityOverrides["GuardAttemptRate"] = 0.5f + 0.15f; // 0.65f
```

#### 4. 최종 Context 상태
```csharp
context = {
    // ... 기존 데이터 ...
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f,        // 공격 성공률 +50% (0.5 + 0.5)
        "ParryPerfectRate": 0.7f,         // 쳐내기 성공률 +20% (0.5 + 0.2)
        "GuardAttemptRate": 0.65f         // 막기 시도 확률 +15% (0.5 + 0.15)
    },
    selectedCommandTag = "강공격",
    selectedCommandIndex = null,
    forcedBehavior = null
}
```

---

## 시나리오 3: executeOnce 활용

### BT 구성
```
Entry 0:
├── 조건: "현재 턴 == 1"
└── 액션: "공격 성공률 +50% 증가" (executeOnce: true, priority: 10)

Entry 1:
├── 조건: "자신 HP < 10%"
└── 액션: "공격 성공률 100% 설정" (executeOnce: true, priority: 15)

Entry 2:
├── 조건: "상대 자세 < 30%"
└── 액션: "쳐내기 성공률 +30% 증가" (executeOnce: false, priority: 5)
```

### Context 데이터 변화 과정

#### 1턴째 실행
```csharp
// Entry 0 평가 (조건 만족: 턴 == 1)
context.probabilityOverrides["AttackPerfectRate"] = 0.5f + 0.5f; // 1.0f
// executeOnce = true이므로 hasExecuted = true로 설정

// Entry 1 평가 (조건 불만족: HP >= 10%)
// 실행 안됨

// Entry 2 평가 (조건 불만족: 상대 자세 >= 30%)
// 실행 안됨

// 최종 Context
context = {
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f
    },
    // ... 기타 데이터 ...
}
```

#### 2턴째 실행
```csharp
// Entry 0 평가 (조건 불만족: 턴 != 1)
// 실행 안됨

// Entry 1 평가 (조건 불만족: HP >= 10%)
// 실행 안됨

// Entry 2 평가 (조건 불만족: 상대 자세 >= 30%)
// 실행 안됨

// 최종 Context (변화 없음)
context = {
    probabilityOverrides = {},
    // ... 기타 데이터 ...
}
```

#### 5턴째 실행 (HP가 5%로 감소)
```csharp
// Entry 0 평가 (조건 불만족: 턴 != 1)
// 실행 안됨

// Entry 1 평가 (조건 만족: HP < 10%)
context.probabilityOverrides["AttackPerfectRate"] = 1.0f; // 100%
// executeOnce = true이므로 hasExecuted = true로 설정

// Entry 2 평가 (조건 불만족: 상대 자세 >= 30%)
// 실행 안됨

// 최종 Context
context = {
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f
    },
    // ... 기타 데이터 ...
}
```

#### 6턴째 실행 (상대 자세가 20%로 감소)
```csharp
// Entry 0 평가 (조건 불만족: 턴 != 1)
// 실행 안됨

// Entry 1 평가 (조건 만족: HP < 10%)
// executeOnce = true이고 hasExecuted = true이므로 실행 안됨

// Entry 2 평가 (조건 만족: 상대 자세 < 30%)
context.probabilityOverrides["ParryPerfectRate"] = 0.5f + 0.3f; // 0.8f
// executeOnce = false이므로 매번 실행

// 최종 Context
context = {
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f,        // 1턴째에 설정된 값 유지
        "ParryPerfectRate": 0.8f          // 6턴째에 새로 설정
    },
    // ... 기타 데이터 ...
}
```

---

## Context 병합 (다중 BT)

### 시나리오: 여러 BT가 동시에 실행되는 경우

```csharp
// BT_기본패턴 실행 결과
var basicContext = new BehaviorTreeContext {
    probabilityOverrides = {
        "AttackPerfectRate": 0.6f,
        "ParryPerfectRate": 0.4f
    },
    selectedCommandTag = "일반공격"
};

// BT_위험상황 실행 결과
var dangerContext = new BehaviorTreeContext {
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f,
        "GuardAttemptRate": 0.8f
    },
    selectedCommandTag = "필살기"
};

// Context 병합
var finalContext = new BehaviorTreeContext();
finalContext.MergeFrom(basicContext);
finalContext.MergeFrom(dangerContext);

// 최종 결과 (나중에 온 것이 우선)
finalContext = {
    probabilityOverrides = {
        "AttackPerfectRate": 1.0f,        // dangerContext 값 (나중에 온 것)
        "ParryPerfectRate": 0.4f,         // basicContext 값
        "GuardAttemptRate": 0.8f          // dangerContext 값
    },
    selectedCommandTag = "필살기",        // dangerContext 값 (나중에 온 것)
    // ... 기타 데이터 ...
}
```

---

## 디버깅 팁

### 1. Context 상태 확인
```csharp
public void LogContextState(BehaviorTreeContext context)
{
    Debug.Log($"[BT Context] 턴: {context.currentTurn}, 공격턴: {context.isAttackTurn}");
    Debug.Log($"[BT Context] 확률 Override: {context.probabilityOverrides.Count}개");
    foreach (var kvp in context.probabilityOverrides)
    {
        Debug.Log($"  {kvp.Key}: {kvp.Value:F2}");
    }
    Debug.Log($"[BT Context] 선택된 검술: {context.selectedCommandTag ?? "없음"}");
    Debug.Log($"[BT Context] 강제 행동: {context.forcedBehavior ?? "없음"}");
}
```

### 2. 조건 평가 로그
```csharp
public void LogConditionEvaluation(BTConditionNode condition, bool result, BehaviorTreeContext context)
{
    Debug.Log($"[BT Condition] {condition.name}: {result}" +
              $"\n  Self HP: {context.self.HP}/{context.self.MaxHP}" +
              $"\n  Target HP: {context.target.HP}/{context.target.MaxHP}" +
              $"\n  Turn: {context.currentTurn}, Attack: {context.isAttackTurn}");
}
```

---

**문서 버전**: 1.0  
**작성일**: 2025년 10월 2일  
**최종 수정일**: 2025년 10월 2일

