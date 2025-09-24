# 개발 일지 - 2025년 9월 24일

## 📋 오늘의 목표
발사체 기반 전투 시스템 구현 명세서에 따른 1-3단계 작업 진행 및 시스템 안정화

## 🎯 작업 방식
- 수동적 포지션으로 사용자 검수 및 승인 후 진행
- 개발 스펙을 세분화하여 하나씩 구현
- 코드 검수 후 수정 요구 시 즉각 대응

---

## 🌅 오전 작업: 발사체 시스템 기본 구조 구현

### 1단계: 발사체 시스템 기본 구조 구현 ✅
**목표**: Projectile 클래스 생성 및 기본 발사/이동 로직 구현

**완료된 작업**:
- **Projectile.cs 생성 및 리팩토링**
  - Combatant 참조 제거, Controller 기반으로 변경
  - 불필요한 글로벌 변수 제거 (sourceCommand, hitIndex, isFromPlayer)
  - 속도 Curve 시스템 추가 (AnimationCurve 기반 감가속 지원)
  - 태그 기반 충돌 감지 (PerfectInputArea, CharacterHitBox)

- **ProjectileManager.cs 생성**
  - 오브젝트 풀링으로 성능 최적화
  - 동적 확장 및 메모리 관리
  - 싱글톤 패턴 적용

- **ActionCommandData.cs 수정**
  - projectilePrefab, projectileScale 필드 추가
  - perfectProjectilePrefab 필드 추가 (완벽 입력 시 다른 발사체)
  - 발사체 프리팹 연결 및 크기 설정 지원

### 2단계: 이중 Collider 시스템 구현 ✅
**목표**: CharacterHitSystem 클래스 생성 및 Collider 간 상호작용 로직 구현

**완료된 작업**:
- **CharacterHitSystem.cs 생성**
  - PerfectInputArea, CharacterHitBox Collider2D 관리
  - 발사체 충돌 감지 및 이벤트 처리
  - Player/Enemy 오브젝트에 컴포넌트 추가

- **태그 시스템 설정**
  - Projectile, PerfectInputArea, CharacterHitBox 태그 추가
  - Collider2D IsTrigger 설정 완료
  - Rigidbody2D 설정 (Kinematic, Freeze Position/Rotation)

### 3단계: 기존 시스템과의 연동 ✅
**목표**: CombatManager에 ProjectileManager 연동 및 발사체 생성 로직 구현

**완료된 작업**:
- **CombatManager.cs 수정**
  - ProjectileManager 연동
  - 완벽 입력 성공/실패 시점에 발사체 발사
  - 발사체 발사 상태 추적 (projectileLaunched 배열)

---

## 🌆 오후 작업: 시스템 안정화 및 문제 해결

### 🚨 발생한 주요 문제들과 해결 과정

#### 문제 1: 중복 판정 문제
**발견 시점**: 오후 2시경, 발사체 기반 전투 시스템 테스트 중
**문제 상황**: 발사체가 CharacterHitBox에 충돌할 때마다 판정이 반복 발생
**근본 원인**: 
- 발사체가 계속 충돌하여 OnProjectileHit가 반복 호출
- 중복 판정 방지 로직 부재

**해결 과정**:
1. **1차 시도**: 발사체 즉시 소멸 → 사용자 반려 (발사체 소멸에만 집중)
2. **2차 시도**: 근본 원인 분석 → 중복 판정 방지 로직 구현
3. **최종 해결**: `hitJudgmentCompleted[]` 배열로 히트당 판정 한 번만 발생하도록 제어

**구현된 해결책**:
```csharp
// 히트당 판정 한 번만 발생하도록 추적
private bool[] hitJudgmentCompleted;
private int[] hitJudgmentCount;

public void OnProjectileHit(Projectile projectile)
{
    int hitIdx = projectile.hitIndex;
    
    // 중복 판정 방지
    if (hitJudgmentCompleted[hitIdx])
    {
        Debug.Log($"히트 {hitIdx} 이미 판정 완료됨 - 중복 판정 방지");
        return;
    }
    
    EvaluateClashResult();
    hitJudgmentCompleted[hitIdx] = true;
}
```

#### 문제 2: CurrentHit 범위 초과 문제
**발견 시점**: 오후 3시경, 2타 공격 테스트 중
**문제 상황**: `IndexOutOfRangeException: Index was outside the bounds of the array`
**근본 원인**: 
- CurrentHit가 히트 전환 후 배열 범위를 벗어남 (CurrentHit=2, 배열 길이=2)
- 발사체 충돌 시점에 CurrentHit를 참조하여 잘못된 인덱스 사용

