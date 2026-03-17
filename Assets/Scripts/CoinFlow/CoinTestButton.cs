//using Gameplay.Events;
//using UnityEngine;
//using UnityEngine.UI;

//// ───────────────────────────────────────────────────────────
//// PURPOSE: Debug/test button that triggers the coin flow
////          effect when clicked.
////
//// SETUP:
////   1. Select your test Button GameObject
////   2. Attach this script to it
////   3. That's it — no Inspector wiring needed
////
//// HOW IT WORKS:
////   - Grabs the Button component automatically
////   - On click, fires GameEvents.CoinCollected
////   - Uses the button's own screen position as the burst origin
////   - Coins burst FROM the button and fly TO the counter
//// ───────────────────────────────────────────────────────────

//[RequireComponent(typeof(Button))]
//public class CoinTestButton : MonoBehaviour
//{
//    [Tooltip("How many coins to award per click")]
//    [SerializeField] private int coinsPerClick = 100;

//    private Button button;

//    private void Awake()
//    {
//        button = GetComponent<Button>();
//    }

//    private void OnEnable()
//    {
//        button.onClick.AddListener(OnClicked);
//    }

//    private void OnDisable()
//    {
//        button.onClick.RemoveListener(OnClicked);
//    }

//    private void OnClicked()
//    {
//        // 'transform.position' on a UI element in a
//        // Screen Space Overlay canvas = screen pixels directly.
//        // So the coins burst right from the button's center.

//        Vector2 buttonScreenPos = transform.position;
//        GameEvent.CoinCollected(buttonScreenPos, coinsPerClick);
//    }
//}