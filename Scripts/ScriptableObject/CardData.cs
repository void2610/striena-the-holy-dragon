using System;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// カードデータを定義するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewCardData", menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string cardName;
    [SerializeField] private Sprite cardImage;
    [SerializeField, TextArea(3, 5)] private string description;
    
    [Header("コスト")]
    [SerializeField] private int healthCost;
    [SerializeField] private int citizenCost;
    
    [Header("効果")]
    [SerializeField] private float enemyDelayAmount = 0.2f;
    [SerializeField] private int healAmount = 0;
    [SerializeField] private int citizenGainAmount = 0;
    [SerializeField] private int evacuationStartAmount = 0;
    
    [Header("ランダム効果")]
    [SerializeField] private bool useRandomEvacuation = false; // ランダム撤退を使用するか
    [SerializeField] private int evacuationMin = 0; // 撤退人数の最小値
    [SerializeField] private int evacuationMax = 0; // 撤退人数の最大値
    [SerializeField] private bool useRandomHealthCost = false; // ランダム体力コストを使用するか
    [SerializeField] private int healthCostMin = 0; // 体力コストの最小値
    [SerializeField] private int healthCostMax = 0; // 体力コストの最大値
    
    [Header("特殊効果")]
    [SerializeField] private int enemyStunTurns = 0; // 敵をスタンさせるターン数（死亡防止効果）
    [SerializeField] private bool reduceHealthToOne = false; // 現在の体力を1にする（撤退コストとして使用）
    
    [Header("危険度")]
    [SerializeField] private bool isDangerousCard = false; // 邪悪な力を使う危険なカード（堕落エンディングの条件）
    
    [Header("抽選設定")]
    [SerializeField, Range(0.1f, 10f)] private float drawWeight = 1f; // 抽選時の重み（高いほど出やすい）
    
    public string CardName => cardName;
    public Sprite CardImage => cardImage;
    public string Description => description;
    public int HealthCost => healthCost;
    public int CitizenCost => citizenCost;
    public float EnemyDelayAmount => enemyDelayAmount;
    public int HealAmount => healAmount;
    public int CitizenGainAmount => citizenGainAmount;
    public int EvacuationStartAmount => evacuationStartAmount;
    public int EnemyStunTurns => enemyStunTurns;
    public bool UseCurrentHpAsEvacuationCost => reduceHealthToOne;
    public bool UseRandomEvacuation => useRandomEvacuation;
    public int EvacuationMin => evacuationMin;
    public int EvacuationMax => evacuationMax;
    public bool UseRandomHealthCost => useRandomHealthCost;
    public int HealthCostMin => healthCostMin;
    public int HealthCostMax => healthCostMax;
    public bool IsDangerousCard => isDangerousCard;
    public float DrawWeight => drawWeight;
    
    /// <summary>
    /// カード効果を適用
    /// </summary>
    public void ApplyEffect(PlayerModel playerModel, GameManager gameManager)
    {
        // 特殊コスト処理
        if (reduceHealthToOne)
        {
            playerModel.ReduceHealthToOne();
        }
        
        // 通常のコストを支払う
        if (useRandomHealthCost)
        {
            // ランダム体力コスト
            var randomHealthCost = UnityEngine.Random.Range(healthCostMin, healthCostMax + 1);
            playerModel.TakeDamage(randomHealthCost);
        }
        else if (healthCost > 0)
        {
            playerModel.TakeDamage(healthCost);
            SeManager.Instance.PlaySe("PlayerDamage");
            ParticleManager.Instance.PlayParticle("EnemyAttack");
        }
        
        if (citizenCost > 0)
        {
            playerModel.SacrificeCitizens(citizenCost);
        }
        
        // 基本効果を適用
        if (healAmount > 0)
        {
            playerModel.Heal(healAmount);
        }
        
        if (citizenGainAmount > 0)
        {
            playerModel.CallReinforcements(citizenGainAmount);
        }
        
        if (evacuationStartAmount > 0)
        {
            playerModel.EvacuateCitizens(evacuationStartAmount);
        }
        
        // ランダム撤退効果
        if (useRandomEvacuation)
        {
            var randomEvacuation = UnityEngine.Random.Range(evacuationMin, evacuationMax + 1);
            playerModel.EvacuateCitizens(randomEvacuation);
        }
        
        // 特殊効果を適用
        if (enemyStunTurns > 0)
        {
            gameManager.StunEnemy(enemyStunTurns);
            ParticleManager.Instance.PlayParticle("PlayerAttack");
            SeManager.Instance.PlaySe("PlayerAttack");
        }
    }
    
    /// <summary>
    /// カード名を基にした等価性判定
    /// </summary>
    public override bool Equals(object obj)
    {
        if (obj is CardData other)
        {
            return cardName == other.cardName;
        }
        return false;
    }
    
    /// <summary>
    /// カード名を基にしたハッシュコード生成
    /// </summary>
    public override int GetHashCode()
    {
        return cardName?.GetHashCode() ?? 0;
    }
}