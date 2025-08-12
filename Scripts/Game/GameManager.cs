using CriWare;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Void2610.UnityTemplate;

/// <summary>
/// ゲームのメインループを管理するクラス
/// </summary>
public class GameManager : IInitializable, ITickable
{
    // ゲームステート
    public enum GameState
    {
        Initialize,      // 初期化
        DrawCard,        // カード配布
        PlayerAction,    // プレイヤー行動
        CardEffect,      // カード効果処理
        CheckGameEnd,    // ゲーム終了判定
        EnemyProgress,   // 敵の進行
        RandomEvent,     // ランダムイベント
        Retreat,         // 撤退演出
        GameEnd          // ゲーム終了（旧GameOver/Clear統合）
    }
    
    private readonly PlayerModel _playerModel;
    private readonly CardPoolService _cardPoolService;
    private readonly EventPoolService _eventPoolService;
    private readonly EndingService _endingService;
    private readonly ScoreService _scoreService;
    private readonly GameSettings _gameSettings;
    
    private GameState _currentState;
    private readonly Subject<GameState> _onStateChanged = new();
    
    // ゲームの実行状態
    private bool _isRunning;
    // 手札リセット処理中フラグ（連打対策用）
    private bool _isResettingHand;
    // 手札リセットアクションフラグ（カード効果をスキップするため）
    private bool _isHandResetAction;
    // 現在のターン数
    public ReactiveProperty<int> CurrentTurn { get; }
    // プレイヤーの選択したカード
    private CardData _selectedCard;
    // 選択されたカードのインデックス
    private int _selectedCardIndex;
    // 選択されたエリア（撤退演出用）
    private BattleAreaType? _selectedArea;
    
    // 敵のスタン残りターン数
    public ReactiveProperty<int> EnemyStunTurnsLeft { get; }
    // 現在のバトルエリア
    public ReactiveProperty<BattleAreaType> CurrentArea { get; }
    public ReactiveProperty<int> RetreatCount { get; } = new(0);
    // ステート変更の通知
    public Observable<GameState> OnStateChanged => _onStateChanged;
    
    // カード使用時の通知（カードデータとインデックス）
    private readonly Subject<(CardData cardData, int index)> _onCardUsed = new();
    public Observable<(CardData cardData, int index)> OnCardUsed => _onCardUsed;
    
    // エリア選択時の通知
    private readonly Subject<(BattleAreaType area1, BattleAreaType area2)> _onAreaSelectionRequired = new();
    public Observable<(BattleAreaType area1, BattleAreaType area2)> OnAreaSelectionRequired => _onAreaSelectionRequired;
    
    // ランダムイベント発生時の通知
    private readonly Subject<EventData> _onRandomEventOccurred = new();
    public Observable<EventData> OnRandomEventOccurred => _onRandomEventOccurred;
    
    // ゲーム終了時の通知（エンディング番号付き）
    private readonly Subject<int> _onGameEnded = new();
    public Observable<int> OnGameEnded => _onGameEnded;
    
    
    public GameManager(PlayerModel playerModel, CardPoolService cardPoolService, EventPoolService eventPoolService, EndingService endingService, ScoreService scoreService, GameSettings gameSettings)
    {
        _playerModel = playerModel;
        _cardPoolService = cardPoolService;
        _eventPoolService = eventPoolService;
        _endingService = endingService;
        _scoreService = scoreService;
        _gameSettings = gameSettings;
        CurrentTurn = new ReactiveProperty<int>(1);
        EnemyStunTurnsLeft = new ReactiveProperty<int>(0);
        CurrentArea = new ReactiveProperty<BattleAreaType>(BattleAreaType.Market); // 初期エリアは市場
    }
    
    public void Initialize()
    {
        // ゲーム開始時の初期化
        Time.timeScale = 1f;
        _isRunning = true;
        _currentState = GameState.Initialize;
        
        // ゲームループを開始
        RunGameLoop().Forget();
    }
    
    /// <summary>
    /// メインゲームループ
    /// </summary>
    private async UniTaskVoid RunGameLoop()
    {
        while (_isRunning)
        {
            switch (_currentState)
            {
                case GameState.Initialize:
                    await InitializeGame();
                    break;
                    
                case GameState.DrawCard:
                    await DrawCards();
                    break;
                    
                case GameState.PlayerAction:
                    await WaitForPlayerAction();
                    break;
                    
                case GameState.CardEffect:
                    await ProcessCardEffect();
                    break;
                    
                case GameState.EnemyProgress:
                    await ProcessEnemyProgress();
                    break;
                    
                case GameState.RandomEvent:
                    await ProcessRandomEvent();
                    break;
                    
                case GameState.CheckGameEnd:
                    CheckGameEnd();
                    break;
                    
                case GameState.Retreat:
                    await ProcessRetreat();
                    break;
                    
                case GameState.GameEnd:
                    _isRunning = false;
                    ProcessGameEnd();
                    break;
            }
            
            await UniTask.Yield();
        }
    }
    
