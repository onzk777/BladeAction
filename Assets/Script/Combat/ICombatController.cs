public interface ICombatController
{
    /// <summary>�ش� ��Ʈ�ѷ��� ������ ������(Combatant)�� ��ȯ�մϴ�.</summary>
    Combatant Combatant { get; }

    /// <summary>�̹� �Ͽ� ������ Ŀ�ǵ��� �ε����� ��ȯ�մϴ�.</summary>
    int GetSelectedCommandIndex();

    /// <summary>
    /// PerformTurn ���� �� ���޹��� ��Ʈ ���� �����
    /// �ش� ��Ʈ�ѷ��� ó���ϵ��� ȣ���մϴ�.
    /// </summary>
    /// <param name="result">Ŀ�ǵ� ���� ��� ��ü</param>
    void ReceiveCommandResult(CombatantCommandResult result);   // �� �ൿ ��� ó��
    void OnHitResult(int hitIndex, bool isPerfect); // �Է� ��� ǥ��
}
