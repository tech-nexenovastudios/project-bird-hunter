using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using Gameplay.Managers;
using Gameplay.PowerUps;

public class SlotReel : MonoBehaviour
{
    [Header("References")]
    public RectTransform itemContainer;
    public TextMeshProUGUI topDescriptionText;
    public TextMeshProUGUI bottomDescriptionText;
    public CanvasGroup topDescriptionGroup;

    [Header("Settings")]
    public float itemHeight = 200f;
    public CanvasGroup bottomDescriptionGroup;
    public float spacing = 5f;
    public float spinSpeed = 2000f;
    public float spinDuration = 2f;

    [HideInInspector] public int resultIndex = 1;

    public PowerupConfig[] powerUp => GameProgressManager.Instance.allPowerups;

    [System.Serializable]
    public class SlotElement
    {
        public string skillName;
        public string powerDescription;
    }

    public SlotElement[] slotElements = new SlotElement[30];

    [Header("Animation Settings")]
    public float descriptionFadeInDuration = 0.5f;

    private void Start()
    {
        InitializeDefaultElements();
        HideDescriptions();
    }
    public float descriptionScaleAnimation = 1.2f;

    public void StartSpin()
    {
        if (resultIndex < 0 || resultIndex >= itemContainer.childCount)
        {
            Debug.LogError($"Invalid result index {resultIndex} for reel: {name}");
            return;
        }

        isSpinning = true;
        HideDescriptions();
        SetAllButtonsInteractable(false);

        StartCoroutine(SpinReelWithEffects());
    }
    public AnimationCurve bounceEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool isSpinning = false;

    private void InitializeDefaultElements()
    {
        for (int i = 0; i < slotElements.Length; i++)
        {
            if (string.IsNullOrEmpty(slotElements[i].skillName))
            {
                slotElements[i].skillName = $"Power Skill {i + 1}";
                slotElements[i].powerDescription = $"Damage: {(i + 1) * 25}\nCooldown: {Random.Range(2, 8)}s";
            }
        }
    }

    private Color GetRandomColor()
    {
        // Not needed anymore since we removed glow effects
        Color[] colors = { Color.white };
        return colors[0];
    }

