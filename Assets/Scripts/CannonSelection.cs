using System;
using System.Collections.Generic;
using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CannonSelection : MonoBehaviour
{
    [SerializeField] CannonHolder_SO cannonUpgradeSO;
    [SerializeField] GameObject      cannonUiTemplate;
    [SerializeField] Transform       cannonUiParent;
    [SerializeField] Transform       cannonSelectionUiParent;
    [SerializeField] Image           selectedUpgradeCannonImage;
    [SerializeField] Image           selectedCannonImage;
    [SerializeField] int             selectedCannonIndex;
    [SerializeField] int             prevSelectedCannonIndex;
    [SerializeField] Sprite          deselectedSprite, selectedSprite;
    [SerializeField] TextMeshProUGUI damageText, healthText, powerText;

    private void Start()
    {
        int startIndex = PlayerPrefs.HasKey("CannonIndex") ? PlayerPrefs.GetInt("CannonIndex") : 0;
        UpdateSelectedCannon(startIndex);

        int i = 0;
        foreach (var cannon in cannonUpgradeSO.cannonsData)
        {
            cannon.cannonIndex = i;

            var cData = cannonUiParent.GetChild(cannon.cannonIndex).GetComponent<AbilityData>();
            var sData = cannonSelectionUiParent.GetChild(cannon.cannonIndex).GetComponent<AbilityData>();

            if (sData != null)
            {
                sData.cannonImage.sprite = cannon.cannonSprite;
                sData.equipButton.onClick.AddListener(() =>
                    SelectCannon(cannon.cannonIndex, cannon.cannonSprite));
            }

            if (cData != null)
            {
                cData.cannonImage.sprite = cannon.cannonSprite;
                cData.equipButton.onClick.AddListener(() =>
                    UpgradeCannon(cannon.cannonIndex, cannon.cannonSprite));
            }

            i++;
        }
    }

    public void SelectCannonImage(Image image) =>
        selectedUpgradeCannonImage.sprite = image.sprite;

    public void SelectCannon(int cannonIndex, Sprite cannonSprite)
    {
        selectedCannonIndex        = cannonIndex;
        selectedCannonImage.sprite = cannonUpgradeSO.cannonsData[cannonIndex].cannonSprite;
    }

    public void UpgradeCannon(int cannonIndex, Sprite cannonSprite)
    {
        selectedCannonIndex = cannonIndex;
        selectedUpgradeCannonImage.sprite =
            cannonUpgradeSO.cannonsData[cannonIndex].cannonSprite;
        RefreshStatsText();
    }

    public void SelectCannon(int cannonIndex)
    {
        selectedCannonIndex = cannonIndex;
        UpdateSelectedCannon(selectedCannonIndex);
    }

    public void EquipBtn()
    {
        prevSelectedCannonIndex = PlayerPrefs.GetInt("CannonIndex", 0);
        PlayerPrefs.SetInt("CannonIndex", selectedCannonIndex);

        cannonSelectionUiParent.GetChild(prevSelectedCannonIndex)
            .GetChild(0).GetComponent<Image>().sprite = deselectedSprite;
        cannonSelectionUiParent.GetChild(selectedCannonIndex)
            .GetChild(0).GetComponent<Image>().sprite = selectedSprite;
    }

    public void UpdateSelectedCannon(int index)
    {
        selectedCannonIndex = index;
        var data  = cannonUpgradeSO.cannonsData[index];
        var stats = data.cannonStats;

        selectedUpgradeCannonImage.sprite = data.cannonSprite;
        selectedCannonImage.sprite        = data.cannonSprite;

        // Apply 600-level progression then display
        stats.ApplyProgression(GameProgressManager.Instance.GlobalLevel);
        RefreshStatsText();
    }

    private void RefreshStatsText()
    {
        var stats = cannonUpgradeSO.cannonsData[selectedCannonIndex].cannonStats;
        if (damageText != null) damageText.text = $"DAMAGE: {stats.bulletDamage:F0}";
        if (healthText != null) healthText.text = $"HEALTH: {stats.maxHealth:F0}";
        if (powerText  != null) powerText.text  = $"FIRE RATE: {stats.fireRate:F2}";
    }
}
