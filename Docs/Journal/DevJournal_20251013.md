# 개발 일지 - 2025년 10월 13일

## 작업 개요
- **주제**: BT 시스템 Phase 3 완료 - 확률 Override 실제 적용 및 통합 테스트
- **목표**: Phase 3 완성하여 BT 시스템을 실제 전투에서 사용 가능한 상태로 만들기

---

## 현재 상황 요약

### ✅ 완료된 Phase
- **Phase 1 (10/1)**: 데이터 구조 확장 완료
- **Phase 2 (10/2)**: BT Core 시스템 완전 구현 완료

### 🔄 Phase 3 진행 상황 (60% 완료)
| 작업 | 상태 |
|------|------|
| BT Executor 구현 | ✅ 완료 |
| EnemyController BT 연동 | ✅ 완료 |
| 확률 Override 시스템 | 🔴 **미완성 (TODO 상태)** |
| 검술 선택 로직 수정 | ✅ 완료 |

### 🎯 오늘의 핵심 과제
**`EnemyCombatant.ApplyBehaviorTreeResults()` 메서드가 TODO 상태로 남아있음**
- BT 평가는 완료되지만 결과가 실제 확률에 적용되지 않음
- 이 부분을 완성해야 BT 시스템이 실제로 동작함

---

## 오늘의 구현 계획

### Phase 3 완료 작업 목록

#### 1단계: 확률 Override 시스템 구현 ⚠️ **최우선**

##### 1.1 NPCRuntimeBehavior 클래스 구현
**위치**: `Assets/Script/NPCRuntimeBehavior.cs` (신규 파일)

**작업 내용**:
- [ ] 원본 확률과 현재 확률을 분리 관리하는 클래스 생성
- [ ] `ApplyOverrides(Dictionary<string, float>)`: BT 결과를 확률에 적용
- [ ] `ResetToOriginal()`: 원본 확률로 복원
- [ ] 확률 접근 프로퍼티 (AttackPerfectRate, ParryPerfectRate 등)

**구현 세부사항**:
```csharp
public class NPCRuntimeBehavior
{
    private NPCBehaviorProbabilities original;  // 원본 (CharacterData)
    private NPCBehaviorProbabilities current;   // 현재 (Override 적용)
    
    // 생성자: 원본 복사
    // ApplyOverrides: Dictionary를 current에 반영
    // ResetToOriginal: current = original 복사
    // Get 프로퍼티: current 값 반환
}
```

##### 1.2 EnemyCombatant 확장
**파일**: `Assets/Script/EnemyCombatant.cs`

**작업 내용**:
- [ ] `runtimeBehavior` 필드 추가
- [ ] `InitializeRuntimeBehavior()`: 전투 시작 시 호출
- [ ] `ApplyBehaviorTreeResults()`: TODO 제거 및 실제 로직 구현
- [ ] `ResetBehaviorOnTurnEnd()`: 턴 종료 시 확률 리셋
- [ ] 기존 확률 참조 코드를 `runtimeBehavior`로 수정

**구현 세부사항**:
```csharp
public class EnemyCombatant : Combatant
{
    public NPCRuntimeBehavior runtimeBehavior;
    
    // 초기화: CharacterData.npcBehavior를 기반으로 생성
    // ApplyBehaviorTreeResults: context의 probabilityOverrides 적용
    // 기존 npcBehavior 참조를 runtimeBehavior로 변경
}
```

##### 1.3 CombatManager 연동
**파일**: `Assets/Script/Combat/CombatManager.cs`

**작업 내용**:
- [ ] 전투 시작 시 `InitializeRuntimeBehavior()` 호출
- [ ] 턴 종료 시 `ResetBehaviorOnTurnEnd()` 호출
- [ ] BT 상태 리셋과 함께 호출되도록 통합

---

#### 2단계: 강제 행동(ForceBehavior) 시스템 구현

##### 2.1 BehaviorTreeContext 확장
**파일**: `Assets/Script/BT/Core/BehaviorTreeContext.cs`

**작업 내용**:
- [ ] `forcedBehavior` 필드 사용 여부 확인
- [ ] 필요 시 enum 또는 string 정의 명확화

##### 2.2 EnemyCombatant 강제 행동 처리
**파일**: `Assets/Script/EnemyCombatant.cs`

**작업 내용**:
- [ ] `ApplyBehaviorTreeResults()` 내에서 `forcedBehavior` 처리
- [ ] 강제 행동이 있으면 확률 무시하고 해당 행동 선택
- [ ] `BTAction_ForceBehavior` 노드 결과 적용

