using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 残り人口を表示するView
/// </summary>
public class PopulationView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI availableCitizensText;
    [SerializeField] private Image availableCitizensIcon;
    [SerializeField] private GameObject evacuatedCitizensTextPrefab;
    [SerializeField] private GameObject killedCitizensTextPrefab;
    [SerializeField] private float prefabOffsetX = 0f;
    
    private float _previousSurvivalRate = -1f;
    private int _previousKilledCitizens = -1;
    private float _previousEvacuatedCitizens = -1f;
    private MotionHandle _shakeHandle;
    private MotionHandle _colorHandle;
    private Vector3 _originalPosition;
    private CancellationTokenSource _delayedCheckCts;
    
    /// <summary>
    /// 人口表示を更新
    /// </summary>
    public void UpdatePopulationDisplay(int availableCitizens, int killedCitizens, float evacuatedCitizens, float survivalRate)
    {
        availableCitizensText.text = $": {availableCitizens} ({survivalRate * 100f:0.0}%)";
        
        // 戦死者数の変化をチェック
        if (_previousKilledCitizens >= 0 && killedCitizens > _previousKilledCitizens)
        {
            int killedDifference = killedCitizens - _previousKilledCitizens;
            ShowKilledAnimation(killedDifference);
        }
        
        // 撤退者数の変化をチェック
        if (_previousEvacuatedCitizens >= 0 && evacuatedCitizens > _previousEvacuatedCitizens)
        {
            float evacuatedDifference = evacuatedCitizens - _previousEvacuatedCitizens;
            ShowEvacuatedAnimation(evacuatedDifference);
        }
        
        // 前回の値を更新
        _previousKilledCitizens = killedCitizens;
        _previousEvacuatedCitizens = evacuatedCitizens;
        
        // 既存の遅延チェックをキャンセル
        _delayedCheckCts?.Cancel();
        _delayedCheckCts = new CancellationTokenSource();
        
        // 少し遅延してから生存率の変化をチェック（R3の連続通知が落ち着くのを待つ）
        CheckSurvivalRateChange(survivalRate, _delayedCheckCts.Token).Forget();
    }
    
    /// <summary>
    /// 敵のスタン状態に応じて市民数テキストの色を更新
    /// </summary>
    public void UpdateStunStateColor(bool isEnemyStunning)
    {
        // 敵がスタン中なら市民数テキストの色を青に、そうでなければ白に
        var blue = new Color(0.3f, 0.6f, 1f);
        availableCitizensText.color = isEnemyStunning ? blue : Color.white;
        availableCitizensIcon.color = isEnemyStunning ? blue : Color.white;
    }
    
    /// <summary>
    /// 遅延後に生存率の変化をチェック
    /// </summary>
    private async UniTaskVoid CheckSurvivalRateChange(float newSurvivalRate, CancellationToken cancellationToken)
    {
        try
        {
            // 50ms待機してR3の連続通知が落ち着くのを待つ
            await UniTask.Delay(50, cancellationToken: cancellationToken);
            
            // 生存率が実際に低下した場合のみアニメーション
            if (_previousSurvivalRate > -1f && _previousSurvivalRate > newSurvivalRate)
            {
                PlayDamageAnimation().Forget();
            }
            
            _previousSurvivalRate = newSurvivalRate;
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
    }
    
    /// <summary>
    /// ダメージ時のアニメーションを再生
    /// </summary>
    private async UniTask PlayDamageAnimation()
    {
        if (_shakeHandle.IsActive()) _shakeHandle.Cancel();
        if (_colorHandle.IsActive()) _colorHandle.Cancel();
        
        // 色変化アニメーション（白→赤→元の色）
        PlayColorAnimation(availableCitizensText.color).Forget();
        
        // 振動アニメーション（アニメーション完了を監視）
        _shakeHandle = LMotion.Shake.Create(Vector3.zero, Vector3.one * 6f, 0.5f)
            .WithFrequency(2)
            .Bind(v => availableCitizensText.transform.localPosition = _originalPosition + v);
        
        await _shakeHandle.ToUniTask();
        
        availableCitizensText.transform.localPosition = _originalPosition;
    }
    
    /// <summary>
    /// 色変化アニメーションを非同期で実行
    /// </summary>
    private async UniTask PlayColorAnimation(Color originalColor)
    {
        // 白→赤へのアニメーション
        _colorHandle = LMotion.Create(Color.white, Color.red, 0.15f)
            .WithEase(Ease.OutQuad)
            .BindToColor(availableCitizensText);
        
        await _colorHandle.ToUniTask();
        
        // 赤→元の色へのアニメーション
        _colorHandle = LMotion.Create(Color.red, originalColor, 0.35f)
            .WithEase(Ease.InQuad)
            .BindToColor(availableCitizensText);
    }
    
    /// <summary>
    /// 戦死者増加アニメーション（下に移動しながら消える）
    /// </summary>
    private void ShowKilledAnimation(int killedCount)
    {
        // プレハブを生成
        var animationObject = Instantiate(killedCitizensTextPrefab, transform);
        var textComponent = animationObject.GetComponent<TextMeshProUGUI>();
        var canvasGroup = animationObject.GetComponent<CanvasGroup>();
        
        textComponent.text = $"-{killedCount}";
        // 初期位置設定（availableCitizensTextの近く）
        var rectTransform = animationObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = availableCitizensText.rectTransform.anchoredPosition + new Vector2(prefabOffsetX, 0f);
        
        // アニメーション実行
        PlayKilledAnimation(animationObject, canvasGroup).Forget();
    }
    
    /// <summary>
    /// 撤退者増加アニメーション（上に移動しながら消える）
    /// </summary>
    private void ShowEvacuatedAnimation(float evacuatedCount)
    {
        // プレハブを生成
        var animationObject = Instantiate(evacuatedCitizensTextPrefab, transform);
        var textComponent = animationObject.GetComponent<TextMeshProUGUI>();
        var canvasGroup = animationObject.GetComponent<CanvasGroup>();
        
        textComponent.text = $"+{evacuatedCount:0}";
        
        var rectTransform = animationObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = availableCitizensText.rectTransform.anchoredPosition + new Vector2(prefabOffsetX, 0f);
        
        // アニメーション実行
        PlayEvacuatedAnimation(animationObject, canvasGroup).Forget();
    }
    
    /// <summary>
    /// 戦死者アニメーション実行
    /// </summary>
    private async UniTaskVoid PlayKilledAnimation(GameObject animationObject, CanvasGroup canvasGroup)
    {
        var rectTransform = animationObject.GetComponent<RectTransform>();
        var startPosition = rectTransform.anchoredPosition;
        var endPosition = startPosition + Vector2.down * 50f; // 下に50ピクセル移動
        
        // 位置とアルファのアニメーションを並行実行
        var moveAnimation = LMotion.Create(startPosition, endPosition, 1.5f)
            .WithEase(Ease.OutQuad)
            .BindToAnchoredPosition(rectTransform);
            
        var fadeAnimation = LMotion.Create(1f, 0f, 1.5f)
            .WithEase(Ease.OutQuad)
            .WithDelay(0.3f) // 少し遅れてフェードアウト開始
            .Bind(alpha => canvasGroup.alpha = alpha);
        
        // 両方のアニメーション完了を待つ
        await UniTask.WhenAll(moveAnimation.ToUniTask(), fadeAnimation.ToUniTask());
        
        // オブジェクト削除
        if (animationObject) Destroy(animationObject);
    }
    
    /// <summary>
    /// 撤退者アニメーション実行
    /// </summary>
    private async UniTaskVoid PlayEvacuatedAnimation(GameObject animationObject, CanvasGroup canvasGroup)
    {
        var rectTransform = animationObject.GetComponent<RectTransform>();
        var startPosition = rectTransform.anchoredPosition;
        var endPosition = startPosition + Vector2.up * 50f; // 上に50ピクセル移動
        
        // 位置とアルファのアニメーションを並行実行
        var moveAnimation = LMotion.Create(startPosition, endPosition, 1.5f)
            .WithEase(Ease.OutQuad)
            .BindToAnchoredPosition(rectTransform);
            
        var fadeAnimation = LMotion.Create(1f, 0f, 1.5f)
            .WithEase(Ease.OutQuad)
            .WithDelay(0.3f) // 少し遅れてフェードアウト開始
            .Bind(alpha => canvasGroup.alpha = alpha);
        
        // 両方のアニメーション完了を待つ
        await UniTask.WhenAll(moveAnimation.ToUniTask(), fadeAnimation.ToUniTask());
        
        // オブジェクト削除
        if (animationObject) Destroy(animationObject);
    }
    
    private void Awake()
    {
        // 初期位置を保存
        _originalPosition = availableCitizensText.transform.localPosition;
    }
    
    private void OnDestroy()
    {
        if (_shakeHandle.IsActive()) _shakeHandle.Cancel();
        if (_colorHandle.IsActive()) _colorHandle.Cancel();
        _delayedCheckCts?.Cancel();
        _delayedCheckCts?.Dispose();
    }
}