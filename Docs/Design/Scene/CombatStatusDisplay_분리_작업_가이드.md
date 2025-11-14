# CombatStatusDisplay 분리 작업 가이드

**작성일**: 2025-11-04  
**목적**: CombatStatusDisplay를 CombatDebugDisplay와 CombatHUD로 분리  
**상태**: 코드 작업 완료 → Unity Editor 작업 필요

---

## 📋 작업 개요

기존의 `CombatStatusDisplay.cs`를 **디버그 정보 표시**와 **게임 HUD** 기능으로 분리했습니다.

### 분리된 컴포넌트

| 기존 | 분리 후 | 배치 Scene | 역할 |
|------|---------|-----------|------|
| CombatStatusDisplay.cs | **CombatDebugDisplay.cs** | PersistentUIScene | 개발자용 디버그 정보 표시 |
| ↓ | **CombatHUD.cs** | CombatScene | 플레이어용 게임 HUD |

---

## ✅ 완료된 작업 (코드)

- ✅ `CombatDebugDisplay.cs` 생성 (`Assets/Script/UI/`)
- ✅ `CombatHUD.cs` 생성 (`Assets/Script/UI/`)
- ✅ `CombatManager.cs` 수정 (호출부 변경)
- ✅ `PlayerController.cs` 수정
- ✅ `EnemyController.cs` 수정
- ✅ `InputVersusResult.cs` 수정
- ✅ `StatsRecalculationMenu.cs` 수정
- ✅ `CombatStatusDisplay.cs` 삭제

---

## 🎯 Unity Editor 작업 (사용자가 직접 수행)

### 단계별 작업 흐름

```
1. PersistentUIScene 생성 (아직 없다면)
2. Canvus_Debug를 PersistentUIScene으로 이동
3. CombatDebugDisplay 컴포넌트 추가 및 UI 연결
4. CombatScene의 Canvas_HUD에 CombatHUD 컴포넌트 추가 및 UI 연결
5. 테스트
```

---

## 📍 Phase 1: PersistentUIScene 생성 및 구성

### 1-1. PersistentUIScene 생성

**목표**: 공통 UI 요소를 담을 Scene 생성

**작업**:
1. Unity Editor에서 `File > New Scene` 선택
2. 새 Scene 이름: `PersistentUIScene`
3. 저장 위치: `Assets/Scenes/UI/PersistentUIScene.unity`
   - `UI` 폴더가 없다면 생성

**Scene 구조**:
```
PersistentUIScene
├── Main Camera (기본 생성됨 - 삭제 가능)
├── Directional Light (기본 생성됨 - 삭제 가능)
└── EventSystem (나중에 CoreSystemScene에서 관리할 예정)
```

**주의**: 
- PersistentUIScene은 UI만 담으므로 Camera, Light는 필요 없음 (삭제해도 됨)
- EventSystem은 나중에 CoreSystemScene으로 옮길 예정

---

### 1-2. ProtoType Scene에서 Canvus_Debug 이동

**목표**: 디버그 UI를 PersistentUIScene으로 이동

**작업**:
1. `ProtoType.unity` (또는 `CombatScene.unity`) 열기
2. Hierarchy에서 `Canvus_Debug` 선택
3. **Prefab으로 만들기** (권장):
   - `Canvus_Debug`를 Project 창의 `Assets/Prefab/UI/` 폴더로 드래그
   - Prefab 이름: `DebugUI.prefab`
4. ProtoType Scene에서 `Canvus_Debug` 삭제
5. `PersistentUIScene.unity` 열기
6. `DebugUI.prefab`을 Hierarchy로 드래그하여 배치

**결과**:
```
PersistentUIScene
└── Canvus_Debug (DebugUI Prefab)
    ├── DebugInfoPanel
    ├── Button_DebugMode
    ├── Button_CombatInfo
    └── Button_BTInfo
```

---

### 1-3. CombatDebugDisplay GameObject 생성 및 컴포넌트 추가

**목표**: 디버그 정보를 표시할 CombatDebugDisplay 컴포넌트 추가

**작업**:
1. `PersistentUIScene.unity` 열기
2. Hierarchy에서 **빈 GameObject 생성**:
   - 우클릭 > `Create Empty`
   - 이름: `CombatDebugDisplay`
