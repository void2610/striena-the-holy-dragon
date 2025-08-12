using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// エリア選択UIのビュークラス
/// </summary>
public class AreaSelectionView : MonoBehaviour
{
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private TextMeshProUGUI areaText1;
    [SerializeField] private TextMeshProUGUI areaText2;
    
    private event Action<int> OnButtonClicked;
    
    private void Awake()
    {
        // ボタンクリックイベントを設定
        button1.onClick.AddListener(() => OnButtonClicked?.Invoke(0));
        button2.onClick.AddListener(() => OnButtonClicked?.Invoke(1));
    }
    
    /// <summary>
    /// エリア選択UIを表示
    /// </summary>
    public void SetArea(BattleAreaType area1, BattleAreaType area2, Action<int> onButtonClicked)
    {
        OnButtonClicked = onButtonClicked;
        areaText1.text = area1.ToJapanese();
        areaText2.text = area2.ToJapanese();
    }
    
    private void OnDestroy()
    {
        // イベントリスナーをクリア
        button1.onClick.RemoveAllListeners();
        button2.onClick.RemoveAllListeners();
    }
}