# Composite Node 메뉴얼

## 개요
Composite Node는 여러 Condition Node를 논리 연산자(AND/OR)로 조합하여 복잡한 조건을 만드는 노드입니다.

---

## 1. Sequence Node (AND 조건)

### 개념
- **모든 자식 Condition이 true일 때만** 연결된 Action 실행
- 논리 AND 연산과 동일
- 하나라도 false면 전체 false

### 구조
```yaml
SequenceNode:
  Children:
    - Condition1
    - Condition2
    - Condition3
  ConnectedActions:
    - Action1
    - Action2
```

### 평가 방식
1. 자식 Condition을 순서대로 평가
2. **하나라도 false면 즉시 중단** (Short-circuit evaluation)
3. 모두 true면 연결된 Action들 실행

### 사용 예시

#### 예시 1: HP와 자세 동시 체크
```yaml
# "내 HP < 30% AND 내 자세 < 50%"
SequenceNode:
  Children:
    - HPComparisonCondition:
        comparisonTarget: Self
        comparisonOperator: Less
        valueType: Percentage
        threshold: 0.3
    - PoiseComparisonCondition:
        comparisonTarget: Self
        comparisonOperator: Less
        valueType: Percentage
        threshold: 0.5
  ConnectedActions:
    - ForceBehaviorAction:
        behaviorType: ForceGuard
        priority: 20
```

#### 예시 2: 방어 턴 + HP 조건
```yaml
# "방어 턴 AND 상대 HP > 70%"
SequenceNode:
  Children:
    - TurnTypeCondition:
        turnType: DefenseTurn
    - HPComparisonCondition:
        comparisonTarget: Target
        comparisonOperator: Greater
        valueType: Percentage
        threshold: 0.7
  ConnectedActions:
    - ProbabilityAdjustmentAction:
        targetProbability: ParryPerfectRate
        adjustmentType: Absolute
        value: 1.0
        priority: 15
```

---

## 2. Selector Node (OR 조건)

### 개념
- **자식 Condition 중 하나라도 true면** 연결된 Action 실행
- 논리 OR 연산과 동일
- 모두 false일 때만 전체 false

### 구조
```yaml
SelectorNode:
  Children:
    - Condition1
    - Condition2
    - Condition3
  ConnectedActions:
    - Action1
    - Action2
```

### 평가 방식
1. 자식 Condition을 순서대로 평가
2. **하나라도 true면 즉시 성공** (Short-circuit evaluation)
3. 하나라도 true면 연결된 Action들 실행

### 사용 예시

#### 예시 1: 위기 상황 체크
```yaml
# "내 HP < 20% OR 내 자세 < 30%"
SelectorNode:
  Children:
    - HPComparisonCondition:
        comparisonTarget: Self
        comparisonOperator: Less
        valueType: Percentage
        threshold: 0.2
    - PoiseComparisonCondition:
        comparisonTarget: Self
        comparisonOperator: Less
        valueType: Percentage
        threshold: 0.3
  ConnectedActions:
    - ActionCommandSelectionAction:
        selectionType: ByTag
        requiredTag: "필살기"
        priority: 20
```

#### 예시 2: 다중 턴 조건
```yaml
# "5턴째 OR 10턴째 OR 15턴째"
SelectorNode:
  Children:
    - TurnCountCondition:
        comparisonOperator: Equal
        turnNumber: 5
    - TurnCountCondition:
        comparisonOperator: Equal
        turnNumber: 10
    - TurnCountCondition:
        comparisonOperator: Equal
        turnNumber: 15
  ConnectedActions:
    - ProbabilityAdjustmentAction:
        targetProbability: AttackPerfectRate
        adjustmentType: Absolute
        value: 1.0
        priority: 10
```

---

## 3. Composite Node 중첩

### 개념
- Composite Node 안에 다른 Composite Node 포함 가능
- 복잡한 논리 조건 구성 가능

### 예시: (A AND B) OR C

```yaml
# "(내 HP < 30% AND 상대 HP > 70%) OR 턴 수 >= 10"
SelectorNode:
  Children:
    - SequenceNode:
        Children:
          - HPComparisonCondition:
              comparisonTarget: Self
              comparisonOperator: Less
              valueType: Percentage
              threshold: 0.3
          - HPComparisonCondition:
              comparisonTarget: Target
              comparisonOperator: Greater
              valueType: Percentage
              threshold: 0.7
    - TurnCountCondition:
        comparisonOperator: GreaterOrEqual
        turnNumber: 10
  ConnectedActions:
    - ActionCommandSelectionAction:
        selectionType: ByTag
        requiredTag: "방어형"
        priority: 15
```

### 예시: A AND (B OR C)

```yaml
# "방어 턴 AND (내 HP < 50% OR 내 자세 < 40%)"
SequenceNode:
  Children:
    - TurnTypeCondition:
        turnType: DefenseTurn
    - SelectorNode:
        Children:
          - HPComparisonCondition:
              comparisonTarget: Self
              comparisonOperator: Less
              valueType: Percentage
              threshold: 0.5
          - PoiseComparisonCondition:
              comparisonTarget: Self
              comparisonOperator: Less
              valueType: Percentage
              threshold: 0.4
  ConnectedActions:
    - ForceBehaviorAction:
        behaviorType: ForceGuard
        priority: 20
```

---

## 4. BT 구조에서의 Composite Node

### BT 기본 구조
```yaml
BehaviorTree:
  - Entry1:
      Condition: (Composite or Simple Condition)
      Actions: [Action1, Action2, ...]
  - Entry2:
      Condition: (Composite or Simple Condition)
      Actions: [Action1, Action2, ...]
```

