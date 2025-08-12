using System;
using System.Collections.Generic;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
using Cysharp.Threading.Tasks;
using Void2610.UnityTemplate;

/// <summary>
/// 手札全体を管理するView
/// </summary>
public class HandView : MonoBehaviour
{
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject dangerousCardPrefab;
    
    [Header("アニメーション設定")]
    [SerializeField] private Vector2 drawStartOffset = new Vector2(0, 500f);
    [SerializeField] private float drawStartRotation = -30f;
    
    public bool IsAnimating { get; private set; }
    
    private readonly List<CardView> _cardViews = new();
    private readonly Dictionary<CardView, Action<CardData>> _cardEventHandlers = new();
    
    // カード選択時のイベント（カードデータとインデックスを渡す）
    public event Action<CardData, int> OnCardSelected;
    
    /// <summary>
    /// 手札を更新（差分更新）
    /// </summary>
    public void UpdateHand(List<CardData> cards)
    {
        UpdateCardViews(cards).Forget();
    }
    
    /// <summary>
    /// カード表示を更新（改善された差分更新）
    /// </summary>
    private async UniTask UpdateCardViews(List<CardData> cards)
    {
        // アニメーション開始：カード操作無効化
        IsAnimating = true;
        SetAllCardsInteractable(false);
        
        // 削除処理：削除されたカードを特定して削除
        var cardsToRemove = new List<int>();
        
        // 削除されたカードのインデックスを特定
        for (int i = 0; i < _cardViews.Count; i++)
        {
            if (_cardViews[i] && _cardViews[i].cardData)
            {
                // カード名で比較して削除対象を特定
                var cardView = _cardViews[i];
                bool foundInNewCards = false;
                
                foreach (var newCard in cards)
                {
                    if (newCard && cardView.cardData && 
                        newCard.CardName == cardView.cardData.CardName)
                    {
                        foundInNewCards = true;
                        break;
                    }
                }
                
                if (!foundInNewCards)
                {
                    cardsToRemove.Add(i);
                }
            }
        }
        
        // 削除対象のカードを後ろから削除（インデックスのずれを防ぐため）
        var removeAnimationTasks = new List<UniTask>();
        
        for (int i = cardsToRemove.Count - 1; i >= 0; i--)
        {
            var removeIndex = cardsToRemove[i];
            var cardView = _cardViews[removeIndex];
            _cardViews.RemoveAt(removeIndex);
            
            if (cardView)
            {
                removeAnimationTasks.Add(RemoveCardViewAsync(cardView));
            }
        }
        
        // すべての削除アニメーションを並列実行して完了を待つ
        if (removeAnimationTasks.Count > 0)
        {
            await UniTask.WhenAll(removeAnimationTasks);
        }
        
        // 既存のCardViewのデータを新しいリストに合わせて更新
        // カードタイプが変わった場合は再作成が必要
        var recreationTasks = new List<UniTask>();
        for (var i = 0; i < _cardViews.Count && i < cards.Count; i++)
        {
            if (_cardViews[i])
            {
                var oldCard = _cardViews[i].cardData;
                var newCard = cards[i];
                
                // カードタイプ（危険カード/通常カード）が変わった場合は再作成
                if (oldCard && oldCard.IsDangerousCard != newCard.IsDangerousCard)
                {
                    // 古いカードビューを削除
                    var oldCardView = _cardViews[i];
                    recreationTasks.Add(RemoveCardViewAsync(oldCardView));
                    
                    // 新しいカードビューを作成
                    var newCardView = CreateCardView(newCard);
                    _cardViews[i] = newCardView;
                }
                else
                {
                    // 同じタイプの場合は通常の更新
                    _cardViews[i].SetCardData(cards[i]);
                }
            }
        }
        
        // カードタイプ変更による削除アニメーションを待つ
        if (recreationTasks.Count > 0)
        {
            await UniTask.WhenAll(recreationTasks);
        }
        
        // 追加処理：新しいカードを作成（アニメーションなし）
        var newCardStartIndex = _cardViews.Count;
        for (var i = _cardViews.Count; i < cards.Count; i++)
        {
            var cardView = CreateCardView(cards[i]);
            _cardViews.Add(cardView);
        }
        
        // 全カードの最終位置を計算
        CalculateCardPositions();
        
        // 新しいカードのドローアニメーション + 既存カードのアレンジアニメーション
        if (newCardStartIndex < cards.Count)
        {
            await PlayDrawAndArrangeAnimation(newCardStartIndex);
        }
        else if (_cardViews.Count > 0)
        {
            // 既存カードのみの場合はアレンジのみ
            await ArrangeExistingCardsAsync();
        }
        
        // アニメーション終了：カード操作有効化
        IsAnimating = false;
        SetAllCardsInteractable(true);
    }
    
