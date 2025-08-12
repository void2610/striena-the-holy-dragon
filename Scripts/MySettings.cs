using UnityEngine;

public static class MySettings
{
    private const string INITIALIZED_KEY = "SettingsInitialized";
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SE_VOLUME_KEY = "SEVolume";
    private const string TIMESCALE_KEY = "TimeScale";
    
    // 静的コンストラクタで初期化処理を行う
    static MySettings()
    {
        if (!PlayerPrefs.HasKey(INITIALIZED_KEY))
        {
            ResetSettings();
            PlayerPrefs.SetInt(INITIALIZED_KEY, 1); // 初期化済みフラグを設定
        }
    }
    
    public static float BgmVolume
    {
        get => PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        set 
        {
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, value);
            PlayerPrefs.Save();
        }
    }
    
    public static float SeVolume
    {
        get => PlayerPrefs.GetFloat(SE_VOLUME_KEY, 0.5f);
        set 
        {
            PlayerPrefs.SetFloat(SE_VOLUME_KEY, value);
            PlayerPrefs.Save();
        }
    }
    
    public static float TimeScale
    {
        get => PlayerPrefs.GetFloat(TIMESCALE_KEY, 1f);
        set 
        {
            PlayerPrefs.SetFloat(TIMESCALE_KEY, value);
            PlayerPrefs.Save();
        }
    }
    
    public static void ResetSettings()
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, 0.5f);
        PlayerPrefs.SetFloat(SE_VOLUME_KEY, 0.5f);
        PlayerPrefs.SetFloat(TIMESCALE_KEY, 1f);
        PlayerPrefs.Save();
    }
}
