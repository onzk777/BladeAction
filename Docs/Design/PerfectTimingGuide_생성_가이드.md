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
  - Guide Container: (아래 참조)
  - Guide Start Marker: (아래 참조)
  - Guide End Marker: (아래 참조)
  - Guide Fill Rect: (아래 참조)
  - Already Container: (아래 참조)
  - Already Start Marker: (아래 참조)
  - Already End Marker: (아래 참조)
  - Already Fill Rect: (아래 참조)
  
**참고:** 
- Guide 세트는 대기 상태(아직 입력하지 않음)를 표시
- Already 세트는 완료 상태(완벽 입력 성공)를 표시
- 색상, 크기, 투명도 등 시각적 속성은 스크립트가 아닌 Prefab에서 직접 설정

### 2. 자식 오브젝트 1: Guide (GameObject 컨테이너)

**컴포넌트:**
- GameObject (빈 오브젝트)
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Left-Center (0, 0.5)
  - Position: (0, 0, 0)

### 3. Guide의 자식 오브젝트 1-1: StartMarker

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Center (0.5, 0.5)
  - Width: 10, Height: 10
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(1, 0.8, 0, 0.7)
  - Image Type: Simple
  - Sprite: UI Sprite Circle (Built-in) 또는 Knob 등 원형 스프라이트
  - Raycast Target: 체크 해제

### 4. Guide의 자식 오브젝트 1-2: EndMarker

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Center (0.5, 0.5)
  - Width: 10, Height: 10
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(1, 0.8, 0, 0.7) - 예시: 노란색 반투명
  - Image Type: Simple
  - Sprite: UI Sprite Circle (Built-in) 또는 Knob 등 원형 스프라이트
  - Raycast Target: 체크 해제

### 5. Guide의 자식 오브젝트 1-3: FillRect

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Left-Center (0, 0.5)
  - Width: 100 (기본값, 스크립트에서 변경됨)
  - Height: 20
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(1, 0.8, 0, 0.35) - 예시: 노란색 더 투명
  - Image Type: Simple
  - Sprite: UI Sprite (Built-in) 또는 단순 사각형 스프라이트
  - Raycast Target: 체크 해제

### 6. 자식 오브젝트 2: Already (GameObject 컨테이너)

**컴포넌트:**
- GameObject (빈 오브젝트)
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Left-Center (0, 0.5)
  - Position: (0, 0, 0)

### 7. Already의 자식 오브젝트 2-1: StartMarker

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Center (0.5, 0.5)
  - Width: 10, Height: 10
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(0, 1, 0, 0.9) - 예시: 초록색 불투명
  - Image Type: Simple
  - Sprite: UI Sprite Circle (Built-in) 또는 Knob 등 원형 스프라이트
  - Raycast Target: 체크 해제

### 8. Already의 자식 오브젝트 2-2: EndMarker

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Center (0.5, 0.5)
  - Width: 10, Height: 10
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(0, 1, 0, 0.9) - 예시: 초록색 불투명
  - Image Type: Simple
  - Sprite: UI Sprite Circle (Built-in) 또는 Knob 등 원형 스프라이트
  - Raycast Target: 체크 해제

### 9. Already의 자식 오브젝트 2-3: FillRect

**컴포넌트:**
- `RectTransform`
  - Anchors: Left-Center (Min: 0, 0.5 / Max: 0, 0.5)
  - Pivot: Left-Center (0, 0.5)
  - Width: 100 (기본값, 스크립트에서 변경됨)
  - Height: 20
  - Position: (0, 0, 0)

- `Image`
  - Color: RGBA(0, 1, 0, 0.6) - 예시: 초록색 반투명
  - Image Type: Simple
  - Sprite: UI Sprite (Built-in) 또는 단순 사각형 스프라이트
  - Raycast Target: 체크 해제

## 계층 구조 요약

```
PerfectTimingGuide (루트)
├── Guide (컨테이너, 초기 활성화)
│   ├── StartMarker
│   ├── EndMarker
│   └── FillRect
└── Already (컨테이너, 초기 비활성화)
    ├── StartMarker
    ├── EndMarker
    └── FillRect
```

**색상 예시:**
- Guide: 노란색 계열 (대기 상태)
- Already: 초록색 계열 (완료 상태)

## Unity Editor에서 생성 단계

**참고:** Guide와 Already 두 세트를 만들어야 하므로, 각 세트를 따로 생성합니다.

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

### 단계 3: Guide 컨테이너 생성
1. PerfectTimingGuide 우클릭 → Create Empty
2. 이름을 "Guide"로 변경
3. Inspector에서:
   - RectTransform:
     - Anchors Preset: Left-Center
     - Pivot: X=0, Y=0.5
     - Pos X=0, Y=0, Z=0

