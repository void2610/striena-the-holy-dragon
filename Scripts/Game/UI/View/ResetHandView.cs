using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手札リセットボタンを管理するView
/// </summary>
[RequireComponent(typeof(Button))]
public class ResetHandView : MonoBehaviour
{
    private Button _resetButton;
    
    // リセットボタンクリック時のイベント
    public event Action OnResetButtonClicked;
    
    private void Awake()
    {
        _resetButton = this.GetComponent<Button>();
        _resetButton.onClick.AddListener(() => OnResetButtonClicked?.Invoke());
    }
    
    /// <summary>
    /// リセットボタンの有効/無効を設定
    /// </summary>
    /// <param name="isEnabled">有効にするかどうか</param>
    public void SetResetButtonEnabled(bool isEnabled)
    {
        _resetButton.interactable = isEnabled;
    }
    
    /// <summary>
    /// リセットボタンの表示/非表示を設定
    /// </summary>
    /// <param name="isVisible">表示するかどうか</param>
    public void SetResetButtonVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
    
    private void OnDestroy()
    {
        _resetButton.onClick.RemoveAllListeners();
    }
}