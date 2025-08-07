public class HitResult
{
    public bool IsPerfect { get; set; }     // 이 히트에서 완벽 입력이 있었는가
    public bool HasResolved { get; set; }    // 이 히트에 대한 판정이 이미 처리되었는가
}