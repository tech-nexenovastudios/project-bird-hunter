using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Gameplay.Slot
{
    public class ReelController : MonoBehaviour
    {
        [Header("Settings")] public float symbolSize = 160f;
        public float spinDuration = 1.2f;
        public List<PowerupConfig> symbolList = new();

        [Header("Assign These")] public RectTransform content;

        private SymbolView[] symbols = new SymbolView[20];
        private Tween spinTween;

        // ── TMP Text References ──────────────────────────────────────────────
        private TextMeshProUGUI _powerNameText;
        private TextMeshProUGUI _descriptionText;

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

            // 5. CREATE 20 SYMBOLS
            for (int i = 0; i < 20; i++)
            {
                symbols[i] = CreatePerfectSymbol(i);
            }

            // 6. Find & hide TMP texts (direct children of this reel GameObject)
            _powerNameText = FindTextChild("PowerNameText");
            _descriptionText = FindTextChild("DescriptionText");
            HideInfoTexts();
            
            symbols[2].gameObject.SetActive(false);
            symbols[0].gameObject.SetActive(false);

            Debug.Log($"✅ Reel initialized: {symbols.Length} symbols at symbolSize={symbolSize}");
        }

        // ── Helper: find a TMP child by name anywhere in this reel's hierarchy ──
        private TextMeshProUGUI FindTextChild(string childName)
        {
            var all = GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
            var found = all.FirstOrDefault(t => t.name == childName);
            if (found == null)
                Debug.LogWarning($"[ReelController] '{childName}' not found in children of {gameObject.name}");
            return found;
        }

        private void HideInfoTexts()
        {
            if (_powerNameText) _powerNameText.gameObject.SetActive(false);
            if (_descriptionText) _descriptionText.gameObject.SetActive(false);
        }

        // ── Public: yellow highlight on the result symbol, white to reset ────
        public void SetHighlight(bool highlighted)
        {
            if (symbols == null || symbols.Length == 0) return;
            var resultSymbol = symbols[^1];
            if (resultSymbol == null || resultSymbol.iconImage == null) return;
            resultSymbol.iconImage.color = highlighted ? Color.yellow : Color.white;
        }

        private SymbolView CreatePerfectSymbol(int row)
        {
            var go = SymbolPool.Instance.Get().gameObject;
            go.transform.SetParent(content, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(symbolSize, symbolSize);
            rt.pivot = Vector2.one * 0.5f;

            float halfHeight = 1.5f * symbolSize - 100f;
            rt.anchoredPosition = new Vector2(0, halfHeight - (row + 0.5f) * symbolSize);

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var symView = go.GetComponent<SymbolView>();
            symView.iconImage = image;

            var randomConfig = symbolList[Random.Range(0, symbolList.Count)];
            if (randomConfig == null || randomConfig.icon == null)
            {
                Debug.LogWarning($"[ReelController] PowerupConfig has null icon — skipping SetIcon");
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

            // Hide texts & reset highlight at spin start
            HideInfoTexts();
            SetHighlight(false);

            var top = symbolList[Random.Range(0, symbolList.Count)];
            var bottom = symbolList[Random.Range(0, symbolList.Count)];

            if (top?.icon != null) symbols[^3].SetIcon(top.icon, "last-mid-top");
            if (results?.icon != null) symbols[^2].SetIcon(bottom.icon, "last-mid");
            if (bottom?.icon != null) symbols[^1].SetIcon(results.icon, "last");

            float stopY = symbolSize * (symbols.Count() - 2);

            spinTween = content.DOAnchorPosY(stopY, spinDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    symbols[^3].gameObject.SetActive(false);
                    symbols[^2].gameObject.SetActive(false);

                    // Show name & description
                    if (_powerNameText)
                    {
                        _powerNameText.gameObject.SetActive(true);
                        _powerNameText.text = results.displayName;   // ← verify field name on PowerupConfig
                    }
                    if (_descriptionText)
                    {
                        _descriptionText.gameObject.SetActive(true);
                        _descriptionText.text = results.description; // ← verify field name on PowerupConfig
                    }

                    // Bounce then register click
                    symbols[^1].transform.DOScale(1.1f, 0.15f).SetLoops(2, LoopType.Yoyo).OnStart(() =>
                        {
                            symbols[^1].button.image.raycastTarget = true;
                        })
                        .OnComplete(() =>
                        {
                            symbols[^1].button.onClick.RemoveAllListeners();
                            symbols[^1].AddListener(() =>
                            {
                                Debug.Log("Powerup Clicked!");
                                // Notify SlotMachineController for highlight management
                                OnPowerupSelected?.Invoke(results);
                                // Commit the choice
                                GameEvents.FirePowerupCommitted(results);
                                spinTween?.Kill();
                            });
                        });
                });
        }

        void OnDestroy()
        {
            spinTween?.Kill();
            foreach (var sym in symbols)
                if (sym != null) SymbolPool.Instance.Release(sym);
        }
    }
}