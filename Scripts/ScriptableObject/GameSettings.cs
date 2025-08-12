using UnityEngine;

/// <summary>
/// ゲーム全体の設定を管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Game/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("プレイヤー設定")]
    [SerializeField] private int playerMaxHp = 150;
    [SerializeField] private int initialCitizens = 200;
    
    [Header("敵の設定")]
    [SerializeField] private int turnDeathCount = 5;
    
    [Header("ゲーム設定")]
    [SerializeField] private int maxTurns = 20;
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private int retreatTurnInterval = 5;
    
    [Header("カードプール設定")]
    [SerializeField] private int specialCardWeight = 3;
    [SerializeField] private int turnConditionThreshold = 15;
    [SerializeField] private int healthConditionThreshold = 20;
    [SerializeField] private int populationConditionThreshold = 50;
    
    public int PlayerMaxHp => playerMaxHp;
    public int InitialCitizens => initialCitizens;
    public int TurnDeathCount => turnDeathCount;
    public int MaxTurns => maxTurns;
    public int MaxHandSize => maxHandSize;
    public int RetreatTurnInterval => retreatTurnInterval;
    public int SpecialCardWeight => specialCardWeight;
    public int TurnConditionThreshold => turnConditionThreshold;
    public int HealthConditionThreshold => healthConditionThreshold;
    public int PopulationConditionThreshold => populationConditionThreshold;
}