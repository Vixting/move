// ItemInstance.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace InventorySystem
{
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
        
        public List<ItemInstance> attachedItems = new List<ItemInstance>();
        
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
        
        public bool CanFitAt(ContainerInstance container, Vector2Int position)
        {
            if (position.x < 0 || position.y < 0 ||
                position.x + GridWidth > container.width ||
                position.y + GridHeight > container.height)
            {
                return false;
            }
            
            for (int x = position.x; x < position.x + GridWidth; x++)
            {
                for (int y = position.y; y < position.y + GridHeight; y++)
                {
                    if (container.IsOccupied(x, y, this))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}