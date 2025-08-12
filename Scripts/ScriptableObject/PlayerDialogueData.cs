using UnityEngine;
using Void2610.UnityTemplate;

/// <summary>
/// プレイヤーのセリフデータを管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "PlayerDialogueData", menuName = "ScriptableObject/PlayerDialogueData")]
public class PlayerDialogueData : ScriptableObject
{
    [SerializeField] private SerializableDictionary<PlayerDialogueType, string[]> dialogues = new();
    
    /// <summary>
    /// 指定されたタイプのセリフをランダムに取得する
    /// </summary>
    /// <param name="type">セリフの種類</param>
    /// <returns>ランダムに選ばれたセリフ</returns>
    public string GetRandomDialogue(PlayerDialogueType type)
    {
        if (dialogues.TryGetValue(type, out var dialogueList) && dialogueList.Length > 0)
        {
            var randomIndex = Random.Range(0, dialogueList.Length);
            return dialogueList[randomIndex];
        }
        
        Debug.LogWarning($"セリフが設定されていません: {type}");
        return "";
    }
    
    /// <summary>
    /// 指定されたタイプのセリフが設定されているかどうか確認する
    /// </summary>
    /// <param name="type">セリフの種類</param>
    /// <returns>セリフが設定されている場合はtrue</returns>
    public bool HasDialogue(PlayerDialogueType type)
    {
        return dialogues.TryGetValue(type, out var dialogueList) && dialogueList.Length > 0;
    }
}