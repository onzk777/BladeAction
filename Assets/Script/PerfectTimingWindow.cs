using System;
using UnityEngine;

[Serializable]
public class PerfectTimingWindow
{
    public float start;     // 타이밍 시작 시점 (초)
    public float duration;  // 성공 가능 구간 길이 (초)
    [Tooltip("이 히트의 공격력 배율 (1.0 = 100%, 1.5 = 150%)")]
    [Range(0.1f, 5.0f)]
    public float damageRatio = 1.0f;  // 이 히트의 공격력 배율

    public float End => start + duration;

    public bool Contains(float inputTime)
    {
        return inputTime >= start && inputTime <= End;
    }
}