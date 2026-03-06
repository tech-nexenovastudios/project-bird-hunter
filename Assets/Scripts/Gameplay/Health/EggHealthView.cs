using TMPro;
using UnityEngine;

namespace Gameplay.Health
{
    /// <summary>
    /// Minimal UI view for egg health. Listens to EggHealth.OnHpChanged
    /// and updates a TMP text with either raw HP or percent.
    /// </summary>
    public class EggHealthView : MonoBehaviour
    {
        [SerializeField] private EggHealth eggHealth;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private bool showPercent = false;

        private void Awake()
        {
            if (eggHealth == null)
                eggHealth = GetComponent<EggHealth>();
        }

        private void OnEnable()
        {
            if (eggHealth == null || hpText == null) return;
            eggHealth.OnHpChanged += HandleHpChanged;
            UpdateText(eggHealth.CurrentHp, eggHealth.MaxHp);
        }

        private void OnDisable()
        {
            if (eggHealth == null || hpText == null) return;
            eggHealth.OnHpChanged -= HandleHpChanged;
        }

        private void HandleHpChanged(int oldHp, int newHp)
        {
            if (eggHealth == null) return;
            UpdateText(newHp, eggHealth.MaxHp);
        }

        private void UpdateText(int current, int max)
        {
            if (hpText == null) return;

            current = Mathf.Max(0, current);

            if (showPercent && max > 0)
            {
                int percent = Mathf.RoundToInt((float)current / max * 100f);
                hpText.text = percent.ToString();
            }
            else
            {
                hpText.text = current.ToString();
            }
        }
    }
}

