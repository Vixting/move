using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class ContainerInstance
    {
        public string instanceId;
        public ContainerData containerData;
        public int width => containerData.width;
        public int height => containerData.height;
        
        private bool[,] _occupiedCells;
        
        private Dictionary<string, ItemInstance> _items = new Dictionary<string, ItemInstance>();
        
        public ContainerInstance(ContainerData data)
        {
            instanceId = Guid.NewGuid().ToString();
            containerData = data;
            _occupiedCells = new bool[data.width, data.height];
        }
        
        public List<ItemInstance> GetAllItems()
        {
            return new List<ItemInstance>(_items.Values);
        }
        
        public void LogAllItems()
        {
            Debug.Log($"Container {containerData.id} contains {_items.Count} items:");
            foreach (var item in _items.Values)
            {
                Debug.Log($"  - Item: {item.itemData.displayName}, Position: ({item.position.x}, {item.position.y}), Size: {item.GetWidth()}x{item.GetHeight()}, Rotated: {item.isRotated}");
            }
        }
        
        public void VisualizeGrid()
        {
            string gridVisual = $"Grid state for container {containerData.id} ({width}x{height}):\n";
            
            for (int y = 0; y < height; y++)
            {
                string row = "";
                for (int x = 0; x < width; x++)
                {
                    if (_occupiedCells[x, y])
                    {
                        row += "[X]";
                    }
                    else
                    {
                        row += "[ ]";
                    }
                }
                gridVisual += row + "\n";
            }
            
            Debug.Log(gridVisual);
        }
        
        public bool IsOccupied(int x, int y, ItemInstance excludeItem = null)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return true;
                
            if (!_occupiedCells[x, y])
                return false;
                
            if (excludeItem != null)
            {
                foreach (var item in _items.Values)
                {
                    if (item == excludeItem)
                        continue;
                        
                    int itemWidth = item.GetWidth();
                    int itemHeight = item.GetHeight();
                    
                    if (x >= item.position.x && x < item.position.x + itemWidth &&
                        y >= item.position.y && y < item.position.y + itemHeight)
                    {
                        return true;
                    }
                }
                return false;
            }
            
            return _occupiedCells[x, y];
        }
        
        public bool CanPlaceItem(ItemInstance item, Vector2Int position)
        {
            if (item == null) return false;
            
            if (containerData.restrictedCategories.Contains(item.itemData.category))
                return false;
                
            int itemWidth = item.GetWidth();
            int itemHeight = item.GetHeight();
            
            if (position.x < 0 || position.y < 0 || 
                position.x + itemWidth > width || 
                position.y + itemHeight > height)
            {
                return false;
            }
            
            for (int x = 0; x < itemWidth; x++)
            {
                for (int y = 0; y < itemHeight; y++)
                {
                    if (IsOccupied(position.x + x, position.y + y, item))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        public bool AddItem(ItemInstance item, Vector2Int position)
        {
            if (!CanPlaceItem(item, position))
            {
                return false;
            }
            
            int itemWidth = item.GetWidth();
            int itemHeight = item.GetHeight();
            
            for (int x = 0; x < itemWidth; x++)
            {
                for (int y = 0; y < itemHeight; y++)
                {
                    _occupiedCells[position.x + x, position.y + y] = true;
                }
            }
            
            item.position = position;
            item.container = this;
            item.containerId = this.containerData.id;
            _items[item.instanceId] = item;
            
            Debug.Log($"Added item {item.itemData.displayName} to {containerData.id} at position ({position.x}, {position.y}) with size {itemWidth}x{itemHeight}");
            
            return true;
        }

        public bool RemoveItem(ItemInstance item)
        {
            if (item == null || !_items.ContainsKey(item.instanceId))
            {
                return false;
            }
            
            int itemWidth = item.GetWidth();
            int itemHeight = item.GetHeight();
            
            for (int x = 0; x < itemWidth; x++)
            {
                for (int y = 0; y < itemHeight; y++)
                {
                    int cellX = item.position.x + x;
                    int cellY = item.position.y + y;
                    
                    if (cellX >= 0 && cellY >= 0 && cellX < width && cellY < height)
                    {
                        _occupiedCells[cellX, cellY] = false;
                    }
                }
            }
            
            _items.Remove(item.instanceId);
            item.container = null;
            item.containerId = null;
            
            Debug.Log($"Removed item {item.itemData.displayName} from {containerData.id}");
            
            return true;
        }
        
        public Vector2Int? FindAvailablePosition(ItemInstance item)
        {
            if (item == null) return null;
            
            item.isRotated = false;
            int itemWidth = item.GetWidth();
            int itemHeight = item.GetHeight();
            
            for (int y = 0; y <= height - itemHeight; y++)
            {
                for (int x = 0; x <= width - itemWidth; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (CanPlaceItem(item, position))
                    {
                        return position;
                    }
                }
            }
            
            if (item.itemData.canRotate)
            {
                item.isRotated = true;
                itemWidth = item.GetWidth();
                itemHeight = item.GetHeight();
                
                for (int y = 0; y <= height - itemHeight; y++)
                {
                    for (int x = 0; x <= width - itemWidth; x++)
                    {
                        Vector2Int position = new Vector2Int(x, y);
                        if (CanPlaceItem(item, position))
                        {
                            return position;
                        }
                    }
                }
                
                item.isRotated = false;
            }
            
            return null;
        }
        
        public ItemInstance GetItemAt(Vector2Int position)
        {
            Debug.Log($"Looking for item at position {position} in container {containerData.id}");
            
            if (_items.Count == 0)
            {
                Debug.Log($"Container {containerData.id} is empty - no items to check");
                return null;
            }
            
            foreach (var item in _items.Values)
            {
                int itemWidth = item.GetWidth();
                int itemHeight = item.GetHeight();
                
                Debug.Log($"Checking item {item.itemData.displayName} at ({item.position.x}, {item.position.y}) with size {itemWidth}x{itemHeight}");
                
                if (position.x >= item.position.x && 
                    position.x < item.position.x + itemWidth &&
                    position.y >= item.position.y && 
                    position.y < item.position.y + itemHeight)
                {
                    Debug.Log($"Found item {item.itemData.displayName} at position {position}");
                    return item;
                }
            }
            
            Debug.Log($"No items found at position {position} in container {containerData.id}");
            return null;
        }
        
        public bool WouldOverlapItems(ItemInstance draggingItem, Vector2Int position, out HashSet<ItemInstance> overlappingItems)
        {
            overlappingItems = new HashSet<ItemInstance>();
            
            int itemWidth = draggingItem.GetWidth();
            int itemHeight = draggingItem.GetHeight();
            
            for (int x = 0; x < itemWidth; x++)
            {
                for (int y = 0; y < itemHeight; y++)
                {
                    int checkX = position.x + x;
                    int checkY = position.y + y;
                    
                    if (checkX < 0 || checkY < 0 || checkX >= width || checkY >= height)
                    {
                        continue;
                    }
                    
                    ItemInstance overlappingItem = GetItemAt(new Vector2Int(checkX, checkY));
                    if (overlappingItem != null && overlappingItem.instanceId != draggingItem.instanceId)
                    {
                        overlappingItems.Add(overlappingItem);
                    }
                }
            }
            
            return overlappingItems.Count > 0;
        }
        
        public Dictionary<Vector2Int, bool> GetCellHighlightingForItem(ItemInstance item, Vector2Int position)
        {
            Dictionary<Vector2Int, bool> cellHighlighting = new Dictionary<Vector2Int, bool>();
            
            if (item == null) return cellHighlighting;
            
            int itemWidth = item.GetWidth();
            int itemHeight = item.GetHeight();
            
            bool canPlace = true;
            
            if (position.x < 0 || position.y < 0 || 
                position.x + itemWidth > width || 
                position.y + itemHeight > height)
            {
                canPlace = false;
            }
            
            if (containerData.restrictedCategories.Contains(item.itemData.category))
            {
                canPlace = false;
            }
            
            for (int x = 0; x < itemWidth; x++)
            {
                for (int y = 0; y < itemHeight; y++)
                {
                    int cellX = position.x + x;
                    int cellY = position.y + y;
                    
                    if (cellX >= 0 && cellY >= 0 && cellX < width && cellY < height)
                    {
                        bool isCellValid = !IsOccupied(cellX, cellY, item);
                        cellHighlighting[new Vector2Int(cellX, cellY)] = isCellValid && canPlace;
                    }
                }
            }
            
            return cellHighlighting;
        }
    }
}