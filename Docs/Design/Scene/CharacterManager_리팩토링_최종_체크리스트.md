# CharacterManager 리팩토링 최종 체크리스트

**작성일**: 2025-11-03  
**목적**: 리팩토링 완료 후 Unity 에디터 설정 가이드  
**관련 문서**: `CharacterManager_분리_구현_계획서.md`, `Scene_계층_구조_설계.md`

---

## 📋 주요 변경사항 요약

### ✅ 완료된 코드 변경

1. **CharacterInitDataProvider 제거** → `CharacterInitDataLoader` (정적 유틸리티)로 대체
2. **CombatCharacterManager 개선**
   - 전투 참가자 정보 필드 추가 (`PlayerInstanceId`, `EnemyInstanceIds`)
   - `InitializeBattle(playerId, enemyIds)` 시그니처 변경
   - `CreatePlayer()`, `CreateEnemy()` 메서드 분리
3. **CombatManager 개선**
   - `StartBattle(playerId, enemyIds)` 진입점 추가
   - 테스트용 전투원 지정 기능 (Inspector)
4. **Resources.Load 기반 로딩**
   - CharacterInitData를 `Resources/CharacterData/` 폴더에서 로드

---

## 🎯 Unity 에디터 설정 단계

### 📦 Phase 1: Resources 폴더 구조 확인

#### 1-1. 현재 폴더 구조 (이미 설정됨)

```
Project 창:
Assets/
└─ Resources/
   └─ Data/
      └─ CharacterData/
         ├─ CharacterDatabase.asset  ← 이미 존재
         └─ CharacterInitData/
            ├─ Player.asset          ← 이미 존재
            └─ Test_Enemy1.asset     ← 이미 존재
```

**⚠️ 작업 불필요!**
이미 올바른 위치에 파일들이 있습니다. 이동하지 마세요!

---

#### 1-2. CharacterInitData 에셋 설정 확인

**Player CharacterInitData (`Player.asset`)**
```
Inspector:
├─ Key: "Player"  ✅ 이미 설정됨 (파일명과 동일)
├─ Character Name: "플레이어"
├─ Base Stats: (기존 값 유지)
├─ Initial Items: (기존 값 유지)
├─ Initial Equipment: (기존 값 유지)
└─ Initial Actions: (기존 값 유지)
```

**Enemy CharacterInitData (`Test_Enemy1.asset`)**
```
Inspector:
├─ Key: "Test_Enemy1"  ✅ 이미 설정됨 (파일명과 동일)
├─ Character Name: (적 이름)
├─ Base Stats: (기존 값 유지)
├─ Initial Items: (기존 값 유지)
├─ Initial Equipment: (기존 값 유지)
├─ Initial Actions: (기존 값 유지)
└─ Behavior Tree: (BT 에셋 연결)
```

**⚠️ 중요:**
- `key` 필드 값과 파일명(확장자 제외)이 **반드시 일치**해야 합니다!
- 예: 파일명 `Player.asset` → key 필드 `"Player"`
- **현재 프로젝트는 이미 올바르게 설정되어 있습니다!** ✅

---

### 📦 Phase 2: CharacterDatabase 설정 확인

#### 2-1. CharacterDatabase 에셋 확인

**파일 위치:** `Assets/Resources/Data/CharacterData/CharacterDatabase.asset` ✅

---

#### 2-2. CharacterDatabase 설정 확인

```
Inspector (이미 설정됨):
┌─────────────────────────────────────────┐
│ Player Entry                            │
│ ├─ Instance Id: "Player"               │ ✅
│ └─ Init Data Key: "Player"             │ ✅
│                                         │
│ Enemy Entries (Size: 1)                │
│   Element 0:                            │
│   ├─ Instance Id: "Test_Enemy1"        │ ✅
│   └─ Init Data Key: "Test_Enemy1"      │ ✅
└─────────────────────────────────────────┘
```

**✅ 현재 상태:**
- CharacterDatabase가 이미 올바르게 설정되어 있습니다!
- Player와 Test_Enemy1이 정상적으로 등록됨

**⚠️ 참고:**
- **Instance Id**: 게임에서 사용할 고유 ID
- **Init Data Key**: CharacterInitData 파일의 key 필드 값 (파일명과 일치)

---

### 🎮 Phase 3: ProtoType Scene GameObject 설정

#### 3-1. 제거할 GameObject

```
❌ 삭제 (있다면):
   - CharacterInitDataProvider
   - CharacterManager (구버전)
```

**이유:** Resources.Load로 대체되어 더 이상 불필요

---

