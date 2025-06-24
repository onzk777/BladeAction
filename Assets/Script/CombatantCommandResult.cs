using UnityEngine;
public class CombatantCommandResult
{
    public ActionCommandData command;     // � Ŀ�ǵ忴�°�
    public HitResult[] hitResults;        // �� ��Ʈ�� ��� �迭
    public bool wasInterrupted = false; // �̹� �׼��� �ߴܵǾ��°�

    public CombatantCommandResult(ActionCommandData command)
    {
        this.command = command;
        int count = Mathf.Max(1, command.hitCount);  // �ּ� 1 �̻� ����
        hitResults = new HitResult[count];
        for (int i = 0; i < hitResults.Length; i++)
        {
            hitResults[i] = new HitResult();
        }
    }
}
