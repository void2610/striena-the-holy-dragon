using UnityEngine;
using LitMotion;
using Void2610.UnityTemplate;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

/// <summary>
/// LitMotionを使用したカメラ振動システム
/// SingletonMonoBehaviourを継承してアクセスしやすくする
/// </summary>
public class CameraShake : SingletonMonoBehaviour<CameraShake>
{
    private Vector3 _originalPosition;
    private MotionHandle _shakeHandle;
    private CancellationTokenSource _cancellationTokenSource;
    
    protected override void Awake()
    {
        base.Awake();
        // DontDestroyOnLoadを解除
        SceneManager.MoveGameObjectToScene(this.gameObject, SceneManager.GetActiveScene());

        _originalPosition = this.transform.position;
    }

    public void ShakeCamera(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f) => 
        ShakeCameraAsync(magnitude, duration, frequency, dampingRatio).Forget();

    /// <summary>
    /// カメラを一定時間揺らし、完了後に await できる
    /// </summary>
    public async UniTask ShakeCameraAsync(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f)
    {
        if(_shakeHandle.IsPlaying()) _shakeHandle.Complete(); // 前の揺れを停止

        // 揺れMotionの開始
        _shakeHandle = LMotion.Shake.Create(startValue: Vector3.zero, strength: Vector3.one * magnitude, duration: duration)
            .WithFrequency(frequency)
            .WithDampingRatio(dampingRatio)
            .Bind(v => this.transform.position = _originalPosition + v)
            .AddTo(this);

        await _shakeHandle.ToUniTask();

        this.transform.position = _originalPosition; // 揺れ後に元の位置に戻す
    }
}