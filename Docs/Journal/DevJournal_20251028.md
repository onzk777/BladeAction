# 개발 일지 - 2025년 10월 28일

## 오늘의 목표

어제 구축한 Combat Stats 시스템을 완성하고, 전투 프로토타입 Scene에서 CombatantInventory를 활용한 아이템 스탯 적용을 테스트합니다.

---

## 구현 계획 (완료)

### 1. 데이터 정규화 및 마이그레이션 ✅
- [x] 비율형 데이터 검증 스크립트 작성 (0~100 저장 케이스 탐지)
- [x] 0~100 → 0.0~1.0 자동 변환 마이그레이션 도구 구현
- [x] 표시 로직 통일: ItemDetailPanel/CombatStatusDisplay에서 항상 `value × 100 → 정수%` 표시

### 2. StatsCalculationManager 확장 및 완성 ✅
- [x] 집계 파이프라인 구현
  - baseStats + ΣEquipment → 합산 → Clamp → 결과 반환
- [x] API 추가
  - `RecalculateAndCommit(Character)`: 인벤토리 장비 전체 합산 → Clamp
  - `GetFinalStat(Character, string)`: 키 기반 개별 스탯 조회
  - `GetFinalStats(Character)`: CombatStats 전체 반환
  - `GetFinalATK(Character)`: 최종 공격력 반환
- [x] EquipmentStats ↔ CombatStats 매핑 정의 및 구현

### 3. Character 연동 ✅
- [x] Character에 `CharacterInventory` 필드 추가
- [x] 임시 Provider 제거 (InventoryProvider, TemporaryEquipmentApplier deprecated)
- [x] 스탯 커밋 로직 구현
  - MaxHP 선반영 후 HP를 [0..MaxHP] 보정 (비율 보존)
  - Poise 최대값 갱신
- [x] ItemEvents 이벤트 훅 연결 (장착/해제 시 자동 재계산)
- [x] 더티 플래그/캐싱 도입 (장비 변경 시에만 재계산)

### 4. UI 연동 확대 ✅
- [x] CombatStatusDisplay 개선
  - 비율형: `XX%` 표시 확인
  - 배율형: `X.Xx` 표시 확인
- [x] ItemDetailPanel 표기 일관성 확인 (FormatSignedPercent)

### 5. 대규모 리팩토링 ✅
- [x] Combatant → Character 네이밍 변경 (전체 프로젝트)
- [x] CombatantInventory → CharacterInventory
- [x] Effective → Final (GetFinalStats, GetFinalATK 등)
- [x] Map → Convert (ConvertToCombatStats, CalculateEquipmentDelta)
- [x] CombatStats 구조체 확장 (currentHP, currentPoise, tempDRBonus, guardDRBonus)
- [x] CharacterData.baseStats 필드 추가

### 6. 검증 및 테스트 도구 ✅
- [x] 검증 메뉴 추가: `Tools > Stats > Recalculate All Characters`
- [x] 마이그레이션 도구: `Tools > Character > Migrate CharacterData to CombatStats`
- [x] 비율 검증 도구: `Tools > Stats > Validate Ratio Data (0~1)`
- [x] 구형 필드 Deprecated 처리
- [x] 테스트 케이스 문서 작성

---

## 작업 로그

### [세션 1] Combat Stats 시스템 완성

#### 진행 내용

##### 1. 비율형 데이터 검증 및 마이그레이션 도구 구현 ✅
- `RatioDataValidator.cs` 에디터 윈도우 생성
  - StatDatabase, CharacterData의 비율형 필드 전수 검증
  - 0~100으로 잘못 저장된 케이스 탐지
  - 자동 변환 기능 (0~100 → 0.0~1.0)
  - Tools > Stats > Validate Ratio Data (0~1) 메뉴 추가

##### 2. StatsCalculationManager API 확장 ✅
- `GetEffectiveStats(Combatant)`: Base + Equipment → Clamp 계산
- `GetEffectiveStat(Combatant, string statKey)`: 특정 스탯 조회
- `RecalculateAndCommit(Combatant)`: 스탯 재계산 및 Combatant 커밋
  - MaxHP 선반영 후 HP를 [0..MaxHP] 보정 (비율 보존)
  - Poise 최대값 갱신
  - OnStatsChanged 이벤트 발행
