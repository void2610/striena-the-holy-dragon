using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Void2610.UnityTemplate
{
    /// <summary>
    /// Unity開発に便利な拡張メソッドのコレクション
    /// </summary>
    public static class ExtendedMethods
    {
        /// <summary>
        /// リストの全要素をコンソールに出力
        /// </summary>
        public static void Print<T>(this List<T> list)
        {
            if (list == null)
            {
                Debug.Log("リストがnullです");
                return;
            }

            Debug.Log($"リストの内容 ({list.Count}個の要素):");
            for (int i = 0; i < list.Count; i++)
            {
                Debug.Log($"[{i}] {list[i]}");
            }
        }
        
        /// <summary>
        /// enum値を次の値に順番に切り替える
        /// 最後の値の後は最初の値に戻る
        /// </summary>
        public static T Toggle<T>(this T current) where T : System.Enum
        {
            var values = (T[])System.Enum.GetValues(current.GetType());
            var index = System.Array.IndexOf(values, current);
            var nextIndex = (index + 1) % values.Length;
            return values[nextIndex];
        }

        /// <summary>
        /// 重み付きリストから確率に基づいてランダムなアイテムを選択
        /// </summary>
        public static T ChooseByWeight<T>(this List<T> list, Func<T, float> weightSelector)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("リストが空またはnullです");

            float totalWeight = 0f;
            foreach (var item in list)
            {
                totalWeight += weightSelector(item);
            }

            if (totalWeight <= 0f)
                throw new ArgumentException("合計重みは0より大きい必要があります");

            var randomValue = UnityEngine.Random.Range(0f, totalWeight);
            var cumulativeWeight = 0f;

            foreach (var item in list)
            {
                cumulativeWeight += weightSelector(item);
                if (randomValue <= cumulativeWeight)
                    return item;
            }

            // フォールバック（浮動小数点精度の問題対策）
            return list[list.Count - 1];
        }

        /// <summary>
        /// UI Selectableのリストにナビゲーションを設定
        /// </summary>
        public static void SetNavigation(this List<Selectable> selectables, bool isHorizontal = true, bool wrapAround = false)
        {
            if (selectables == null || selectables.Count == 0)
                return;

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (selectable == null) continue;

                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.Explicit;

                if (isHorizontal)
                {
                    // 水平ナビゲーション
                    if (i > 0)
                        navigation.selectOnLeft = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnLeft = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnRight = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnRight = selectables[0];
                }
                else
                {
                    // 垂直ナビゲーション
                    if (i > 0)
                        navigation.selectOnUp = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnUp = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnDown = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnDown = selectables[0];
                }

                selectable.navigation = navigation;
            }
        }

        /// <summary>
        /// GameObjectを安全に破棄（プレイモードとエディットモード両方に対応）
        /// </summary>
        public static void SafeDestroy(this GameObject gameObject)
        {
            if (gameObject == null) return;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// GameObjectからコンポーネントを取得または追加
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Graphic（UI要素）のアルファ値を設定
        /// </summary>
        public static void SetAlpha(this Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            
            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        /// <summary>
        /// GameObjectとその全ての子オブジェクトのレイヤーを設定
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null) return;

            gameObject.layer = layer;
            
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 名前で子オブジェクトを再帰的に検索
        /// </summary>
        public static Transform FindChildRecursive(this Transform parent, string name)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                var result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Transformをデフォルト値にリセット
        /// </summary>
        public static void ResetTransform(this Transform transform)
        {
            if (transform == null) return;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 指定したScriptableObjectと同じディレクトリ内の指定した型のアセットを一括登録
        /// </summary>
        /// <typeparam name="T">登録するScriptableObjectの型</typeparam>
        /// <param name="targetObject">検索基準となるScriptableObject</param>
        /// <param name="targetList">登録先のリスト</param>
        /// <param name="sortKeySelector">ソート用のキーセレクタ（nullの場合はソートしない）</param>
        public static void RegisterAssetsInSameDirectory<T>(
            this ScriptableObject targetObject, 
            List<T> targetList, 
            System.Func<T, string> sortKeySelector = null) where T : ScriptableObject
        {
            // このScriptableObjectと同じディレクトリパスを取得
            var path = AssetDatabase.GetAssetPath(targetObject);
            path = System.IO.Path.GetDirectoryName(path);
            
            // 指定ディレクトリ内の指定した型のアセットを検索
            var typeName = typeof(T).Name;
            var guids = AssetDatabase.FindAssets($"t:{typeName}", new[] { path });
            
            // 検索結果をリストに追加
            targetList.Clear();
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset) targetList.Add(asset);
            }
            
            // ソートが指定されている場合はソート実行
            if (sortKeySelector != null)
            {
                targetList.Sort((a, b) => string.Compare(sortKeySelector(a), sortKeySelector(b)));
            }
            
            // ScriptableObjectを更新
            EditorUtility.SetDirty(targetObject);
        }
#endif
    }
}