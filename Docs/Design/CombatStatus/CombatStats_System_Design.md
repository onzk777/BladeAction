## Combat Stats 시스템 설계서 (Clamp 규칙 포함)

### 1. 개요와 목표
- **목표**: 여러 소스(기본값, 장착 장비, 유파/액션, 전투 중 지속효과 등)를 합산해 얻은 Combatant의 "최종 결과 스탯"에 대해, 에셋 기반의 범위 규칙(Clamp)을 적용하여 하한/상한을 강제한다. 데이터 원본은 수정하지 않는다.
- **범위(1차 단계)**: 현재 단계에서는 "장착 장비"의 기여만 합산 대상으로 한다. 추후 유파/액션, 지속효과(Durational Effect)를 같은 파이프라인에 편입한다.

### 2. 핵심 원칙
- **단일 구조체 표준화**: 전투 스탯은 `CombatStats` 단일 구조체로 표준화한다. Combatant는 이 구조체의 최종 값만을 보유한다.
- **합산 후 Clamp**: 모든 델타(기여)를 합산하여 raw 결과를 만든 뒤, Combatant에 커밋하기 직전에 1회 Clamp한다.
- **단위 일관성**: 내부 계산은 비율/확률을 0.0~1.0(float)로 통일, 배율은 multiplier로 통일. 정수형 스탯은 최종 커밋 시 반올림(Mathf.RoundToInt)로 정수화한다.
- **HP 특례**: `MaxHP`를 먼저 Clamp 적용 후, `HP`는 [0, MaxHP]로 별도 Clamp.

### 3. 데이터 모델
- `CombatStats` 구조체(표준 내부 표현)
  - 정수 스탯(내부 float 계산 → 커밋 시 정수 반영): `attack`, `defenseDR`, `maxHP`, `maxPoise`, `parryPoiseDamage`, `blockPoiseConsumption`, `parryPoiseConsumption`, `parryPoiseAttackPower`, `poiseGain`
  - 비율/확률(내부 0.0~1.0): `critChance`, `guardDamageReduction`, `damageReduction`, `blockEfficiency`, `parryEfficiency`
  - 배율(곱셈용 multiplier): `critMultiplier` (예: 1.5)

예시 제안 스켈레톤(참고용):
```csharp
public struct CombatStats
{
    // 정수형(내부 계산은 float, 커밋 시 반올림)
    public float attack;
    public float defenseDR;
    public float maxHP;
    public float maxPoise;
    public float parryPoiseDamage;
    public float blockPoiseConsumption;
    public float parryPoiseConsumption;
    public float parryPoiseAttackPower;
    public float poiseGain;

    // 비율(0~1)
    public float critChance;              // 0~1
    public float guardDamageReduction;    // 0~1
    public float damageReduction;         // 0~1 (즉시 전환)
    public float blockEfficiency;         // 0~1
    public float parryEfficiency;         // 0~1

    // 배율(multiplier)
    public float critMultiplier;          // 예: 1.5 = 150%
}
```

### 4. Clamp 규칙 에셋(StatLimitRules)
- 파일: `Assets/Resources/Data/Stat/StatLimitRules.asset`
- 항목 필드: `statKey`, `valueKind(int|ratio|multiplier|float)`, `min`, `max`
- 동기화: 에디터에서 `CombatStats`의 public 필드를 리플렉션으로 스캔하여 신규 항목은 자동 추가(기존 범위는 보존), 삭제 후보는 표시만(보존)한다.
- 적용 정책:
  - 모든 스탯에 대해 `min ≤ value ≤ max`로 Clamp.
  - 정수 스탯은 Clamp 후 커밋 시 반올림(Mathf.RoundToInt).
  - HP 특례: `maxHP` Clamp → `HP`는 [0..maxHP] 재 Clamp.

### 5. 계산 파이프라인(1차 단계: 장착 장비)
1) `CharacterData`로부터 base 스탯을 `CombatStats`로 변환
2) 장착된 모든 장비의 `EquipmentStats`를 `CombatStats` 델타로 변환(단위 변환 포함: % → ratio)
3) 합산: `finalRaw = base + Σ(equipmentDelta)`
4) Clamp: `finalClamped = StatLimiter.ClampAll(finalRaw, rules)`
5) 커밋: 정수 스탯 반올림 → Combatant에 적용 → `OnStatsChanged` 이벤트 발행

API 제안(요약):
```csharp
public sealed class StatsCalculationManager : MonoBehaviour
{
    public CombatStats GetEffectiveStats(Combatant combatant);
    public void RecalculateAndCommit(Combatant combatant);
    public void RegisterEquipment(Combatant combatant, Item equipment);
    public void UnregisterEquipment(Combatant combatant, Item equipment);
}

public static class StatLimiter
{
    public static CombatStats ClampAll(in CombatStats src, StatLimitRules rules);
    public static float Clamp(string statKey, float value, StatLimitRules rules);
}
```

