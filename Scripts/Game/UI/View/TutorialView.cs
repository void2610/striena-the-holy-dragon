using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialView : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    
    public void SetCloseButtonListener(UnityEngine.Events.UnityAction action)
    {
        closeButton.onClick.AddListener(action);
    }
    
    private void OnDestroy()
    {
        // ボタンイベントの購読解除
        closeButton.onClick.RemoveAllListeners();
    }
}