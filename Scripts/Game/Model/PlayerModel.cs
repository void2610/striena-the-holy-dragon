using R3;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーのデータモデル
/// </summary>
public class PlayerModel
{
    // プレイヤーの最大体力
    private readonly int _maxHp;
    
    // ゲーム設定
    private readonly GameSettings _gameSettings;
    
    // プレイヤーの現在体力
    public ReactiveProperty<int> CurrentHp { get; }
    
    // 利用可能な市民の数（カードコストとして使用）
    public ReactiveProperty<int> AvailableCitizens { get; }
    
    // 撤退完了した市民の数
    public ReactiveProperty<int> EvacuatedCitizens { get; }
    
    // 戦死した市民の数
    public ReactiveProperty<int> KilledInActionCitizens { get; }
    
    // 危険なカードの使用回数
    public ReactiveProperty<int> DangerousCardUsageCount { get; }
    
    // 次のダメージを軽減するフラグ
    public ReactiveProperty<bool> ReduceDamageNext { get; }
    
    // 現在の最大手札数
    public ReactiveProperty<int> MaxHandSize { get; }
    
    // 手札のカードデータ
    public List<CardData> HandCards { get; }
    
    // 手札が変更されたときのイベント
    public Subject<Unit> HandCardsChanged { get; }
    
    // 無効化されたカードと残りターン数
    public Dictionary<CardData, int> DisabledCards { get; }
    
    
    // プレイヤーが生存しているか
    public ReadOnlyReactiveProperty<bool> IsAlive { get; }
    
    // 全市民が撤退完了したか
    public ReadOnlyReactiveProperty<bool> AllCitizensEvacuated { get; }
    
    // 撤退進行度（0.0f〜1.0f）
    public ReadOnlyReactiveProperty<float> EvacuationProgress { get; }
    
    // 生存率（0.0f〜1.0f）
    public ReadOnlyReactiveProperty<float> SurvivalRate { get; }

    public PlayerModel(GameSettings gameSettings)
    {
        _gameSettings = gameSettings;
        _maxHp = _gameSettings.PlayerMaxHp;
        var initialPopulation = _gameSettings.InitialCitizens;
        
        CurrentHp = new ReactiveProperty<int>(_maxHp);
        AvailableCitizens = new ReactiveProperty<int>(_gameSettings.InitialCitizens);
        EvacuatedCitizens = new ReactiveProperty<int>(0);
        KilledInActionCitizens = new ReactiveProperty<int>(0);
        DangerousCardUsageCount = new ReactiveProperty<int>(0);
        ReduceDamageNext = new ReactiveProperty<bool>(false);
        MaxHandSize = new ReactiveProperty<int>(_gameSettings.MaxHandSize);
        HandCards = new List<CardData>();
        HandCardsChanged = new Subject<Unit>();
        DisabledCards = new Dictionary<CardData, int>();
        
        // 体力が0より大きければ生存
        IsAlive = CurrentHp
            .Select(hp => hp > 0)
            .ToReadOnlyReactiveProperty();
            
        // 全市民が撤退完了したかチェック
        AllCitizensEvacuated = AvailableCitizens
            .CombineLatest(EvacuatedCitizens, (available, evacuated) => 
                available == 0 && evacuated > 0)
            .ToReadOnlyReactiveProperty();
            
        // 撤退進行度を計算
        EvacuationProgress = AvailableCitizens
            .CombineLatest(EvacuatedCitizens, (available, evacuated) =>
            {
                var total = available + evacuated;
                return total > 0 ? (float)evacuated / total : 0f;
            })
            .ToReadOnlyReactiveProperty();
        
        // 生存率を計算（生存者 / 初期人口）
        SurvivalRate = AvailableCitizens
            .CombineLatest(EvacuatedCitizens, KilledInActionCitizens, 
                (available, evacuated, killed) =>
                {
                    var survivors = available + evacuated;
                    return initialPopulation > 0 ? (float)survivors / initialPopulation : 0f;
                })
            .ToReadOnlyReactiveProperty();
    }
    
    /// <summary>
    /// ダメージを受ける
    /// </summary>
    public void TakeDamage(int damage)
    {
        var actualDamage = damage;
        
        // ダメージ軽減フラグが有効な場合、ダメージを半分にする
        if (ReduceDamageNext.Value)
        {
            actualDamage = (int)Math.Ceiling(damage * 0.5); // 切り上げで半分
            ReduceDamageNext.Value = false; // フラグをリセット
        }
        
        CurrentHp.Value = Math.Max(0, CurrentHp.Value - actualDamage);
    }
    
    /// <summary>
    /// 体力を回復する
    /// </summary>
    public void Heal(int healAmount)
    {
        CurrentHp.Value = Math.Min(_maxHp, CurrentHp.Value + healAmount);
    }
    
