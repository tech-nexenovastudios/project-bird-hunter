using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Gameplay.PowerUps;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Gameplay.Slot
{
    public class ReelController : MonoBehaviour
    {
        // ── Layout / animation constants ────────────────────────────────────
        private const int   SymbolBufferCount = 20;
        private const float ContentTopPadding = 100f;
        private const float BounceScale       = 1.1f;
        private const float BounceDuration    = 0.15f;
        private const int   BounceLoops       = 2;

        // Visible tail indices, counted from the end of the symbol strip.
        private const int IdxResult = 1; // _symbols[^1]
        private const int IdxMid    = 2; // _symbols[^2]
        private const int IdxTop    = 3; // _symbols[^3]

        [Header("Settings")]
        public float symbolSize = 160f;
        public float spinDuration = 1.2f;
        public List<PowerupConfig> symbolList = new();

        [Header("Assign These")]
        public RectTransform content;

        // Subscribed by SlotMachineController; raised when player taps the result symbol.
        public Action<PowerupConfig> OnPowerupSelected;

        private readonly SymbolView[] _symbols = new SymbolView[SymbolBufferCount];
        private Tween _spinTween;
        private TextMeshProUGUI _powerNameText;
        private TextMeshProUGUI _descriptionText;

        [ContextMenu("🔧 Initialize Reel")]
        public void InitializeReel()
        {
            if (!ResolveContent()) return;
            if (symbolList == null || symbolList.Count == 0)
            {
                Debug.LogError($"[Reel:{name}] symbolList is empty.");
                return;
            }
            if (SymbolPool.Instance == null)
            {
                Debug.LogError($"[Reel:{name}] SymbolPool.Instance is null. Ensure SymbolPool exists in the scene.");
                return;
            }

            ConfigureMaskAndContent();
            ClearExistingSymbols();
            CreateSymbols();
            CacheLabels();
            HideInfoTexts();

            // Symbols at indices 0 and 2 stay hidden — only the central window plus the
            // animated tail (last three) are visible during a spin.
            if (_symbols[0] != null) _symbols[0].gameObject.SetActive(false);
            if (_symbols[2] != null) _symbols[2].gameObject.SetActive(false);
        }

        public void SetHighlight(bool highlighted)
        {
            var resultSymbol = _symbols[^IdxResult];
            if (resultSymbol == null || resultSymbol.iconImage == null) return;
            resultSymbol.iconImage.color = highlighted ? Color.yellow : Color.white;
        }

        public void SpinToResult(PowerupConfig result)
        {
            if (result == null || result.icon == null)
            {
                Debug.LogError($"[Reel:{name}] SpinToResult called with null PowerupConfig or icon.");
                return;
            }

            _spinTween?.Kill();
            HideInfoTexts();
            SetHighlight(false);

            // Reset reel position and re-enable the tail symbols so a second spin animates
            // correctly — OnSpinComplete deactivates the tail at the end of the previous spin.
            content.anchoredPosition = Vector2.zero;
            _symbols[^IdxTop].gameObject.SetActive(true);
            _symbols[^IdxMid].gameObject.SetActive(true);

            // Fill the visible tail with random filler then the actual result at the bottom.
            var top = symbolList[Random.Range(0, symbolList.Count)];
            var mid = symbolList[Random.Range(0, symbolList.Count)];
            if (top?.icon != null) _symbols[^IdxTop].SetIcon(top.icon, "tail-top");
            if (mid?.icon != null) _symbols[^IdxMid].SetIcon(mid.icon, "tail-mid");
            _symbols[^IdxResult].SetIcon(result.icon, "result");

            float stopY = symbolSize * (_symbols.Length - 2);

            _spinTween = content.DOAnchorPosY(stopY, spinDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => OnSpinComplete(result));
        }

        // ─── Initialization helpers ─────────────────────────────────────────

        private bool ResolveContent()
        {
            if (content != null) return true;
            content = transform.Find("Content") as RectTransform;
            if (content != null) return true;
            Debug.LogError($"[Reel:{name}] Missing 'Content' child RectTransform.");
            return false;
        }

        private void ConfigureMaskAndContent()
        {
            if (GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();
            ((RectTransform)transform).sizeDelta = new Vector2(symbolSize, 2 * symbolSize);

            content.sizeDelta = new Vector2(symbolSize, 3 * symbolSize);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
        }

        private void ClearExistingSymbols()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                DestroyImmediate(content.GetChild(i).gameObject);
        }

        private void CreateSymbols()
        {
            for (int i = 0; i < SymbolBufferCount; i++)
                _symbols[i] = CreatePerfectSymbol(i);
        }

        private SymbolView CreatePerfectSymbol(int row)
        {
            var go = SymbolPool.Instance.Get().gameObject;
            go.transform.SetParent(content, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(symbolSize, symbolSize);
            rt.pivot = Vector2.one * 0.5f;

            float halfHeight = 1.5f * symbolSize - ContentTopPadding;
            rt.anchoredPosition = new Vector2(0, halfHeight - (row + 0.5f) * symbolSize);

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var symView = go.GetComponent<SymbolView>();
            symView.iconImage = image;

            var randomConfig = symbolList[Random.Range(0, symbolList.Count)];
            if (randomConfig?.icon != null)
                symView.SetIcon(randomConfig.icon, randomConfig.id);
            return symView;
        }

        private void CacheLabels()
        {
            _powerNameText   = FindTextChild("PowerNameText");
            _descriptionText = FindTextChild("DescriptionText");
        }

        private TextMeshProUGUI FindTextChild(string childName)
        {
            var found = GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true)
                            .FirstOrDefault(t => t.name == childName);
            if (found == null) Debug.LogWarning($"[Reel:{name}] '{childName}' not found in children.");
            return found;
        }

        private void HideInfoTexts()
        {
            if (_powerNameText)   _powerNameText.gameObject.SetActive(false);
            if (_descriptionText) _descriptionText.gameObject.SetActive(false);
        }

        // ─── Spin completion ────────────────────────────────────────────────

        private void OnSpinComplete(PowerupConfig result)
        {
            _symbols[^IdxTop].gameObject.SetActive(false);
            _symbols[^IdxMid].gameObject.SetActive(false);

            if (_powerNameText)
            {
                _powerNameText.gameObject.SetActive(true);
                _powerNameText.text = result.displayName;
            }
            if (_descriptionText)
            {
                _descriptionText.gameObject.SetActive(true);
                _descriptionText.text = result.description;
            }

            BounceAndArmClick(result);
        }

        private void BounceAndArmClick(PowerupConfig result)
        {
            var resultSymbol = _symbols[^IdxResult];
            resultSymbol.transform
                .DOScale(BounceScale, BounceDuration)
                .SetLoops(BounceLoops, LoopType.Yoyo)
                .OnStart(() => resultSymbol.button.image.raycastTarget = true)
                .OnComplete(() =>
                {
                    resultSymbol.button.onClick.RemoveAllListeners();
                    resultSymbol.AddListener(() =>
                    {
                        OnPowerupSelected?.Invoke(result);
                        _spinTween?.Kill();
                    });
                });
        }

        private void OnDestroy()
        {
            _spinTween?.Kill();
            for (int i = 0; i < _symbols.Length; i++)
                if (_symbols[i] != null) SymbolPool.Instance?.Release(_symbols[i]);
        }
    }
}
