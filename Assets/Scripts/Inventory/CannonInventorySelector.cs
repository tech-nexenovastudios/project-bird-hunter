using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CannonInventorySelector : MonoBehaviour
{
    [Header("Cannon Data")]
    [SerializeField] private CannonUpgrade_SO cannonUpgradeSO;
    [SerializeField] private int cannonIndex;
    
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cannonSpriteImage;
    [SerializeField] private TextMeshProUGUI cannonNameText;
    [SerializeField] private TextMeshProUGUI cannonLevelText;
    
    [Header("Background Sprites")]
    [SerializeField] private Sprite activeBg;
    [SerializeField] private Sprite normalBg;
    
    [Header("Button Reference")]
    [SerializeField] private Button selectorButton;
    
    // Static reference to track currently selected cannon
    private static CannonInventorySelector currentlySelected;
    
    // PlayerPrefs key for saving selected cannon
    private const string SELECTED_CANNON_KEY = "SelectedCannonIndex";
    
    private bool isSelected = false;
    
    private void Awake()
    {
        // Get button component if not assigned
        if (selectorButton == null)
        {
            selectorButton = GetComponent<Button>();
        }
        
        // Add listener to button
        if (selectorButton != null)
        {
            selectorButton.onClick.AddListener(OnCannonSelected);
        }
    }
    
    private void Start()
    {
        // Initialize UI with cannon data
        UpdateCannonDisplay();
        
        // Load saved selection or set default
        LoadSavedSelection();
    }
    
    private void LoadSavedSelection()
    {
        // Get saved cannon index (default to 0 if nothing saved)
        int savedCannonIndex = PlayerPrefs.GetInt(SELECTED_CANNON_KEY, 0);
        
        // If this is the saved cannon, select it
        if (cannonIndex == savedCannonIndex)
        {
            // Small delay to ensure all cannons are initialized
            Invoke(nameof(SelectThisCannon), 0.1f);
        }
        else
        {
            // Set initial state to inactive
            SetSelectionState(false);
        }
    }
    
    private void SelectThisCannon()
    {
        SelectCannon();
    }
    
    private void OnCannonSelected()
    {
        // If this cannon is already selected, do nothing
        if (isSelected)
            return;
        
        // Deselect previously selected cannon
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.SetSelectionState(false);
        }
        
        // Select this cannon
        SetSelectionState(true);
        currentlySelected = this;
        
        // Save selection
        SaveSelection();
        
        // Update display
        UpdateCannonDisplay();
        
        // Optional: Trigger any additional events or callbacks here
        Debug.Log($"Selected Cannon: {cannonUpgradeSO.cannonName} (Index: {cannonIndex})");
    }
    
    private void SetSelectionState(bool selected)
    {
        isSelected = selected;
        
        // Change background sprite based on selection state
        if (backgroundImage != null)
        {
            backgroundImage.sprite = selected ? activeBg : normalBg;
        }
    }
    
    private void UpdateCannonDisplay()
    {
        if (cannonUpgradeSO == null)
        {
            Debug.LogWarning($"CannonUpgrade_SO not assigned on {gameObject.name}");
            return;
        }
        
        // Update cannon sprite
        if (cannonSpriteImage != null)
        {
            cannonSpriteImage.sprite = cannonUpgradeSO.cannonSprite;
        }
        
        // Update cannon name
        if (cannonNameText != null)
        {
            cannonNameText.text = cannonUpgradeSO.cannonName;
        }
        
        // Update cannon level
        if (cannonLevelText != null)
        {
            cannonLevelText.text = $"Level {cannonUpgradeSO.cannonLevel}";
        }
        
        // Store cannon index from SO (optional, can also set manually in inspector)
        cannonIndex = cannonUpgradeSO.cannonIndex;
    }
    
    private void SaveSelection()
    {
        // Save the selected cannon index to PlayerPrefs
        PlayerPrefs.SetInt(SELECTED_CANNON_KEY, cannonIndex);
        PlayerPrefs.Save();
        
        Debug.Log($"Saved Cannon Selection: Index {cannonIndex}");
    }
    
    // Public method to refresh display (call after upgrading cannon)
    public void RefreshDisplay()
    {
        UpdateCannonDisplay();
    }
    
    // Public getters for accessing cannon data
    public CannonUpgrade_SO GetCannonData()
    {
        return cannonUpgradeSO;
    }
    
    public int GetCannonIndex()
    {
        return cannonIndex;
    }
    
    public bool IsSelected()
    {
        return isSelected;
    }
    
    // Public method to manually select this cannon (useful for initialization)
    public void SelectCannon()
    {
        OnCannonSelected();
    }
    
    // Static method to get currently selected cannon
    public static CannonInventorySelector GetCurrentlySelected()
    {
        return currentlySelected;
    }
    
    // Static method to get currently selected cannon index
    public static int GetCurrentlySelectedIndex()
    {
        return currentlySelected != null ? currentlySelected.cannonIndex : 0;
    }
    
    // Method to reset selection to default (cannon index 0)
    public static void ResetToDefault()
    {
        PlayerPrefs.SetInt(SELECTED_CANNON_KEY, 0);
        PlayerPrefs.Save();
    }
    
    private void OnDestroy()
    {
        // Clean up listener
        if (selectorButton != null)
        {
            selectorButton.onClick.RemoveListener(OnCannonSelected);
        }
        
        // Clear static reference if this was selected
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }
    }
}