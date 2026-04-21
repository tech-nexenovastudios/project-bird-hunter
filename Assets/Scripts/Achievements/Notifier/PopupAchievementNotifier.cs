using DefaultNamespace;

namespace BirdHunter.Achievement.Notifier
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public sealed class PopupAchievementNotifier : MonoBehaviour, IAchievementNotifier
    {
        [SerializeField] private AchievementPopupView _popupPrefab;
        [SerializeField] private Transform _popupParent;
        [SerializeField] private float _displaySeconds = 2.5f;

        private readonly Queue<IAchievement> _queue = new();
        private bool _isShowing;

        public UniTask ShowUnlockedAsync(IAchievement achievement)
        {
            _queue.Enqueue(achievement);

            if (!_isShowing)
                _ = ShowLoopAsync();

            return UniTask.CompletedTask;
        }

        private async UniTask ShowLoopAsync()
        {
            _isShowing = true;

            while (_queue.Count > 0)
            {
                var achievement = _queue.Dequeue();

                var instance = Instantiate(_popupPrefab, _popupParent);
                instance.Initialize(achievement);

                await UniTask.Delay(System.TimeSpan.FromSeconds(_displaySeconds), ignoreTimeScale: false);

                Destroy(instance.gameObject);
            }

            _isShowing = false;
        }
    }
}