    /// <summary>
    /// ゲーム初期化
    /// </summary>
    private async UniTask InitializeGame()
    {
        // CRI初期化を待つ
        while(CriAtom.CueSheetsAreLoading)
        {
            await UniTask.Yield();
        }
        
        await UniTask.Delay(500);
        NextState();
    }
    
    /// <summary>
    /// カード配布
    /// </summary>
    private async UniTask DrawCards()
    {
        _cardPoolService.UpdateCondition(
            CurrentTurn.CurrentValue,
            _playerModel.CurrentHp.CurrentValue,
            _playerModel.AvailableCitizens.CurrentValue
        );
        
        // 手札を補充（現在の最大手札数まで）
        while (_playerModel.HandCards.Count < _playerModel.MaxHandSize.Value)
        {
            var randomCard = _cardPoolService.GetRandomCard();
            if (randomCard)
            {
                _playerModel.DrawCard(randomCard);
            }
            else
            {
                Debug.LogWarning("カードプールからカードを取得できませんでした");
                break;
            }
            await UniTask.Delay(200);
        }
        
        NextState();
    }
    
    /// <summary>
    /// プレイヤーの行動を待つ
    /// </summary>
    private async UniTask WaitForPlayerAction()
    {
        _selectedCard = null;
        _selectedCardIndex = -1;
        
        // プレイヤーがカードを選択するまで待機
        await UniTask.WaitUntil(() => _selectedCard);
        
        NextState();
    }
    
    /// <summary>
    /// カード効果処理
    /// </summary>
    private async UniTask ProcessCardEffect()
    {
        // 手札リセットアクションの場合はカード効果をスキップ
        if (_isHandResetAction)
        {
            _isHandResetAction = false; // フラグをリセット
            // 少し遅延してから次の状態へ
            await UniTask.Delay(100);
            NextState();
            return;
        }
        
        // 通常のカード効果処理
        // カード効果を適用
        _selectedCard.ApplyEffect(_playerModel, this);
        
        // 危険なカードの使用を記録
        if (_selectedCard.IsDangerousCard)
        {
            _playerModel.IncrementDangerousCardUsage();
        }
        
        // カード使用イベントを発火（アニメーション用）
        _onCardUsed.OnNext((_selectedCard, _selectedCardIndex));
        
        // 少し遅延してからカードを手札から削除
        await UniTask.Delay(300); // アニメーション時間を考慮
        
        // カードを手札から削除（この時点でアニメーションは既に開始済み）
        _playerModel.RemoveCard(_selectedCard);
        
        await UniTask.Delay(200);
        NextState();
    }
    
    /// <summary>
    /// 敵の進行処理
    /// </summary>
    private async UniTask ProcessEnemyProgress()
    {
        // 敵のスタン状態をチェック
        if (EnemyStunTurnsLeft.Value > 0)
        {
            EnemyStunTurnsLeft.Value--;
        }
        else
        {
            // 毎ターン一定数の市民が死亡
            _playerModel.ProcessTurnDeaths(_gameSettings.TurnDeathCount);
        }
        
        await UniTask.Delay(500);
        NextState();
    }
    
    /// <summary>
    /// ランダムイベント処理
    /// </summary>
    private async UniTask ProcessRandomEvent()
    {
        // 3ターンに1回の確率でイベント発生をチェック（33%の確率）
        if (CurrentTurn.CurrentValue > 1 && CurrentTurn.CurrentValue % 3 == 0)
        {
            var randomEvent = _eventPoolService.GetRandomEvent(CurrentArea.Value);
            // イベント発生の通知
            _onRandomEventOccurred.OnNext(randomEvent);
            // イベント効果を適用
            randomEvent.ApplyEffect(_playerModel, this);
            // イベント処理の時間
            await UniTask.Delay(2000);
        }
        
        NextState();
    }
    
    /// <summary>
    /// ゲーム終了判定
    /// </summary>
    private void CheckGameEnd()
    {
        // 敗北条件: プレイヤーの体力が0
        if (!_playerModel.IsAlive.CurrentValue)
        {
            ChangeState(GameState.GameEnd);
            return;
        }
        
        // クリア条件1: ターン経過
        if (CurrentTurn.CurrentValue >= _gameSettings.MaxTurns)
        {
            ChangeState(GameState.GameEnd);
            return;
        }
        
        // クリア条件2: 残り人口が0（全市民が撤退完了または戦死）
        if (_playerModel.AvailableCitizens.CurrentValue == 0)
        {
            ChangeState(GameState.GameEnd);
            return;
        }
        
        // ゲーム継続 - 次のターンへ
        CurrentTurn.Value++;
        
        // 無効化されたカードのターン数を更新
        _playerModel.UpdateDisabledCards();
        
        // 設定されたターン間隔ごとに撤退演出へ
        if (CurrentTurn.CurrentValue % _gameSettings.RetreatTurnInterval == 0)
        {
            ChangeState(GameState.Retreat);
        }
        else
        {
            ChangeState(GameState.DrawCard);
        }
    }
    
