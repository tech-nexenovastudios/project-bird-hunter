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

    [Header("Locked State")]
    public Sprite lockedBackgroundSprite;   // Grey background
    public Sprite lockIconSprite;           // Lock icon

    private Button currentActiveButton;
    private Dictionary<Button, LayoutElement> buttonLayoutElements;
    private Dictionary<Button, Image> buttonImages;
    private Dictionary<Button, RectTransform> buttonIconTransforms;
    private Dictionary<Button, Image> buttonIconImages;

    private bool isEventUnlocked = false;
    [SerializeField] private GameObject shadowImage;

    private void Start()
    {
        buttonLayoutElements = new Dictionary<Button, LayoutElement>();
        buttonImages = new Dictionary<Button, Image>();
        buttonIconTransforms = new Dictionary<Button, RectTransform>();
        buttonIconImages = new Dictionary<Button, Image>();

        CacheComponents();
        LockEventButton();
        SetActiveButton(homeButton);

        shopButton.onClick.AddListener(() => SetActiveButton(shopButton));
        inventoryButton.onClick.AddListener(() => SetActiveButton(inventoryButton));
        homeButton.onClick.AddListener(() => SetActiveButton(homeButton));
        powerUpButton.onClick.AddListener(() => SetActiveButton(powerUpButton));
        eventButton.onClick.AddListener(() => SetActiveButton(eventButton));
    }

    private void CacheComponents()
    {
        CacheButtonComponents(shopButton);
        CacheButtonComponents(inventoryButton);
        CacheButtonComponents(homeButton);
        CacheButtonComponents(powerUpButton);
        CacheButtonComponents(eventButton);
    }

    private void CacheButtonComponents(Button button)
    {
        buttonLayoutElements[button] = button.GetComponent<LayoutElement>();
        buttonImages[button] = button.GetComponent<Image>();

        if (button.transform.childCount > 0)
        {
            Transform iconChild = button.transform.GetChild(0);
            buttonIconTransforms[button] = iconChild.GetComponent<RectTransform>();
            buttonIconImages[button] = iconChild.GetComponent<Image>();
        }
    }

    // ==================== Lock / Unlock Event ====================

    private void LockEventButton()
    {
        isEventUnlocked = false;
        eventButton.interactable = false;

        // Keep full opacity (Unity dims disabled buttons by default)
        var colors = eventButton.colors;
        colors.disabledColor = Color.white;
        eventButton.colors = colors;

        // Set grey background
        if (buttonImages.ContainsKey(eventButton) && lockedBackgroundSprite != null)
            buttonImages[eventButton].sprite = lockedBackgroundSprite;

        // Set lock icon (keep current size, don't change it)
        if (buttonIconImages.ContainsKey(eventButton) && lockIconSprite != null)
            buttonIconImages[eventButton].sprite = lockIconSprite;
    }

    //Call this when the event feature unlocks.
    public void UnlockEventButton()
    {
        isEventUnlocked = true;
        eventButton.interactable = true;

        // Restore normal background
        if (buttonImages.ContainsKey(eventButton))
            buttonImages[eventButton].sprite = buttonBackgroundNormal;

        // Restore normal event icon and size
        if (buttonIconImages.ContainsKey(eventButton) && eventIconNormal != null)
            buttonIconImages[eventButton].sprite = eventIconNormal;

        if (buttonIconTransforms.ContainsKey(eventButton))
            buttonIconTransforms[eventButton].sizeDelta = normalIconSize;
        shadowImage.SetActive(true);
    }

    // ==================== Button State ====================

    public void SetActiveButton(Button newActiveButton)
    {
        // Block if event is locked
        if (newActiveButton == eventButton && !isEventUnlocked) return;

        if (currentActiveButton == newActiveButton) return;

        if (currentActiveButton != null)
            SetButtonState(currentActiveButton, false);

        SetButtonState(newActiveButton, true);
        currentActiveButton = newActiveButton;

        HandleButtonAction(newActiveButton);
    }

    private void SetButtonState(Button button, bool isActive)
    {
        // Skip visual changes for locked event button
        if (button == eventButton && !isEventUnlocked) return;

        Vector2 targetSize = isActive ? activeSize : normalSize;
        SetButtonSize(button, targetSize);
        SetButtonSprite(button, isActive);

        Vector2 targetIconSize = isActive ? activeIconSize : normalIconSize;
        SetIconSize(button, targetIconSize);
        SetIconSprite(button, isActive);
    }

    private void SetButtonSize(Button button, Vector2 targetSize)
    {
        LayoutElement layoutElement = buttonLayoutElements[button];
        layoutElement.DOPreferredSize(new Vector2(targetSize.x, targetSize.y), 0.2f);
    }

    private void SetButtonSprite(Button button, bool isActive)
    {
        Image buttonImage = buttonImages[button];
        if (buttonImage != null)
            buttonImage.sprite = isActive ? buttonBackgroundActive : buttonBackgroundNormal;
    }

    private void SetIconSize(Button button, Vector2 targetSize)
    {
        if (buttonIconTransforms.ContainsKey(button) && buttonIconTransforms[button] != null)
            buttonIconTransforms[button].sizeDelta = targetSize;
    }

    private void SetIconSprite(Button button, bool isActive)
    {
        if (buttonIconImages.ContainsKey(button) && buttonIconImages[button] != null)
        {
            Sprite targetSprite = GetIconSprite(button, isActive);
            if (targetSprite != null)
                buttonIconImages[button].sprite = targetSprite;
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

    private void HandleButtonAction(Button clickedButton)
    {
        if (clickedButton == shopButton)
            Debug.Log("Shop panel activated");
        else if (clickedButton == inventoryButton)
            Debug.Log("Inventory panel activated");
        else if (clickedButton == homeButton)
            Debug.Log("Home panel activated");
        else if (clickedButton == powerUpButton)
            Debug.Log("PowerUp panel activated");
        else if (clickedButton == eventButton)
            Debug.Log("Event panel activated");
    }
}