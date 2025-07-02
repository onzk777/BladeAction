public interface ICombatController
{
    /// <summary>�ش� ��Ʈ�ѷ��� ������ ������(Combatant)�� ��ȯ�մϴ�.</summary>
    Combatant Combatant { get; }

    /// <summary>�̹� �Ͽ� ������ Ŀ�ǵ��� �ε����� ��ȯ�մϴ�.</summary>
    int GetSelectedIndex();

    /// <summary>
    /// PerformTurn ���� �� ���޹��� ��Ʈ ���� �����
    /// �ش� ��Ʈ�ѷ��� ó���ϵ��� ȣ���մϴ�.
    /// </summary>
    /// <param name="result">Ŀ�ǵ� ���� ��� ��ü</param>
    void ReceiveCommandResult(CombatantCommandResult result);   // �� �ൿ ��� ó��
    void OnHitResult(int hitIndex, bool isPerfect); // ��Ʈ ��� ó��
}