    private IEnumerator SpinReelWithEffects()
    {

        HighlightResultWithAnimation(resultIndex);



        float timeElapsed = 0f;
        float currentY = itemContainer.anchoredPosition.y;

        // Add screen shake effect during spin
        StartCoroutine(ShakeEffect());

        // Fast spinning phase with speed variation
        while (timeElapsed < spinDuration)
        {
            // Vary speed for more natural feel
            float speedMultiplier = Mathf.Lerp(1.2f, 0.8f, timeElapsed / spinDuration);
            currentY += spinSpeed * speedMultiplier * Time.deltaTime;
            itemContainer.anchoredPosition = new Vector2(0f, currentY);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Calculate target position
        float targetY = CalculateTargetPosition(resultIndex);

        // Smooth deceleration with bounce
        yield return StartCoroutine(SmoothSnapWithBounce(targetY));

        // Final positioning and highlight
        itemContainer.anchoredPosition = new Vector2(0f, targetY);
        isSpinning = false;

        yield return new WaitForSeconds(0.2f); // Brief pause for anticipation

    }

    private IEnumerator ShakeEffect()
    {
        Vector3 originalPos = transform.localPosition;
        float shakeDuration = spinDuration * 0.7f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float shakeIntensity = Mathf.Lerp(2f, 0f, elapsed / shakeDuration);
            Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeOffset.z = 0;

            transform.localPosition = originalPos + shakeOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    private float CalculateTargetPosition(int index)
    {
        return index * (itemHeight + spacing);
    }

    private IEnumerator SmoothSnapWithBounce(float targetY)
    {
        float startY = itemContainer.anchoredPosition.y;
        float snapDuration = 0.6f;
        float elapsed = 0f;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapDuration;

            // Bounce easing
            float easedT = BounceEaseOut(t);
            float currentY = Mathf.Lerp(startY, targetY, easedT);

            itemContainer.anchoredPosition = new Vector2(0f, currentY);
            yield return null;
        }
    }

    private float BounceEaseOut(float t)
    {
        if (t < (1f / 2.75f))
            return 7.5625f * t * t;
        else if (t < (2f / 2.75f))
            return 7.5625f * (t -= (1.5f / 2.75f)) * t + 0.75f;
        else if (t < (2.5f / 2.75f))
            return 7.5625f * (t -= (2.25f / 2.75f)) * t + 0.9375f;
        else
            return 7.5625f * (t -= (2.625f / 2.75f)) * t + 0.984375f;
    }

    private void HighlightResultWithAnimation(int index)
    {
        // if (index >= slotElements.Length) return;

     
        // Get the result button
        Transform resultTransform = itemContainer.GetChild(index);
        Debug.Log(resultTransform.name);
        Button resultButton = resultTransform.GetComponent<Button>();

        /*for (int i = 0; i < itemContainer.childCount; i++)
        {
            if (i != index)
            {

                var objicon = itemContainer.GetChild(i).AddComponent<SlotIcon>();
                objicon.SetIcon(powerUp[Random.Range(0, powerUp.Count)]);
            }
        }*/

        var icon = resultTransform.gameObject.AddComponent<SlotIcon>();
        icon.SetIcon(powerUp[index]);

        // Enable button interaction
        if (resultButton != null)
        {
            resultButton.interactable = true;
            resultButton.onClick.RemoveAllListeners();
            resultButton.onClick.AddListener(() =>
            {
                icon.ApplyPowerUp();
            });
            resultButton.onClick.AddListener(() =>
            {
                OnIconSelected();
            });
            resultButton.onClick.AddListener(() =>
            {
            });
        }

        // Show descriptions with animations
        //  StartCoroutine(ShowDescriptionsWithAnimation(index));
    }

    private IEnumerator ShowDescriptionsWithAnimation(int index)
    {
        SlotElement element = slotElements[index];

        // Set text content
        topDescriptionText.text = element.skillName;
        bottomDescriptionText.text = element.powerDescription;

        // Animate top description
        StartCoroutine(AnimateDescriptionArea(topDescriptionGroup, true));
        yield return new WaitForSeconds(0.1f);

        // Animate bottom description with slight delay
        StartCoroutine(AnimateDescriptionArea(bottomDescriptionGroup, false));
    }

    private IEnumerator AnimateDescriptionArea(CanvasGroup canvasGroup, bool isTop)
    {
        // Reset initial state
        canvasGroup.alpha = 0;
        canvasGroup.transform.localScale = Vector3.zero;
        canvasGroup.gameObject.SetActive(true);

        // Animate scale and fade in
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one;

        while (elapsed < descriptionFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / descriptionFadeInDuration;

            // Bounce scale animation
            float scaleProgress = BounceEaseOut(t);
            canvasGroup.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, scaleProgress);

            // Fade in
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);

            yield return null;
        }

        canvasGroup.alpha = 1;
        canvasGroup.transform.localScale = targetScale;

        // Add floating animation
        StartCoroutine(FloatingAnimation(canvasGroup.transform, isTop));
    }

    private IEnumerator FloatingAnimation(Transform target, bool isTop)
    {
        Vector3 basePosition = target.localPosition;
        float floatAmount = 5f;
        float floatSpeed = 2f;

        while (target.gameObject.activeInHierarchy)
        {
            float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            if (isTop) offset = -offset; // Opposite direction for top

            target.localPosition = basePosition + Vector3.up * offset;
            yield return null;
        }
    }

    private void OnIconSelected()
    {
        // Trigger selection effect
        StartCoroutine(SelectionEffect());

        // Notify controller
        FindAnyObjectByType<SlotMachineController>().OnUserSelectsIcon();
    }

    private IEnumerator SelectionEffect()
    {
        // Scale up selected button
        Transform selectedButton = itemContainer.GetChild(resultIndex);
        Vector3 originalScale = selectedButton.localScale;

        for (float t = 0; t <= 1; t += Time.deltaTime * 4f)
        {
            selectedButton.localScale = Vector3.Lerp(originalScale, originalScale * 1.3f, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        for (float t = 0; t <= 1; t += Time.deltaTime * 4f)
        {
            selectedButton.localScale = Vector3.Lerp(originalScale * 1.3f, originalScale, t);
            yield return null;
        }
    }

    private void HideDescriptions()
    {
        if (topDescriptionGroup) topDescriptionGroup.gameObject.SetActive(false);
        if (bottomDescriptionGroup) bottomDescriptionGroup.gameObject.SetActive(false);
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < itemContainer.childCount; i++)
        {
            Button button = itemContainer.GetChild(i).GetComponent<Button>();
            if (button != null)
                button.interactable = interactable;
        }
    }


    public int ItemCount()
    {
        return itemContainer.childCount;
    }
}
