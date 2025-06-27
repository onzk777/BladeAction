public class InterruptManager
{
    private static bool _isInterrupted = false;

    /// <summary>
    /// 외부에서 인터럽트를 요청할 때 호출
    /// </summary>
    public static void RequestInterrupt()
    {
        _isInterrupted = true;
    }

    /// <summary>
    /// 인터럽트 발생 여부 확인
    /// </summary>
    public static bool IsInterrupted()
    {
        return _isInterrupted;
    }

    /// <summary>
    /// 인터럽트 상태 초기화
    /// </summary>
    public static void Reset()
    {
        _isInterrupted = false;
    }
}