    /// <summary>
    /// CardViewを作成（アニメーションなし）
    /// </summary>
    private CardView CreateCardView(CardData cardData)
    {
        var cardObj = Instantiate(!cardData.IsDangerousCard ? cardPrefab : dangerousCardPrefab, cardContainer);
        var cardView = cardObj.GetComponent<CardView>();
        
        if (cardView)
        {
            cardView.SetCardData(cardData);
            // カード選択時にインデックスを取得できるようにCardViewにコールバックを設定
            Action<CardData> handler = (selectedCard) => HandleCardSelected(selectedCard, cardView);
            _cardEventHandlers[cardView] = handler;
            cardView.OnCardSelected += handler;
        }
        
        return cardView;
    }
    
    /// <summary>
    /// 全カードの最終位置を計算
    /// </summary>
    private void CalculateCardPositions()
    {
        var cardCount = _cardViews.Count;
        if (cardCount == 0) return;
        
        float cardSpacing = 120f; // カード間隔
        var startX = -(cardCount - 1) * cardSpacing * 0.5f;
        
        for (int i = 0; i < cardCount; i++)
        {
            var cardView = _cardViews[i];
            if (!cardView) continue;
            
            var targetPosition = new Vector2(startX + i * cardSpacing, 0);
            UpdateCardOriginalPosition(cardView, targetPosition);
        }
    }
    
    /// <summary>
    /// カードの基準位置を更新
    /// </summary>
    private void UpdateCardOriginalPosition(CardView cardView, Vector2 position)
    {
        var rectTransform = cardView.GetComponent<RectTransform>();
        if (rectTransform)
        {
            rectTransform.anchoredPosition = position;
        }
        // CardViewにも元の位置を通知
        cardView.UpdateOriginalPosition(position);
    }
    
    /// <summary>
    /// 新しいカードのドローと既存カードのアレンジを同時実行
    /// </summary>
    private async UniTask PlayDrawAndArrangeAnimation(int newCardStartIndex)
    {
        var animationTasks = new List<UniTask>();
        
        for (var i = newCardStartIndex; i < _cardViews.Count; i++)
        {
            var cardView = _cardViews[i];
            if (cardView)
            {
                var cardIndex = i - newCardStartIndex;
                var delay = cardIndex * 150;
                animationTasks.Add(DelayedDrawAnimation(cardView, delay));
            }
        }
        
        // 既存カードのアレンジアニメーション（少し遅れて開始）
        if (newCardStartIndex > 0)
        {
            animationTasks.Add(DelayedArrangeExistingCards(200));
        }
        
        await UniTask.WhenAll(animationTasks);
    }
    
    /// <summary>
    /// 遅延付きドローアニメーション
    /// </summary>
    private async UniTask DelayedDrawAnimation(CardView cardView, int delayMs)
    {
        if (delayMs > 0) await UniTask.Delay(delayMs);
        await PlayCardDrawAnimation(cardView);
    }
    
    /// <summary>
    /// 遅延付き既存カードアレンジ
    /// </summary>
    private async UniTask DelayedArrangeExistingCards(int delayMs)
    {
        if (delayMs > 0) await UniTask.Delay(delayMs);
        await ArrangeExistingCardsAsync();
    }
    
