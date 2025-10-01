# 개발 일지 - 2025년 10월 1일

## 작업 개요
- **주제**: NPC 전투 패턴 - Behavior Tree 시스템 설계 및 구현 시작
- **목표**: Phase 1 (데이터 구조 확장) 완료

---

## 진행 사항

### 1. BT 시스템 설계 문서 작성 완료 ✅

#### 1.1 시스템 기획서 작성
**파일**: `Docs/Design/BT/BehaviorTree_System_Design.md`

- BT 시스템 전체 아키텍처 정의
- CharacterData 중심 설계 (GlobalConfig 제외)
- BT 구조: Condition(s) + Action(s) 쌍의 리스트
- 순차 평가 (리스트 인덱스 = 우선순위)
- 턴 시작 시 1회 평가
- Priority 기반 중복 처리

#### 1.2 노드 사양 문서 작성
**파일**: 
- `Docs/Design/BT/Condition_Node_List.md`: Condition Node 상세 사양
- `Docs/Design/BT/Action_Node_List.md`: Action Node 상세 사양
- `Docs/Design/BT/Composite_Node_Manual.md`: Composite Node 사용법

**주요 노드 타입**:
- Condition: HP 비교, 자세 비교, 턴 타입, 턴 수
- Action: 확률 조정, 강제 행동, 검술 선택, 행동 비활성화
- Composite: Sequence(AND), Selector(OR)

#### 1.3 구현 계획서 작성
**파일**: `Docs/Design/BT/Implementation_Plan.md`

**5단계 구현 계획**:
- Phase 1: 데이터 구조 확장 (1-2일) ← **오늘 진행**
- Phase 2: BT Core 시스템 (3-4일)
- Phase 3: BT 실행 및 AI 연동 (2-3일)
- Phase 4: 디버깅 및 최적화 (1-2일)
- Phase 5: Additional Turn Duration (추후)

---

### 2. Tag 시스템 설계 개선

#### 문제점
- 초기: String 기반 태그 → 오타 위험, 관리 어려움
- Enum 기반 제안 → Inspector에서 수정 불가

#### 해결책: ScriptableObject 기반 Tag 시스템
**구현 내용**:
- `ActionCommandTagList.cs`: 중앙 집중식 태그 관리
- Inspector에서 태그 추가/수정/삭제 가능
- Custom Editor로 Dropdown 지원
- 색상 지원으로 시각화

**장점**:
- ✅ Inspector 친화적
- ✅ 중앙 관리
- ✅ 오타 방지
- ✅ 타입 안정성

---

## Phase 1 구현 계획

### 목표: 데이터 구조 확장 (기반 작업)

#### 1.1 CharacterData 확장
- [ ] NPC 행동 확률 데이터 구조 추가
  - `NPCBehaviorProbabilities` 클래스 정의
  - 기본값 0.0f, Range(0f, 1f) 설정
- [ ] BT 리스트 추가
  - `List<BehaviorTreeData> behaviorTrees`

**필드 목록**:
```csharp
- attackPerfectRate: float (공격 성공률)
- parryPerfectRate: float (쳐내기 성공률)
- guardAttemptRate: float (막기 시도 확률)
- parryWhileGuarding: bool (막기 중 쳐내기 시도 여부)
- parryWhileGuardingRate: float (막기 중 쳐내기 성공률)
```

#### 1.2 ActionCommandTag 시스템 구현
- [ ] `ActionCommandTagList.cs` 생성
  - ScriptableObject 기반 태그 관리
  - TagEntry 클래스 (tagName, displayColor)
  - GetAllTagNames(), IsValidTag() 메서드
  
- [ ] `Resources/ActionCommandTagList.asset` 생성
  - 기본 태그 추가: "필살기", "원거리", "방어형", "빠른공격", "강공격"

- [ ] `ActionCommandData.cs` 확장
  - `List<string> tags` 필드
  - `HasTag(string tag)` 메서드

- [ ] `ActionCommandDataEditor.cs` 생성 (Custom Editor)
  - TagList Dropdown으로 태그 선택
  - 태그 추가/제거 버튼
  - 유효성 검증

