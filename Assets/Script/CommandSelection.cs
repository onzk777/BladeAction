using UnityEngine;

[System.Serializable]
public class CommandSelection
{
    [Tooltip("���� ���õ� Ŀ�ǵ��� �ε���(0 = ù ��°)")]
    [Min(0)]
    public int selectedIndex;

    // �Ǵ�, �ε��� ��� ���� ��� ������ ����
    // public ActionCommand selectedCommand;
}