# Perfect Timing Guide 구현 완료 요약

## 구현 내용

### 1. 생성된 파일

#### 스크립트
- **`Assets/Script/UI/PerfectTimingGuide.cs`**
  - Perfect Timing 구간을 시각적으로 표시하는 UI 컴포넌트
  - Start/End 마커와 Fill Rect로 구성
  - Width와 색상을 동적으로 설정 가능

#### 수정된 파일
- **`Assets/Script/UI/CombatStatusDisplay.cs`**
  - Perfect Timing 가이드 생성/제거 로직 추가
  - `ShowPerfectTimingGuides()`: 검술의 모든 Hit에 대해 가이드 생성
  - `ClearPerfectTimingGuides()`: 가이드 제거
  - `ClearResults()`에 가이드 자동 제거 추가

- **`Assets/Script/Combat/CombatManager.cs`**
  - 턴 시작 시 `ShowPerfectTimingGuides()` 호출 추가
  - 검술 데이터와 턴 지속 시간을 전달하여 가이드 표시

#### 문서
- **`Docs/Design/PerfectTimingGuide_생성_가이드.md`**
  - Prefab 생성 상세 가이드
  - Unity Editor 설정 단계별 설명
  - 테스트 및 문제 해결 방법

### 2. 주요 기능

#### PerfectTimingGuide 컴포넌트
```csharp
public void SetGuideWidth(float width)
public void SetGuideColor(Color color)
public void Cleanup()
```

#### CombatStatusDisplay 연동
```csharp
public void ShowPerfectTimingGuides(ActionCommandData actionData, float totalTurnTime)
public void ClearPerfectTimingGuides()
```

### 3. 동작 흐름

```
CombatManager.PerformTurn()
    ↓
턴 시작 (command, turnDuration 계산)
    ↓
CombatStatusDisplay.ShowPerfectTimingGuides(command, turnDuration)
    ↓
각 Hit의 PerfectTimingWindow 추출
    ↓
각 Hit마다:
    1. PerfectTimingGuide Prefab 인스턴스화
    2. Start 시간 → 게이지 상 X 위치 계산
    3. Duration → Width 계산
    4. 위치 및 크기 설정
    5. 색상 적용
    ↓
턴 종료 또는 다음 턴 시작
    ↓
CombatStatusDisplay.ClearPerfectTimingGuides()
    ↓
모든 가이드 제거
```

### 4. Prefab 구조

```
PerfectTimingGuide (RectTransform + PerfectTimingGuide script)
├── StartMarker (Image, 원형)
├── EndMarker (Image, 원형)
└── FillRect (Image, 사각형)
```

**Anchor 설정:**
- PerfectTimingGuide: Left-Center (0, 0.5)
- FillRect: Left-Center (0, 0.5)
- StartMarker: Center (0.5, 0.5)
- EndMarker: Center (0.5, 0.5)

**색상:**
- 기본: RGBA(1, 0.8, 0, 0.7) - 반투명 노란색
- FillRect: 알파값 50% 더 투명
- 히트별로 자동 색조 조정

### 5. 위치 계산 로직

```csharp
// Start 위치 (게이지 바 상의 픽셀 위치)
float startRatio = timing.start / totalTurnTime;
float startPositionX = gaugeWidth * startRatio;

// Width (Perfect 구간 길이)
float durationRatio = timing.duration / totalTurnTime;
float guideWidth = gaugeWidth * durationRatio;
```

### 6. 다중 Hit 지원

- 검술의 `perfectTimings` 리스트를 순회
- 각 Hit마다 독립적인 가이드 생성
- 히트 인덱스에 따라 색상 자동 변경
- 모든 가이드는 `activeGuides` 리스트에 추적

## Unity Editor 설정 필요

### 1. Prefab 생성
`Docs/Design/PerfectTimingGuide_생성_가이드.md` 참조하여:
1. Canvas에 PerfectTimingGuide 오브젝트 생성
2. StartMarker, EndMarker, FillRect 자식 오브젝트 생성
3. 스크립트 컴포넌트 추가 및 참조 연결
4. Prefab으로 저장 (`Assets/Prefab/PerfectTimingGuide.prefab`)

