# 개발 일지 - 2025년 9월 27일

## 오늘의 목표
1. **멀티 히트 방어 시스템 수정**
   - 첫 번째 Hit 외의 후속 Hit에 대한 방어 입력 처리 개선
   - Hit Index 기반 발사체 추적 시스템 구현

2. **플레이어 막기 애니메이션 지속성 개선**
   - 막기 입력 유지 시 애니메이션 지속되도록 수정
   - 입력 해제 또는 방어 턴 종료 시에만 애니메이션 종료

3. **AI 방어 입력 시스템 복구**
   - 발사체 기반으로 전환하면서 손실된 AI 방어 입력 기능 복구
   - GlobalConfig AI 설정 연동

4. **AI 의사결정 시스템 모듈화**
   - 확장 가능한 AI 의사결정 시스템 설계
   - 쳐내기/막기 의사결정 분리

5. **AI 막기 기능 구현**
   - AI 막기 의사결정 및 실행 시스템
   - 막기 중 쳐내기 시도 제어 기능

## 진행 상황
- [x] 멀티 히트 방어 시스템 수정
- [x] 플레이어 막기 애니메이션 지속성 개선
- [x] AI 방어 입력 시스템 복구
- [x] AI 의사결정 시스템 모듈화
- [x] AI 막기 기능 구현
- [x] AI 막기 애니메이션 제어 문제 해결
- [x] AI 막기 판정 반영 문제 해결
- [x] NpcInputDifficulty 관련 로직 제거

## 예상 소요 시간
- 멀티 히트 방어 수정: 45분
- 막기 애니메이션 개선: 30분
- AI 방어 입력 복구: 60분
- AI 의사결정 모듈화: 90분
- AI 막기 기능 구현: 90분
- 버그 수정 및 테스트: 60분
- **총 예상 시간: 5시간 15분**

## 구현 완료 내용

### 1. 멀티 히트 방어 시스템 수정 ✅
- **문제**: 첫 번째 Hit 외의 후속 Hit에 대한 방어 입력이 처리되지 않음
- **원인**: `currentProjectile` 단일 변수로 인한 후속 Hit 덮어쓰기
- **해결**: 
  - `Dictionary<int, Projectile> projectilesByHitIndex` 도입
  - Hit Index별 상태 추적 시스템 구현
  - `perfectInputSucceededByHitIndex`, `projectileInPerfectZoneByHitIndex` 등 딕셔너리 추가

### 2. 플레이어 막기 애니메이션 지속성 개선 ✅
- **문제**: 막기 입력 유지 시 애니메이션이 일정 시간 후 해제됨
- **해결**: 
  - Animator의 `isGuarding` boolean 파라미터 활용
  - 입력 유지 시 애니메이션 지속, 입력 해제 시 즉시 종료
  - 턴 종료 시 자동 해제

### 3. AI 방어 입력 시스템 복구 ✅
- **문제**: 발사체 기반 전환으로 AI 방어 입력 기능 손실
- **해결**:
  - `IAIDefenseDecisionMaker` 인터페이스 설계
  - `DefaultAIDefenseDecisionMaker` 구현체 생성
  - GlobalConfig 연동으로 AI 행동 제어

### 4. AI 의사결정 시스템 모듈화 ✅
- **설계**:
  - `IAIDefenseDecisionMaker` 인터페이스
  - `AIDefenseDecision` 구조체 (willAttempt, willSucceed, reactionTime)
  - `AIContext` 구조체 (컨텍스트 정보)
- **구현**:
  - `DefaultAIDefenseDecisionMaker` 기본 구현
  - 확장 가능한 구조로 설계

### 5. AI 막기 기능 구현 ✅
- **기능**:
  - AI 막기 의사결정 (`NpcGuardAttemptRate` 기반)
  - 첫 번째 Hit 타이밍에 막기 시작
  - 막기 중 쳐내기 시도 제어 (`NpcParryWhileGuarding`)
- **구현**:
  - `StartAIGuardDecision()` 메서드
  - `WaitForFirstHitAndStartGuard()` 코루틴
  - `StartAIGuard()`, `StopAIGuard()` 메서드

### 6. AI 막기 애니메이션 제어 문제 해결 ✅
- **문제**: AI 막기 애니메이션이 턴 종료 후에도 지속됨
- **원인**: `DisableInput()` 호출 누락
- **해결**:
  - `PerformTurn`에서 명시적 `DisableInput()` 호출
  - `ResetTurnState()` 메서드로 상태 초기화 통합

### 7. AI 막기 판정 반영 문제 해결 ✅
- **문제**: AI 막기가 전투 판정에 반영되지 않음
- **원인**: `IsGuardActive`가 AI 막기 상태를 포함하지 않음
- **해결**:
  - `IsGuardActive` 프로퍼티 수정: `isGuardActive || aiIsGuarding`
  - AI 막기 상태가 전투 판정에 정상 반영

### 8. NpcInputDifficulty 관련 로직 제거 ✅
- **제거 항목**:
  - `GlobalConfig.cs`에서 `npcInputDifficulty` 필드 및 프로퍼티 제거
  - `CombatManager.cs`에서 AI 입력 지연 로직 제거
- **결과**: AI가 완벽 타이밍에 즉시 입력 시도

## 기술적 개선사항

### 1. 딕셔너리 기반 상태 관리
- Hit Index별 독립적인 상태 추적
- 멀티 히트 공격에 대한 정확한 방어 처리

### 2. 모듈화된 AI 시스템
- 인터페이스 기반 설계로 확장성 확보
- 의사결정 로직과 실행 로직 분리

### 3. Animator 소유권 검증
- `ValidateAnimatorOwnership()` 메서드로 올바른 캐릭터 애니메이션 제어
- 플레이어/AI 구분에 따른 안전한 애니메이션 처리

### 4. 통합된 턴 상태 관리
- `ResetTurnState()` 메서드로 턴 관련 상태 일괄 초기화
- 코드 중복 제거 및 유지보수성 향상

## 버그 수정 내역

1. **멀티 히트 방어 실패**: Hit Index 기반 딕셔너리 시스템으로 해결
2. **막기 애니메이션 조기 종료**: Animator boolean 파라미터 활용으로 해결
3. **AI 방어 입력 누락**: 모듈화된 AI 시스템으로 복구
4. **AI 막기 애니메이션 지속**: `DisableInput()` 호출 보장으로 해결
5. **AI 막기 판정 미반영**: `IsGuardActive` 프로퍼티 수정으로 해결

## 다음 작업 예정
- AI 행동 패턴 다양화
- 전투 시스템 성능 최적화
- 사용자 인터페이스 개선

## 참고사항
- 발사체 기반 전투 시스템이 안정적으로 동작
- AI 의사결정 시스템의 모듈화로 향후 확장 용이
- 멀티 히트 공격에 대한 방어 시스템 완성
