# Action Node List

## 개요
Behavior Tree에서 사용되는 Action Node들의 상세 사양 및 사용법을 정의합니다.

---

## 1. 확률 조정 액션 (Probability Adjustment Action)

### 사양
```yaml
NodeType: Action
Name: ProbabilityAdjustmentAction
```

### 설정 데이터
- **targetProbability**: enum
  - `AttackPerfectRate`: 공격 성공률
  - `ParryPerfectRate`: 쳐내기 성공률
  - `GuardAttemptRate`: 막기 시도 확률
  - `ParryWhileGuardingRate`: 막기 중 쳐내기 성공률
  
- **adjustmentType**: enum (Absolute, Relative)
  - `Absolute`: 절대값 설정 (기존 값 무시)
  - `Relative`: 상대값 증감 (기존 값에 더하거나 뺌)
  
- **value**: float (0~1 범위)
  - 설정할 확률 값 또는 증감값
  
- **priority**: int (0 이상)
  - 우선순위 (높을수록 우선)
  - 동일 확률 조정 시 높은 Priority만 적용

### 동작 방식
1. `targetProbability`에 해당하는 확률 수치 선택
2. 동일 수치를 조정하는 다른 Action과 Priority 비교
3. 가장 높은 Priority Action만 적용
4. `adjustmentType`에 따라 값 조정
   - Absolute: 확률 = `value`
   - Relative: 확률 += `value` (0~1 범위 클램핑)
5. 원본 수치 저장 (복원용)

### 사용 예시
```yaml
# 예시 1: 쳐내기 확률을 100%로 설정
targetProbability: ParryPerfectRate
adjustmentType: Absolute
value: 1.0
priority: 10

# 예시 2: 막기 시도 확률을 50% 증가
targetProbability: GuardAttemptRate
adjustmentType: Relative
value: 0.5
priority: 5

# 예시 3: 공격 성공률을 20% 감소
targetProbability: AttackPerfectRate
adjustmentType: Relative
value: -0.2
priority: 3
```

### 주의사항
- Relative 조정 후 값이 0~1 범위를 벗어나면 클램핑
- Priority가 같으면 먼저 실행된 Action 적용 (비권장, 디자인 시 중복 방지)

---

## 2. 강제 행동 액션 (Force Behavior Action)

### 사양
```yaml
NodeType: Action
Name: ForceBehaviorAction
```

### 설정 데이터
- **behaviorType**: enum
  - `ForceGuard`: 막기 강제 실행
  - `ForceParry`: 쳐내기 강제 시도
  - `ForceNoAction`: 아무 행동도 하지 않음
  
- **priority**: int (0 이상)

### 동작 방식
1. 연결된 Condition이 만족되면 실행
2. **확률 체크 우선**: 모든 확률 조정 무시하고 강제 실행
3. Priority가 높은 강제 행동만 적용 (중복 시)
4. 해당 턴에만 적용

### 사용 예시
```yaml
# 예시 1: 무조건 막기 실행
behaviorType: ForceGuard
priority: 20

# 예시 2: 쳐내기 강제 시도
behaviorType: ForceParry
priority: 15
```

### 주의사항
- **강제 행동 > 확률 조정**: 강제 행동이 활성화되면 확률 조정 Action 무의미
- 방어 턴에서만 의미 있음
- 공격 턴에서는 검술 선택 액션 사용

---

## 3. 검술 선택 액션 (Action Command Selection Action)

### 사양
```yaml
NodeType: Action
Name: ActionCommandSelectionAction
```

### 설정 데이터
- **selectionType**: enum (ByIndex, ByTag)
  - `ByIndex`: 검술 인덱스로 직접 지정
  - `ByTag`: 태그 기반 랜덤 선택
  
- **commandIndex**: int (selectionType이 ByIndex일 때)
  - 사용할 검술의 인덱스
  
- **requiredTag**: string (selectionType이 ByTag일 때)
  - 필터링할 태그 (해당 태그를 가진 검술 중 랜덤)
  
- **priority**: int (0 이상)

### 동작 방식

