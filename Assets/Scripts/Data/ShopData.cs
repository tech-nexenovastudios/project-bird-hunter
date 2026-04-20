using System;
using System.Collections.Generic;

[Serializable]
public class ShopItemCost
{
    public string currencyKey;
    public long amount;
}

[Serializable]
public class ShopItem
{
    public string purchaseId;
    public List<ShopItemCost> costs;
    public List<string> rewardItemKeys;

    public ShopItem()
    {
        purchaseId = string.Empty;
        costs = new List<ShopItemCost>();
        rewardItemKeys = new List<string>();
    }
}

[Serializable]
public class ShopData
{
    public List<ShopItem> items;

    public ShopData()
    {
        items = new List<ShopItem>();
    }

    public ShopItem GetItem(string purchaseId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].purchaseId == purchaseId)
                return items[i];
        }
        return null;
    }
}