using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace Void2610.UnityTemplate
{
    public class SeManager : SingletonMonoBehaviour<SeManager>
    {
        [System.Serializable]
        public class SoundData
        {
            public string name;
            public AudioClip audioClip;
            public float volume = 1.0f;
        }

        [SerializeField] private AudioMixerGroup seMixerGroup;
        [SerializeField] private SoundData[] soundData;

        private readonly AudioSource[] _seAudioSourceList = new AudioSource[20];
        private float _seVolume = 0.5f;

        public float SeVolume
        {
            get => _seVolume;
            set
            {
                _seVolume = value;
                if (value <= 0.0f)
                {
                    value = 0.0001f;
                }
                seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(value) * 20);
                MySettings.SeVolume = value;
            }
        }

        public void PlaySe(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, bool important = false)
        {
            var audioSource = GetAvailableAudioSource(important);
            if (!clip)
            {
                Debug.LogError("AudioClip could not be found.");
                return;
            }
            if (!audioSource)
            {
                Debug.LogWarning("There is no available AudioSource.");
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.Play();
        }

        public void PlaySe(string seName, float volume = 1.0f, float pitch = -1.0f, bool important = false)
        {
            var data = this.soundData.FirstOrDefault(t => t.name == seName);
            var audioSource = GetAvailableAudioSource(important);
            if (data == null) return;
            if (!audioSource) return;

            audioSource.clip = data.audioClip;
            audioSource.volume = data.volume * volume;
            audioSource.pitch = pitch < 0.0f ? Random.Range(0.8f, 1.2f) : pitch;
            audioSource.Play();
        }
    
        public void WaitAndPlaySe(string seName, float time, float volume = 1.0f, float pitch = 1.0f, bool important = false)
        {
            WaitAndPlaySeAsync(seName, time, volume, pitch, important).Forget();
        }
    
        private async UniTaskVoid WaitAndPlaySeAsync(string seName, float time, float volume = 1.0f, float pitch = 1.0f, bool important = false)
        {
            await UniTask.Delay((int)(time * 1000));
            PlaySe(seName, volume, pitch, important);
        }

        /// <summary>
        /// 利用可能なAudioSourceを取得する
        /// </summary>
        /// <param name="important">重要な効果音フラグ。trueの場合、すべてのAudioSourceが使用中でも強制的に再生する</param>
        /// <returns>利用可能なAudioSource。importantがfalseで全て使用中の場合はnull</returns>
        private AudioSource GetAvailableAudioSource(bool important = false)
        {
            // 使用中でないAudioSourceを探す
            var unusedAudioSource = _seAudioSourceList.FirstOrDefault(t => t.isPlaying == false);
            if (unusedAudioSource) return unusedAudioSource;
            
            // importantフラグがtrueの場合、強制的に最初のAudioSourceを使用
            if (important)
            {
                var forcedAudioSource = _seAudioSourceList[0];
                forcedAudioSource.Stop(); // 現在の再生を停止
                return forcedAudioSource;
            }
            
            // importantがfalseで利用可能なAudioSourceがない場合はnullを返す
            return null;
        }

        protected override void Awake()
        {
            base.Awake();
            for (var i = 0; i < _seAudioSourceList.Length; ++i)
            {
                _seAudioSourceList[i] = gameObject.AddComponent<AudioSource>();
                _seAudioSourceList[i].outputAudioMixerGroup = seMixerGroup;
            }
            SeVolume = MySettings.SeVolume;
        }
        
        private void Start()
        {
            seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(_seVolume) * 20);
        }
    }
}