3. `CombatDebugDisplay` GameObject 선택
4. Inspector에서 `Add Component` 클릭
5. `CombatDebugDisplay` 스크립트 추가

**결과**:
```
PersistentUIScene
├── Canvus_Debug (DebugUI Prefab)
└── CombatDebugDisplay ← 새로 생성
    └── CombatDebugDisplay (Script)
```

---

### 1-4. CombatDebugDisplay 컴포넌트에 UI 오브젝트 연결

**목표**: CombatDebugDisplay가 디버그 UI 요소들을 참조하도록 설정

**Canvus_Debug의 기대 구조** (ProtoType Scene 기준):

```
Canvus_Debug (Canvas)
├── DebugInfoPanel (Panel)
│   ├── Player_DebugInfo (Panel)
│   │   ├── Text_PlayerName (TextMeshProUGUI)
│   │   ├── Text_PlayerHP (TextMeshProUGUI)
│   │   ├── Text_PlayerPoise (TextMeshProUGUI)
│   │   ├── Text_PlayerATK (TextMeshProUGUI)
│   │   ├── Text_PlayerDR (TextMeshProUGUI)
│   │   ├── Text_PlayerCrit (TextMeshProUGUI)
│   │   ├── Text_PlayerActionCommand (TextMeshProUGUI)
│   │   ├── Text_PlayerInputCooldown (TextMeshProUGUI)
│   │   └── PlayerHitResultContainer (Transform/Panel)
│   ├── Enemy_DebugInfo (Panel)
│   │   ├── Text_EnemyName (TextMeshProUGUI)
│   │   ├── Text_EnemyHP (TextMeshProUGUI)
│   │   ├── Text_EnemyPoise (TextMeshProUGUI)
│   │   ├── Text_EnemyATK (TextMeshProUGUI)
│   │   ├── Text_EnemyDR (TextMeshProUGUI)
│   │   ├── Text_EnemyCrit (TextMeshProUGUI)
│   │   ├── Text_EnemyActionCommand (TextMeshProUGUI)
│   │   ├── Text_EnemyInputCooldown (TextMeshProUGUI)
│   │   └── EnemyHitResultContainer (Transform/Panel)
│   ├── Text_ActionProgress (TextMeshProUGUI)
│   ├── TurnResultContainer (Transform/Panel)
│   └── ResultLinePrefab (Prefab 참조)
├── Button_DebugMode
├── Button_CombatInfo
└── Button_BTInfo
```

**연결 작업**:

1. `CombatDebugDisplay` GameObject 선택
2. Inspector에서 다음 필드들을 연결:

#### Player Debug UI
| 필드 이름 | 연결할 오브젝트 | 위치 |
|----------|---------------|------|
| **Player Name** | `Text_PlayerName` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player HP** | `Text_PlayerHP` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player Poise** | `Text_PlayerPoise` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player ATK** | `Text_PlayerATK` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player DR** | `Text_PlayerDR` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player Crit** | `Text_PlayerCrit` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player Action Command Name** | `Text_PlayerActionCommand` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player Action Input Cooldown** | `Text_PlayerInputCooldown` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |
| **Player Hit Result Container** | `PlayerHitResultContainer` | `Canvus_Debug/DebugInfoPanel/Player_DebugInfo/` |

#### Enemy Debug UI
| 필드 이름 | 연결할 오브젝트 | 위치 |
|----------|---------------|------|
| **Enemy Name** | `Text_EnemyName` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy HP** | `Text_EnemyHP` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy Poise** | `Text_EnemyPoise` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy ATK** | `Text_EnemyATK` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy DR** | `Text_EnemyDR` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy Crit** | `Text_EnemyCrit` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy Action Command Name** | `Text_EnemyActionCommand` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy Action Input Cooldown** | `Text_EnemyInputCooldown` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |
| **Enemy Hit Result Container** | `EnemyHitResultContainer` | `Canvus_Debug/DebugInfoPanel/Enemy_DebugInfo/` |

