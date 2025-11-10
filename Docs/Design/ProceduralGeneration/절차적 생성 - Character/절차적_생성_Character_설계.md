# Character 절차적 생성 시스템 설계

> 작성일: 2025-11-05
> 상태: 개념 설계 완료, 구현 대기

---

## 🎯 배경 및 목적

### 문제
- 게임에 등장할 NPC 수: **천 명 이상**
- CharacterDatabase에 모두 수동 등록 불가능
- 하지만 각 NPC는 고유한 인생과 데이터를 가져야 함

### 목표
- **Unique NPC**: 소수의 중요 인물 (Database 수동 정의)
- **Procedural NPC**: 대다수 일반 인물 (Template 기반 자동 생성)
- **생성 이후에는 완전히 동등한 존재**

---

## ⚠️ 중요한 관점 정리

### ❌ 잘못된 생각
```
Unique NPC = 영속적, 고유한 라이프
Procedural NPC = 임시적, 재생성 가능, Delta만 저장
```

### ✅ 올바른 생각
```
생성 방법만 다를 뿐, 생성 이후에는 완전히 동등!

Unique NPC:
  Database → CharacterInitData → Character

Procedural NPC:
  Template + Seed → CharacterInitData → Character
                    ↓
            (생성 이후는 동일)
```

**Procedural NPC도:**
- ✅ 고유한 instanceId
- ✅ 자신의 라이프 사이클 (스케줄, 행동, 위치)
- ✅ 관계도, 성장, 능력 변화
- ✅ 영속적 데이터 (전체 상태 저장)
- ✅ Unique NPC와 완전히 동등

---

## 🏗️ 시스템 구조

### 1. CharacterTemplate (ScriptableObject)

범위형 데이터로 NPC 생성 규칙 정의

```
CharacterTemplate
├─ 스탯 범위 (HP 50~100, ATK 10~20 등)
├─ 장비 풀 (이 중 하나 무작위)
│   ├─ 무기 풀
│   ├─ 갑옷 풀
│   └─ 장신구 풀
├─ 검술 풀 (가능한 유파 리스트)
├─ 외형 풀 (캐릭터 모델 리스트)
└─ 이름 생성 규칙 (성씨 + 이름 조합)
```

**템플릿 예시:**
- `CommonSwordsman_Tier1` (초보 검사)
- `CommonSwordsman_Tier2` (중수)
- `CommonSwordsman_Tier3` (고수)
- `Merchant` (상인)
- `Villager` (마을 주민)
- `FactionMember` (문파원)

### 2. ProceduralCharacterGenerator

Template + Seed를 입력받아 CharacterInitData 생성

```
입력: CharacterTemplate + Seed
  ↓
범위 내 무작위 스탯 생성
장비 풀에서 가중치 기반 선택
검술 풀에서 무작위 선택
이름 생성 규칙 적용
  ↓
출력: CharacterInitData (일반 Character와 동일한 구조)
```

**핵심: Seed의 역할**
- 같은 Template + 같은 Seed = 같은 CharacterInitData
- **생성 시에만 사용됨** (생성 이후는 의미 없음)
- 재현 가능성 보장

### 3. CharacterFactory (생성 통합 클래스)

두 가지 생성 경로 제공

```csharp
// 방법 1: Database에서 생성 (Unique)
Character CreateFromDatabase(string databaseEntryId);

// 방법 2: Template에서 생성 (Procedural)
Character CreateFromTemplate(string instanceId, CharacterTemplate template, int seed);
```

### 4. NonPlayerCharacterManager 확장

생성 방법 무관하게 모든 NPC를 동등하게 관리

```
GetCharacter(instanceId)
  ↓
1. 메모리에 있으면 반환
2. 없으면 디스크에서 로드 (세이브 파일)
3. 저장된 것도 없으면 최초 생성
   - Database에 있으면 → CreateFromDatabase
   - Procedural 패턴이면 → CreateFromTemplate
4. 생성 즉시 디스크에 저장
```

---

## 💾 메모리 및 세이브 시스템

### 메모리 관리: 언로드/리로드

**문제:**
- 천 명의 NPC를 동시에 메모리에? ✗

**해결:**
- 필요한 NPC만 메모리에 유지 (예: 최대 100명)
- 플레이어 주변, 최근 상호작용 NPC 우선
- 나머지는 디스크에 저장 후 언로드
- 필요 시 다시 로드

```
마을 입장 → 해당 마을 NPC 로드
마을 떠남 → 언로드 (디스크에 저장)
```

### 세이브 시스템

**모든 NPC (Unique/Procedural)를 동일하게 저장**

```json
{
  "instanceId": "NPC_Village_A_001",
  "characterName": "김철수",
  
  // 생성 메타데이터 (참고용)
  "creationSource": "Procedural",
  "templateId": "CommonSwordsman_Tier2",
  "generationSeed": 12345,
  
  // 전체 런타임 데이터
  "stats": { ... },
  "inventory": { ... },
  "actions": { ... },
  "worldPosition": { ... },
  "relationships": { ... },
  "schedule": { ... }
}
```

**핵심:**
- ✅ 전체 상태 저장 (Delta가 아님)
- ✅ Template/Seed는 디버그/참고용으로만
- ✅ 실제 데이터만이 진실

---

## 🎮 사용 시나리오

### 시나리오 1: 마을 입장

