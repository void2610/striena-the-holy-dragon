using UnityEngine;

/// <summary>
/// スコア計算を行うサービスクラス
/// </summary>
public class ScoreService
{
    /// <summary>
    /// ゲームクリア時のスコアを計算する
    /// </summary>
    /// <param name="survivalRate">生存率（0.0f〜1.0f）</param>
    /// <param name="turnCount">経過ターン数</param>
    /// <returns>計算されたスコア</returns>
    public int CalculateScore(float survivalRate, int turnCount)
    {
        // 基本スコア：生存率による得点（最大10000点）
        var survivalScore = Mathf.RoundToInt(survivalRate * 10000);
        
        // ターンボーナス：早くクリアするほど高得点
        // 最大20ターンで、早くクリアするほどボーナスが高い
        var turnBonus = 0;
        if (turnCount <= 20)
        {
            // 20ターン以内でクリアした場合のボーナス（最大5000点）
            turnBonus = Mathf.RoundToInt((20 - turnCount) / 20f * 5000);
        }
        
        // パーフェクトボーナス：生存率100%の場合
        var perfectBonus = 0;
        if (survivalRate >= 1.0f)
        {
            perfectBonus = 5000;
        }
        
        // 合計スコア
        return survivalScore + turnBonus + perfectBonus;
    }
    
    /// <summary>
    /// unityroomにスコアを送信する
    /// </summary>
    /// <param name="score">送信するスコア</param>
    /// <param name="boardNo">ボード番号（デフォルト1）</param>
    public void SendScoreToUnityroom(int score, int boardNo)
    {
        try
        {
            unityroom.Api.UnityroomApiClient.Instance.SendScore(boardNo, score, unityroom.Api.ScoreboardWriteMode.HighScoreDesc);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"スコア送信に失敗しました: {e.Message}");
        }
    }
    
    /// <summary>
    /// エンディング回収数をunityroomに送信する
    /// </summary>
    public void SendEndingCountToUnityroom(int endingCount, int boardNo)
    {
        try
        {
            // エンディング回収数をスコアとして送信（HighScoreDescで最大値を記録）
            unityroom.Api.UnityroomApiClient.Instance.SendScore(boardNo, endingCount, unityroom.Api.ScoreboardWriteMode.HighScoreDesc);
            Debug.Log($"エンディング回収数を送信しました: {endingCount}個 (ボード番号: {boardNo})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"エンディング回収数送信に失敗しました: {e.Message}");
        }
    }
}