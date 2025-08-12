using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Void2610.UnityTemplate;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全てのカードデータを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "AllCardDataList", menuName = "Game/All Card Data List")]
public class AllCardDataList : ScriptableObject
{
    [SerializeField] private List<CardData> cardDataList = new();
    
    public List<CardData> CardDataList => cardDataList;
    
    /// <summary>
    /// 同じディレクトリ内の全てのCardDataを自動的に登録
    /// </summary>
    public void RegisterAllCards()
    {
#if UNITY_EDITOR
        var previousCount = cardDataList.Count;
        this.RegisterAssetsInSameDirectory(cardDataList, x => x.CardName);
        
        if (cardDataList.Count != previousCount)
        {
            Debug.Log($"AllCardDataList: {cardDataList.Count}枚のカードデータを自動登録しました");
        }
#endif
    }
    
    public CardData GetCardDataByName(string cardName)
    {
        return cardDataList.FirstOrDefault(card => card.CardName == cardName);
    }
}