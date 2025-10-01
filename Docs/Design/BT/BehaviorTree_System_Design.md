# Behavior Tree 시스템 기획서

## 1. 시스템 개요

### 1.1 목적
- **NPC 전투 패턴 제어**: 턴제 기반 전투에서 NPC의 행동을 동적으로 제어
- **조건부 행동 시스템**: 전투 상황에 따른 NPC 행동 확률 조정 및 강제 행동 설정
- **개체별 특성화**: 각 NPC마다 고유한 행동 패턴과 특성 부여

### 1.2 핵심 개념
- **턴 기반 결정**: "이번 턴에 어떤 행동을 할지"를 BT로 결정
- **CharacterData 중심**: 모든 NPC 행동 확률은 CharacterData에 필수 설정
- **확률 Override**: CharacterData 확률 수치를 임시로 덮어쓰는 시스템
- **지속 시간 관리**: BT 효과의 턴 단위 지속 시간 관리
- **원본 수치 보존**: BT 적용 전 CharacterData 원본 확률 수치 보존 및 복원

## 2. 시스템 구조

### 2.1 전체 아키텍처
```
CharacterData (NPC별 기본 확률 수치 + BT 리스트)
    ↓
Behavior Tree (동적 확률 Override)
    ↓
EnemyController (AI 행동 결정 및 제어)
    ↓
EnemyCombatant (CharacterData 보관 및 정보 제공)
```

### 2.2 컴포넌트별 역할
- **CharacterData**: NPC별 기본 확률 수치 보관 (필수), BT 리스트 보유
- **Behavior Tree**: 동적 확률 Override 및 행동 결정 로직
- **EnemyController**: AI 행동 결정 및 제어 로직 담당
- **EnemyCombatant**: CharacterData 보관 및 기본 정보 제공
- **GlobalConfig**: Default BT 지정 (CharacterData에 BT가 없을 때만 사용)

### 2.3 BT 구조
- **BT = Condition(s) + Action Node(s) 쌍의 리스트**
- **순서**: 리스트 인덱스가 곧 우선순위 (상위 조건 만족 시 하위 미체크)
- **평가 타이밍**: 턴 시작 시 1회만 평가

## 3. BT 노드 타입 정의

### 3.1 Condition Nodes (조건 노드)

#### 3.1.1 HP 비교 조건
- **기능**: NPC와 적(플레이어)의 HP 비교
- **비교 연산자**: `>`, `<`, `>=`, `<=`, `==`, `!=`
- **임계값 설정**: 절대값 또는 비율값

#### 3.1.2 자세 게이지 비교 조건
- **기능**: NPC와 적의 자세 포인트 비교
- **비교 연산자**: `>`, `<`, `>=`, `<=`, `==`, `!=`
- **임계값 설정**: 절대값 또는 비율값

#### 3.1.3 턴 타입 조건
- **기능**: 현재 턴이 공격 턴인지 방어 턴인지 확인
- **조건값**: `AttackTurn`, `DefenseTurn`

#### 3.1.4 턴 수 조건
- **기능**: 현재 턴 번호 확인
- **비교 연산자**: `>`, `<`, `>=`, `<=`, `==`, `!=`

#### 3.1.5 비교 대상 설정
- **Self**: 자신(NPC)의 수치 확인
- **Target**: 상대(플레이어)의 수치 확인
- **조합**: Composite Node로 AND/OR 조건 조합 가능

### 3.2 Action Nodes (행동 노드)

#### 3.2.1 확률 조정 액션
- **기능**: 특정 행동의 성공 확률을 임시로 변경
- **대상**: 공격 성공률, 막기 시도 확률, 쳐내기 성공률 등
- **방식**: 절대값 설정 또는 상대값 증감
- **Priority**: 우선순위 (정수, 음수 불가)

#### 3.2.2 강제 행동 액션
- **기능**: 특정 행동을 확정적으로 수행하도록 설정
- **대상**: 막기, 쳐내기, 특정 방어 행동 등
- **특징**: 확률 체크 우선, 다른 확률 조정 노드 무시
- **Priority**: 우선순위 (정수, 음수 불가)

#### 3.2.3 검술 선택 액션
- **기능**: 이번 턴에 사용할 검술(ActionCommand) 지정
- **선택 방식 1**: 검술 Index 직접 지정
- **선택 방식 2**: Tag 기반 랜덤 선택 (지정 Tag 포함 검술 중 랜덤)
- **Priority**: 우선순위 (정수, 음수 불가)

#### 3.2.4 행동 비활성화 액션
- **기능**: 특정 행동을 임시로 비활성화
- **대상**: 막기 중 쳐내기 시도 등
- **방식**: 해당 확률을 0%로 설정
- **Priority**: 우선순위 (정수, 음수 불가)

### 3.3 Composite Nodes (복합 노드)

#### 3.3.1 Sequence Node (AND)
- **기능**: 여러 Condition을 AND 조건으로 묶음
- **실행 조건**: 모든 자식 Condition이 true일 때만 연결된 Action 실행
- **사용 예**: "내 HP < 30% AND 상대 HP > 70%" 동시 만족

#### 3.3.2 Selector Node (OR)
- **기능**: 여러 Condition을 OR 조건으로 묶음
- **실행 조건**: 자식 Condition 중 하나라도 true면 연결된 Action 실행
- **사용 예**: "내 HP < 20% OR 내 자세 < 30%" 둘 중 하나 만족

#### 3.3.3 사용 방법
- **메뉴얼 필요**: 구현 시 상세 사용법 및 예시 작성 예정
- **중첩 가능**: Composite Node 안에 다른 Composite Node 포함 가능