### 단계 4: Guide 세트 생성 (FillRect, StartMarker, EndMarker)

**4-1. Guide/FillRect:**
1. Guide 우클릭 → UI → Image
2. 이름을 "FillRect"로 변경
3. Inspector:
   - RectTransform: Anchors=Left-Center, Pivot=(0, 0.5), Width=100, Height=20
   - Image: Color=노란색 반투명 (예: R=1, G=0.8, B=0, A=0.35)
   - Raycast Target 체크 해제

**4-2. Guide/StartMarker:**
1. Guide 우클릭 → UI → Image
2. 이름을 "StartMarker"로 변경
3. Inspector:
   - RectTransform: Anchors=Left-Center, Pivot=(0.5, 0.5), Width=10, Height=10
   - Image: Color=노란색 (R=1, G=0.8, B=0, A=0.7), Sprite=Knob
   - Raycast Target 체크 해제

**4-3. Guide/EndMarker:**
1. Guide 우클릭 → UI → Image
2. 이름을 "EndMarker"로 변경
3. Inspector:
   - RectTransform: Anchors=Left-Center, Pivot=(0.5, 0.5), Width=10, Height=10
   - Image: Color=노란색 (R=1, G=0.8, B=0, A=0.7), Sprite=Knob
   - Raycast Target 체크 해제

### 단계 5: Already 컨테이너 생성
1. PerfectTimingGuide 우클릭 → Create Empty
2. 이름을 "Already"로 변경
3. Inspector에서:
   - RectTransform:
     - Anchors Preset: Left-Center
     - Pivot: X=0, Y=0.5
     - Pos X=0, Y=0, Z=0
4. **Already 오브젝트를 비활성화** (Inspector 상단 체크박스 해제)

### 단계 6: Already 세트 생성 (Guide 세트와 동일하지만 색상만 다름)

**6-1. Already/FillRect:**
- Guide/FillRect를 복사하여 Already 아래로 이동
- Color를 초록색 반투명으로 변경 (예: R=0, G=1, B=0, A=0.6)

**6-2. Already/StartMarker:**
- Guide/StartMarker를 복사하여 Already 아래로 이동
- Color를 초록색으로 변경 (예: R=0, G=1, B=0, A=0.9)

**6-3. Already/EndMarker:**
- Guide/EndMarker를 복사하여 Already 아래로 이동
- Color를 초록색으로 변경 (예: R=0, G=1, B=0, A=0.9)

### 단계 7: PerfectTimingGuide 스크립트 연결
1. PerfectTimingGuide 루트 오브젝트 선택
2. Inspector에서 `PerfectTimingGuide` 스크립트 컴포넌트 찾기
3. 필드 할당:
   - Guide Container: Guide 오브젝트 드래그
   - Guide Start Marker: Guide/StartMarker 드래그
   - Guide End Marker: Guide/EndMarker 드래그
   - Guide Fill Rect: Guide/FillRect 드래그
   - Already Container: Already 오브젝트 드래그
   - Already Start Marker: Already/StartMarker 드래그
   - Already End Marker: Already/EndMarker 드래그
   - Already Fill Rect: Already/FillRect 드래그

### 단계 8: Prefab으로 저장
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
- ✅ **초기 상태: Guide 활성화, Already 비활성화인가?**
- ✅ **완벽 입력 성공 시: 해당 Hit의 가이드가 Guide → Already로 전환되는가?**
- ✅ **완벽 입력 실패 시: 해당 Hit의 가이드가 Guide 상태로 유지되는가?**
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
Prefab의 각 오브젝트 선택 → Inspector → Image 컴포넌트 → Color 필드에서 직접 수정
- StartMarker: 원하는 색상/투명도 설정
- EndMarker: 원하는 색상/투명도 설정
- FillRect: 원하는 색상/투명도 설정 (보통 마커보다 더 투명하게)

### 크기 조정
Prefab의 각 오브젝트 선택 → Inspector → Rect Transform → Width/Height 수정
- StartMarker: Width/Height (예: 10x10, 15x15 등)
- EndMarker: Width/Height (예: 10x10, 15x15 등)
- FillRect: Height만 조정 (Width는 스크립트가 자동 계산)

### 투명도 조정
Image 컴포넌트의 Color → Alpha 값 조정 (0~255 또는 0~1)

## 참고 사항
- 가이드는 매 턴마다 새로 생성되므로 성능에 유의하세요
- 히트가 많은 검술의 경우 가이드가 겹칠 수 있으니 적절히 조정하세요
- 스크립트는 width(시간 정보)만 계산하며, 모든 시각적 속성은 Prefab에서 설정됩니다
- 히트별로 다른 색상을 사용하고 싶다면 여러 Prefab 변형을 만들고 코드에서 선택하도록 확장 가능합니다

