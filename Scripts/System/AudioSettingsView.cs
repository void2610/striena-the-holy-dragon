using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// BGMとSEの音量設定を管理するView
/// 複数のシーンで再利用可能
/// </summary>
public class AudioSettingsView : MonoBehaviour
{
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider seVolumeSlider;
    
    private void Awake()
    {
        SetupSliders();
    }
    
    /// <summary>
    /// スライダーの初期設定
    /// </summary>
    private void SetupSliders()
    {
        // BGMスライダーの設定
        if (bgmVolumeSlider)
        {
            bgmVolumeSlider.value = MySettings.BgmVolume;
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }
        
        // SEスライダーの設定
        if (seVolumeSlider)
        {
            seVolumeSlider.value = MySettings.SeVolume;
            seVolumeSlider.onValueChanged.AddListener(OnSeVolumeChanged);
            
            // SEスライダーのクリックを離したときにテスト音源を再生
            var eventTrigger = seVolumeSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp
            };
            entry.callback.AddListener(_ => PlayTestSound());
            eventTrigger.triggers.Add(entry);
        }
    }
    
    /// <summary>
    /// BGM音量変更時の処理
    /// </summary>
    private void OnBgmVolumeChanged(float value)
    {
        MySettings.BgmVolume = value;
        CriBgmController.Instance.BgmVolume = value;
    }
    
    /// <summary>
    /// SE音量変更時の処理
    /// </summary>
    private void OnSeVolumeChanged(float value)
    {
        SeManager.Instance.SeVolume = value;
    }
    
    /// <summary>
    /// テストSEを再生
    /// </summary>
    private void PlayTestSound()
    {
        SeManager.Instance.PlaySe("Test");
    }
    
    /// <summary>
    /// スライダーの値を外部から設定
    /// </summary>
    public void SetBgmVolume(float volume)
    {
        if (bgmVolumeSlider)
        {
            bgmVolumeSlider.value = volume;
        }
    }
    
    /// <summary>
    /// スライダーの値を外部から設定
    /// </summary>
    public void SetSeVolume(float volume)
    {
        if (seVolumeSlider)
        {
            seVolumeSlider.value = volume;
        }
    }
    
    private void OnDestroy()
    {
        // リスナーの解除
        if (bgmVolumeSlider)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        }
        
        if (seVolumeSlider)
        {
            seVolumeSlider.onValueChanged.RemoveListener(OnSeVolumeChanged);
        }
    }
}