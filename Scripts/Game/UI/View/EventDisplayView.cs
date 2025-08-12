using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

/// <summary>
/// ランダムイベント表示用のUIビュークラス
/// </summary>
public class EventDisplayView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI eventDescriptionText;
    [SerializeField] private TextMeshProUGUI effectDescriptionText;
    
    private MotionHandle _typewriterHandle;
    
    /// <summary>
    /// イベント情報を設定
    /// </summary>
    public void SetEventInfo(string eventDescription, string effectDescription)
    {
        // 既存のタイプライター処理をキャンセル
        if (_typewriterHandle.IsActive()) _typewriterHandle.Cancel();
        // タイプライター効果で表示
        ShowTypewriterText(eventDescription, effectDescription);
    }
    
    /// <summary>
    /// タイプライター効果でテキストを表示
    /// </summary>
    private void ShowTypewriterText(string eventDescription, string effectDescription)
    {
        // まずイベント説明をタイプライター効果で表示
        eventDescriptionText.text = "";
        effectDescriptionText.text = "";
        
        var totalText = eventDescription + "\n\n" + effectDescription;
        var eventDescLength = eventDescription.Length;
        
        _typewriterHandle = LMotion.Create(0, totalText.Length, 1f)
            .WithEase(Ease.Linear)
            .Bind(progress =>
            {
                var currentIndex = Mathf.RoundToInt(progress);
                
                if (currentIndex <= eventDescLength)
                {
                    // イベント説明の表示中
                    eventDescriptionText.text = eventDescription.Substring(0, currentIndex);
                }
                else
                {
                    // イベント説明完了、効果説明の表示中
                    eventDescriptionText.text = eventDescription;
                    var effectIndex = currentIndex - eventDescLength - 2; // "\n\n"分を引く
                    if (effectIndex > 0)
                    {
                        effectDescriptionText.text = effectDescription.Substring(0, Mathf.Min(effectIndex, effectDescription.Length));
                    }
                }
            });
    }
    
    
    private void OnDestroy()
    {
        if (_typewriterHandle.IsActive()) _typewriterHandle.Cancel();
    }
}