#정의
Durational Effect
Durational = 일정 "턴" 동안 유지되는
Effect = 다양한 효과.

#목적
단순히 공격 일변도로 피해를 주기만 하는 것 보다는 다양한 패턴의 행동을 정의할 수 있도록 한다.

#상세 정의
##Durational 의 정의는 총 3가지.
- Next Turn : 다음 N턴 동안 유지됨
- Next Attack Turn : 다음 N번의 공격 턴 동안 유지됨
- Next Defense Turn : 다음 N번의 방어 턴 동안 유지됨

##데이터 테이블 구조
###DurationalEffect 테이블
Effect 의 데이터 셋은 2가지의 값을 조합하는 방식으로 의미를 가진다.
- Target : 효과를 적용할 "대상" 정의. 
-- Self : 자기 자신에게
-- Enemy : 적에게
- Effect : 어떤 효과를 적용할 것이냐.
-- Effect 는 별도의 테이블을 통해 하위 값들을 정의한다.

###EffectModule : 효과에 대한 정의. 모듈 느낌으로 작성해서 DurationalEffect 테이블에서 참조하는 하위 테이블.(이후 다른 테이블에서도 참조 가능)
- EffectType : 어떤 효과인가?
-- Enum : ModATK, ModDR, ModPerfectChance
--- ModATK : 대상의 기본 공격력을 조정한다. value 값을 직접 Add. 음수를 통해 깎을 수도 있다. 
--- ModDR : 대상의 피해 감소량을 조정한다. value 값을 직접 Add. 음수를 통해 깎을 수도 있다. 
--- ModPerfectChance : 대상의 완벽 입력 확률을 조정한다. (실제 입력에 실패했더라도, 이 값을 통해 확률을 계산하여 성공 처리가 될 수 있다.) value 값을 0~100 까지 받아서 1=1% 로 처리한다. (즉, 최대값은 100을 오버해도 100으로 처리하여 100% 로 인식.)
-- value : Enum 에서 정의한 효과 종류에 어떤 값을 적용할 것인가. 소수점은 사용하지 않는다. 음수가 적용될 수 있어야 한다.

---

# 구현 계획서

## 1. 데이터 구조 설계

### 1.1 Enum 정의
```csharp
public enum Target
{
    Self,    // 자기 자신
    Enemy    // 적
}

public enum EffectType
{
    ModATK,           // 공격력 조정
    ModDR,            // 피해 감소량 조정
    ModPerfectChance  // 완벽 입력 확률 조정
}
```

### 1.2 클래스 구조
```csharp
[System.Serializable]
public class EffectModule
{
    public EffectType effectType;
    public int value; // 정수, 음수 가능
}

[System.Serializable]
public class DurationalEffect
{
    public Target target;
    public EffectModule effectModule;
    public int duration; // 남은 턴 수
    public string effectId; // 고유 식별자 (덮어쓰기용)
}
```

## 2. 핵심 시스템 구현

### 2.1 DurationalEffectManager (싱글톤)
- **역할**: 지속 효과 관리 및 적용
- **주요 메서드**:
  - `ApplyEffect(DurationalEffect effect)`: 효과 적용
  - `RemoveEffect(string effectId)`: 효과 해제
  - `TickTurn()`: 턴 감소 처리
  - `GetActiveEffects()`: 활성 효과 목록 반환
  - `ClearAllEffects()`: 모든 효과 제거

### 2.2 효과 적용 시스템
- **CharacterData 확장**: 지속 효과 적용된 스탯 계산
- **CombatManager 연동**: 턴 시작/종료 시 효과 관리
- **덮어쓰기 로직**: 같은 effectId의 효과가 있으면 기존 것 제거 후 새로 적용

## 3. 구현 단계

### Phase 1: 기본 구조
1. Enum 및 데이터 클래스 정의
2. DurationalEffectManager 기본 구조 구현
3. 단위 테스트 작성

### Phase 2: 효과 적용 시스템
1. CharacterData에 지속 효과 적용 로직 추가
2. 스탯 계산 시 지속 효과 반영
3. CombatManager와 연동

### Phase 3: UI 및 시각화
1. 지속 효과 표시 UI 구현
2. 아이콘 + 숫자 표시 시스템
3. 효과 적용/해제 시각적 피드백

### Phase 4: 테스트 및 최적화
1. 다양한 시나리오 테스트
2. 성능 최적화
3. 버그 수정 및 안정화

## 4. 기술적 고려사항

### 4.1 성능 최적화
- **효과 목록 캐싱**: 자주 사용되는 효과 목록 캐싱
- **이벤트 기반 업데이트**: 스탯 변경 시에만 UI 업데이트
- **메모리 관리**: 만료된 효과 즉시 제거

### 4.2 확장성
- **EffectType 확장**: 새로운 효과 타입 쉽게 추가 가능
- **모듈화**: EffectModule을 다른 시스템에서도 재사용 가능
- **설정 가능**: Inspector에서 효과 값 조정 가능

### 4.3 디버깅
- **로깅 시스템**: 효과 적용/해제/만료 로그
- **디버그 UI**: 현재 활성 효과 목록 표시
- **단위 테스트**: 각 효과 타입별 테스트 케이스

## 5. 예상 구현 시간
- **Phase 1**: 2-3시간 (기본 구조)
- **Phase 2**: 4-5시간 (효과 적용 시스템)
- **Phase 3**: 3-4시간 (UI 구현)
- **Phase 4**: 2-3시간 (테스트 및 최적화)
- **총 예상 시간**: 11-15시간

## 6. 의존성
- **기존 시스템**: CharacterData, CombatManager, GlobalConfig
- **새로운 의존성**: 없음 (독립적 구현 가능)
- **UI 시스템**: 기존 UI 시스템과 연동

