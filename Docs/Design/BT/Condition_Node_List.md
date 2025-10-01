# Condition Node List

## 개요
Behavior Tree에서 사용되는 Condition Node들의 상세 사양 및 사용법을 정의합니다.

---

## 1. HP 비교 조건 (HP Comparison Condition)

### 사양
```yaml
NodeType: Condition
Name: HPComparisonCondition
```

### 설정 데이터
- **comparisonTarget**: enum (Self, Target)
  - `Self`: NPC 자신의 HP 확인
  - `Target`: 상대(플레이어)의 HP 확인
  
- **comparisonOperator**: enum (Greater, Less, GreaterOrEqual, LessOrEqual, Equal, NotEqual)
  - `>`, `<`, `>=`, `<=`, `==`, `!=`
  
- **valueType**: enum (Absolute, Percentage)
  - `Absolute`: 절대값 (예: 50)
  - `Percentage`: 비율값 (예: 0.3 = 30%)
  
- **threshold**: float
  - 비교할 임계값

### 동작 방식
1. `comparisonTarget`에 따라 비교 대상 결정
2. 대상의 현재 HP 획득
3. `valueType`에 따라 비교값 계산
   - Absolute: `currentHP` vs `threshold`
   - Percentage: `currentHP / maxHP` vs `threshold`
4. `comparisonOperator`로 비교 수행
5. 조건 만족 시 `true` 반환

### 사용 예시
```yaml
# 예시 1: 자신의 HP가 30% 미만일 때
comparisonTarget: Self
comparisonOperator: Less
valueType: Percentage
threshold: 0.3

# 예시 2: 상대의 HP가 50 초과일 때
comparisonTarget: Target
comparisonOperator: Greater
valueType: Absolute
threshold: 50
```

---

## 2. 자세 게이지 비교 조건 (Poise Comparison Condition)

### 사양
```yaml
NodeType: Condition
Name: PoiseComparisonCondition
```

### 설정 데이터
- **comparisonTarget**: enum (Self, Target)
  - `Self`: NPC 자신의 자세 포인트 확인
  - `Target`: 상대(플레이어)의 자세 포인트 확인
  
- **comparisonOperator**: enum (Greater, Less, GreaterOrEqual, LessOrEqual, Equal, NotEqual)
  
- **valueType**: enum (Absolute, Percentage)
  
- **threshold**: float

### 동작 방식
1. `comparisonTarget`에 따라 비교 대상 결정
2. 대상의 현재 자세 포인트 획득
3. `valueType`에 따라 비교값 계산
   - Absolute: `currentPoise` vs `threshold`
   - Percentage: `currentPoise / maxPoise` vs `threshold`
4. `comparisonOperator`로 비교 수행
5. 조건 만족 시 `true` 반환

### 사용 예시
```yaml
# 예시 1: 자신의 자세 포인트가 20% 이하일 때
comparisonTarget: Self
comparisonOperator: LessOrEqual
valueType: Percentage
threshold: 0.2

# 예시 2: 상대의 자세 포인트가 30 미만일 때
comparisonTarget: Target
comparisonOperator: Less
valueType: Absolute
threshold: 30
```

---

## 3. 턴 타입 조건 (Turn Type Condition)

### 사양
```yaml
NodeType: Condition
Name: TurnTypeCondition
```

### 설정 데이터
- **turnType**: enum (AttackTurn, DefenseTurn)
  - `AttackTurn`: NPC가 공격하는 턴
  - `DefenseTurn`: NPC가 방어하는 턴

### 동작 방식
1. 현재 턴의 타입 확인
2. `turnType`과 일치하면 `true` 반환

### 사용 예시
```yaml
# 예시 1: 방어 턴일 때
turnType: DefenseTurn

# 예시 2: 공격 턴일 때
turnType: AttackTurn
```

### 주의사항
- 턴 타입은 CombatManager에서 관리
- 플레이어 턴 = NPC 방어 턴
- NPC 턴 = NPC 공격 턴

---

## 4. 턴 수 조건 (Turn Count Condition)

### 사양
```yaml
NodeType: Condition
Name: TurnCountCondition
```

### 설정 데이터
- **comparisonOperator**: enum (Greater, Less, GreaterOrEqual, LessOrEqual, Equal, NotEqual)
  
- **turnNumber**: int
  - 비교할 턴 번호

### 동작 방식
1. 현재 턴 번호 획득 (전투 시작부터의 누적 턴)
2. `comparisonOperator`로 `turnNumber`와 비교
3. 조건 만족 시 `true` 반환

### 사용 예시
```yaml
# 예시 1: 5턴 이상일 때
comparisonOperator: GreaterOrEqual
turnNumber: 5

# 예시 2: 정확히 10턴일 때
comparisonOperator: Equal
turnNumber: 10

# 예시 3: 3턴 미만일 때
comparisonOperator: Less
turnNumber: 3
```

---

## 5. Composite Node와의 조합

### AND 조건 (Sequence Node)
여러 Condition을 모두 만족해야 할 때 사용

```yaml
# 예시: 내 HP < 30% AND 상대 HP > 70%
CompositeNode: Sequence
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
```

### OR 조건 (Selector Node)
여러 Condition 중 하나라도 만족하면 될 때 사용

```yaml
# 예시: 내 HP < 20% OR 내 자세 < 30%
CompositeNode: Selector
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
```

---

## 구현 시 고려사항

### 1. 성능 최적화
- 자주 체크되는 조건은 캐싱 고려
- 불필요한 연산 최소화

### 2. 에러 처리
- null 체크 (Combatant, CharacterData)
- 범위 체크 (Percentage는 0~1)
- 잘못된 enum 값 처리

### 3. 디버깅
- 조건 체크 결과 로그 출력
- 비교값 및 임계값 표시
- 조건 만족/불만족 이유 명시

---

**문서 버전**: 1.0  
**작성일**: 2024년  
**최종 수정일**: 2024년

