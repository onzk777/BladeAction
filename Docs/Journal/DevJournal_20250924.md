# 개발 일지 - 2025년 9월 24일

## 📋 오늘의 목표
발사체 기반 전투 시스템 구현 명세서에 따른 1단계 작업 진행
- Projectile 클래스 생성
- 기본 발사 및 이동 로직 구현
- 발사체 풀링 시스템 구현

## 🎯 작업 방식
- 수동적 포지션으로 사용자 검수 및 승인 후 진행
- 개발 스펙을 세분화하여 하나씩 구현
- 코드 검수 후 수정 요구 시 즉각 대응

## 📝 진행 상황
- [x] 1단계: 발사체 시스템 기본 구조 구현
  - [x] Projectile 클래스 생성 및 리팩토링
  - [x] 기본 발사 및 이동 로직 구현
  - [x] 발사체 풀링 시스템 구현
- [x] 2단계: 이중 Collider 시스템 구현
  - [x] CharacterHitSystem 클래스 생성
  - [x] PerfectInputArea, CharacterHitBox 설정
  - [x] Collider 간 상호작용 로직 구현
- [x] 3단계: 기존 시스템과의 연동
  - [x] ActionCommandData에 발사체 프리팹 필드 추가
  - [x] CombatManager에 ProjectileManager 연동
  - [x] 발사체 생성 로직 구현

## 🔍 현재 상태
- 발사체 기반 전투 시스템 기본 구조 완성
- Projectile, ProjectileManager, CharacterHitSystem 구현 완료
- CombatManager 발사체 생성 로직 구현 완료
- 태그 시스템 및 Collider2D 설정 완료

## ✅ 완료된 작업
1. **Projectile.cs 생성 및 리팩토링**
   - Combatant 참조 제거, Controller 기반으로 변경
   - 불필요한 글로벌 변수 제거 (sourceCommand, hitIndex, isFromPlayer)
   - 속도 Curve 시스템 추가 (AnimationCurve 기반 감가속 지원)
   - 태그 기반 충돌 감지 (PerfectInputArea, CharacterHitBox)

2. **ProjectileManager.cs 생성**
   - 오브젝트 풀링으로 성능 최적화
   - 동적 확장 및 메모리 관리

3. **ActionCommandData.cs 수정**
   - projectilePrefab, projectileScale 필드 추가
   - 발사체 프리팹 연결 및 크기 설정 지원

4. **CombatManager.cs 수정**
   - ProjectileManager 연동
   - 완벽 입력 성공/실패 시점에 발사체 발사
   - 발사체 발사 상태 추적 (projectileLaunched 배열)

5. **CharacterHitSystem.cs 생성**
   - PerfectInputArea, CharacterHitBox Collider2D 관리
   - 발사체 충돌 감지 및 이벤트 처리
   - Player/Enemy 오브젝트에 컴포넌트 추가

6. **태그 시스템 설정**
   - Projectile, PerfectInputArea, CharacterHitBox 태그 추가
   - Collider2D IsTrigger 설정 완료

## 📊 작업 시간
- 1단계: 1일 (완료)
- 2단계: 1일 (완료)
- 3단계: 1일 (완료)

## 🎯 다음 단계
- DefenderInputHandler 발사체 기반 리팩토링
- 턴 로직을 발사체 완료 기준으로 재구성
- 전체 시스템 통합 테스트 및 디버깅


