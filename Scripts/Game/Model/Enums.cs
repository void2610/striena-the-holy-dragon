/// <summary>
/// プレイヤーの立ち絵の種類
/// </summary>
public enum PlayerStance
{
    /// <summary>
    /// 通常状態
    /// </summary>
    Normal,
    
    /// <summary>
    /// 会話状態
    /// </summary>
    Dialogue,
    
    /// <summary>
    /// ダメージ状態
    /// </summary>
    Damage,
    
    /// <summary>
    /// 回復状態
    /// </summary>
    Heal,
    
    /// <summary>
    /// 死に際状態
    /// </summary>
    Dying
    
}

/// <summary>
/// プレイヤーのセリフの種類
/// </summary>
public enum PlayerDialogueType
{
    /// <summary>
    /// ダメージを受けた時のセリフ
    /// </summary>
    TakeDamage,
   
    /// <summary>
    /// HPが回復した時のセリフ
    /// </summary>
    Heal,
    
    /// <summary>
    /// 撤退が進行した時のセリフ
    /// </summary>
    RetreatProgress,
    
    /// <summary>
    /// ゲーム開始時のセリフ
    /// </summary>
    GameStart,
    
    /// <summary>
    /// 勝利時のセリフ
    /// </summary>
    Victory,
    
    /// <summary>
    /// 敗北時のセリフ
    /// </summary>
    Defeat,
    
    /// <summary>
    /// 危険カード使用時のセリフ
    /// </summary>
    DangerCardUsed,
}

/// <summary>
/// 戦闘エリアの種類
/// </summary>
public enum BattleAreaType
{
    /// <summary>
    /// 市場
    /// </summary>
    Market,
    
    /// <summary>
    /// 住宅街
    /// </summary>
    Residential,
    
    /// <summary>
    /// 裏路地
    /// </summary>
    BackAlley,
    
    /// <summary>
    /// 聖堂
    /// </summary>
    Cathedral,
}

public static class EnumExtensions
{
    public static string ToJapanese(this BattleAreaType type)
    {
        return type switch
        {
            BattleAreaType.Market => "市場",
            BattleAreaType.Residential => "住宅街",
            BattleAreaType.BackAlley => "裏路地",
            BattleAreaType.Cathedral => "聖堂",
            _ => "不明"
        };
    }
}