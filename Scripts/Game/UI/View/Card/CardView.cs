using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using LitMotion;
using LitMotion.Extensions;
using Void2610.UnityTemplate;

/// <summary>
/// 個別のカードを表示するView
/// </summary>
public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button cardButton;
    
    public CardData cardData;
    
    // カード選択時のイベント
    public event Action<CardData> OnCardSelected;
    
    // ホバーアニメーション用
    private RectTransform _rectTransform;
    private Vector2 _originalPosition;
    private Vector3 _originalScale;
    private MotionHandle _positionMotionHandle;
    private MotionHandle _scaleMotionHandle;
    private bool _isHovering;
    
    // HandViewの参照（アニメーション状態確認用）
    private HandView _handView;
    
    private void Awake()
    {
        // ボタンクリック時のイベント設定
        if (cardButton)
        {
            cardButton.onClick.AddListener(OnCardClicked);
        }
        
        // RectTransformの参照を取得
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform)
        {
            _originalPosition = _rectTransform.anchoredPosition;
            _originalScale = _rectTransform.localScale;
        }
        
        // HandViewの参照を取得（親をたどって取得）
        _handView = GetComponentInParent<HandView>();
    }
    
    /// <summary>
    /// カードデータを設定して表示を更新
    /// </summary>
    public void SetCardData(CardData data)
    {
        this.cardData = data;
        UpdateDisplay();
    }
    
    /// <summary>
    /// 表示を更新
    /// </summary>
    private void UpdateDisplay()
    {
        if (!cardData) return;
        
        // カード名
        if (cardNameText)
        {
            cardNameText.text = cardData.CardName;
        }
        
        // カード画像
        if (cardData.CardImage && cardImage)
        {
            cardImage.sprite = cardData.CardImage;
        }
        else
        {
            cardImage.color = Color.clear; // 画像がない場合は透明にする
        }
        
        // 説明文
        if (descriptionText)
        {
            descriptionText.text = cardData.Description;
        }
    }
    
    /// <summary>
    /// カード使用可能状態を更新
    /// </summary>
    public void UpdateUsability(bool canUse)
    {
        if (cardButton)
        {
            cardButton.interactable = canUse;
        }
        
        // 使用不可の場合は視覚的に暗くする
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = canUse ? 1.0f : 0.5f;
        }
    }
    
    /// <summary>
    /// カードボタンクリック時の処理
    /// </summary>
    private void OnCardClicked()
    {
        // アニメーション中はクリックイベントを無視
        if (_handView && _handView.IsAnimating) return;
        
        OnCardSelected?.Invoke(cardData);
    }
    
    /// <summary>
    /// カードを非表示にする
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// カードを表示する
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 元の位置を更新（HandViewから呼ばれる）
    /// </summary>
    public void UpdateOriginalPosition(Vector2 position)
    {
        _originalPosition = position;
        // ホバー中は位置を更新しない（アニメーション中の競合を防ぐ）
        if (!_isHovering && _rectTransform)
        {
            // 既存のアニメーションをキャンセル
            CancelAnimations();
            _rectTransform.anchoredPosition = position;
        }
    }
    
    /// <summary>
    /// アニメーションをキャンセル
    /// </summary>
    private void CancelAnimations()
    {
        if (_positionMotionHandle.IsActive())
        {
            _positionMotionHandle.Cancel();
        }
        if (_scaleMotionHandle.IsActive())
        {
            _scaleMotionHandle.Cancel();
        }
    }
    
    /// <summary>
    /// マウスがカードに乗ったとき
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // アニメーション中やボタンが無効な場合は何もしない
        if (!cardButton.interactable || !_rectTransform || 
            (_handView && _handView.IsAnimating)) return;
        
        _isHovering = true;
        
        // 既存のアニメーションをキャンセル
        CancelAnimations();
        
        // ホバー時のSE再生
        SeManager.Instance.PlaySe("CardHover");
        
        // ホバー時のアニメーション
        var targetPosition = _originalPosition + Vector2.up * 20f;
        var targetScale = _originalScale * 1.1f;
        
        // 位置のアニメーション
        _positionMotionHandle = LMotion.Create(_rectTransform.anchoredPosition, targetPosition, 0.2f)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(_rectTransform)
            .AddTo(gameObject);
            
        // スケールのアニメーション
        _scaleMotionHandle = LMotion.Create(_rectTransform.localScale, targetScale, 0.2f)
            .WithEase(Ease.OutCubic)
            .BindToLocalScale(_rectTransform)
            .AddTo(gameObject);
    }
    
    /// <summary>
    /// マウスがカードから離れたとき
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // アニメーション中は何もしない
        if (!_rectTransform || (_handView && _handView.IsAnimating)) return;
        
        _isHovering = false;
        
        // 既存のアニメーションをキャンセル
        CancelAnimations();
        
        // 元に戻るアニメーション
        _positionMotionHandle = LMotion.Create(_rectTransform.anchoredPosition, _originalPosition, 0.2f)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(_rectTransform)
            .AddTo(gameObject);
            
        _scaleMotionHandle = LMotion.Create(_rectTransform.localScale, _originalScale, 0.2f)
            .WithEase(Ease.OutCubic)
            .BindToLocalScale(_rectTransform)
            .AddTo(gameObject);
    }
    
    /// <summary>
    /// オブジェクト破棄時の処理
    /// </summary>
    private void OnDestroy()
    {
        // アニメーションをクリーンアップ
        CancelAnimations();
    }
    
    /// <summary>
    /// カードの操作可能状態を設定（HandViewから呼び出される）
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (cardButton) cardButton.interactable = interactable;
        
        // 操作不可の場合は視覚的に暗くする
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = interactable ? 1.0f : 0.5f;
        }
    }
}