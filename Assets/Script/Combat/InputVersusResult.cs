using UnityEngine;

public class InputVersusResult
{
    public enum ResultType
    {
        Hit, // ����
        Parry, // �Ϲ��� �ĳ���
        Deflect, // �Ϻ��� ���ݿ� ���� �ĳ���
        PerfectAttack, // �Ϻ� ����
        Guard, // ���Ƴ���
        GuardBreak, // ���Ƴ��⿡ �Ϻ� ������ ������
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
        if (isPerfectAtk && isPerfectDef) // ���� ��� �Ϻ� �Է�
        {
            return new InputVersusResult(ResultType.Deflect, isPerfectAtk, isPerfectDef, guard); // �Ϻ��� ���ݿ� ���� �ĳ���
        }
        else if (!isPerfectAtk && isPerfectDef) // �� �Ϻ� �Է�
        {
            return new InputVersusResult(ResultType.Parry, isPerfectAtk, isPerfectDef, guard); // �Ϲ��� �ĳ���
        }
        else if (isPerfectAtk && !isPerfectDef && guard) // ���ݸ� �Ϻ� �Է�
        {
            return new InputVersusResult(ResultType.GuardBreak, isPerfectAtk, isPerfectDef, guard); // ���Ƴ��⿡ �Ϻ� ������ ������
        }
        else if (!isPerfectAtk && !isPerfectDef & guard) // ���� ��� �ҿϺ� �Է�, ����ڰ� ���Ƴ��� ���
        {
            return new InputVersusResult(ResultType.Guard, isPerfectAtk, isPerfectDef, guard); // ���Ƴ���
        }
        else if (!isPerfectAtk && !isPerfectDef & !guard) // ���� ��� �ҿϺ� �Է�
        {
            return new InputVersusResult(ResultType.Hit, isPerfectAtk, isPerfectDef, guard); // ����
        }
        else
        {
            Debug.Log($"Invalid InputVersusResult parameters. isPerfectAtk={isPerfectAtk}, isPerfectDef={isPerfectDef}, guard={guard}");
            return null; // �߸��� �Է� ó��
        }
    }
}
