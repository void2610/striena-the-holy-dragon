using TMPro;
using UnityEngine;

/// <summary>
/// ターン数を表示するView
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class TurnCountView : MonoBehaviour
{
    private TextMeshProUGUI _turnText;
    
    /// <summary>
    /// ターン数表示を更新
    /// </summary>
    public void UpdateTurnDisplay(int currentTurn)
    {
        _turnText.text = $"ターン: {currentTurn}";
    }
    
    private void Awake()
    {
        _turnText = this.GetComponent<TextMeshProUGUI>();
    }
}