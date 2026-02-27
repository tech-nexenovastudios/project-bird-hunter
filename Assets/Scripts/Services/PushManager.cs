using UnityEngine;

namespace com.birdhunter.core.Services
{
    public interface IPushManager
    {
        void Subscribe();
    }
    public class PushManager : IPushManager
    {
        public void Subscribe()
        {
            Debug.Log("Subscribing to push notifications");
        }
    }
}