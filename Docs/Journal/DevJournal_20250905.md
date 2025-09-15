# 개발 일지 - 2025년 9월 5일

## 오늘의 주요 작업

### 1. 완벽 입력 시스템 구조적 문제 해결

**문제 상황:**
- TurnDuration 설정에 따라 완벽 입력이 불가능한 상태 발생
- AdditionalTurnDuration이 올바르게 동작하지 않음
- 턴 지속 시간 참조가 일관되지 않음

**원인 분석:**
1. `GlobalConfig`의 `TurnDurationSeconds`가 실제 턴 지속 시간과 다른 의미로 사용됨
2. `IsInBufferPeriod()` 메서드에서 잘못된 턴 지속 시간 참조
3. `CalculateTurnDuration`과 `IsInBufferPeriod` 간의 불일치

**해결 방안:**
- **방안 A 채택**: CombatManager에서 현재 턴 지속 시간을 전역으로 관리

### 2. GlobalConfig 변수명 및 의미 재정의

**변경 내용:**
```csharp
// 기존
[SerializeField] private float turnDurationSeconds = 3f;
public float TurnDurationSeconds => turnDurationSeconds;

// 변경 후
[SerializeField] private float additionalTurnDuration = 0f;
[Tooltip("마지막 히트 완료 후 추가 턴 지속 시간 (초) - 빠른 템포 테스트용")]
public float AdditionalTurnDuration => additionalTurnDuration;
```

**의미 변경:**
- `TurnDurationSeconds` → `AdditionalTurnDuration`
- 실제 턴 지속 시간 = 마지막 히트 완료 시간 + AdditionalTurnDuration

### 3. CombatManager에 CurrentTurnDuration 프로퍼티 추가

**구현 내용:**
```csharp
// 현재 턴 지속 시간 (전역 접근 가능)
public float CurrentTurnDuration { get; private set; } = 0f;

// PerformTurn 메서드에서 설정
float turnDuration = CalculateTurnDuration(command);
CurrentTurnDuration = turnDuration; // 전역 접근 가능하도록 설정
```

### 4. BaseInputHandler의 IsInBufferPeriod 메서드 수정

**수정 전:**
```csharp
float turnDuration = GlobalConfig.Instance.TurnDurationSeconds;
bool inEndBuffer = relativeTime >= (turnDuration - GlobalConfig.Instance.AdditionalTurnDuration);
```

**수정 후:**
```csharp
// CombatManager에서 현재 턴 지속 시간을 가져옴
float turnDuration = CombatManager.Instance?.CurrentTurnDuration ?? 1.0f;
bool inEndBuffer = relativeTime >= (turnDuration - GlobalConfig.Instance.InputBufferEndSeconds);
```

### 5. AttackerInputHandler의 RegisterHitTiming 메서드 개선

**문제:**
- `loadedTimings`가 설정되지 않아 완벽 입력 판정에 문제 발생

**해결:**
```csharp
public override void RegisterHitTiming(PerfectTimingWindow timing)
{
    currentTiming = timing;
    loadedTimings = new List<PerfectTimingWindow> { timing }; // loadedTimings도 설정
#if UNITY_EDITOR
    Debug.Log($"[AttackerInputHandler] Registered Timing: start={timing.start}, duration={timing.duration}");
#endif
}
```

## 기술적 개선사항

### 1. 턴 지속 시간 계산 로직 통일
- **실제 턴 지속 시간** = 마지막 히트 완료 시간 + `AdditionalTurnDuration`
- 모든 참조 지점에서 `CombatManager.CurrentTurnDuration` 사용

### 2. 버퍼 구간 로직 개선
- **시작 버퍼**: 턴 시작 0.1초 동안 입력 무시
- **종료 버퍼**: 턴 종료 0.1초 전부터 입력 무시
- `InputBufferStartSeconds`, `InputBufferEndSeconds` 사용

### 3. 입력 핸들러 일관성 확보
- `AttackerInputHandler`와 `DefenderInputHandler` 모두 `loadedTimings` 설정
- 완벽 입력 판정 로직 통일

## 테스트 결과

### ✅ 성공한 테스트
1. **완벽 입력 정상 동작**: TurnDuration을 5초로 설정해도 완벽 입력 가능
2. **AdditionalTurnDuration 적용**: 마지막 히트 완료 후 추가 시간 정상 적용
3. **버퍼 구간 동작**: 턴 시작/종료 시 입력 무시 정상 동작

### 🔧 개선된 점
- 턴 지속 시간 참조의 일관성 확보
- 완벽 입력 시스템의 안정성 향상
- 코드 구조의 명확성 개선

## 다음 작업 계획

### 1. 추가 테스트 필요사항
- 다양한 턴 지속 시간 설정에서의 안정성 확인
- 연타 공격에서의 타이밍 윈도우 동작 검증

### 2. 코드 정리
- 불필요한 디버그 로그 정리
- 주석 및 문서화 개선

### 3. 성능 최적화
- `CombatManager.Instance?.CurrentTurnDuration` 호출 최적화 검토

## 학습한 점

1. **설계 일관성의 중요성**: 같은 개념을 다른 곳에서 다르게 참조하면 예상치 못한 버그 발생
2. **전역 상태 관리**: 싱글톤 패턴을 활용한 전역 상태 관리의 효과적 활용
3. **점진적 리팩터링**: 기존 구조를 크게 변경하지 않고도 문제 해결 가능

## 작업 시간
- **총 작업 시간**: 약 2시간
- **문제 분석**: 30분
- **코드 수정**: 1시간
- **테스트 및 검증**: 30분

---
*작성자: AI Assistant*  
*작성일: 2025년 9월 5일*

