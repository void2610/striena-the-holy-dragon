using CriWare;
using CriWare.Assets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LitMotion;
using Void2610.UnityTemplate;

public class CriBgmController : SingletonMonoBehaviour<CriBgmController>
{
    [SerializeField] private CriAtomCueReference bgm1CueReference;
    [SerializeField] private CriAtomCueReference bgm2CueReference;
    [SerializeField] private CriAtomCueReference nearTrueEndCueReference;
    [SerializeField] private CriAtomCueReference endCueReference;

    public bool IsPlayingBGM1 { get; private set; } = true;
    public bool HasCurrentPlayback { get; private set; } // SimplePlaybackがstructなのでフラグで管理
    
    private const string AISAC_CONTROL_NAME = "AisacControl_00";
    private const float FADE_TIME = 1.0f;
    
    private SimpleSoundManager.SimplePlayback _currentPlayback;
    private CriAtomExAcb _acbAsset;
    
    private float _bgmVolume = 1.0f;
    private float _currentFadeVolume; // フェード用の音量
    private float _aisacValue; // インタラクティブミュージック用の値
    private MotionHandle _fadeHandle;

    // 音量プロパティ（MySettingsと連携）
    public float BgmVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = Mathf.Clamp01(value);
            MySettings.BgmVolume = _bgmVolume;
            // 音量を即座に反映
            if (HasCurrentPlayback)
            {
                _currentPlayback.SetVolumeAndPitch(_bgmVolume * _currentFadeVolume, 1.0f);
            }
        }
    }

    // AISACコントロール値（インタラクティブミュージック用）
    public float AisacValue
    {
        get => _aisacValue;
        set
        {
            _aisacValue = Mathf.Clamp01(value);
            if (HasCurrentPlayback)
            {
                _currentPlayback.SetAisacControl(AISAC_CONTROL_NAME, _aisacValue);
            }
        }
    }

    public void PlayBGM1()
    {
        IsPlayingBGM1 = true;
        PlayBGMInternal(bgm1CueReference, false).Forget();
    }

    public void PlayBGM2()
    {
        PlayBGMInternal(bgm2CueReference, false).Forget();
        IsPlayingBGM1 = false;
    }
    
    public void PlayNearTrueEndBGM()
    {
        PlayBGMInternal(nearTrueEndCueReference, false).Forget();
        IsPlayingBGM1 = false;
    }
    
    public void PlayEndBGM()
    {
        PlayBGMInternal(endCueReference, false).Forget();
        IsPlayingBGM1 = false;
    }

    public void Pause() => PauseInternal().Forget();

    // 停止
    public async UniTask Stop(float fadeOutTime = FADE_TIME)
    {
        await FadeOut(fadeOutTime);
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
        }
    }

    // 再生を再開
    public void Resume()
    {
        if (!HasCurrentPlayback) return;
        _currentPlayback.Resume();
        FadeIn().Forget();
    }

    protected override void Awake()
    {
        base.Awake();
        BgmVolume = MySettings.BgmVolume;
    }

    private void Start()
    {
        InitializeCri().Forget();
    }
    
    private void OnEnable()
    {
        // シーン遷移後の再アクティブ化時にCRIを再初期化
        if (!HasCurrentPlayback && _acbAsset == null)
        {
            InitializeCri().Forget();
        }
    }

    private async UniTaskVoid InitializeCri()
    {
        // CRIのキューシートがロードされるまで待機
        while (CriAtom.CueSheetsAreLoading)
        {
            await UniTask.Yield();
        }

        _currentFadeVolume = 0f;
    }

    // BGM再生の内部処理
    private async UniTaskVoid PlayBGMInternal(CriAtomCueReference r, bool useFadeIn = true)
    {
        // 現在のBGMをフェードアウト
        if (HasCurrentPlayback)
        {
            await FadeOut();
            _currentPlayback.Stop();
            HasCurrentPlayback = false;
            // 少し間を空ける
            await UniTask.Delay(50);
        }

        // 新しいBGMを再生
        _currentPlayback = SimpleSoundManager.Instance.StartPlayback(r);
        HasCurrentPlayback = true;
        
        // 少し待ってからチェック
        await UniTask.Delay(100);
        
        // 再生チェック
        if (!_currentPlayback.IsPlaying())
        {
            HasCurrentPlayback = false;
            return;
        }

        // 初期音量を0に設定
        _currentFadeVolume = 0f;
        _currentPlayback.SetVolumeAndPitch(0f, 1.0f);
        
        // AISACの初期値を設定
        _currentPlayback.SetAisacControl(AISAC_CONTROL_NAME, _aisacValue);
        
        // フェードイン
        if (useFadeIn)
        {
            await FadeIn();
        }
        else
        {
            _currentFadeVolume = 1f; // フェードインしない場合は音量を最大に設定
            _currentPlayback.SetVolumeAndPitch(_bgmVolume, 1.0f);
        }
    }

    private void Update()
    {
        // 音量を毎フレーム更新
        if (HasCurrentPlayback)
        {
            _currentPlayback.SetVolumeAndPitch(_bgmVolume * _currentFadeVolume, 1.0f);
        }
    }

    // フェードイン
    private async UniTask FadeIn(float fadeInTime = FADE_TIME)
    {
        if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        
        _fadeHandle = LMotion.Create(0f, 1f, fadeInTime)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithEase(Ease.InQuad)
            .Bind(x => _currentFadeVolume = x)
            .AddTo(this);
        
        await _fadeHandle.ToUniTask();
    }

    // フェードアウト
    private async UniTask FadeOut(float fadeOutTime = FADE_TIME)
    {
        if (!HasCurrentPlayback) return;
        
        if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        
        _fadeHandle = LMotion.Create(_currentFadeVolume, 0f, fadeOutTime)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .WithEase(Ease.InQuad)
            .Bind(x => _currentFadeVolume = x)
            .AddTo(this);
        
        await _fadeHandle.ToUniTask();
    }

    private async UniTaskVoid PauseInternal()
    {
        await FadeOut();
        if (HasCurrentPlayback)
        {
            _currentPlayback.Pause();
        }
    }

    // AISACをスムーズに変更
    public async UniTask SetAisacSmooth(float targetValue, float duration = 1f)
    {
        if (!HasCurrentPlayback) return;
        
        await LMotion.Create(_aisacValue, targetValue, duration)
            .WithEase(Ease.InOutQuad)
            .Bind(x => AisacValue = x)
            .ToUniTask();
    }

    protected override void OnDestroy()
    {
        if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        if (HasCurrentPlayback)
        {
            _currentPlayback.Stop();
        }
        base.OnDestroy();
    }
}