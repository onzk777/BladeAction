# BT 디버그 패널 완전 가이드

## 개요

BT 디버그 패널은 Behavior Tree 실행을 **보기 쉽게 정리된 UI**로 표시하는 고급 디버깅 도구입니다.

### 핵심 특징
- ✅ **요약 정보**: 현재 턴, 로그 상태 한눈에 파악
- ✅ **실행 히스토리**: 최근 N개 BT 평가 기록 (최신순)
- ✅ **상세 로그**: 선택한 로그의 조건/액션/확률 전체 표시
- ✅ **필터링**: Enemy/Player, 매칭 성공만 등
- ✅ **제어**: 일시정지, 클리어, 내보내기
- ✅ **실용성**: Console 로그 대신 정리된 UI

---

## Unity 설정

### 1단계: Hierarchy 구조 생성

```
Canvas
└── DebugPanel
    ├── TabButtons (Horizontal Layout)
    │   ├── CombatInfoButton ("전투 정보")
    │   └── BTInfoButton ("BT 정보")
    │
    ├── CombatInfoPanel (전투 정보 패널)
    │   └── ... (기존 구조)
    │
    └── BTInfoPanel (BT 정보 패널) ✨
        ├── SummaryPanel
        │   └── SummaryText (TMP)
        │
        ├── HistoryPanel (ScrollView 권장)
        │   └── HistoryText (TMP)
        │
        ├── DetailPanel (ScrollView 권장)
        │   └── DetailText (TMP)
        │
        ├── ControlPanel
        │   ├── ClearHistoryButton ("히스토리 클리어")
        │   ├── PauseLoggingButton ("일시정지")
        │   └── ExportButton ("내보내기")
        │
        └── FilterPanel
            ├── ShowEnemyToggle ("Enemy 표시")
            ├── ShowPlayerToggle ("Player 표시")
            └── ShowMatchedOnlyToggle ("매칭만")
```

### 2단계: BTDebugPanel 컴포넌트 추가

**BTInfoPanel GameObject에 BTDebugPanel 컴포넌트 추가**

**Inspector 설정**:
```
BTDebugPanel
├── UI 텍스트 참조
│   ├── Summary Text: [SummaryText]
│   ├── History Text: [HistoryText]
│   └── Detail Text: [DetailText]
│
├── 컨트롤 버튼
│   ├── Clear History Button: [ClearHistoryButton]
│   ├── Pause Logging Button: [PauseLoggingButton]
│   └── Export Button: [ExportButton]
│
├── 필터 토글
│   ├── Show Enemy Toggle: [ShowEnemyToggle]
│   ├── Show Player Toggle: [ShowPlayerToggle]
│   └── Show Matched Only Toggle: [ShowMatchedOnlyToggle]
│
└── 설정
    ├── Update Interval: 0.5
    ├── Max History Display: 10
    └── Verbose Mode: false (체크박스)
```

### 3단계: DebugPanelController 설정

**DebugPanel GameObject의 DebugPanelController 설정**:
```
DebugPanelController
├── Debug Panel: [DebugPanel]
├── Combat Info Panel: [CombatInfoPanel]  ✨
└── BT Info Panel: [BTInfoPanel]          ✨
```

### 4단계: 버튼 연결

**CombatInfoButton** → OnClick():
- DebugPanelController → `ShowCombatInfoPanel()`

**BTInfoButton** → OnClick():
- DebugPanelController → `ShowBTInfoPanel()`

---

## UI 구성 상세

### 1. 요약 정보 (SummaryText)

**표시 내용**:
```
╔═══ BT 디버그 패널 ═══╗
║ 턴: 5
║ 공격자: Enemy
║ ━━━ 로그 상태 ━━━
║ 총 기록: 23개
║ 표시 중: 10개
║ 로깅: 활성
║ 상세 모드: OFF
╚═══════════════════════╝
```

**용도**:
- 현재 전투 상황 확인
- 로그 상태 확인
- 필터링 결과 확인

---

### 2. 실행 히스토리 (HistoryText) ✨ 핵심!

**표시 내용**:
```
╔═══ 실행 히스토리 (최근 5개) ═══╗
║   ⚔ T3 | Goblin | ✓
║    HP < 50%
║    조건: 1/1 | 액션: 3/3
║    확률: 공격=80%, 막기=60%
║ ──────────────────
║ ► 🛡 T2 | Player | ✗  ← 선택된 로그
║    조건: 0/2 | 액션: 0/0
║ ──────────────────
║   ⚔ T2 | Goblin | ✓
║    HP >= 50%
║    확률: 공격=50%
╚══════════════════════════════╝

클릭으로 선택 → 상세 보기
```