    /// <summary>
    /// 既存カードのアレンジアニメーション
    /// </summary>
    private async UniTask ArrangeExistingCardsAsync()
    {
        var cardCount = _cardViews.Count;
        float cardSpacing = 120f;
        var startX = -(cardCount - 1) * cardSpacing * 0.5f;
        var animationTasks = new List<UniTask>();
        
        for (var i = 0; i < cardCount; i++)
        {
            var cardView = _cardViews[i];
            if (!cardView) continue;
            
            var targetPosition = new Vector2(startX + i * cardSpacing, 0);
            animationTasks.Add(PlayCardArrangeAnimation(cardView, targetPosition));
        }
        
        await UniTask.WhenAll(animationTasks);
    }
    
    /// <summary>
    /// カードのドローアニメーションを再生
    /// </summary>
    private async UniTask PlayCardDrawAnimation(CardView cardView)
    {
        SeManager.Instance.PlaySe("CardDraw");
        
        var rectTransform = cardView.GetComponent<RectTransform>();
        if (!rectTransform) return;
        
        // 最終位置とスケールを記録
        var finalPosition = rectTransform.anchoredPosition;
        var finalScale = rectTransform.localScale;
        var finalRotation = rectTransform.localEulerAngles;
        
        // 開始位置を設定（SerializeFieldで設定されたオフセットを使用）
        var startPosition = finalPosition + drawStartOffset;
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.one * 0.1f;
        rectTransform.localEulerAngles = new Vector3(0, 0, drawStartRotation);
        
        // 移動アニメーション（0.5秒）
        var moveTask = LMotion.Create(startPosition, finalPosition, 0.5f)
            .WithEase(Ease.OutBack)
            .BindToAnchoredPosition(rectTransform)
            .AddTo(cardView.gameObject)
            .ToUniTask();
        
        // スケールアニメーション（3段階）
        // 1. 小さい状態から大きくなる（0.35秒）
        var scaleTask1 = LMotion.Create(Vector3.one * 0.1f, finalScale * 1.3f, 0.35f)
            .WithEase(Ease.OutCubic)
            .BindToLocalScale(rectTransform)
            .AddTo(cardView.gameObject)
            .ToUniTask();
        
        // 回転アニメーション（0.5秒）
        var rotationTask = LMotion.Create(drawStartRotation, 0f, 0.5f)
            .WithEase(Ease.OutBack)
            .BindToLocalEulerAnglesZ(rectTransform)
            .AddTo(cardView.gameObject)
            .ToUniTask();
        
        // 移動、スケール、回転アニメーションを同時実行
        await UniTask.WhenAll(moveTask, scaleTask1, rotationTask);
        
        // 2. 大きい状態から元のサイズに戻る（0.15秒）
        await LMotion.Create(rectTransform.localScale, finalScale, 0.15f)
            .WithEase(Ease.InOutCubic)
            .BindToLocalScale(rectTransform)
            .AddTo(cardView.gameObject)
            .ToUniTask();
        
        // アニメーション完了後、新しい位置を基準位置として更新
        cardView.UpdateOriginalPosition(finalPosition);
    }
    
    /// <summary>
    /// カードのアレンジアニメーションを再生
    /// </summary>
    private async UniTask PlayCardArrangeAnimation(CardView cardView, Vector2 targetPosition)
    {
        var rectTransform = cardView.GetComponent<RectTransform>();
        if (!rectTransform) return;
        
        // 移動アニメーション
        await LMotion.Create(rectTransform.anchoredPosition, targetPosition, 0.3f)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(rectTransform)
            .AddTo(cardView.gameObject)
            .ToUniTask();
            
        // アニメーション完了後、新しい位置を基準位置として更新
        cardView.UpdateOriginalPosition(targetPosition);
    }
    
