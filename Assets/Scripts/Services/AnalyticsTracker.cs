using UnityEngine;

namespace com.birdhunter.core.Services
{
    public interface IAnalyticsTracker
    {
        void TrackEvent(string eventName);
    }
    public class AnalyticsTracker : IAnalyticsTracker
    {
        public void TrackEvent(string eventName)
        {
            Debug.Log($"Tracking event: {eventName}");
        }
    }
}