#### Combat Log
| 필드 이름 | 연결할 오브젝트 | 위치 |
|----------|---------------|------|
| **Action Progress** | `Text_ActionProgress` | `Canvus_Debug/DebugInfoPanel/` |
| **Turn Result Container** | `TurnResultContainer` | `Canvus_Debug/DebugInfoPanel/` |
| **Result Line Prefab** | `ResultLinePrefab` (Prefab) | `Assets/Prefab/UI/` 또는 기존 Prefab |

**연결 방법**:
- Hierarchy에서 해당 오브젝트를 드래그하여 Inspector의 필드에 드롭
- 또는 필드 오른쪽의 동그라미 아이콘 클릭 → 오브젝트 선택

**주의**:
- 오브젝트 이름은 ProtoType Scene 구조에 따라 다를 수 있음
- 실제 Hierarchy 구조를 확인하여 연결할 것

---

## 📍 Phase 2: CombatScene의 Canvas_HUD 구성

### 2-1. ProtoType Scene 확인 (또는 CombatScene)

**목표**: 전투 HUD 구조 확인

**작업**:
1. `ProtoType.unity` (또는 `CombatScene.unity`) 열기
2. Hierarchy에서 `Canvas_HUD` 구조 확인

**기대 구조**:
```
Canvas_HUD (Canvas)
├── Panel (전체 HUD 컨테이너)
│   ├── Panel_TurnInfo (턴 정보)
│   │   ├── Text_TurnLabel (TextMeshProUGUI) - 턴 타이머
│   │   ├── Image_TurnTimerBar (Image) - 프로그레스 바
│   │   ├── Image_TurnTimerBarBG (Image) - 배경
│   │   └── GuideContainer (RectTransform) - Perfect Timing 가이드 부모
│   ├── Panel_InputPrompt (입력 프롬프트)
│   │   └── Text_InputPrompt (TextMeshProUGUI)
│   ├── Panel_BattleEnd (전투 종료)
│   │   └── Text_BattleEndMessage (TextMeshProUGUI)
├── Panel_HP (HP 표시)
│   ├── PlayerHPPanel
│   └── EnemyHPPanel
└── Panel_TurnTimer (턴 타이머 별도 패널)
```

**실제 구조가 다를 수 있음** - ProtoType Scene의 실제 구조를 확인해주세요.

---

### 2-2. CombatHUD GameObject 생성 및 컴포넌트 추가

**목표**: 게임 HUD를 관리할 CombatHUD 컴포넌트 추가

**작업**:
1. `ProtoType.unity` (또는 `CombatScene.unity`) 열기
2. Hierarchy에서 **빈 GameObject 생성**:
   - 우클릭 > `Create Empty`
   - 이름: `CombatHUD`
   - 위치: `Canvas_HUD`와 같은 레벨 (Root 또는 Managers 폴더)
3. `CombatHUD` GameObject 선택
4. Inspector에서 `Add Component` 클릭
5. `CombatHUD` 스크립트 추가

**결과**:
```
CombatScene (또는 ProtoType)
├── Canvas_HUD (기존)
├── CombatManager
├── CombatCharacterManager
└── CombatHUD ← 새로 생성
    └── CombatHUD (Script)
```

---

### 2-3. CombatHUD 컴포넌트에 UI 오브젝트 연결

**목표**: CombatHUD가 게임 HUD 요소들을 참조하도록 설정

**연결 작업**:

1. `CombatHUD` GameObject 선택
2. Inspector에서 다음 필드들을 연결:

#### Turn Indicator
| 필드 이름 | 연결할 오브젝트 | 설명 | 위치 추정 |
|----------|---------------|------|---------|
| **Player Turn Container** | `Panel_HP/PlayerHPPanel` 또는 별도 Panel | 플레이어 턴일 때 초록색 강조 | `Canvas_HUD/Panel_HP/` |
| **Enemy Turn Container** | `Panel_HP/EnemyHPPanel` 또는 별도 Panel | 적 턴일 때 빨간색 강조 | `Canvas_HUD/Panel_HP/` |

**주의**:
- ProtoType Scene에 이 기능이 없다면, 새로운 Panel을 만들어도 됨
- 또는 기존의 HP Panel 배경 Image를 사용
- CombatStatusDisplay의 `whosTurnText()` 메서드에서 `playerHitResultContainer`, `enemyHitResultContainer`를 사용했음
  - 해당 오브젝트들을 찾아서 연결