**아이콘**:
- ⚔: 공격 턴
- 🛡: 방어 턴
- ✓: Entry 매칭 성공 (녹색)
- ✗: 매칭 실패 (빨간색)
- ►: 현재 선택된 로그 (금색)

**특징**:
- 최신 로그가 위에 표시 (최신순)
- 간략한 요약 정보
- 클릭으로 상세 보기 전환 (향후 확장)

---

### 3. 상세 로그 (DetailText) ✨ 강력!

**표시 내용**:
```
╔═══ 상세 로그: 턴 3 | Goblin ═══╗
║ BT: BT_AggressiveEnemy
║ 타입: 공격 턴
║ 결과: ✓ 매칭 성공
║ Entry[0]: HP < 50%
║ ━━━ 조건 평가 ━━━
║  ✓ HP50Less
║     └ Self HP: 45/100 (45.0%) Less 0.50 (Percentage)
║ ━━━ 액션 실행 ━━━
║  ▶ 공격 성공률 80% [P:0]
║     └ AttackPerfectRate 절대값: 80%
║  ▶ 검술 인덱스 1 [P:0]
║     └ 검술 인덱스: 1
║  ⊘ 막기 시도 90%
║     └ 건너뜀: 이미 실행됨 (executeOncePerCombat)
║ ━━━ 확률 변경 ━━━
║  • AttackPerfectRate: 80%
║  • GuardAttemptRate: 60%
║ ━━━ 검술 선택 ━━━
║  인덱스: 1
║ ━━━━━━━━━━━━━━━
║ 시각: 14:32:15
╚══════════════════════════════╝
```

**특징**:
- 모든 조건 평가 결과
- 모든 액션 실행/건너뜀 정보
- 확률 변경 전체 내역
- 검술 선택 정보
- 실행 시각

---

### 4. 컨트롤 버튼

**히스토리 클리어**:
- 모든 로그 삭제
- 상세 로그도 클리어

**일시정지**:
- 로그 기록 중단
- UI 업데이트 중단
- 버튼 텍스트: "⏸ 일시정지" ↔ "▶ 재개"

**내보내기**:
- 모든 로그를 텍스트 파일로 저장
- 위치: `Application.persistentDataPath`
- 파일명: `BTLogs_yyyyMMdd_HHmmss.txt`

---

### 5. 필터 토글

**Enemy 표시**:
- ON: Enemy 로그 표시
- OFF: Enemy 로그 숨김

**Player 표시**:
- ON: Player 로그 표시
- OFF: Player 로그 숨김

**매칭만**:
- ON: Entry 매칭 성공한 로그만 표시
- OFF: 모든 로그 표시

**예시**:
```
Enemy ON, Player OFF, 매칭만 ON
→ Enemy의 매칭 성공 로그만 표시
```

---

## 사용 시나리오

### 시나리오 1: BT 에셋 테스트

**목표**: HP < 50% 조건이 제대로 동작하는지 확인

1. **F3 키** → 디버그 패널 열기
2. **BT 정보 버튼** 클릭
3. 전투 진행 (Enemy HP 감소)
4. **히스토리 확인**:
   ```
   HP > 50%:
   ║ ⚔ T2 | Goblin | ✗  ← 조건 불만족
   
   HP < 50%:
   ║ ⚔ T5 | Goblin | ✓  ← 조건 만족!
   ║  HP < 50%
   ║  확률: 공격=80%
   ```
5. **성공!** HP 50% 이하에서 조건 만족 확인

---

### 시나리오 2: 확률 변경 추적

**목표**: BT가 확률을 제대로 변경하는지 확인

1. BT 정보 패널 열기
2. 히스토리에서 특정 로그 선택
3. **상세 로그 확인**:
   ```
   ╔═══ 상세 로그: 턴 3 | Goblin ═══╗
   ║ ━━━ 확률 변경 ━━━
   ║  • AttackPerfectRate: 80%  ✓
   ║  • GuardAttemptRate: 60%   ✓
   ```
4. 의도한 확률 변경 확인!

---

### 시나리오 3: executeOncePerCombat 검증

**목표**: 특정 액션이 1회만 실행되는지 확인

1. BT 정보 패널 열기
2. **매칭만 필터** ON
3. 같은 Entry가 여러 번 실행된 로그 찾기
4. **상세 로그 비교**:
   ```
   턴 4 (첫 실행):
   ║  ▶ 공격 성공률 100% [P:0]  ✓ 실행
   
   턴 6 (재실행 시도):
   ║  ⊘ 공격 성공률 100%
   ║     └ 건너뜀: 이미 실행됨  ✓ 1회 제한 작동!
   ```

---

### 시나리오 4: 로그 내보내기

**목표**: 테스트 결과 공유

