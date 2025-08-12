using R3;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer.Unity;
using LitMotion;
using LitMotion.Extensions;
using Object = UnityEngine.Object;

/// <summary>
/// URPのポストプロセスエフェクトを管理するクラス
/// プレイヤーのHP変化に応じてビネット効果を制御する
/// </summary>
public class PostProcessController : IInitializable, IDisposable
{
    private readonly PlayerModel _playerModel;
    private readonly CompositeDisposable _disposables = new();
    
    private Volume _globalVolume;
    private Vignette _vignette;
    
    // ビネットの設定値
    private const float MIN_INTENSITY = 0.0f;      // 最小強度（HP満タン時）
    private const float MAX_INTENSITY = 0.4f;      // 最大強度（HP最小時）
    private const float ANIMATION_DURATION = 1.0f; // アニメーション時間
    
    private MotionHandle _vignetteHandle;
    
    public PostProcessController(PlayerModel playerModel)
    {
        _playerModel = playerModel;
    }
    
    public void Initialize()
    {
        // Global Volumeを検索
        _globalVolume = Object.FindFirstObjectByType<Volume>();
        
        if (!_globalVolume.profile.TryGet(out _vignette))
        {
            Debug.LogError("[PostProcessController] Vignetteエフェクトが見つかりません");
            return;
        }
        
        // HP変化の購読
        _playerModel.CurrentHp
            .Subscribe(OnHpChanged)
            .AddTo(_disposables);
        
        // 初期状態を設定
        SetVignetteIntensity(CalculateVignetteIntensity(_playerModel.CurrentHp.Value));
    }
    
    /// <summary>
    /// HP変化時のコールバック
    /// </summary>
    private void OnHpChanged(int newHp)
    {
        var targetIntensity = CalculateVignetteIntensity(newHp);
        AnimateVignetteIntensity(targetIntensity);
    }
    
    /// <summary>
    /// HPに基づいてビネット強度を計算
    /// </summary>
    private float CalculateVignetteIntensity(int currentHp)
    {
        // HP割合を計算（0.0f〜1.0f）
        var hpRatio = Mathf.Clamp01((float)currentHp / 100f); // 最大HP100として計算
        
        // HP割合を反転してビネット強度に変換
        var intensity = Mathf.Lerp(MAX_INTENSITY, MIN_INTENSITY, hpRatio);
        
        return intensity;
    }
    
    /// <summary>
    /// ビネット強度をスムーズに変更
    /// </summary>
    private void AnimateVignetteIntensity(float targetIntensity)
    {
        // 既存のアニメーションをキャンセル
        if (_vignetteHandle.IsActive())
        {
            _vignetteHandle.Cancel();
        }
        
        var currentIntensity = _vignette.intensity.value;
        
        _vignetteHandle = LMotion.Create(currentIntensity, targetIntensity, ANIMATION_DURATION)
            .WithEase(Ease.OutCubic)
            .Bind( SetVignetteIntensity);
    }
    
    /// <summary>
    /// ビネット強度を直接設定
    /// </summary>
    private void SetVignetteIntensity(float intensity)
    {
        if (_vignette)
        {
            _vignette.intensity.value = intensity;
        }
    }
    
    public void Dispose()
    {
        // アニメーションハンドルをキャンセル
        if (_vignetteHandle.IsActive())
        {
            _vignetteHandle.Cancel();
        }
        
        _disposables?.Dispose();
    }
}