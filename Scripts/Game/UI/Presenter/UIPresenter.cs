using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using Void2610.UnityTemplate;

/// <summary>
/// UI全体を管理するPresenter
/// </summary>
public class UIPresenter : IInitializable, IDisposable
{
    private readonly PlayerModel _playerModel;
    private readonly GameManager _gameManager;
    
    // UI View参照
    private TurnCountView _turnCountView;
    private PopulationView _populationView;
    private HandView _handView;
    private PauseView _pauseView;
    private TutorialView _tutorialView;
    private PauseButtonView _pauseButtonView;
    private FadeImageView _fadeImageView;
    private AreaSelectionView _areaSelectionView;
    private BattleAreaView _battleAreaView;
    private EventDisplayView _eventDisplayView;
    private BackgroundMapView _backgroundMapView;
    private PlayerView _playerView;
    private ResetHandView _resetHandView;
    
    // CanvasGroup切り替え管理
    private CanvasGroupSwitcher _canvasGroupSwitcher;
    
    // 購読管理用
    private readonly CompositeDisposable _disposables = new();
    
    public UIPresenter(PlayerModel playerModel, GameManager gameManager)
    {
        _playerModel = playerModel;
        _gameManager = gameManager;
    }
    
    public void Initialize()
    {
        // UI Viewを取得
        _turnCountView = UnityEngine.Object.FindAnyObjectByType<TurnCountView>();
        _populationView = UnityEngine.Object.FindAnyObjectByType<PopulationView>();
        _handView = UnityEngine.Object.FindAnyObjectByType<HandView>();
        _pauseView = UnityEngine.Object.FindAnyObjectByType<PauseView>();
        _tutorialView = UnityEngine.Object.FindAnyObjectByType<TutorialView>();
        _pauseButtonView = UnityEngine.Object.FindAnyObjectByType<PauseButtonView>();
        _fadeImageView = UnityEngine.Object.FindAnyObjectByType<FadeImageView>();
        _areaSelectionView = UnityEngine.Object.FindAnyObjectByType<AreaSelectionView>();
        _battleAreaView = UnityEngine.Object.FindAnyObjectByType<BattleAreaView>();
        _eventDisplayView = UnityEngine.Object.FindAnyObjectByType<EventDisplayView>();
        _backgroundMapView = UnityEngine.Object.FindAnyObjectByType<BackgroundMapView>();
        _playerView = UnityEngine.Object.FindAnyObjectByType<PlayerView>();
        _resetHandView = UnityEngine.Object.FindAnyObjectByType<ResetHandView>();
        
        // CanvasGroupSwitcherを初期化
        InitializeCanvasGroupSwitcher();
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
        
        // 初期表示を更新
        UpdateInitialViews();
        // PlayerModelの変更を監視してViewを更新
        BindPlayerModel();
        // GameManagerの状態変更を監視
        BindGameManager();
        // PauseButtonのイベントを設定
        _pauseButtonView.SetPauseButtonListener(() => 
        {
            _canvasGroupSwitcher.EnableCanvasGroup("Pause", true);
            Time.timeScale = 0f;
        });
        // PauseViewの再開ボタンイベントを設定
        _pauseView.SetResumeButtonListener(() => 
        {
            _canvasGroupSwitcher.EnableCanvasGroup("Pause", false);
            Time.timeScale = 1f;
        });
        _pauseView.SetTitleButtonListener(() => GoToTitle().Forget());
        _tutorialView.SetCloseButtonListener(() => _canvasGroupSwitcher.EnableCanvasGroup("Tutorial", false));
        _fadeImageView.FadeOut().Forget();
    }
    
    /// <summary>
    /// PlayerModelとViewをバインド
    /// </summary>
    private void BindPlayerModel()
    {
        // 人口表示のViewを更新
        // 市民数と生存率の変更を監視
        _playerModel.AvailableCitizens
            .CombineLatest(_playerModel.EvacuatedCitizens, 
                _playerModel.KilledInActionCitizens, _playerModel.SurvivalRate,
                (available, evacuated, killed, survivalRate) => 
                    new { available, evacuated, killed, survivalRate })
            .Subscribe(d =>
            {
                _populationView.UpdatePopulationDisplay(d.available, d.killed, d.evacuated, d.survivalRate);
            })
            .AddTo(_disposables);
            
        // 手札の変更を監視
        _playerModel.HandCardsChanged
            .Subscribe(_ => OnHandCardsChanged())
            .AddTo(_disposables);
        
        // HandViewのカード選択イベントを監視
        _handView.OnCardSelected += OnCardSelected;
        // ResetHandViewのリセットボタンイベントを監視
        _resetHandView.OnResetButtonClicked += OnResetHandButtonClicked;
    }
    