1. 전투 진행 (여러 턴)
2. **내보내기 버튼** 클릭
3. 파일 생성 확인:
   ```
   C:\Users\...\AppData\LocalLow\...\BTLogs_20251014_143215.txt
   ```
4. 파일 열기:
   ```
   === BT 로그 내보내기 ===
   생성 시각: 2025-10-14 14:32:15
   총 로그 수: 23
   
   --- 턴 3 | Goblin | 공격 ---
   BT: BT_AggressiveEnemy
   결과: 매칭 성공
   Entry[0]: HP < 50%
   조건: 1개
   액션: 3개
   확률 변경: 2개
   ...
   ```

---

## 필터링 활용

### 예시 1: Enemy BT만 분석

**설정**:
- Enemy 표시: ON
- Player 표시: OFF
- 매칭만: OFF

**결과**:
```
╔═══ 실행 히스토리 (최근 5개) ═══╗
║ ⚔ T5 | Goblin | ✓
║ 🛡 T4 | Goblin | ✗
║ ⚔ T3 | Goblin | ✓
║ 🛡 T2 | Goblin | ✗
╚══════════════════════════════╝
```

**용도**: Enemy AI 패턴 분석

---

### 예시 2: 성공한 BT만 보기

**설정**:
- Enemy 표시: ON
- Player 표시: ON
- 매칭만: ON ✨

**결과**:
```
╔═══ 실행 히스토리 (최근 3개) ═══╗
║ ⚔ T5 | Goblin | ✓
║   HP < 50%
║   확률: 공격=80%
║ ──────────────────
║ ⚔ T3 | Goblin | ✓
║   HP >= 50%
║   확률: 공격=50%
╚══════════════════════════════╝
```

**용도**: 실제로 실행된 BT만 확인

---

## 상세 모드 vs 일반 모드

### 일반 모드 (Verbose Mode OFF)

**히스토리 표시**:
```
║ ⚔ T3 | Goblin | ✓
║  HP < 50%
```

**특징**:
- 간결함
- 빠른 스캔
- 성능 최적

### 상세 모드 (Verbose Mode ON)

**히스토리 표시**:
```
║ ⚔ T3 | Goblin | ✓
║  HP < 50%
║  조건: 1/1 | 액션: 3/3
║  확률: 공격=80%, 막기=60%
```

**특징**:
- 통계 정보 포함
- 확률 변경 미리보기
- 더 많은 정보

**권장**:
- 일반 상황: OFF
- 상세 분석: ON

---

## 로그 일시정지 활용

### 케이스: 특정 턴 분석

```
1. 전투 진행 중
2. 흥미로운 패턴 발견
3. 일시정지 버튼 클릭 ⏸
4. 히스토리를 천천히 분석
5. 상세 로그 확인
6. 재개 버튼 클릭 ▶
```

**효과**:
- 로그가 계속 쌓이지 않음
- 현재 상태 고정
- 차분히 분석 가능

---

## 내보내기 기능

### 파일 위치

**Windows**:
```
C:\Users\[사용자]\AppData\LocalLow\[회사명]\[제품명]\BTLogs_20251014_143215.txt
```

**코드로 확인**:
```csharp
Debug.Log(Application.persistentDataPath);
```

### 파일 형식

```
=== BT 로그 내보내기 ===
생성 시각: 2025-10-14 14:32:15
총 로그 수: 23

--- 턴 3 | Goblin | 공격 ---
BT: BT_AggressiveEnemy
결과: 매칭 성공
Entry[0]: HP < 50%
조건: 1개
액션: 3개
확률 변경: 2개
시각: 2025-10-14 14:31:05

--- 턴 2 | Player | 방어 ---
...
```

### 활용

- QA 테스터에게 공유
- 버그 리포트 첨부
- 패턴 분석 자료
- 밸런싱 데이터

---

## 문제 추적 예시

### 케이스 1: BT가 작동 안 함

**히스토리 확인**:
```
║ ⚔ T1 | Goblin | ✗
║ ⚔ T2 | Goblin | ✗
║ ⚔ T3 | Goblin | ✗
```

**원인**: 모든 턴에서 매칭 실패 (✗)

**해결**:
1. 상세 로그에서 조건 확인
2. Entry 조건이 너무 엄격한지 체크
3. Fallback Entry 추가

---

### 케이스 2: 확률이 변경 안 됨

**히스토리**:
```
║ ⚔ T3 | Goblin | ✓
║  HP < 50%
║  조건: 1/1 | 액션: 1/1
║  확률: (없음)  ← 문제!
```

**상세 로그**:
```
║ ━━━ 액션 실행 ━━━
║  ▶ 검술 인덱스 1 [P:0]  ← CommandSelection만 있음
```

