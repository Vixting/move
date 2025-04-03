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
        
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.id))
            {
                _itemsById[item.id] = item;
            }
            else
            {
                Debug.LogWarning($"Item with empty ID found in database: {item.displayName}");
            }
        }
    }
    
    public ItemData GetItem(string id)
    {
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
        return items.FindAll(item => item.category == category);
    }
    
    public List<ItemData> GetItemsByTag(string tag)
    {
        return items.FindAll(item => item.tags.Contains(tag));
    }
}