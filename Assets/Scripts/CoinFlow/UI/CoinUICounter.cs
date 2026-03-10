using System.Collections;
using Gameplay.Events;
using TMPro;
using UnityEngine;

// ───────────────────────────────────────────────────────────
// PURPOSE: Displays current coin count. Increments smoothly
//          as each coin icon arrives at the counter.
//
// SETUP: Attach to a GameObject that has a TextMeshProUGUI.
//        The text auto-updates — no other wiring needed.
// ───────────────────────────────────────────────────────────

[RequireComponent(typeof(TextMeshProUGUI))]
public class CoinUICounter : MonoBehaviour
{
    [Tooltip("How fast the displayed number counts up to the real value")]
    [SerializeField] private float countSpeed = 200f;

    [Tooltip("Punch scale when coin arrives (1.0 = no punch)")]
    [SerializeField] private float punchScale = 1.2f;

    [Tooltip("How fast the punch scale returns to normal")]
    [SerializeField] private float punchSpeed = 8f;


    private TextMeshProUGUI label;
    private RectTransform rect;

    private int actualCoins;       // the real count (instant)
    private float displayedCoins;    // what's currently shown (animated)
    private float currentScale = 1f;


    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
        UpdateLabel();
    }


    private void OnEnable()
    {
        GameEvent.OnCoinArrived += HandleCoinArrived;
    }

    private void OnDisable()
    {
        GameEvent.OnCoinArrived -= HandleCoinArrived;
    }


    private void HandleCoinArrived(int value)
    {
        actualCoins += value;
        currentScale = punchScale;   // trigger punch
    }


    private void Update()
    {
        // ── Animate displayed number toward actual ───────
        if (displayedCoins < actualCoins)
        {
            displayedCoins += countSpeed * Time.deltaTime;

            if (displayedCoins >= actualCoins)
                displayedCoins = actualCoins;

            UpdateLabel();
        }

        // ── Animate punch scale back to 1 ────────────────
        if (currentScale > 1f)
        {
            currentScale = Mathf.Lerp(currentScale, 1f, punchSpeed * Time.deltaTime);

            if (Mathf.Abs(currentScale - 1f) < 0.01f)
                currentScale = 1f;

            rect.localScale = Vector3.one * currentScale;
        }
    }


    /// <summary>
    /// Call this to set coin count from a save file / server.
    /// </summary>
    public void SetCoins(int amount)
    {
        actualCoins = amount;
        displayedCoins = amount;
        UpdateLabel();
    }


    private void UpdateLabel()
    {
        label.text = Mathf.FloorToInt(displayedCoins).ToString("N0");
        // "N0" formats with commas: 1234 → "1,234"
    }
}