**구현 세부사항**:
```csharp
// ApplyBehaviorTreeResults 내부
if (!string.IsNullOrEmpty(context.forcedBehavior))
{
    // 강제 행동 설정
    // "Attack", "Guard", "Parry" 등
}
```

---

#### 3단계: BT 실행 로그 시스템 강화

##### 3.1 디버그 로그 추가
**파일**: 여러 BT 노드 파일들

**작업 내용**:
- [ ] `BehaviorTreeExecutor.cs`: BT 평가 시작/종료 로그
- [ ] `EnemyCombatant.cs`: 확률 변경 전/후 로그
- [ ] 각 Condition Node: 평가 결과 로그
- [ ] 각 Action Node: 실행 내용 로그

**로그 형식**:
```
[BT] === BT 평가 시작 (턴: 3, 타입: 공격) ===
[BT Condition] HPComparison: True (Self HP: 45/100 = 45%)
[BT Action] ProbabilityAdjustment: AttackPerfectRate 0.3 → 0.8
[BT] === BT 평가 완료 ===
[BT Apply] 확률 적용 완료
  - AttackPerfectRate: 0.3 → 0.8
  - ParryPerfectRate: 0.5 → 0.5 (변경 없음)
```

---

#### 4단계: 테스트 BT 에셋 생성

##### 4.1 공격형 NPC BT 생성
**에셋**: `Assets/Data/BT/BT_AggressiveEnemy.asset`

**패턴 설계**:
- HP 50% 이상: 공격 성공률 80%
- HP 50% 미만: 공격 성공률 50%, 막기 시도율 70%

**작업 내용**:
- [ ] HP Comparison Condition 노드 생성
- [ ] Probability Adjustment Action 노드 생성
- [ ] BehaviorTreeData 생성 및 Entry 구성

##### 4.2 방어형 NPC BT 생성
**에셋**: `Assets/Data/BT/BT_DefensiveEnemy.asset`

**패턴 설계**:
- 방어 턴: 막기 시도율 90%, 쳐내기 성공률 70%
- 공격 턴: 공격 성공률 40%

**작업 내용**:
- [ ] Turn Type Condition 노드 생성
- [ ] Probability Adjustment Action 노드 생성
- [ ] BehaviorTreeData 생성 및 Entry 구성

##### 4.3 특수 패턴 BT 생성
**에셋**: `Assets/Data/BT/BT_SpecialPattern.asset`

**패턴 설계**:
- 턴 3 이상: 특정 검술(인덱스 0) 강제 사용
- 턴 5 이상: "Ultimate" 태그 검술 강제 사용

**작업 내용**:
- [ ] Turn Count Condition 노드 생성
- [ ] Command Selection Action 노드 생성
- [ ] BehaviorTreeData 생성 및 Entry 구성

---

#### 5단계: 통합 테스트 및 검증

##### 5.1 Unity 플레이 테스트
**작업 내용**:
- [ ] 테스트 BT를 CharacterData에 할당
- [ ] 전투 진행하며 로그 확인
- [ ] 확률이 실제로 변경되는지 확인
- [ ] 검술 선택이 BT 결과대로 동작하는지 확인

##### 5.2 검증 체크리스트
- [ ] **BT 평가**: 턴 시작 시 BT가 정상적으로 평가됨
- [ ] **확률 적용**: Override된 확률이 실제 행동에 반영됨
- [ ] **검술 선택**: selectedCommandIndex/Tag가 정상 작동
- [ ] **Priority 처리**: 같은 대상 확률 조정 시 최고 Priority만 적용
- [ ] **강제 행동**: forcedBehavior가 확률보다 우선
- [ ] **턴 리셋**: 턴 종료 시 확률이 원본으로 복원
- [ ] **전투 리셋**: 새 전투 시작 시 BT 상태 초기화
- [ ] **개체 독립성**: 같은 BT를 사용하는 다른 NPC가 서로 영향 없음

##### 5.3 버그 수정 및 최적화
**작업 내용**:
- [ ] 발견된 버그 수정
- [ ] 로그 출력 최적화
- [ ] null 처리 강화
- [ ] 예외 상황 처리

---

#### 6단계: Phase 4 사전 작업 (시간 여유 시)

##### 6.1 BTLogger 클래스 구현
**파일**: `Assets/Script/BT/BTLogger.cs` (신규)

