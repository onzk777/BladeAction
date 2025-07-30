using UnityEngine;

public class InputVersusResult
{
    public enum ResultType
    {
        Hit, // ����
        Parry, // �Ϻ��� �ĳ���
        HalfParry, // �Ϻ��� ���ݿ� ���� �ҿ��� �ĳ���
        PerfectAttack, // �Ϻ� ����
        Guard, // ���Ƴ���
        GuardBreak, // ���Ƴ��⿡ �Ϻ� ������ ������
    }

    public ResultType resultType;
    public bool isPerfectAttack;
    public bool isPerfectDefense;
    public float atkTime; // ���� �Է� �ð�
    public float defTime; // ��� �Է� �ð�
    public bool isGuard;

    public InputVersusResult(bool isPerfectAtk, float atkTime, bool isPerfectDef, float defTime, bool guard)
    {
        resultType = ResultType.Hit; // �⺻���� Hit
        isPerfectAttack = isPerfectAtk;
        isPerfectDefense = isPerfectDef;        
        this.atkTime = atkTime;
        this.defTime = defTime;
        isGuard = guard;
    }
    public InputVersusResult(ResultType type)
    {
        resultType = type;
        isPerfectAttack = false; // �⺻���� false
        isPerfectDefense = false; // �⺻���� false
        atkTime = 0f; // �⺻���� 0
        defTime = 0f; // �⺻���� 0
        isGuard = false; // �⺻���� false
    }
    public ResultType GetResult(bool isPerfectAtk, float atkTime, bool isPerfectDef, float defTime, bool guard)
    {
        bool isAtkFast = atkTime < defTime; // ���� �Է��� ��� �Էº��� ������ ����
        ResultType returnResult;

        if (isPerfectAtk && isPerfectDef) // ���� ��� �Ϻ� �Է�
        {
            returnResult = isAtkFast ? ResultType.PerfectAttack : ResultType.HalfParry; // ������ �������� �Ϻ� ����, ������ �������� �ҿ��� �ĳ���
            return returnResult; 
        }
        else if (!isPerfectAtk && isPerfectDef) // �� �Ϻ� �Է�
        {
            return ResultType.Parry; // �Ϻ��� �ĳ���
        }
        else if (isPerfectAtk && !isPerfectDef) // ���ݸ� �Ϻ� �Է� & ���Ƴ���
        {
            returnResult = guard ? ResultType.GuardBreak : ResultType.PerfectAttack; // ���Ƴ��� ���� �� ���Ƴ��� �ڻ�, ���� �� �Ϻ� ����
            return returnResult; // �Ϻ� ������ ���Ƴ��⸦ �ڻ쳿
        }
        else if (!isPerfectAtk && !isPerfectDef & guard) // ���� ��� �ҿϺ� �Է� & ���Ƴ���
        {
            return ResultType.Guard; // ���Ƴ���
        }
        else if (!isPerfectAtk && !isPerfectDef & !guard) // ���� ��� �ҿϺ� �Է�
        {
            return ResultType.Hit; // ����
        }
        else
        {
            Debug.Log($"Invalid InputVersusResult parameters. isPerfectAtk={isPerfectAtk}, isPerfectDef={isPerfectDef}, guard={guard}");
            return ResultType.Hit; // �߸��� �Է� ó��
        }
    }
    public void OnHitVersusResult(int hitIndex, InputVersusResult.ResultType resultType)
    {
        // ��Ʈ ����� UI�� ǥ���մϴ�.
        switch (resultType)
        {
            case InputVersusResult.ResultType.Hit:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "����!");
                break;
            case InputVersusResult.ResultType.Parry:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "�Ϻ��ϰ� �ĳ´�!");
                break;
            case InputVersusResult.ResultType.HalfParry:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "����� �ĳ´�...");
                break;
            case InputVersusResult.ResultType.Guard:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "���Ƴ´�!");
                break;
            case InputVersusResult.ResultType.GuardBreak:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "���� �극��ũ!");
                break;
            case InputVersusResult.ResultType.PerfectAttack:
                CombatStatusDisplay.Instance.ShowHitVersusResult(hitIndex, "�Ϻ��� �ϰ�!");
                break;
        }
    }
}