#### 1.3 GlobalConfig 확장
- [ ] Default BT 참조 추가
  - `BehaviorTreeData defaultBehaviorTree`
  - CharacterData에 BT 없을 때만 사용

---

## 검증 체크리스트

### CharacterData
- [ ] Inspector에서 NPC 확률 설정 가능
- [ ] 기본값 0.0f로 초기화
- [ ] 기존 CharacterData 에셋 정상 동작

### ActionCommandTag
- [ ] ActionCommandTagList.asset 생성 및 태그 추가
- [ ] ActionCommandData Inspector에서 Dropdown 선택
- [ ] 태그 추가/제거 정상 동작
- [ ] TagList에 없는 태그 선택 불가
- [ ] 기존 ActionCommandData 에셋 정상 동작

### GlobalConfig
- [ ] Inspector에서 Default BT 할당 가능
- [ ] null 처리 준비

---

## 다음 작업 예정

### 오늘 (10월 1일)
1. CharacterData 확장 구현
2. ActionCommandTag 시스템 구현
3. GlobalConfig 확장 구현
4. Phase 1 검증 및 테스트

### 다음 (Phase 2 예정)
- BT Core 시스템 구현
- Condition/Action/Composite Node 클래스
- BehaviorTreeData ScriptableObject

---

## 메모 및 이슈

### 설계 변경 사항
1. **아키텍처**: GlobalConfig를 시스템 아키텍처에서 완전 제외
   - CharacterData 중심 설계
   - GlobalConfig는 비상용 Default BT만 보유

2. **Tag 시스템**: Enum → ScriptableObject
   - Inspector 친화성 증대
   - 런타임 수정 가능

3. **Priority 처리**: 음수 불가, 정수만 지원
   - 안전 장치로 사용
   - 기본적으로 중복 방지 디자인

### 주의사항
- CharacterData 확장 시 기존 에셋 호환성 유지
- Tag 시스템 Custom Editor는 권장 사항 (구현하면 편리함)
- GlobalConfig의 NPC AI 설정은 테스트용으로 유지

---

## Phase 1 구현 완료 ✅

### 구현된 파일 목록

#### 1. CharacterData.cs 확장
- [x] NPCBehaviorProbabilities 클래스 추가
- [x] npcBehavior 필드 추가 (기본값 0.0f)
- [x] behaviorTrees 리스트 추가

#### 2. ActionCommandTagList.cs 생성 (신규)
- [x] ScriptableObject 기반 태그 관리 시스템
- [x] TagEntry 클래스 (tagName, displayColor)
- [x] GetAllTagNames(), IsValidTag() 메서드
- [x] Singleton Instance 패턴 구현

#### 3. ActionCommandData.cs 확장
- [x] tags 리스트 추가 (List<string>)
- [x] HasTag(string tag) 메서드 추가

#### 4. ActionCommandDataEditor.cs 생성 (신규)
- [x] Custom Editor 구현
- [x] TagList Dropdown 지원
- [x] 태그 추가/제거 버튼
- [x] 색상 표시 기능
- [x] 유효성 검증 및 경고 메시지

#### 5. GlobalConfig.cs 확장
- [x] defaultBehaviorTree 필드 추가

#### 6. BehaviorTreeData.cs 생성 (Placeholder)
- [x] 임시 클래스 생성 (Phase 2에서 구현 예정)
- [x] ScriptableObject로 생성 가능하도록 설정

---

## 검증 및 테스트 결과

### Unity 에셋 생성 및 테스트 ✅

#### 1. ActionCommandTagList 에셋 생성 완료
- `Resources/ActionCommandTagList.asset` 생성
- 기본 태그 추가:
  - NormalAttack
  - PowerAttack
  - Ultimate
  - Defense

#### 2. BehaviorTreeData 컴파일 이슈 해결
- Unity Reimport로 컴파일 오류 해결
- .meta 파일 정상 생성 확인

#### 3. Custom Editor 개선 작업
**이슈**: ActionCommandData Multi-Object Editing 미지원
- **해결**: `[CanEditMultipleObjects]` 속성 추가