#### Turn Timer
| 필드 이름 | 연결할 오브젝트 | 타입 | 위치 추정 |
|----------|---------------|------|---------|
| **Turn Label** | `Text_TurnLabel` | TextMeshProUGUI | `Canvas_HUD/Panel_TurnInfo/` |
| **Turn Timer Progress Bar** | `Image_TurnTimerBar` | Image (Type=Filled 권장) | `Canvas_HUD/Panel_TurnInfo/` |
| **Turn Timer Progress Bar Background** | `Image_TurnTimerBarBG` | Image | `Canvas_HUD/Panel_TurnInfo/` |

#### Perfect Timing Guide
| 필드 이름 | 연결할 오브젝트 | 타입 | 위치 추정 |
|----------|---------------|------|---------|
| **Perfect Timing Guide Prefab** | `PerfectTimingGuide` Prefab | Prefab | `Assets/Prefab/UI/` |
| **Guide Container** | `GuideContainer` | RectTransform | `Canvas_HUD/Panel_TurnInfo/` (턴 타이머 바 부모) |

#### Input Prompt
| 필드 이름 | 연결할 오브젝트 | 타입 | 위치 추정 |
|----------|---------------|------|---------|
| **Input Prompt Text** | `Text_InputPrompt` | TextMeshProUGUI | `Canvas_HUD/Panel_InputPrompt/` |

#### Battle End
| 필드 이름 | 연결할 오브젝트 | 타입 | 위치 추정 |
|----------|---------------|------|---------|
| **Battle End Message** | `Text_BattleEndMessage` | TextMeshProUGUI | `Canvas_HUD/Panel_BattleEnd/` 또는 `actionProgress` 재사용 가능 |

**기존 CombatStatusDisplay에서 사용하던 필드 매핑**:

| 기존 CombatStatusDisplay 필드 | CombatHUD 필드 | 비고 |
|------------------------------|--------------|------|
| `inputPromptText` | Input Prompt Text | ✅ 동일 |
| `turnLabel` | Turn Label | ✅ 동일 |
| `turnTimerProgressBar` | Turn Timer Progress Bar | ✅ 동일 |
| `turnTimerProgressBarBackground` | Turn Timer Progress Bar Background | ✅ 동일 |
| `perfectTimingGuidePrefab` | Perfect Timing Guide Prefab | ✅ 동일 |
| `guideContainer` | Guide Container | ✅ 동일 |
| `actionProgress` | Battle End Message | ⚠️ 용도 변경 또는 별도 Text 사용 |
| `playerHitResultContainer` (Image) | Player Turn Container | ⚠️ 턴 표시용으로 재사용 |
| `enemyHitResultContainer` (Image) | Enemy Turn Container | ⚠️ 턴 표시용으로 재사용 |

**연결 팁**:
1. 기존 ProtoType Scene의 `CombatStatusDisplay` 컴포넌트가 붙어있던 GameObject를 찾기
2. 해당 컴포넌트에 연결되어 있던 오브젝트들을 확인
3. 같은 오브젝트들을 새로운 `CombatHUD` 컴포넌트에 연결

---

## 📍 Phase 3: 기존 CombatStatusDisplay 컴포넌트 제거

### 3-1. ProtoType Scene 정리

**목표**: 기존 CombatStatusDisplay 컴포넌트 제거

**작업**:
1. `ProtoType.unity` (또는 `CombatScene.unity`) 열기
2. Hierarchy에서 `CombatStatusDisplay` GameObject 또는 컴포넌트 찾기
3. **GameObject 전체 삭제** 또는 **컴포넌트만 제거**
   - GameObject 이름이 `CombatStatusDisplay`라면 전체 삭제
   - 다른 GameObject에 컴포넌트로 붙어있다면 컴포넌트만 제거

**확인**:
- Console 창에 `CombatStatusDisplay`에 대한 Missing Reference 경고가 없는지 확인

---

### 3-2. Prefab 정리 (있다면)

**작업**:
1. Project 창에서 `CombatStatusDisplay.prefab` 검색
2. 발견되면 삭제 또는 백업

