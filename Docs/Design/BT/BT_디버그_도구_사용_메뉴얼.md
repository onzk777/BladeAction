# BT 디버그 도구 사용 메뉴얼

## 개요

Behavior Tree 디버그 도구는 BT 실행을 **보기 쉽게 정리된 UI**로 표시하는 시스템입니다.  
단순히 Console 로그만 쌓는 방식이 아닌, **실시간 모니터링**과 **히스토리 추적**을 제공합니다.

---

## 구성 요소

### 1. BTLogger
- BT 실행 과정을 기록하는 로깅 시스템
- Console 로그 출력 (개발 중)
- BTLogHistory에 데이터 저장 (UI 표시용)

### 2. BTLogHistory
- BT 실행 기록을 메모리에 저장
- 최대 50개 히스토리 보관
- 조건 평가, 액션 실행, 확률 변경 등 모든 데이터 포함

### 3. BTMonitorUI
- Unity UI로 정리된 BT 상태 표시
- 4개 영역:
  1. **일반 정보**: 현재 턴, 공격자
  2. **Enemy BT 상태**: 실시간 확률, Override 상태
  3. **Player BT 상태**: (향후 자동 전투 대비)
  4. **실행 히스토리**: 최근 N개 BT 평가 기록

---

## Unity 설정

### 1단계: DebugPanel 준비

DebugPanel이 없으면 Canvas 하위에 생성합니다.

```
Canvas
└── DebugPanel (Panel UI)
    ├── GeneralInfoText (TextMeshPro)
    ├── EnemyBTStatusText (TextMeshPro)
    ├── PlayerBTStatusText (TextMeshPro)
    └── HistoryText (TextMeshPro)  ← 스크롤 영역 추천
```

### 2단계: TextMeshPro 추가

1. **GeneralInfoText**
   - 위치: 상단
   - 크기: 작게 (턴 정보만 표시)
   
2. **EnemyBTStatusText**
   - 위치: 좌측 상단
   - 크기: 중간 (Enemy 상태 표시)
   
3. **PlayerBTStatusText**
   - 위치: 우측 상단
   - 크기: 중간 (Player 상태 표시)
   
4. **HistoryText** ✨ 중요!
   - 위치: 하단 또는 별도 ScrollView
   - 크기: 크게 (히스토리 10개 표시)
   - **Scroll View 안에 배치 권장**

### 3단계: BTMonitorUI 컴포넌트 추가

1. DebugPanel에 `BTMonitorUI` 컴포넌트 추가
2. Inspector에서 TextMeshPro 4개 연결:
   - General Info Text → GeneralInfoText
   - Enemy BT Status Text → EnemyBTStatusText
   - Player BT Status Text → PlayerBTStatusText
   - History Text → HistoryText ✨
3. 설정 조정:
   - **Update Interval**: 0.5초 (기본값)
   - **Max History Display**: 10개 (기본값, 필요시 조정)

### 4단계: F3 키 토글 설정 (선택)

`DebugPanelController`가 이미 있으면 F3 키로 토글 가능합니다.

---

## 사용 방법

### 기본 사용

1. **Unity 실행 → F3 키**
   - DebugPanel 표시/숨김

2. **전투 진행**
   - BT가 평가될 때마다 히스토리에 기록됨
   - 실시간 확률 변화 모니터링

3. **히스토리 확인**
   - 최신 로그가 상단에 표시 (최신순)
   - 각 턴별로 어떤 Entry가 실행되었는지 확인

### BTLogger 제어

**스크립트에서 로그 On/Off**:
```csharp
using BladeAction.BT;

// 전체 로그 비활성화 (성능 모드)
BTLogger.EnableLogging = false;

// 특정 로그만 비활성화
BTLogger.EnableConditionLogging = false; // 조건 평가 로그
BTLogger.EnableActionLogging = false;     // 액션 실행 로그
BTLogger.EnableProbabilityLogging = false; // 확률 변경 로그

// 상세 로그 (성능 영향 주의!)
BTLogger.EnableVerboseLogging = true;
```

**권장 설정**:
- **개발 중**: 모든 로그 활성화, Verbose ON
- **테스트**: 기본 로그만, Verbose OFF
- **릴리즈**: 전체 로그 OFF

