using System;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Managers;
using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Gameplay.Slot
{
    public class ReelController : MonoBehaviour
    {
        [Header("Settings")] public float symbolSize = 160f;
        public float spinDuration = 1.2f; // ← ADDED THIS!
        public List<PowerupConfig> symbolList = new();

        [Header("Assign These")] public RectTransform content;

        private SymbolView[] symbols = new SymbolView[20];
        private Tween spinTween;
        
        public Action<PowerupConfig> OnPowerupSelected;

        [ContextMenu("🔧 Initialize Reel")]
        public void InitializeReel()
        {
            // 1. Validate
            if (content == null)
            {
                content = transform.Find("Content") as RectTransform;
                if (content == null)
                {
                    Debug.LogError("No Content found! Create Empty child named 'Content'");
                    return;
                }
            }

            if (symbolList.Count == 0)
            {
                Debug.LogError("Assign symbolSprites!");
                return;
            }

            // 2. Setup MASK viewport (2 symbols tall)
            var mask = GetComponent<RectMask2D>();
            if (mask == null) gameObject.AddComponent<RectMask2D>();
            ((RectTransform)transform).sizeDelta = new Vector2(symbolSize, 2 * symbolSize);

            // 3. Setup CONTENT (3 symbols tall)
            content.sizeDelta = new Vector2(symbolSize, 3 * symbolSize);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;

            // 4. DESTROY old symbols
            for (int i = content.childCount - 1; i >= 0; i--)
                DestroyImmediate(content.GetChild(i).gameObject);

            // 5. CREATE 3 PERFECT SYMBOLS
            for (int i = 0; i < 20; i++)
            {
                symbols[i] = CreatePerfectSymbol(i);
            }

            Debug.Log($"✅ Reel initialized: {symbols.Length} symbols at symbolSize={symbolSize}");
        }

        private SymbolView CreatePerfectSymbol(int row)
        {
            // Create GameObject
            var go = SymbolPool.Instance.Get().gameObject;
            go.transform.SetParent(content, false);

            // RectTransform
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(symbolSize, symbolSize);
            rt.pivot = Vector2.one * 0.5f; // CENTER

            // POSITION: row0=center-top, row1=center-middle, row2=center-bottom
            float halfHeight = 1.5f * symbolSize; // Half of 3 symbols
            rt.anchoredPosition = new Vector2(0, halfHeight - (row + 0.5f) * symbolSize);

            // Image + SymbolView
            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var symView = go.GetComponent<SymbolView>();
            symView.iconImage = image; // Direct reference!

            var randomConfig = symbolList[Random.Range(0, symbolList.Count)];
            if (randomConfig == null || randomConfig.icon == null)
            {
                Debug.LogWarning($"[ReelController] PowerupConfig at index has null icon — skipping SetIcon");
                return symView;
            }
            symView.SetIcon(randomConfig.icon, randomConfig.id);
            return symView;
        }
        public void SpinToResult(PowerupConfig results)
        {
            if (results == null || results.icon == null)
            {
                Debug.LogError("[ReelController] SpinToResult called with null PowerupConfig or icon!");
                return;
            }
            spinTween?.Kill();

            var top    = symbolList[Random.Range(0, symbolList.Count)];
            var bottom = symbolList[Random.Range(0, symbolList.Count)];

            if (top?.icon != null)    symbols[1].SetIcon(top.icon,     "top");
            if (results?.icon != null) symbols[2].SetIcon(results.icon, "middle");
            if (bottom?.icon != null) symbols[0].SetIcon(bottom.icon,  "bottom");

            // Animate: overshoot up → settle
            float overshootY = symbolSize * 20;
            content.DOAnchorPosY(0, spinDuration)
                .From(new Vector2(0, overshootY))
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    // Hide bottom row
                    symbols[0].gameObject.SetActive(false);

                    // Win pop
                    symbols[1].transform.DOScale(1.1f, 0.15f).SetLoops(2, LoopType.Yoyo).OnComplete(() =>
                    {
                        symbols[1].button.onClick.RemoveAllListeners();
                        symbols[1].AddListener(() =>
                        {
                            Debug.Log("Powerup Clicked!");
                            OnPowerupSelected?.Invoke(results);
                        });
                    });
                    symbols[2].gameObject.SetActive(false);
                });
        }
        void OnDestroy()
        {
            spinTween?.Kill();
            // Return to pool
            foreach (var sym in symbols)
                if (sym != null) SymbolPool.Instance.Release(sym);
        }
    }
}