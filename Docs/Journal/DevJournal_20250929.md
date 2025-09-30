# 개발 일지 - 2025년 9월 29일

## 오늘의 목표
1. **마이너 이슈 체크 및 수정**
   - 최종 적중 판정 텍스트에 Hit Index가 3으로 표시되는 문제 수정
   - 방어자 완벽 입력 성공 시 공격자 발사체 제거 기능 구현

2. **디버그 패널 기능 구현**
   - 현재의 정보 UI 캔버스 중 검술 선택 UI를 제외한 나머지 요소를 디버그 패널로 명명
   - 캔버스를 분리하여 해당 캔버스는 디버그 모드가 true일 때에만 표시되도록 함
   - 검술 선택 UI를 포함한 정식 HUD 구현 (정식 HUD 스펙은 별도로 문서화 예정)

3. **발사체 프리팹 참조 구조 점검**
   - Perfect 입력 시 Perfect 발사체가 아닌 일반 발사체가 발사되는 문제 해결
   - ProjectileManager 프리팹별 풀링 시스템 구현

4. **FloatingText 크기 및 위치 문제 해결**
   - FloatingText Scale 문제 해결
   - 캐릭터 머리 위에 정확히 표시되도록 위치 수정

## 진행 상황

### ✅ 완료된 작업

#### 1. Hit Index 3 표시 문제 수정
- **문제**: `EvaluateClashResult()` 메서드에서 `CurrentHit` 대신 발사체의 `hitIdx` 사용 필요
- **해결**: `EvaluateClashResult(int hitIndex)` 오버로드 메서드 추가
- **수정 파일**: `Assets/Script/Combat/CombatManager.cs`
- **결과**: 최종 적중 판정 텍스트에 올바른 Hit Index 표시

#### 2. 방어자 완벽 입력 성공 시 발사체 제거 기능
- **문제**: 방어자가 완벽 입력 성공 시 공격자 발사체가 남아있어 연출상 문제
- **해결**: `TriggerProjectileBasedFinalJudgment()` 메서드에 발사체 제거 로직 추가
- **수정 파일**: `Assets/Script/Combat/CombatManager.cs`
- **결과**: 완벽 방어 시 발사체가 즉시 제거되어 연출 개선

#### 3. 디버그 패널 기능 구현
- **구현**: `DebugPanelController.cs` 스크립트 생성
- **기능**: F3 키로 디버그 패널 토글 기능
- **Input System**: UI 액션 맵에 F3 키 바인딩 추가
- **수정 파일**: `Assets/Script/UI/DebugPanelController.cs`
- **결과**: F3 키로 디버그 정보 표시/숨김 제어 가능

#### 4. ProjectileManager 프리팹별 풀링 시스템
- **문제**: 기존 풀링 시스템에서 Perfect/Basic 발사체가 섞여서 사용됨
- **해결**: `Dictionary<GameObject, Queue<Projectile>>` 기반 프리팹별 풀링 구현
- **수정 파일**: `Assets/Script/Combat/ProjectileManager.cs`
- **결과**: Perfect 입력 시 Perfect 발사체, 일반 입력 시 Basic 발사체 정확히 생성

#### 5. FloatingText 위치 및 크기 문제 해결
- **문제 1**: Canvas 좌표 변환으로 인한 화면 중앙 표시 문제
- **해결 1**: Canvas 좌표 변환 과정 주석처리, 월드 좌표 직접 사용
- **문제 2**: Canvas Scaler에 의한 자동 Scale 조정 (1158배 등)
- **해결 2**: `rectTransform.localScale = Vector3.one` 강제 설정
- **문제 3**: riseSpeed가 100으로 설정되어 y값 변화가 과도함
- **해결 3**: riseSpeed를 50으로 조정
- **수정 파일**: `Assets/Script/UI/FloatingTextManager.cs`, `Assets/Script/UI/FloatingText.cs`
- **결과**: FloatingText가 캐릭터 머리 위에 적절한 크기로 정확히 표시

### 🔧 기술적 개선사항

#### 1. 디버그 로그 시스템 개선
- **구분자 기반 필터링**: `[PROJECTILE]`, `[FLOATING_TEXT]` 등으로 로그 필터링 가능
- **간소화된 로그**: 불필요한 상세 로그 제거, 핵심 정보만 출력
- **효과**: 디버깅 효율성 대폭 향상

#### 2. 2D 게임 최적화
- **Canvas 좌표 변환 제거**: 2D 게임에서는 월드 좌표 직접 사용이 더 효율적
- **프리팹별 풀링**: 메모리 효율성과 정확성 동시 확보
- **효과**: 성능 향상 및 코드 단순화

## 발견된 문제점 및 해결 과정

### 1. Perfect 발사체 문제 진단 과정
- **초기 추정**: ActionCommandData의 perfectProjectilePrefab 설정 문제
- **실제 원인**: ProjectileManager의 풀링 시스템에서 기존 발사체 재사용
- **해결 방법**: 프리팹별 풀링 시스템으로 변경

### 2. FloatingText 위치 문제 진단 과정
- **초기 추정**: Canvas 설정 문제
- **실제 원인**: 2D 게임에서 불필요한 Canvas 좌표 변환
- **해결 방법**: 월드 좌표 직접 사용

## 다음 작업 계획

### 1. Unity에서 Canvas 구조 변경
- **DebugCanvas**: 기존 Canvas를 DebugCanvas로 이름 변경
- **HUDCanvas**: 검술 선택 UI를 위한 새로운 Canvas 생성
- **Panel 구조**: 디버그 정보들을 상위 Panel로 그룹화

### 2. 정식 HUD 구현
- **HUD 스펙 문서화**: 검술 선택 UI를 포함한 정식 HUD 명세 작성
- **UI/UX 개선**: 사용자 경험 향상을 위한 HUD 디자인

### 3. 디버그 로그 정리
- **임시 디버그 로그 제거**: 개발 완료 후 불필요한 디버그 로그 정리
- **최적화**: 성능에 영향을 주는 로그 제거

## 오늘의 성과
- **6개 주요 이슈 해결**: Hit Index, 발사체 제거, 디버그 패널, Perfect 발사체, FloatingText 위치/크기
- **시스템 개선**: 풀링 시스템, 디버그 시스템, 좌표 변환 시스템
- **코드 품질 향상**: 디버그 로그 개선, 2D 게임 최적화
- **사용자 경험 개선**: 정확한 발사체 표시, 적절한 FloatingText 위치

## 총 작업 시간
- **약 4시간**: 문제 진단, 해결, 테스트, 문서화 포함

## 메모
- 2D 게임에서는 복잡한 좌표 변환보다 단순한 월드 좌표 사용이 더 효율적
- 프리팹별 풀링은 메모리 효율성과 정확성을 동시에 확보하는 좋은 방법
- 디버그 로그에 구분자를 사용하면 개발 효율성이 크게 향상됨
