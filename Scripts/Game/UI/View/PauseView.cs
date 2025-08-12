using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseView : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button titleButton;
    
    public void SetResumeButtonListener(UnityEngine.Events.UnityAction action)
    {
        if (resumeButton)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(action);
        }
    }
    
    public void SetTitleButtonListener(UnityEngine.Events.UnityAction action)
    {
        if (titleButton)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(action);
        }
    }
    
    private void OnDestroy()
    {
        // ボタンイベントの購読解除
        resumeButton.onClick.RemoveAllListeners();
        titleButton.onClick.RemoveAllListeners();
    }
}