**원인**: ProbabilityAdjustment 액션이 없음

**해결**: BT 에셋에 확률 조정 액션 추가

---

### 케이스 3: executeOncePerCombat 미작동

**히스토리**:
```
턴 4:
║  액션: 3/3 실행  ✓ 첫 실행

턴 6:
║  액션: 3/3 실행  ← 또 실행됨?!
```

**상세 로그 비교**:
```
턴 4:
║  ▶ 공격 성공률 100% [P:0]

턴 6:
║  ▶ 공격 성공률 100% [P:0]  ← 건너뛰기 안 됨!
```

**원인**: executeOncePerCombat = false

**해결**: BT 에셋에서 체크박스 활성화

---

## 성능 최적화

### Update Interval 조정

**빠른 반응** (0.2초):
```
Update Interval: 0.2
```
- 거의 실시간
- 약간의 성능 영향

**기본** (0.5초):
```
Update Interval: 0.5
```
- 권장 설정
- 성능 무시 가능

**성능 우선** (1.0초):
```
Update Interval: 1.0
```
- 저사양 기기
- 긴 전투

### 히스토리 수 조정

**간단** (5개):
```
Max History Display: 5
```
- 최근 몇 턴만
- UI 간결

**기본** (10개):
```
Max History Display: 10
```
- 권장
- 적당한 분석 깊이

**상세** (20개):
```
Max History Display: 20
```
- 긴 전투
- 패턴 분석

---

## 팁 & 요령

### 팁 1: 매칭 필터 활용

**문제**: 로그가 너무 많아서 보기 어려움

**해결**:
- "매칭만" 토글 ON
- 실제로 실행된 BT만 표시
- 실패한 조건은 숨김

---

### 팁 2: 일시정지로 스냅샷

**상황**: 중요한 순간 발견

**방법**:
1. 일시정지 ⏸
2. 히스토리 분석
3. 상세 로그 확인
4. 스크린샷 또는 내보내기
5. 재개 ▶

---

### 팁 3: 필터 조합

**Enemy 공격 패턴 분석**:
- Enemy: ON
- Player: OFF
- 매칭만: ON
→ Enemy의 성공한 공격만 표시

**Player 방어 실패 분석**:
- Enemy: OFF
- Player: ON
- 매칭만: OFF
→ Player BT 평가 결과 (방어 턴)

---

## Console 로그 vs BT 디버그 패널

### Console 로그

**장점**:
- 매우 상세한 정보
- Verbose 모드 지원
- 실시간 출력

**단점**:
- 로그가 계속 쌓임
- 이전 정보 찾기 어려움
- 정리 안 됨

**사용 시점**:
- 특정 조건/액션 디버깅
- 값 추적 (HP, Poise 등)

### BT 디버그 패널 ✨ 추천!

**장점**:
- 정리된 UI
- 히스토리 보관 (최대 50개)
- 필터링 기능
- 상세 로그 선택적 표시
- 내보내기 기능

**단점**:
- 초기 설정 필요

**사용 시점**:
- 일반적인 BT 테스트
- 패턴 분석
- QA 테스트
- 밸런싱

---

## 확장 가능성

### 향후 추가 가능 기능

1. **그래프 시각화**
   - 확률 변화 그래프
   - 턴별 추이

2. **로그 필터 고급화**
   - 특정 BT만
   - 특정 Entry만
   - 날짜/시간 범위

3. **통계**
   - Entry별 실행 횟수
   - 평균 확률 변경
   - 가장 많이 실행된 액션

4. **UI 인터랙션**
   - 히스토리 클릭으로 상세 전환
   - 드래그로 스크롤
   - 더블 클릭으로 즐겨찾기

---

## 요약

### 구성 요소
1. **요약 정보**: 현재 상태 한눈에
2. **실행 히스토리**: 최근 N개, 간략 요약
3. **상세 로그**: 선택한 로그의 모든 정보
4. **컨트롤**: 클리어, 일시정지, 내보내기
5. **필터**: Enemy/Player, 매칭만

### 핵심 기능
- ✅ 정리된 UI (Console 로그 대신)
- ✅ 히스토리 보관 (최대 50개)
- ✅ 필터링 (Enemy/Player, 매칭)
- ✅ 상세 선택적 표시
- ✅ 로그 내보내기

### 효과
- ⚡ BT 문제 즉시 파악
- 🎯 패턴 분석 용이
- 📊 테스트 데이터 수집
- 🐛 디버깅 효율 향상

---

**문서 버전**: 1.0  
**작성일**: 2025년 10월 14일  
**관련**: BTDebugPanel.cs, BTLogHistory.cs, BTLogger.cs

