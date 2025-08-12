using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LitMotion;
using Cysharp.Threading.Tasks;
using Void2610.UnityTemplate;

/// <summary>
/// ストーリーテキストを文字送りで表示するViewクラス
/// プロローグやエンディングなど、複数のシーンで使用可能
/// </summary>
public class StoryTextView : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI storyText;
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private GameObject nextIndicator; // 次へ進むインジケーター（▼など）
    
    [Header("文字送り設定")]
    [SerializeField] private float charSpeed = 0.05f; // 1文字表示する間隔（秒）
    [SerializeField] private float autoNextDelay = 3f; // 自動で次へ進むまでの待機時間（秒）
    [SerializeField] private bool enableAutoNext; // 自動で次へ進むかどうか
    
    [Header("フェード設定")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    private List<string> _textList;
    private int _currentIndex;
    private bool _isTyping;
    private bool _isWaitingForNext;
    private bool _skipTyping;
    
    private MotionHandle _typewriterMotion;
    private MotionHandle _fadeMotion;
    private MotionHandle _indicatorMotion;
    
    private void Awake()
    {
        // 初期状態を設定
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        if (nextIndicator)
        {
            nextIndicator.SetActive(false);
        }
    }
    
    /// <summary>
    /// ストーリーテキストの表示を開始する
    /// </summary>
    /// <param name="textList">表示するテキストのリスト</param>
    public async UniTask StartStory(List<string> textList)
    {
        _textList = new List<string>(textList);
        _currentIndex = 0;
        
        // フェードイン
        await FadeIn();
        
        // 最初のテキストを表示
        await ShowNextText();
    }
    
    /// <summary>
    /// 次のテキストを表示する
    /// </summary>
    private async UniTask ShowNextText()
    {
        if (_currentIndex >= _textList.Count)
        {
            // すべてのテキストを表示完了
            FadeOut().Forget();
            return;
        }
        
        SeManager.Instance.PlaySe("Dialog");
        // 現在のテキストを取得
        string currentText = _textList[_currentIndex];
        _currentIndex++;
        
        // テキストをクリア
        storyText.text = "";
        
        // インジケーターを非表示
        nextIndicator.SetActive(false);
        if (_indicatorMotion.IsActive())
            _indicatorMotion.Cancel();
        
        // 文字送りアニメーションを開始
        _isTyping = true;
        _isWaitingForNext = false;
        _skipTyping = false;
        
        await TypewriterEffect(currentText);
        
        // アニメーション完了後の状態を確実にリセット
        _isTyping = false;
        _skipTyping = false;
        _isWaitingForNext = true;
        
        // インジケーターを表示してアニメーション
        ShowNextIndicator();
        
        // 自動で次へ進む場合
        if (enableAutoNext)
        {
            await WaitForNextWithTimeout();
        }
        else
        {
            await WaitForNext();
        }
        
        // 次のテキストへ
        await ShowNextText();
    }
    
    /// <summary>
    /// 文字送りエフェクト
    /// </summary>
    private async UniTask TypewriterEffect(string text)
    {
        if (_typewriterMotion.IsActive())
            _typewriterMotion.Cancel();
        
        // スキップフラグがすでに true の場合は即座に完了
        if (_skipTyping)
        {
            storyText.text = text;
            return;
        }
        
        // 文字送りアニメーション
        _typewriterMotion = LMotion.Create(0f, text.Length, charSpeed * text.Length)
            .WithEase(Ease.Linear)
            .Bind(charCount =>
            {
                if (_skipTyping)
                {
                    storyText.text = text;
                    _typewriterMotion.Cancel();
                    return;
                }
                
                var displayCount = Mathf.FloorToInt(charCount);
                if (displayCount <= text.Length)
                {
                    storyText.text = text.Substring(0, displayCount);
                }
            })
            .AddTo(this);
        
        try
        {
            await _typewriterMotion.ToUniTask();
        }
        catch (OperationCanceledException) { }
        
        // 最終的に完全なテキストを表示（念のため）
        storyText.text = text;
    }
    
    /// <summary>
    /// 次へ進むのを待つ
    /// </summary>
    private async UniTask WaitForNext()
    {
        while (_isWaitingForNext)
        {
            await UniTask.Yield();
        }
    }
    
    /// <summary>
    /// タイムアウト付きで次へ進むのを待つ
    /// </summary>
    private async UniTask WaitForNextWithTimeout()
    {
        var elapsedTime = 0f;
        
        while (_isWaitingForNext && elapsedTime < autoNextDelay)
        {
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }
    }
    
    /// <summary>
    /// マウスクリック検知による進行処理
    /// </summary>
    private void Update()
    {
        // マウスクリックまたはタッチ入力を検知
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            OnClick();
        }
    }
    
    /// <summary>
    /// クリック時の処理
    /// </summary>
    private void OnClick()
    {
        // CanvasGroup が非アクティブな場合は無視
        if (canvasGroup.alpha == 0f || !canvasGroup.interactable)
            return;
            
        if (_isTyping && !_skipTyping)
        {
            // 文字送り中の場合はスキップ
            _skipTyping = true;
        }
        else if (_isWaitingForNext)
        {
            // 次へ進む
            _isWaitingForNext = false;
        }
    }
    
    /// <summary>
    /// 次へ進むインジケーターを表示
    /// </summary>
    private void ShowNextIndicator()
    {
        nextIndicator.SetActive(true);
        
        // 最後の文字の横にインジケーターを配置
        PositionIndicatorAtLastCharacter();
        
        // 上下にゆらゆら動くアニメーション
        if (_indicatorMotion.IsActive())
            _indicatorMotion.Cancel();
        
        var rectTransform = nextIndicator.GetComponent<RectTransform>();
        if (rectTransform)
        {
            var originalPos = rectTransform.anchoredPosition;
            _indicatorMotion = LMotion.Create(0f, 1f, 1f)
                .WithLoops(-1, LoopType.Yoyo)
                .WithEase(Ease.InOutSine)
                .Bind(t =>
                {
                    var pos = originalPos;
                    pos.y += Mathf.Sin(t * Mathf.PI) * 5f;
                    rectTransform.anchoredPosition = pos;
                })
                .AddTo(this);
        }
    }
    
    /// <summary>
    /// インジケーターを最後の文字の横に配置する
    /// </summary>
    private void PositionIndicatorAtLastCharacter()
    {
        var indicatorRectTransform = nextIndicator.GetComponent<RectTransform>();
        // TextMeshProUGUIのtextInfoを使用して最後の文字の位置を取得
        storyText.ForceMeshUpdate();
        
        var textInfo = storyText.textInfo;
        if (textInfo.characterCount == 0) return;
        
        // 最後の可視文字のインデックスを取得
        var lastVisibleCharIndex = textInfo.characterCount - 1;
        
        // 改行や空白文字を除いた実際の最後の文字を探す
        while (lastVisibleCharIndex >= 0)
        {
            var charInfo = textInfo.characterInfo[lastVisibleCharIndex];
            if (charInfo.isVisible && !char.IsWhiteSpace(charInfo.character))
            {
                break;
            }
            lastVisibleCharIndex--;
        }
        
        if (lastVisibleCharIndex >= 0)
        {
            var lastCharInfo = textInfo.characterInfo[lastVisibleCharIndex];
            
            // 最後の文字の右端の位置を計算
            var lastCharPosition = new Vector3(lastCharInfo.topRight.x, lastCharInfo.bottomRight.y, 0);
            
            // テキストのRectTransformからワールド座標に変換
            var textRectTransform = storyText.GetComponent<RectTransform>();
            var worldPos = textRectTransform.TransformPoint(lastCharPosition);
            
            // インジケーターの親のRectTransformでローカル座標に変換
            var localPos = indicatorRectTransform.parent.GetComponent<RectTransform>().InverseTransformPoint(worldPos);
            
            // インジケーターの位置を設定（少し右にオフセット）
            indicatorRectTransform.anchoredPosition = new Vector2(localPos.x + 20f, localPos.y + 5f);
        }
    }
    
    /// <summary>
    /// フェードイン
    /// </summary>
    private async UniTask FadeIn()
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        if (_fadeMotion.IsActive())
            _fadeMotion.Cancel();
        
        _fadeMotion = LMotion.Create(0f, 1f, fadeInDuration)
            .WithEase(Ease.OutCubic)
            .Bind(alpha => canvasGroup.alpha = alpha)
            .AddTo(this);
        
        await _fadeMotion.ToUniTask();
    }
    
    /// <summary>
    /// フェードアウト
    /// </summary>
    private async UniTask FadeOut()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        if (_fadeMotion.IsActive())
            _fadeMotion.Cancel();
        
        _fadeMotion = LMotion.Create(1f, 0f, fadeOutDuration)
            .WithEase(Ease.InCubic)
            .Bind(alpha => canvasGroup.alpha = alpha)
            .AddTo(this);
        
        await _fadeMotion.ToUniTask();
    }
    
    private void OnDestroy()
    {
        // モーションを停止
        if (_typewriterMotion.IsActive())
            _typewriterMotion.Cancel();
        if (_fadeMotion.IsActive())
            _fadeMotion.Cancel();
        if (_indicatorMotion.IsActive())
            _indicatorMotion.Cancel();
    }
}