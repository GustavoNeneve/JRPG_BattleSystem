using System.Collections.Generic;
using UnityEngine;
using NewBark.Data;

namespace NewBark.Runtime
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance;

        // ItemID -> Quantity
        public Dictionary<string, int> Items = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddItem(string itemId, int quantity = 1)
        {
            if (!GameDatabase.Instance.Items.ContainsKey(itemId))
            {
                Debug.LogWarning($"[Inventory] Trying to add non-existent item {itemId}");
                return;
            }

            if (Items.ContainsKey(itemId))
            {
                Items[itemId] += quantity;
            }
            else
            {
                Items.Add(itemId, quantity);
            }
            Debug.Log($"[Inventory] Added {quantity} x {itemId}. Total: {Items[itemId]}");
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (Items.ContainsKey(itemId) && Items[itemId] >= quantity)
            {
                Items[itemId] -= quantity;
                if (Items[itemId] <= 0)
                {
                    Items.Remove(itemId);
                }
                return true;
            }
            return false;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            return Items.ContainsKey(itemId) && Items[itemId] >= quantity;
        }
        
        public ItemData GetItemData(string itemId)
        {
            if (GameDatabase.Instance.Items.TryGetValue(itemId, out var data))
            {
                return data;
            }
            return null;
        }
    }
}
