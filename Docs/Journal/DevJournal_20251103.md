# 개발 일지 - 2025년 11월 3일

**작업 주제**: Scene 계층 구조 설계 및 CharacterManager 리팩토링  
**작업 시간**: 전일  
**상태**: CharacterManager 리팩토링 완료 / Scene 계층 구조 설계 완료

---

## 📋 오늘의 목표
- Scene 계층 구조 설계 및 문서화
- CharacterManager 리팩토링 (3개 매니저로 분리)

---

## ✅ 완료된 작업

### 1. Scene 계층 구조 설계 (설계 완료, 구현 대기)

#### 1-1. 설계 문서 작성
- **파일**: `Docs/Design/Scene/Scene_계층_구조_설계.md`
- **내용**:
  - 3-Layer Scene 구조 설계 (Core Systems / Persistent UI / Content)
  - Scene별 역할 및 생명주기 정의
  - Scene 전환 플로우 다이어그램
  - 매니저 아키텍처 및 데이터 흐름
  - PersistentUI 기술 상세 설명

#### 1-2. 설계된 Scene 목록
```
[Layer 1: Core Systems - 영속]
- CoreSystemScene
  └─ 영속 매니저들 (CharacterDatabaseManager, PlayerCharacterManager, etc)

[Layer 2: Persistent UI - 공통]
- PersistentUIScene
  └─ 공통 UI 요소들

[Layer 3: Content - 동적 로드/언로드]
- TitleScene (게임 시작)
- MainMenuScene (메인 메뉴)
- CombatScene (전투 - 기존 ProtoType Scene 기반)
- InventoryScene (인벤토리/장비 관리)
- ResultScene (전투 결과)
- TestScene (개발용 테스트) ← 추가 예정
```

---

### 2. CharacterManager 리팩토링 (완료)

#### 2-1. 매니저 분리
**Before (단일 매니저):**
```
CharacterManager (DontDestroyOnLoad)
├── PlayerData (영속)
├── EnemyData (임시)
├── PlayerCharacter (런타임)
└── EnemyCharacter (런타임)
```

**After (3개 매니저):**
```
PlayerCharacterManager (CoreSystemScene)
├── 플레이어 영속 데이터 (Level, Gold, Exp, Inventory, Equipment, Actions)
└── CreatePlayerCharacterForBattle() - 전투용 인스턴스 생성

CharacterDatabaseManager (CoreSystemScene)
├── CharacterDatabase (모든 캐릭터 인스턴스 정의)
└── GetEntry(instanceId) - 인스턴스 조회

CombatCharacterManager (CombatScene)
├── 전투 참가자 정보 (PlayerInstanceId, EnemyInstanceIds)
├── 전투용 인스턴스 (PlayerCharacter, EnemyCharacters)
└── InitializeBattle(playerId, enemyIds) - 전투 초기화
```

#### 2-2. 생성된 파일
**새 클래스:**
- `CharacterInitData.cs` (CharacterData에서 이름 변경)
- `CharacterInitDataLoader.cs` (Resources.Load 기반)
- `CharacterDatabase.cs` (ScriptableObject)
- `CharacterDatabaseEntry.cs` (Database Entry)
- `CharacterDatabaseManager.cs` (Database 런타임 관리)
- `PlayerCharacterManager.cs` (플레이어 영속 데이터)
- `CombatCharacterManager.cs` (전투 인스턴스 관리)

**삭제된 파일:**
- `CharacterManager.cs` (구버전)
- `CharacterData.cs` (CharacterInitData로 이름 변경)
- `CharacterInitDataProvider.cs` (Resources.Load로 대체)

#### 2-3. 업데이트된 파일
**Core:**
- `Character.cs` - CharacterInitData 사용, InstanceId 추가
- `PlayerCharacter.cs` - 생성자 시그니처 변경
- `EnemyCharacter.cs` - 생성자 시그니처 변경

