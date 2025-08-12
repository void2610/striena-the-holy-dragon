using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 個別のエンディングデータを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "EndingData", menuName = "Game/Ending Data")]
public class EndingData : ScriptableObject
{
    [Header("エンディング情報")]
    [SerializeField] private int endingNumber;
    [SerializeField] private string endingTitle;
    [SerializeField, TextArea(3, 5)] private string endingDescription;
    
    [Header("ストーリーテキスト")]
    [SerializeField, TextArea(2, 10)] private List<string> storyTexts = new();
    
    /// <summary>
    /// エンディング番号
    /// </summary>
    public int EndingNumber => endingNumber;
    
    /// <summary>
    /// エンディングタイトル
    /// </summary>
    public string EndingTitle => endingTitle;
    
    /// <summary>
    /// エンディング説明
    /// </summary>
    public string EndingDescription => endingDescription;
    
    /// <summary>
    /// ストーリーテキストリスト
    /// </summary>
    public List<string> StoryTexts => new List<string>(storyTexts);
}