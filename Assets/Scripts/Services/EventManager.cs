using UnityEngine;

namespace com.birdhunter.core.Services
{
    public interface IEventManager
    {
        void Subscribe(string eventName, System.Action callback);
    }
    public class EventManager : IEventManager
    {
        public void Subscribe(string eventName, System.Action callback)
        {
            Debug.Log($"Subscribing to event: {eventName}");
        }
    }
}