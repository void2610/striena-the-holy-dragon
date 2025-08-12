using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// エンディング分岐の判定とエンディング情報の保存を管理するサービス
/// </summary>
public class EndingService
{
    // エンディング番号の定数
    private const int ENDING_GAME_OVER = 1;           // ゲームオーバー（体力0）
    private const int ENDING_ALL_EVACUATED = 2;       // 全員生還（生存率100%）
    private const int ENDING_HIGH_SURVIVAL = 3;       // 並以上の生存率（生存率50%以上）
    private const int ENDING_LOW_SURVIVAL = 4;        // 低い生存率（生存率50%未満）
    private const int ENDING_CORRUPTION = 5;           // 堕落（危険なカードを使用）
    
    // PlayerPrefsのキー
    private const string ENDING_NUMBER_KEY = "LastEndingNumber";
    private const string ENDING_TURN_KEY = "LastEndingTurn";
    private const string ENDING_SURVIVAL_RATE_KEY = "LastEndingSurvivalRate";
    private const string ENDING_EVACUATED_KEY = "LastEndingEvacuated";
    private const string ENDING_KILLED_KEY = "LastEndingKilled";
    private const string ENDING_DANGEROUS_CARD_COUNT_KEY = "LastEndingDangerousCardCount";
    private const string COLLECTED_ENDINGS_KEY = "CollectedEndings";
    
    public static bool IsTrueEnding(int endingNumber) => endingNumber == ENDING_ALL_EVACUATED;
    
    /// <summary>
    /// 保存されたエンディング情報を取得する
    /// </summary>
    public static (int endingNumber, int turn, float survivalRate, int evacuated, int killed) GetSavedEndingData()
    {
        return (
            PlayerPrefs.GetInt(ENDING_NUMBER_KEY, ENDING_GAME_OVER),
            PlayerPrefs.GetInt(ENDING_TURN_KEY, 0),
            PlayerPrefs.GetFloat(ENDING_SURVIVAL_RATE_KEY, 0f),
            PlayerPrefs.GetInt(ENDING_EVACUATED_KEY, 0),
            PlayerPrefs.GetInt(ENDING_KILLED_KEY, 0)
        );
    }
    
    /// <summary>
    /// 指定されたエンディングが回収済みかどうかを確認する
    /// </summary>
    /// <param name="endingNumber">確認するエンディング番号</param>
    /// <returns>回収済みの場合はtrue</returns>
    public static bool IsEndingCollected(int endingNumber)
    {
        var collectedEndings = GetCollectedEndings();
        return collectedEndings.Contains(endingNumber);
    }
    
    /// <summary>
    /// エンディング番号を判定する
    /// </summary>
    private int DetermineEnding(PlayerModel playerModel, int currentTurn, int maxTurns)
    {
        // プレイヤーが死亡している場合はゲームオーバーエンディング
        if (!playerModel.IsAlive.CurrentValue)
            return ENDING_GAME_OVER;
        
        // 20ターンに達した場合は時間切れ（ゲームオーバー）
        if (currentTurn >= maxTurns)
            return ENDING_GAME_OVER;
        
        // 危険なカードを使用した場合は堕落エンディング
        if (playerModel.DangerousCardUsageCount.CurrentValue > 1)
            return ENDING_CORRUPTION;
        
        // 全員生還（生存率100%）
        if (playerModel.SurvivalRate.CurrentValue >= 1.0f)
            return ENDING_ALL_EVACUATED;
        
        // 並以上の生存率（50%以上）
        if (playerModel.SurvivalRate.CurrentValue >= 0.5f)
            return ENDING_HIGH_SURVIVAL;
        
        // 低い生存率（50%未満）
        return ENDING_LOW_SURVIVAL;
    }
    
    private readonly GameSettings _gameSettings;
    
    public EndingService(GameSettings gameSettings)
    {
        _gameSettings = gameSettings;
    }
    
    /// <summary>
    /// エンディング番号を判定し、データを保存する
    /// </summary>
    public int DetermineAndSaveEnding(PlayerModel playerModel, int currentTurn)
    {
        // エンディング番号を判定
        var endingNumber = DetermineEnding(playerModel, currentTurn, _gameSettings.MaxTurns);
        
        // 最新のエンディングデータを保存
        PlayerPrefs.SetInt(ENDING_NUMBER_KEY, endingNumber);
        PlayerPrefs.SetInt(ENDING_TURN_KEY, currentTurn);
        PlayerPrefs.SetFloat(ENDING_SURVIVAL_RATE_KEY, playerModel.SurvivalRate.CurrentValue);
        PlayerPrefs.SetInt(ENDING_EVACUATED_KEY, playerModel.EvacuatedCitizens.CurrentValue);
        PlayerPrefs.SetInt(ENDING_KILLED_KEY, playerModel.KilledInActionCitizens.CurrentValue);
        PlayerPrefs.SetInt(ENDING_DANGEROUS_CARD_COUNT_KEY, playerModel.DangerousCardUsageCount.CurrentValue);
        
        // 回収済みエンディングに追加
        AddCollectedEnding(endingNumber);
        
        PlayerPrefs.Save();
        
        return endingNumber;
    }
    
    /// <summary>
    /// 回収済みエンディングに新しいエンディングを追加する
    /// </summary>
    /// <param name="endingNumber">追加するエンディング番号</param>
    private void AddCollectedEnding(int endingNumber)
    {
        var collectedEndings = GetCollectedEndings();
        
        // 既に回収済みの場合は追加しない
        if (collectedEndings.Contains(endingNumber)) return;
        
        collectedEndings.Add(endingNumber);
        // 回収済みエンディングを保存（カンマ区切り文字列として）
        PlayerPrefs.SetString(COLLECTED_ENDINGS_KEY, string.Join(",", collectedEndings));
    }
    
    /// <summary>
    /// 回収済みエンディングのリストを取得する
    /// </summary>
    /// <returns>回収済みエンディング番号のリスト</returns>
    private static List<int> GetCollectedEndings()
    {
        var collectedEndingsString = PlayerPrefs.GetString(COLLECTED_ENDINGS_KEY, "");
        var collectedEndings = new List<int>();
        
        if (!string.IsNullOrEmpty(collectedEndingsString))
        {
            var endingStrings = collectedEndingsString.Split(',');
            foreach (var endingString in endingStrings)
            {
                if (int.TryParse(endingString, out var endingNumber))
                {
                    collectedEndings.Add(endingNumber);
                }
            }
        }
        
        return collectedEndings;
    }
    
    /// <summary>
    /// 回収済みエンディング数を取得する
    /// </summary>
    /// <returns>回収済みエンディングの数</returns>
    public static int GetCollectedEndingCount()
    {
        return GetCollectedEndings().Count;
    }
}