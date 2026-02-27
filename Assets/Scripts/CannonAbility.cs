using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CannonAbility : MonoBehaviour
{
    public Transform birdHolder;

    // ── Set by CannonSpawner or falls back to AbilityManager ──
    [HideInInspector] public Abilities_SO activeAbility;

    private Queue<GameObject> electricFieldQueue = new Queue<GameObject>();

    private void Start()
    {
        // Fallback to AbilityManager if not set by spawner
        if (activeAbility == null && AbilityManager.instance?.activeAbility != null)
            activeAbility = AbilityManager.instance.activeAbility;

        if (activeAbility == null)
        {
            Debug.LogWarning("[CannonAbility] No active ability selected.");
            ManaManager.instance?.manaPowerUseBtn.gameObject.SetActive(false);
            return;
        }

        // Spawn ability bird
        if (activeAbility.abilityBird != null)
        {
            var birdGo = Instantiate(activeAbility.abilityBird, birdHolder);
            birdGo.transform.localPosition = Vector3.zero;
            birdGo.GetComponentsInChildren<IAbilityUse>()[0].abilityLevel = activeAbility.powerLevel;
        }

        // Wire ManaManager
        if (ManaManager.instance != null)
        {
            ManaManager.instance.manaPowerUseBtn.gameObject.SetActive(true);
            ManaManager.instance.maxManaValue = activeAbility.manaRequire;
            ManaManager.instance.manaPowerUseBtn.onClick.RemoveAllListeners();
            ManaManager.instance.manaPowerUseBtn.onClick.AddListener(() =>
            {
                if (ManaManager.instance.manaValue >= activeAbility.manaRequire)
                {
                    ManaManager.instance.manaValue = 0;
                    birdHolder.GetComponentsInChildren<IAbilityUse>()[0].UseAbility();
                }
                else
                {
                    Debug.Log("[CannonAbility] Not enough mana.");
                }
            });
        }
        else
        {
            Debug.LogError("[CannonAbility] ManaManager not found.");
        }
    }
}
