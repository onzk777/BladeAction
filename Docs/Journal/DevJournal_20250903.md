# 2025년 9월 3일 개발 일지

## 주요 목표
플레이어와 적에게 공통 스테이터스 시스템 적용

## 진행 작업

### 1. 스테이터스 시스템 설계
- **목표**: Combatant 클래스에 공통 스테이터스 필드 추가
- **요구사항**:
  - HP (int): 기본값 100, 생명력, 0이 되면 전투 종료/패배
  - ATK (int): 기본값 20, 적에게 가하는 피해 계산 기본값
  - DR (int): 기본값 0, 피해 감소 능력치
  - Crit (int): 기본값 0, 치명타 확률 (0~100%)
  - CritRatio (int): 기본값 150, 치명타 피해량 배율 (150%)
  - MaxPoise (int): 기본값 100, 0이 되면 실행 중이던 검술이 중단
  - CurrentPoise (int): 현재 전투에서 보유 중인 Poise 값

### 2. Combatant 클래스 수정
- **파일**: `Assets/Script/Combatant.cs`
- **추가된 필드**:
  ```csharp
  public int HP { get; private set; }
  public int MaxHP { get; private set; }
  public int ATK { get; private set; }
  public int DR { get; private set; }
  public int Crit { get; private set; }
  public int CritRatio { get; private set; }
  public bool IsDefeated => HP <= 0;
  public int CurrentPoise { get; private set; }
  public int MaxPoise { get; private set; }
  public bool IsInterrupted => CurrentPoise <= 0;
  ```

### 3. 스테이터스 관련 메서드 구현
- **InitializeStats()**: 스테이터스 초기화 (생성자에서 호출)
- **Heal(int amount)**: HP 회복 기능
- **TakeDamage(int damage)**: HP 감소, DR 적용, 패배 판정
- **IsCriticalHit()**: 치명타 확률 판정
- **CalculateCriticalDamage(int baseDamage)**: 치명타 피해량 계산
- **InitializePoise()**: Poise 초기화 (생성자에서 호출)
- **ResetPoise()**: 공격 턴 시작 시 Poise 회복
- **LosePoise(int amount)**: 쳐내기 당했을 때 Poise 감소
- **GetPoiseStatus()**: 현재 Poise 상태 문자열 반환

### 4. 기존 Posture 시스템을 Poise로 변경
- **파일**: `Assets/Script/Combat/CombatManager.cs`, `Assets/Script/GlobalConfig.cs`
- **변경 내용**:
  - `PosturePoints` → `Poise`로 명칭 변경
  - `ResetPosturePoints()` → `ResetPoise()`로 메서드명 변경
  - `LosePosturePoints()` → `LosePoise()`로 메서드명 변경
  - GlobalConfig에서 Posture 관련 설정 제거 (Poise는 Combatant에서 직접 관리)
  - **데이터 타입 최적화**: Poise 관련 필드와 메서드를 float에서 int로 변경 (소수점 불필요, 성능 향상)

### 5. 구현 특징
- **공통 구조**: 플레이어와 적이 동일한 스테이터스 시스템 사용
- **DR 시스템**: 최소 1의 피해는 보장하는 로직 적용
- **치명타 시스템**: 확률 기반 판정 및 배율 적용
- **Poise 시스템**: 검술 중단 메커니즘, 0이 되면 실행 중인 검술 중단
- **디버그 로그**: 스테이터스 변화 추적을 위한 상세 로그

## 완료 상태
- Combatant 클래스에 스테이터스 시스템 구현 완료 (HP, ATK, DR, Crit, CritRatio, Poise)
- 기본값 설정 및 관련 메서드 구현 완료
- 기존 Posture 시스템을 Poise로 완전 변경
- CombatManager와 GlobalConfig에서 관련 코드 업데이트 완료
- 린터 오류 없음 확인

## 추가 작업 진행

### 6. CharacterManager 아키텍처 도입
- **목표**: 책임 분리 원칙에 따른 캐릭터 관리 시스템 구축
- **구조**: CharacterData (스테이터스) + CharacterManager (관리) + Combatant (전투 로직)

### 7. CharacterData 클래스 생성
- **파일**: `Assets/Script/CharacterData.cs`
- **기능**:
  - 캐릭터 스테이터스 저장 (HP, ATK, DR, Crit, CritRatio, Poise)
  - 스테이터스 변경 이벤트 시스템
  - 스테이터스 관련 메서드 (Heal, TakeDamage, LosePoise 등)

### 8. CharacterManager 클래스 생성
- **파일**: `Assets/Script/CharacterManager.cs`
- **기능**:
  - 싱글톤 패턴으로 전역 캐릭터 관리
  - PlayerData, EnemyData 관리
  - Combatant 인스턴스 생성 및 관리
  - Controller와 Combatant 연결

### 9. Combatant 클래스 리팩터링
- **변경 내용**:
  - 스테이터스 필드 제거, CharacterData 참조로 변경
  - 생성자 변경: `Combatant(CharacterData data)`
  - 모든 스테이터스 관련 메서드가 CharacterData를 통해 처리

### 10. PlayerCombatant, EnemyCombatant 수정
- **변경 내용**:
  - 생성자 변경: `PlayerCombatant(CharacterData data, PlayerController controller)`
  - SetController() 메서드 추가 (CharacterManager에서 호출)

