using UnityEngine;

public class InputVersusResult
{
    public enum ResultType
    {
        Hit, // 명중
        Parry, // 완벽한 쳐내기
        HalfParry, // 완벽한 공격에 대한 불완전 쳐내기
        PerfectAttack, // 완벽 공격
        Guard, // 막아내기
        GuardBreak, // 막아내기에 완벽 공격이 가해짐
    }

    public ResultType resultType;
    public bool isPerfectAttack;
    public bool isPerfectDefense;
    public float atkTime; // 공격 입력 시간
    public float defTime; // 방어 입력 시간
    public bool isGuard;

    public InputVersusResult(bool isPerfectAtk, float atkTime, bool isPerfectDef, float defTime, bool guard)
    {
        resultType = ResultType.Hit; // 기본값은 Hit
        isPerfectAttack = isPerfectAtk;
        isPerfectDefense = isPerfectDef;        
        this.atkTime = atkTime;
        this.defTime = defTime;
        isGuard = guard;
    }
    public InputVersusResult(ResultType type)
    {
        resultType = type;
        isPerfectAttack = false; // 기본값은 false
        isPerfectDefense = false; // 기본값은 false
        atkTime = 0f; // 기본값은 0
        defTime = 0f; // 기본값은 0
        isGuard = false; // 기본값은 false
    }
    public ResultType GetResult(bool isPerfectAtk, float atkTime, bool isPerfectDef, float defTime, bool guard)
    {
        bool isAtkFast = atkTime < defTime; // 공격 입력이 방어 입력보다 빠른지 여부
        ResultType returnResult;

        if (isPerfectAtk && isPerfectDef) // 공방 모두 완벽 입력
        {
            returnResult = isAtkFast ? ResultType.PerfectAttack : ResultType.HalfParry; // 공격이 빨랐으면 완벽 공격, 공격이 느렸으면 불완전 쳐내기
            return returnResult; 
        }
        else if (!isPerfectAtk && isPerfectDef) // 방어만 완벽 입력
        {
            return ResultType.Parry; // 완벽한 쳐내기
        }
        else if (isPerfectAtk && !isPerfectDef) // 공격만 완벽 입력 & 막아내기
        {
            returnResult = guard ? ResultType.GuardBreak : ResultType.PerfectAttack; // 막아내기 성공 시 막아내기 박살, 실패 시 완벽 공격
            return returnResult; // 완벽 공격이 막아내기를 박살냄
        }
        else if (!isPerfectAtk && !isPerfectDef & guard) // 공방 모두 불완벽 입력 & 막아내기
        {
            return ResultType.Guard; // 막아내기
        }
        else if (!isPerfectAtk && !isPerfectDef & !guard) // 공방 모두 불완벽 입력
        {
            return ResultType.Hit; // 명중
        }
        else
        {
            Debug.Log($"Invalid InputVersusResult parameters. isPerfectAtk={isPerfectAtk}, isPerfectDef={isPerfectDef}, guard={guard}");
            return ResultType.Hit; // 잘못된 입력 처리
        }
    }
    public void OnHitVersusResult(int hitIndex, InputVersusResult.ResultType resultType)
    {
        // 히트 결과를 UI에 표시합니다.
        switch (resultType)
        {
            case InputVersusResult.ResultType.Hit:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "적중!");
                break;
            case InputVersusResult.ResultType.Parry:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "완벽하게 쳐냈다!");
                break;
            case InputVersusResult.ResultType.HalfParry:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "가까스로 쳐냈다...");
                break;
            case InputVersusResult.ResultType.Guard:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "막아냈다!");
                break;
            case InputVersusResult.ResultType.GuardBreak:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "가드 브레이크!");
                break;
            case InputVersusResult.ResultType.PerfectAttack:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "완벽한 일격!");
                break;
        }
    }
}
