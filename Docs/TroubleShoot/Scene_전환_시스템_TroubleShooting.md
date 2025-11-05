# Scene 전환 시스템 TroubleShooting

**작성일**: 2025-11-05  
**목적**: Scene 전환 시스템 및 관련 전투 시스템 버그 추적

---

## 📊 버그 통계

| 상태 | 개수 |
|------|------|
| 총 버그 | 9 |
| 🔴 해결 중 | 2 |
| ✅ 완료 | 7 |
| ⏸️ 보류 | 0 |

---

## 📋 버그 리스트

### 🔴 해결 중

- 🔴 [이슈 #8: TestScene에서 인벤토리/검술설정 UI가 화면에 표시되지 않음](#이슈-8-testscene에서-인벤토리검술설정-ui가-화면에-표시되지-않음)
- 🔴 [이슈 #9: 검술 설정 UI에 아무것도 표시되지 않음](#이슈-9-검술-설정-ui에-아무것도-표시되지-않음)

### ✅ 완료

- ✅ [이슈 #1: 전투 시작 후 TestScene이 언로드되지 않음](#이슈-1-전투-시작-후-testscene이-언로드되지-않음)
- ✅ [이슈 #2: 전투 중 CombatHUD가 화면에 표시되지 않음](#이슈-2-전투-중-combathud가-화면에-표시되지-않음)
- ✅ [이슈 #3: 전투 종료 후 ResultScene으로 전환되지 않음](#이슈-3-전투-종료-후-resultscene으로-전환되지-않음)
- ✅ [이슈 #4: 공격 턴에서 공격 애니메이션이 중복 재생됨](#이슈-4-공격-턴에서-공격-애니메이션이-중복-재생됨)
- ✅ [이슈 #5: HP Bar 가로 사이즈 비율이 제대로 반영되지 않음](#이슈-5-hp-bar-가로-사이즈-비율이-제대로-반영되지-않음)
- ✅ [이슈 #6: FadeController를 찾을 수 없음 (Fade 효과 미표시)](#이슈-6-fadecontroller를-찾을-수-없음-fade-효과-미표시)
- ✅ [이슈 #7: 공격/방어 턴이 순차적으로 순환되지 않음](#이슈-7-공격방어-턴이-순차적으로-순환되지-않음)

---

## 🔴 해결 중인 버그 상세

### 이슈 #8: TestScene에서 인벤토리/검술설정 UI가 화면에 표시되지 않음

**발생 일시**: 2025-11-05  
**상태**: 🔴 해결 중  
**우선순위**: 🔥 높음  
**카테고리**: UI 시스템

**증상**:
- TestScene에서 인벤토리, 검술설정 UI를 열면 GameObject는 활성화됨
- 내부 로직은 정상 동작함 (활성화 상태 확인됨)
- 하지만 화면에는 UI가 표시되지 않음
- TestScene의 Canvas가 PersistentUIScene의 UI를 덮어쓰는 것으로 추정

**재현 방법**:
1. TestScene에서 인벤토리 또는 검술설정 UI 열기
2. Hierarchy에서 GameObject 활성화 확인
3. 화면에는 보이지 않음

**예상 원인**:
- **Canvas Sort Order 문제**
- TestScene Canvas의 Sort Order가 PersistentUIScene의 MainMenuManager Canvas보다 높음
- 결과: TestScene Canvas가 위에 그려져서 PersistentUIScene UI를 가림

**해결 방법**:
- TestScene Canvas의 Sort Order 확인
- PersistentUIScene MainMenuManager Canvas의 Sort Order 확인
- PersistentUIScene의 UI Canvas Sort Order를 높게 조정

**관련 파일**:
- `Assets/Scenes/00.TestScene.unity`
- `Assets/Scenes/02.PersistentUIScene.unity`

---

### 이슈 #9: 검술 설정 UI에 아무것도 표시되지 않음

**발생 일시**: 2025-11-05  
**상태**: 🔴 해결 중  
**우선순위**: 🔥 높음  
**카테고리**: 데이터 시스템

**증상**:
- TestScene에서 인벤토리는 접근 가능
- 검술 설정 UI를 열면 "아무것도 없음" 표시
- 플레이어가 장비할 수 있는 검술 목록이 비어있음

**재현 방법**:
1. TestScene에서 검술 설정 UI 열기
2. 검술 목록이 비어있음

**예상 원인**:
- PlayerCharacterManager의 ActionCommand 데이터 초기화 안 됨
- ActionCommandDatabase 연결 문제
- Player Character의 AvailableCommands가 비어있음

**해결 방법**:
- PlayerCharacterManager 초기화 로직 확인
- Player Character의 ActionCommand 데이터 확인
- 조사 필요

**관련 파일**:
- `Assets/Script/Manager/PlayerCharacterManager.cs`
- `Assets/Script/Manager/CharacterDatabaseManager.cs`

---

## ✅ 해결 완료된 버그 상세

### 이슈 #1: 전투 시작 후 TestScene이 언로드되지 않음

**발생 일시**: 2025-11-05  
**상태**: ✅ 완료  
**우선순위**: 🔥 높음  
**카테고리**: Scene 전환 시스템

**증상**:
- TestScene에서 "전투 시작" 버튼 클릭
- 전투는 진행되지만 화면은 TestScene 그대로
- TestScene이 언로드되지 않음

**원인**:
- CoreSystemInitializer가 초기 Scene 로드 후 SceneTransitionManager에 등록하지 않음
- currentContentScene이 비어있어서 언로드할 Scene을 모름

**해결 방법**:
- CoreSystemInitializer에 `SceneTransitionManager.SetCurrentContentScene()` 호출 추가

---

### 이슈 #2: 전투 중 CombatHUD가 화면에 표시되지 않음

**발생 일시**: 2025-11-05  
**상태**: ✅ 완료  
**우선순위**: 🔥 높음  
**카테고리**: Scene 전환 시스템

**증상**:
- 전투가 시작되면 캐릭터는 보임
- 하지만 CombatHUD(턴 정보, 커맨드 UI 등)가 화면에 표시되지 않음

**원인**:
- Canvas_CombatHUD가 "Screen Space - Camera" 모드
- 공용 Camera로 통합하면서 Render Camera 참조가 끊김

**해결 방법**:
- Canvas Render Mode를 "Screen Space - Overlay"로 변경

---

### 이슈 #3: 전투 종료 후 ResultScene으로 전환되지 않음

**발생 일시**: 2025-11-05  
**상태**: ✅ 완료  
**우선순위**: 🔥 높음  
**카테고리**: Scene 전환 시스템

**증상**:
- 전투 종료 후 ResultScene이 로드되지 않음
- Console 오류: "Scene '07.ResultScene' couldn't be loaded..."

**원인**:
- CombatManager에서 Scene 이름 하드코딩 (`"07.ResultScene"`)
- Scene 이름 변경 시 반영 안 됨

**해결 방법**:
- SceneFlowController에 `GoToResultScene()` 메서드 추가
- CombatManager에서 `SceneFlowController.Instance.GoToResultScene()` 호출로 변경
- Scene 이름 하드코딩 완전히 제거

---

### 이슈 #6: FadeController를 찾을 수 없음 (Fade 효과 미표시)

**발생 일시**: 2025-11-05  
**상태**: ✅ 완료  
**우선순위**: 🔥 높음  
**카테고리**: Scene 전환 시스템

**증상**:
- Scene 전환 시 Fade 효과가 표시되지 않음
- Console 로그: "FadeController를 찾을 수 없습니다"

**원인**:
- FadeImage GameObject가 활성화/비활성화 방식으로 제어하기로 했으나
- FadeController 로직이 Alpha 방식으로만 구현되어 있음

**해결 방법**:
1. FadeController 로직 수정:
   - Fade 시작 시 FadeImage 활성화
   - Fade In 완료 시 FadeImage 비활성화
   - Alpha와 GameObject 활성화를 함께 사용
2. 에디터 설정:
   - FadeCanvas: 활성화
   - FadeImage: 비활성화

---

### 이슈 #7: 공격/방어 턴이 순차적으로 순환되지 않음

**발생 일시**: 2025-11-05  
**상태**: ✅ 완료  
**우선순위**: 🔥 높음  
**카테고리**: 전투 시스템

**증상**:
- 공격 턴, 방어 턴이 순차적으로 순환되지 않음
- 한 쪽이 공격을 두 번 연속으로 하는 경우가 발생
- 턴 타이머가 갑자기 처음부터 다시 도는 경우 발생

**원인**:
- **CombatManager의 자동 시작 + 외부 호출로 RunCombat() 코루틴이 2번 실행됨**
- Start() → WaitForManagersAndStartBattle() → StartBattle() → RunCombat() (1번째)
- SceneFlowController → StartBattle() → RunCombat() (2번째)
- 두 개의 RunCombat() 코루틴이 동시에 턴을 진행하여 순서가 꼬임

**해결 방법**:
- CombatManager.Start()에서 자동 시작 로직 제거
- WaitForManagersAndStartBattle(), StartBattleTest() 메서드 제거
- 외부(SceneFlowController)에서만 StartBattle() 호출하도록 변경

**부수 효과**:
- 이슈 #4(애니메이션 중복 재생)도 함께 해결됨
- 이슈 #5(HP Bar 비율)도 함께 해결됨

---

### 이슈 #4: 공격 턴에서 공격 애니메이션이 중복 재생됨

**발생 일시**: 2025-11-05  
**상태**: ✅ 완료  
**우선순위**: ⚠️ 중간  
**카테고리**: 전투 시스템

**증상**:
- 공격 턴에서 공격 애니메이션이 여러 번 재생되는 경우가 있음

**원인**:
- 이슈 #7과 동일 (RunCombat() 코루틴 중복 실행)

**해결 방법**:
- 이슈 #7 해결로 함께 해결됨

---

### 이슈 #5: HP Bar 가로 사이즈 비율이 제대로 반영되지 않음

**발생 일시**: 2025-11-05  
**상태**: ✅ 완료  
**우선순위**: ⚠️ 중간  
**카테고리**: 전투 시스템

**증상**:
- HP Bar의 fillAmount 또는 가로 사이즈가 실제 HP 비율과 맞지 않음

**원인**:
- 이슈 #7과 동일 (RunCombat() 코루틴 중복 실행으로 UI 업데이트 꼬임)

**해결 방법**:
- 이슈 #7 해결로 함께 해결됨

---

## 💡 자주 발생하는 문제 (FAQ)

### Q1: "SceneFlowController를 찾을 수 없습니다" 오류
**원인**: CoreSystemScene에 SceneFlowController가 없거나 비활성화됨  
**해결**: CoreSystemScene에 SceneFlowController GameObject 및 컴포넌트 확인

### Q2: Fade 효과가 보이지 않음
**원인**: FadeCanvas가 없거나 FadeImage가 설정 안 됨  
**해결**: 
- FadeCanvas Sort Order를 900으로 설정
- FadeCanvas: 활성화, FadeImage: 비활성화로 설정

### Q3: 버튼 클릭이 안됨
**원인**: EventSystem이 없거나 중복됨  
**해결**: CoreSystemScene에만 EventSystem 존재하는지 확인, Content Scene의 EventSystem 삭제

### Q4: Scene 이름 변경 시 로드 실패
**원인**: Scene 이름 하드코딩  
**해결**: SceneAsset 참조 시스템 사용, 모든 하드코딩 제거

### Q5: Canvas UI가 안 보임
**원인**: Canvas가 "Screen Space - Camera" 모드인데 Camera 참조 없음  
**해결**: Canvas를 "Screen Space - Overlay"로 변경

---

## 🔍 디버깅 팁

### Console 로그 필터링
```
[CoreSystemInitializer]
[SceneTransitionManager]
[SceneFlowController]
[FadeController]
[TitleSceneManager]
[TestSceneManager]
[CombatManager]
[ResultSceneManager]
```

### Hierarchy 확인 사항
- CoreSystemScene: 항상 로드됨
- PersistentUIScene: 항상 로드됨
- Content Scene: 하나만 로드되어야 함

### Inspector 디버깅
- SceneTransitionManager: Current Content Scene 확인
- SceneTransitionManager: Is Transitioning 확인
- FadeController: Fade Image 활성화 상태 확인

---

**작성자**: AI Assistant  
**최종 업데이트**: 2025-11-05

