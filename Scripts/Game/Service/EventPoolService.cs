using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// イベントプールを管理するサービス
/// </summary>
public class EventPoolService
{
    // すべてのイベントデータ
    private readonly List<EventData> _allEvents;
    
    public EventPoolService(AllEventDataList allEventDataList)
    {
        // AllEventDataListからすべてのEventDataを取得
        allEventDataList.RegisterAllEvents();
        _allEvents = allEventDataList.EventDataList;
        
        if (_allEvents.Count == 0)
        {
            Debug.LogWarning("イベントデータが見つかりません。AllEventDataListにイベントデータを配置してください。");
        }
        else
        {
            Debug.Log($"EventPoolService: {_allEvents.Count}個のイベントデータを読み込みました");
        }
    }
    
    /// <summary>
    /// 現在のエリアで発生可能なランダムイベントを取得
    /// </summary>
    public EventData GetRandomEvent(BattleAreaType currentArea)
    {
        // 現在のエリアで発生可能なイベントをフィルタリング
        var availableEvents = _allEvents
            .Where(e => e.CanOccurInArea(currentArea))
            .ToList();
        
        if (availableEvents.Count == 0)
        {
            Debug.LogWarning($"エリア {currentArea} で発生可能なイベントがありません。");
            return null;
        }
        
        // ランダムに1つ選択
        int randomIndex = Random.Range(0, availableEvents.Count);
        return availableEvents[randomIndex];
    }
}