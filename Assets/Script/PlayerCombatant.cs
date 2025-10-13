using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatant : Combatant
{
    public int selectedIndex = 0; // 인스펙터에서 지정하는 테스트용 인덱스
    public bool useTestMode;  // true면 테스트 모드로 동작

    private PlayerController controller; // PlayerController 인스턴스 참조
    public PlayerCombatant(CharacterData data, PlayerController controller) : base(data)
    {
        this.controller = controller;
    }
    
    public void SetController(PlayerController newController)
    {
        controller = newController;
    }
    
    /// <summary>
    /// 플레이어의 검술을 선택합니다.
    /// 
    /// 현재: UI에서 선택된 버튼 인덱스 사용
    /// 향후: BT 시스템 추가 가능 (자동 전투 모드 등)
    /// 
    /// 중요:
    /// - Controller.GetSelectedCommandIndex()를 호출하면 순환 참조 발생!
    /// - 직접 UI에서 선택을 가져와야 함
    /// </summary>
    public override CommandSelection ChooseCommand()
    {
        // UI에서 직접 선택된 버튼 인덱스 가져오기
        var playerActionSelectUI = UnityEngine.Object.FindFirstObjectByType<PlayerActionSelectUI>();
        
        int idx = 0;
        if (playerActionSelectUI != null)
        {
            idx = playerActionSelectUI.GetCurrentSelectedButtonIndex();
            Debug.Log($"[PlayerCombatant] UI에서 선택된 인덱스: {idx}");
        }
        else
        {
            Debug.LogWarning("[PlayerCombatant] PlayerActionSelectUI를 찾을 수 없음 - 기본값 0 사용");
            idx = 0;
        }
        
        // 범위 체크
        idx = UnityEngine.Mathf.Clamp(idx, 0, AvailableCommands.Count - 1);
        
        return new CommandSelection { selectedIndex = idx };
    }
}
