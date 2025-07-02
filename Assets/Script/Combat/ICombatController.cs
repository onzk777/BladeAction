public interface ICombatController
{
    /// <summary>해당 컨트롤러가 소유한 전투자(Combatant)를 반환합니다.</summary>
    Combatant Combatant { get; }

    /// <summary>이번 턴에 실행할 커맨드의 인덱스를 반환합니다.</summary>
    int GetSelectedIndex();

    /// <summary>
    /// PerformTurn 종료 시 전달받은 히트 판정 결과를
    /// 해당 컨트롤러가 처리하도록 호출합니다.
    /// </summary>
    /// <param name="result">커맨드 실행 결과 객체</param>
    void ReceiveCommandResult(CombatantCommandResult result);   // 턴 행동 결과 처리
    void OnHitResult(int hitIndex, bool isPerfect); // 히트 결과 처리
}
