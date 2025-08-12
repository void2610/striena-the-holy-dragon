using UnityEngine;
using VContainer;
using VContainer.Unity;

public class EndingLifetimeScope : LifetimeScope
{
    [SerializeField] private AllEndingDataList allEndingDataList;
    
    protected override void Configure(IContainerBuilder builder)
    {
        allEndingDataList.RegisterAllEndings();
        // ScriptableObjectの登録
        builder.RegisterInstance(allEndingDataList);
        
        // EndingPresenterの登録
        builder.RegisterEntryPoint<EndingPresenter>();
    }
}
