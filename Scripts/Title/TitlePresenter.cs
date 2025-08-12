using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VContainer;

public class TitlePresenter : MonoBehaviour
{
    [SerializeField] private MainCreditData mainCreditData;
    [SerializeField] private TextMeshProUGUI creditText;
    [SerializeField] private TextMeshProUGUI licenseText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button creditButton;
    [SerializeField] private Button licenseButton;
    [SerializeField] private Button closeCreditButton;
    [SerializeField] private Button closeLicenseButton;
    [SerializeField] private StoryTextView storyTextView;
    
    private CreditService _creditService;
    private LicenseService _licenseService;
    private PrologueData _prologueData;
    
    private MainCreditView _mainCreditView;
    private TitleBackgroundView _titleBackgroundView;
    private FadeImageView _fadeImageView;
    private CanvasGroupSwitcher _canvasGroupSwitcher;
    
    [Inject]
    public void Construct(CreditService creditService, LicenseService licenseService, PrologueData prologueData)
    {
        _creditService = creditService;
        _licenseService = licenseService;
        _prologueData = prologueData;
    }
    
    
    private void UpdateContentSize(TextMeshProUGUI text)
    {
        var preferredHeight = text.GetPreferredValues().y;
        var content = text.GetComponent<RectTransform>();
        var parentRect = content.parent.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(content.sizeDelta.x, preferredHeight);
        parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, preferredHeight);
    }

    private async UniTask OnClickStartButton()
    {
        // BGM1が流れていない場合は流す
        if (!CriBgmController.Instance.IsPlayingBGM1)
            CriBgmController.Instance.PlayBGM1();

        await storyTextView.StartStory(_prologueData.StoryTexts);
        await _fadeImageView.FadeIn();
        await UniTask.Delay(500); // フェードイン後に少し待つ
        SceneManager.LoadScene("MainScene");
    }

    private void Awake()
    {
        _mainCreditView = FindAnyObjectByType<MainCreditView>();
        _titleBackgroundView = FindAnyObjectByType<TitleBackgroundView>();
        _fadeImageView = FindAnyObjectByType<FadeImageView>();
        
        // CanvasGroupSwitcherを初期化
        var canvasGroups = new List<CanvasGroup>(FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None));
        _canvasGroupSwitcher = new CanvasGroupSwitcher(canvasGroups);
        
        startButton.onClick.AddListener(() => OnClickStartButton().Forget());
        creditButton.onClick.AddListener(() => _canvasGroupSwitcher.EnableCanvasGroup("Credit", true));
        licenseButton.onClick.AddListener(() => _canvasGroupSwitcher.EnableCanvasGroup("License", true));
        closeCreditButton.onClick.AddListener(() => _canvasGroupSwitcher.EnableCanvasGroup("Credit", false));
        closeLicenseButton.onClick.AddListener(() => _canvasGroupSwitcher.EnableCanvasGroup("License", false));
    }
    
    public void Start()
    {
        Time.timeScale = 1f;
        // True エンド（No.2）を回収しているかチェック
        var hasTrueEnding = EndingService.IsEndingCollected(2);
        // 背景画像を初期化
        _titleBackgroundView.SetBackground(hasTrueEnding);
        // BGMを流す（すでに再生中の場合は再生しない）
        if (hasTrueEnding)
        {
            // TrueエンドBGMが再生されていない場合のみ再生
            if (!CriBgmController.Instance.HasCurrentPlayback)
            {
                CriBgmController.Instance.PlayEndBGM();
            }
        }
        else
        {
            // BGM1が再生されていない場合のみ再生
            if (!CriBgmController.Instance.HasCurrentPlayback || !CriBgmController.Instance.IsPlayingBGM1)
            {
                CriBgmController.Instance.PlayBGM1();
            }
        }
        
        creditText.text = _creditService.GetCreditText();
        licenseText.text = _licenseService.GetLicenseText();
        UpdateContentSize(licenseText);
        _mainCreditView.Initialize(mainCreditData);
        
        _fadeImageView.FadeOut().Forget();
    }
}