### 히스토리 관리

**히스토리 클리어** (스크립트):
```csharp
// BTMonitorUI 참조
BTMonitorUI monitor = FindObjectOfType<BTMonitorUI>();
monitor.ClearHistory();
```

**최대 로그 수 조정** (스크립트):
```csharp
BladeAction.BT.BTLogHistory.Instance.MaxLogCount = 100; // 기본 50
```

---

## UI 표시 내용

### 일반 정보
```
╔═══ BT 모니터 ═══╗
║ 턴: 3
║ 공격자: Enemy
╚═════════════════╝
```

### Enemy BT 상태
```
╔═══ Enemy: Goblin ═══╗
║ BT 수: 1개
║ ━━━ 확률 상태 ━━━
║ 공격 성공률: 80%
║ 쳐내기 성공률: 50%
║ 막기 시도율: 60%
║ 막기중 쳐내기: O
║ 막기중 성공률: 70%
║ ━━━ BT Override ━━━
║ 활성화 (2개)
║  • AttackPerfectRate: 80%
║  • GuardAttemptRate: 60%
║ 선택 검술: 인덱스 1
╚═════════════════════════╝
```

### 실행 히스토리 ✨ 핵심!
```
╔═══ BT 실행 히스토리 (최근 5개) ═══╗
║ ⚔ 턴 3 | Goblin | ✓
║   Entry[0]: HP < 50%
║   조건: 1/1 통과
║   액션: 3/3 실행
║   확률: AttackPerfectRate=80%, GuardAttemptRate=60%
║ ───────────────────────
║ 🛡 턴 2 | Player | ✗
║   조건: 0/2 통과
║   액션: 0/0 실행
║ ───────────────────────
║ ⚔ 턴 2 | Goblin | ✓
║   Entry[1]: HP >= 50%
║   조건: 1/1 통과
║   액션: 2/2 실행
║   확률: AttackPerfectRate=50%
╚═══════════════════════════════════════════╝
```

**아이콘 의미**:
- ⚔: 공격 턴
- 🛡: 방어 턴
- ✓: Entry 매칭 성공 (녹색)
- ✗: 매칭 실패 (빨간색)

---

## 히스토리 분석

### 읽는 방법

**예시**:
```
║ ⚔ 턴 5 | Goblin | ✓
║   Entry[2]: 턴 5 이상
║   조건: 1/1 통과
║   액션: 2/3 실행
║   확률: AttackPerfectRate=100%, GuardAttemptRate=90%
```

**분석**:
1. **턴 5, Goblin 공격 턴**
2. **Entry[2] "턴 5 이상" 조건 만족**
3. **1개 조건 평가, 1개 통과**
4. **3개 액션 중 2개 실행** (1개는 건너뜀, 예: executeOncePerCombat)
5. **2개 확률 조정**: 공격 성공률 100%, 막기 시도율 90%

### 문제 추적

#### 케이스 1: BT가 동작하지 않음
```
║ ⚔ 턴 3 | Goblin | ✗
║   조건: 0/2 통과
║   액션: 0/0 실행
```

**분석**:
- 모든 Entry의 조건이 불만족
- 액션이 실행되지 않음

**해결**:
1. Entry 조건 확인 (HP, 턴 수 등)
2. 조건 임계값 조정
3. Fallback Entry 추가 (항상 true 조건)

#### 케이스 2: 일부 액션만 실행됨
```
║ ⚔ 턴 4 | Goblin | ✓
║   Entry[0]: HP < 50%
║   조건: 1/1 통과
║   액션: 2/4 실행
```

**분석**:
- 4개 액션 중 2개만 실행됨
- 나머지 2개는 건너뛰기됨

**가능한 원인**:
- executeOncePerCombat = true인 액션이 이미 실행됨
- Console 로그에서 "⊘ 건너뜀" 확인

#### 케이스 3: 확률 Override가 적용 안 됨
```
║ ⚔ 턴 3 | Goblin | ✓
║   Entry[0]: HP < 50%
║   조건: 1/1 통과
║   액션: 1/1 실행
║   확률: (없음)
```