### 2. CombatStatusDisplay 설정
1. Scene에서 CombatStatusDisplay 오브젝트 찾기
2. Inspector에서:
   - **Perfect Timing Guide Prefab**: 생성한 Prefab 할당
   - **Guide Container**: 턴 타이머 게이지 바와 같은 레벨의 Container 할당

### 3. GuideContainer 설정 (필요 시)
- 턴 타이머 게이지 바와 동일한 위치/크기의 RectTransform
- Anchor: 게이지 바와 동일하게
- Z-order: 게이지 바 위에 표시되도록 배치

## 테스트 방법

### 1. Play Mode 실행
1. Unity Editor에서 Play 버튼 클릭
2. 전투 시작
3. 검술 선택

### 2. 확인 사항
- ✅ 턴 타이머 게이지 바 위에 노란색 가이드 표시
- ✅ Hit 개수만큼 가이드 생성
- ✅ Perfect 타이밍 구간에 올바르게 배치
- ✅ 다음 턴 시작 시 이전 가이드 제거

### 3. Console 로그
```
[CombatStatusDisplay] Hit 1 가이드 생성: Start=0.500초, Duration=0.200초, X=125.0px, Width=50.0px
[CombatStatusDisplay] Hit 2 가이드 생성: Start=1.000초, Duration=0.200초, X=250.0px, Width=50.0px
[CombatStatusDisplay] 2개의 Perfect Timing 가이드 생성 완료
```

## 커스터마이징 옵션

### 색상 변경
`PerfectTimingGuide.cs`:
```csharp
[SerializeField] private Color guideColor = new Color(1f, 0.8f, 0f, 0.7f);
```

### 크기 조정
```csharp
[SerializeField] private float markerSize = 10f;
[SerializeField] private float fillHeight = 20f;
```

### 히트별 색상 로직
`CombatStatusDisplay.cs`:
```csharp
private Color GetGuideColorForHit(int hitIndex)
{
    // 색조 회전 로직
}
```

## 주의 사항

### 1. Unity Editor 재시작
새 스크립트가 추가되었으므로 Unity Editor를 재시작하거나 Assets → Refresh를 실행하세요.

### 2. Prefab 연결 필수
CombatStatusDisplay에 Prefab과 GuideContainer를 연결하지 않으면 가이드가 생성되지 않습니다.

### 3. Canvas Scaler 고려
Canvas Scaler 설정에 따라 픽셀 크기가 달라질 수 있습니다.

### 4. Z-order 조정
가이드가 다른 UI에 가려진다면:
- GuideContainer의 Hierarchy 순서 조정
- Canvas Group 또는 Sorting Order 사용

## 성능 고려사항

- 가이드는 매 턴마다 재생성됨 (Instantiate/Destroy)
- 많은 Hit를 가진 검술의 경우 가이드가 많이 생성될 수 있음
- 필요 시 오브젝트 풀링 패턴 적용 고려

## 향후 개선 가능 사항

1. **오브젝트 풀링**: 성능 최적화
2. **애니메이션**: 가이드 등장/사라질 때 페이드 효과
3. **상호작용**: 마우스 호버 시 Hit 정보 툴팁 표시
4. **시각 효과**: 현재 시간이 Perfect 구간에 진입하면 하이라이트
5. **색상 프리셋**: 검술 타입별로 다른 색상 적용

## 문제 해결

### 가이드가 보이지 않음
1. CombatStatusDisplay의 Prefab/Container 연결 확인
2. Console에서 경고/오류 메시지 확인
3. GuideContainer가 활성화되어 있는지 확인
4. 색상 알파값 확인

### 위치가 이상함
1. GuideContainer의 Anchor/Pivot 확인
2. 턴 타이머 게이지 바와 크기/위치 동일한지 확인
3. Canvas Scaler 설정 확인

### 컴파일 오류
1. Unity Editor 재시작
2. Assets → Reimport All
3. Library 폴더 삭제 후 재시작

## 관련 파일
- `Assets/Script/UI/PerfectTimingGuide.cs`
- `Assets/Script/UI/CombatStatusDisplay.cs`
- `Assets/Script/Combat/CombatManager.cs`
- `Assets/Script/ActionCommandData.cs`
- `Assets/Script/PerfectTimingWindow.cs`
- `Docs/Design/PerfectTimingGuide_생성_가이드.md`

