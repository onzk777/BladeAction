# Perfect Timing Guide Prefab 생성 가이드

## 개요
턴 타이머 게이지 바 위에 검술의 Perfect 입력 타이밍 구간을 시각적으로 표시하는 UI 시스템입니다.

## Prefab 구조

### 1. 루트 오브젝트: PerfectTimingGuide

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Left-Center (0, 0.5)
  - Position: (0, 0, 0)
  
- `PerfectTimingGuide` 스크립트
  - Start Marker: (아래 참조)
  - End Marker: (아래 참조)
  - Fill Rect: (아래 참조)
  - Marker Size: 10
  - Fill Height: 20
  - Guide Color: RGBA(1, 0.8, 0, 0.7) - 반투명 노란색

### 2. 자식 오브젝트 1: StartMarker

**컴포넌트:**
- `RectTransform`
  - Anchors: Center (Min: 0.5, 0.5 / Max: 0.5, 0.5)
  - Pivot: Center (0.5, 0.5)
  - Width: 10, Height: 10
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(1, 0.8, 0, 0.7)
  - Image Type: Simple
  - Sprite: UI Sprite Circle (Built-in) 또는 Knob 등 원형 스프라이트
  - Raycast Target: 체크 해제

### 3. 자식 오브젝트 2: EndMarker

**컴포넌트:**
- `RectTransform`
  - Anchors: Center (Min: 0.5, 0.5 / Max: 0.5, 0.5)
  - Pivot: Center (0.5, 0.5)
  - Width: 10, Height: 10
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(1, 0.8, 0, 0.7)
  - Image Type: Simple
  - Sprite: UI Sprite Circle (Built-in) 또는 Knob 등 원형 스프라이트
  - Raycast Target: 체크 해제

### 4. 자식 오브젝트 3: FillRect

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Left-Center (0, 0.5)
  - Width: 100 (기본값, 스크립트에서 변경됨)
  - Height: 20
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(1, 0.8, 0, 0.35) - Fill은 더 투명하게
  - Image Type: Simple
  - Sprite: UI Sprite (Built-in) 또는 단순 사각형 스프라이트
  - Raycast Target: 체크 해제

## Unity Editor에서 생성 단계

### 단계 1: Canvas에서 작업 준비
1. Hierarchy에서 Canvas 선택 (없으면 Create → UI → Canvas)
2. Canvas 설정 확인:
   - Render Mode: Screen Space - Overlay (일반적)
   - Canvas Scaler 설정 확인

### 단계 2: 루트 오브젝트 생성
1. Canvas에서 우클릭 → Create Empty
2. 이름을 "PerfectTimingGuide"로 변경
3. Inspector에서:
   - Add Component → `PerfectTimingGuide` 스크립트 추가
   - RectTransform 설정:
     - Anchors Preset: Left-Center 클릭
     - Pivot: X=0, Y=0.5
     - Pos X=0, Y=0, Z=0

### 단계 3: FillRect 생성
1. PerfectTimingGuide 우클릭 → UI → Image
2. 이름을 "FillRect"로 변경
3. Inspector에서:
   - RectTransform:
     - Anchors Preset: Left-Center
     - Pivot: X=0, Y=0.5
     - Width: 100, Height: 20
     - Pos X=0, Y=0, Z=0
   - Image 컴포넌트:
     - Color: R=1, G=0.8, B=0, A=0.35
     - Source Image: UISprite (기본 흰색 사각형)
     - Raycast Target: 체크 해제

### 단계 4: StartMarker 생성
1. PerfectTimingGuide 우클릭 → UI → Image
2. 이름을 "StartMarker"로 변경
3. Inspector에서:
   - RectTransform:
     - Anchors Preset: Center
     - Pivot: X=0.5, Y=0.5
     - Width: 10, Height: 10
     - Pos X=0, Y=0, Z=0
   - Image 컴포넌트:
     - Color: R=1, G=0.8, B=0, A=0.7
     - Source Image: Knob (또는 원형 스프라이트)
     - Raycast Target: 체크 해제

### 단계 5: EndMarker 생성
1. PerfectTimingGuide 우클릭 → UI → Image
2. 이름을 "EndMarker"로 변경
3. Inspector에서:
   - RectTransform:
     - Anchors Preset: Center
     - Pivot: X=0.5, Y=0.5
     - Width: 10, Height: 10
     - Pos X=0, Y=0, Z=0
   - Image 컴포넌트:
     - Color: R=1, G=0.8, B=0, A=0.7
     - Source Image: Knob (또는 원형 스프라이트)
     - Raycast Target: 체크 해제

### 단계 6: PerfectTimingGuide 스크립트 연결
1. PerfectTimingGuide 오브젝트 선택
2. Inspector에서 `PerfectTimingGuide` 스크립트 컴포넌트 찾기
3. 필드 할당:
   - Start Marker: StartMarker 오브젝트 드래그
   - End Marker: EndMarker 오브젝트 드래그
   - Fill Rect: FillRect 오브젝트 드래그
   - Marker Size: 10
   - Fill Height: 20
   - Guide Color: R=1, G=0.8, B=0, A=0.7

