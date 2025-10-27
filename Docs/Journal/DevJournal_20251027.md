# 개발 일지 - 2025년 10월 27일

[세션 1]

## 오늘 작업 수집 (정리 전 원자료)

- ItemDetailPanel UI 표시 개선
  - 설명/스탯 토글이 실제 상위 컨테이너를 활성/비활성하도록 구조 변경
    - 추가 필드: `statsContainer`, `descriptionContainer`
  - 스탯 동적 생성 도입 (프리팹 `ItemDetail_StatInfo` 기반)
    - 추가 필드: `statInfoItemPrefab`
    - 내부: `EnsureStatLineCount`, `HideAllStatLines`로 풀 기반 재사용
  - 스탯 표시 범위 확장 및 규칙 정비
    - 추가 표기: `blockPoiseConsumption`, `parryPoiseConsumption`, `parryPoiseAttackPower`
    - 0 값은 숨김, 음/양수만 표시, 부호 포함
    - 퍼센트 계열(막기 효율/쳐내기 효율/피해 감소): 0.0~1.0f → 0~100%로 표시 (정수)
      - DB가 0~100 저장 케이스에 대한 자가 정규화는 추후 일괄 정리 예정(현 상태 유지)
  - 토글 버튼 노출 조건
    - `useStatTable=false` 또는 `statTableKey` 비어있을 때 토글 UI 숨김
  - 인스펙터 연결 포인트 정리
    - `statsContainer` ← `ItemStatsInfo`
    - `descriptionContainer` ← `ItemDescription`
    - `statInfoItemPrefab` ← `ItemDetail_StatInfo`
    - 기존 토글 버튼/텍스트 참조는 유지

- InventoryUI 자동 스크롤 규칙 구현/개선
  - 스크롤 연결 필드 추가: `itemScrollRect`
  - 선택 슬롯 클릭 시 EndOfFrame 이후 레이아웃 확정 → 가시성 검사 후 스크롤
  - 스크롤 트리거 조건: 뷰포트 밖에 있을 때만 수행
  - 스크롤 양: “가장 짧은 이동”으로 타겟을 뷰포트 안으로 들여오기
    - 위로 벗어남: 타겟 상단이 뷰포트 상단 안으로 들어오도록 최소 이동
    - 아래로 벗어남: 타겟 하단이 뷰포트 하단 안으로 들어오도록 최소 이동
  - 과스크롤/부분 노출 오판 방지를 위한 tolerance 적용(2px)
  - 과거 시도(행 스냅/톱-바텀 정렬)는 폐기하고 최소 이동 규칙으로 단순화

- 데이터/자산 확인 메모
  - `ItemTable.asset`의 `statDatabase` GUID가 `StatDB.asset.meta`와 일치 확인 (연결 정상)
  - `itm_weapon_test2` 테스트 기준, 스탯 표시는 UI 토글/컨테이너 처리 수정으로 해결 경과 확인
  - 에셋 키 변경은 수행하지 않음(표시 정책 내에서 처리)

- 테스트/검증 포인트 기록
  - 설명↔스탯 토글 시 컨테이너 단위 가시성 전환 확인
  - 스탯 라인 동적 생성/재사용 시 누수 없이 갱신 확인
  - 퍼센트 항목 표시: +정수%/−정수% 표기, 0은 미표시
  - 자동 스크롤: 디테일 패널 온/오프에 따른 그리드 높이 변화 후에도 최소 이동으로 가시화


[세션 2]

## 오늘 작업 수집 (정리 전 원자료)

- Combat Stats 시스템 설계 문서 초안 작성/추가
  - `Docs/Design/CombatStatus/CombatStats_System_Design.md`
- 크리티컬 확률/배율 내부 단위 전환 및 UI 반영
  - CharacterData: `baseCritChance(float 0~1)`, `baseCritMultiplier(float)` 추가
  - Combatant: `IsCriticalHit()` → `Random.value < CritChance`, `CalculateCriticalDamage()` → `* CritMultiplier`
  - 마이그레이션 스크립트: `Tools > Migration > Convert Crit % to Ratio (CharacterData)` 추가
  - UI: `CombatStatusDisplay`에서 `CritChance`를 `%`로 표시 (기존 `Crit` 참조 제거)