**경로 추정**:
- `Assets/Prefab/CombatScene/CombatStatusDisplay.prefab`

---

## 📍 Phase 4: Scene 전환 시스템 준비 (선택사항)

**현재 상태**:
- PersistentUIScene과 CombatScene이 분리되어 있음
- 실행 시 두 Scene을 함께 로드해야 함

**향후 작업** (나중에 진행):
1. CoreSystemScene 생성
2. SceneTransitionManager 구현
3. PersistentUIScene을 Additive로 자동 로드
4. CombatScene을 Additive로 로드/언로드

**현재 테스트 방법**:
1. `File > Build Settings` 열기
2. `Scenes In Build`에 다음 Scene 추가:
   - PersistentUIScene
   - ProtoType (또는 CombatScene)
3. **방법 A**: 스크립트로 수동 로드
   ```csharp
   // CombatManager의 Awake() 또는 Start()에 추가
   SceneManager.LoadScene("PersistentUIScene", LoadSceneMode.Additive);
   ```
4. **방법 B**: Hierarchy에서 수동 로드
   - 재생 중 `Hierarchy > 우클릭 > Load Scene Additively`
   - PersistentUIScene 선택

---

## 📍 Phase 5: 테스트

### 5-1. 컴파일 에러 확인

**작업**:
1. Unity Editor로 돌아가기
2. Console 창 확인
3. 컴파일 에러가 있다면 해결

**예상 에러**:
- `CombatStatusDisplay`를 찾을 수 없음 → 정상 (이미 삭제했음)
- Missing Reference 경고 → UI 연결 확인 필요

---

### 5-2. Play Mode 테스트

**작업**:
1. ProtoType Scene (또는 CombatScene) 실행
2. **PersistentUIScene 수동 로드** (방법 A 또는 B 사용)
3. 전투 시작

**확인 사항**:

#### ✅ CombatHUD (게임 HUD)
- [ ] 턴 타이머가 표시되는가?
- [ ] 턴 타이머 프로그레스 바가 진행되는가?
- [ ] Perfect Timing 가이드가 표시되는가?
- [ ] 입력 프롬프트가 표시되는가? ("입력 대기", "지금이닷!" 등)
- [ ] 플레이어/적 턴에 따라 색상이 변경되는가? (초록/빨강)
- [ ] 전투 종료 시 메시지가 표시되는가?

#### ✅ CombatDebugDisplay (디버그 UI)
- [ ] 플레이어/적 이름이 표시되는가?
- [ ] 플레이어/적 HP, Poise, ATK, DR, Crit이 표시되는가?
- [ ] 스탯 변경 시 UI가 업데이트되는가?
- [ ] 플레이어 히트 결과가 표시되는가? ("Perfect!", "Miss!")
- [ ] 적 히트 결과가 표시되는가?
- [ ] 히트 대결 결과가 표시되는가? ("적중!", "완벽하게 쳐냈다!" 등)
- [ ] 액션 커맨드 이름이 표시되는가?
- [ ] 입력 쿨다운이 표시되는가?

---

### 5-3. 문제 해결

#### ❌ UI가 표시되지 않는 경우

**원인 1**: Scene이 로드되지 않음
- **해결**: PersistentUIScene을 Additive로 로드했는지 확인

**원인 2**: 컴포넌트에 UI가 연결되지 않음
- **해결**: Inspector에서 필드 연결 확인 (None이 아니어야 함)

**원인 3**: Instance가 null
- **해결**: CombatDebugDisplay와 CombatHUD GameObject가 Scene에 존재하는지 확인
- Console에서 `Instance is null` 경고 확인

#### ❌ 컴파일 에러

**Missing Type Reference**:
```
error CS0246: The type or namespace name 'CombatStatusDisplay' could not be found
```
- **원인**: 코드 수정이 완료되지 않음
- **해결**: 프로젝트를 다시 컴파일하거나 Unity Editor 재시작

#### ❌ Missing Component

```
The referenced script on this Behaviour (Game Object 'XXX') is missing!
```
- **원인**: 기존 CombatStatusDisplay 컴포넌트가 남아있음
- **해결**: 해당 GameObject에서 컴포넌트 제거