    /// <summary>
    /// ゲーム終了処理（エンディング判定と通知）
    /// </summary>
    private void ProcessGameEnd()
    {
        // エンディング番号を判定し、データを保存
        var endingNumber = _endingService.DetermineAndSaveEnding(_playerModel, CurrentTurn.CurrentValue);
        
        // ゲームクリアかどうかを判定
        // 20ターンに達した場合はゲームオーバー（時間切れ）
        // 20ターン未満で市民全員が撤退した場合のみゲームクリア
        var isGameClear = _playerModel.IsAlive.CurrentValue && 
                         CurrentTurn.CurrentValue < _gameSettings.MaxTurns && 
                         _playerModel.AvailableCitizens.CurrentValue == 0;
        
        // スコア計算と送信（クリア時のみ）
        if (isGameClear)
        {
            // 生存率と経過ターンからスコアを計算
            var survivalRate = _playerModel.SurvivalRate.CurrentValue;
            var score = _scoreService.CalculateScore(survivalRate, CurrentTurn.CurrentValue);
            // unityroomにスコアを送信
            _scoreService.SendScoreToUnityroom(score, 1);
        }
        
        // 回収済みエンディング数を取得
        var endingCount = EndingService.GetCollectedEndingCount();
        // エンディング回収数をunityroomに送信（ボード番号2）
        _scoreService.SendEndingCountToUnityroom(endingCount, 2);
        
        // UIPresenterにゲーム終了を通知（エンディング番号付き）
        _onGameEnded.OnNext(endingNumber);
    }
    
    /// <summary>
    /// ステート変更
    /// </summary>
    private void ChangeState(GameState newState)
    {
        _currentState = newState;
        _onStateChanged.OnNext(_currentState);
    }
    
    /// <summary>
    /// 次のステートへ遷移
    /// </summary>
    private void NextState()
    {
        GameState nextState = _currentState switch
        {
            GameState.Initialize => GameState.DrawCard,
            GameState.DrawCard => GameState.PlayerAction,
            GameState.PlayerAction => GameState.CardEffect,
            GameState.CardEffect => GameState.EnemyProgress,
            GameState.EnemyProgress => GameState.RandomEvent,
            GameState.RandomEvent => GameState.CheckGameEnd,
            GameState.CheckGameEnd => GameState.DrawCard, // CheckGameEnd後も次のターンに進む
            GameState.Retreat => GameState.DrawCard,
            GameState.GameEnd => GameState.GameEnd, // 終了状態は変更しない
            _ => GameState.Initialize
        };
        
        ChangeState(nextState);
    }
    
    /// <summary>
    /// プレイヤーがカードを選択
    /// </summary>
    public void SelectCard(CardData cardData, int cardIndex)
    {
        if (_currentState == GameState.PlayerAction && cardData && _playerModel.HandCards.Contains(cardData))
        {
            // 無効化されているカードは選択できない
            if (_playerModel.IsCardDisabled(cardData))
            {
                Debug.Log("このカードは使用不能です");
                return;
            }
            
            _selectedCard = cardData;
            _selectedCardIndex = cardIndex;
        }
    }
    
    /// <summary>
    /// 撤退演出処理
    /// </summary>
    private async UniTask ProcessRetreat()
    {
        RetreatCount.Value++;
        
        // セリフ表示のためちょっと待機
        await UniTask.Delay(1500);
        // ランダムに2つのエリアを選択
        var availableAreas = GetRandomTwoAreas();
        
        // エリア選択UIを表示するイベントを発火
        _selectedArea = null;
        _onAreaSelectionRequired.OnNext((availableAreas.area1, availableAreas.area2));
        // プレイヤーがエリアを選択するまで待機
        await UniTask.WaitUntil(() => _selectedArea.HasValue);
        
        // 選択されたエリアを適用
        CurrentArea.Value = _selectedArea ?? availableAreas.area1; // デフォルトはarea1
        // 撤退演出の時間
        await UniTask.Delay(1500);
        
        // 次のカード配布へ
        ChangeState(GameState.DrawCard);
    }
    
