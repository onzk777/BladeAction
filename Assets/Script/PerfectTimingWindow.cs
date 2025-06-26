using System;

[Serializable]
public class PerfectTimingWindow
{
    public float start;     // Ÿ�̹� ���� ���� (��)
    public float duration;  // ���� ���� ���� ���� (��)

    public float End => start + duration;

    public bool Contains(float inputTime)
    {
        return inputTime >= start && inputTime <= End;
    }

}