# 개발 일지 - 2025년 11월 8일

**작업 주제**: 전투 시스템 일반화 1단계 (CombatSessionSystem)
**작업 시간**: 당일
**상태**: 🔄 진행 중

---

## 📚 현재 개발 중인 시스템 개요
- **CombatSessionSystem** 전반을 “Player vs Enemy” 고정 구조에서 “Character vs Character”로 일반화하는 장기 작업 진행 중
- 관련 설계 문서
  - `Docs/Design/BT/BehaviorTree 시스템 구현 계획서.md` – 전투 AI/BT 구조 및 진행도 기준 (현재 약 40% 달성)
  - `Docs/Design/BT/BT_시스템_구현_진행상황.md` – BehaviorTree 및 전투 진행 모듈 세부 TODO (Team/Slot 일반화 항목 진행 중)
  - `Docs/Design/전투 기반 시스템/PerfectTimingGuide_구현_요약.md` – 타이밍/발사체/입력 시퀀스 가이드라인
- 전체 CombatSessionSystem 진행도: 약 35% (UI 일반화 1단계 완료, 컨트롤러/턴 로직 일반화는 착수 예정)

## ✅ 오늘 진행 내용
- Player/Enemy 전용 UI 스크립트를 제거하고 `TeamActionSelectUI` 하나로 통합
  - 버튼 컨테이너/CanvasGroup 자동 탐지, 인스펙터 의존 최소화
  - `ActionCommandSelectionManager`를 팀별 UI 관리 방식으로 정리
- NPC vs NPC 전투 테스트 중 발견된 NullReference 예외 대응
  - 입력 핸들러가 없는 경우 `CombatManager.RunCombat()` 및 `PerformTurn()`에서 null-safe 처리
  - 프리팹에서 등록된 UI 인스턴스를 잘못 참조하는 문제 방지 로직 추가 (씬 인스턴스만 등록)

## ⚠️ 발견 이슈
- 전투 턴 로직 전반에 `isPlayer`, `playerController`, `enemyController` 등 플레이어 고정 전제가 여전히 잔존
- NPC vs NPC 시나리오에서 발사체 타이밍 처리 시 `attackerInputHandler`가 null이면 fallback 로직이 정상적으로 작동하지 않아 애니메이션이 겹치는 현상 관찰

## 🔜 다음 작업 메모
- `CombatManager`/`CombatCharacterManager`/입력 핸들러 전역에서 `isPlayer` 기반 로직 전수 조사
- 턴 진행(`PerformTurn`, `RunCombat`)을 슬롯/팀 기반 컨텍스트로 리팩토링 (공격자/방어자 정보를 `CombatantSlot`으로 관리)
- 입력/애니메이션/HUD 업데이트가 팀 구성과 무관하게 동작하도록 공통 API 설계
- TeamA=Player, TeamB=NPC / 양쪽 NPC / TeamB=Player 등 주요 시나리오 회귀 테스트 계획 수립

## 📝 내일 이어서 시작할 프롬프트 제안
```
어제 CombatManager 전반의 플레이어 전용 로직(isPlayer, playerController 등)을 팀/슬롯 기반 구조로 전환하려 했다. 우선 PerformTurn/RunCombat에서 공격자·방어자를 CombatantSlot을 통해 다루도록 리팩토링하고, 입력/애니메이션/UI 갱신 로직이 팀 구성을 가리지 않도록 점검하고 싶다. 어제 조사한 isPlayer 의존 위치 목록부터 보여달라.
```

