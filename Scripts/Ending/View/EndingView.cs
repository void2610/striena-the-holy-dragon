using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using LitMotion;
using LitMotion.Extensions;
using Void2610.UnityTemplate;

/// <summary>
/// エンディング画面の表示を管理するビュー
/// </summary>
public class EndingView : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI endingTitleText;
    [SerializeField] private TextMeshProUGUI endingDescriptionText;
    [SerializeField] private TextMeshProUGUI statisticsText;
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private Button tweetButton;
    
    // エンディング情報を保持
    private EndingData _currentEndingData;
    private float _currentSurvivalRate;
    
    private void Awake()
    {
        backToTitleButton.onClick.AddListener(BackToTitle);
        
        // ツイートボタンのイベントリスナーを設定
        if (tweetButton)
        {
            tweetButton.onClick.AddListener(OnTweetButtonClicked);
        }
    }
    
    /// <summary>
    /// エンディング表示を更新
    /// </summary>
    public void UpdateEndingDisplay(EndingData endingData, string endingDescription, int turn, float survivalRate, int evacuated, int killed)
    {
        // エンディング情報を保持
        _currentEndingData = endingData;
        _currentSurvivalRate = survivalRate;
        
        // エンディングタイトルと説明を設定
        endingTitleText.text = $"END{endingData.EndingNumber}: {endingData.EndingTitle}";
        endingDescriptionText.text = endingDescription;
        
        // 統計情報
        var survivalPercentage = Mathf.RoundToInt(survivalRate * 100);
        statisticsText.text = $"経過ターン: {turn}\n" +
                             $"生存率: {survivalPercentage}%\n" +
                             $"撤退者数: {evacuated}人\n" +
                             $"戦死者数: {killed}人";
    }
    
    /// <summary>
    /// タイトルシーンに戻る
    /// </summary>
    private void BackToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
    
    /// <summary>
    /// ツイートボタンがクリックされた時の処理
    /// </summary>
    private void OnTweetButtonClicked()
    {
        if (_currentEndingData)
        {
            // ツイートサービスを使用してツイート画面を開く
            TweetService.TweetEnding(
                _currentEndingData.EndingNumber,
                _currentEndingData.EndingTitle,
                _currentSurvivalRate
            );
        }
    }
}