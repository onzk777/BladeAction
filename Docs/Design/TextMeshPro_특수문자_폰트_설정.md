# TextMeshPro 특수 문자 폰트 설정 가이드

## 문제 상황

BT 디버그 패널에서 **특수 유니코드 문자가 □□(네모)**로 표시됨

**문제 문자**:
- 박스 그림: `╔ ╗ ║ ╚ ═ ━ ─`
- 이모지: `⚔ 🛡`
- 기호: `► ✓ ✗ ⊘ ▶ ⏸`

**원인**: TextMeshPro 기본 폰트에 해당 문자가 없음

---

## 해결 방법

### 방법 1: Font Asset에 특수 문자 추가 (추천) ⭐

Unity에서 직접 폰트를 생성하여 필요한 문자만 포함시킵니다.

#### 1단계: Font Asset Creator 열기

```
Unity 메뉴: Window → TextMeshPro → Font Asset Creator
```

#### 2단계: 폰트 선택

**Source Font File**: 
- 한글 지원 폰트 선택 (예: NanumGothic, Noto Sans KR)
- `Assets/Fonts/` 폴더에 있는 `.ttf` 또는 `.otf` 파일

**Sampling Point Size**:
- Auto Sizing: 체크
- Font Size: 40-50 (권장)

**Padding**: 5

**Packing Method**: Optimum

#### 3단계: Character Set 설정

**Character Set**: `Custom Characters` 선택

**Custom Character List**에 아래 문자들 복사-붙여넣기:
```
╔╗║╚═╠╣╦╩╬─━│├┤┬┴┼
►▶◄◀▲▼✓✗⊘⏸⏵⏹
⚔🛡⚡🔥💥✨
```

**Character Sequence**: `Unicode Range (Hex)` 
- 추가로 이모지 범위 포함하려면:
  ```
  2600-26FF  (기호)
  2700-27BF  (장식 기호)
  1F300-1F9FF (이모지)
  ```

#### 4단계: Font Atlas 생성

1. **Generate Font Atlas** 버튼 클릭
2. 잠시 대기 (10-30초)
3. Atlas에 문자들이 보이는지 확인

#### 5단계: 저장

1. **Save** 버튼 클릭
2. 저장 위치: `Assets/Resources/Fonts/` (폴더 생성 필요)
3. 파일명: `YourFont_Special_SDF` (예: `NanumGothic_Special_SDF`)

#### 6단계: TextMeshPro에 적용

**BT 디버그 패널의 Text들에 적용**:
1. `SummaryText` 선택 → Inspector
2. Font Asset: 방금 생성한 폰트 선택
3. `HistoryText`, `DetailText`에도 동일 적용

---

### 방법 2: Fallback Font 사용

기본 폰트는 유지하고 특수 문자만 다른 폰트로 표시

#### 1단계: 특수 문자용 Font Asset 생성

위 방법 1과 동일하게 폰트 생성하되, **Custom Characters에 특수 문자만** 포함

#### 2단계: Fallback Font 설정

1. 기존 Font Asset 선택 (예: 한글 폰트)
2. Inspector → Fallback Font Assets
3. `+` 버튼 클릭
4. 특수 문자용 폰트 추가

**동작**:
- 기본 폰트에 문자 없음 → Fallback 폰트 검색
- Fallback에서 발견 → 표시

---

### 방법 3: Noto Sans KR 사용 (가장 쉬움) ⭐⭐⭐

Noto Sans KR 폰트는 **대부분의 특수 문자를 포함**합니다.

#### 1단계: Noto Sans KR 다운로드

**Google Fonts**:
- https://fonts.google.com/noto/specimen/Noto+Sans+KR
- Download Family 클릭

#### 2단계: Unity에 Import

1. 다운로드한 `.ttf` 파일을 `Assets/Fonts/` 폴더에 복사
2. Unity에서 자동 import

#### 3단계: Font Asset 생성

**Font Asset Creator**:
- Source Font File: `NotoSansKR-Regular.ttf`
- Font Size: 40
- Character Set: `Custom Characters`
- Custom Character List:
  ```
  ╔╗║╚═╠╣╦╩╬─━│├┤┬┴┼►▶◄◀▲▼✓✗⊘⏸⏵⏹⚔🛡
  ```

#### 4단계: 적용

BT 디버그 패널의 모든 Text에 적용

---

## 빠른 해결 (임시)

폰트 설정이 번거로우면 임시로 이모지만 교체:

```csharp
// BTDebugPanel.cs에서
string turnTypeIcon = log.isAttackTurn ? "[공격]" : "[방어]";  // ⚔ 🛡 대신
string matchIcon = log.foundMatch ? "[O]" : "[X]";            // ✓ ✗ 대신
```

---

## 추천 폰트

### 한글 + 특수 문자 지원

1. **Noto Sans KR** (Google Fonts) ⭐ 최고 추천
   - 한글 + 대부분 특수 문자
   - 무료, 상업적 사용 가능

2. **Malgun Gothic (맑은 고딕)** (Windows 기본)
   - 한글 + 일부 특수 문자
   - 윈도우에 기본 설치

3. **나눔고딕** (Naver)
   - 한글 전용 (특수 문자 적음)
   - Fallback 필요

---

## 이모지 문제

**이모지 (⚔ 🛡)**는 일반 폰트에 없을 수 있음

**해결책**:
1. **Noto Color Emoji** 폰트 사용 (Google)
2. **Sprite Asset** 사용 (TextMeshPro 기능)
3. **일반 문자로 대체**: `[공격]` `[방어]`

---

## 최종 추천

### Unity에서 바로 사용 가능한 방법:

**Window → TextMeshPro → Font Asset Creator**

**설정**:
```
Source Font File: NotoSansKR-Regular.ttf (다운로드 필요)
Font Size: 40
Character Set: Custom Characters
Custom Character List:
  ╔╗║╚═━─►▶✓✗⊘⏸

Generate Font Atlas → Save
```

**저장**: `Assets/Resources/Fonts/NotoSansKR_Special_SDF.asset`

**적용**: BT 디버그 패널의 모든 TextMeshPro에 설정

---

**시간**: 약 5분  
**효과**: 모든 특수 문자 정상 표시 ✨

