using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// エンディングシーンのプレゼンター
/// </summary>
public class EndingPresenter : IStartable
{
    private readonly AllEndingDataList _allEndingDataList;
    
    private EndingView _endingView;
    private FadeImageView _fadeImageView;
    private StoryTextView _storyTextView;
    
    [Inject]
    public EndingPresenter(AllEndingDataList allEndingDataList)
    {
        _allEndingDataList = allEndingDataList;
    }
    
    public void Start()
    {
        Time.timeScale = 1;
        
        _endingView = Object.FindAnyObjectByType<EndingView>();
        _fadeImageView = Object.FindAnyObjectByType<FadeImageView>();
        _storyTextView = Object.FindAnyObjectByType<StoryTextView>();
        
        // エンディングシーケンスを開始
        StartEndingSequence().Forget();
    }
    /// <summary>
    /// エンディングシーケンスを開始する
    /// </summary>
    private async UniTaskVoid StartEndingSequence()
    {
        var (endingNumber, turn, survivalRate, evacuated, killed) = EndingService.GetSavedEndingData();
        var endingData = _allEndingDataList.GetEndingData(endingNumber);
        _endingView.UpdateEndingDisplay(endingData, endingData.EndingDescription, turn, survivalRate, evacuated, killed);
        
        // TrueエンドならBGMを再生
        if (EndingService.IsTrueEnding(endingNumber))
            CriBgmController.Instance.PlayEndBGM();
        
        await _storyTextView.StartStory(endingData.StoryTexts);
        await _fadeImageView.FadeOut();
    }
}