**작업 내용**:
- [ ] 정적 로그 메서드 구현
- [ ] 로그 활성화/비활성화 플래그
- [ ] 조건부 로그 출력

##### 6.2 런타임 모니터링 UI 설계
**작업 내용**:
- [ ] 현재 확률 표시 UI 설계
- [ ] BT 실행 결과 표시 UI 설계
- [ ] 다음 작업 시 구현 계획 수립

---

## 예상 작업 흐름

### 우선순위 1: 핵심 기능 (필수)
1. NPCRuntimeBehavior 클래스 구현 (30분)
2. EnemyCombatant 확장 (1시간)
3. 강제 행동 시스템 구현 (30분)
4. 디버그 로그 추가 (30분)

**예상 소요 시간**: 2.5시간

### 우선순위 2: 테스트 (필수)
5. 테스트 BT 에셋 생성 (1시간)
6. Unity 플레이 테스트 (1시간)
7. 버그 수정 (1시간)

**예상 소요 시간**: 3시간

### 우선순위 3: 추가 작업 (선택)
8. Phase 4 사전 작업 (시간 여유 시)

**총 예상 소요 시간**: 5.5시간 (Phase 3 완료 기준)

---

## 주요 파일 목록

### 수정할 기존 파일
- `Assets/Script/EnemyCombatant.cs`: 확률 Override 적용 로직
- `Assets/Script/Combat/CombatManager.cs`: 초기화 및 리셋 호출
- `Assets/Script/CharacterData.cs`: 필요 시 메서드 추가

### 생성할 신규 파일
- `Assets/Script/NPCRuntimeBehavior.cs`: 런타임 확률 관리 클래스
- `Assets/Script/BT/BTLogger.cs`: 디버그 로그 시스템 (선택)

### 생성할 테스트 에셋
- `Assets/Data/BT/TestConditions/`: Condition 노드들
- `Assets/Data/BT/TestActions/`: Action 노드들
- `Assets/Data/BT/BT_AggressiveEnemy.asset`: 공격형 BT
- `Assets/Data/BT/BT_DefensiveEnemy.asset`: 방어형 BT
- `Assets/Data/BT/BT_SpecialPattern.asset`: 특수 패턴 BT

---

## 검증 포인트

### 기능 검증
1. **확률 적용**: BT 결과가 실제 NPC 행동 확률에 반영되는가?
2. **턴 리셋**: 턴 종료 시 확률이 원본으로 복원되는가?
3. **전투 리셋**: 새 전투 시 BT 상태가 초기화되는가?
4. **Priority**: 여러 확률 조정 시 최고 Priority가 적용되는가?
5. **강제 행동**: forcedBehavior가 확률을 무시하는가?
6. **검술 선택**: BT에서 지정한 검술이 선택되는가?

### 안정성 검증
1. **null 처리**: BT가 없어도 오류 없이 동작하는가?
2. **에셋 호환성**: 기존 CharacterData 에셋이 정상 동작하는가?
3. **메모리**: 개체별 BT 인스턴스가 독립적으로 관리되는가?
4. **성능**: BT 평가가 프레임 드롭을 일으키지 않는가?

---

## 예상 이슈 및 대응

### 이슈 1: 확률 참조 위치 불일치
**증상**: 기존 코드에서 `CharacterData.npcBehavior`를 직접 참조
**대응**: 모든 참조를 `runtimeBehavior`로 변경

### 이슈 2: 턴 리셋 타이밍
**증상**: 확률이 리셋되지 않거나 너무 빨리 리셋됨
**대응**: CombatManager의 턴 종료 시점 확인 및 명확한 호출

### 이슈 3: Priority 충돌 처리
**증상**: 여러 Action이 같은 확률을 조정할 때 충돌
**대응**: BehaviorTreeExecutor에서 Priority 정렬 및 중복 제거

### 이슈 4: 강제 행동 타입 불일치
**증상**: forcedBehavior 문자열과 실제 행동 타입 매칭 실패
**대응**: Enum 또는 명확한 문자열 규칙 정의

---

## 성공 기준

### Phase 3 완료 조건
✅ 모든 1-5단계 작업 완료
✅ 통합 테스트 검증 체크리스트 모두 통과
✅ 치명적인 버그 없이 안정적 동작
✅ 테스트 BT 에셋으로 의도한 패턴 구현 가능

### 추가 목표
✅ 명확한 디버그 로그로 문제 추적 가능
✅ 코드 문서화 및 주석 추가
✅ Phase 4 작업 계획 수립

