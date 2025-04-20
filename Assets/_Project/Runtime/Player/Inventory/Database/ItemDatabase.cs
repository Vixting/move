using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();
   
    private Dictionary<string, ItemData> _itemsById;
   
    private void OnEnable()
    {
        InitializeDatabase();
    }
   
    private void InitializeDatabase()
    {
        _itemsById = new Dictionary<string, ItemData>();
       
        // Remove any null entries
        items.RemoveAll(item => item == null);
        
        foreach (var item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.id))
            {
                _itemsById[item.id] = item;
            }
            else if (item != null)
            {
                Debug.LogWarning($"Item with empty ID found in database: {item.displayName}");
            }
        }
    }
   
    public ItemData GetItem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
            
        if (_itemsById == null)
        {
            InitializeDatabase();
        }
       
        if (_itemsById.TryGetValue(id, out ItemData item))
        {
            return item;
        }
       
        return null;
    }
   
    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(items);
    }
   
    public List<ItemData> GetItemsByCategory(ItemCategory category)
    {
        return items.FindAll(item => item != null && item.category == category);
    }
   
    public List<ItemData> GetItemsByTag(string tag)
    {
        return items.FindAll(item => item != null && item.tags.Contains(tag));
    }
    
    public bool AddItem(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
        {
            Debug.LogError("Cannot add null item or item with empty ID to database");
            return false;
        }
        
        if (_itemsById == null)
        {
            InitializeDatabase();
        }
        
        // Check if an item with this ID already exists
        if (_itemsById.ContainsKey(item.id))
        {
            Debug.LogWarning($"Item with ID '{item.id}' already exists in database");
            return false;
        }
        
        // Add to both collections
        items.Add(item);
        _itemsById[item.id] = item;
        
        Debug.Log($"Added item '{item.displayName}' with ID '{item.id}' to database");
        return true;
    }
    
    public bool RemoveItem(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }
        
        if (_itemsById == null)
        {
            InitializeDatabase();
        }
        
        if (_itemsById.TryGetValue(id, out ItemData item))
        {
            items.Remove(item);
            _itemsById.Remove(id);
            return true;
        }
        
        return false;
    }
    
    public bool UpdateItem(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
        {
            return false;
        }
        
        if (_itemsById == null)
        {
            InitializeDatabase();
        }
        
        // Find the index of the old item with null protection
        int index = items.FindIndex(i => i != null && i.id == item.id);
        if (index >= 0)
        {
            items[index] = item;
            _itemsById[item.id] = item;
            return true;
        }
        
        return false;
    }
    
#if UNITY_EDITOR
    public void ClearDatabase()
    {
        items.Clear();
        if (_itemsById != null)
        {
            _itemsById.Clear();
        }
    }
#endif
}