---

## 📊 오브젝트 연결 체크리스트

### CombatDebugDisplay (총 19개 필드)

#### Player Debug UI (9개)
- [ ] Player Name
- [ ] Player HP
- [ ] Player Poise
- [ ] Player ATK
- [ ] Player DR
- [ ] Player Crit
- [ ] Player Action Command Name
- [ ] Player Action Input Cooldown
- [ ] Player Hit Result Container

#### Enemy Debug UI (9개)
- [ ] Enemy Name
- [ ] Enemy HP
- [ ] Enemy Poise
- [ ] Enemy ATK
- [ ] Enemy DR
- [ ] Enemy Crit
- [ ] Enemy Action Command Name
- [ ] Enemy Action Input Cooldown
- [ ] Enemy Hit Result Container

#### Combat Log (3개)
- [ ] Action Progress
- [ ] Turn Result Container
- [ ] Result Line Prefab

---

### CombatHUD (총 8개 필드)

#### Turn Indicator (2개)
- [ ] Player Turn Container
- [ ] Enemy Turn Container

#### Turn Timer (3개)
- [ ] Turn Label
- [ ] Turn Timer Progress Bar
- [ ] Turn Timer Progress Bar Background

#### Perfect Timing Guide (2개)
- [ ] Perfect Timing Guide Prefab
- [ ] Guide Container

#### Input & Battle End (2개)
- [ ] Input Prompt Text
- [ ] Battle End Message

---

## 🗂️ 최종 Scene 구조

### PersistentUIScene
```
PersistentUIScene
├── Canvus_Debug (DebugUI Prefab)
│   ├── DebugInfoPanel
│   │   ├── Player_DebugInfo
│   │   │   ├── Text_PlayerName
│   │   │   ├── Text_PlayerHP
│   │   │   ├── ... (기타 플레이어 디버그 UI)
│   │   │   └── PlayerHitResultContainer
│   │   ├── Enemy_DebugInfo
│   │   │   ├── Text_EnemyName
│   │   │   ├── Text_EnemyHP
│   │   │   ├── ... (기타 적 디버그 UI)
│   │   │   └── EnemyHitResultContainer
│   │   ├── Text_ActionProgress
│   │   └── TurnResultContainer
│   ├── Button_DebugMode
│   ├── Button_CombatInfo
│   └── Button_BTInfo
└── CombatDebugDisplay (GameObject)
    └── CombatDebugDisplay (Script) ← 19개 필드 연결 필요
```

---

### CombatScene (또는 ProtoType)
```
CombatScene
├── Main Camera
├── Directional Light
├── Player
├── Enemy
├── CombatManager
├── CombatCharacterManager
├── ProjectileManager
├── FloatingTextManager
├── Canvas_HUD
│   ├── Panel
│   │   ├── Panel_TurnInfo
│   │   │   ├── Text_TurnLabel
│   │   │   ├── Image_TurnTimerBar
│   │   │   ├── Image_TurnTimerBarBG
│   │   │   └── GuideContainer
│   │   ├── Panel_InputPrompt
│   │   │   └── Text_InputPrompt
│   │   └── Panel_BattleEnd
│   │       └── Text_BattleEndMessage
│   └── Panel_HP
│       ├── PlayerHPPanel (Image - 턴 표시용)
│       └── EnemyHPPanel (Image - 턴 표시용)
└── CombatHUD (GameObject)
    └── CombatHUD (Script) ← 8개 필드 연결 필요
```

---

## 🎯 추가 작업 (선택사항)

### 1. Result Line Prefab 생성 (없다면)

**목적**: 디버그 로그 라인 표시용 Prefab

**작업**:
1. Hierarchy에서 `Canvus_Debug/DebugInfoPanel` 선택
2. 우클릭 > `UI > Text - TextMeshPro` 생성
3. 이름: `ResultLinePrefab`
4. Text 설정:
   - Font Size: 14
   - Color: White
   - Alignment: Left, Top
5. Project 창의 `Assets/Prefab/UI/`로 드래그하여 Prefab 생성
6. Hierarchy에서 원본 삭제
7. `CombatDebugDisplay`의 `Result Line Prefab` 필드에 연결