- 전투 스탯 표준 구조/매니저 토대 구축
  - `CombatStats`(struct) 추가, 내부 계산 표준화
  - `StatLimitRules`(ScriptableObject) 추가 → 이후 고정 스키마(MinMax)로 구조 전환 완료 (`TryGetRange` 제공)
  - `StatLimiter` 추가: `ClampAll`, `Clamp(statKey, ...)`
  - `StatsCalculationManager` 추가: base + Σequipment 합산, Clamp, `GetEffectiveATK`
  - `CombatManager`: 피해량 계산에 `GetEffectiveATK` 연동
  - 임시 테스트: `InventoryProvider`, `TemporaryEquipmentApplier` 추가 (전/후 ATK 로그)
  - API 업데이트: `FindObjectOfType` → `FindFirstObjectByType`로 경고 제거
- 에디터 전역 룰(StatLimitRules) 적용
  - 런타임 Attribute: `StatLimitAttribute`
  - 커스텀 드로어: `StatLimitDrawer`
    - 룰 있을 때만 슬라이더 표시, 비율형(0~1) 우측 `%` 미리보기 표시
  - `EquipmentStats`의 `[Range]` 제거, `[StatLimit("...")]` 부착 (`blockEfficiency`, `parryEfficiency`, `damageReduction`, `poise` 등)
  - `StatDatabase.OnValidate`: 전역 룰 기반 Clamp 적용
  - `StatLimitRulesEditor`: 에셋 인스펙터에서 비율형 `%`, 배율형 `x` 미리보기
- 오류/경고 처리
  - `CombatStatusDisplay`: `Crit` → `CritChance`로 교체
  - `StatDatabase`: `TryGet` → `TryGetRange`로 교체
  - 권장 API로 경고 해결(FindFirstObjectByType)

## 다음에 이어서 진행 요약 (미완료 항목)

- StatsCalculationManager 확장: CombatStats 전 항목 합산/Clamp 반환 + Combatant 커밋 API
- Combatant 연동: 최종 합산 직후 Clamp 결과를 런타임 스탯으로 커밋(MaxHP 선행/HP [0..MaxHP] 특례)
- 인벤토리 주입: Combatant가 `CombatantInventory`를 직접 보유/주입(임시 Provider 제거)
- UI 연동 확대: DR/비율형(%), 배율형(x표시) 등 표준 출력 통일
- StatLimitRules 값 확정: ratio(0~1), multiplier, 정수 범위 최종 합의(에셋 반영)
- 검증 메뉴: Tools > Stats > Recalculate & Clamp All(로그 요약)
- 구형 필드 제거: `baseCrit`, `baseCritRatio` 삭제(마이그레이션 확인 후)

[세션 3]

## 오늘 작업 수집 (정리 전 원자료)

- 아이템 타입 참조 구조 전환: SO 참조 → ItemTypeDatabase 인라인 엔트리
  - `WeaponTypeEntry/ArmorTypeEntry/AccessoryTypeEntry` 추가, Getter 반환 타입 교체
  - `Item.cs`의 `Get*Type` 반환 타입 교체
  - `ItemTypeDatabase` OnValidate 자동 마이그레이션 추가
  - `ItemTypeDatabaseValidator` 검증/중복키 수정 도구 추가
- 검술 유파 참조 구조 전환
  - `SwordArtStyleDatabase` 추가 (key↔SO 매핑)
  - `Item.swordArtStyle` → `swordArtStyleKey` + `GetSwordArtStyle`
  - `EquippedSwordArtStyleUI` 키 기반 조회로 수정
  - `ItemDatabaseEditor` 인스펙터 필드 교체(`swordArtStyleKey`)
- CSV Import/Export 안정화 및 포맷 확장
  - Read: FileShare.ReadWrite + 재시도
  - Export: 재시도 + 대체 파일명 저장
  - CSV 컬럼 추가: `SwordArtStyleKey` (Reader/Mapper 반영)
- 기타
  - 마이그레이션 도구 `ItemTypeMigrationTool` 추가(선택 사용)
  - `DatabaseKey` 드롭다운 기존 필드명 유지로 호환 확인

- 성능/UX 개선 (에디터/드로어 공통 규칙 적용)
  - DatabaseKeyDrawer: 전역 5초 TTL 캐시 적용(이미 구현), 캐시 초기화 메뉴 추가
  - StatLimitDrawer: 전역 룰/키별 범위 5초 TTL 캐시 추가, Clear 메뉴 추가
  - StatDB/ItemDB 인라인 에디터 스크롤 끊김 완화(반복 리플렉션/검색 억제)