**Combat:**
- `CombatManager.cs` - StartBattle(playerId, enemyIds) 진입점 추가, 테스트용 전투원 지정 기능
- `PlayerController.cs` - CombatCharacterManager 사용
- `EnemyController.cs` - CombatCharacterManager 사용
- `StatsCalculationManager.cs` - CharacterInitData 사용

**UI:**
- `CombatStatusDisplay.cs` - 이름 표시 추가, CombatCharacterManager 사용
- `BTMonitorUI.cs` - CombatCharacterManager 사용
- `HPPanelController.cs` - CombatCharacterManager 사용
- `InventoryUI.cs` - 전투 중/외 구분하여 PlayerCharacter 연결
- `ActionCommandEquipUI.cs` - 전투 중/외 구분하여 연결
- `PlayerActionSelectUI.cs` - CombatCharacterManager.PlayerCharacter 대기 후 초기화
- `EnemyActionSelectUI.cs` - CombatCharacterManager.CurrentEnemy 대기 후 초기화

**Test:**
- `StatsTest.cs` - CombatCharacterManager 사용
- `TemporaryEquipmentApplier.cs` - CombatCharacterManager 사용

**Editor:**
- `StatsRecalculationMenu.cs` - CombatCharacterManager 사용
- `CharacterInitDataKeyDrawer.cs` - 콤보박스 PropertyDrawer 생성

#### 2-4. 문서 작성
- `CharacterManager_분리_구현_계획서.md` (798줄)
- `CharacterManager_리팩토링_최종_체크리스트.md` (396줄)
- Scene_계층_구조_설계.md에 "매니저 아키텍처" 섹션 추가

---

### 3. 주요 개선 사항

#### 3-1. 데이터 구조 명확화
**CharacterInitData (템플릿):**
- 캐릭터 초기화 데이터 (기본 스탯, 초기 아이템, 초기 검술)
- `key` 필드로 식별
- Resources.Load로 로드

**CharacterDatabase (인스턴스 정의):**
- 게임 내 모든 캐릭터 인스턴스 정의
- `instanceId` (고유 ID) + `initDataKey` (템플릿 참조)
- 예: instanceId="Player", initDataKey="player_default"

**Character (런타임 인스턴스):**
- 실제 게임에서 사용되는 캐릭터 객체
- `InstanceId` + `CharacterInitData` 보유

#### 3-2. 전투 참가자 정보 명시화
**Before:**
```csharp
// 누가 싸우는지 불명확
CombatCharacterManager.PlayerCharacter
CombatCharacterManager.CurrentEnemy
```

**After:**
```csharp
// 전투 참가자 정보 명시
CombatCharacterManager.PlayerInstanceId: "Player"
CombatCharacterManager.EnemyInstanceIds: ["enemy_goblin_01"]

// 전투 시작
CombatManager.StartBattle("Player", "enemy_goblin_01");
```

#### 3-3. UI 초기화 타이밍 수정
**문제:** UI가 CombatCharacterManager보다 먼저 초기화되어 PlayerCharacter/CurrentEnemy를 찾지 못함

**해결:** Coroutine으로 대기 후 초기화
```csharp
// PlayerActionSelectUI, EnemyActionSelectUI, InventoryUI, ActionCommandEquipUI
private IEnumerator WaitForCombatCharacterAndInitialize()
{
    while (CombatCharacterManager.Instance == null || 
           CombatCharacterManager.Instance.PlayerCharacter == null)
    {
        yield return null;
    }
    Initialize();
}
```

---

## 📊 현재 상태

### ✅ 완료
- CharacterManager 리팩토링 100% 완료
- 모든 컴파일 에러 해결
- 전투 테스트 정상 동작 확인:
  - 캐릭터 이름 표시 ✅
  - 검술 리스트 생성 ✅
  - 인벤토리 연결 ✅
  - 전투 진행 ✅

### 🔄 진행 중
- Scene 계층 구조 설계 완료, 구현 대기

---

## 🎯 다음 작업: Scene 계층 구조 구현

