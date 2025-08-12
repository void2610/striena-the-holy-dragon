using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ランダムイベントのデータを定義するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "EventData", menuName = "Game/EventData", order = 2)]
public class EventData : ScriptableObject
{
    [Header("基本情報")]
    
    [Header("説明文")]
    [Tooltip("イベントの説明文")]
    [TextArea(3, 5)]
    [SerializeField] private string eventDescription;
    
    [Tooltip("効果の説明文")]
    [TextArea(2, 4)]
    [SerializeField] private string effectDescription;
    
    [Header("発生条件")]
    [Tooltip("汎用イベント（すべてのエリアで発生）")]
    [SerializeField] private bool isUniversal;
    
    [Tooltip("発生可能なバトルエリア（汎用イベントの場合は無視）")]
    [SerializeField] private BattleAreaType availableArea;
    
    [Header("効果パラメータ（暫定）")]
    [Tooltip("体力変化量（正で回復、負でダメージ）")]
    [SerializeField] private int hpChange;
    
    [Tooltip("市民変化量（正で増加、負で減少）")]
    [SerializeField] private int citizenChange;
    
    [Tooltip("敵スタンターン数")]
    [SerializeField] private int enemyStunTurns;
    
    [Tooltip("手札をシャッフルするかどうか")]
    [SerializeField] private bool shuffleHand;
    
    [Tooltip("次のダメージを半分にするかどうか")]
    [SerializeField] private bool reduceDamageNextTime;
    
    [Tooltip("手札数の変更（正でドロー、負でランダム破棄）")]
    [SerializeField] private int handCardChange;
    
    [Tooltip("ランダムなカードをこのカードに変換する（nullなら何もしない）")]
    [SerializeField] private CardData replaceCard;
    
    [Tooltip("ランダムなカードを1ターン使用不能にする数")]
    [SerializeField] private int disableCardCount;
    
    public string EventDescription => eventDescription;
    public string EffectDescription => effectDescription;
    public bool IsUniversal => isUniversal;
    public BattleAreaType AvailableArea => availableArea;
    public int HpChange => hpChange;
    public int CitizenChange => citizenChange;
    public int EnemyStunTurns => enemyStunTurns;
    public bool ShuffleHand => shuffleHand;
    public bool ReduceDamageNextTime => reduceDamageNextTime;
    public int HandCardChange => handCardChange;
    public CardData ReplaceCard => replaceCard;
    public int DisableCardCount => disableCardCount;
    
    /// <summary>
    /// 指定されたエリアでこのイベントが発生可能かチェック
    /// </summary>
    public bool CanOccurInArea(BattleAreaType area)
    {
        // 汎用イベントならどのエリアでも発生可能
        if (isUniversal) return true;
        
        // 指定されたエリアと一致するかチェック
        return availableArea == area;
    }
    
    /// <summary>
    /// イベント効果を適用
    /// </summary>
    public void ApplyEffect(PlayerModel player, GameManager gameManager)
    {
        // 体力変化
        if (hpChange > 0)
        {
            player.Heal(hpChange);
        }
        else if (hpChange < 0)
        {
            player.TakeDamage(-hpChange);
        }
        
        // 市民変化
        if (citizenChange > 0)
        {
            player.CallReinforcements(citizenChange);
        }
        else if (citizenChange < 0)
        {
            player.SacrificeCitizens(-citizenChange);
        }
        
        // 敵スタン
        if (enemyStunTurns > 0)
        {
            gameManager.StunEnemy(enemyStunTurns);
        }
        
        // 手札引き直し
        if (shuffleHand)
        {
            gameManager.ResetHandAsync(false).Forget();
        }
        
        // ダメージ軽減
        if (reduceDamageNextTime)
        {
            player.SetDamageReductionNext();
        }
        
        // 手札数変更
        if (handCardChange > 0)
        {
            // 追加ドロー
            gameManager.DrawAdditionalCards(handCardChange);
        }
        else if (handCardChange < 0)
        {
            // ランダム破棄
            gameManager.RemoveRandomCards(-handCardChange);
        }
        
        // カード交換
        if (replaceCard != null)
        {
            player.ReplaceRandomCard(replaceCard);
        }
        
        // カード無効化
        if (disableCardCount > 0)
        {
            player.DisableRandomCards(disableCardCount);
        }
    }
}