## 오늘 작업 정리

- 인벤토리/아이템 UI
  - ItemDetailPanel: 컨테이너 토글(설명/스탯), 스탯 동적 생성, 표기 규칙(0 숨김/부호/%) 정리
  - EquippedSwordArtStyleUI: 유파 키 기반 SO 조회(Resources 내 DB 자동 검색/캐시), 하위 검술 리스트 표시 복구
  - 자동 스크롤: 뷰포트 밖에서만 최소 이동 규칙으로 보정(EndOfFrame 이후 계산)

- 데이터/CSV 파이프라인
  - Export 헤더 확장: SwordArtStyleKey 포함
  - CSV Reader 유연화(9~10컬럼 지원) 및 Merge Import 도입(빈 값=미변경, Unity Asset 참조 보존)

- 에디터 성능
  - DatabaseKeyDrawer/StatLimitDrawer에 5초 TTL 캐시 적용 및 캐시 초기화 메뉴 제공
  - StatDB/ItemDB 인라인 편집 시 리플렉션/검색 빈도 축소로 스크롤 끊김 완화


## 내일 작업 계획 (To-Dos)

### 퍼센트 비율 데이터 정규화 및 마이그레이션
- [ ] 잘못된 0~100 저장 케이스 탐지: StatDB/CharacterData 내 비율형 필드 전수 검증
- [ ] 0~100 → 0.0~1.0 변환 마이그레이션 스크립트: 발견 시 자동 변환/백업/로그 제공
- [ ] 표시 로직 통일: ItemDetailPanel/CombatStatusDisplay에서 "자동 판별" 제거, 항상 value×100→정수% 표시
- [ ] StatLimitRules 강제 적용: 비율형 키에 0~1 범위 강제(에디터 입력 차단), OnValidate Clamp

### StatCalculationManager 확장 및 전투 스탯 집계 완성 (세션 2 연계)
- [ ] 집계 파이프라인 구현: baseStats(Character/Combatant) + ΣEquipment(Inventory→GetStats) → 합산 → Clamp
- [ ] EquipmentStats ↔ CombatStats 매핑 정의: 9개 항목(ATK/Block/Parry/생존) 1:1 대응 확인
- [ ] API 추가
  - `RecalculateAll(Combatant combatant)`: 인벤토리 장비 전체 합산→Clamp→결과 반환
  - `GetEffectiveStat(string statKey)`: 키 기반 개별 스탯 조회
  - `GetEffectiveStats()`: CombatStats 전체 반환
- [ ] Combatant 커밋 로직: RecalculateAll 후 CombatStats를 런타임 스탯에 반영
  - MaxHP 선반영 후 HP를 [0..MaxHP] 보정(현재 HP 손실 없도록 비율 보존)
  - 기타 스탯(ATK/Poise/효율 등) 즉시 커밋
- [ ] 이벤트 훅 연결: ItemEvents.OnItemEquipped/Unequipped에서 RecalculateAll 자동 호출
- [ ] 인벤토리 주입: Combatant에 `CombatantInventory` 필드 추가, 임시 Provider 제거
- [ ] 더티 플래그/캐싱 도입: 잦은 재계산 방지(장비 변경 시에만 Dirty=true)
- [ ] 테스트 시나리오: 장착/해제/중첩, 스탯 테이블 on/off, 극단값 Clamp, 이벤트 연동 확인

### UI/에디터/CSV 개선
- [ ] StatDatabase 인라인 에디터 최적화: 리스트 가상화/페이지/검색 도입(렌더링 최소화)
- [ ] CSV Import 옵션 추가: "빈 값은 필드 클리어" 토글(기본 OFF)
- [ ] CSV 머지-업데이트 통합 테스트: 추가/갱신/보존(아이콘/appearance) 시나리오 검증
- [ ] InventoryUI 스크롤 엣지 케이스 추가 점검(그리드 리사이즈/동적 슬롯 증감 시)
- [ ] UI 연결 재확인: ItemDetailPanel과 EquippedSwordArtStyleUI 인스펙터 참조(필수 항목) 점검 가이드 반영

