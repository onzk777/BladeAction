## Spine 애니메이션 연동 및 플로팅 텍스트 구현 계획

### 목표
- 검술 액션 실행 시 Spine 애니메이션 재생을 전투 루프와 동기화한다.
- Spine 타임라인 이벤트 발생 시, 애니메이션 근처에 플로팅 텍스트를 생성·상승·페이드아웃 처리한다.

### 산출물(파일/프리팹/스크립트)
- 프리팹
  - `Prefabs/FloatingText.prefab` (World-Space Canvas + TextMeshProUGUI)
- 스크립트
  - `FloatingText.cs`: 텍스트 이동/페이드 로직(코루틴/Update 기반)
  - `FloatingTextPool.cs`: 오브젝트 풀, Prewarm/Reuse
  - `FloatingTextSpawner.cs`: 특정 위치/본(Transform) 기준 스폰 API
  - `SpineEventRelay.cs`: Spine `SkeletonAnimation`/`AnimationState` 이벤트 구독 → Unity 이벤트/콜백으로 릴레이
  - (선택) `ActionAnimationBinder.cs`: `ActionCommandData` ↔ Spine 애니메이션명/트랙/루프 여부 매핑
- 데이터 변경(권장)
  - `ActionCommandData`에 `spineAnimationName`(string), `spineTrack`(int, 기본 0), `loop`(bool) 필드 추가
  - (선택) 이벤트명 → 텍스트/스타일 매핑: `List<SpineEventTextRule>`

### Spine 데이터 임포트 체크리스트
- `SkeletonDataAsset`, `AtlasAsset`, 머티리얼 정상 생성 확인
- `SkeletonAnimation` 컴포넌트 사용(2D 기준) 또는 `SkeletonMecanim`(Mecanim 필요 시)
- 스케일/좌표계 확인(씬 내 크기/위치 자연스러운지)
- `Default Mix Duration` 설정(부드러운 전환 필요 시)
- 애니메이션 클립/이벤트명 목록 정리(아래 바인딩에 사용)

### 전투 루프 연동 설계
- 트리거 위치: 커맨드 시작 시점(`CombatManager.PerformTurn`에서 `ShowCommandStart` 직후 또는 컨트롤러 레벨)
- 재생 주체: 각 컨트롤러가 자신의 `SkeletonAnimation`에 애니메이션 재생 명령을 수행하도록 분리
- 바인딩 규칙(예시)
  - `ICombatController.PlayActionAnimation(ActionCommandData cmd)` 메서드 도입(컨트롤러 별 구현)
  - 내부에서 `ActionAnimationBinder`를 통해 `cmd.spineAnimationName`을 조회하여 `state.SetAnimation(track, name, loop)` 호출
  - 연타 커맨드의 경우: 첫 재생 이후 `AddAnimation`으로 후속 애니메이션 큐잉 또는 이벤트 기반 분기

### 타이밍 동기화 전략
- 기본: 입력 윈도우는 게임 로직 기준(`perfectTimings`)으로 유지, 애니메이션은 **연출 동기**(시각/음향) 역할
- 고급(선택): Spine 이벤트를 윈도우 오픈/클로즈 트리거로 사용
  - 이벤트명 규칙 예: `Hit1_Open`, `Hit1_Close`, `Hit2_Open`, ...
  - `SpineEventRelay`에서 해당 이벤트를 수신 → `CombatManager`에 콜백(옵션 플래그로 전환)

### Spine 이벤트 처리 → 플로팅 텍스트
- 구독: `skeletonAnimation.AnimationState.Event += HandleSpineEvent;`
- 이벤트 페이로드: Spine 이벤트의 `string`, `int`, `float` 파라미터 해석
  - `string`을 텍스트로 사용하거나, 매핑 테이블에서 텍스트/스타일 조회
- 스폰 위치: 기본 캐릭터 루트 또는 특정 본(예: `chest`, `weapon_tip`) Transform 기준 + 오프셋
- 예시 이벤트 매핑
  - `Hit`: "명중!", 색상=노랑, 약한 흔들림
  - `ParryWindowOpen`: "지금이닷!", 색상=하늘색
  - `Guard`: "방어", 색상=회색