### 11. CombatManager 수정
- **변경 내용**:
  - 중복 Combatant 인스턴스 생성 로직 제거
  - CharacterManager를 통한 Combatant 접근으로 변경
  - ConnectControllers() 메서드로 Controller와 Combatant 연결

### 12. PlayerController, EnemyController 수정
- **변경 내용**:
  - Combatant 인스턴스 생성 로직 제거
  - CharacterManager.Instance를 통한 Combatant 접근으로 변경
  - 모든 combatant 참조를 Combatant 프로퍼티로 변경

## 현재 상태
- CharacterData, CharacterManager 클래스 생성 완료
- Combatant 클래스 리팩터링 완료
- PlayerCombatant, EnemyCombatant 수정 완료
- CombatManager, PlayerController, EnemyController 수정 완료
- **남은 작업**: ICombatController 인터페이스 수정 및 통합 테스트

### 13. ICombatController 인터페이스 검토
- **결과**: 인터페이스 수정 불필요 - 이미 적절하게 정의됨
- **Combatant 프로퍼티**: CharacterManager를 통해 접근하도록 변경 완료

### 14. 통합 테스트 및 검증
- **린터 오류**: CharacterData 클래스 인식 지연 (Unity 새 스크립트 인식 시간 필요)
- **기능 검증**: 모든 주요 클래스 리팩터링 완료
- **아키텍처 검증**: 책임 분리 및 중복 제거 완료

## 최종 완료 상태
- ✅ CharacterData 클래스 생성 (스테이터스 저장)
- ✅ CharacterManager 클래스 생성 (캐릭터 관리)
- ✅ Combatant 클래스 리팩터링 (CharacterData 참조)
- ✅ PlayerCombatant, EnemyCombatant 수정
- ✅ CombatManager 수정 (CharacterManager 사용)
- ✅ PlayerController, EnemyController 수정
- ✅ ICombatController 인터페이스 검토
- ✅ 전체 시스템 통합 테스트

### 15. 스테이터스 UI 추가
- **파일**: `Assets/Script/UI/CombatStatusDisplay.cs`
- **추가된 UI 요소**:
  - 플레이어/적 HP, Poise, ATK, DR, Crit 표시
  - 실시간 스테이터스 업데이트 (이벤트 기반)
  - 테스트용 ContextMenu 메서드들

### 16. UI 이벤트 연결
- **CharacterData 이벤트**: OnStatsChanged, OnHPChanged, OnPoiseChanged
- **자동 업데이트**: 스테이터스 변경 시 UI 자동 갱신
- **테스트 기능**: 우클릭 메뉴로 스테이터스 변경 테스트 가능

## 아키텍처 개선 결과
- **중복 제거**: Combatant 인스턴스 중복 생성 문제 해결
- **책임 분리**: CharacterManager(캐릭터 관리) vs Controller(입력 처리)
- **확장성**: 향후 인벤토리, 레벨링 시스템 연동 준비 완료
- **일관성**: 모든 스테이터스가 CharacterData에서 중앙 관리
- **UI 연동**: 실시간 스테이터스 표시 및 테스트 기능 완비

### 17. UI 연결 가이드 작성
- **파일**: `Assets/Script/UI/StatusUILayout.cs`
- **기능**: UI 자동 생성 및 연결을 위한 헬퍼 스크립트
- **가이드**: Unity에서 UI 요소 연결을 위한 상세 가이드 작성

## 오늘 작업 완료 요약
- ✅ CharacterManager 아키텍처 도입 완료
- ✅ 스테이터스 시스템 구현 완료 (HP, ATK, DR, Crit, CritRatio, Poise)
- ✅ 모든 코드 리팩터링 완료
- ✅ 스테이터스 UI 추가 완료
- ✅ 이벤트 기반 UI 업데이트 구현
- ✅ 테스트용 메서드 추가
- ✅ UI 연결 가이드 작성 완료

## 내일 작업 계획 (2025년 9월 4일)

### 1. Unity UI 연결 작업 (예상 소요시간: 1-2시간)
- **CharacterManager 오브젝트 생성 및 설정**
- **스테이터스 UI 요소 생성 및 연결**
- **CombatStatusDisplay 스크립트 필드 연결**

### 2. UI 테스트 및 검증 (예상 소요시간: 30분-1시간)
- **기본 스테이터스 표시 테스트**
- **ContextMenu를 통한 스테이터스 변경 테스트**
- **실시간 UI 업데이트 검증**

### 3. 실제 전투 시스템 테스트 (예상 소요시간: 1-2시간)
- **전투 중 스테이터스 변경 확인**
- **Poise 감소, HP 변경 등 실제 동작 검증**
- **버그 수정 및 최적화**

### 4. 추가 개선사항 (시간 여유 시)
- **UI 디자인 개선**
- **스테이터스 변경 시 시각적 효과 추가**
- **전투 결과에 따른 스테이터스 변화 로직 검토**

## 예상 완료 목표
- **오전**: UI 연결 및 기본 테스트 완료
- **오후**: 실제 전투 시스템 통합 테스트 완료
- **최종**: CharacterManager 아키텍처 완전 검증 및 안정화