    /// <summary>
    /// GameManagerとUIをバインド
    /// </summary>
    private void BindGameManager()
    {
        // ゲームステートの変更を監視
        _gameManager.OnStateChanged
            .Subscribe(OnGameStateChanged)
            .AddTo(_disposables);
        
        // 敵のスタン状態の変更を監視
        _gameManager.EnemyStunTurnsLeft
            .Subscribe(stunTurns => _populationView.UpdateStunStateColor(stunTurns > 0))
            .AddTo(_disposables);
            
        // ターン数の変更を監視
        _gameManager.CurrentTurn
            .Subscribe(turn => _turnCountView.UpdateTurnDisplay(turn))
            .AddTo(_disposables);
            
        // カード使用時の削除アニメーションを監視
        _gameManager.OnCardUsed
            .Subscribe(usedCardInfo => OnCardUsed(usedCardInfo.cardData, usedCardInfo.index))
            .AddTo(_disposables);
            
        // エリア選択要求を監視
        _gameManager.OnAreaSelectionRequired
            .Subscribe(areas => OnAreaSelectionRequired(areas.area1, areas.area2).Forget())
            .AddTo(_disposables);
            
        // ランダムイベント発生を監視
        _gameManager.OnRandomEventOccurred
            .Subscribe(eventData => OnRandomEventOccurred(eventData).Forget())
            .AddTo(_disposables);
            
        // ゲーム終了を監視
        _gameManager.OnGameEnded
            .Subscribe(endingNumber => OnGameEnded(endingNumber).Forget())
            .AddTo(_disposables);
            
        // 現在エリアの変更を監視（背景マップ移動）
        _gameManager.CurrentArea
            .Subscribe(area => 
            {
                _backgroundMapView.MoveToArea(area);
                _backgroundMapView.SetBackground(_gameManager.RetreatCount.CurrentValue);
                _battleAreaView.UpdateBattleArea(area);
                if (_gameManager.RetreatCount.CurrentValue > 1)
                {
                    if (_playerModel.KilledInActionCitizens.CurrentValue < 1)
                        CriBgmController.Instance.PlayNearTrueEndBGM();
                    else
                        CriBgmController.Instance.PlayBGM2();
                }
            })
            .AddTo(_disposables);
    }
    
    private async UniTask GoToTitle()
    {
        var bgm = CriBgmController.Instance.Stop();
        var fade = _fadeImageView.FadeIn();
        await UniTask.WhenAll(bgm, fade);
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }
    
    /// <summary>
    /// 手札変更時の処理
    /// </summary>
    private void OnHandCardsChanged()
    {
        // 手札ビューを更新
        _handView.UpdateHand(_playerModel.HandCards);
        _handView.UpdateCardUsability(_playerModel);
    }
    
    /// <summary>
    /// カード選択時の処理
    /// </summary>
    private void OnCardSelected(CardData cardData, int cardIndex)
    {
        _gameManager.SelectCard(cardData, cardIndex);
    }
    
    /// <summary>
    /// 手札リセットボタンクリック時の処理
    /// </summary>
    private void OnResetHandButtonClicked()
    {
        ResetHandAsync().Forget();
        return;
        
        async UniTaskVoid ResetHandAsync()
        {
            // リセット処理中はボタンを無効化
            _resetHandView.SetResetButtonEnabled(false);
            // リセット処理を非同期で実行
            await _gameManager.ResetHandAsync(true);
        }
    }
    
    /// <summary>
    /// 使用されたカードを削除アニメーション付きで削除
    /// </summary>
    private async UniTask RemoveUsedCard(int cardIndex)
    {
        // HandViewの削除アニメーションを実行（インデックス指定）
        await _handView.RemoveSpecificCardByIndex(cardIndex);
    }
    
    /// <summary>
    /// カード使用時の処理（削除アニメーション）
    /// </summary>
    private void OnCardUsed(CardData usedCard, int cardIndex)
    {
        // 削除アニメーションを非同期で実行（Forget）
        RemoveUsedCard(cardIndex).Forget();
        
        // 危険カード演出
        if (usedCard.IsDangerousCard)
        {
            _playerView.ShowDialogue(PlayerDialogueType.DangerCardUsed);
        }
    }
    
