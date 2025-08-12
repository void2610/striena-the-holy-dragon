using System.Collections.Generic;
using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// 全てのエンディングデータを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "AllEndingDataList", menuName = "Game/All Ending Data List")]
public class AllEndingDataList : ScriptableObject
{
    [SerializeField] private List<EndingData> endingDataList = new();
    
    /// <summary>
    /// 指定されたエンディング番号のデータを取得
    /// </summary>
    /// <param name="endingNumber">エンディング番号</param>
    /// <returns>エンディングデータ、見つからない場合はnull</returns>
    public EndingData GetEndingData(int endingNumber)
    {
        return endingDataList.Find(data => data.EndingNumber == endingNumber);
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // エンディング番号の重複チェック
        var endingNumbers = new List<int>();
        foreach (var data in endingDataList)
        {
            if (!data) continue;
            
            if (endingNumbers.Contains(data.EndingNumber))
                Debug.LogWarning($"エンディング番号 {data.EndingNumber} が重複しています");
            else
                endingNumbers.Add(data.EndingNumber);
        }
    }
#endif
    
    public void RegisterAllEndings()
    {
#if UNITY_EDITOR
        var previousCount = endingDataList.Count;
        this.RegisterAssetsInSameDirectory(endingDataList, x => x.EndingNumber.ToString());
        
        if (endingDataList.Count != previousCount)
        {
            Debug.Log($"AllEventDataList: {endingDataList.Count}個のイベントデータを自動登録しました");
        }
#endif
    }
}