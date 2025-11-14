# 개발 일지 - 2025년 11월 14일

**작업 주제**: CombatSessionSystem 일반화 리팩토링 1단계  
**작업 시간**: 금일  
**상태**: 진행 중  

---

## 오늘 계획 (ToDo)
- [ ] `CombatManager` 및 관련 매니저에서 `isPlayer` 전제 로직 전수 조사 → 슬롯/팀 기반 대체 설계
- [ ] `PerformTurn` / `RunCombat`를 `CombatantSlot` 중심으로 리팩토링하여 공격자·방어자 컨텍스트 일원화
- [ ] 확률 리셋·입력·HUD 업데이트를 슬롯 기반 공통 API로 재정비하고 NPC 대 NPC 시나리오까지 검증
- [ ] TeamA=Player vs TeamB=NPC / 양쪽 NPC / TeamB=Player 시나리오 회귀 테스트 체크리스트 초안 작성

---

## 참고 메모
- 2025-11-08 일지 메모: `isPlayer` 의존 구간을 우선 추출하고, 턴 컨텍스트를 슬롯 기준으로 재구성할 것.
- 리팩토링 후 UI 이벤트 연결은 사용자 확인 절차를 기다릴 것.
- Spine 애니메이션 타임라인은 초 단위 기준 유지.
- `CombatCharacterManager.CharacterType`의 `Player/Enemy` 명칭은 레거시 `PlayerCharacter`/`EnemyCharacter` 구조 의존 때문에 일시 유지 중. 팀/슬롯 기반 구조 안정화 후 `ControlType` 등 중립 용어로 교체하고 슬롯 정보와 분리할 것. (후속 리팩토링 TODO)

