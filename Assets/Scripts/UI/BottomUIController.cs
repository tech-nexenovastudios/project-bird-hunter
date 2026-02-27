using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomUIController : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button shopButton;
    public Button inventoryButton;
    public Button homeButton;
    public Button powerUpButton;
    public Button eventButton;

    [Header("Size Settings")]
    public Vector2 normalSize = new Vector2(205, 180);
    public Vector2 activeSize = new Vector2(260, 220);
    public float animationDuration = 0.3f;

    [Header("Background Sprites")]
    public Sprite buttonBackgroundNormal;
    public Sprite buttonBackgroundActive;

    [Header("Icon Size Settings")]
    public Vector2 normalIconSize = new Vector2(90, 90);
    public Vector2 activeIconSize = new Vector2(150, 150);

    [Header("Icon Sprites")]
    [Header("Shop Icon")]
    public Sprite shopIconNormal;
    public Sprite shopIconActive;

    [Header("Inventory Icon")]
    public Sprite inventoryIconNormal;
    public Sprite inventoryIconActive;

    [Header("Home Icon")]
    public Sprite homeIconNormal;
    public Sprite homeIconActive;

    [Header("PowerUp Icon")]
    public Sprite powerUpIconNormal;
    public Sprite powerUpIconActive;

    [Header("Event Icon")]
    public Sprite eventIconNormal;
    public Sprite eventIconActive;

    private Button currentActiveButton;
    private Dictionary<Button, LayoutElement> buttonLayoutElements;
    private Dictionary<Button, Image> buttonImages;
    private Dictionary<Button, RectTransform> buttonIconTransforms;
    private Dictionary<Button, Image> buttonIconImages;

    private void Start()
    {
        // Cache components for better performance
        buttonLayoutElements = new Dictionary<Button, LayoutElement>();
        buttonImages = new Dictionary<Button, Image>();
        buttonIconTransforms = new Dictionary<Button, RectTransform>();
        buttonIconImages = new Dictionary<Button, Image>();

        CacheComponents();

        // Set initial active button (e.g., Home)
        SetActiveButton(homeButton);

        // Add button listeners
        shopButton.onClick.AddListener(() => SetActiveButton(shopButton));
        inventoryButton.onClick.AddListener(() => SetActiveButton(inventoryButton));
        homeButton.onClick.AddListener(() => SetActiveButton(homeButton));
        powerUpButton.onClick.AddListener(() => SetActiveButton(powerUpButton));
        eventButton.onClick.AddListener(() => SetActiveButton(eventButton));
    }

    private void CacheComponents()
    {
        // Cache layout elements and button images
        CacheButtonComponents(shopButton);
        CacheButtonComponents(inventoryButton);
        CacheButtonComponents(homeButton);
        CacheButtonComponents(powerUpButton);
        CacheButtonComponents(eventButton);
    }

    private void CacheButtonComponents(Button button)
    {
        // Cache LayoutElement
        buttonLayoutElements[button] = button.GetComponent<LayoutElement>();

        // Cache button's Image component (for background sprite)
        buttonImages[button] = button.GetComponent<Image>();

        // Cache the first child's RectTransform and Image component (assuming it's the icon)
        if (button.transform.childCount > 0)
        {
            Transform iconChild = button.transform.GetChild(0);
            buttonIconTransforms[button] = iconChild.GetComponent<RectTransform>();
            buttonIconImages[button] = iconChild.GetComponent<Image>();

            // Debug information
            Debug.Log($"Button: {button.name}, Child: {iconChild.name}, Has Image: {buttonIconImages[button] != null}");

            if (buttonIconImages[button] == null)
            {
                Debug.LogWarning($"Button {button.name}'s child '{iconChild.name}' doesn't have an Image component for icon sprite!");
            }
        }
        else
        {
            Debug.LogWarning($"Button {button.name} has no child component for icon!");
        }
    }

    public void SetActiveButton(Button newActiveButton)
    {
        // If same button clicked, do nothing
        if (currentActiveButton == newActiveButton) return;

        // Reset previous active button to normal state
        if (currentActiveButton != null)
        {
            SetButtonState(currentActiveButton, false);
        }

        // Set new active button to active state
        SetButtonState(newActiveButton, true);
        currentActiveButton = newActiveButton;

        // Handle your scene/panel switching logic here
        HandleButtonAction(newActiveButton);
    }

    private void SetButtonState(Button button, bool isActive)
    {
        // Set button size
        Vector2 targetSize = isActive ? activeSize : normalSize;
        SetButtonSize(button, targetSize);

        // Set button background sprite
        SetButtonSprite(button, isActive);

        // Set icon size
        Vector2 targetIconSize = isActive ? activeIconSize : normalIconSize;
        SetIconSize(button, targetIconSize);

        // Set icon sprite
        SetIconSprite(button, isActive);
    }

    private void SetButtonSize(Button button, Vector2 targetSize)
    {
        LayoutElement layoutElement = buttonLayoutElements[button];

        // Method 1: Instant resize
        //  layoutElement.preferredWidth = targetSize.x;
        //  layoutElement.preferredHeight = targetSize.y;

        // Method 2: Animated resize (uncomment if you want smooth animation)
        // StartCoroutine(AnimateButtonSize(layoutElement, targetSize));

        // Method 3: Animate Resize using do tween
        layoutElement.DOPreferredSize(new Vector2(targetSize.x, targetSize.y), 0.2f);
    }

    private void SetButtonSprite(Button button, bool isActive)
    {
        Image buttonImage = buttonImages[button];
        if (buttonImage != null)
        {
            buttonImage.sprite = isActive ? buttonBackgroundActive : buttonBackgroundNormal;
        }
    }

    private void SetIconSize(Button button, Vector2 targetSize)
    {
        if (buttonIconTransforms.ContainsKey(button) && buttonIconTransforms[button] != null)
        {
            RectTransform iconTransform = buttonIconTransforms[button];

            // Method 1: Instant resize
            iconTransform.sizeDelta = targetSize;

            // Method 2: Animated resize (uncomment if you want smooth animation)
            // StartCoroutine(AnimateIconSize(iconTransform, targetSize));
        }
    }

    private void SetIconSprite(Button button, bool isActive)
    {
        if (buttonIconImages.ContainsKey(button) && buttonIconImages[button] != null)
        {
            Image iconImage = buttonIconImages[button];
            Sprite targetSprite = GetIconSprite(button, isActive);

            // Debug information
            Debug.Log($"Setting icon for {button.name}, isActive: {isActive}, targetSprite: {(targetSprite != null ? targetSprite.name : "NULL")}");

            if (targetSprite != null)
            {
                iconImage.sprite = targetSprite;
                Debug.Log($"Successfully set sprite for {button.name} to {targetSprite.name}");
            }
            else
            {
                Debug.LogWarning($"Missing icon sprite for button {button.name}, isActive: {isActive}");
            }
        }
        else
        {
            Debug.LogWarning($"No icon image found for button {button.name}");
        }
    }

    private Sprite GetIconSprite(Button button, bool isActive)
    {
        if (button == shopButton)
            return isActive ? shopIconActive : shopIconNormal;
        else if (button == inventoryButton)
            return isActive ? inventoryIconActive : inventoryIconNormal;
        else if (button == homeButton)
            return isActive ? homeIconActive : homeIconNormal;
        else if (button == powerUpButton)
            return isActive ? powerUpIconActive : powerUpIconNormal;
        else if (button == eventButton)
            return isActive ? eventIconActive : eventIconNormal;

        return null;
    }

    // Debug method - you can call this from inspector or another script to test
    [ContextMenu("Test Sprite Assignment")]
    public void TestSpriteAssignment()
    {
        Debug.Log("=== Testing Sprite Assignment ===");
        Debug.Log($"Shop Normal: {shopIconNormal}, Active: {shopIconActive}");
        Debug.Log($"Inventory Normal: {inventoryIconNormal}, Active: {inventoryIconActive}");
        Debug.Log($"Home Normal: {homeIconNormal}, Active: {homeIconActive}");
        Debug.Log($"PowerUp Normal: {powerUpIconNormal}, Active: {powerUpIconActive}");
        Debug.Log($"Event Normal: {eventIconNormal}, Active: {eventIconActive}");

        // Test setting sprites manually
        if (buttonIconImages.Count > 0)
        {
            foreach (var kvp in buttonIconImages)
            {
                if (kvp.Value != null)
                {
                    Debug.Log($"Button {kvp.Key.name} has valid Image component");
                }
            }
        }
    }

    // Optional: Smooth animation method for button size
    private System.Collections.IEnumerator AnimateButtonSize(LayoutElement layoutElement, Vector2 targetSize)
    {
        Vector2 startSize = new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // Smooth easing
            progress = Mathf.SmoothStep(0f, 1f, progress);

            Vector2 currentSize = Vector2.Lerp(startSize, targetSize, progress);
            layoutElement.preferredWidth = currentSize.x;
            layoutElement.preferredHeight = currentSize.y;

            yield return null;
        }

        layoutElement.preferredWidth = targetSize.x;
        layoutElement.preferredHeight = targetSize.y;
    }

    // Optional: Smooth animation method for icon size
    private System.Collections.IEnumerator AnimateIconSize(RectTransform iconTransform, Vector2 targetSize)
    {
        Vector2 startSize = iconTransform.sizeDelta;
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // Smooth easing
            progress = Mathf.SmoothStep(0f, 1f, progress);

            Vector2 currentSize = Vector2.Lerp(startSize, targetSize, progress);
            iconTransform.sizeDelta = currentSize;

            yield return null;
        }

        iconTransform.sizeDelta = targetSize;
    }

    private void HandleButtonAction(Button clickedButton)
    {
        if (clickedButton == shopButton)
        {
            Debug.Log("Shop panel activated");
            // ShowShopPanel();
        }
        else if (clickedButton == inventoryButton)
        {
            Debug.Log("Inventory panel activated");
            // ShowInventoryPanel();
        }
        else if (clickedButton == homeButton)
        {
            Debug.Log("Home panel activated");
            // ShowHomePanel();
        }
        else if (clickedButton == powerUpButton)
        {
            Debug.Log("PowerUp panel activated");
            // ShowPowerUpPanel();
        }
        else if (clickedButton == eventButton)
        {
            Debug.Log("Event panel activated");
            // ShowEventPanel();
        }
    }
}