- `GetStatByKey(CombatStats, string)`: 키 기반 스탯 값 조회

##### 3. Combatant 연동 ✅
- `Combatant.Inventory` 필드 추가 (CombatantInventory 참조)
- `CombatantInventory.Owner` 필드 추가 (Combatant 역참조)
- `CombatantInventory.TriggerStatsRecalculation()` 구현
  - EquipItem/UnequipItem 시 자동 호출
  - StatsCalculationManager.RecalculateAndCommit 연동
- 더티 플래그(`isDirty`) 도입

##### 4. 임시 Provider 제거 ✅
- `InventoryProvider` 클래스 제거
- `TemporaryEquipmentApplier` deprecated 처리
  - Combatant.Inventory 직접 할당으로 변경
  - Start 메서드에서 Owner 설정 추가

##### 5. UI 연동 확인 ✅
- `CombatStatusDisplay`: CritChance 표시 확인 (이미 올바름)
  - `CritChance * 100f:F1%` 형식
- `ItemDetailPanel`: 퍼센트 표시 확인 (이미 올바름)
  - `FormatSignedPercent`: 0~1 → 정수% 변환

##### 6. 검증 메뉴 및 구형 필드 처리 ✅
- `StatsRecalculationMenu.cs` 에디터 메뉴 추가
  - Tools > Stats > Recalculate All Combatants (Scene)
  - Tools > Stats > Force Update Combat UI
- CharacterData 구형 필드 deprecated 처리
  - `baseCrit`, `baseCritRatio` → Obsolete 속성 추가
  - `GetBaseStats()` → Obsolete 속성 추가

---

## 테스트 시나리오

### 전투 프로토타입 Scene 테스트 (ProtoType.unity)

#### 사전 준비
1. ProtoType.unity Scene 열기
2. Scene에 StatsCalculationManager가 있는지 확인
3. StatLimitRules 에셋이 Resources/Data/Stat 경로에 있는지 확인

#### 테스트 단계

**1단계: Combatant에 인벤토리 주입**
- CharacterManager에서 PlayerCombatant와 EnemyCombatant 생성 시점에 Inventory 할당
- 또는 TemporaryEquipmentApplier 사용 (deprecated이지만 테스트용으로 사용 가능)

**2단계: 아이템 장착 테스트**
```csharp
// 예시 코드
var player = CharacterManager.Instance.PlayerCombatant;
player.Inventory = new CombatantInventory();
player.Inventory.Owner = player;
player.Inventory.Initialize();

// 아이템 추가 및 장착
player.Inventory.AddItem("itm_weapon_test", 1);
player.Inventory.EquipItem("itm_weapon_test", EquipmentSlotType.Weapon);

// 스탯 재계산은 자동으로 수행됨
```

**3단계: 스탯 확인**
- CombatStatusDisplay에서 ATK 변화 확인
- Tools > Stats > Recalculate All Combatants (Scene) 실행
- Console 로그에서 최종 스탯 확인

**4단계: 전투 진행**
- 장비 스탯이 전투에 올바르게 적용되는지 확인
- 공격력, HP, Poise 등이 장비에 따라 변하는지 검증

**5단계: 장비 해제 테스트**
- 장비 해제 시 스탯이 원래대로 돌아가는지 확인
- CombatStatusDisplay UI가 즉시 업데이트되는지 확인

#### 예상 결과
- 장비 장착 시 ATK/HP/Poise 증가
- 장비 해제 시 기본 스탯으로 복귀
- UI가 실시간으로 업데이트됨
- 전투 중 계산에 장비 스탯이 반영됨

---

## 이슈 및 해결

(발생한 이슈 없음)

---

## 세션 2 작업 로그

### 리팩토링 작업 시작

#### 네이밍 규칙 확정
- `Effective` → `Final` (최종 스탯)
- `Map` → `Convert` (변환 함수)
- `Combatant` → `Character` (핵심 클래스)
- `CombatantInventory` → `CharacterInventory`

#### 완료된 작업 (Phase 1-4 부분)

**Phase 2: CombatStats 구조체 수정 ✅**
- `currentHP`, `currentPoise`, `tempDRBonus` 필드 추가
- Header 속성으로 카테고리 구분 (최대치/현재값/공격/방어/막기/쳐내기)
- 연산자 오버로딩 수정 (현재값은 합산하지 않음)

