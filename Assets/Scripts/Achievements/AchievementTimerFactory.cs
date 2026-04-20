using System;
using ImprovedTimers;

namespace BirdHunter.Achievement
{
    public interface IAchievementTimer
    {
        event Action OnCompleted;
        void Start();
        void Stop();
        void Reset(float? newDuration = null);
        bool IsRunning { get; }
        float Progress { get; }
    }

    public interface IAchievementTimerFactory
    {
        IAchievementTimer CreateCountdown(float seconds);
    }

    public sealed class CountdownTimerAdapter : IAchievementTimer, IDisposable
    {
        private readonly CountdownTimer _timer;

        public event Action OnCompleted;

        public CountdownTimerAdapter(float durationSeconds)
        {
            _timer = new CountdownTimer(durationSeconds);
            _timer.OnTimerStop += HandleEnd;
        }

        private void HandleEnd()
        {
            OnCompleted?.Invoke();
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Reset(float? newDuration = null)
        {
            if (newDuration.HasValue)
                _timer.Reset(newDuration.Value);
            else
                _timer.Reset();
        }

        public bool IsRunning => _timer.IsRunning;
        public float Progress => _timer.Progress;

        public void Dispose()
        {
            _timer.OnTimerStop -= HandleEnd;
            _timer.Dispose();
        }
    }
    public sealed class AchievementTimerFactory : IAchievementTimerFactory
    {
        public IAchievementTimer CreateCountdown(float seconds)
        {
            return new CountdownTimerAdapter(seconds);
        }
    }
}