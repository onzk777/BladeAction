using System;

[Serializable]
public class PerfectTimingWindow
{
    public float start;     // 타이밍 시작 시점 (초)
    public float duration;  // 성공 가능 구간 길이 (초)

    public float End => start + duration;

    public bool Contains(float inputTime)
    {
        return inputTime >= start && inputTime <= End;
    }

}