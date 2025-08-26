public interface ICombatController
{
    /// <summary>해당 컨트롤러가 소유한 전투자(Combatant)를 반환합니다.</summary>
    Combatant Combatant { get; }

    /// <summary>이번 턴에 실행할 커맨드의 인덱스를 반환합니다.</summary>
    int GetSelectedCommandIndex();
    ActionCommandData GetSelectedCommand();

    /// <summary>
    /// PerformTurn 종료 시 전달받은 히트 판정 결과를
    /// 해당 컨트롤러가 처리하도록 호출합니다.
    /// </summary>
    /// <param name="result">커맨드 실행 결과 객체</param>
    void ReceiveCommandResult(CombatantCommandResult result);   // 턴 행동 결과 처리
    void OnHitResult(int hitIndex, bool isPerfect); // 입력 결과 표시

    // ✅ 아래는 에디터에서 드롭다운 테스트 기능을 위한 추가 프로퍼티
    /// <summary>현재 장착 중인 소드 아트 스타일을 반환합니다.</summary>
    SwordArtStyleData EquippedStyle { get; }

    /// <summary>테스트용으로 선택된 커맨드 인덱스를 가져오거나 설정합니다.</summary>
    int TestCommandIndex { get; set; }
    
    // ✅ 애니메이션 재생 관련 메서드 (정규 Feature)
    /// <summary>
    /// 공격 커맨드 애니메이션 재생
    /// </summary>
    void OnPlayActionCommand();
    
    /// <summary>
    /// 중단 애니메이션 재생
    /// </summary>
    void OnInterrupted();
    
    /// <summary>쳐내기 성공 시 호출 - 쳐내기 애니메이션 재생</summary>
    void OnSuccessParry();
    
    /// <summary>피격 시 호출 - 피격 애니메이션 재생</summary>
    void OnBeHitted();
    
    /// <summary>방어 시 호출 - 방어 애니메이션 재생</summary>
    void OnPlayDefence();
}