**분석**:
- 액션은 실행되었지만 확률 변경이 없음

**가능한 원인**:
- BTAction_ProbabilityAdjustment가 없음
- 액션이 다른 타입 (CommandSelection 등)

---

## Console 로그 vs UI 히스토리

### Console 로그

**장점**:
- 상세한 디버그 정보
- Verbose 모드 시 HP, Poise 값까지 표시
- 개발 중 문제 추적

**단점**:
- 로그가 계속 쌓여서 찾기 어려움
- 이전 턴 정보 확인 불편
- 성능 영향 (많은 로그)

**사용 시점**:
- 특정 조건/액션 상세 분석
- 버그 추적
- BT 에셋 제작 중

### UI 히스토리 ✨ 추천

**장점**:
- 정리된 요약 정보
- 최근 N개만 표시 (깔끔)
- 한눈에 패턴 파악 가능
- 실시간 확률 변화 모니터링
- 성능 영향 적음 (0.5초 간격 업데이트)

**단점**:
- 상세 정보는 부족 (상세 정보는 Console 로그 참고)

**사용 시점**:
- 전투 중 BT 동작 확인
- 턴별 패턴 분석
- 테스트 플레이
- QA 테스트

---

## 활용 예시

### 예시 1: HP 기반 BT 테스트

**목표**: HP 50% 이하일 때 방어적으로 변하는지 확인

**BT 설정**:
```
Entry[0]: HP < 50%
  Condition: BTCondition_HPComparison (Self, Less, 0.5, Percentage)
  Actions:
    - ProbabilityAdjustment: GuardAttemptRate = 90%
    - ProbabilityAdjustment: ParryPerfectRate = 80%
```

**테스트**:
1. Unity 실행 → F3 키로 디버그 패널 표시
2. 전투 진행하며 Enemy HP 감소
3. **히스토리 확인**:

```
HP > 50% 일 때:
║ ⚔ 턴 2 | Goblin | ✗
║   조건: 0/1 통과           ← HP < 50% 불만족
║   확률: (없음)

HP < 50% 일 때:
║ ⚔ 턴 5 | Goblin | ✓
║   Entry[0]: HP < 50%       ← 조건 만족!
║   조건: 1/1 통과
║   액션: 2/2 실행
║   확률: GuardAttemptRate=90%, ParryPerfectRate=80%  ← 확률 변경 확인!
```

**확인 사항**:
- ✅ HP 50% 이하에서 Entry[0] 실행
- ✅ 막기/쳐내기 확률 상승
- ✅ **Enemy BT 상태** 섹션에서도 확률 90%, 80% 표시

---

### 예시 2: 턴별 검술 선택 테스트

**목표**: 턴 3부터 특정 검술 사용

**BT 설정**:
```
Entry[0]: TurnCount >= 3
  Condition: BTCondition_TurnCount (GreaterOrEqual, 3)
  Actions:
    - CommandSelection: ByIndex, 2
```

**테스트**:
1. 히스토리에서 턴별 확인

```
턴 1-2:
║ ⚔ 턴 1 | Goblin | ✗
║   조건: 0/1 통과           ← 턴 < 3

║ ⚔ 턴 2 | Goblin | ✗
║   조건: 0/1 통과

턴 3 이후:
║ ⚔ 턴 3 | Goblin | ✓
║   Entry[0]: TurnCount >= 3
║   조건: 1/1 통과
║   액션: 1/1 실행
```

2. **Enemy BT 상태** 확인:
```
║ 선택 검술: 인덱스 2        ← BT가 검술 지정!
```

---

### 예시 3: executeOncePerCombat 검증

**목표**: 특정 액션이 전투당 1회만 실행되는지 확인

**BT 설정**:
```
Entry[0]: HP < 30%
  Actions:
    - ProbabilityAdjustment: AttackPerfectRate = 100%
      executeOncePerCombat = true  ← 1회만
```

**테스트**:

