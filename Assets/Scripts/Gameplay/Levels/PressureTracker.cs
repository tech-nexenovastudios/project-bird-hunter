using System.Collections.Generic;
using Egg = Gameplay.Eggs.Egg;
namespace Gameplay.Levels
{
    public class PressureTracker
    {
        readonly List<Eggs.Egg> _eggs = new List<Eggs.Egg>();
        readonly Dictionary<Eggs.Egg, int> _eggPressure = new Dictionary<Eggs.Egg, int>();

        public int CurrentPressure { get; private set; }

        public void RegisterEgg(Eggs.Egg egg)
        {
            if (egg == null || egg.config == null)
                return;

            _eggs.Add(egg);
            int p = egg.config.pressureValue;
            _eggPressure[egg] = p;
            CurrentPressure += p;

            //egg.OnTrySplit += HandleEggDestroyed;
            egg.OnDestroyed += HandleEggDestroyed;
        }

        void HandleEggDestroyed(Eggs.Egg egg)
        {
            if (_eggPressure.TryGetValue(egg, out int p))
            {
                CurrentPressure -= p;
                _eggPressure.Remove(egg);
            }

            _eggs.Remove(egg);
        }

        public void Recalculate()
        {
            CurrentPressure = 0;

            for (int i = _eggs.Count - 1; i >= 0; i--)
            {
                var egg = _eggs[i];
                if (egg == null)
                {
                    _eggs.RemoveAt(i);
                    continue;
                }

                if (egg.config == null)
                    continue;

                CurrentPressure += egg.config.pressureValue;
            }
        }
    }

}