### 플로팅 텍스트 사양
- 연출: 위로 상승 + 알파 페이드아웃 + 약간의 좌우 랜덤 드리프트(선택)
- 파라미터(프리팹 노출)
  - `float lifetime = 0.8f`, `float riseSpeed = 1.2f`, `Vector2 startOffset`, `Color color`
  - 카메라 빌보드 옵션(항상 카메라를 바라봄)
- 풀링: `FloatingTextPool`에 `Prewarm(count)`, `Get()`, `Release()` 제공
- 스폰 API(Spawner)
  - `Spawn(string text, Transform anchor, Color? color = null)`
  - `SpawnStyled(string styleKey, Transform anchor)` → 룰 기반 스타일 적용

### 데이터/바인딩 구조(권장)
- `ActionCommandData`
  - `spineAnimationName: string`
  - `spineTrack: int`(기본 0)
  - `loop: bool`
  - (선택) `List<SpineEventTextRule>`: `{ eventName, text, color, styleKey }`
- `SpineEventRelay`
  - 인스펙터에 `FloatingTextSpawner` 참조, `Transform anchorBone`(선택)
  - `OnEvent(Spine.Event e)` → 룰 매칭 후 Spawner 호출

### 통합 흐름(의사 코드)
```csharp
// 컨트롤러 측
public void PlayActionAnimation(ActionCommandData cmd) {
    var name = cmd.spineAnimationName;
    skeletonAnimation.AnimationState.SetAnimation(cmd.spineTrack, name, cmd.loop);
}

// Spine 이벤트 릴레이 측
void HandleSpineEvent(TrackEntry entry, Spine.Event e) {
    var rule = FindRule(e.Data.Name); // eventName → text/style
    if (rule != null) spawner.Spawn(rule.text, anchorBone ?? transform, rule.color);
}
```

### 구현 순서(내일 실행용 체크리스트)
1. Spine 데이터 임포트 확인(스켈레톤, 아틀라스, 머티리얼) 및 씬에 `SkeletonAnimation` 배치
2. `SpineEventRelay` 스크립트 작성/부착 → AnimationState.Event 구독 → 디버그 로그로 이벤트명 출력 확인
3. `Prefabs/FloatingText.prefab` 제작(World-Space Canvas + TMP), `FloatingText.cs`로 이동/페이드 구현
4. `FloatingTextPool`, `FloatingTextSpawner` 제작 → 스폰 API 검증(테스트 버튼/에디터 툴 가능)
5. 컨트롤러에 `PlayActionAnimation(ActionCommandData)` 추가 → `CombatManager.PerformTurn` 시작 시 호출
6. Spine 이벤트명 ↔ 텍스트 규칙 초안 구성(우선: `Hit`, `ParryWindowOpen`, `Guard`)
7. 실제 플레이에서 이벤트 트리거 시 텍스트 생성되는지 확인, 위치/크기/가독성 조정
8. 연타 액션에서 이벤트 스팸/중복 처리 확인 → 풀 사이즈/쿨다운 조정

### 테스트 시나리오
- 단일 히트 공격에서 `Hit` 이벤트 발생 시 텍스트 1회 생성/상승/사라짐
- 연타(3타)에서 각 히트 이벤트마다 텍스트가 개별 생성되며 누락/중복 없음
- 방어/쳐내기 이벤트에서 색상/문구가 규칙대로 반영
- 카메라 거리/각도 변화에도 가독성 유지(빌보드, 스케일 조정)

### 리스크 및 대응
- 이벤트 명칭/철자 변경 → 룰 매칭 실패: 기본 텍스트/스타일 폴백 제공
- 풀 과부하 → GC/할당 증가: Prewarm/최대치 관리, 초과 시 LRU 반환 또는 스폰 제한
- 좌표/정렬 문제 → 본/루트 기준 오프셋과 Canvas Sorting Layer 점검
- 애니 전환 중 이벤트 유실 → TrackEntry 교체 시 구독 유지/이중 구독 방지(OnEnable/OnDisable 관리)

### 수치/설정(초기값 제안)
- FloatingText: `lifetime 0.8s`, `riseSpeed 1.2`, `startOffset (0, 1.2)`, `alphaFrom 1.0 → 0.0`
- Canvas: World-Space, `Sorting Layer: UI`, `Order in Layer: 500+`(필요 시)
- Spine Event 룰: `Hit → 노랑`, `Guard → 회색`, `ParryWindowOpen → 하늘색`