#### 3-2. 유지/수정할 GameObject

**① CharacterDatabaseManager**
```
Hierarchy:
└─ CharacterDatabaseManager (Empty GameObject)
   └─ Component: CharacterDatabaseManager

Inspector 설정:
┌─────────────────────────────────────────┐
│ CharacterDatabaseManager (Script)       │
│                                         │
│ Database Asset                          │
│ └─ [CharacterDatabase 에셋 드래그]     │
└─────────────────────────────────────────┘
```

**② PlayerCharacterManager**
```
Hierarchy:
└─ PlayerCharacterManager (Empty GameObject)
   └─ Component: PlayerCharacterManager

Inspector 설정:
┌─────────────────────────────────────────┐
│ PlayerCharacterManager (Script)         │
│                                         │
│ (모든 필드 자동 설정 - 수동 입력 불필요)  │
└─────────────────────────────────────────┘
```

**③ CombatCharacterManager**
```
Hierarchy:
└─ CombatCharacterManager (Empty GameObject)
   └─ Component: CombatCharacterManager

Inspector 설정:
┌─────────────────────────────────────────┐
│ CombatCharacterManager (Script)         │
│                                         │
│ (모든 필드 자동 설정 - 수동 입력 불필요)  │
└─────────────────────────────────────────┘
```

**④ CombatManager** (테스트 기능 추가)
```
Hierarchy:
└─ CombatManager (기존 GameObject)
   └─ Component: CombatManager

Inspector 설정 (새로 추가된 부분):
┌─────────────────────────────────────────┐
│ 테스트: 전투 참가자 지정                │
│ ├─ Test Player Instance Id: "Player"   │ ✅ 올바른 값
│ └─ Test Enemy Instance Ids (Size: 1)   │
│     └─ Element 0: "Test_Enemy1"        │ ✅ 올바른 값
│                                         │
│ 컨트롤러                                │
│ ├─ Player Controller: [연결됨]         │
│ └─ Enemy Controller: [연결됨]          │
│ ... (기존 필드들)                       │
└─────────────────────────────────────────┘
```

**💡 테스트 기능 설명:**
- `Test Player Instance Id`: 테스트용 플레이어 ID 지정
- `Test Enemy Instance Ids`: 테스트용 적 ID 배열 지정
- 추후 상위 시스템(필드, 던전)에서 `StartBattle(playerId, enemyIds)` 직접 호출 가능

---

## 🧪 테스트 단계

### ✅ 1단계: 기본 초기화 테스트

**Play 모드 진입 후 Console 로그 확인:**

```
✅ 정상 로그 예시:

[CharacterDatabaseManager] Player 등록: player (템플릿: player_default)
[CharacterDatabaseManager] Enemy 등록: enemy_goblin_01 (템플릿: goblin_warrior)
[CharacterDatabaseManager] 초기화 완료: 2개 Character 인스턴스 정의됨

[CombatManager] === 전투 시작 명령 수신 ===
[CombatManager] 전투원: player vs [enemy_goblin_01]

[CombatCharacterManager] === 전투 초기화 시작 ===
[CombatCharacterManager] 전투 참가자: player vs [enemy_goblin_01]
[CharacterInitDataLoader] ✅ 로드 성공: player_default (플레이어)
[CombatCharacterManager] ✅ Player 생성: 플레이어 (ID: player)
[CharacterInitDataLoader] ✅ 로드 성공: goblin_warrior (고블린 전사)
[CombatCharacterManager] ✅ Enemy 생성: 고블린 전사 (ID: enemy_goblin_01)
[CombatCharacterManager] === 전투 초기화 완료: 플레이어 vs 고블린 전사 (외 0명) ===
```

---

### ❌ 오류 로그 및 해결

#### 오류 1: CharacterInitData를 찾을 수 없음
```
[CharacterInitDataLoader] 'CharacterData/player_default'에서 CharacterInitData를 찾을 수 없습니다!
```
**원인:** CharacterInitData 에셋이 `Resources/CharacterData/` 폴더에 없음  
**해결:** Phase 1-1 참고하여 에셋을 올바른 위치로 이동

---

#### 오류 2: 파일명과 key 불일치
```
[CharacterInitDataLoader] 파일명(player_default)과 key 필드(player)가 일치하지 않습니다!
```
**원인:** CharacterInitData의 `key` 필드가 파일명과 다름  
**해결:** `key` 필드를 파일명과 동일하게 수정

---

