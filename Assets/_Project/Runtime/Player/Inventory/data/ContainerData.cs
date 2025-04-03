// ContainerData.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace InventorySystem
{
    [Serializable]
    public class ContainerData
    {
        public string id;
        public string displayName;
        public int width;
        public int height;
        public bool allowsFoldedWeapons = true;
        public List<ItemCategory> restrictedCategories = new List<ItemCategory>();
        
        public int Size => width * height;
    }

    [Serializable]
    public class ContainerInstance
    {
        public string instanceId;
        public ContainerData containerData;
        public int width => containerData.width;
        public int height => containerData.height;
        public Dictionary<Vector2Int, ItemInstance> items = new Dictionary<Vector2Int, ItemInstance>();
        
        public ContainerInstance(ContainerData data)
        {
            instanceId = Guid.NewGuid().ToString();
            containerData = data;
        }
        
        public bool IsOccupied(int x, int y, ItemInstance excludeItem = null)
        {
            foreach (var item in items.Values)
            {
                if (item == excludeItem) continue;
                
                int startX = item.position.x;
                int startY = item.position.y;
                int endX = startX + (item.isRotated ? item.itemData.height : item.itemData.width);
                int endY = startY + (item.isRotated ? item.itemData.width : item.itemData.height);
                
                if (x >= startX && x < endX && y >= startY && y < endY)
                {
                    return true;
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
                
                if (item.attachedItems.Count > 0)
                {
                    result.AddRange(item.attachedItems);
                }
            }
            return result;
        }
        
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
                item.Rotate();
            }
            
            return null;
        }
        
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
            
            item.position = pos;
            item.container = this;
            items[pos] = item;
            
            return true;
        }
        
        public bool RemoveItem(ItemInstance item)
        {
            if (items.ContainsValue(item))
            {
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
}