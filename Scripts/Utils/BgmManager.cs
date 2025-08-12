using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;

namespace Void2610.UnityTemplate
{
    public class BgmManager : SingletonMonoBehaviour<BgmManager>
    {
        [System.Serializable]
        public class SoundData
        {
            public string name;
            public AudioClip audioClip;
            public float volume = 1.0f;
        }

        [SerializeField] private bool playOnStart = true;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private List<SoundData> bgmList = new List<SoundData>();

        private AudioSource _audioSource;
        private const float FADE_TIME = 1.0f;

        private bool _isPlaying;
        private float _bgmVolume = 1.0f;
        private bool _isFading;
        private SoundData _currentBGM;
        private MotionHandle _fadeHandle;

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = value;
                if (value <= 0.0f) value = 0.0001f;
                bgmMixerGroup.audioMixer.SetFloat("BgmVolume", Mathf.Log10(value) * 20);
                MySettings.BgmVolume = value;
            }
        }

        public void Resume()
        {
            if (_currentBGM == null) return;

            _isPlaying = true;
            _audioSource.Play();
            
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            _fadeHandle = LMotion.Create(_audioSource.volume, _currentBGM.volume, FADE_TIME)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .AddTo(this);
        }

        public void Pause()
        {
            _isPlaying = false;
            PauseInternal().Forget();
        }
        
        private async UniTaskVoid PauseInternal()
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            await LMotion.Create(_audioSource.volume, 0f, FADE_TIME)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .ToUniTask();
            
            _audioSource.Stop();
        }

        public async UniTask Stop()
        {
            _isPlaying = false;
            
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            await LMotion.Create(_audioSource.volume, 0f, FADE_TIME)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .ToUniTask();
            
            _audioSource.Stop();
            _currentBGM = null;
        }

        public void PlayBGM(string bgmName)
        {
            var data = bgmList.FirstOrDefault(t => t.name == bgmName);
            if (data == null)
            {
                Debug.LogError($"BGM '{bgmName}' が見つかりません。");
                return;
            }
            
            PlayBGMInternal(data).Forget();
        }

        public void PlayRandomBGM()
        {
            if (bgmList.Count == 0) return;

            var data = bgmList[Random.Range(0, bgmList.Count)];
            PlayBGMInternal(data).Forget();
        }
        
        private async UniTaskVoid PlayBGMInternal(SoundData data)
        {
            // 現在のBGMをフェードアウト
            if (_currentBGM != null)
            {
                if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
                await LMotion.Create(_audioSource.volume, 0f, FADE_TIME)
                    .WithEase(Ease.InQuad)
                    .BindToVolume(_audioSource)
                    .ToUniTask();
                _audioSource.Stop();
            }

            _currentBGM = data;
            _audioSource.clip = _currentBGM.audioClip;
            _audioSource.volume = 0;
            _audioSource.Play();

            // フェードイン
            _isFading = true;
            _fadeHandle = LMotion.Create(0f, _currentBGM.volume, FADE_TIME)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .AddTo(this);
            
            // フェードイン完了を待機
            FadeInComplete().Forget();
        }
        
        private async UniTaskVoid FadeInComplete()
        {
            await UniTask.Delay((int)(FADE_TIME * 1000));
            _isFading = false;
        }

        protected override void Awake()
        {
            base.Awake();
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.outputAudioMixerGroup = bgmMixerGroup;
            _audioSource.loop = false; // ループは手動で管理
            BgmVolume = MySettings.BgmVolume;
        }
        
        private void Start()
        {
            _currentBGM = null;
            bgmMixerGroup.audioMixer.SetFloat("BgmVolume", Mathf.Log10(_bgmVolume) * 20);
            _audioSource.volume = 0;
            
            if (playOnStart)
            {
                _isPlaying = true;
                PlayRandomBGM();
            }
        }

        private void Update()
        {
            if (_isPlaying && _audioSource.clip && !_isFading)
            {
                var remainingTime = _audioSource.clip.length - _audioSource.time;
                if (remainingTime <= FADE_TIME)
                {
                    _isFading = true;
                    LoopToNextBGM(remainingTime).Forget();
                }
            }
        }
        
        private async UniTaskVoid LoopToNextBGM(float fadeTime)
        {
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            await LMotion.Create(_audioSource.volume, 0f, fadeTime)
                .WithEase(Ease.InQuad)
                .BindToVolume(_audioSource)
                .ToUniTask();
            
            PlayRandomBGM();
        }
    }
}