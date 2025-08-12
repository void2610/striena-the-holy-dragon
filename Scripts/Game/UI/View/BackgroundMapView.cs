using LitMotion;
using LitMotion.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// マップ背景画像の移動とズームを制御するView
/// エリアごとに設定された座標を中心にズームイン/アウトする
/// 3種類の背景画像を設定・切り替え可能
/// </summary>
public class BackgroundMapView : MonoBehaviour
{
    [SerializeField] private List<Sprite> backgroundSprites;
    
    [Header("Background Images")]
    [SerializeField] private Image backgroundImageA; // メイン背景画像
    [SerializeField] private Image backgroundImageB; // クロスフェード用背景画像
    
    [Header("Map Settings")]
    [SerializeField] private SerializableDictionary<BattleAreaType, Vector2> areaPositions = new();
    [SerializeField] private float zoomScale = 1.5f; // ズーム時の拡大率
    [SerializeField] private float animationDuration = 1.0f; // アニメーション時間
    [SerializeField] private Ease moveEase = Ease.OutCubic; // 移動のイージング
    [SerializeField] private Ease scaleEase = Ease.OutCubic; // スケールのイージング
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true; // エディタでエリア位置を表示
    
    private RectTransform _rectTransform;
    private Vector2 _originalPosition;
    private Vector3 _originalScale;
    private MotionHandle _positionMotion;
    private MotionHandle _scaleMotion;
    private MotionHandle _backgroundFadeMotion;
    
    // 現在の背景インデックス
    private int _currentBackgroundIndex;
    // どちらがアクティブな背景かを追跡
    private bool _isBackgroundAActive = true;
    
    /// <summary>
    /// 指定されたエリアにマップを移動・ズームする
    /// </summary>
    public void MoveToArea(BattleAreaType areaType)
    {
        if (!areaPositions.TryGetValue(areaType, out var targetPosition))
        {
            Debug.LogWarning($"エリア {areaType} の座標が設定されていません");
            return;
        }
        
        // 進行中のアニメーションをキャンセル
        if (_positionMotion.IsActive())
        {
            _positionMotion.Cancel();
        }
        if (_scaleMotion.IsActive())
        {
            _scaleMotion.Cancel();
        }
        
        // 目標位置を計算（中心点を画面の中央に持ってくるように調整）
        var adjustedPosition = -targetPosition * zoomScale;
        
        // 位置とスケールのアニメーション
        _positionMotion = LMotion.Create(_rectTransform.anchoredPosition, adjustedPosition, animationDuration)
            .WithEase(moveEase)
            .BindToAnchoredPosition(_rectTransform);
            
        _scaleMotion = LMotion.Create(_rectTransform.localScale, Vector3.one * zoomScale, animationDuration)
            .WithEase(scaleEase)
            .BindToLocalScale(_rectTransform);
    }
    
    /// <summary>
    /// マップを元の位置・スケールに戻す
    /// </summary>
    public void ResetPosition()
    {
        // 進行中のアニメーションをキャンセル
        if (_positionMotion.IsActive())
        {
            _positionMotion.Cancel();
        }
        if (_scaleMotion.IsActive())
        {
            _scaleMotion.Cancel();
        }
        
        _positionMotion = LMotion.Create(_rectTransform.anchoredPosition, _originalPosition, animationDuration)
            .WithEase(moveEase)
            .BindToAnchoredPosition(_rectTransform);
            
        _scaleMotion = LMotion.Create(_rectTransform.localScale, _originalScale, animationDuration)
            .WithEase(scaleEase)
            .BindToLocalScale(_rectTransform);
    }
    
    /// <summary>
    /// 背景画像を切り替える（クロスフェード付き）
    /// </summary>
    public void SetBackground(int backgroundIndex)
    {
        // 範囲チェック
        if (backgroundIndex < 0 || backgroundIndex >= backgroundSprites.Count) return;
        
        // 同じ背景の場合は何もしない
        if (backgroundIndex == _currentBackgroundIndex) return;
        
        // 進行中のフェードアニメーションをキャンセル
        if (_backgroundFadeMotion.IsActive())
        {
            _backgroundFadeMotion.Cancel();
        }
        
        // 目標のスプライトを取得
        var targetSprite = backgroundSprites[backgroundIndex];
        if (targetSprite == null)
        {
            Debug.LogWarning($"BackgroundMapView: インデックス {backgroundIndex} の背景画像がnullです。");
            return;
        }
        
        // クロスフェードアニメーションを実行
        StartCrossFade(targetSprite, backgroundIndex);
    }
    
    /// <summary>
    /// クロスフェードアニメーションを実行する
    /// </summary>
    /// <param name="targetSprite">変更先のスプライト</param>
    /// <param name="newIndex">新しい背景インデックス</param>
    private void StartCrossFade(Sprite targetSprite, int newIndex)
    {
        // 現在アクティブでない背景に新しいスプライトを設定
        var targetImage = _isBackgroundAActive ? backgroundImageB : backgroundImageA;
        
        // 新しいスプライトを設定
        targetImage.sprite = targetSprite;
        targetImage.color = new Color(1f, 1f, 1f, 0f);
        
        // クロスフェードアニメーション
        _backgroundFadeMotion = LMotion.Create(0f, 1f, 0.5f)
            .WithOnComplete(() =>
            {
                // アニメーション完了後、役割を交代
                _isBackgroundAActive = !_isBackgroundAActive;
                _currentBackgroundIndex = newIndex;
            })
            .Bind(alpha => targetImage.color = new Color(1f, 1f, 1f, alpha))
            .AddTo(this);
    }
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalPosition = _rectTransform.anchoredPosition;
        _originalScale = _rectTransform.localScale;
        
        // 背景画像の初期設定
        InitializeBackgroundImages();
        
        // デフォルトのエリア座標を設定（必要に応じて調整）
        if (areaPositions.Count == 0)
        {
            areaPositions.Add(BattleAreaType.Market, new Vector2(0, 100));
            areaPositions.Add(BattleAreaType.Residential, new Vector2(-200, -50));
            areaPositions.Add(BattleAreaType.BackAlley, new Vector2(200, -100));
            areaPositions.Add(BattleAreaType.Cathedral, new Vector2(0, -200));
        }
    }
    
    /// <summary>
    /// 背景画像の初期設定を行う
    /// </summary>
    private void InitializeBackgroundImages()
    {
        // 初期状態ではAをアクティブ、Bを透明に
        backgroundImageA.color = Color.white;
        backgroundImageB.color = new Color(1f, 1f, 1f, 0f);
        _isBackgroundAActive = true;
    }
    
    private void OnDestroy()
    {
        // すべてのアニメーションを停止
        if (_positionMotion.IsActive())
        {
            _positionMotion.Cancel();
        }
        if (_scaleMotion.IsActive())
        {
            _scaleMotion.Cancel();
        }
        if (_backgroundFadeMotion.IsActive())
        {
            _backgroundFadeMotion.Cancel();
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos || areaPositions == null) return;
        
        Gizmos.color = Color.yellow;
        
        foreach (var kvp in areaPositions)
        {
            var worldPos = transform.TransformPoint(kvp.Value);
            Gizmos.DrawWireSphere(worldPos, 0.7f);
            UnityEditor.Handles.Label(worldPos, kvp.Key.ToJapanese());
        }
    }
#endif
}