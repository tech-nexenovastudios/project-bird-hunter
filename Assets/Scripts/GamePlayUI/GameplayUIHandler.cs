using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Gameplay.UI
{
    public class GameplayUIHandler : MonoBehaviour
    {
        public static GameplayUIHandler Instance;
        [Header("HUD Reference")]
        [SerializeField] private GameplayHUD gameplayHUD;
        [Header("Slot")]
        [SerializeField] private SlotMachineScreen slotMachineScreen;
        [SerializeField] private GameObject gameoverUI;
        private void Awake()
        {
            Instance = this;
        }
        private void OnEnable()
        {
            GameEvents.OnSpinTriggered += slotMachineScreen.ShowSlot;
            //GameEvents.OnPowerupSelected += OnPlayerConfirmedSpin;
            GameEvents.OnPowerupSelected += OnPowerupSelected;
            GameEvents.OnPlayerDeath += OnPlayerDeath;
        }
        private void OnDisable()
        {
            GameEvents.OnSpinTriggered -= slotMachineScreen.ShowSlot;
            //GameEvents.OnPlayerConfirmedSpin -= OnPlayerConfirmedSpin;
            GameEvents.OnPowerupSelected -= OnPowerupSelected;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
        }
        // ───────── spin confirmed ─────────
        private void OnPlayerConfirmedSpin()
        {
            slotMachineScreen.HideSlot();
            //gameplayHUD.ShowLevelDetailPopup();
            GameManager.Instance.StartGameplay();
        }
        // ───────── powerup selected ─────────
        private void OnPowerupSelected(PowerupConfig config)
        {
            // Use config.displayName / config.icon / config.rarity for UI preview
            slotMachineScreen.HideSlot();
            GameManager.Instance.StartGameplay();
        }
        private void OnPlayerDeath()
        {
            gameoverUI.SetActive(true);
        }
        public void OnClickReturnToMenu()
        {
            Destroy(GameProgressManager.Instance.gameObject);
            Destroy(XPManager.Instance.gameObject);

            SceneManager.LoadScene("MainMenu");
        }
    }
}