### 📍 현재 상황
**설계는 완료되었으나 실제 Scene은 생성되지 않음**

**현재 Scene 상태:**
```
Assets/Scenes/
├─ ProtoType.unity (전투 테스트용 - 모든 것이 여기 있음)
└─ SampleScene.unity (미사용)
```

**필요한 Scene:**
```
Assets/Scenes/
├─ Core/
│  └─ CoreSystemScene.unity (영속 매니저들) ← 생성 필요
├─ UI/
│  └─ PersistentUIScene.unity (공통 UI) ← 생성 필요
└─ Content/
   ├─ TestScene.unity (개발용 테스트) ← 생성 필요 🎯
   ├─ CombatScene.unity (ProtoType 기반 정리) ← 정리 필요
   ├─ TitleScene.unity (게임 시작) ← 추후
   ├─ MainMenuScene.unity (메인 메뉴) ← 추후
   ├─ InventoryScene.unity (인벤토리) ← 추후
   └─ ResultScene.unity (전투 결과) ← 추후
```

---

### 🗓️ 내일 작업 계획 (2025-11-04)

#### Phase 1: 최소 Scene 구조 구현 (우선순위: 높음)

**목표:** TestScene에서 CombatScene을 로드/언로드하는 기본 플로우 구현

**Step 1-1: CoreSystemScene 생성 (30분)**
```
작업 내용:
1. Scene 생성: Assets/Scenes/Core/CoreSystemScene.unity
2. GameObject 배치:
   - CharacterDatabaseManager
   - PlayerCharacterManager
   - (필요시) 기타 영속 매니저들
3. 스크립트 작성: CoreSystemInitializer.cs
   - Scene 로드 순서 관리
   - PersistentUIScene Additive 로드
   - 초기 Content Scene 로드
```

**Step 1-2: TestScene 생성 (1시간)**
```
작업 내용:
1. Scene 생성: Assets/Scenes/Content/TestScene.unity
2. UI 구성:
   - 전투 시작 버튼
   - 전투원 선택 드롭다운 (Player ID, Enemy ID)
   - 전투 설정 옵션
3. 스크립트 작성: TestSceneManager.cs
   - 전투 시작: CombatScene Additive 로드
   - 전투 종료 처리: CombatScene 언로드, TestScene 복귀
4. 이벤트 연결:
   - CombatManager.OnBattleEnd 이벤트 구독
```

**Step 1-3: CombatScene 정리 (1시간)**
```
작업 내용:
1. ProtoType Scene → CombatScene 이름 변경
2. 독립 실행 불가 설정:
   - CoreSystemScene 없이는 실행 안됨
   - CombatCharacterManager만 포함 (영속 매니저 제거)
3. 전투 종료 처리:
   - CombatManager.FinalizeBattle() 호출 시점 명확화
   - OnBattleEnd 이벤트 발생
```

**Step 1-4: Scene 전환 플로우 테스트 (30분)**
```
테스트 시나리오:
1. CoreSystemScene 실행
   → TestScene 자동 로드
2. TestScene에서 "전투 시작" 버튼 클릭
   → CombatScene Additive 로드
   → 전투 진행
3. 전투 종료
   → CombatScene 언로드
   → TestScene 복귀
   → 플레이어 상태 유지 확인
```

**예상 소요 시간: 3시간**

---

#### Phase 2: PersistentUIScene 구현 (우선순위: 중간)

**Step 2-1: PersistentUIScene 생성 (1시간)**
```
작업 내용:
1. Scene 생성: Assets/Scenes/UI/PersistentUIScene.unity
2. Canvas 구성:
   - Render Mode: Screen Space - Overlay
   - Sort Order: 높게 설정 (Content Scene 위에 표시)
3. 공통 UI 요소 이동:
   - 디버그 패널 (BTMonitorUI, CombatStatusDisplay 등)
   - 설정 버튼
   - 로딩 인디케이터
4. PersistentUIManager.cs 작성:
   - UI 가시성 제어
   - Scene별 UI 표시 정책
```