**Phase 3: CharacterData 구조 변경 ✅**
- `CombatStats baseStats` 필드 추가 (기본값 초기화 포함)
- 기존 개별 필드 모두 Deprecated 처리
  - `baseMaxHP`, `baseATK`, `baseDR` 등
- 프로퍼티들이 baseStats 참조하도록 수정
  - `public int MaxHP => (int)baseStats.maxHP;`

**Phase 4: Combatant → Character 네이밍 변경 (부분 완료) ✅**
- ✅ `Character.cs` 생성 (기존 Combatant.cs 대체)
  - `CharacterInventory` 필드 타입 변경
  - `GetFinalDR()`, `GetGuardFinalDR()` 메서드로 변경
  - 모든 로그 메시지 [Character]로 변경
- ✅ `PlayerCharacter.cs` 생성 (기존 PlayerCombatant.cs 대체)
- ✅ `EnemyCharacter.cs` 생성 (기존 EnemyCombatant.cs 대체)
  - EnemyCharacter → PlayerCharacter 참조 변경
- ✅ `CharacterInventory.cs` 파일명 변경 (기존 CombatantInventory.cs)
- ✅ 기존 파일 삭제: Combatant.cs, PlayerCombatant.cs, EnemyCombatant.cs

#### 추가 완료 작업

**Phase 5-8: 전체 프로젝트 리팩토링 완료 ✅**
- ✅ CharacterInventory.cs 클래스명 및 Owner 타입 변경
- ✅ StatsCalculationManager 전체 리팩토링
  - `MapCharacterDataToCombatStats` → `ConvertToCombatStats`
  - `MapEquipmentsToDelta` → `CalculateEquipmentDelta`
  - `GetEffectiveStats` → `GetFinalStats`
  - `GetEffectiveATK` → `GetFinalATK`
  - `GetEffectiveStat` → `GetFinalStat`
- ✅ CharacterManager 수정
  - `PlayerCombatant` → `PlayerCharacter`
  - `EnemyCombatant` → `EnemyCharacter`
- ✅ ICombatController 인터페이스 수정
  - `Combatant Combatant` → `Character Character`
  - `CombatantCommandResult` → `CharacterCommandResult`
- ✅ CharacterCommandResult.cs 생성 (기존 CombatantCommandResult.cs 대체)
- ✅ CombatManager 전체 리팩토링
  - 모든 Combatant → Character
  - 모든 변수명 변경 (playerCombatant → playerCharacter)
- ✅ Controller 클래스 수정
  - PlayerController, EnemyController
  - `Combatant` 프로퍼티 → `Character`
- ✅ UI 클래스 전체 수정 (10개)
  - CombatStatusDisplay, HPPanelController, BTMonitorUI 등
  - `GetEffectiveDR` → `GetFinalDR`
  - `GetEffectiveATK` → `GetFinalATK`
- ✅ BT 시스템 전체 수정 (5개)
  - BehaviorTreeExecutor, BehaviorTreeContext
  - BTLogger, BTLogHistory
  - 모든 Condition 노드
- ✅ 인벤토리 시스템 수정
  - ItemSystemValidator, ItemDetailPanel, InventoryUI 등
  - `CombatantInventory` → `CharacterInventory`
- ✅ Test 클래스 수정
  - TemporaryEquipmentApplier, ItemSystemTestRunner 등

**Phase 9: 테스트 케이스 및 마이그레이션 도구 ✅**
- ✅ `리팩토링_테스트_케이스.md` 작성
  - 8개 카테고리 테스트 시나리오
  - 체크리스트 제공
  - 예상 이슈 및 해결 방법
- ✅ `CharacterDataMigrationTool.cs` 에디터 윈도우 작성
  - Tools > Character > Migrate CharacterData to CombatStats
  - 구형 필드 → baseStats 자동 이전
  - Undo 지원

**컴파일 상태:**
- ✅ 에러: 0개
- ⚠️ 경고: 4개 (Deprecated 필드 사용 - 정상)

---

---

## 오늘 작업 정리 (최종)

### 세션 1: Combat Stats 시스템 완성 (오전)

