using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private GameObject bgmManagerPrefab;
    [SerializeField] private GameObject seManagerPrefab;
    
    protected override void Configure(IContainerBuilder builder)
    {
        CreateAudioManagers();
    }
    
    private void CreateAudioManagers()
    {
        var bgmManager = Instantiate(bgmManagerPrefab);
        DontDestroyOnLoad(bgmManager);
        var seManager = Instantiate(seManagerPrefab);
        DontDestroyOnLoad(seManager);
    }
}