**해결 과정**:
1. **문제 인식**: CurrentHit의 본래 목적(히트 전환)과 발사체 판정의 분리 필요
2. **해결책 도출**: 발사체에 `hitIndex` 필드 추가하여 발사 시점의 히트 인덱스 저장
3. **구현**: 충돌 시점에 `projectile.hitIndex` 사용하여 정확한 히트 판정

**구현된 해결책**:
```csharp
// Projectile.cs
public int hitIndex = -1;

public void Initialize(ActionCommandData command, int hit, bool fromPlayer)
{
    hitIndex = hit; // 발사 시점의 히트 인덱스 저장
}

// CombatManager.cs
Projectile projectile = ProjectileManager.Instance.GetProjectile(projectilePrefab);
projectile.Initialize(command, CurrentHit, isPlayerAttacker);
```

#### 문제 3: 턴 종료 로직 문제
**발견 시점**: 오후 4시경, 턴 전환 테스트 중
**문제 상황**: 턴 종료 대기 시간이 제대로 동작하지 않음
**근본 원인**: 
- 발사체 완료 기준 턴 종료로 잘못된 설계
- WaitForTurnEnd 코루틴이 독립적으로 실행되어 다음 턴에 영향

**해결 과정**:
1. **문제 인식**: 발사체 완료와 턴 종료의 개념 분리 필요
2. **해결책 도출**: 턴 종료를 시간 기반으로 변경 (마지막 히트 end + Turn End Buffer)
3. **구현**: PerformTurn의 히트 전환 루틴에서 직접 턴 종료 처리

**구현된 해결책**:
```csharp
// PerformTurn에서 히트 전환 시
if (CurrentHit >= command.hitCount)
{
    if (hitJudgmentCompleted[CurrentHit - 1])
    {
        yield return new WaitForSeconds(GlobalConfig.Instance.TurnEndBuffer);
        break; // 턴 종료
    }
}
```

---

## 🎯 포스트모텀 - 개선해야 할 문제들

### 1. 문제 진단 능력 부족
**문제**: 
- 사용자가 "판정이 발생하지 않는다"고 했을 때 발사체 소멸에만 집중
- 근본 원인(중복 판정 방지 로직)을 놓침

**개선 방안**:
- 사용자 피드백을 정확히 파악하고 근본 원인 분석
- 증상이 아닌 원인에 집중하여 해결책 제시

### 2. CurrentHit 개념 오해
**문제**: 
- CurrentHit를 발사체 판정에 사용하려 함
- CurrentHit의 본래 목적(히트 전환)을 무시

**개선 방안**:
- 각 변수의 본래 목적과 사용 범위를 명확히 이해
- 새로운 시스템 도입 시 기존 개념과의 관계 정리

### 3. 배열 인덱스 안전성 부족
**문제**: 
- 배열 범위 체크 없이 인덱스 접근
- CurrentHit가 배열 범위를 벗어날 가능성 미고려

**개선 방안**:
- 모든 배열 접근 전에 범위 체크
- 안전한 배열 접근 패턴 적용

---

## 📊 작업 시간 및 완료된 작업

### 작업 시간
- **오전**: 1단계-3단계 구현 (4시간)
- **오후**: 문제 해결 및 시스템 안정화 (4.5시간)
  - 중복 판정 문제 해결: 2시간
  - CurrentHit 범위 초과 해결: 1시간  
  - 턴 종료 로직 수정: 1시간
  - 디버깅 로그 추가: 30분

### 최종 완료된 작업
1. **발사체 시스템 기본 구조 구현** ✅
   - Projectile, ProjectileManager, CharacterHitSystem 구현
   - ActionCommandData 발사체 프리팹 필드 추가
   - CombatManager 발사체 생성 로직 구현

2. **중복 판정 방지 시스템 구현** ✅
   - `hitJudgmentCompleted[]` 배열로 히트당 판정 한 번만 발생
   - `hitJudgmentCount[]` 배열로 호출 횟수 추적
   - 안전한 배열 접근으로 IndexOutOfRangeException 방지

3. **발사체 hitIndex 시스템 구현** ✅
   - 발사체에 `hitIndex` 필드 추가
   - 발사 시점의 히트 인덱스 저장
   - 충돌 시점에 정확한 히트 판정

4. **시간 기반 턴 종료 로직** ✅
   - 발사체 완료 기준 → 시간 기반 턴 종료
   - PerformTurn에서 직접 턴 종료 처리
   - Turn End Buffer 정확한 적용

5. **디버깅 시스템 강화** ✅
   - 중복 판정 추적 로그 추가
   - 배열 범위 체크 로그 추가
   - 발사체 충돌 과정 추적 로그

---

## 🎯 다음 단계
- 발사체 기반 전투 시스템 구현 명세서에 따른 추가 개발
- 방어자 입력 시스템 발사체 기반 리팩토링
- 전체 시스템 통합 테스트 및 최종 검증