#### ByIndex 방식
1. `commandIndex`로 검술 직접 지정
2. 해당 인덱스의 ActionCommand 선택
3. 인덱스가 범위를 벗어나면 기본 검술(0번) 선택

#### ByTag 방식
1. 현재 장착한 검술 리스트에서 `requiredTag`를 포함한 검술 필터링
2. 필터링된 검술 중 랜덤으로 하나 선택
3. 해당 태그를 가진 검술이 없으면 기본 검술(0번) 선택

### Priority 처리
- 여러 검술 선택 Action이 실행되면 Priority 높은 것만 적용
- Priority 같으면 먼저 실행된 것 적용

### 사용 예시
```yaml
# 예시 1: 인덱스 2번 검술 강제 사용
selectionType: ByIndex
commandIndex: 2
priority: 15

# 예시 2: "필살기" 태그를 가진 검술 중 랜덤 선택
selectionType: ByTag
requiredTag: "필살기"
priority: 20

# 예시 3: "원거리" 태그 검술 중 랜덤 선택
selectionType: ByTag
requiredTag: "원거리"
priority: 10
```

### ActionCommandData Tag 구조
```csharp
// ActionCommandData.cs에 추가 필요
public List<string> tags = new List<string>();
```

### 주의사항
- 공격 턴에서만 의미 있음
- Tag는 대소문자 구분
- 여러 태그를 가진 검술은 각 태그로 필터링 가능

---

## 4. 행동 비활성화 액션 (Disable Behavior Action)

### 사양
```yaml
NodeType: Action
Name: DisableBehaviorAction
```

### 설정 데이터
- **targetBehavior**: enum
  - `DisableParryWhileGuarding`: 막기 중 쳐내기 시도 비활성화
  - `DisableGuard`: 막기 완전 비활성화
  - `DisableParry`: 쳐내기 완전 비활성화
  
- **priority**: int (0 이상)

### 동작 방식
1. `targetBehavior`에 해당하는 행동 비활성화
2. 내부적으로 해당 행동의 확률을 0으로 설정
3. Priority 높은 비활성화만 적용
4. 해당 턴에만 적용

### 사용 예시
```yaml
# 예시 1: 막기 중 쳐내기 시도 금지
targetBehavior: DisableParryWhileGuarding
priority: 5

# 예시 2: 막기 완전 비활성화
targetBehavior: DisableGuard
priority: 8
```

### 주의사항
- 확률 조정 액션의 특수한 형태
- Priority 0으로 설정하면 다른 Action에 의해 쉽게 덮어씌워질 수 있음

---

## 5. Priority 우선순위 처리

### 동일 타입 Action 중복 시
1. 모든 조건 만족 Action 수집
2. 동일 대상(확률/행동)을 조정하는 Action 그룹화
3. 각 그룹에서 **Priority 가장 높은 Action만 적용**
4. Priority 동일 시 먼저 실행된 것 적용 (권장하지 않음)

### 서로 다른 타입 Action
- 독립적으로 모두 적용
- 예: 확률 조정 + 검술 선택 동시 적용 가능

### 강제 행동 vs 확률 조정
- **강제 행동 우선**: 강제 행동이 활성화되면 해당 행동의 확률 조정 무시
- 예: ForceGuard가 실행되면 GuardAttemptRate 조정 무의미

---

## 6. Additional Turn Duration (낮은 우선순위)

### 개념
- 각 Action에 `additionalTurnDuration` 필드 추가 (추후 구현)
- `-1`: 영구 지속
- `0`: 해당 턴만
- `1 이상`: 지정 턴 수만큼 추가 지속

### 동작 방식 (추후 구현)
1. Action 실행 시 Duration 값 저장
2. 매 턴마다 Duration 감소
3. 0이 되면 원본 수치로 복원
4. -1이면 수동 해제 또는 특정 조건으로 해제

### 현재 상태
- **우선순위 낮음**: 초기 구현에서는 생략
- 모든 Action은 해당 턴에만 적용

---

## 구현 시 고려사항

### 1. 원본 수치 보존
- Action 적용 전 CharacterData의 원본 확률 저장
- 턴 종료 시 또는 Duration 종료 시 복원
- 복원 메커니즘 필수

