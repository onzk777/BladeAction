using UnityEngine;
using UnityEditor;
using TMPro;

public class StatusUILayout : MonoBehaviour
{
    public Transform parentTransform;
    public bool isPlayer;

    [ContextMenu("Create Status UI")]
    private void CreateStatusUI()
    {
        if (parentTransform == null)
        {
            Debug.LogError("Parent Transform is not set!");
            return;
        }

        CreateText("HP:", "HP", parentTransform);
        CreateText("Poise:", "Poise", parentTransform);
        CreateText("ATK:", "ATK", parentTransform);
        CreateText("DR:", "DR", parentTransform);
        CreateText("Crit:", "Crit", parentTransform);

        Debug.Log("Status UI created for " + (isPlayer ? "Player" : "Enemy"));
    }

    private TextMeshProUGUI CreateText(string label, string name, Transform parent)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent);
        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = label + " 0"; // Initial text
        tmpText.fontSize = 24;
        tmpText.color = Color.black;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;

        // Set RectTransform properties
        RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.sizeDelta = new Vector2(200, 30); // Adjust size as needed
        rectTransform.anchoredPosition = new Vector2(0, -30 * parent.childCount); // Stack vertically

        return tmpText;
    }

    [ContextMenu("Connect to CombatDebugDisplay")]
    private void ConnectToCombatDebugDisplay()
    {
        CombatDebugDisplay display = FindFirstObjectByType<CombatDebugDisplay>();
        if (display == null)
        {
            Debug.LogError("CombatDebugDisplay not found in scene!");
            return;
        }

        if (isPlayer)
        {
            display.playerHP = parentTransform.Find("HP")?.GetComponent<TextMeshProUGUI>();
            display.playerPoise = parentTransform.Find("Poise")?.GetComponent<TextMeshProUGUI>();
            display.playerATK = parentTransform.Find("ATK")?.GetComponent<TextMeshProUGUI>();
            display.playerDR = parentTransform.Find("DR")?.GetComponent<TextMeshProUGUI>();
            display.playerCrit = parentTransform.Find("Crit")?.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            display.enemyHP = parentTransform.Find("HP")?.GetComponent<TextMeshProUGUI>();
            display.enemyPoise = parentTransform.Find("Poise")?.GetComponent<TextMeshProUGUI>();
            display.enemyATK = parentTransform.Find("ATK")?.GetComponent<TextMeshProUGUI>();
            display.enemyDR = parentTransform.Find("DR")?.GetComponent<TextMeshProUGUI>();
            display.enemyCrit = parentTransform.Find("Crit")?.GetComponent<TextMeshProUGUI>();
        }
        Debug.Log("Status UI connected to CombatDebugDisplay for " + (isPlayer ? "Player" : "Enemy"));
    }
}
