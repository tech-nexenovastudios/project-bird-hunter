using System;
using System.Collections.Generic;

[Serializable]
public class InventoryItem
{
    public string itemKey;
    public int quantity;
    public string instanceId;

    public InventoryItem()
    {
        itemKey = string.Empty;
        quantity = 0;
        instanceId = string.Empty;
    }
}

[Serializable]
public class InventoryData
{
    public List<InventoryItem> items;

    public InventoryData()
    {
        items = new List<InventoryItem>();
    }

    public int GetQuantity(string itemKey)
    {
        int total = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemKey == itemKey)
                total += items[i].quantity;
        }
        return total;
    }

    public bool HasItem(string itemKey)
    {
        return GetQuantity(itemKey) > 0;
    }
}