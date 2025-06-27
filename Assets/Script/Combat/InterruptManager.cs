public class InterruptManager
{
    private static bool _isInterrupted = false;

    /// <summary>
    /// �ܺο��� ���ͷ�Ʈ�� ��û�� �� ȣ��
    /// </summary>
    public static void RequestInterrupt()
    {
        _isInterrupted = true;
    }

    /// <summary>
    /// ���ͷ�Ʈ �߻� ���� Ȯ��
    /// </summary>
    public static bool IsInterrupted()
    {
        return _isInterrupted;
    }

    /// <summary>
    /// ���ͷ�Ʈ ���� �ʱ�ȭ
    /// </summary>
    public static void Reset()
    {
        _isInterrupted = false;
    }
}