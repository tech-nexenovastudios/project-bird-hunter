using DG.Tweening;
using Gameplay.Events;
using Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    public class LevelProgressBar : MonoBehaviour
    {
        [Header("Progress Bar")]
        [SerializeField] private Image progressBarFill;

        [Header("Progress Bar Icon")]
        [SerializeField] private Image EggImage;
        [SerializeField] private Image BirdImage;

        private int _targetScore = 0;
        private bool _isBossLevel;
        private bool _isLevel20;

        // ───────── Lifecycle ─────────

        private void OnEnable()
        {
            GameEvents.OnGameLevelUpdated += OnGameLevelUpdated;
            GameEvents.OnLevelScoreUpdated += OnLevelScoreUpdated;

            if (BossEventBus.Instance != null)
                BossEventBus.Instance.OnHealthChanged += OnBossHealthChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameLevelUpdated -= OnGameLevelUpdated;
            GameEvents.OnLevelScoreUpdated -= OnLevelScoreUpdated;

            if (BossEventBus.Instance != null)
                BossEventBus.Instance.OnHealthChanged -= OnBossHealthChanged;
        }

        // ───────── Level Started ─────────

        private void OnGameLevelUpdated(int levelIndex)
        {
            var spawn = SpawnController.Instance;
            _targetScore = spawn != null ? spawn.TargetScore : 0;
            _isBossLevel = spawn != null && spawn.IsBossLevel;
            _isLevel20 = levelIndex == 20;

            if (progressBarFill != null)
            {
                progressBarFill.DOKill();

                // Boss levels start fully filled and drain down
                progressBarFill.fillAmount = _isBossLevel ? 1f : 0f;
            }

            UpdateProgressBarIcon(levelIndex);
        }

        // ───────── Icon Swap ─────────

        private void UpdateProgressBarIcon(int currentLevel)
        {
            if (EggImage == null || BirdImage == null) return;

            EggImage.gameObject.SetActive(!_isBossLevel);
            BirdImage.gameObject.SetActive(_isBossLevel);
        }

        // ───────── Score Updated (normal levels only) ─────────

        private void OnLevelScoreUpdated(int levelScore, int delta)
        {
            // Ignore score updates on boss levels — bar is driven by boss HP
            if (_isBossLevel) return;

            UpdateProgressBar(levelScore);
        }

        // ───────── Boss HP Updated ─────────

        private void OnBossHealthChanged(float hpNormalized)
        {
            if (!_isBossLevel || progressBarFill == null) return;

            // hpNormalized = current boss HP ratio (1.0 = full, 0.0 = dead)
            //
            // Level 10 (phase 1): boss fights from 100% → 50% then retreats
            //   Map HP 1.0–0.5  →  bar fill 1.0–0.0
            //
            // Level 20 (phase 2): boss returns with ~50% HP and fights to 0%
            //   Map HP 0.5–0.0  →  bar fill 1.0–0.0

            float fill;

            if (_isLevel20)
            {
                // Phase 2: boss returns at 50% HP and fights to 0%.
                // hpNormalized is current/max where max = phase 2 max HP, so it
                // genuinely starts at ~0.5 and drains to 0 — map it straight to fill
                // so the player sees "boss came back with half a bar".
                fill = Mathf.Clamp01(hpNormalized);
            }
            else
            {
                // Phase 1: HP goes from 1.0 down to 0.5 (retreat threshold)
                // Map 1.0→0.5 onto fill 1.0→0.0
                fill = Mathf.Clamp01((hpNormalized - 0.5f) / 0.5f);
            }

            progressBarFill.DOKill();
            progressBarFill
                .DOFillAmount(fill, 0.3f)
                .SetEase(Ease.OutCubic);
        }

        // ───────── Progress Bar Fill (normal levels) ─────────

        private void UpdateProgressBar(int levelScore)
        {
            if (progressBarFill == null) return;

            float fill = _targetScore > 0
                ? Mathf.Clamp01((float)levelScore / _targetScore)
                : 0f;

            progressBarFill
                .DOFillAmount(fill, 0.3f)
                .SetEase(Ease.OutCubic);
        }
    }
}