    /// <summary>
    /// 市民を撤退させる
    /// </summary>
    public void EvacuateCitizens(int citizenCount)
    {
        var toEvacuate = Math.Min(citizenCount, AvailableCitizens.Value);
        AvailableCitizens.Value -= toEvacuate;
        EvacuatedCitizens.Value += toEvacuate;
    }
    
    /// <summary>
    /// 市民を増援として呼ぶ（カード効果）
    /// </summary>
    public void CallReinforcements(int citizenCount)
    {
        AvailableCitizens.Value += citizenCount;
    }
    
    /// <summary>
    /// 市民を犠牲にする（カードコスト）
    /// </summary>
    public void SacrificeCitizens(int count)
    {
        AvailableCitizens.Value = Math.Max(0, AvailableCitizens.Value - count);
        KilledInActionCitizens.Value += count; // 戦死者として記録
    }
    
    /// <summary>
    /// 毎ターン一定数の市民が死亡する処理
    /// </summary>
    public void ProcessTurnDeaths(int deathCount)
    {
        var actualDeaths = Math.Min(deathCount, AvailableCitizens.Value);
        if (actualDeaths > 0)
        {
            AvailableCitizens.Value -= actualDeaths;
            KilledInActionCitizens.Value += actualDeaths;
        }
    }
    
    /// <summary>
    /// 体力を1にする
    /// </summary>
    public void ReduceHealthToOne()
    {
        CurrentHp.Value = 1;
    }
    
    /// <summary>
    /// 手札にカードを追加
    /// </summary>
    public void DrawCard(CardData cardData)
    {
        HandCards.Add(cardData);
        HandCardsChanged.OnNext(Unit.Default);
    }
    
    /// <summary>
    /// 手札からカードを削除
    /// </summary>
    public void RemoveCard(CardData cardData)
    {
        if (HandCards.Remove(cardData))
        {
            HandCardsChanged.OnNext(Unit.Default);
        }
    }
    
    /// <summary>
    /// 危険なカードの使用回数を増やす
    /// </summary>
    public void IncrementDangerousCardUsage()
    {
        DangerousCardUsageCount.Value++;
    }
    
    /// <summary>
    /// 次のダメージを軽減するフラグを設定
    /// </summary>
    public void SetDamageReductionNext()
    {
        ReduceDamageNext.Value = true;
    }
    
    /// <summary>
    /// 最大手札数を変更する
    /// </summary>
    public void ChangeMaxHandSize(int change)
    {
        MaxHandSize.Value = Math.Max(0, MaxHandSize.Value + change);
    }
    
    /// <summary>
    /// ランダムなカードを指定したカードに交換する
    /// </summary>
    public bool ReplaceRandomCard(CardData newCard)
    {
        if (HandCards.Count == 0 || newCard == null) return false;
        
        var randomIndex = UnityEngine.Random.Range(0, HandCards.Count);
        HandCards[randomIndex] = newCard;
        HandCardsChanged.OnNext(Unit.Default);
        return true;
    }
    
    /// <summary>
    /// ランダムなカードを無効化する
    /// </summary>
    public void DisableRandomCards(int count)
    {
        var enabledCards = new List<CardData>();
        
        // 無効化されていないカードのリストを作成
        foreach (var card in HandCards)
        {
            if (!DisabledCards.ContainsKey(card))
            {
                enabledCards.Add(card);
            }
        }
        
        // 無効化するカード数を調整
        var disableCount = Math.Min(count, enabledCards.Count);
        
        // ランダムに選択して無効化
        for (var i = 0; i < disableCount; i++)
        {
            var randomIndex = UnityEngine.Random.Range(0, enabledCards.Count);
            var cardToDisable = enabledCards[randomIndex];
            
            DisabledCards[cardToDisable] = 1; // 1ターン無効化
            enabledCards.RemoveAt(randomIndex);
            
            // UIに通知
            HandCardsChanged.OnNext(Unit.Default);
        }
    }
    
    /// <summary>
    /// カードが無効化されているか確認
    /// </summary>
    public bool IsCardDisabled(CardData card)
    {
        return DisabledCards.ContainsKey(card);
    }
    
    /// <summary>
    /// ターン経過時に無効化を解除
    /// </summary>
    public void UpdateDisabledCards()
    {
        var cardsToRemove = new List<CardData>();
        var updatedCards = new Dictionary<CardData, int>();
        
        foreach (var kvp in DisabledCards)
        {
            var remainingTurns = kvp.Value - 1;
            if (remainingTurns <= 0)
            {
                cardsToRemove.Add(kvp.Key);
            }
            else
            {
                updatedCards[kvp.Key] = remainingTurns;
            }
        }
        
        // 無効化を解除
        foreach (var card in cardsToRemove)
        {
            DisabledCards.Remove(card);
        }
        
        // 残りターン数を更新
        foreach (var kvp in updatedCards)
        {
            DisabledCards[kvp.Key] = kvp.Value;
        }
        
        if (cardsToRemove.Count > 0 || updatedCards.Count > 0)
        {
            HandCardsChanged.OnNext(Unit.Default);
        }
    }
}