    /// <summary>
    /// エリア選択要求時の処理
    /// </summary>
    private async UniTask OnAreaSelectionRequired(BattleAreaType area1, BattleAreaType area2)
    {
        await CriBgmController.Instance.SetAisacSmooth(1f);
        
        // 選択可能なエリアを保持
        var areas = new[] { area1, area2 };
        // イベントハンドラを登録
        _areaSelectionView.SetArea(area1, area2, i => OnButtonClicked(i).Forget());
        // エリア選択UIを表示
        _canvasGroupSwitcher.EnableCanvasGroup("AreaSelection", true);
        return;

        async UniTask OnButtonClicked(int index)
        {
            _gameManager.SelectArea(areas[index]);
            _canvasGroupSwitcher.EnableCanvasGroup("AreaSelection", false);
            SeManager.Instance.PlaySe("Footstep");
            await UniTask.Delay(500);
            CriBgmController.Instance.SetAisacSmooth(0f, 0.5f).Forget();
        }
    }
    
    /// <summary>
    /// ゲームステート変更時の処理
    /// </summary>
    private void OnGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Retreat:
                // 撤退演出中は手札を非表示にする
                _handView.gameObject.SetActive(false);
                break;
                
            case GameManager.GameState.DrawCard:
                // カード配布時は手札を表示する
                _handView.gameObject.SetActive(true);
                break;
                
            case GameManager.GameState.PlayerAction:
                // プレイヤーアクション中はリセットボタンを有効化
                _resetHandView.SetResetButtonEnabled(true);
                break;
                
            default:
                _resetHandView.SetResetButtonEnabled(false);
                break;
        }
    }
    
    /// <summary>
    /// CanvasGroupSwitcherを初期化
    /// </summary>
    private void InitializeCanvasGroupSwitcher()
    {
        // シーン内のすべてのCanvasGroupを取得
        var allCanvasGroups = UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
        var canvasGroups = new List<CanvasGroup>(allCanvasGroups);
        _canvasGroupSwitcher = new CanvasGroupSwitcher(canvasGroups, "Tutorial");
    }
    
    /// <summary>
    /// 初期表示を更新
    /// </summary>
    private void UpdateInitialViews()
    {
        // ターン数表示を更新
        _turnCountView.UpdateTurnDisplay(_gameManager.CurrentTurn.CurrentValue);
        
        // 人口表示を更新
        _populationView.UpdatePopulationDisplay(
            _playerModel.AvailableCitizens.CurrentValue,
            _playerModel.KilledInActionCitizens.CurrentValue,
            _playerModel.EvacuatedCitizens.CurrentValue,
            _playerModel.SurvivalRate.CurrentValue);
        
        // 手札表示を更新
        _handView.UpdateHand(_playerModel.HandCards);
        _handView.UpdateCardUsability(_playerModel);
    }
    
    /// <summary>
    /// ランダムイベント発生時の処理
    /// </summary>
    private async UniTask OnRandomEventOccurred(EventData eventData)
    {
        // イベント情報を設定
        _eventDisplayView.SetEventInfo(eventData.EventDescription, eventData.EffectDescription);
        
        // イベント表示UIを表示
        _canvasGroupSwitcher.EnableCanvasGroup("EventDisplay", true);
        await UniTask.Delay(3000);
        _canvasGroupSwitcher.EnableCanvasGroup("EventDisplay", false);
    }
    
    /// <summary>
    /// ゲーム終了時の処理（エンディングシーンへの遷移）
    /// </summary>
    private async UniTask OnGameEnded(int endingNumber)
    {
        await UniTask.Delay(2000);
        
        var  t1 = CriBgmController.Instance.Stop(2f); 
        var t2 = _fadeImageView.FadeIn(2f);
        
        await UniTask.WhenAll(t1, t2);
        
        // エンディングシーンに遷移
        UnityEngine.SceneManagement.SceneManager.LoadScene("EndingScene");
    }
    
    public void Dispose()
    {
        // イベントの購読解除
        if (_handView) _handView.OnCardSelected -= OnCardSelected;
        if (_resetHandView) _resetHandView.OnResetButtonClicked -= OnResetHandButtonClicked;
        _disposables?.Dispose();
    }
}