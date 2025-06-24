using System;

[Serializable]
public class PerfectTimingWindow
{
    public float start;     // 타이밍 시작 시점 (초 단위, 커맨드 시작 기준 상대 시간)
    public float duration;  // 입력 성공 가능 구간의 길이 (초 단위)
}
