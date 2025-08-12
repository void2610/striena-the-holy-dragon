using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Void2610.UnityTemplate;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全てのイベントデータを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "AllEventDataList", menuName = "Game/All Event Data List")]
public class AllEventDataList : ScriptableObject
{
    [SerializeField] private List<EventData> eventDataList = new();
    
    public List<EventData> EventDataList => eventDataList;
    
    /// <summary>
    /// 同じディレクトリ内の全てのEventDataを自動的に登録
    /// </summary>
    public void RegisterAllEvents()
    {
#if UNITY_EDITOR
        var previousCount = eventDataList.Count;
        this.RegisterAssetsInSameDirectory(eventDataList, x => x.name);
        
        if (eventDataList.Count != previousCount)
        {
            Debug.Log($"AllEventDataList: {eventDataList.Count}個のイベントデータを自動登録しました");
        }
#endif
    }
}