### 2. Priority 처리
- 정수형 (int), 음수 불가
- 기본값 0 권장
- 중복 Priority 방지를 위한 디자인 권장

### 3. 에러 처리
- 범위 체크 (확률은 0~1)
- null 체크
- 잘못된 인덱스/태그 처리

### 4. 디버깅
- 적용된 Action 로그 출력
- Priority 비교 과정 로그
- 원본 수치 vs 조정 수치 표시

---

## 5. 막기 중 쳐내기 활성화 액션 (Do Parry While Guarding Action)

### 사양
```yaml
NodeType: Action
Name: DoParryWhileGuardingAction
ScriptName: BTAction_DoParryWhileGuarding
MenuPath: BT/Actions/Do Parry While Guarding
```

### 설정 데이터
- **enableParryWhileGuarding**: bool
  - `true`: 막기 중 쳐내기 시도 활성화
  - `false`: 막기 중 쳐내기 시도 비활성화
  
- **priority**: int (0 이상, 상속)
  - 우선순위 (높을수록 우선)
  
- **executeOncePerCombat**: bool (상속)
  - 전투 중 1회만 실행 여부

### 동작 방식
1. `enableParryWhileGuarding` bool 값을 float로 변환
   - `true` → `1.0`
   - `false` → `0.0`
2. `DoParryWhileGuarding` 키로 Context에 저장
3. `NPCRuntimeProbabilities`에서 `parryWhileGuarding` bool 필드에 적용
4. AI Defense 시스템에서 참조하여 막기 중 쳐내기 시도 여부 결정

### 사용 예시
```yaml
# 예시 1: 막기 중 쳐내기 활성화
enableParryWhileGuarding: true
executeOncePerCombat: false

# 예시 2: 막기 중 쳐내기 비활성화 (원본값 복원용)
enableParryWhileGuarding: false
executeOncePerCombat: false

# 예시 3: HP 50% 미만 시 1회만 활성화
Condition: HP < 50%
Action: DoParryWhileGuarding
  enableParryWhileGuarding: true
  executeOncePerCombat: true
```

### Unity 인스펙터 표시
```
[Header("막기 중 쳐내기 설정")]
Enable Parry While Guarding: ☑  (체크박스)

Description: "막기 중 쳐내기 시도: 활성화"
```

### BT 구성 예시
```
Entry (HP < 50%):
  Condition: HPLess50
  Actions:
    1. Prob100Guard (막기 시도율 100%)
    2. DoParryWhileGuarding (막기 중 쳐내기 활성화) ✅
    3. Prob100ParryWhileGuardRate (막기 중 쳐내기 성공률 100%)
```

### 설계 의도
**단일 책임 원칙 (Single Responsibility Principle):**
- `BTAction_ProbabilityAdjustment`: float 확률 조정 전용
- `BTAction_DoParryWhileGuarding`: bool 행동 활성화 전용

**사용자 경험 개선:**
- float 슬라이더 대신 직관적인 **체크박스** 사용
- "막기 중 쳐내기를 허용할 것인가?" 명확한 의미 전달

### 관련 시스템
1. **NPCRuntimeProbabilities**: bool → bool 변환 처리
2. **DefaultAIDefenseDecisionMaker**: `GetParryWhileGuarding()` 참조
3. **BehaviorTreeContext**: `DoParryWhileGuarding` 키 관리

### 주의사항
1. **Bool 타입**: float 확률이 아닌 On/Off 토글
2. **성공률과 별개**: `ParryWhileGuardingRate`(성공률)는 별도 조정 필요
3. **AI Defense에서만 사용**: 공격 턴에는 영향 없음
4. **원본 값 복원**: 턴 종료 시 자동으로 원본 값으로 리셋

### 디버깅 로그
```
[BTAction_DoParryWhileGuarding] 막기 중 쳐내기 시도: 활성화
[NPCRuntimeProbabilities] 막기 중 쳐내기 시도: False → True (입력: 1.00)
[AIDefense] 막기 중 - 쳐내기 가능
```

---

**문서 버전**: 2.0 (DoParryWhileGuarding 추가)  
**작성일**: 2024년  
**최종 수정일**: 2025년 10월 13일

