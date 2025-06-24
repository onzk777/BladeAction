using UnityEngine;

public class GlobalConfig : MonoBehaviour
{
    [SerializeField, Tooltip("입력 허용 시간 (초)")]
    [Header("입력 윈도우(초)")]
    private float inputWindowSeconds = 1.0f;

    public float InputWindowSeconds => inputWindowSeconds;

    [SerializeField, Tooltip("AI 기본 완벽 입력 확률(테스트용)")]    
    public float npcActionPerfectRate = 0.5f;
}
