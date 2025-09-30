# 개발 일지 - 2025년 9월 30일

## 오늘의 목표
1. **HUD 중 HP 패널 컨트롤러 스크립트 작성**
   - Player와 Enemy의 HP Bar를 비율 기반으로 크기 조정하는 시스템 구현
   - HP 변경 시 실시간 패널 크기 업데이트 기능

2. **발사체 생성 위치 조절 시스템 구현**
   - ProjectileManager에서 발사체 생성 위치 공통 관리
   - Inspector에서 발사체 생성 오프셋 조절 가능

## 진행 상황

### ✅ 완료된 작업

#### 1. HPPanelController 스크립트 구현
- **파일**: `Assets/Script/UI/HPPanelController.cs`
- **기능**:
  - Player와 Enemy HP Bar의 RectTransform 참조 관리
  - 현재 HP 기준 비율 계산 (Player HP / (Player HP + Enemy HP))
  - HP 변경 시 부드러운 크기 변화 애니메이션
  - Combatant의 OnHPChanged 이벤트 구독으로 실시간 업데이트

- **주요 특징**:
  - `localScale`을 사용한 크기 조절
  - `SmoothStep`을 사용한 부드러운 애니메이션
  - 디버그 로그를 통한 상태 추적
  - Context Menu를 통한 테스트 기능

#### 2. HP 패널 비율 계산 로직 구현
- **계산 방식**: 현재 HP 기준 상대적 비율
  - Player HP 100, Enemy HP 100 → Player: 0.5, Enemy: 0.5
  - Player HP 100, Enemy HP 50 → Player: 0.67, Enemy: 0.33
  - Player HP 100, Enemy HP 0 → Player: 1.0, Enemy: 0.0 (완전히 사라짐)

- **최소 크기 제한 제거**: HP가 0이면 패널이 완전히 사라지도록 구현

#### 3. ProjectileManager 발사체 생성 위치 시스템 구현
- **파일**: `Assets/Script/Combat/ProjectileManager.cs`
- **추가된 설정**:
  - `spawnOffset` (Vector3): 기본 오프셋
  - `heightOffset` (float): 높이 오프셋 (Y축)
  - `forwardOffset` (float): 앞쪽 오프셋 (방어자 방향)

- **새로운 메서드**:
  - `CalculateSpawnPosition()`: 공격자-방어자 방향을 고려한 생성 위치 계산

#### 4. CombatManager 발사체 생성 로직 수정
- **파일**: `Assets/Script/Combat/CombatManager.cs`
- **변경사항**:
  - `ProjectileManager.CalculateSpawnPosition()` 사용으로 변경
  - 공통 규칙으로 발사체 생성 위치 관리
  - 디버그 로그에 생성 위치 정보 추가

### 🔧 기술적 개선사항

#### 1. Unity UI 시스템 이해 개선
- **Anchor vs Scale**: UI 크기 조절에 `localScale` 사용이 적합함을 확인
- **RectTransform**: `anchorMin/Max`는 위치 설정, `localScale`은 크기 조절

#### 2. 싱글톤 패턴 문제 진단
- **문제**: `CombatManager.Instance`가 null인 상황 발생
- **원인**: `OnDisable()` 시점에서 `CombatManager`가 파괴된 경우
- **해결 방향**: 초기화 순서 보장 및 null 체크 강화 필요

#### 3. 디버그 시스템 개선
- **로그 필터링**: `[HPPanelController]` 구분자로 로그 필터링 가능
- **상세 로그**: HP 비율 계산, 애니메이션 상태, anchor 값 변화 추적

## 발견된 문제점 및 해결 과정

### 1. HP 패널 크기 조절 방식 혼동
- **초기 시도**: `anchorMin/Max` 조절로 크기 변경 시도
- **실제 해결**: `localScale` 사용이 올바른 방법
- **학습**: Unity UI에서 크기 조절은 Scale, 위치 조절은 Anchor 사용

### 2. HP 비율 계산 로직 수정
- **초기 문제**: 최대 HP 기준으로 비율 계산 (HP 변경 시 변화 없음)
- **수정**: 현재 HP 기준으로 비율 계산으로 변경
- **결과**: HP 변경 시 실시간으로 패널 크기 변화

### 3. 최소 크기 제한 문제
- **문제**: `minPanelSize` 설정으로 HP가 0이어도 패널이 완전히 사라지지 않음
- **해결**: `minPanelSize` 제거, `Mathf.Clamp01()` 사용
- **결과**: HP가 0이면 패널이 완전히 사라짐

## 다음 작업 계획

### 1. HPPanelController 통합 테스트
- **Unity Scene에서 테스트**: 실제 HP Bar 오브젝트와 연결하여 동작 확인
- **애니메이션 튜닝**: `animationSpeed` 값 조절로 적절한 속도 설정

### 2. 발사체 생성 위치 튜닝
- **Inspector 설정**: `ProjectileManager`의 오프셋 값들을 적절히 조절
- **시각적 확인**: 발사체가 의도한 위치에서 생성되는지 확인

### 3. CombatManager 싱글톤 문제 해결
- **초기화 순서 보장**: `DontDestroyOnLoad` 설정 확인
- **null 체크 강화**: `OnDisable()` 시점에서 안전한 처리

## 오늘의 성과
- **HPPanelController 완전 구현**: HP 기반 실시간 패널 크기 조절 시스템
- **발사체 위치 시스템 구현**: 공통 규칙으로 발사체 생성 위치 관리
- **Unity UI 시스템 이해**: Anchor와 Scale의 올바른 사용법 학습
- **디버그 시스템 개선**: 체계적인 로그 관리로 개발 효율성 향상

## 총 작업 시간
- **약 3시간**: 스크립트 구현, 테스트, 디버깅, 문서화 포함

## 메모
- Unity UI에서 크기 조절은 `localScale`, 위치 조절은 `anchorMin/Max` 사용
- HP 기반 비율 계산은 현재 HP를 기준으로 해야 실시간 변화 가능
- 공통 규칙으로 관리하면 유지보수성과 일관성 확보 가능
- 디버그 로그에 구분자를 사용하면 문제 진단이 훨씬 쉬워짐
