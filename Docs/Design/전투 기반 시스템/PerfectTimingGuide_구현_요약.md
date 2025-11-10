# Perfect Timing Guide 구현 완료 요약

## 구현 내용

### 1. 생성된 파일

#### 스크립트
- **`Assets/Script/UI/PerfectTimingGuide.cs`**
  - Perfect Timing 구간을 시각적으로 표시하는 UI 컴포넌트
  - Guide (대기 상태) / Already (완료 상태) 두 세트로 구성
  - 완벽 입력 성공 시 Guide → Already로 자동 전환
  - Width만 동적으로 설정 (색상/크기는 Prefab에서 설정)

#### 수정된 파일
- **`Assets/Script/UI/CombatStatusDisplay.cs`**
  - Perfect Timing 가이드 생성/제거/완료 전환 로직 추가
  - `ShowPerfectTimingGuides()`: 검술의 모든 Hit에 대해 가이드 생성
  - `MarkGuideAsCompleted()`: 완벽 입력 성공 시 가이드 완료 상태로 전환
  - `ClearPerfectTimingGuides()`: 가이드 제거
  - `ClearResults()`에 가이드 자동 제거 추가

- **`Assets/Script/Combat/CombatManager.cs`**
  - 턴 시작 시 `ShowPerfectTimingGuides()` 호출 추가
  - 검술 데이터와 턴 지속 시간을 전달하여 가이드 표시
  - 공격자 완벽 입력 성공 시 `MarkGuideAsCompleted()` 호출하여 가이드 전환

#### 문서
- **`Docs/Design/PerfectTimingGuide_생성_가이드.md`**
  - Prefab 생성 상세 가이드
  - Unity Editor 설정 단계별 설명
  - 테스트 및 문제 해결 방법

### 2. 주요 기능

#### PerfectTimingGuide 컴포넌트
```csharp
public void SetGuideWidth(float width)  // width만 동적 설정 (Guide/Already 양쪽)
public void MarkAsCompleted()            // 완벽 입력 성공 시 Guide → Already 전환
public void Cleanup()                    // 가이드 제거
```

**참고:** 
- 색상, 크기, 투명도 등은 코드가 아닌 Prefab에서 설정
- 초기 상태: Guide 활성화, Already 비활성화
- 완벽 입력 성공: Guide 비활성화, Already 활성화

#### CombatStatusDisplay 연동
```csharp
public void ShowPerfectTimingGuides(ActionCommandData actionData, float totalTurnTime)  // 가이드 생성
public void MarkGuideAsCompleted(int hitIndex)                                         // 가이드 완료 전환
public void ClearPerfectTimingGuides()                                                 // 가이드 제거
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
├── Guide (컨테이너, 초기 활성화)
│   ├── StartMarker (Image, 원형)
│   ├── EndMarker (Image, 원형)
│   └── FillRect (Image, 사각형)
└── Already (컨테이너, 초기 비활성화)
    ├── StartMarker (Image, 원형)
    ├── EndMarker (Image, 원형)
    └── FillRect (Image, 사각형)
```

**Anchor 설정:**
- PerfectTimingGuide: Left-Center (0, 0.5) / Pivot: Left-Center (0, 0.5)
- Guide / Already: Left-Center (0, 0.5) / Pivot: Left-Center (0, 0.5)
- FillRect: Left-Center (0, 0.5) / Pivot: Left-Center (0, 0.5)
- StartMarker / EndMarker: Left-Center (0, 0.5) / Pivot: Center (0.5, 0.5)

**시각적 속성:**
- Guide 세트: 대기 상태 색상 (예: 노란색 반투명)
- Already 세트: 완료 상태 색상 (예: 초록색 불투명)
- 색상, 크기, 투명도는 Prefab에서 직접 설정
- 스크립트는 width(시간 정보)만 동적으로 계산하여 설정

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
Prefab 에디터에서 각 UI 요소(StartMarker, EndMarker, FillRect)의 Image 컴포넌트 → Color 필드 수정

### 크기 조정
Prefab 에디터에서:
- StartMarker, EndMarker: RectTransform → Width/Height 수정
- FillRect: RectTransform → Height 수정 (Width는 스크립트가 자동 계산)

### 투명도 조정
각 Image 컴포넌트의 Color → Alpha 값 수정

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

1. **오브젝트 풀링**: 성능 최적화 (매 턴마다 Instantiate/Destroy 대신)
2. **애니메이션**: 가이드 등장/사라질 때 페이드 효과
3. **상호작용**: 마우스 호버 시 Hit 정보 툴팁 표시
4. **시각 효과**: 현재 시간이 Perfect 구간에 진입하면 하이라이트
5. **다중 Prefab**: 히트별 또는 검술 타입별로 다른 Prefab 사용 (색상/모양 구분)

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