---

## 진행 상황 체크리스트

### 1단계: 확률 Override 시스템 구현
- [ ] NPCRuntimeBehavior.cs 생성
- [ ] EnemyCombatant.cs 수정
- [ ] CombatManager.cs 연동
- [ ] 컴파일 오류 확인

### 2단계: 강제 행동 시스템 구현
- [ ] BehaviorTreeContext 확인
- [ ] 강제 행동 처리 로직 구현
- [ ] 테스트 준비

### 3단계: 로그 시스템 강화
- [ ] BehaviorTreeExecutor 로그 추가
- [ ] EnemyCombatant 로그 추가
- [ ] Condition/Action 노드 로그 추가

### 4단계: 테스트 BT 에셋 생성
- [ ] Condition 노드 에셋 생성
- [ ] Action 노드 에셋 생성
- [ ] BT 에셋 3종 생성

### 5단계: 통합 테스트
- [ ] Unity 플레이 테스트
- [ ] 검증 체크리스트 확인
- [ ] 버그 수정

### 6단계: Phase 4 사전 작업 (선택)
- [ ] BTLogger 구현
- [ ] UI 설계

---

## 메모 및 참고사항

### 설계 원칙
1. **원본 보존**: CharacterData의 npcBehavior는 절대 수정하지 않음
2. **턴별 리셋**: 매 턴 종료 시 확률을 원본으로 복원
3. **전투별 격리**: 전투마다 BT 상태 완전 초기화
4. **개체별 독립**: 같은 BT를 사용하는 NPC도 완전히 독립적

### Phase 3 핵심 개념
- **Runtime Behavior**: 원본과 현재 확률을 분리하여 BT가 안전하게 조작
- **Override System**: BT 결과를 임시로 적용하고 턴 종료 시 자동 복원
- **Priority System**: 여러 BT/Action이 같은 값을 조정해도 충돌 없이 처리

---

**작성자**: AI Assistant  
**시작 시간**: 2025년 10월 13일  
**Phase 3 목표**: BT 시스템을 실제 전투에서 사용 가능한 상태로 완성  
**Phase 3 상태**: 🔄 진행 중 → ✅ 완료 목표  

---

## 작업 로그

### 시작 (오전)
- [x] DevJournal 작성 완료
- [x] 구현 계획 검토 완료
- [x] 작업 시작

### 오전 완료 항목 ✅
1. **NPCRuntimeProbabilities 클래스 구현 완료**
   - 확률 데이터 관리 (원본 보호, 복사본 수정, 리셋)
   
2. **EnemyCombatant 확장 완료**
   - runtimeProbabilities 필드 추가 및 초기화
   - ApplyBehaviorTreeResults() TODO 제거 및 실제 구현
   - ResetProbabilities() 구현
   
3. **CombatManager 연동 완료**
   - ResetNPCProbabilities() 구현
   - 턴 종료 시 확률 리셋 호출
   
4. **턴 타이머 UI 개선 완료**
   - 잔여/전체 시간 + 진행률(%) 표시
   - 프로그레스 바 구현 (Image Fill Amount 지원)
   - Inspector 연결 가능

---

## 🚨 발견된 문제점 및 분석 (저녁 작업 대기)

### 문제 1: Enemy 검술이 UI에 표시되지 않음 ⚠️

**증상:**
- Enemy가 사용하는 검술이 검술 선택 UI에 표현되지 않음
- 버튼이 비활성화되어 포커싱 표시 등이 제대로 표현되지 않음

**원인 분석:**
1. `EnemyActionSelectUI` 존재 여부 확인 필요
2. PlayerController와 달리 EnemyController는 UI 업데이트 로직이 없을 가능성
3. 검술 선택 후 UI에 반영하는 코드 누락 가능성

**해결 계획:**
```
1. EnemyActionSelectUI 스크립트 확인
   - 존재하는가?
   - PlayerActionSelectUI와 유사한 구조인가?

2. EnemyController에 UI 업데이트 로직 추가
   - GetSelectedCommandIndex() 호출 후 UI 갱신
   - 선택된 버튼 하이라이트 처리

3. CombatManager에서 Enemy 턴 시작 시
   - EnemyActionSelectUI.UpdateSelectedButton(index) 호출
   - 시각적 피드백 제공
```

**관련 파일:**
- `Assets/Script/UI/EnemyActionSelectUI.cs` (확인 필요)
- `Assets/Script/Controller/EnemyController.cs`

---