### 6. 단위 일관화(즉시 전환)
- 비율/확률 항목은 내부 0.0~1.0으로 통일해 계산한다. UI/CSV/인스펙터 표시는 %로 변환해 보여준다.
- `damageReduction`은 즉시 ratio(0~1)로 운용한다(표시는 %). 기존 % 입력이 있다면 어댑터에서 ratio로 변환 후 합산한다.

현재 코드의 퍼센트/비율 사용 현황(참고 스냅샷):

```13:16:Assets/Script/Item/EquipmentStats.cs
[Tooltip("막기 효율 (%)")]
[Range(0f, 100f)]
public float blockEfficiency = 0f;
```
```21:24:Assets/Script/Item/EquipmentStats.cs
[Tooltip("쳐내기 효율 (%)")]
[Range(0f, 100f)]
public float parryEfficiency = 0f;
```
```35:38:Assets/Script/Item/EquipmentStats.cs
[Tooltip("피해 감소율 (%)")]
[Range(0f, 100f)]
public float damageReduction = 0f;
```
```26:29:Assets/Script/CharacterData.cs
public int guardDRBonus = 5; // 막기 시 DR 보너스
public float guardDamageReduction = 0.5f; // 막기 시 피해 감소 비율 (0.5 = 50% 감소)
```
```139:142:Assets/Script/Combatant.cs
public bool IsCriticalHit()
{
    return UnityEngine.Random.Range(0, 100) < Crit;
}
```
```147:150:Assets/Script/Combatant.cs
public int CalculateCriticalDamage(int baseDamage)
{
    return Mathf.RoundToInt(baseDamage * CritRatio / 100f);
}
```

정책 요약:
- 내부: ratio(0~1) 및 multiplier 기반 계산.
- 외부 표시/입력: %를 유지하되, 입출력 경계에서 ratio↔% 변환.

### 7. 정수 스탯 반올림 정책
- 모든 정수형 스탯은 최종 커밋 직전에 일괄 `Mathf.RoundToInt`로 반영한다.
- 장점: 소수 누적을 계산 단계에서 유지, 마지막에 한 번만 반올림하여 상/하 편향을 줄이고 예측 가능성을 높임.

### 8. 이벤트/검증/성능
- 이벤트: Combatant에 커밋 시 `OnStatsChanged` 발행. 추후 특정 스탯 단위 이벤트는 확장 포인트로 남김.
- 검증: 에디터 메뉴로 "모든 Combatant 재계산+Clamp" 실행, 결과 로그/요약 제공.
- 성능: 스탯 수 M에 대해 O(M) 클램프. 장비 수 N 합산 O(N·M). 일반 규모에서 무시 가능.

### 9. 마이그레이션 가이드(즉시 전환)
- `damageReduction` 및 기타 퍼센트 기반 필드는 내부 ratio로 해석한다.
- 기존 에디터/CSV/표시는 %를 유지하되, 매핑 레이어에서 ratio로 변환 후 합산.
- 점진적 리팩터링: 코드 주석/툴팁에 내부 단위 명시(0~1), 표시만 %.

### 10. 통합 계획(현재 → 미래)
- 현재: 장착 장비 → 합산/Clamp/커밋 구현.
- 차후: 유파/액션, 지속효과(Durational Effect)를 동일 파이프라인에 모듈로 편입(등록/해제 API 재사용).

### 11. 구현 To-Dos
1) `CombatStats` 정의 및 필드/단위 확정
2) `StatLimitRules.asset` 생성 및 에디터 동기화(리플렉션)
3) 단위 어댑터: `EquipmentStats` → `CombatStats` (%, 배율 변환 포함)
4) `StatsCalculationManager`(1차: 장비만) 구현: base+Σequip → Clamp → 커밋
5) `StatLimiter` 구현: per-stat Clamp, MaxHP 선행, HP [0..MaxHP]
6) `Combatant` 연동: 기존 API 유지, 내부만 매니저 값을 사용하도록 연결
7) 검증 도구: 전체 재계산/Clamp 메뉴, 기본 로그/요약

### 12. 용어 정의
- **ratio(0~1)**: 0~100%를 내부에서 0.0~1.0으로 환산한 값.
- **multiplier**: 1.0을 기준으로 곱해지는 배율(예: 1.5배 = 150%).
- **Clamp**: 값을 특정 구간[min, max] 안으로 강제하는 연산.


