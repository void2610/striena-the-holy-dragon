using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Void2610.UnityTemplate;

/// <summary>
/// プレイヤーの体力を表示するView
/// </summary>
[RequireComponent(typeof(Button))]
public class PauseButtonView : MonoBehaviour
{
    private Button _pauseButton;
    
    public void SetPauseButtonListener(UnityEngine.Events.UnityAction action)
    {
        if (_pauseButton)
        {
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(action);
            _pauseButton.onClick.AddListener(() => SeManager.Instance.PlaySe("ButtonClick"));
        }
    }
    
    private void Awake()
    {
        _pauseButton = this.GetComponent<Button>();
    }
    
    private void OnDestroy()
    {
        if (_pauseButton)
        {
            _pauseButton.onClick.RemoveAllListeners();
        }
    }
}