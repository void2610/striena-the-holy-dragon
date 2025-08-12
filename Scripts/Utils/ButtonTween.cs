using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;
using System.Collections.Generic;

public class ButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Settings")]
    [SerializeField]
    private bool tweenByPointer = true;
    [SerializeField]
    private bool tweenByClick = true;

    [Header("Tween Settings")]
    [SerializeField]
    private float scale = 1.1f;
    [SerializeField]
    private float duration = 0.5f;

    [Header("Raycast Settings")]
    [SerializeField]
    private GraphicRaycaster raycaster;
    [SerializeField]
    private EventSystem eventSystem;

    private float _defaultScale = 1.0f;
    private readonly List<MotionHandle> _motionHandles = new();

    private void OnClick()
    {
        // 既存のモーションをキャンセル
        CancelAllMotions();
        
        // スケールアニメーション
        var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale * scale, duration)
            .WithEase(Ease.OutElastic)
            .BindToLocalScale(transform);
        _motionHandles.Add(handle);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tweenByPointer && this.GetComponent<Button>()?.interactable == true)
        {
            // 既存のモーションをキャンセル
            CancelAllMotions();
            
            // スケールアップアニメーション
            var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale * scale, duration)
                .WithEase(Ease.OutElastic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(transform);
            _motionHandles.Add(handle);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tweenByPointer && this.GetComponent<Button>()?.interactable == true)
        {
            // 既存のモーションをキャンセル
            CancelAllMotions();
            
            // スケールダウンアニメーション
            var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale, duration)
                .WithEase(Ease.OutElastic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(transform);
            _motionHandles.Add(handle);
        }
    }

    public void ResetScale()
    {
        // 既存のモーションをキャンセル
        CancelAllMotions();
        
        // デフォルトスケールに戻すアニメーション
        var handle = LMotion.Create(transform.localScale, Vector3.one * _defaultScale, duration)
            .WithEase(Ease.OutElastic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .BindToLocalScale(transform);
        _motionHandles.Add(handle);
    }

    private void CancelAllMotions()
    {
        foreach (var handle in _motionHandles)
        {
            if (handle.IsActive())
            {
                handle.Cancel();
            }
        }
        _motionHandles.Clear();
    }

    private void Awake()
    {
        _defaultScale = this.transform.localScale.x;
        if (!tweenByClick) return;
        
        if (this.GetComponent<Button>() != null)
        {
            this.GetComponent<Button>().onClick.AddListener(OnClick);
        }
    }

    private void Start()
    {
        _defaultScale = this.transform.localScale.x;
    }

    private void OnDestroy()
    {
        CancelAllMotions();
    }
}