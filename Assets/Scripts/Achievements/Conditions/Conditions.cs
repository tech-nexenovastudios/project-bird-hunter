using System;
using BirdHunter.Achievement.GameplayEvents;
using UnityEngine;

namespace BirdHunter.Achievement.Conditions
{


    public sealed class CounterCondition : IAchievementCondition
    {
        private readonly Func<GameplayEvent, bool> _filter;
        private readonly int _target;
        private int _current;

        public CounterCondition(Func<GameplayEvent, bool> filter, int target, int initial = 0)
        {
            _filter = filter;
            _target = target;
            _current = initial;
        }

        public bool IsMet => _current >= _target;

        public void OnEvent(GameplayEvent evt)
        {
            if (IsMet)
                return;

            if (_filter(evt))
                _current++;
        }

        public float GetProgress01()
        {
            if (_target <= 0)
                return 1f;

            return Mathf.Clamp01((float)_current / _target);
        }
    }

    public sealed class LevelReachedCondition : IAchievementCondition
    {
        private readonly int _targetLevel;
        private int _currentLevel;

        public LevelReachedCondition(int targetLevel)
        {
            _targetLevel = targetLevel;
        }

        public bool IsMet => _currentLevel >= _targetLevel;

        public void OnEvent(GameplayEvent evt)
        {
            if (IsMet)
                return;

            if (evt is LevelReachedEvent levelEvt)
            {
                if (levelEvt.NewLevel > _currentLevel)
                    _currentLevel = levelEvt.NewLevel;
            }
        }

        public float GetProgress01()
        {
            if (_targetLevel <= 0)
                return 1f;

            return Mathf.Clamp01((float)_currentLevel / _targetLevel);
        }
    }

    public sealed class TimedCounterCondition : IAchievementCondition
    {
        private readonly Func<GameplayEvent, bool> _filter;
        private readonly int _target;
        private readonly IAchievementTimer _timer;

        private int _current;

        public TimedCounterCondition(
            Func<GameplayEvent, bool> filter,
            int target,
            IAchievementTimerFactory timerFactory,
            float windowSeconds)
        {
            _filter = filter;
            _target = target;
            _timer = timerFactory.CreateCountdown(windowSeconds);
            _timer.OnCompleted += HandleTimerCompleted;
        }

        public bool IsMet => _current >= _target;

        public void OnEvent(GameplayEvent evt)
        {
            if (IsMet)
                return;

            if (!_filter(evt))
                return;

            if (!_timer.IsRunning)
            {
                _current = 0;
                _timer.Reset();
                _timer.Start();
            }

            _current++;
        }

        private void HandleTimerCompleted()
        {
            if (!IsMet)
                _current = 0;
        }

        public float GetProgress01()
        {
            if (_target <= 0)
                return 1f;

            return Mathf.Clamp01((float)_current / _target);
        }
    }
}