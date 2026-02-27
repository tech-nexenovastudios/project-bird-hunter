using UnityEngine;

namespace com.birdhunter.core.Services
{
    public interface IAdsManager
    {
        void ShowAds();
    }
    public class AdsManager : IAdsManager
    {
        public void ShowAds()
        {
            Debug.Log("Show Ads");
        }
    }
}