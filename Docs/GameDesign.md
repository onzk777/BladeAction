## 게임 기획 개요 (GDD)

### 콘셉트
- **장르**: 턴제 타이밍 기반 검술 대전
- **핵심 재미**: 제한된 윈도우 내 정확한 입력(Perfect)으로 유리한 전투 전개

### 핵심 루프
1) 플레이어/적이 **커맨드 선택**
2) **타이밍 윈도우**가 열리고 입력 수행
3) 입력 결과(Perfect/Miss 등)로 **대결 판정**
4) **피드백(UI/로그)** 제공 → 다음 히트/턴 진행

### 전투 시스템 개요
- **턴 구조**: 공격자/방어자 교대. `CombatManager`가 흐름 관리
- **타이밍 윈도우**: `perfectTimings` 기반. `Attacker/DefenderInputHandler`가 입력 기록/판정
- **판정 로직**: `InputVersusResult`가 공격/방어 입력과 타이밍 차이를 비교해 결과 산출
- **UI 피드백**: `CombatStatusDisplay`가 프롬프트/결과/쿨다운/턴 정보 표시

### 액션 커맨드 데이터
- **정의 위치**: `ActionCommandData`, `SwordArtStyleData` (ScriptableObject)
- **주요 필드(예시)**
  - `commandName`, `hitCount`, `perfectTimings[]`
  - 전역 설정: `GlobalConfig`의 `TurnDurationSeconds`, `NpcDefensePerfectRate` 등

### 밸런싱 파라미터
- 타이밍 허용폭, 난이도(NPC 입력 지점/성공률), 쿨다운, 히트 수 등은 **Config/ScriptableObject**로 제어

### UI 요구사항(요약)
- 현재 턴/남은 시간/입력 가능 상태 표시
- 히트별 판정 결과, 대결 결과 라인 로그

### 오디오/아트(프로토타입 기준)
- 본 저장소는 **로직 중심**. 아트/사운드는 생략 또는 대체 리소스 사용