## 4. 지속 시간 시스템 (낮은 우선순위)

### 4.1 Additional Turn Duration 개념
- **정의**: BT 액션 효과가 추가로 지속되는 턴 수
- **값 의미**:
  - `-1`: 영구 지속 (수동 해제 필요)
  - `0`: 해당 턴만 적용
  - `1 이상`: 지정된 턴 수만큼 추가 지속
- **우선순위**: 낮음 (추후 구현 예정)

### 4.2 지속 시간 관리 (추후 구현)
- **시작**: 액션 실행 시점
- **카운트다운**: 매 턴마다 Additional Turn Duration 감소
- **종료**: 값이 0이 되면 원본 수치로 복원
- **영구 효과**: -1일 경우 수동 해제 또는 특정 조건으로 해제

### 4.3 중첩 효과 처리
- **Priority 기반**: 동일 수치 조정 시 Priority 높은 Action만 적용
- **단일 효과**: 효과 중첩 없음, 하나의 Action만 활성화

## 5. NPC 행동 확률 시스템

### 5.1 CharacterData 확장
```yaml
NPCBehaviorProbabilities:
  attackPerfectRate: float        # 공격 성공률 (0~1)
  parryPerfectRate: float         # 쳐내기 성공률 (0~1)
  guardAttemptRate: float         # 막기 시도 확률 (0~1)
  parryWhileGuarding: bool        # 막기 중 쳐내기 시도 여부
  parryWhileGuardingRate: float   # 막기 중 쳐내기 성공률 (0~1)

# 기본값: 모두 0.0f (false for bool)
# 범위: 0.0 ~ 1.0 (확률 기반)
```

### 5.1.2 ActionCommandData Tag 확장
```yaml
ActionCommandData:
  tags: List<string>              # 검술 태그 리스트 (예: "필살기", "원거리")
```

### 5.1.3 BT 리스트 추가
```yaml
CharacterData:
  behaviorTrees: List<BehaviorTreeData>  # NPC BT 리스트
  
GlobalConfig:
  defaultBehaviorTree: BehaviorTreeData  # CharacterData에 BT 없을 때 사용
```

### 5.2 확률 수치 관리
- **원본 수치**: CharacterData에서 로드 (필수 설정)
- **현재 수치**: BT Override 적용된 실시간 수치
- **복원 메커니즘**: Duration Turn 종료 시 CharacterData 원본 수치로 자동 복원
- **제어 흐름**: EnemyController → Behavior Tree → 확률 수치 조정 → 행동 결정

### 5.3 NPC별 특성화 예시
- **공격형 NPC**: 높은 공격 성공률, 낮은 막기 시도
- **방어형 NPC**: 높은 막기 시도, 낮은 쳐내기 성공률
- **균형형 NPC**: 모든 행동의 중간 확률
- **전문형 NPC**: 특정 행동의 매우 높은 성공률

## 6. BT 실행 규칙

### 6.1 BT 평가 순서
1. **턴 시작 시 1회 평가**: 턴 중간 재평가 없음
2. **순차 체크**: BT 내 Condition + Action 쌍을 리스트 순서대로 체크
3. **조기 종료**: 상위 Condition 만족 시 하위 Condition 미체크
4. **리스트 인덱스 = 우선순위**: 0번 인덱스가 최우선

### 6.2 여러 BT 처리
- **순차 실행**: CharacterData BT 리스트를 순서대로 모두 평가
- **Default BT**: CharacterData에 BT 없을 때만 GlobalConfig의 Default BT 사용
- **조건 독립**: 각 BT는 독립적으로 조건 체크

### 6.3 턴 타입별 동작
- **Attack Turn**: 공격 검술 선택, 공격 확률 조정
- **Defense Turn**: 방어 행동 선택, 막기/쳐내기 확률 조정
- **공통**: 턴 타입 Condition으로 분기 가능

## 7. 구현 고려사항

### 7.1 성능 최적화
- **조건 체크 최적화**: 불필요한 조건 체크 방지
- **메모리 관리**: BT 효과 종료 시 메모리 해제
- **캐싱**: 자주 사용되는 조건 결과 캐싱

### 7.2 확장성
- **새로운 조건 노드**: 쉽게 추가 가능한 구조
- **새로운 액션 노드**: 플러그인 방식으로 확장
- **복합 조건**: 복잡한 조건 조합 지원

### 7.3 디버깅 및 테스트
- **BT 실행 로그**: 조건 체크 및 액션 실행 로그
- **확률 수치 모니터링**: 실시간 확률 변화 추적
- **시각적 편집기**: ScriptableObject + Inspector 기반 BT 편집
- **우선순위 표시**: List 인덱스로 우선순위 시각화

### 7.4 노드 사양 문서
- **Condition Node List**: 각 Condition 노드의 상세 사양 및 사용법
- **Action Node List**: 각 Action 노드의 상세 사양 및 사용법
- **Composite Node 메뉴얼**: AND/OR 노드 사용법 및 예시

## 8. 향후 확장 계획

### 8.1 고급 기능
- **학습 시스템**: 플레이어 패턴 학습 기반 BT 조정
- **상황별 BT**: 전투 상황에 따른 BT 전환
- **협력 AI**: 다수 NPC 간 협력 행동

### 8.2 편의 기능
- **BT 템플릿**: 미리 정의된 BT 패턴
- **BT 프리셋**: NPC별 기본 BT 설정
- **실시간 편집**: 게임 실행 중 BT 수정

---

**문서 버전**: 1.0  
**작성일**: 2024년  
**최종 수정일**: 2024년
