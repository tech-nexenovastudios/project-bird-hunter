//using DG.Tweening;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;


////Responsible for slot scroll
//public class RowScroll : MonoBehaviour
//{

//    [Tooltip("Assign SlotContainer")][SerializeField] SlotContainer slotContainer;
//    //[SerializeField] List<RectTransform> slot;
//    [SerializeField] List<Vector2> slotStartPos;
//    [Tooltip("Enter Slot Height")][SerializeField] float slotHeight;
//    [Tooltip("Assign Slot")][SerializeField] RectTransform slotParentHeight;
//    float distanceRequireForMid; 
//    [SerializeField] int counter = 1;
//    [Tooltip("Assign Slot Container")][SerializeField] RectTransform slotRect;

//    int totalChild = 0;
//    int currentChildCount = 0;

//    [SerializeField] float scrollSpeed = 50;


//    [SerializeField] float distance = 100;

//    [SerializeField] int maxLoops = 30;

//    bool isComplete;


//    [SerializeField] public int resultIndex;
//    [HideInInspector] public SlotPowerHolder_SO powerUp;

//    [SerializeField] SlotDataSet slot;


//    Coroutine spinCoroutine;

///*    private void OnEnable()
//    {
//         StartSpin(); 
//    }*/
//    private void Start()
//    {

//    }
//    public void StartSpin()
//    {
//        counter = 1;
//        isComplete = false;
//        totalChild = transform.childCount;
//        currentChildCount = totalChild;
//        //resultIndex = Random.Range(0, currentChildCount);
//        distanceRequireForMid = ((slotParentHeight.sizeDelta.y) / 2) - (slotHeight / 2);
//        Debug.Log(slotRect.sizeDelta.y);

//        foreach (var pos in slotContainer.slots)
//        {
//            slotStartPos.Add(pos.anchoredPosition);
//        }


//        //Vector2 loopValue = new Vector2(0, ((slotHeight * currentChildCount) + (distance * currentChildCount)) * maxLoops); ;
//         spinCoroutine =  StartCoroutine(SpinUpdate());


//        slot.powerUp = powerUp;
//        slot.HighlightResultWithAnimation(resultIndex - 1);

//    }


//    IEnumerator SpinUpdate()
//    {
//        Debug.Log("Spin");
//        while (true)
//        {
//           // Debug.Log("Check");
//            if (counter < (maxLoops * currentChildCount) + 1)
//            {
//                slotRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
//            }
//            else
//            {
//                if (!isComplete)
//                {

//                    isComplete = true;
//                    Debug.Log(slotRect.anchoredPosition.y);
//                    float anchorPos = ((slotHeight * currentChildCount) + (distance * currentChildCount)) * (maxLoops + 1);
//                    float desirePos = (anchorPos - distanceRequireForMid) + ((resultIndex - 1) * (distance + slotHeight));
//                   slotRect.DOAnchorPosY(desirePos, scrollSpeed).SetSpeedBased().OnComplete(() =>
//                    {
//                        slotRect.anchoredPosition = new Vector2(0, desirePos);
//                        StopCoroutine(spinCoroutine);
//                        //Invoke("ResetSlot", 3f);
//                    });



//                }

//            }
//            if (slotRect.anchoredPosition.y >= (distance * counter) + (slotHeight * counter))
//            {
//                //Debug.Log((distance * counter) + (slotHeight * counter));
//                transform.GetChild((counter - 1) % (transform.childCount)).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, ((-(slotHeight * totalChild) - distance * totalChild) - slotHeight / 2));
//                counter++;
//                totalChild++;
//            }
//            yield return null;

//        }

//    }

//    private void OnDisable()
//    {
//        ResetSlot(); //Rest slot after disable
//    }

//    public void ResetSlot()
//    {

//        slotRect.anchoredPosition = new Vector2(0, 0);

//        for (int i = 0; i < slotContainer.slots.Length; i++)
//        {
//            slotContainer.slots[i].anchoredPosition = slotStartPos[i];
//        }
//    }


//}
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.Serialization;

