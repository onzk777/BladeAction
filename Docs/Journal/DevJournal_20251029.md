# 개발 일지 - 2025년 10월 29일

## 📋 오늘의 목표
- ActionCommand 장착 시스템 구현 준비
- 시스템 구현 명세서 및 계획서 문서화
- (가능하면) 간단한 구현 시작

---

## ✅ 진행 상황

### 1. 시스템 설계 결정
- **검술을 아이템으로 관리** 방식 채택
- 기존 인벤토리 시스템(CharacterInventory) 활용
- 별도 시스템 구현 대신 통합 방식으로 결정
- 코드 재사용 및 일관성 확보

### 2. ActionCommand 장착 시스템 구현 명세서 작성 완료
**파일**: `Docs/Design/Item/ActionCommand_장착_시스템_구현_명세서.md` (1,070줄)

**주요 내용**:
- 시스템 개요 및 설계 철학
- 데이터 구조 확장 (ItemType, EquipmentSlotType, Item 클래스)
- CharacterInventory 확장 내용
- UI 시스템 설계 (ActionCommandEquip)
  - PPT 기반 레이아웃 설계
  - 좌측: 유파 정보 + 장착 슬롯 4개
  - 중앙: 검술 목록 + 상세 정보 (토글)
  - 우측: 캐릭터 스테이터스

**핵심 결정사항**:
- 검술 슬롯 4개 (ActionSlot1~4)
- 유파 해제 시 유파 출처 검술 자동 해제
- 장착 버튼 클릭 방식 (드래그앤드롭 추후 확장)
- 4개 슬롯 가득 참 시 4번 슬롯 자동 대체

### 3. ActionCommand 장착 시스템 구현 계획서 작성 완료
**파일**: `Docs/Design/Item/ActionCommand_장착_시스템_구현_계획서.md` (1,218줄)

**구현 계획 (7 Phase)**:
- **Phase 1**: 데이터 구조 확장 (1~2시간)
- **Phase 2**: 인벤토리 시스템 확장 (2~3시간)
- **Phase 3**: Character 클래스 정리 (1시간)
- **Phase 4**: 검술 아이템 생성 도구 (1~2시간)
- **Phase 5**: UI 구현 (4~6시간) - ActionCommandEquipUI
- **Phase 6**: CSV 통합 (1~2시간, 선택)
- **Phase 7**: 테스트 및 디버깅 (2~3시간)

**총 예상 시간**: 12~16시간 (3일 정도)

### 4. UI 설계 완료
- UI 이름: **ActionCommandEquip** (시스템 이름과 통일)
- 컴포넌트 분리: 7개 스크립트
  - ActionCommandEquipUI (메인)
  - EquippedStyleInfoPanel
  - EquippedActionSlotsPanel / EquippedActionSlotUI
  - ActionListPanel / ActionCommandSlotUI
  - ActionCommandDetailPanel
- Unity UI 작업은 사용자 직접 수행 예정

---

## 📌 다음 작업 계획

### 우선순위 1: Phase 1 구현 시작
- ItemType에 ActionCommand 추가
- EquipmentSlotType에 ActionSlot1~4 추가
- Item 클래스에 actionCommandData 필드 추가
- EquipmentSlot 검증 로직 확장

### 우선순위 2: Phase 2 진행
- CharacterInventory 슬롯 초기화 수정 (검술 슬롯 4개 추가)
- UnequipAllStyleActions() 구현
- 편의 메서드 추가 (GetAcquiredActions, GetEquippedStyleActions 등)

### 나중에 고려할 사항
- 검술 아이템 자동 생성 도구 (Phase 4)
- UI 구현 (Phase 5) - 스크립트만, Unity 작업은 별도
- CSV 통합 (Phase 6, 선택사항)

---

## 💡 배운 점 / 메모

### 설계 결정 과정
1. 처음에는 별도 ActionCommandInventory 시스템 고려
2. 검술도 본질적으로 "획득 → 보유 → 장착 → 사용" 흐름
3. 기존 인벤토리 시스템과 동일한 패턴
4. **통합 방식 채택** → 코드 재사용, 일관성, 유지보수 용이

### UI 이름 변경 과정
- SwordArtSetting → SwordArtEquip → **ActionCommandEquip**
- 시스템 이름과 일관성 유지가 중요

### 참고한 문서
- `아이템_시스템_명세서.md` - 기존 인벤토리 구조
- `아이템_시스템_구현_계획서.md` - UI 구현 방식 참고
- `인벤토리_Prefab_생성_가이드.md` - Unity UI 작업 참고용

---

## 📊 진행률

```
[====================] 100% - 문서화 (명세서, 계획서)
[                    ]   0% - 구현
```

**문서 작성**: ✅ 완료  
**코드 구현**: 🚧 대기 중 (Phase 1부터 시작 예정)

---

## 🔜 다음 세션 시작 시 할 일
1. Phase 1 (데이터 구조 확장) 구현 시작
2. 컴파일 오류 수정 및 검증
3. Phase 2로 진행

---

**작성 시각**: 2025-10-29 (진행 중)  
**다음 세션**: Phase 1 구현















