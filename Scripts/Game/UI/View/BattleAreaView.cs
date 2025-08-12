using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの体力を表示するView
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class BattleAreaView : MonoBehaviour
{
    private TextMeshProUGUI _healthText;
    
    /// <summary>
    /// 体力表示を更新
    /// </summary>
    public void UpdateBattleArea(BattleAreaType battleAreaType)
    {
        _healthText.text = $"エリア: {battleAreaType.ToJapanese()}";
    }
    
    private void Awake()
    {
        _healthText = this.GetComponent<TextMeshProUGUI>();
    }
}