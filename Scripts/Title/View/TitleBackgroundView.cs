using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の背景画像を管理するViewクラス
/// MVPパターンに従い、表示のみを担当する
/// </summary>
public class TitleBackgroundView : MonoBehaviour
{
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite trueEndBackgroundSprite;
    [SerializeField] private Image backgroundImage;
    
    /// <summary>
    /// 背景画像を設定する（Presenterから呼び出される）
    /// </summary>
    /// <param name="hasTrueEnding">Trueエンドを回収済みかどうか</param>
    public void SetBackground(bool hasTrueEnding)
    {
        // 適切な背景画像を設定
        backgroundImage.sprite = hasTrueEnding ? trueEndBackgroundSprite : normalBackgroundSprite;
    }
}