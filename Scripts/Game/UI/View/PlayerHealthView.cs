using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの体力を表示するView
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class PlayerHealthView : MonoBehaviour
{
    private TextMeshProUGUI _healthText;
    
    /// <summary>
    /// 体力表示を更新
    /// </summary>
    public void UpdateHealthDisplay(int currentHp)
    {
        _healthText.text = $"HP: {currentHp}";
    }
    
    private void Awake()
    {
        _healthText = this.GetComponent<TextMeshProUGUI>();
    }
}