public class RowScroll : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] RectTransform slotRect; // The parent containing all icons
    [SerializeField] SlotContainer slotContainer;

    [Tooltip("Height of one icon + gap")]
    [SerializeField] float itemHeightWithGap = 200f;

    [SerializeField] float scrollSpeed = 2000f;
    [SerializeField] int extraLoopsBeforeStop = 3;

    [Header("Debug/State")]
    public int resultIndex;
    [HideInInspector] public PowerupConfig[] powerUps;
    [SerializeField] SlotDataSet slot;

    private bool isSpinning = false;
    private float totalContentHeight;
    private int totalItems;
    private List<Vector2> initialPositions = new List<Vector2>();

    private void Start()
    {
        totalItems = slotContainer.slots.Length;

        // Auto-calculate height if not set
        if (itemHeightWithGap <= 0 && totalItems > 1)
        {
            itemHeightWithGap = Mathf.Abs(slotContainer.slots[0].anchoredPosition.y - slotContainer.slots[1].anchoredPosition.y);
        }

        totalContentHeight = itemHeightWithGap * totalItems;

        foreach (var t in slotContainer.slots)
        {
            initialPositions.Add(t.anchoredPosition);
        }
    }

    [ContextMenu("Test Spin")]
    public void StartSpin()
    {
        if (isSpinning) return;
        isSpinning = true;

        if (slot != null)
        {
            //slot.powerUp = powerUps;
            slot.HighlightResultWithAnimation(resultIndex);
        }

        StartCoroutine(SpinRoutine());
    }

    IEnumerator SpinRoutine()
    {
        // 1. Constant Spin Phase
        float timer = 0f;
        float minSpinTime = 1.5f;

        while (timer < minSpinTime)
        {
            float moveStep = scrollSpeed * Time.deltaTime;
            slotRect.anchoredPosition += Vector2.up * moveStep;

            UpdateItemsLooping(); // CRITICAL: This keeps items from disappearing
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. Calculate Landing
        // We calculate how much further we need to go to reach the resultIndex
        float currentY = slotRect.anchoredPosition.y;
        float currentMod = currentY % totalContentHeight;
        float targetMod = (resultIndex * itemHeightWithGap);

        float distanceToGo = targetMod - currentMod;
        if (distanceToGo < 0) distanceToGo += totalContentHeight;

        // Add the extra loops for a smooth slowdown
        distanceToGo += (totalContentHeight * extraLoopsBeforeStop);
        float finalDestinationY = currentY + distanceToGo;

        // 3. Smooth Stop with DOTween
        slotRect.DOAnchorPosY(finalDestinationY, 2f)
            .SetEase(Ease.OutCubic)
            .OnUpdate(UpdateItemsLooping) // Keep looping items during the tween
            .OnComplete(() =>
            {
                isSpinning = false;
                Debug.Log("Spin Finished at Index: " + resultIndex);
            });
    }

    /// <summary>
    /// This is the "Treadmill" logic. 
    /// It checks if an item has moved too far up and moves it to the bottom.
    /// </summary>
    void UpdateItemsLooping()
    {
        float containerY = slotRect.anchoredPosition.y;

        for (int i = 0; i < slotContainer.slots.Length; i++)
        {
            RectTransform item = slotContainer.slots[i];

            // 1. Get the initial offset of this specific item
            float startY = initialPositions[i].y;

            // 2. Calculate the "New" Y position relative to the moving parent
            // We use Mathf.Repeat (which is a modulo) to keep the Y within the total range
            float worldY = startY + containerY;

            // This is the magic: it keeps the value between 0 and -totalContentHeight
            // Adjust the offset (+ totalContentHeight / 2) depending on your pivot points
            float wrappedY = Mathf.Repeat(worldY + (totalContentHeight / 2f), totalContentHeight) - (totalContentHeight / 2f);

            // 3. Apply only the difference to the item
            item.anchoredPosition = new Vector2(item.anchoredPosition.x, wrappedY - containerY);
        }
    }

    public void ResetSlot()
    {
        slotRect.DOKill();
        isSpinning = false;
        slotRect.anchoredPosition = Vector2.zero;
        for (int i = 0; i < slotContainer.slots.Length; i++)
        {
            slotContainer.slots[i].anchoredPosition = initialPositions[i];
        }
    }

    private void OnDisable() => ResetSlot();
}