    /// <summary>
    /// ランダムに2つのエリアを取得
    /// </summary>
    private (BattleAreaType area1, BattleAreaType area2) GetRandomTwoAreas()
    {
        var allAreas = new[] 
        { 
            BattleAreaType.Market, 
            BattleAreaType.Residential, 
            BattleAreaType.BackAlley, 
            BattleAreaType.Cathedral 
        };
        
        // 現在のエリア以外から選択
        var availableAreas = new System.Collections.Generic.List<BattleAreaType>();
        foreach (var area in allAreas)
        {
            if (area != CurrentArea.Value)
            {
                availableAreas.Add(area);
            }
        }
        
        // ランダムに2つ選択
        var random = new System.Random();
        var index1 = random.Next(availableAreas.Count);
        var area1 = availableAreas[index1];
        availableAreas.RemoveAt(index1);
        
        var index2 = random.Next(availableAreas.Count);
        var area2 = availableAreas[index2];
        
        return (area1, area2);
    }
    
    /// <summary>
    /// エリアを選択
    /// </summary>
    public void SelectArea(BattleAreaType area)
    {
        if (_currentState == GameState.Retreat)
        {
            _selectedArea = area;
        }
    }
    
    /// <summary>
    /// 敵をスタンさせる
    /// </summary>
    public void StunEnemy(int turns)
    {
        EnemyStunTurnsLeft.Value += turns;
    }
    
    /// <summary>
    /// 手札をリセットする（全て削除して同じ枚数だけ引き直す）
    /// </summary>
    public async UniTask ResetHandAsync(bool needChangeState)
    {
        if (_currentState is not GameState.PlayerAction and not GameState.RandomEvent) return;
        // 手札リセット中フラグ（連打対策）
        if (_isResettingHand) return;
        
        _isResettingHand = true;
        
        Debug.Log(_currentState);
        try
        {
            var currentHandCount = _playerModel.HandCards.Count;
            
            // 手札リセット開始の通知（UIでアニメーション用）
            var resetStarted = new Subject<Unit>();
            resetStarted.OnNext(Unit.Default);
            
            // 少し待機（削除アニメーション用）
            await UniTask.Delay(300);
            
            // 全ての手札を削除
            _playerModel.HandCards.Clear();
            _playerModel.HandCardsChanged.OnNext(Unit.Default);
            
            // 少し待機
            await UniTask.Delay(200);
            
            // 条件を更新
            _cardPoolService.UpdateCondition(
                CurrentTurn.CurrentValue,
                _playerModel.CurrentHp.CurrentValue,
                _playerModel.AvailableCitizens.CurrentValue
            );
            
            // 同じ枚数だけ新しいカードを引く
            for (var i = 0; i < currentHandCount; i++)
            {
                var randomCard = _cardPoolService.GetRandomCard();
                if (randomCard)
                {
                    _playerModel.DrawCard(randomCard);
                    await UniTask.Delay(200);
                }
            }

            if (needChangeState)
            {
                // リセット処理が完了したら、ダミーカードで状態遷移をトリガー
                // 実際にカードを選択したかのようにフラグを設定
                _selectedCard = _playerModel.HandCards.Count > 0 ? _playerModel.HandCards[0] : null;
                _selectedCardIndex = 0;

                // 通常のゲームループにより次の状態（CardEffect）に遷移する
                // ただし、実際のカード効果は適用しないため特別フラグを設定
                _isHandResetAction = true;
            }
        }
        finally
        {
            _isResettingHand = false;
        }
    }
    
    /// <summary>
    /// 追加でカードをドローする
    /// </summary>
    public async void DrawAdditionalCards(int count)
    {
        var actualDrawn = 0;
        for (var i = 0; i < count; i++)
        {
            var randomCard = _cardPoolService.GetRandomCard();
            if (randomCard)
            {
                _playerModel.DrawCard(randomCard);
                actualDrawn++;
                await UniTask.Delay(200);
            }
        }
        
        // 最大手札数も増やした分だけ増加
        _playerModel.ChangeMaxHandSize(actualDrawn);
    }
    
    /// <summary>
    /// ランダムに手札を削除する
    /// </summary>
    public void RemoveRandomCards(int count)
    {
        var handCards = _playerModel.HandCards;
        var cardsToRemove = Mathf.Min(count, handCards.Count);
        
        for (var i = 0; i < cardsToRemove; i++)
        {
            if (handCards.Count > 0)
            {
                var randomIndex = UnityEngine.Random.Range(0, handCards.Count);
                var cardToRemove = handCards[randomIndex];
                _playerModel.RemoveCard(cardToRemove);
            }
        }
        
        // 最大手札数も削除した分だけ減らす
        _playerModel.ChangeMaxHandSize(-cardsToRemove);
    }
    
    public void Tick()
    {
        if(!Application.isEditor) return;
        
        if (Input.GetKeyDown(KeyCode.Space))
            _playerModel.EvacuateCitizens(10);
            
    }
}