# Scene 전환 시스템 Unity 에디터 설정 가이드

**작성일**: 2025-11-05  
**목적**: Scene 전환 시스템 코드 구현 후 Unity 에디터에서 수동으로 설정해야 할 작업 가이드

---

## 📋 목차

1. [PersistentUIScene 설정](#1-persistentuiscene-설정)
2. [CoreSystemScene 설정](#2-coresystemscene-설정)
3. [TitleScene 생성](#3-titlescene-생성)
4. [TestScene 설정](#4-testscene-설정)
5. [ResultScene 생성](#5-resultscene-생성)
6. [통합 테스트](#6-통합-테스트)

---

## 1. PersistentUIScene 설정

### FadeCanvas 추가

**Scene**: `02.PersistentUIScene.unity`

1. **FadeCanvas GameObject 생성**
   ```
   Hierarchy 우클릭 → UI → Canvas
   이름: FadeCanvas
   ```

2. **FadeCanvas 설정**
   - Canvas 컴포넌트:
     - Render Mode: `Screen Space - Overlay`
     - Sort Order: `900`
   - CanvasGroup 컴포넌트 추가:
     - `Add Component → Canvas Group`
   - FadeController 컴포넌트 추가:
     - `Add Component → Fade Controller`

3. **FadeImage GameObject 생성**
   ```
   FadeCanvas 우클릭 → UI → Image
   이름: FadeImage
   ```

4. **FadeImage 설정**
   - RectTransform:
     - Anchor Preset: `Stretch` (전체 화면)
     - Left: 0, Top: 0, Right: 0, Bottom: 0
   - Image 컴포넌트:
     - Source Image: `None` (단색 사용)
     - Color: `검은색 (R:0, G:0, B:0, A:255)`
     - Raycast Target: `✓` (체크)

5. **FadeController 참조 연결**
   - FadeCanvas 선택
   - FadeController 컴포넌트:
     - Fade Image: `FadeImage` 드래그 앤 드롭
     - Canvas Group: `FadeCanvas의 CanvasGroup` 드래그 앤 드롭
     - Default Fade Duration: `0.5`
     - Enable Debug Log: `✓` (체크)

**완료 확인:**
- FadeCanvas가 가장 위에 렌더링되는지 확인 (Sort Order: 900)
- FadeImage가 전체 화면을 덮는지 확인

---

## 2. CoreSystemScene 설정

### SceneTransitionManager 추가

**Scene**: `01.CoreSystemScene.unity`

1. **빈 GameObject 생성**
   ```
   Hierarchy 우클릭 → Create Empty
   이름: SceneTransitionManager
   ```

2. **SceneTransitionManager 컴포넌트 추가**
   - `Add Component → Scene Transition Manager`

3. **설정 확인**
   - Default Fade Out Duration: `0.5`
   - Default Fade In Duration: `0.5`
   - Enable Debug Log: `✓` (체크)

**완료 확인:**
- CoreSystemScene에 SceneTransitionManager GameObject가 존재
- SceneTransitionManager.cs 컴포넌트가 추가됨

---

### SceneFlowController 추가

**Scene**: `01.CoreSystemScene.unity`

1. **빈 GameObject 생성**
   ```
   Hierarchy 우클릭 → Create Empty
   이름: SceneFlowController
   ```

2. **SceneFlowController 컴포넌트 추가**
   - `Add Component → Scene Flow Controller`

3. **설정**
   - Combat Scene Asset: `03.CombatScene.unity` 드래그
   - Title Scene Asset: `05.TitleScene.unity` 드래그
   - Test Scene Asset: `00.TestScene.unity` 드래그
   - Default Player Id: `"Player"`
   - Default Enemy Id: `"Test_Enemy1"`
   - Enable Debug Log: `✓` (체크)

**완료 확인:**
- CoreSystemScene에 SceneFlowController GameObject가 존재
- SceneFlowController.cs 컴포넌트가 추가됨

---

## 3. TitleScene 생성

### Scene 파일 생성

1. **새 Scene 생성**
   ```
   File → New Scene
   저장: Assets/Scenes/05.TitleScene.unity
   ```

2. **Canvas 추가**
   ```
   Hierarchy 우클릭 → UI → Canvas
   이름: TitleCanvas
   ```

3. **Canvas 설정**
   - Canvas 컴포넌트:
     - Render Mode: `Screen Space - Overlay`
     - Sort Order: `0`
   - Canvas Scaler 추가:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`

### UI 요소 추가

4. **Title Panel 추가**
   ```
   TitleCanvas 우클릭 → UI → Panel
   이름: TitlePanel
   ```

5. **Title Text 추가**
   ```
   TitlePanel 우클릭 → UI → Text - TextMeshPro
   이름: TitleText
   ```
   - Text: `"BladeAction"` (또는 원하는 게임 제목)
   - Font Size: `72`
   - Alignment: 중앙 정렬
   - Color: 원하는 색상

6. **Start Button 추가**
   ```
   TitlePanel 우클릭 → UI → Button - TextMeshPro
   이름: StartGameButton
   ```
   - 하위 Text: `"게임 시작"`
   - 위치: 화면 중앙

7. **Exit Button 추가**
   ```
   TitlePanel 우클릭 → UI → Button - TextMeshPro
   이름: ExitGameButton
   ```
   - 하위 Text: `"종료"`
   - 위치: Start Button 아래

8. **Version Text 추가**
   ```
   TitlePanel 우클릭 → UI → Text - TextMeshPro
   이름: VersionText
   ```
   - Text: `"v0.1.0"`
   - Font Size: `18`
   - Alignment: 우측 하단
   - Color: 회색

### TitleSceneManager 추가

9. **빈 GameObject 생성**
   ```
   Hierarchy 우클릭 → Create Empty
   이름: TitleSceneManager
   ```

10. **TitleSceneManager 컴포넌트 추가**
    - `Add Component → Title Scene Manager`

11. **참조 연결**
    - Start Game Button: `StartGameButton` 드래그
    - Exit Game Button: `ExitGameButton` 드래그
    - Version Text: `VersionText` 드래그
    - Game Version: `"v0.1.0"`
    - Enable Debug Log: `✓` (체크)

**Note**: Scene 전환 경로는 SceneFlowController에서 관리하므로 여기서는 설정하지 않습니다.

**완료 확인:**
- TitleScene이 정상적으로 표시되는지 확인
- 버튼들이 정상적으로 배치되어 있는지 확인

---

## 4. TestScene 설정

**Note**: TestScene(00.TestScene)은 이미 존재하는 Scene입니다. 이 Scene에 Scene 전환 기능을 추가합니다.

### UI 버튼 추가 (필요시)

TestScene에 다음 버튼들이 없다면 추가:

1. **Canvas 확인**
   - TestScene에 Canvas가 없으면 생성
   ```
   Hierarchy 우클릭 → UI → Canvas
   ```

2. **Start Combat Button 추가**
   ```
   Canvas 우클릭 → UI → Button - TextMeshPro
   이름: StartCombatButton
   하위 Text: "전투 시작"
   ```

3. **Return To Title Button 추가** (선택)
   ```
   Canvas 우클릭 → UI → Button - TextMeshPro
   이름: ReturnToTitleButton
   하위 Text: "타이틀로"
   ```

### TestSceneManager 추가

**중요**: SceneFlowController는 다른 Scene(CoreSystemScene)에 있으므로 **코드로만 연결 가능**합니다.

1. **빈 GameObject 생성**
   ```
   Hierarchy 우클릭 → Create Empty
   이름: TestSceneManager
   ```

2. **TestSceneManager 컴포넌트 추가**
   - `Add Component → Test Scene Manager`

3. **참조 연결**
   - Start Combat Button: `StartCombatButton` 드래그 (같은 Scene이므로 가능)
   - Return To Title Button: `ReturnToTitleButton` 드래그
   - Enable Debug Log: `✓` (체크)

**완료 확인:**
- TestScene에 TestSceneManager GameObject가 존재
- 버튼들이 TestSceneManager에 연결되어 있는지 확인
- Play 모드에서 "전투 시작" 버튼 클릭 시 CombatScene으로 전환되는지 확인

### 추가 기능 (선택)

TestScene의 다른 스크립트에서 SceneFlowController를 호출하려면:

```csharp
// 전투 시작 (기본 ID)
SceneFlowController.Instance.StartCombat();

// 전투 시작 (커스텀 ID)
SceneFlowController.Instance.StartCombat("CustomPlayer", "BossEnemy");

// 타이틀로 복귀
SceneFlowController.Instance.ReturnToTitle();
```

---

## 5. ResultScene 생성

### Scene 파일 생성

1. **새 Scene 생성**
   ```
   File → New Scene
   저장: Assets/Scenes/07.ResultScene.unity
   ```

2. **Canvas 추가**
   ```
   Hierarchy 우클릭 → UI → Canvas
   이름: ResultCanvas
   ```

3. **Canvas 설정**
   - Canvas 컴포넌트:
     - Render Mode: `Screen Space - Overlay`
     - Sort Order: `0`
   - Canvas Scaler 추가:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`

### UI 요소 추가

4. **Result Panel 추가**
   ```
   ResultCanvas 우클릭 → UI → Panel
   이름: ResultPanel
   ```

5. **Result Title Text 추가**
   ```
   ResultPanel 우클릭 → UI → Text - TextMeshPro
   이름: ResultTitleText
   ```
   - Text: `"승리!" / "패배..."`
   - Font Size: `72`
   - Alignment: 중앙 상단
   - Color: 노란색

6. **Victory Panel 추가**
   ```
   ResultPanel 우클릭 → UI → Panel
   이름: VictoryPanel
   ```
   - 배경색: 약간 투명한 노란색
   - 초기 상태: 비활성화 (Inactive)

7. **Defeat Panel 추가**
   ```
   ResultPanel 우클릭 → UI → Panel
   이름: DefeatPanel
   ```
   - 배경색: 약간 투명한 빨간색
   - 초기 상태: 비활성화 (Inactive)

8. **Gold Reward Text 추가**
   ```
   ResultPanel 우클릭 → UI → Text - TextMeshPro
   이름: GoldRewardText
   ```
   - Text: `"골드: +100"`
   - Font Size: `36`
   - Alignment: 중앙

9. **Exp Reward Text 추가**
   ```
   ResultPanel 우클릭 → UI → Text - TextMeshPro
   이름: ExpRewardText
   ```
   - Text: `"경험치: +50"`
   - Font Size: `36`
   - Alignment: 중앙

10. **Continue Button 추가**
    ```
    ResultPanel 우클릭 → UI → Button - TextMeshPro
    이름: ContinueButton
    ```
    - 하위 Text: `"계속"`

11. **Return To Title Button 추가**
    ```
    ResultPanel 우클릭 → UI → Button - TextMeshPro
    이름: ReturnToTitleButton
    ```
    - 하위 Text: `"타이틀로"`

### ResultSceneManager 추가

12. **빈 GameObject 생성**
    ```
    Hierarchy 우클릭 → Create Empty
    이름: ResultSceneManager
    ```

13. **ResultSceneManager 컴포넌트 추가**
    - `Add Component → Result Scene Manager`

14. **참조 연결**
    - Result Title Text: `ResultTitleText` 드래그
    - Gold Reward Text: `GoldRewardText` 드래그
    - Exp Reward Text: `ExpRewardText` 드래그
    - Victory Panel: `VictoryPanel` 드래그
    - Defeat Panel: `DefeatPanel` 드래그
    - Continue Button: `ContinueButton` 드래그
    - Return To Title Button: `ReturnToTitleButton` 드래그
    - Enable Debug Log: `✓` (체크)

**Note**: Scene 전환 경로는 SceneFlowController에서 관리하므로 여기서는 설정하지 않습니다.

**완료 확인:**
- ResultScene이 정상적으로 표시되는지 확인
- 버튼들이 정상적으로 배치되어 있는지 확인

---

## 6. 통합 테스트

### Build Settings 설정

1. **Build Settings 열기**
   ```
   File → Build Settings
   ```

2. **Scene 추가**
   - `Add Open Scenes` 클릭하거나 다음 Scene들을 드래그:
     1. `01.CoreSystemScene`
     2. `02.PersistentUIScene`
     3. `03.CombatScene`
     4. `05.TitleScene`
     5. `00.TestScene`
     6. `07.ResultScene`

3. **Scene 순서 확인**
   - `01.CoreSystemScene`이 가장 위(Index 0)에 있어야 함

### 전체 플로우 테스트

**테스트 시나리오:**

1. **게임 시작**
   - `01.CoreSystemScene` 실행 (Play 버튼)
   - ✅ PersistentUIScene 자동 로드 확인
   - ✅ TitleScene 자동 로드 확인
   - ✅ Fade In 효과 확인

2. **타이틀 → 테스트**
   - "게임 시작" 버튼 클릭
   - ✅ Fade Out 효과 확인
   - ✅ TitleScene 언로드 확인
   - ✅ TestScene 로드 확인
   - ✅ Fade In 효과 확인

3. **테스트 → 전투**
   - "전투 시작" 버튼 클릭
   - ✅ Fade Out 효과 확인
   - ✅ TestScene 언로드 확인
   - ✅ CombatScene 로드 확인
   - ✅ Fade In 효과 확인
   - ✅ 전투 자동 시작 확인

4. **전투 진행**
   - 전투를 진행하여 승리 또는 패배
   - ✅ 전투 종료 후 2초 대기 확인
   - ✅ Fade Out 효과 확인
   - ✅ CombatScene 언로드 확인
   - ✅ ResultScene 로드 확인
   - ✅ Fade In 효과 확인

5. **결과 화면**
   - ✅ 승리/패배 표시 확인
   - ✅ 골드/경험치 보상 표시 확인
   - ✅ VictoryPanel 또는 DefeatPanel 활성화 확인

6. **결과 → 테스트**
   - "계속" 버튼 클릭
   - ✅ Fade Out 효과 확인
   - ✅ ResultScene 언로드 확인
   - ✅ TestScene 로드 확인
   - ✅ Fade In 효과 확인

7. **테스트 → 타이틀**
   - "타이틀로" 버튼 클릭
   - ✅ Fade Out 효과 확인
   - ✅ TestScene 언로드 확인
   - ✅ TitleScene 로드 확인
   - ✅ Fade In 효과 확인

8. **게임 종료**
   - "종료" 버튼 클릭
   - ✅ 게임 종료 확인 (에디터에서는 Play 모드 종료)

### 디버그 확인 사항

**Console 로그 확인:**
- `[CoreSystemInitializer]` 로그: Scene 로딩 순서 확인
- `[SceneTransitionManager]` 로그: Scene 전환 과정 확인
- `[SceneFlowController]` 로그: Scene 흐름 제어 확인
- `[FadeController]` 로그: Fade 효과 동작 확인
- `[TitleSceneManager]` 로그: 버튼 클릭 이벤트 확인
- `[CombatManager]` 로그: 전투 종료 및 Scene 전환 확인
- `[ResultSceneManager]` 로그: 결과 표시 및 보상 적용 확인

**Hierarchy 확인:**
- Scene 전환 시 이전 Content Scene이 사라지는지 확인
- CoreSystemScene과 PersistentUIScene은 항상 유지되는지 확인

**메모리 확인:**
- Profiler 창에서 메모리 누수 여부 확인
- Scene 언로드 후 메모리가 정리되는지 확인

---

## 🚨 문제 해결

### Fade 효과가 안 보일 때
- FadeCanvas의 Sort Order가 900인지 확인
- FadeImage가 전체 화면을 덮는지 확인
- CanvasGroup.alpha 값이 변하는지 Console 로그 확인

### Scene 전환이 안 될 때
- SceneTransitionManager가 CoreSystemScene에 있는지 확인
- Scene 이름이 정확한지 확인 (대소문자 구분)
- Build Settings에 해당 Scene이 추가되어 있는지 확인

### 버튼이 작동하지 않을 때
- Button 컴포넌트의 OnClick 이벤트가 자동으로 연결되는지 확인
- Inspector에서 Manager의 Button 참조가 연결되어 있는지 확인
- EventSystem이 Scene에 존재하는지 확인 (CoreSystemScene에 있음)

### 전투 결과가 표시되지 않을 때
- CombatManager.TransitionToResultScene()이 호출되는지 Console 로그 확인
- ResultSceneManager.LastBattleResult가 null이 아닌지 확인
- BattleResult의 isVictory, goldReward, expReward 값 확인

---

## 📝 최종 체크리스트

### Scene 파일
- [ ] 05.TitleScene.unity 생성 및 설정 완료
- [ ] 00.TestScene.unity 설정 완료 (TestSceneManager 추가)
- [ ] 07.ResultScene.unity 생성 및 설정 완료

### CoreSystemScene
- [ ] SceneTransitionManager GameObject 추가
- [ ] SceneTransitionManager.cs 컴포넌트 연결
- [ ] SceneFlowController GameObject 추가
- [ ] SceneFlowController.cs 컴포넌트 연결

### PersistentUIScene
- [ ] FadeCanvas 추가 (Sort Order: 900)
- [ ] FadeImage 추가 (전체 화면)
- [ ] FadeController.cs 컴포넌트 연결 및 참조 설정

### Build Settings
- [ ] 모든 Scene이 Build Settings에 추가됨
- [ ] 01.CoreSystemScene이 Index 0

### 통합 테스트
- [ ] 전체 게임 플로우 테스트 완료
- [ ] Scene 전환 시 Fade 효과 동작 확인
- [ ] 전투 종료 후 ResultScene 전환 확인
- [ ] 보상 적용 확인
- [ ] 메모리 누수 없음 확인

---

**작성자**: AI Assistant  
**검토자**: (검토 후 기입)