**예상 소요 시간: 1시간**

---

#### Phase 3: 전투 결과 처리 구현 (우선순위: 중간)

**Step 3-1: BattleResult 확장 (30분)**
```
작업 내용:
1. BattleResult.cs 확장:
   - 전투 통계 (턴 수, 데미지, 크리티컬 등)
   - 획득 보상 상세 (아이템, 경험치, 골드)
2. CombatManager.FinalizeBattle() 완성:
   - BattleResult 생성
   - CombatCharacterManager.FinalizeBattle() 호출
   - OnBattleEnd 이벤트 발생
```

**Step 3-2: ResultScene 생성 (선택사항)**
```
작업 내용:
1. Scene 생성: Assets/Scenes/Content/ResultScene.unity
2. UI 구성:
   - 승패 표시
   - 전투 통계
   - 획득 보상
   - "계속" 버튼
```

**예상 소요 시간: 1-2시간**

---

### 📋 전체 우선순위 정리

**1순위 (필수):**
- CoreSystemScene 생성 및 설정
- TestScene 생성 및 전투 시작/종료 플로우
- CombatScene 정리

**2순위 (중요):**
- PersistentUIScene 구현
- 전투 결과 처리 구현

**3순위 (추후):**
- TitleScene, MainMenuScene, InventoryScene 구현
- ResultScene 구현
- Scene 간 데이터 전달 시스템 고도화

---

## 🎯 내일 목표 (2025-11-04)

### 최소 목표 (Must Have)
- [ ] CoreSystemScene 생성 완료
- [ ] TestScene 생성 완료 (전투 시작 기능)
- [ ] TestScene → CombatScene → TestScene 플로우 동작 확인

### 목표 (Should Have)
- [ ] PersistentUIScene 생성 완료
- [ ] 전투 종료 시 상태 동기화 동작 확인

### 추가 목표 (Nice to Have)
- [ ] ResultScene 구현
- [ ] 전투 결과 UI 표시

---

## 📝 개발 노트

### 배운 점
1. **책임 분리의 중요성**
   - CharacterManager 하나가 너무 많은 역할을 담당했음
   - 영속 데이터 / 인스턴스 정의 / 전투 인스턴스를 명확히 분리하니 코드가 명확해짐

2. **타이밍 이슈 해결**
   - Unity의 비동기 초기화 순서 문제
   - Coroutine을 활용한 대기 패턴이 효과적

3. **데이터 vs 인스턴스 구분**
   - CharacterInitData (템플릿) ≠ Character (인스턴스)
   - instanceId (게임 내 고유 ID) ≠ initDataKey (템플릿 참조)

### 어려웠던 점
1. **전체 코드베이스 업데이트**
   - 18개 파일에 흩어진 CharacterManager 참조
   - 하나씩 수동으로 확인하며 업데이트 필요

2. **UI 초기화 순서**
   - 여러 UI 컴포넌트가 각자 다른 타이밍에 초기화됨
   - 각 UI마다 대기 로직 추가 필요

### 다음에 개선할 점
1. Scene 전환 시스템을 먼저 구현하고 매니저 분리를 했다면 더 명확했을 것
2. 하지만 매니저 분리가 먼저 되어야 Scene에 배치할 수 있으므로, 순서는 적절했음

---

## 🔗 관련 문서
- `Docs/Design/Scene/Scene_계층_구조_설계.md`
- `Docs/Design/CharacterManager_분리_구현_계획서.md`
- `Docs/Design/CharacterManager_리팩토링_최종_체크리스트.md`

---

## 📊 프로젝트 진행률

```
[=====================================>           ] 75%

완료:
- 전투 시스템 프로토타입 ✅
- CharacterManager 리팩토링 ✅
- Scene 계층 구조 설계 ✅

진행 중:
- Scene 계층 구조 구현 🔄

예정:
- 전투 결과 처리
- 인벤토리/장비 Scene 분리
- 타이틀/메인 메뉴 구현
```