```
첫 실행 (턴 4):
║ ⚔ 턴 4 | Goblin | ✓
║   Entry[0]: HP < 30%
║   조건: 1/1 통과
║   액션: 1/1 실행           ← 정상 실행
║   확률: AttackPerfectRate=100%

재실행 시도 (턴 6):
║ ⚔ 턴 6 | Goblin | ✓
║   Entry[0]: HP < 30%
║   조건: 1/1 통과
║   액션: 0/1 실행           ← 1회 제한으로 건너뜀!
║   확률: (없음)
```

**Console 로그**:
```
⊘ BTAction_ProbabilityAdjustment: 공격 성공률 100% - 이미 실행됨 (executeOncePerCombat)
```

---

## 성능 고려사항

### UI 업데이트 간격
```csharp
// BTMonitorUI Inspector
Update Interval = 0.5f  // 기본값 (0.5초마다 갱신)
```

**조정 가이드**:
- **빠른 반응** (0.2~0.3초): 턴이 빠른 게임
- **기본** (0.5초): 대부분 상황
- **성능 우선** (1.0초): 저사양 기기

### 히스토리 수 제한
```csharp
// BTMonitorUI Inspector
Max History Display = 10  // 기본값 (10개 표시)
```

**조정 가이드**:
- **5개**: 간단한 확인용
- **10개**: 기본 (권장)
- **20개**: 긴 전투 분석

**메모리**:
- BTLogHistory는 최대 50개 보관
- UI는 최근 N개만 표시
- 오래된 로그는 자동 삭제

---

## 문제 해결

### Q1: 히스토리가 표시되지 않음

**확인 사항**:
1. `historyText`가 Inspector에 연결되었는지 확인
2. BTLogger.EnableLogging = true인지 확인
3. BT가 실제로 평가되는지 Console 로그 확인

**해결**:
```csharp
// 강제 업데이트
BTMonitorUI monitor = FindObjectOfType<BTMonitorUI>();
monitor.ForceUpdate();
```

### Q2: 히스토리가 너무 길어서 읽기 어려움

**해결**:
1. **ScrollView 사용**:
   - HistoryText를 ScrollView 안에 배치
   - Content Size Fitter 추가

2. **표시 수 줄이기**:
   ```
   Max History Display = 5  // Inspector에서 조정
   ```

### Q3: 성능 저하

**해결**:
1. **업데이트 간격 늘리기**:
   ```
   Update Interval = 1.0f  // 1초로 변경
   ```

2. **Verbose 로그 비활성화**:
   ```csharp
   BTLogger.EnableVerboseLogging = false;
   ```

3. **Console 로그 비활성화**:
   ```csharp
   BTLogger.EnableLogging = false;  // UI만 사용
   ```

---

## 릴리즈 빌드 설정

**권장 설정**:
```csharp
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    // 개발/에디터: 로그 활성화
    BTLogger.EnableLogging = true;
    BTLogger.EnableVerboseLogging = false;  // Verbose는 OFF
#else
    // 릴리즈: 완전 비활성화
    BTLogger.EnableLogging = false;
#endif
```

**DebugPanel 숨김**:
```csharp
// 릴리즈 빌드에서 F3 키 비활성화
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
    gameObject.SetActive(false);
#endif
```

---

## 요약

### 핵심 기능
1. ✅ **실시간 BT 상태**: Enemy/Player의 현재 확률 모니터링
2. ✅ **실행 히스토리**: 최근 N개 BT 평가 기록, 한눈에 패턴 파악
3. ✅ **성능 최적화**: 0.5초 간격 업데이트, 최대 50개 히스토리
4. ✅ **보기 쉬운 UI**: 색상 코드, 아이콘, 정리된 레이아웃

### 활용 시나리오
- ✅ BT 에셋 제작 및 검증
- ✅ 전투 중 패턴 분석
- ✅ 턴별 확률 변화 추적
- ✅ 버그 발견 및 디버깅
- ✅ QA 테스트 지원

### 효과
- ⚡ **개발 속도**: BT 문제 즉시 파악
- 🎯 **정확성**: 의도한 대로 동작하는지 확인
- 📊 **데이터**: 실제 전투 데이터 수집
- 🐛 **디버깅**: 문제 발생 시점 명확히 파악

---

**문서 버전**: 1.0  
**작성일**: 2025년 10월 14일  
**대상**: 개발자, QA 테스터