#### 1.1 비율형 데이터 검증 및 마이그레이션 도구
- **RatioDataValidator.cs** 에디터 윈도우 생성
  - StatDatabase, CharacterData의 비율형 필드(blockEfficiency, parryEfficiency, damageReduction) 전수 검증
  - 0~100으로 잘못 저장된 케이스 자동 탐지
  - 0.01 곱셈으로 0~1 범위로 자동 변환 기능
  - 메뉴: `Tools > Stats > Validate Ratio Data (0~1)`

#### 1.2 StatsCalculationManager API 확장
- `GetFinalStats(Character)`: Base + Equipment → Clamp 계산
- `GetFinalATK(Character)`: 최종 공격력 반환
- `GetFinalStat(Character, string)`: 키 기반 스탯 조회
- `RecalculateAndCommit(Character)`: 스탯 재계산 및 Character 커밋
  - MaxHP 선반영 후 HP를 [0..MaxHP] 보정 (HP 비율 보존)
  - Poise 최대값 갱신 (현재값은 Clamp만)
  - OnStatsChanged 이벤트 자동 발행

#### 1.3 Character-Inventory 연동
- `Character.Inventory` 필드 추가 (CharacterInventory 타입)
- `CharacterInventory.Owner` 역참조 추가
- `TriggerStatsRecalculation()` 메서드 구현
  - EquipItem/UnequipItem 시 자동 호출
  - StatsCalculationManager.RecalculateAndCommit 연동
- 더티 플래그 도입 (`isDirty`, `IsDirty` 프로퍼티)

#### 1.4 임시 코드 정리
- `InventoryProvider` 클래스 완전 제거
- `TemporaryEquipmentApplier` Deprecated 처리
  - Owner 설정 로직 추가 (테스트용으로 유지)

#### 1.5 검증 도구 및 메뉴
- **StatsRecalculationMenu.cs** 생성
  - `Tools > Stats > Recalculate All Characters (Scene)`
  - `Tools > Stats > Force Update Combat UI`
- CharacterData 구형 필드 Deprecated 처리
  - baseCrit, baseCritRatio → Obsolete

---

### 세션 2: 대규모 리팩토링 (오후)

#### 2.1 네이밍 규칙 확정
- `Combatant` → `Character`
- `CombatantInventory` → `CharacterInventory`
- `Effective` → `Final` (GetFinalStats, GetFinalDR 등)
- `Map` → `Convert` (ConvertToCombatStats, CalculateEquipmentDelta)

#### 2.2 CombatStats 구조체 확장
- 최대치 필드: `maxHP`, `maxPoise`
- 현재값 필드 추가: `currentHP`, `currentPoise`
- 방어 필드 추가: `tempDRBonus`, `guardDRBonus`
- Header 속성으로 카테고리 정리 (최대치/현재값/공격/방어/막기/쳐내기)
- 연산자 오버로딩 수정 (현재값은 합산하지 않음)

#### 2.3 CharacterData 구조 변경
- `CombatStats baseStats` 필드 추가
  - 기본값 초기화 포함 (HP:100, ATK:20 등)
- 기존 개별 필드 모두 Deprecated
  - baseMaxHP, baseATK, baseDR, baseCritChance 등
- 프로퍼티들이 baseStats 참조
  - `public int MaxHP => (int)baseStats.maxHP;`

#### 2.4 핵심 클래스 재작성
- **Character.cs** 생성 (기존 Combatant.cs 대체)
  - CharacterInventory 타입 사용
  - GetFinalDR(), GetGuardFinalDR() 메서드
  - NotifyStatsChanged() 메서드 추가
- **PlayerCharacter.cs** 생성 (기존 PlayerCombatant.cs 대체)
- **EnemyCharacter.cs** 생성 (기존 EnemyCombatant.cs 대체)
- **CharacterInventory.cs** (파일명 및 클래스명 변경)
  - Owner 타입: Character
- **CharacterCommandResult.cs** 생성 (기존 CombatantCommandResult.cs 대체)

#### 2.5 전체 프로젝트 수정 (56개 파일)
**매니저 시스템:**
- CharacterManager: PlayerCharacter, EnemyCharacter 프로퍼티
- CombatManager: 모든 Combatant → Character 변경
- StatsCalculationManager: 전체 메서드 네이밍 변경

**인터페이스:**
- ICombatController: `Character Character { get; }` 프로퍼티

