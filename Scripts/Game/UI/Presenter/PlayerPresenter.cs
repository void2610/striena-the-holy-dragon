using R3;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Void2610.UnityTemplate;
using Object = UnityEngine.Object;

/// <summary>
/// プレイヤーの状態に応じて立ち絵とセリフを制御するPresenterクラス
/// </summary>
public class PlayerPresenter : IInitializable, IDisposable
{
    private readonly PlayerModel _playerModel;
    private readonly GameManager _gameManager;
    private PlayerView _playerView;
    private PlayerHealthView _playerHealthView;
    private readonly CompositeDisposable _disposables = new();
    
    private int _previousHp;
    private bool _isInitialized;
    
    public PlayerPresenter(PlayerModel playerModel, GameManager gameManager)
    {
        _playerModel = playerModel;
        _gameManager = gameManager;
        _previousHp = playerModel.CurrentHp.Value;
    }
    
    public void Initialize()
    {
        _playerView = Object.FindAnyObjectByType<PlayerView>();
        _playerHealthView = Object.FindAnyObjectByType<PlayerHealthView>();
        
        // UI初期化（少し遅延してから実行）
        InitializeUIDelayed().Forget();
    } 
    
    /// <summary>
    /// UI初期化処理（遅延実行）
    /// </summary>
    private async UniTaskVoid InitializeUIDelayed()
    {
        // 少し待ってからUI初期化を実行
        await UniTask.DelayFrame(1);
        
        BindEvents();
        
        _playerHealthView.UpdateHealthDisplay(_playerModel.CurrentHp.Value);
        
        // 初期化完了フラグを設定
        _isInitialized = true;
        
        // 初期の体力に基づいてスタンスを設定
        var initialStance = GetNormalStanceBasedOnHealth();
        _playerView.ChangeStance(initialStance);
        
        // ゲーム開始時のセリフを表示
        await UniTask.Delay(500); // 少し遅延してから表示
        _playerView.ShowDialogue(PlayerDialogueType.GameStart);
    }
    
    private void BindEvents()
    {
        // 体力変化を監視
        _playerModel.CurrentHp
            .Subscribe(hp =>
            {
                // 初期化完了後のみ処理を実行
                if (_isInitialized && hp != _previousHp)
                {
                    if (hp < _previousHp)
                        ShowDamageReaction().Forget();
                    else if (hp > _previousHp)
                        ShowHealReaction().Forget();
                }
                _previousHp = hp;
            })
            .AddTo(_disposables);
        
        // ゲームステート変化を監視
        _gameManager.OnStateChanged
            .Subscribe(state =>
            {
                switch (state)
                {
                    case GameManager.GameState.Retreat:
                        _playerView.ShowDialogue(PlayerDialogueType.RetreatProgress);
                        ShowDialogueStance().Forget();
                        break;
                    case GameManager.GameState.GameEnd:
                        // プレイヤーの生存状態で勝利・敗北を判断
                        _playerView.ShowDialogue(_playerModel.IsAlive.CurrentValue
                            ? PlayerDialogueType.Victory
                            : PlayerDialogueType.Defeat);
                        break;
                }
            })
            .AddTo(_disposables);
        
        // プレイヤー体力の変更を監視
        _playerModel.CurrentHp
            .Subscribe(_playerHealthView.UpdateHealthDisplay)
            .AddTo(_disposables);
    }
    
    
    /// <summary>
    /// エリア変更時にDialogueスタンスを表示
    /// </summary>
    private async UniTaskVoid ShowDialogueStance()
    {
        if (!_isInitialized) return;
        
        _playerView.ChangeStance(PlayerStance.Dialogue);
        await UniTask.Delay(1500); // 1.5秒表示
        
        // 生存していれば体力に応じた通常状態に戻す
        if (_playerModel.IsAlive.CurrentValue)
        {
            var normalStance = GetNormalStanceBasedOnHealth();
            _playerView.ChangeStance(normalStance);
        }
    }

    /// <summary>
    /// ダメージ受けた時の立ち絵表示
    /// </summary>
    private async UniTaskVoid ShowDamageReaction()
    {
        _playerView.ShowDialogue(PlayerDialogueType.TakeDamage);
        SeManager.Instance.PlaySe("PlayerDamage");
        ParticleManager.Instance.PlayParticle("EnemyAttack");
        _playerView.ChangeStance(PlayerStance.Damage);
        
        // カメラを揺らす（ダメージ量に応じて強度を調整）
        var damageAmount = _previousHp - _playerModel.CurrentHp.Value;
        var shakeMagnitude = Mathf.Clamp(damageAmount * 0.05f, 0.5f, 2f); // ダメージ量に応じて0.1〜0.5の範囲で揺れの強さを設定
        CameraShake.Instance.ShakeCamera(shakeMagnitude, 0.2f, 15, 0.6f);
        
        await UniTask.Delay(500); // 0.5秒表示
        
        // 生存していれば体力に応じた通常状態に戻す
        if (_playerModel.IsAlive.CurrentValue)
        {
            var normalStance = GetNormalStanceBasedOnHealth();
            _playerView.ChangeStance(normalStance);
        }
    }
    
    /// <summary>
    /// 回復アクション時の立ち絵表示
    /// </summary>
    private async UniTaskVoid ShowHealReaction()
    {
        _playerView.ShowDialogue(PlayerDialogueType.Heal);
        SeManager.Instance.PlaySe("PlayerHeal");
        ParticleManager.Instance.PlayParticle("PlayerHeal");
        _playerView.ChangeStance(PlayerStance.Heal);
        await UniTask.Delay(400); // 0.4秒表示
        
        // 生存していれば体力に応じた通常状態に戻す
        if (_playerModel.IsAlive.CurrentValue)
        {
            var normalStance = GetNormalStanceBasedOnHealth();
            _playerView.ChangeStance(normalStance);
        }
    }
    
    /// <summary>
    /// 体力に基づいて通常時のスタンスを取得（体力が低い場合はDying、それ以外はNormal）
    /// </summary>
    private PlayerStance GetNormalStanceBasedOnHealth()
    {
        const int lowHealthThreshold = 45; // 最大体力150の30%
        return _playerModel.CurrentHp.Value <= lowHealthThreshold ? PlayerStance.Dying : PlayerStance.Normal;
    }
    
    public void Dispose()
    {
        _disposables?.Dispose();
    }
}