### 문제 2: Enemy가 BT 선택 결과를 무시함 🔴 **CRITICAL**

**증상:**
- UseTestMode = false일 때도 BT의 검술 선택이 무시됨
- testCommandIndex 값을 그대로 사용함

**원인 분석:**
`EnemyController.GetSelectedCommandIndex()` 라인 135-156에서 **치명적인 버그 발견:**

```csharp
public int GetSelectedCommandIndex()
{
    int index = 0;
    if(useTestMode)
    {
        if (useRandomAction)
        {
            int len = equippedStyle.CommandSet.Count;
            if (len == 0) return testCommandIndex;
            int randomIndex = UnityEngine.Random.Range(0, len);
            index = randomIndex;
        }
        else
        {
            index = testCommandIndex;
        }
    }
    else
        index = testCommandIndex;  // ← 🔴 버그! useTestMode=false여도 testCommandIndex 사용!
    return index;
}
```

**문제점:**
- `else` 절이 잘못됨
- useTestMode=false일 때 **BT의 선택 결과를 가져와야 하는데** testCommandIndex를 반환
- `EnemyCombatant.ChooseCommand()`의 BT 결과가 완전히 무시됨

**올바른 로직:**
```csharp
public int GetSelectedCommandIndex()
{
    if (useTestMode)
    {
        // 테스트 모드: testCommandIndex 사용
        if (useRandomAction)
            return Random.Range(0, CommandCount);
        else
            return testCommandIndex;
    }
    else
    {
        // 🔥 BT 모드: Combatant의 ChooseCommand() 사용!
        var selection = Combatant?.ChooseCommand();
        return Mathf.Clamp(selection?.selectedIndex ?? 0, 0, CommandCount - 1);
    }
}
```

**해결 계획:**
```
1. EnemyController.GetSelectedCommandIndex() 수정
   - else 절에서 Combatant.ChooseCommand() 호출
   - BT의 선택 결과 반영

2. 테스트
   - UseTestMode = true: testCommandIndex 사용 (기존 동작)
   - UseTestMode = false: BT 결과 사용 (수정 후)
   
3. 로그 추가
   - BT 선택 vs 테스트 모드 선택 구분 로그
```

**관련 파일:**
- `Assets/Script/Controller/EnemyController.cs` (라인 135-156)

---

### 문제 3: Poise=0 중단 시 턴이 무한 대기 🔴 **CRITICAL**

**증상:**
- Poise를 0으로 만들어 행동을 중단시키면 턴이 넘어가지 않음
- 무한 대기 상태에 빠짐

**원인 분석:**
1. **발사체 미발사 문제:**
   ```
   행동 중단 (Poise=0)
       ↓
   애니메이션 중단
       ↓
   발사체 발사 이벤트 실행 안 됨
       ↓
   OnProjectileHit() 호출 안 됨
       ↓
   hitJudgmentCompleted[i] = true 설정 안 됨
       ↓
   EnsureAllHitJudgmentsCompleted() 무한 대기 ⚠️
   ```

2. **근본 원인:**
   - `CombatManager.EnsureAllHitJudgmentsCompleted()` (라인 703-750)
   - 모든 히트 판정이 완료될 때까지 대기
   - 중단 시 마지막 히트가 발사되지 않으면 영원히 대기

**해결 계획:**
```
1. 중단 감지 시 hitJudgmentCompleted 강제 완료 처리
   
   CombatManager.PerformTurn():
   - isInterrupted = true일 때 (라인 501)
   - 남은 모든 hitJudgmentCompleted[i] = true 설정
   - 즉시 턴 종료

2. CheckInterruptCondition() 개선:
   if (CheckInterruptCondition())
   {
       Debug.Log("턴이 중단되었습니다.");
       
       // 🔥 추가: 남은 히트 판정 강제 완료
       for (int i = CurrentHit; i < hitCount; i++)
       {
           if (!hitJudgmentCompleted[i])
           {
               hitJudgmentCompleted[i] = true;
               Debug.Log($"[중단] Hit {i} 판정 강제 완료");
           }
       }
       
       break; // 턴 종료
   }

3. isInterrupted 체크 위치 개선:
   - 라인 501의 isInterrupted 체크에도 동일 로직 추가
   - 중단 시 즉시 턴 종료 보장
```

**관련 파일:**
- `Assets/Script/Combat/CombatManager.cs` (라인 501-511, 703-750)

---

## 📋 저녁 작업 계획 (우선순위 순)

