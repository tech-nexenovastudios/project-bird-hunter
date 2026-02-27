using UnityEngine;

namespace com.birdhunter.core.Services
{
    public interface IIAPManager
    {
        void PurchaseProduct(string productId); 
    }
    public class IAPManager : IIAPManager
    {
        public void PurchaseProduct(string productId)
        {
            Debug.Log($"Purchasing product: {productId}");
        }
    }
}