    /// <summary>
    /// CardViewを削除
    /// </summary>
    private async UniTask RemoveCardViewAsync(CardView cardView)
    {
        if (!cardView) return;
        
        // キャッシュされたハンドラーを使用してイベント解除
        if (_cardEventHandlers.TryGetValue(cardView, out var handler))
        {
            cardView.OnCardSelected -= handler;
            _cardEventHandlers.Remove(cardView);
        }
        
        // 削除アニメーション（上に移動しながらフェード）
        var rectTransform = cardView.GetComponent<RectTransform>();
        if (rectTransform)
        {
            var currentPos = rectTransform.anchoredPosition;
            var targetPos = currentPos + Vector2.up * 100f;
            
            var moveTask = LMotion.Create(currentPos, targetPos, 0.3f)
                .WithEase(Ease.InCubic)
                .BindToAnchoredPosition(rectTransform)
                .AddTo(cardView.gameObject)
                .ToUniTask();
                
            var scaleTask = LMotion.Create(rectTransform.localScale, Vector3.zero, 0.3f)
                .WithEase(Ease.InCubic)
                .BindToLocalScale(rectTransform)
                .AddTo(cardView.gameObject)
                .ToUniTask();
                
            await UniTask.WhenAll(moveTask, scaleTask);
        }
        else
        {
            await UniTask.Delay(300);
        }
        
        Destroy(cardView.gameObject);
    }
    
    /// <summary>
    /// カードの使用可能状態を更新
    /// </summary>
    public void UpdateCardUsability(PlayerModel playerModel)
    {
        foreach (var cardView in _cardViews)
        {
            if (cardView && cardView.cardData)
            {
                // カードが無効化されているかチェック
                var isDisabled = playerModel.IsCardDisabled(cardView.cardData);
                cardView.UpdateUsability(!isDisabled);
            }
        }
    }
    
    /// <summary>
    /// カード選択時の処理
    /// </summary>
    private void HandleCardSelected(CardData cardData, CardView selectedCardView)
    {
        // 選択されたCardViewのインデックスを特定
        int cardIndex = _cardViews.IndexOf(selectedCardView);
        OnCardSelected?.Invoke(cardData, cardIndex);
    }
    
    /// <summary>
    /// 手札を表示/非表示
    /// </summary>
    public void SetHandVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// 特定のカードを削除（崩壊アニメーション付き）
    /// </summary>
    public async UniTask RemoveSpecificCard(CardData cardData)
    {
        if (!cardData) return;
        
        // 削除対象のCardViewを見つける（カード名で比較）
        for (int i = 0; i < _cardViews.Count; i++)
        {
            var cardView = _cardViews[i];
            if (cardView && cardView.cardData)
            {
                // まず参照比較を試み、ダメならカード名で比較
                bool isMatch = (cardView.cardData == cardData) || 
                              (cardView.cardData.CardName == cardData.CardName);
                              
                if (isMatch)
                {
                    // アニメーション開始：カード操作無効化
                    IsAnimating = true;
                    SetAllCardsInteractable(false);
                    
                    // リストから削除
                    _cardViews.RemoveAt(i);
                    
                    // 削除アニメーションを実行
                    await RemoveCardViewAsync(cardView);
                    
                    // 残りのカードを再配置
                    CalculateCardPositions();
                    await ArrangeExistingCardsAsync();
                    
                    // アニメーション終了：カード操作有効化
                    IsAnimating = false;
                    SetAllCardsInteractable(true);
                    
                    return;
                }
            }
        }
    }
    
    /// <summary>
    /// 特定のインデックスのカードを削除（崩壊アニメーション付き）
    /// </summary>
    public async UniTask RemoveSpecificCardByIndex(int cardIndex)
    {
        // インデックスの範囲チェック
        if (cardIndex < 0 || cardIndex >= _cardViews.Count) return;
        
        var cardView = _cardViews[cardIndex];
        if (!cardView) return;
        
        // アニメーション開始：カード操作無効化
        IsAnimating = true;
        SetAllCardsInteractable(false);
        
        // リストから削除
        _cardViews.RemoveAt(cardIndex);
        
        // 削除アニメーションを実行
        await RemoveCardViewAsync(cardView);
        
        // 残りのカードを再配置
        CalculateCardPositions();
        await ArrangeExistingCardsAsync();
        
        // アニメーション終了：カード操作有効化
        IsAnimating = false;
        SetAllCardsInteractable(true);
    }
    
    /// <summary>
    /// すべてのカードの操作可能状態を設定
    /// </summary>
    private void SetAllCardsInteractable(bool interactable)
    {
        foreach (var cardView in _cardViews)
        {
            if (cardView) cardView.SetInteractable(interactable);
        }
    }
    
}