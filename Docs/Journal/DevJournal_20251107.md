# 개발 일지 - 2025년 11월 7일

**작업 주제**: CombatSessionSystem 1단계 일반화 (NPC vs NPC 지원)
**작업 시간**: 당일
**상태**: 🔄 진행 중

---

## 📋 오늘의 목표
- TestScene에서 TeamA/TeamB 구성이 가능하도록 UI 개선
- CombatManager/CombatCharacterManager 컨트롤러 연결 구조를 팀 슬롯 기반으로 전환
- 전투 씬의 Player/NPC 프리팹 런타임 스폰 구조 설계 착수
- ActionSelect 등 전투 UI의 TeamA/TeamB 일반화 계획 수립
- Player vs NPC, NPC vs NPC 등 회귀 테스트 시나리오 정리

---

## ✅ 진행 현황

### 1. TeamA/TeamB 선택 UI 안정화 ✅
- `TestSceneManager`가 CharacterDatabase 초기화를 기다린 뒤 TeamA/TeamB 드롭다운을 채우도록 변경
- TeamA 토글 ON 시 플레이어 고정, OFF 시 NonPlayer 선택 가능하도록 UI 연동 완료

### 2. 슬롯 기반 컨트롤러 연결 ✅
- `CombatManager`가 리더 슬롯(TeamA/TeamB)을 조사해 플레이어/NonPlayer에 맞는 컨트롤러를 연결하도록 수정
- TeamA/TeamB 모두 NonPlayer일 때 `AIController` 런타임 생성 및 재사용
- 현재 공격자/방어자 컨텍스트 추적(`currentAttackerSlot` 등)으로 애니메이션/이펙트 호출 분기 정비
- **잔여작업**: isPlayerAttacker 네이밍 리팩토링 및 BT/디버그 UI 반영

### 3. 런타임 스폰 구조 설계 🔄
- Player/NPC 프리팹을 전투 시작 시 스폰하도록 `CombatManager`에 스폰 포인트 및 프리팹 참조 추가
- TeamB 배치 시 좌우 반전 처리
- 전투 재시작을 위한 Actor 비활성화/정리 루틴 포함
- **잔여작업**: 기존 CombatScene에 배치된 Actor 제거, 프리팹/스폰 포인트 에디터 설정 가이드 적용, 풀링 전략 검토

### 4. UI 일반화 준비 💤
- PlayerActionSelectUI / EnemyActionSelectUI → TeamAActionSelectUI / TeamBActionSelectUI 리네임 및 구조 재설계 예정
- HP Bar, Combat HUD 등도 슬롯/팀 기반으로 연동하도록 계획 수립 필요

### 5. 회귀 테스트 계획 💤
- 플레이어 vs NonPlayer (TeamA=Player, TeamB=NPC)
- NonPlayer vs NonPlayer (TeamA/B 모두 NPC)
- (향후) 플레이어가 TeamB에 배치되는 시나리오
- 각 시나리오별 컨트롤러/애니메이션/입력 정상 동작 확인 체크리스트 작성 예정

---

## 🔜 다음 단계
- TeamB 리더가 플레이어일 때 컨트롤러/입력/애니메이션이 정상 동작하도록 구조 보완
- 전투 Actor 런타임 스폰 시스템 구현 시작 (프리팹 배치, 좌우 반전 포함)
- UI(ActionSelect, HUD) 팀 기반 일반화 설계 및 착수
- 회귀 테스트 체크리스트 초안 작성 후 단계별 실행

---

## 📌 참고 메모
- CombatSessionSystem 구현 명세/계획서에 맞춰 Team/Slot 개념을 코드 전반에 반영해야 함
- `isPlayerAttacker` 등 기존 필드는 TeamA 고정 가정을 벗어나도록 리팩토링 예정
- 구조 변경에 따른 UI/씬 구성 변경(프리팹 런타임화)을 동시 추진해야 테스트 효율 확보 가능

