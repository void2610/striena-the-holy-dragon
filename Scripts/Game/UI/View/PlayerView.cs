using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitMotion;
using LitMotion.Extensions;
using Cysharp.Threading.Tasks;
using Void2610.UnityTemplate;
using System.Threading;



/// <summary>
/// プレイヤーの立ち絵表示とセリフ表示を管理するViewクラス
/// </summary>
public class PlayerView : MonoBehaviour
{
    [Header("立ち絵設定")]
    [SerializeField] private Image playerImage;
    [SerializeField] private SerializableDictionary<PlayerStance, Sprite> stanceSprites = new();
    
    [Header("セリフ設定")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private PlayerDialogueData dialogueData;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private float textSpeed = 0.05f; // 1文字表示する間隔（秒）
    [SerializeField] private float dialogueDuration = 3f; // セリフを表示する時間（秒）
    
    private MotionHandle _dialogueTextMotion;
    private MotionHandle _dialogueFadeMotion;
    private CancellationTokenSource _dialogueCts;
    
    private void Awake()
    {
        // 初期状態として通常立ち絵を表示
        ChangeStance(PlayerStance.Normal);
        
        // セリフテキストを初期化
        dialogueText.text = "";
        dialogueCanvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// プレイヤーの立ち絵を変更する
    /// </summary>
    /// <param name="stance">変更する立ち絵の種類</param>
    public void ChangeStance(PlayerStance stance)
    {
        if (stanceSprites.TryGetValue(stance, out var sprite))
        {
            playerImage.sprite = sprite;
        }
        else
        {
            Debug.LogWarning($"立ち絵が設定されていません: {stance}");
        }
    }
    
    /// <summary>
    /// 指定されたタイプのセリフをランダムに表示する
    /// </summary>
    /// <param name="dialogueType">表示するセリフの種類</param>
    public void ShowDialogue(PlayerDialogueType dialogueType)
    {
        var dialogue = dialogueData.GetRandomDialogue(dialogueType);
        if (string.IsNullOrEmpty(dialogue)) return;
        ShowDialogueText(dialogue).Forget();
    }
    
    /// <summary>
    /// セリフテキストを1文字ずつ表示する
    /// </summary>
    /// <param name="dialogue">表示するセリフ</param>
    private async UniTaskVoid ShowDialogueText(string dialogue)
    {
        // 前のダイアログ処理をキャンセル
        _dialogueCts?.Cancel();
        _dialogueCts = new CancellationTokenSource();
        var ct = _dialogueCts.Token;
        
        if (_dialogueTextMotion.IsActive())
            _dialogueTextMotion.Cancel();
        if (_dialogueFadeMotion.IsActive())
            _dialogueFadeMotion.Cancel();

        _dialogueFadeMotion = LMotion.Create(0f, 1f, 0.25f)
            .Bind(alpha => dialogueCanvasGroup.alpha = alpha)
            .AddTo(this);
        
        // セリフテキストを表示
        dialogueText.text = "";
        
        // 1文字ずつ表示するアニメーション
        _dialogueTextMotion = LMotion.Create(0f, dialogue.Length, textSpeed * dialogue.Length)
            .WithEase(Ease.Linear)
            .Bind(charCount => 
            {
                var displayCount = Mathf.FloorToInt(charCount);
                if (displayCount <= dialogue.Length)
                {
                    dialogueText.text = dialogue.Substring(0, displayCount);
                }
            })
            .AddTo(this);
        
        // アニメーション完了まで待機
        await _dialogueTextMotion.ToUniTask();
        
        // キャンセル確認
        if (ct.IsCancellationRequested) return;
        
        // 指定時間待機
        await UniTask.Delay((int)(dialogueDuration * 1000), cancellationToken: ct);
        
        // キャンセル確認
        if (ct.IsCancellationRequested) return;
        
        // セリフを非表示
        HideDialogue();
    }
    
    /// <summary>
    /// セリフを非表示にする
    /// </summary>
    private void HideDialogue()
    {
        if (_dialogueTextMotion.IsActive())
            _dialogueTextMotion.Cancel();
        if (_dialogueFadeMotion.IsActive())
            _dialogueFadeMotion.Cancel();
        
        if (!this) return;

        _dialogueFadeMotion = LMotion.Create(1f, 0f, 0.25f)
            .Bind(alpha => dialogueCanvasGroup.alpha = alpha)
            .AddTo(this);
    }
    
    private void OnDestroy()
    {
        // モーションを停止
        if (_dialogueTextMotion.IsActive())
            _dialogueTextMotion.Cancel();
        if (_dialogueFadeMotion.IsActive())
            _dialogueFadeMotion.Cancel();
            
        // キャンセレーショントークンをクリーンアップ
        _dialogueCts?.Cancel();
        _dialogueCts?.Dispose();
    }
}