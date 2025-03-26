using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemData
{
    public string id;
    public string displayName;
    public string description;
    public int width = 1;
    public int height = 1;
    public float weight = 0.5f;
    public Sprite icon;
    public GameObject prefab;
    public ItemCategory category;
    public ItemRarity rarity;
    public List<ItemProperty> properties = new List<ItemProperty>();
    public List<string> tags = new List<string>();
    public bool rotatable = true;
    public bool stackable = false;
    public int maxStackSize = 1;
    
    [NonSerialized] public bool isRotated = false;
    
    // Useful getter that accounts for rotation
    public int GridWidth => isRotated ? height : width;
    public int GridHeight => isRotated ? width : height;
}

public enum ItemCategory
{
    Weapon,
    Ammunition,
    Armor,
    Helmet,
    Medicine,
    Food,
    Drink,
    Key,
    Container,
    Mod,
    Valuable,
    Barter,
    Quest,
    Misc
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[Serializable]
public class ItemProperty
{
    public string name;
    public string value;
    public string unit;
    public Color color = Color.white;
}

[Serializable]
public class ContainerData
{
    public string id;
    public string displayName;
    public int width;
    public int height;
    public bool allowsFoldedWeapons = true;
    public List<ItemCategory> restrictedCategories = new List<ItemCategory>();
    
    // Size in cells
    public int Size => width * height;
}

public class InventorySlot
{
    public Vector2Int position;
    public ItemInstance item;
    public bool isOccupied => item != null;
    
    public InventorySlot(int x, int y)
    {
        position = new Vector2Int(x, y);
        item = null;
    }
}

[Serializable]
public class ItemInstance
{
    public string instanceId;
    public ItemData itemData;
    public Vector2Int position;
    public ContainerInstance container;
    public bool isRotated;
    public int currentStack = 1;
    public float currentDurability = 100f;
    public Dictionary<string, object> customData = new Dictionary<string, object>();
    
    // For weapons, armor, etc.
    public List<ItemInstance> attachedItems = new List<ItemInstance>();
    
    // Constructor
    public ItemInstance(ItemData data, Vector2Int pos, ContainerInstance containerInstance)
    {
        instanceId = Guid.NewGuid().ToString();
        itemData = data;
        position = pos;
        container = containerInstance;
        isRotated = data.isRotated;
    }
    
    public int GridWidth => isRotated ? itemData.height : itemData.width;
    public int GridHeight => isRotated ? itemData.width : itemData.height;
    
    public void Rotate()
    {
        if (!itemData.rotatable) return;
        isRotated = !isRotated;
    }
    
    // Check if this item can fit at the given position in the specified container
    public bool CanFitAt(ContainerInstance container, Vector2Int position)
    {
        if (position.x < 0 || position.y < 0 ||
            position.x + GridWidth > container.width ||
            position.y + GridHeight > container.height)
        {
            return false;
        }
        
        // Check if any cells are already occupied
        for (int x = position.x; x < position.x + GridWidth; x++)
        {
            for (int y = position.y; y < position.y + GridHeight; y++)
            {
                if (container.IsOccupied(x, y))
                {
                    return false;
                }
            }
        }
        
        return true;
    }
}

[Serializable]
public class ContainerInstance
{
    public string instanceId;
    public ContainerData containerData;
    public int width => containerData.width;
    public int height => containerData.height;
    public Dictionary<Vector2Int, ItemInstance> items = new Dictionary<Vector2Int, ItemInstance>();
    
    // Constructor
    public ContainerInstance(ContainerData data)
    {
        instanceId = Guid.NewGuid().ToString();
        containerData = data;
    }
    
    public bool IsOccupied(int x, int y)
    {
        foreach (var item in items.Values)
        {
            if (item.isRotated)
            {
                if (x >= item.position.x && x < item.position.x + item.itemData.height &&
                    y >= item.position.y && y < item.position.y + item.itemData.width)
                {
                    return true;
                }
            }
            else
            {
                if (x >= item.position.x && x < item.position.x + item.itemData.width &&
                    y >= item.position.y && y < item.position.y + item.itemData.height)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    public List<ItemInstance> GetAllItems()
    {
        List<ItemInstance> result = new List<ItemInstance>();
        foreach (var item in items.Values)
        {
            result.Add(item);
            
            // Add any attached items recursively
            if (item.attachedItems.Count > 0)
            {
                result.AddRange(item.attachedItems);
            }
        }
        return result;
    }
    
    // Find first available position for an item
    public Vector2Int? FindAvailablePosition(ItemInstance item)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (item.CanFitAt(this, pos))
                {
                    return pos;
                }
            }
        }
        
        // Try rotating the item if allowed
        if (item.itemData.rotatable)
        {
            item.Rotate();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (item.CanFitAt(this, pos))
                    {
                        return pos;
                    }
                }
            }
            // Rotate back if still no fit
            item.Rotate();
        }
        
        return null; // Could not fit
    }
    
    // Try to add an item to this container
    public bool AddItem(ItemInstance item, Vector2Int? position = null)
    {
        Vector2Int pos;
        
        if (position.HasValue)
        {
            pos = position.Value;
            if (!item.CanFitAt(this, pos))
            {
                return false;
            }
        }
        else
        {
            var availablePos = FindAvailablePosition(item);
            if (!availablePos.HasValue)
            {
                return false;
            }
            pos = availablePos.Value;
        }
        
        // Add the item
        item.position = pos;
        item.container = this;
        items[pos] = item;
        
        return true;
    }
    
    // Remove an item from this container
    public bool RemoveItem(ItemInstance item)
    {
        if (items.ContainsValue(item))
        {
            // Have to find the key because we're using position as key
            Vector2Int? keyToRemove = null;
            foreach (var pair in items)
            {
                if (pair.Value == item)
                {
                    keyToRemove = pair.Key;
                    break;
                }
            }
            
            if (keyToRemove.HasValue)
            {
                items.Remove(keyToRemove.Value);
                return true;
            }
        }
        return false;
    }
}