---

### 2. Perfect Timing Guide Prefab 확인

**확인**:
- `Assets/Prefab/UI/PerfectTimingGuide.prefab` 존재 확인
- 존재한다면 `CombatHUD`의 `Perfect Timing Guide Prefab` 필드에 연결

**없다면**:
- Perfect Timing Guide는 이미 구현되어 있어야 함
- 기존 문서 참조: `Docs/Design/PerfectTimingGuide_구현_요약.md`

---

### 3. Canvas Sort Order 설정

**목적**: PersistentUIScene의 Canvas가 CombatScene 위에 표시되도록 설정

**작업**:
1. `PersistentUIScene` 열기
2. `Canvus_Debug` (Canvas) 선택
3. Inspector에서 `Canvas` 컴포넌트 찾기
4. `Sort Order` 설정:
   - Canvus_Debug: **1000** (최상위)

5. `CombatScene` 열기
6. `Canvas_HUD` (Canvas) 선택
7. Inspector에서 `Canvas` 컴포넌트 찾기
8. `Sort Order` 설정:
   - Canvas_HUD: **500** (중간)

**결과**:
- DebugUI가 HUD 위에 표시됨

---

## 📝 작업 완료 후 확인 사항

### ✅ 파일 구조
- [ ] `Assets/Script/UI/CombatDebugDisplay.cs` 존재
- [ ] `Assets/Script/UI/CombatHUD.cs` 존재
- [ ] `Assets/Script/UI/CombatStatusDisplay.cs` 삭제됨
- [ ] `Assets/Prefab/UI/DebugUI.prefab` 생성 (선택사항)

### ✅ Scene 구조
- [ ] `PersistentUIScene.unity` 존재 (`Assets/Scenes/UI/`)
- [ ] `PersistentUIScene`에 `CombatDebugDisplay` GameObject 존재
- [ ] `CombatScene`에 `CombatHUD` GameObject 존재
- [ ] 기존 `CombatStatusDisplay` GameObject 제거

### ✅ 컴포넌트 연결
- [ ] `CombatDebugDisplay` 컴포넌트의 19개 필드 모두 연결
- [ ] `CombatHUD` 컴포넌트의 8개 필드 모두 연결

### ✅ 테스트 통과
- [ ] 컴파일 에러 없음
- [ ] Play Mode에서 전투 실행 가능
- [ ] 게임 HUD 정상 작동 (턴 타이머, Perfect Timing 가이드 등)
- [ ] 디버그 UI 정상 작동 (스탯 표시, 히트 결과 등)

---

## 🚨 문제 발생 시

### Console 로그 확인

**CombatHUD 관련**:
```
[CombatHUD] 중복 인스턴스 감지!
```
→ CombatScene에 CombatHUD GameObject가 여러 개 있음 (하나만 남기기)

```
NullReferenceException: Object reference not set to an instance of an object
CombatHUD.UpdateTurnInfo()
```
→ CombatHUD 컴포넌트의 필드가 연결되지 않음 (Inspector 확인)

**CombatDebugDisplay 관련**:
```
[CombatDebugDisplay] 중복 인스턴스 감지!
```
→ PersistentUIScene에 CombatDebugDisplay GameObject가 여러 개 있음 (하나만 남기기)

```
NullReferenceException: Object reference not set to an instance of an object
CombatDebugDisplay.UpdatePlayerStatus()
```
→ CombatDebugDisplay 컴포넌트의 필드가 연결되지 않음 (Inspector 확인)

---

## 📞 도움이 필요하면

1. ProtoType Scene의 실제 Hierarchy 구조를 스크린샷으로 공유
2. Inspector에서 연결된 필드들의 스크린샷 공유
3. Console 창의 에러 메시지 전체 복사

---

## 🎉 작업 완료!

모든 작업이 완료되면 다음 단계로 진행할 수 있습니다:
1. Scene 계층 구조 구현 (CoreSystemScene, TestScene 생성)
2. Scene 전환 시스템 구현
3. MainMenuManager 및 ItemEvents를 PersistentUIScene으로 이동

---

**작성자**: AI Assistant  
**최종 수정일**: 2025-11-04










