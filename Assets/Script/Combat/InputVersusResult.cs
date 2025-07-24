using UnityEngine;

public class InputVersusResult
{
    public enum ResultType
    {
        Hit, // 명중
        Parry, // 일방적 쳐내기
        Deflect, // 완벽한 공격에 대한 쳐내기
        PerfectAttack, // 완벽 공격
        Guard, // 막아내기
        GuardBreak, // 막아내기에 완벽 공격이 가해짐
    }

    public ResultType resultType;
    public bool isPerfectAttack;
    public bool isPerfectDefense;
    public bool isGuard;

    public InputVersusResult(ResultType type, bool isPerfectAtk, bool isPerfectDef, bool guard)
    {
        resultType = type;
        isPerfectAttack = isPerfectAtk;
        isPerfectDefense = isPerfectDef;
        isGuard = guard;
    }
    public static InputVersusResult GetResult(bool isPerfectAtk, bool isPerfectDef, bool guard)
    {
        if (isPerfectAtk && isPerfectDef) // 공방 모두 완벽 입력
        {
            return new InputVersusResult(ResultType.Deflect, isPerfectAtk, isPerfectDef, guard); // 완벽한 공격에 대한 쳐내기
        }
        else if (!isPerfectAtk && isPerfectDef) // 방어만 완벽 입력
        {
            return new InputVersusResult(ResultType.Parry, isPerfectAtk, isPerfectDef, guard); // 일방적 쳐내기
        }
        else if (isPerfectAtk && !isPerfectDef && guard) // 공격만 완벽 입력
        {
            return new InputVersusResult(ResultType.GuardBreak, isPerfectAtk, isPerfectDef, guard); // 막아내기에 완벽 공격이 가해짐
        }
        else if (!isPerfectAtk && !isPerfectDef & guard) // 공방 모두 불완벽 입력, 방어자가 막아내기 사용
        {
            return new InputVersusResult(ResultType.Guard, isPerfectAtk, isPerfectDef, guard); // 막아내기
        }
        else if (!isPerfectAtk && !isPerfectDef & !guard) // 공방 모두 불완벽 입력
        {
            return new InputVersusResult(ResultType.Hit, isPerfectAtk, isPerfectDef, guard); // 명중
        }
        else
        {
            Debug.Log($"Invalid InputVersusResult parameters. isPerfectAtk={isPerfectAtk}, isPerfectDef={isPerfectDef}, guard={guard}");
            return null; // 잘못된 입력 처리
        }
    }
}
