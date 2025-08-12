using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

/// <summary>
/// CanvasGroupの表示/非表示を管理するクラス（LitMotionフェード付き）
/// </summary>
public class CanvasGroupSwitcher
{
    private readonly List<CanvasGroup> _canvasGroups;
    private readonly Dictionary<CanvasGroup, MotionHandle> _activeMotions = new();
    
    public CanvasGroupSwitcher(List<CanvasGroup> canvasGroups, string initialVisibleCanvas = null)
    {
        _canvasGroups = canvasGroups;
        
        foreach (var cg in _canvasGroups)
        {
            if (cg.name == initialVisibleCanvas)
            {
                // 初期表示するキャンバスは即座に表示状態に設定
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                EnableCanvasGroupAsync(cg.name, false).Forget();
            }
        }
    }
    
    /// <summary>
    /// 現在表示されているCanvasGroupのGameObjectを取得
    /// </summary>
    public GameObject GetTopCanvasGroup() => _canvasGroups.Find(c => c.alpha > 0)?.gameObject;
    
    /// <summary>
    /// CanvasGroupの表示状態を切り替え（同期）
    /// </summary>
    public void EnableCanvasGroup(string canvasName, bool enable) => EnableCanvasGroupAsync(canvasName, enable).Forget();
    
    /// <summary>
    /// CanvasGroupの表示状態を切り替え（非同期）
    /// </summary>
    public async UniTask EnableCanvasGroupAsync(string canvasName, bool enable)
    {
        var cg = _canvasGroups.Find(c => c.name == canvasName);
        if (!cg) return;
        
        // 進行中のアニメーションをキャンセル
        if (_activeMotions.TryGetValue(cg, out var existingMotion) && existingMotion.IsActive())
        {
            existingMotion.Cancel();
        }
        
        if (enable)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
            
            var motion = LMotion.Create(cg.alpha, 1f, 0.15f)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(alpha => cg.alpha = alpha);
                
            _activeMotions[cg] = motion;
            await motion.ToUniTask();
        }
        else
        {
            var motion = LMotion.Create(cg.alpha, 0f, 0.15f)
                .WithEase(Ease.OutQuad)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(alpha => cg.alpha = alpha);
                
            _activeMotions[cg] = motion;
            await motion.ToUniTask();
            
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        
        // アニメーション完了後、辞書から削除
        _activeMotions.Remove(cg);
    }
    
    /// <summary>
    /// 指定したCanvasGroup以外をすべて非表示にする
    /// </summary>
    public void ShowOnly(string canvasName)
    {
        foreach (var cg in _canvasGroups)
        {
            EnableCanvasGroup(cg.name, cg.name == canvasName);
        }
    }
    
    /// <summary>
    /// すべてのCanvasGroupを非表示にする
    /// </summary>
    public void HideAll()
    {
        foreach (var cg in _canvasGroups)
        {
            EnableCanvasGroup(cg.name, false);
        }
    }
    
    /// <summary>
    /// すべてのアニメーションを停止し、リソースをクリーンアップする
    /// </summary>
    public void Dispose()
    {
        foreach (var kvp in _activeMotions)
        {
            if (kvp.Value.IsActive())
            {
                kvp.Value.Cancel();
            }
        }
        _activeMotions.Clear();
    }
}