### 단계 7: Prefab으로 저장
1. PerfectTimingGuide 오브젝트를 Project 창의 `Assets/Prefab/` 폴더로 드래그
2. Prefab이 생성되면 Hierarchy의 인스턴스는 삭제해도 됨

## CombatStatusDisplay에 연결

### 단계 1: Scene에서 CombatStatusDisplay 찾기
1. Hierarchy에서 CombatStatusDisplay가 붙어있는 오브젝트 찾기
2. 해당 오브젝트 선택

### 단계 2: Prefab 할당
1. Inspector에서 `Combat Status Display` 컴포넌트 찾기
2. "Perfect Timing Guide" 섹션에서:
   - **Perfect Timing Guide Prefab**: 위에서 만든 PerfectTimingGuide Prefab 드래그
   - **Guide Container**: 
     - 턴 타이머 게이지 바의 부모 오브젝트 또는 같은 레벨의 Container 할당
     - 일반적으로 `turnTimerProgressBar`와 같은 부모를 가진 Container
     - 없으면 새로 만들기: Canvas 우클릭 → Create Empty → "PerfectTimingGuideContainer"

### 단계 3: GuideContainer 설정 (새로 만드는 경우)
1. PerfectTimingGuideContainer 생성
2. RectTransform 설정:
   - Anchors: turnTimerProgressBar와 동일하게 설정
   - Position: turnTimerProgressBar와 동일한 위치
   - Size: turnTimerProgressBar와 동일한 크기
   - Pivot: Left-Center (0, 0.5)
3. turnTimerProgressBar 바로 위에 배치 (같은 부모, 같은 레벨)

## 계층 구조 예시

```
Canvas
├── CombatStatusDisplay (GameObject with CombatStatusDisplay script)
├── TurnTimerPanel
│   ├── TurnTimerProgressBarBackground (Image)
│   ├── TurnTimerProgressBar (Image) ← 게이지 바
│   └── PerfectTimingGuideContainer (RectTransform) ← 여기에 가이드들이 생성됨
```

## 테스트 방법

### 1. 에디터에서 테스트
1. Play Mode 진입
2. 전투 시작
3. 검술 선택 시 턴 타이머 게이지 바 위에 노란색 가이드들이 나타나는지 확인
4. 각 Hit마다 별도의 가이드가 생성되는지 확인
5. Console 창에서 다음 로그 확인:
   ```
   [CombatStatusDisplay] Hit 1 가이드 생성: Start=0.5초, Duration=0.2초, X=100px, Width=40px
   [CombatStatusDisplay] 3개의 Perfect Timing 가이드 생성 완료
   ```

### 2. 비주얼 확인 사항
- ✅ 가이드가 게이지 바와 올바르게 정렬되어 있는가?
- ✅ Start 마커가 Perfect 시작 시간에 위치하는가?
- ✅ End 마커가 Perfect 종료 시간에 위치하는가?
- ✅ Fill Rect가 Start~End 구간을 올바르게 채우는가?
- ✅ 여러 Hit가 있을 때 각각의 가이드가 생성되는가?
- ✅ 다음 턴 시작 시 이전 가이드가 제거되는가?

### 3. 문제 해결
- **가이드가 보이지 않는 경우:**
  - CombatStatusDisplay에 Prefab과 GuideContainer가 올바르게 할당되었는지 확인
  - GuideContainer의 Canvas Renderer가 활성화되어 있는지 확인
  - 가이드의 색상 알파값이 너무 낮지 않은지 확인

- **위치가 이상한 경우:**
  - GuideContainer의 Anchor와 Pivot 설정 확인
  - turnTimerProgressBar와 동일한 크기/위치인지 확인
  - PerfectTimingGuide의 Anchor가 Left-Center인지 확인

- **크기가 이상한 경우:**
  - FillRect의 Height가 적절한지 확인
  - Marker Size가 적절한지 확인
  - Canvas Scaler 설정 확인

## 커스터마이징

### 색상 변경
- `PerfectTimingGuide` 스크립트의 `guideColor` 필드 조정
- 또는 `GetGuideColorForHit` 메서드를 수정하여 히트별로 다른 색상 적용

### 크기 조정
- `markerSize`: 시작/끝 마커의 크기
- `fillHeight`: FillRect의 높이

### 투명도 조정
- `guideColor.a`: 전체 투명도
- `ApplyColors` 메서드에서 FillRect는 `guideColor.a * 0.5f`로 더 투명하게 설정됨

## 참고 사항
- 가이드는 매 턴마다 새로 생성되므로 성능에 유의하세요
- 히트가 많은 검술의 경우 가이드가 겹칠 수 있으니 적절히 조정하세요
- 가이드 색상은 히트 인덱스에 따라 자동으로 변경됩니다

