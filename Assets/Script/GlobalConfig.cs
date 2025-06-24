using UnityEngine;

public class GlobalConfig : MonoBehaviour
{
    [SerializeField, Tooltip("입력 허용 시간 (초)")]
    [Header("입력 윈도우(초)")]
    private float inputWindowSeconds = 1.0f;

    public float InputWindowSeconds => inputWindowSeconds;
}
