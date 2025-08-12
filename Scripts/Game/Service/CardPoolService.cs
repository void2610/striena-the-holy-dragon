using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// カードプールを管理するサービス
/// </summary>
public class CardPoolService
{
    private const string TURN_CONDITION_CARD = "祈る";
    private const string TURN_CONDITION_CARD_2 = "謎の魔法";
    private const string HEALTH_CONDITION_CARD = "生力吸収";
    private const string POPULATION_CONDITION_CARD = "背水の陣";
    
    // 特殊条件カードの出現率倍率
    private readonly int _conditionCardWeightMultiplier; // 通常の3倍の確率
    private readonly int _turnConditionThreshold; // ターン数条件
    private readonly int _healthConditionThreshold; // 体力条件
    private readonly int _populationConditionThreshold; // 人口条件

    private readonly AllCardDataList _allCardDataList;
    private readonly List<CardData> _cardDataList;
    
    // 条件達成フラグ（一度だけカードを追加するため）
    private bool _turnConditionMet;
    private bool _healthConditionMet;
    private bool _populationConditionMet;
    
    public CardPoolService(AllCardDataList allCardDataList, GameSettings gameSettings)
    {
        _allCardDataList = allCardDataList;
        _allCardDataList.RegisterAllCards();
        _cardDataList = new List<CardData>(_allCardDataList.CardDataList);
        
        _conditionCardWeightMultiplier = gameSettings.SpecialCardWeight;
        _turnConditionThreshold = gameSettings.TurnConditionThreshold;
        _healthConditionThreshold = gameSettings.HealthConditionThreshold;
        _populationConditionThreshold = gameSettings.PopulationConditionThreshold;
        
        // 条件に応じてカードをフィルタリング
        _cardDataList.RemoveAll(card => 
            card.CardName is TURN_CONDITION_CARD or TURN_CONDITION_CARD_2 or HEALTH_CONDITION_CARD or POPULATION_CONDITION_CARD);
            
        // イベントのカードを除外
        _cardDataList.RemoveAll(card => card.CardName is "行動不能");
    }
    
    /// <summary>
    /// ランダムなカードを取得（重み付き抽選）
    /// </summary>
    public CardData GetRandomCard()
    {
        if (_cardDataList.Count == 0)
        {
            Debug.LogWarning("カードプールが空です");
            return null;
        }
        
        // 全カードの重みの合計を計算
        float totalWeight = 0f;
        foreach (var card in _cardDataList)
        {
            var weight = GetCardWeight(card);
            totalWeight += weight;
        }
        
        // 0から合計重みまでの乱数を生成
        var randomValue = Random.Range(0f, totalWeight);
        
        // 累積重みを計算して抽選
        float cumulativeWeight = 0f;
        foreach (var card in _cardDataList)
        {
            var weight = GetCardWeight(card);
            cumulativeWeight += weight;
            
            if (randomValue <= cumulativeWeight)
            {
                return card;
            }
        }
        
        // フォールバック：最後のカードを返す（通常は到達しない）
        return _cardDataList[_cardDataList.Count - 1];
    }
    
    /// <summary>
    /// カードの重みを取得（特殊条件カードは倍率適用）
    /// </summary>
    private float GetCardWeight(CardData card)
    {
        var baseWeight = card.DrawWeight;
        
        // 特殊条件カードの重みを増加
        var isConditionCard = card.CardName is TURN_CONDITION_CARD or TURN_CONDITION_CARD_2 or HEALTH_CONDITION_CARD or POPULATION_CONDITION_CARD;
        if (isConditionCard)
        {
            baseWeight *= _conditionCardWeightMultiplier;
        }
        
        return baseWeight;
    }
    
    /// <summary>
    /// 現在の状況に基づいてカードプールを更新
    /// </summary>
    public void UpdateCondition(int elapsedTurn, int playerHealth, int remainingPopulation)
    {
        if (elapsedTurn > _turnConditionThreshold && !_turnConditionMet)
        {
            var turnCard = _allCardDataList.GetCardDataByName(TURN_CONDITION_CARD);
            _cardDataList.Add(turnCard);
            var turnCard2 = _allCardDataList.GetCardDataByName(TURN_CONDITION_CARD_2);
            _cardDataList.Add(turnCard2);
            _turnConditionMet = true;
        }
        
        // 体力条件（15以下で一度だけ追加）
        if (playerHealth < _healthConditionThreshold && !_healthConditionMet)
        {
            var healthCard = _allCardDataList.GetCardDataByName(HEALTH_CONDITION_CARD);
            _cardDataList.Add(healthCard);
            _healthConditionMet = true;
        }
        
        // 人口条件（10人以下で一度だけ追加）
        if (remainingPopulation < _populationConditionThreshold && !_populationConditionMet)
        {
            var populationCard = _allCardDataList.GetCardDataByName(POPULATION_CONDITION_CARD);
            _cardDataList.Add(populationCard);
            _populationConditionMet = true;
        }
    }
}