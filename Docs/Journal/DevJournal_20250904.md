# 개발 일지 - 2025년 9월 4일

## 📋 오늘의 진행 사항

### ✅ 완료된 작업

#### 1. 스테이터스 UI 시스템 구현 및 테스트
- **목표**: 게임에서 실시간으로 스테이터스 정보를 확인할 수 있는 UI 구현
- **구현 내용**:
  - `CombatStatusDisplay` 클래스의 UI 업데이트 시스템 구축
  - `StatusUILayout` 클래스로 자동 UI 생성 및 연결
  - `UI_Setup_Guide` 클래스로 Unity 씬에서 UI 자동 설정
- **문제 해결 과정**:
  - 초기 UI 연결 문제 → 수동 UI 연결 기능 추가로 해결
  - 이벤트 구독 문제 → 안전장치 및 수동 구독 기능 추가
  - UI 필드 null 문제 → `ManualUIConnection` ContextMenu로 해결

#### 2. Poise 시스템 UI 연동
- **목표**: 전투 중 Poise 변화를 실시간으로 UI에 반영
- **구현 내용**:
  - `CharacterData`의 `OnPoiseChanged` 이벤트 구독
  - `CombatStatusDisplay`에서 실시간 UI 업데이트
  - 패리/하프패리 시 공격자 Poise 감소 로직 확인
- **테스트 결과**: 
  - ✅ 이벤트 발생 정상
  - ✅ UI 업데이트 정상
  - ✅ 실시간 Poise 변화 표시 확인

#### 3. 패리 Poise 감소 수치를 캐릭터 스탯으로 구현
- **목표**: 하드코딩된 패리 Poise 감소량(25)을 캐릭터별 스탯으로 변경
- **구현 내용**:
  - `CharacterData`에 `ParryPoiseDamage` 스탯 추가 (기본값: 25)
  - `CharacterStatsData`에 `parryPoiseDamage` 필드 추가
  - `CharacterManager`에서 스탯 로딩 시 ParryPoiseDamage 포함
  - `CombatManager`에서 방어자의 ParryPoiseDamage 스탯 사용
- **장점**:
  - 캐릭터별로 다른 패리 위력 설정 가능
  - Unity Inspector에서 쉽게 조정 가능
  - 확장성 있는 구조로 향후 유파별 특성 적용 가능

### 🔧 기술적 세부사항

#### UI 시스템 아키텍처
```
CombatStatusDisplay (Singleton)
├── UI 필드들 (playerHP, playerPoise, etc.)
├── 이벤트 구독 (OnStatsChanged, OnPoiseChanged)
└── 실시간 업데이트 (UpdatePlayerStatus, UpdateEnemyStatus)

StatusUILayout
├── 자동 UI 생성 (CreateStatusUI)
├── UI 연결 (ConnectToCombatStatusDisplay)
└── 수동 연결 (ManualUIConnection)
```

#### Poise 시스템 흐름
```
전투 중 패리 발생
→ CombatManager.EvaluateClashResult()
→ 방어자의 ParryPoiseDamage 스탯 확인
→ 공격자의 LosePoise() 호출
→ CharacterData.OnPoiseChanged 이벤트 발생
→ CombatStatusDisplay UI 업데이트
```

### 🐛 해결된 문제들

1. **UI 연결 문제**: StatusUILayout에서 생성한 UI 요소들이 CombatStatusDisplay에 연결되지 않음
   - **해결**: 수동 UI 연결 기능 추가로 해결

2. **이벤트 구독 문제**: CombatStatusDisplay의 초기화 순서로 인한 이벤트 구독 실패
   - **해결**: 안전장치 및 수동 구독 기능 추가

3. **하드코딩 문제**: 패리 Poise 감소량이 25로 고정되어 있음
   - **해결**: 캐릭터 스탯으로 변경하여 유연성 확보

## 📅 내일 계획 (2025년 9월 5일)

### 🎯 주요 목표: 전투 피해 시스템 구현

#### 1. 검술 ActionCommand에 공격력 계수 설정
- **목표**: `ActionCommandData`에 `DamageRatio` 필드 추가
- **세부사항**:
  - 타입: `float` (기본값: 1.0)
  - 의미: 1.5 = 150% 공격력
  - 용도: 검술별로 다른 공격력 배율 적용

#### 2. 막기 행동 중 방어자의 DR 일시적 증가
- **목표**: 막기 상태에서 방어자의 방어력(DR) 임시 상승
- **구현 방향**:
  - 막기 입력 시 DR 증가
  - 막기 해제 시 원래 DR로 복원
  - 막기 중 피해량 계산 시 증가된 DR 적용

#### 3. 피해량 계산 시스템 구현
- **목표**: ATK + DamageRatio → DR 감소 → 최종 HP 감소
- **계산 공식**:
  ```
  기본 피해량 = 공격자의 ATK × 검술의 DamageRatio
  최종 피해량 = 기본 피해량 - 방어자의 DR
  최종 피해량 = max(0, 최종 피해량)  // 음수 방지
  ```
- **구현 위치**: `CombatManager`의 피해 처리 로직

#### 4. 패리/하프패리 피해량 감소 시스템
- **목표**: 패리 성공 시 받는 피해량 감소
- **구현 방향**:
  - 패리: 피해량 0% (완전 무효화)
  - 하프패리: 피해량 50% 감소
  - 일반 명중: 피해량 100% (감소 없음)

#### 5. 전투 종료 조건 구현
- **목표**: HP 0 시 전투 종료 및 패배 선언
- **구현 내용**:
  - HP 0 체크 로직
  - 전투 종료 플래그 설정
  - 패배자 선언 (후속 처리용)

#### 6. 게임룰 기본 수치 정의 (GameRule 시스템)
- **목표**: 공용 게임룰 수치들을 별도 테이블로 관리
- **필요한 기본값들**:
  - 막기 시 피해량 감소량 (기본값: 50%)
  - 패리 시 피해량 감소량 (기본값: 100%)
  - 하프패리 시 피해량 감소량 (기본값: 50%)
- **구현 방식**: `GlobalConfig`와 유사한 `GameRule` ScriptableObject

### 🔮 향후 확장 계획 (참고용)

#### 유파별 특성 시스템
- **대검 유파**: 막기 효율 150%, 패리 효율 50%
- **쌍단검 유파**: 패리 효율 +10% 추가
- **확장성**: GameRule 기본값에 배율/합산 조정으로 구현

### 📝 개발 노트

#### 오늘의 교훈
1. **UI 연결 문제**: 자동화된 시스템도 수동 연결 기능을 백업으로 준비해야 함
2. **이벤트 시스템**: 초기화 순서에 주의하고 안전장치를 마련해야 함
3. **하드코딩 방지**: 확장성을 고려한 스탯 시스템 설계의 중요성

#### 내일 주의사항
1. **피해량 계산**: 음수 방지 및 최소 피해량 보장
2. **상태 관리**: 막기 상태의 시작/종료 타이밍 정확히 처리
3. **게임룰 설계**: 확장 가능한 구조로 기본값 정의

---

**총 작업 시간**: 약 4시간  
**완료된 기능**: 스테이터스 UI 시스템, Poise 실시간 업데이트, 패리 스탯 시스템  
**다음 우선순위**: 전투 피해 시스템 구현