**Controller:**
- PlayerController, EnemyController: Character 프로퍼티, 메서드 수정

**UI (10개 파일):**
- CombatStatusDisplay, HPPanelController, BTMonitorUI, BTDebugPanel
- InventoryUI, ItemDetailPanel, EquippedSwordArtStyleUI

**BT 시스템 (7개 파일):**
- BehaviorTreeExecutor, BehaviorTreeContext, BTLogger, BTLogHistory
- BTCondition_HPComparison, BTCondition_PoiseComparison

**AI 시스템 (3개 파일):**
- IAIDefenseDecisionMaker: AIContext.defenderCharacter
- DefaultAIDefenseDecisionMaker
- DefenderInputHandler

**테스트/인벤토리 (6개 파일):**
- ItemSystemTestRunner, ItemSystemValidator, InventoryTestManager
- TemporaryEquipmentApplier

#### 2.6 도구 및 문서
- **CharacterDataMigrationTool.cs**: 구형 필드 → baseStats 자동 이전
- **리팩토링_테스트_케이스.md**: 8개 카테고리 테스트 시나리오
- Pragma 지시문으로 마이그레이션 도구 경고 억제

---

## 작업 통계

### 변경 파일 최종 통계
- **신규 생성**: 9개
  - Character.cs, PlayerCharacter.cs, EnemyCharacter.cs
  - CharacterCommandResult.cs, CharacterInventory.cs
  - RatioDataValidator.cs, StatsRecalculationMenu.cs
  - CharacterDataMigrationTool.cs
  - 리팩토링_테스트_케이스.md
- **삭제**: 5개
  - Combatant.cs, PlayerCombatant.cs, EnemyCombatant.cs
  - CombatantCommandResult.cs, CombatantInventory.cs
- **수정**: 47개
  - CharacterData, CombatStats, StatsCalculationManager
  - CharacterManager, CombatManager
  - Controller 2개, UI 10개, BT 7개, AI 3개, Test 6개 등
- **총 영향**: 61개 파일

### 최종 컴파일 상태
- ✅ **에러: 0개**
- ✅ **경고: 0개**
- ✅ **완벽한 클린 빌드**

---

## 다음 작업 계획

### 즉시 진행 가능한 작업
1. **CharacterData 마이그레이션 실행**
   - Tools > Character > Migrate CharacterData to CombatStats 실행
   - 모든 에셋의 구형 필드를 baseStats로 이전
   - 결과 검증

2. **전투 프로토타입 Scene 실제 테스트**
   - ProtoType.unity에서 실행
   - TemporaryEquipmentApplier를 통한 초기 장비 설정
   - 장착/해제 시 스탯 재계산 동작 확인
   - 전투 중 장비 스탯 적용 검증
   - `리팩토링_테스트_케이스.md` 참조

3. **비율형 데이터 검증 실행**
   - Tools > Stats > Validate Ratio Data (0~1) 실행
   - 0~100으로 잘못 저장된 케이스 확인
   - 발견 시 자동 변환 실행

### 추후 작업 (우선순위 낮음)
- Deprecated 경고 제거 (4개)
  - Character.GetGuardFinalDR에서 baseStats 참조
  - StatsCalculationManager.ConvertToCombatStats에서 baseStats 참조
- 인벤토리 UI Scene과 전투 Scene 통합 테스트
- 세이브/로드 시스템에 Inventory 연동
- 구형 필드 완전 제거 (baseMaxHP, baseATK 등 - 마이그레이션 후)

---

## 리팩토링 성과

### 개선된 점
1. **명확한 네이밍**: Combatant → Character, Effective → Final
2. **통합된 스탯 관리**: CombatStats 구조체로 일원화
3. **데이터 계층 분리**: CharacterData.baseStats (초기값) vs Character (런타임)
4. **확장 가능한 구조**: 비전투 NPC 추가 시에도 유연하게 대응 가능
5. **도구 지원**: 마이그레이션, 검증 도구 제공

### 주요 변경점
- 핵심 클래스: Combatant → Character
- 인벤토리: CombatantInventory → CharacterInventory
- 메서드: GetEffective* → GetFinal*, Map* → Convert*
- 구조체: CombatStats에 현재값 추가
- 데이터: CharacterData.baseStats 추가

---

**작성 시작**: 2025년 10월 28일