```
마을에 NPC 10명
├─ "UniqueNPC_Blacksmith" (Unique: 대장장이, 스토리 인물)
├─ "NPC_Village_A_001" (Procedural: 일반 주민 1)
├─ "NPC_Village_A_002" (Procedural: 일반 주민 2)
└─ ...

마을 입장 시:
- 각 NPC의 GetCharacter() 호출
- 이전에 만난 적 있으면: 저장된 상태 로드
- 처음 만나는 NPC면: 최초 생성
  - Unique: Database에서
  - Procedural: Template에서

마을 떠날 때:
- 모든 NPC 디스크에 저장 후 언로드
```

### 시나리오 2: NPC와 전투 후

```
NPC와 전투 → 부상 입음, 관계 악화, 무기 빼앗김
  ↓
현재 상태 디스크에 저장
  ↓
나중에 다시 만남
  ↓
저장된 상태 그대로 로드
  ↓
여전히 부상 상태, 관계 나쁨, 무기 없음!
```

---

## 🔑 핵심 설계 원칙

### 1. 생성은 다르지만, 존재는 동등
```
생성 방법 ≠ 존재의 가치
모든 NPC는 고유하고 영속적
```

### 2. Seed는 생성 시에만
```
Seed → 최초 생성 시 CharacterInitData 결정
생성 이후 → Seed는 의미 없음, 실제 데이터만 중요
```

### 3. 전체 상태 저장
```
Delta 추적? ✗ (복잡하고 버그 위험)
전체 저장/로드 ✓ (단순하고 안전)
```

### 4. 필요할 때만 메모리에
```
천 명 동시 로드? ✗
필요한 NPC만 메모리, 나머지는 디스크 ✓
```

---

## ⚙️ 구현 고려사항

### 1. instanceId 네이밍 규칙

```
Unique NPC:
- "UniqueNPC_Blacksmith"
- "UniqueNPC_Boss_001"

Procedural NPC:
- "NPC_{지역}_{그룹}_{번호}"
- 예: "NPC_Village_A_001"
- 예: "NPC_City_Merchant_042"
```

### 2. Template 설계

**Tier 기반 분류:**
- CommonSwordsman_Tier1~4
- Elite_Tier1~4

**역할/직업 기반:**
- Merchant (상인)
- Villager (마을 주민)
- Wanderer (떠돌이 검객)
- FactionMember (문파원)

**가중치 시스템:**
- 장비 풀: 아이템마다 등장 확률 가중치
- Tier가 높을수록 좋은 장비 확률 상승

### 3. 성능 최적화

**메모리 제한:**
- 최대 로드 개수 설정 (예: 100명)
- 우선순위 기반 언로드
  - 플레이어와의 거리
  - 최근 상호작용 시간
  - 중요도 (Unique > Procedural)

**세이브 파일 구조:**
```
SaveData/
├─ Characters/
│   ├─ UniqueNPC_Blacksmith.json
│   ├─ NPC_Village_A_001.json
│   ├─ NPC_Village_A_002.json
│   └─ ...
```

### 4. 디버그 기능

- 생성 메타데이터를 SaveData에 포함
  - 어떤 Template에서 생성됐는지
  - Seed 값은 무엇인지
- 디버그 패널에서 NPC 재생성 가능
- Template 수정 후 테스트 용이

---

## 📅 구현 로드맵

### Phase 1: 기반 구조 (1~2일)
- `CharacterTemplate` ScriptableObject 정의
- `ProceduralCharacterGenerator` 클래스 작성
- 기본 범위 생성 (스탯, 이름)

### Phase 2: 장비/검술 생성 (1~2일)
- `EquipmentPool` 시스템
- 가중치 기반 무작위 선택
- 검술 풀에서 선택 로직

### Phase 3: 통합 (1일)
- `CharacterFactory` 작성
- `NonPlayerCharacterManager` 확장
- 두 경로 통합 테스트

### Phase 4: 메모리 관리 (1일)
- 언로드/리로드 시스템
- 우선순위 기반 관리

### Phase 5: 세이브 연동 (세이브 시스템 구현 시)
- Procedural NPC 저장/로드
- 전체 상태 직렬화

---

## 🔗 관련 시스템

### 선행 필요
- ✅ Character 아키텍처 (완료)
- ✅ CharacterInitData 구조 (완료)
- ✅ NonPlayerCharacterManager (완료)

### 동시 개발 필요
- 세이브 시스템 (저장/로드)
- 월드 관리 시스템 (NPC 위치, 스케줄)

### 향후 연동
- NPC 월드 AI (스케줄, 행동 패턴)
- 관계도 시스템
- 대화 시스템
- 이벤트 시스템

---

## 📝 메모

### 고민 사항
1. **Unique와 Procedural의 경계**
   - 문파장은? 야간 무투장 챔피언은?
   - 기준: 이름 있는 스토리 인물 = Unique

2. **Template 개수**
   - 너무 적으면: 모든 NPC 비슷함
   - 너무 많으면: 관리 부담
   - 균형 찾기 필요

3. **생성 시점**
   - 게임 시작 시 전체 생성? ✗ (메모리 낭비)
   - 처음 만날 때 생성? ✓ (Lazy)
   - 지역 진입 시 해당 지역 NPC 일괄 생성? ✓

### 참고
- 귀곡팔황의 NPC 시스템 참고
- Seed 기반 절차적 생성 = 재현 가능성
- 생성 ≠ 존재 (생성 방법만 다를 뿐, 본질은 동등)

---

**작성자 노트:**
이 문서는 개념 설계 단계입니다. 실제 구현은 전투 세션 시스템 및 세이브 시스템 이후에 진행할 예정입니다.