### 우선순위 처리
- **리스트 순서 = 우선순위**
- Entry1의 Condition이 만족되면 Entry2는 체크하지 않음
- Composite Node도 하나의 Condition처럼 동작

### 완전한 BT 예시

```yaml
BehaviorTree:
  # 우선순위 1: 위급 상황 (내 HP < 20% AND 방어 턴)
  - Entry1:
      Condition:
        SequenceNode:
          Children:
            - HPComparisonCondition:
                comparisonTarget: Self
                comparisonOperator: Less
                valueType: Percentage
                threshold: 0.2
            - TurnTypeCondition:
                turnType: DefenseTurn
      Actions:
        - ForceBehaviorAction:
            behaviorType: ForceGuard
            priority: 30
  
  # 우선순위 2: 공격 기회 (상대 자세 < 30% AND 공격 턴)
  - Entry2:
      Condition:
        SequenceNode:
          Children:
            - PoiseComparisonCondition:
                comparisonTarget: Target
                comparisonOperator: Less
                valueType: Percentage
                threshold: 0.3
            - TurnTypeCondition:
                turnType: AttackTurn
      Actions:
        - ActionCommandSelectionAction:
            selectionType: ByTag
            requiredTag: "강공격"
            priority: 20
  
  # 우선순위 3: 일반 방어 (방어 턴)
  - Entry3:
      Condition:
        TurnTypeCondition:
          turnType: DefenseTurn
      Actions:
        - ProbabilityAdjustmentAction:
            targetProbability: GuardAttemptRate
            adjustmentType: Absolute
            value: 0.8
            priority: 10
```

---

## 5. 구현 가이드

### ScriptableObject 구조 제안

```csharp
// 추상 Condition 클래스
public abstract class BTConditionNode : ScriptableObject
{
    public abstract bool Evaluate(CombatContext context);
}

// Composite Sequence Node
public class BTSequenceNode : BTConditionNode
{
    public List<BTConditionNode> children = new List<BTConditionNode>();
    
    public override bool Evaluate(CombatContext context)
    {
        foreach (var child in children)
        {
            if (!child.Evaluate(context))
                return false; // Short-circuit
        }
        return true;
    }
}

// Composite Selector Node
public class BTSelectorNode : BTConditionNode
{
    public List<BTConditionNode> children = new List<BTConditionNode>();
    
    public override bool Evaluate(CombatContext context)
    {
        foreach (var child in children)
        {
            if (child.Evaluate(context))
                return true; // Short-circuit
        }
        return false;
    }
}
```

### Inspector 표시
- Composite Node는 접을 수 있는(Foldout) 리스트로 표시
- 중첩 레벨을 시각적으로 구분 (들여쓰기)
- 각 Condition의 평가 결과를 런타임에 표시 (디버깅용)

---

## 6. 디버깅 팁

### 로그 출력
```
[BT] Entry 0 평가 시작
  [Sequence] 평가 시작
    [HPCondition] Self HP: 45/100 (45%) vs 50% -> false
  [Sequence] 결과: false (조기 종료)
[BT] Entry 0 조건 불만족, 다음 Entry로

[BT] Entry 1 평가 시작
  [Selector] 평가 시작
    [HPCondition] Self HP: 45/100 (45%) vs 30% -> false
    [PoiseCondition] Self Poise: 25/100 (25%) vs 40% -> true
  [Selector] 결과: true
  [Action] ForceGuard 실행 (Priority: 20)
[BT] Entry 1 조건 만족, 하위 Entry 무시
```

### 주의사항
- **Short-circuit 최적화**: 첫 실패/성공 시 나머지 조건 체크 안 함
- **순서 중요**: 자주 실패/성공하는 조건을 앞에 배치하면 성능 향상
- **중첩 제한**: 과도한 중첩은 가독성 저하 (3단계 이하 권장)

---

## 7. 실전 활용 예시

### 패턴 1: 방어형 NPC
```yaml
BehaviorTree:
  # 위급 시 무조건 막기
  - Condition:
      SequenceNode:
        - HPCondition: Self < 30%
        - TurnType: DefenseTurn
    Actions:
      - ForceGuard (Priority: 30)
  
  # 평상시 높은 막기 확률
  - Condition:
      TurnType: DefenseTurn
    Actions:
      - GuardAttemptRate = 0.8 (Priority: 10)
```

### 패턴 2: 공격형 NPC
```yaml
BehaviorTree:
  # 상대 약화 시 강공격
  - Condition:
      SequenceNode:
        - PoiseCondition: Target < 40%
        - TurnType: AttackTurn
    Actions:
      - SelectByTag: "강공격" (Priority: 20)
  
  # 평상시 공격 성공률 높임
  - Condition:
      TurnType: AttackTurn
    Actions:
      - AttackPerfectRate = 0.7 (Priority: 5)
```

### 패턴 3: 전략적 NPC
```yaml
BehaviorTree:
  # 내가 우세하고 상대 약함 -> 필살기
  - Condition:
      SequenceNode:
        - HPCondition: Self > 60%
        - HPCondition: Target < 40%
        - TurnType: AttackTurn
    Actions:
      - SelectByTag: "필살기" (Priority: 25)
  
  # 내가 열세 -> 방어 집중
  - Condition:
      SequenceNode:
        - SelectorNode:
            - HPCondition: Self < 40%
            - PoiseCondition: Self < 30%
        - TurnType: DefenseTurn
    Actions:
      - ParryPerfectRate = 1.0 (Priority: 20)
```

---

**문서 버전**: 1.0  
**작성일**: 2024년  
**최종 수정일**: 2024년