**이슈**: Tag Dropdown이 표시되지 않음
- **원인**: `DrawDefaultInspector()`가 tags 필드를 이미 그려서 Custom UI와 충돌
- **해결**: SerializedProperty를 순회하며 tags 필드만 제외하고 그리기

**이슈**: 추가된 태그 이름이 표시되지 않음
- **원인**: EditorGUILayout.LabelField 렌더링 문제
- **해결**: 
  - GUILayout.Label 사용
  - HelpBox로 태그 리스트 감싸기
  - 디버그 로그 추가
- **결과**: 태그가 `● Defense` 형태로 정상 표시 확인

#### 4. 최종 검증 완료

**ActionCommandTagList**
- [x] `Resources/ActionCommandTagList.asset` 생성 완료
- [x] 기본 태그 추가 완료 (NormalAttack, PowerAttack, Ultimate, Defense)
- [x] Inspector에서 태그 추가/수정/삭제 정상 동작

**ActionCommandData**
- [x] Inspector에서 "태그 관리" 섹션 표시 확인
- [x] Dropdown에 TagList의 태그들 정상 표시
- [x] "추가" 버튼으로 태그 추가 정상 동작
- [x] "현재 태그 목록"에 추가된 태그 이름 표시 (HelpBox 내부)
- [x] "제거" 버튼으로 태그 삭제 정상 동작
- [x] Multi-Object Editing 지원 확인 (여러 에셋 동시 편집)

**CharacterData**
- [x] "NPC AI 설정" 섹션 표시 확인
- [x] NPCBehaviorProbabilities 필드 정상 동작
- [x] "Behavior Tree" 섹션 표시 확인
- [x] behaviorTrees 리스트 정상 동작

**GlobalConfig**
- [x] "Behavior Tree" 섹션 표시 확인
- [x] defaultBehaviorTree 필드 정상 동작

**BehaviorTreeData**
- [x] Placeholder 클래스 컴파일 정상
- [x] ScriptableObject 생성 가능 (Create → BT → Behavior Tree)

---

## Phase 1 최종 완료 상태

### 구현 완료된 기능
1. ✅ CharacterData - NPC 행동 확률 시스템
2. ✅ ActionCommandTag - ScriptableObject 기반 중앙 관리
3. ✅ ActionCommandData - Tag 시스템
4. ✅ Custom Editor - Tag Dropdown 및 Multi-Edit 지원
5. ✅ GlobalConfig - Default BT 참조
6. ✅ BehaviorTreeData - Placeholder 생성

### 테스트 완료 항목
- Unity 컴파일 정상
- 에셋 생성 및 Inspector 표시 정상
- Tag 추가/제거/표시 모든 기능 동작 확인
- Multi-Object Editing 지원 확인

---

## 이슈 및 해결 과정

### 이슈 1: BehaviorTreeData 컴파일 오류
- **증상**: CS0246 오류 (형식을 찾을 수 없음)
- **해결**: Unity Reimport (Assets → Refresh)

### 이슈 2: Multi-Edit 미지원
- **증상**: 여러 ActionCommandData 선택 시 Inspector 비활성화
- **해결**: `[CanEditMultipleObjects]` 속성 추가

### 이슈 3: Tag Dropdown 미표시
- **증상**: tags 필드가 기본 Inspector에 이미 표시됨
- **해결**: SerializedProperty 순회로 tags 필드만 제외

### 이슈 4: 태그 이름 미표시
- **증상**: 태그 추가 후 목록에 이름이 보이지 않음
- **원인**: EditorGUILayout.LabelField 렌더링 이슈
- **해결**: GUILayout.Label + HelpBox 사용

---

## 다음 작업 계획

### Phase 2 준비 완료
- BT Core 시스템 구현 준비
- Condition/Action/Composite Node 클래스 구현 예정
- BehaviorTreeData 본격 구현 예정

---

**작성자**: AI Assistant  
**작업 시간**: 
- 문서 작성 및 설계: 약 2-3시간
- Phase 1 구현: 약 1시간
- Unity 테스트 및 디버깅: 약 1시간
**완료 시간**: 2025년 10월 1일  
**Phase 1 상태**: ✅ 완료  
**다음 목표**: Phase 2 - BT Core 시스템 구현