#### 오류 3: Instance를 CharacterDatabase에서 찾을 수 없음
```
[CombatCharacterManager] Enemy Instance 'enemy_goblin_01'를 CharacterDatabase에서 찾을 수 없습니다!
```
**원인:** CharacterDatabase에 해당 Instance ID가 등록되지 않음  
**해결:** Phase 2-2 참고하여 CharacterDatabase에 Entry 추가

---

### ✅ 2단계: 전투 시스템 테스트

1. **캐릭터 표시 확인**
   - [ ] 플레이어 캐릭터 표시됨
   - [ ] 적 캐릭터 표시됨

2. **UI 표시 확인**
   - [ ] HP/Poise UI 정상 표시
   - [ ] 액션 커맨드 UI 정상 표시

3. **전투 진행 확인**
   - [ ] 턴 시스템 정상 작동
   - [ ] 애니메이션 재생 정상
   - [ ] 데미지 계산 정상
   - [ ] 전투 종료 처리 정상

---

### ✅ 3단계: 테스트 기능 검증

**CombatManager Inspector에서 전투원 변경 테스트:**

1. **다른 적과 싸우기**
   ```
   Test Enemy Instance Ids:
   └─ Element 0: "enemy_boss_dragon"  ← 변경
   ```
   → Play 모드 진입 → 다른 적과 전투 시작 확인

2. **다중 적 전투 (추후 지원)**
   ```
   Test Enemy Instance Ids (Size: 2):
   ├─ Element 0: "enemy_goblin_01"
   └─ Element 1: "enemy_goblin_02"
   ```

---

## 📊 최종 체크리스트

### 파일 및 폴더 구조

```
□ Assets/Resources/CharacterData/ 폴더 생성됨
□ player_default.asset → Resources/CharacterData/ 이동
□ goblin_warrior.asset → Resources/CharacterData/ 이동
□ CharacterDatabase.asset 생성됨
```

### 에셋 설정

```
□ player_default.asset - key 필드 = "player_default"
□ goblin_warrior.asset - key 필드 = "goblin_warrior"
□ CharacterDatabase.asset - Player Entry 설정됨
□ CharacterDatabase.asset - Enemy Entry 설정됨
```

### Scene GameObject

```
□ CharacterInitDataProvider GameObject 삭제됨
□ CharacterDatabaseManager - Database Asset 연결됨
□ PlayerCharacterManager - 존재함
□ CombatCharacterManager - 존재함
□ CombatManager - 테스트 필드 설정됨
```

### 테스트 결과

```
□ 컴파일 에러 0개
□ Play 모드 정상 진입
□ Console에 정상 로그 출력
□ 캐릭터 표시 정상
□ 전투 시스템 정상 작동
```

---

## 🎯 상위 시스템 연동 가이드 (추후)

### 필드/던전 시스템에서 전투 시작하기

```csharp
// 예시: 필드에서 적 조우
public class FieldManager : MonoBehaviour
{
    public void OnEnemyEncounter(string enemyId)
    {
        // 전투 Scene 로드
        SceneManager.LoadScene("CombatScene", LoadSceneMode.Additive);
        
        // CombatManager에 전투 시작 지시
        CombatManager.Instance.StartBattle("player", enemyId);
    }
    
    public void OnBossEncounter(string bossId)
    {
        // 보스전 시작
        CombatManager.Instance.StartBattle("player", bossId);
    }
    
    public void OnMultipleEnemyEncounter(params string[] enemyIds)
    {
        // 다중 적 전투 시작
        CombatManager.Instance.StartBattle("player", enemyIds);
    }
}
```

---

## 🔑 핵심 포인트

1. **CharacterInitData는 Resources 폴더에**
   - `Resources/CharacterData/` 필수
   - 파일명과 key 필드 일치 필수

2. **CharacterDatabase = 게임 내 캐릭터 인스턴스 정의**
   - Instance ID = 게임 내 고유 식별자
   - Init Data Key = 초기화 템플릿 참조

3. **CombatCharacterManager = 전투 참가자 관리 + 인스턴스 생성**
   - "누가 싸우는지" 정보 저장
   - 전투원 인스턴스 생성 및 제공

4. **CombatManager = 전투 진행 총괄**
   - StartBattle(playerId, enemyIds) 진입점
   - 테스트용: Inspector에서 전투원 지정 가능
   - 정식용: 상위 시스템에서 호출

---

## 📝 추가 참고 문서

- `CharacterManager_분리_구현_계획서.md`: 상세한 설계 문서
- `Scene_계층_구조_설계.md`: Scene 구조 및 매니저 배치
- `최종-테스트-가이드.md`: 기존 테스트 가이드 (업데이트 필요)