### 🔴 CRITICAL (반드시 수정)

#### 1. EnemyController BT 선택 버그 수정
**예상 소요:** 10분
```csharp
// 파일: Assets/Script/Controller/EnemyController.cs
// 위치: GetSelectedCommandIndex() 메서드 (라인 135-156)

public int GetSelectedCommandIndex()
{
    if (useTestMode)
    {
        // 테스트 모드
        if (useRandomAction)
            return Random.Range(0, CommandCount);
        else
            return testCommandIndex;
    }
    else
    {
        // BT 모드: Combatant의 ChooseCommand() 사용
        var selection = Combatant?.ChooseCommand();
        int btIndex = selection?.selectedIndex ?? 0;
        Debug.Log($"[EnemyController] BT 모드 - 선택된 인덱스: {btIndex}");
        return Mathf.Clamp(btIndex, 0, CommandCount - 1);
    }
}
```

#### 2. Poise 중단 시 무한 대기 해결
**예상 소요:** 20분
```csharp
// 파일: Assets/Script/Combat/CombatManager.cs
// 위치: PerformTurn() 메서드

// 라인 501-511 수정:
if (isInterrupted)
{
    Debug.LogWarning("[PerformTurn] 중단 발생으로 턴이 조기 종료됩니다.");
    
    // 남은 히트 판정 강제 완료
    ForceCompleteRemainingHits(CurrentHit, hitCount);
    
    break;
}

if (CheckInterruptCondition())
{
    Debug.Log("턴이 중단되었습니다.");
    
    // 남은 히트 판정 강제 완료
    ForceCompleteRemainingHits(CurrentHit, hitCount);
    
    break;
}

// 새 메서드 추가:
private void ForceCompleteRemainingHits(int currentHit, int totalHits)
{
    Debug.Log($"[중단] 남은 히트 판정 강제 완료: {currentHit} ~ {totalHits-1}");
    for (int i = currentHit; i < totalHits; i++)
    {
        if (i < hitJudgmentCompleted.Length && !hitJudgmentCompleted[i])
        {
            hitJudgmentCompleted[i] = true;
            Debug.Log($"  - Hit {i}: 강제 완료");
        }
    }
}
```

### ⚠️ HIGH (중요)

#### 3. Enemy UI 표시 문제 해결
**예상 소요:** 30분
```
1. EnemyActionSelectUI 스크립트 확인
   - grep으로 검색: "EnemyActionSelectUI"
   - 없으면 생성 필요

2. EnemyController에 UI 업데이트 추가
   - GetSelectedCommandIndex() 호출 후
   - UI 갱신 메서드 호출

3. CombatManager에서 Enemy 턴 시작 시
   - Enemy 선택 UI 업데이트
```

---

## 🚀 저녁 작업 재개 프롬프트

```
Phase 3 확률 Override 시스템 구현이 완료되었습니다.
오전에 발견된 3가지 CRITICAL 버그를 수정하려고 합니다.

발견된 버그:
1. EnemyController.GetSelectedCommandIndex()에서 UseTestMode=false여도 BT 결과를 무시하고 testCommandIndex를 반환하는 버그
2. Poise=0으로 중단 시 마지막 발사체가 발사되지 않아 hitJudgmentCompleted가 완료되지 않아 무한 대기하는 버그  
3. Enemy 검술 선택이 UI에 표시되지 않는 문제

우선순위에 따라 순서대로 수정해주세요:
1. EnemyController BT 선택 버그 (CRITICAL)
2. Poise 중단 무한 대기 버그 (CRITICAL)
3. Enemy UI 표시 문제

각 버그의 상세 분석과 수정 계획은 DevJournal_20251013.md의
"🚨 발견된 문제점 및 분석" 섹션을 참고하세요.
```

---

## 📊 Phase 3 완료 현황

### ✅ 완료된 기능
- NPCRuntimeProbabilities 클래스 (확률 관리)
- EnemyCombatant 확장 (BT 결과 적용)
- CombatManager 연동 (턴 종료 시 리셋)
- 턴 타이머 UI 개선

### 🔴 발견된 버그 (수정 대기)
1. EnemyController BT 선택 무시
2. Poise 중단 시 무한 대기
3. Enemy UI 미표시

### ⏳ 대기 중
- 테스트 BT 에셋 생성
- Unity 플레이 테스트
- 버그 수정 후 최종 검증

---

**다음 목표**: 저녁에 CRITICAL 버그 3개 수정 후 Phase 3 완전 완료

