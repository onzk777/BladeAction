using UnityEngine;

[System.Serializable]
public class CommandSelection
{
    [Tooltip("현재 선택된 커맨드의 인덱스(0 = 첫 번째)")]
    [Min(0)]
    public int selectedIndex;

    // 또는, 인덱스 대신 실제 명령 참조를 저장
    // public ActionCommand selectedCommand;
}