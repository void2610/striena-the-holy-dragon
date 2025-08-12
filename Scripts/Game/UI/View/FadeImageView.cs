using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;

[RequireComponent(typeof(Image))]
public class FadeImageView : MonoBehaviour
{
    private Image _image;
    
    public async UniTask FadeIn(float duration = 0.5f)
    {
        var handle = LMotion.Create(0f, 1f, duration)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToColorA(_image)
            .AddTo(this);
        await handle.ToUniTask();
    }
    
    public async UniTask FadeOut(float duration = 0.5f)
    {
        var handle = LMotion.Create(1f, 0f, duration)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToColorA(_image)
            .AddTo(this);
        await handle.ToUniTask();
    }

    private void Awake()
    {
        _image = this.GetComponent<Image>();
        _image.color = new Color(0f, 0f, 0f, 1f); // 初期状態は完全に黒
    }
}
