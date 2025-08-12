using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainLifetimeScope : LifetimeScope
{
    [SerializeField] private AllCardDataList allCardDataList;
    [SerializeField] private AllEventDataList allEventDataList;
    [SerializeField] private GameSettings gameSettings;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // ScriptableObjectの登録
        builder.RegisterInstance(allCardDataList);
        builder.RegisterInstance(allEventDataList);
        builder.RegisterInstance(gameSettings);
        
        builder.Register<CardPoolService>(Lifetime.Singleton);
        builder.Register<EventPoolService>(Lifetime.Singleton);
        builder.Register<EndingService>(Lifetime.Singleton);
        builder.Register<ScoreService>(Lifetime.Singleton);
        
        builder.Register<PlayerModel>(Lifetime.Singleton).AsSelf();

        builder.RegisterEntryPoint<GameManager>().AsSelf();
        builder.RegisterEntryPoint<PlayerPresenter>();
        builder.RegisterEntryPoint<UIPresenter>();
        builder.RegisterEntryPoint<PostProcessController>();
    }
}
