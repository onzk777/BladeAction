using UnityEngine;

public class GlobalConfig : MonoBehaviour
{
    [SerializeField, Tooltip("�Է� ��� �ð� (��)")]
    [Header("�Է� ������(��)")]
    private float inputWindowSeconds = 1.0f;

    public float InputWindowSeconds => inputWindowSeconds;
}
