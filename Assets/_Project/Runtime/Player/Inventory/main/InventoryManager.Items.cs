using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager
    {
        private void LoadPlayerInventory()
        {
            Debug.Log("Loading player inventory...");
            
            if (_character != null && _character.InventoryData != null && _character.InventoryData.Items.Count > 0)
            {
                Debug.Log($"Found {_character.InventoryData.Items.Count} items in character inventory data");
                
                foreach (var itemData in _character.InventoryData.Items)
                {
                    Debug.Log($"Adding item {itemData.ItemData.displayName} to {itemData.ContainerId} at ({itemData.X}, {itemData.Y})");
                    ItemInstance item = AddItemToContainer(itemData.ItemData, itemData.ContainerId, new Vector2Int(itemData.X, itemData.Y), itemData.IsRotated);
                    
                    if (item != null)
                    {
                        Debug.Log($"Successfully added item {item.itemData.displayName} to {itemData.ContainerId}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to add item {itemData.ItemData.displayName} to {itemData.ContainerId}");
                    }
                }
                
                ReconnectAllReferences();
            }
            else
            {
                Debug.Log("No saved inventory data found, adding sample items...");
                AddSampleItems();
            }
            
            foreach (var container in _containers.Values)
            {
                container.LogAllItems();
                container.VisualizeGrid();
            }
        }
        
        private void AddSampleItems()
        {
            Debug.Log("Adding sample items to inventory...");
            
            try {
                ItemData testItem = CreateTestItem("test_item", "Test Item", ItemCategory.Weapon);
                ItemInstance item = AddItemToContainer(testItem, "backpack", new Vector2Int(0, 0));
                if (item != null) {
                    Debug.Log($"Added test item to backpack at (0,0) with ID {item.instanceId}");
                } else {
                    Debug.LogError("Failed to add test item to backpack");
                }
                
                RefreshContainerUI("backpack");
                RefreshContainerUI("stash");
            }
            catch (Exception e) {
                Debug.LogError($"Error adding sample items: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private ItemData CreateTestItem(string id, string displayName, ItemCategory category)
        {
            ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
            itemData.id = id;
            itemData.displayName = displayName;
            itemData.description = "Test item for debugging";
            itemData.category = category;
            itemData.width = 2;
            itemData.height = 2;
            itemData.weight = 1.0f;
            itemData.canStack = false;
            itemData.canRotate = true;
            itemData.canEquip = category == ItemCategory.Weapon;
            
            Texture2D iconTexture = new Texture2D(64, 64);
            Color fillColor = category == ItemCategory.Weapon ? Color.red : Color.green;
            for (int x = 0; x < iconTexture.width; x++) {
                for (int y = 0; y < iconTexture.height; y++) {
                    iconTexture.SetPixel(x, y, fillColor);
                }
            }
            iconTexture.Apply();
            
            return itemData;
        }
        
        public ItemInstance AddItemToContainer(ItemData itemData, string containerId, Vector2Int position, bool rotated = false)
        {
            if (!_containers.TryGetValue(containerId, out ContainerInstance container))
            {
                Debug.LogError($"Container {containerId} not found");
                return null;
            }
            
            ItemInstance item = new ItemInstance(itemData, position, container);
            item.isRotated = rotated;
            
            if (container.CanPlaceItem(item, position))
            {
                container.AddItem(item, position);
                _items[item.instanceId] = item;
                
                if (_initialized)
                {
                    CreateItemUI(item, containerId);
                }
                
                return item;
            }
            else
            {
                Debug.LogWarning($"Cannot place item {itemData.displayName} at position {position} in container {containerId}");
            }
            
            return null;
        }
        
        private void CreateItemUI(ItemInstance item, string containerId)
        {
            Debug.Log($"CreateItemUI called for item: {item?.itemData?.displayName ?? "null"} (ID: {item?.instanceId ?? "null"}) in container: {containerId}");
            
            if (_root == null)
            {
                Debug.LogError("Root element is null - cannot create item UI");
                return;
            }
            
            VisualElement containerGrid = _root.Q($"{containerId}-grid");
            if (containerGrid == null)
            {
                Debug.LogError($"Container grid {containerId}-grid not found");
                return;
            }
            
            VisualElement existingElement = _root.Q(item.instanceId);
            if (existingElement != null)
            {
                Debug.Log($"Found existing element with ID {item.instanceId} - removing it first");
                existingElement.RemoveFromHierarchy();
            }
            
            Debug.Log($"About to create visual element for {item.itemData.displayName} at position ({item.position.x}, {item.position.y})");
            VisualElement itemElement = InventoryUIHelper.CreateItemVisualElement(item, itemTemplate);
            
            if (itemElement == null)
            {
                Debug.LogError($"Failed to create visual element for {item.itemData.displayName}");
                return;
            }
            
            itemElement.RegisterCallback<MouseDownEvent>(evt => {
                Debug.Log($"Direct click on item {item.itemData.displayName}");
                if (evt.button == 0)
                {
                    if (evt.clickCount == 2)
                    {
                        HandleItemDoubleClick(item);
                    }
                    else
                    {
                        StartDragItem(item, evt.mousePosition);
                    }
                    evt.StopPropagation();
                }
                else if (evt.button == 1)
                {
                    ShowItemContextMenu(item, evt.mousePosition);
                    evt.StopPropagation();
                }
                else if (evt.button == 2 && item.itemData.canRotate)
                {
                    RotateItem(item);
                    evt.StopPropagation();
                }
            });
            
            Debug.Log($"Adding item element to container grid");
            containerGrid.Add(itemElement);
            Debug.Log($"Successfully added {item.itemData.displayName} to {containerId}");
        }
        
        public void RefreshItemUI(ItemInstance item, string containerId)
        {
            if (_root == null) return;
            
            VisualElement existingElement = _root.Q(item.instanceId);
            if (existingElement != null)
            {
                existingElement.RemoveFromHierarchy();
            }
            
            CreateItemUI(item, containerId);
        }
        
        private void RotateItem(ItemInstance item)
        {
            if (item == null || !item.itemData.canRotate)
                return;
            
            item.ToggleRotation();
            
            if (item.container != null)
            {
                RefreshItemUI(item, item.container.containerData.id);
                onInventoryChanged?.Invoke();
            }
        }
    }
}