using System;
using UnityEngine;

/// <summary>
/// ツイート機能を提供するサービスクラス
/// </summary>
public static class TweetService
{
    private const string TWEET_URL = "https://x.com/intent/tweet";
    private const string GAME_URL = "https://unityroom.com/games/striena-the-holy-dragon";
    private const string GAME_TITLE = "聖なる竜のストリエナ";
    
    /// <summary>
    /// エンディング情報からツイートを開く（ワンステップ実行）
    /// </summary>
    public static void TweetEnding(int endingNumber, string endingTitle, float survivalRate)
    {
        var url = CreateEndingTweetUrl(endingNumber, endingTitle, survivalRate);
        Application.OpenURL(url);
    }
    
    /// <summary>
    /// エンディング情報からツイート用URLを生成する
    /// </summary>
    /// <param name="endingNumber">エンディング番号</param>
    /// <param name="endingTitle">エンディングタイトル</param>
    /// <param name="survivalRate">生存率</param>
    /// <returns>ツイート用URL</returns>
    private static string CreateEndingTweetUrl(int endingNumber, string endingTitle, float survivalRate)
    {
        // 生存率をパーセント表記に変換
        var survivalPercentage = Mathf.RoundToInt(survivalRate * 100);
        
        // ツイート文を作成
        var tweetText = $"{GAME_TITLE}で、{survivalPercentage}%の市民を撤退させました！\n\n" +
                        $"END{endingNumber}: {endingTitle}\n\n" +
                        $"{GAME_URL}\n\n" +
                        $"#unityroom #{GAME_TITLE}";
        
        // URLエンコード
        var encodedText = Uri.EscapeDataString(tweetText);
        
        // 完成したツイート用URL
        return $"{TWEET_URL}?text={encodedText}";
    }
}