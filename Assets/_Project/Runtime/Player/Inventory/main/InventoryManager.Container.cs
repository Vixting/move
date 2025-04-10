using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager //InventoryManager.Container
    {
        private void CreateDefaultContainers()
        {
            Debug.Log("Creating default containers");
            CreateContainer("stash", "Stash", 10, 60);
            CreateContainer("backpack", "Backpack", 5, 5);
            CreateContainer("tactical-rig", "Tactical Rig", 4, 4);
            CreateContainer("pockets", "Pockets", 2, 2);
            
            SetupContainerUI("stash", "stash-grid");
            SetupContainerUI("backpack", "backpack-grid");
            SetupContainerUI("tactical-rig", "vest-grid");
            SetupContainerUI("pockets", "pockets-grid");
        }
        
        private ContainerInstance CreateContainer(string id, string displayName, int width, int height)
        {
            ContainerData containerData = new ContainerData
            {
                id = id,
                displayName = displayName,
                width = width,
                height = height
            };
            
            ContainerInstance container = new ContainerInstance(containerData);
            _containers[id] = container;
            
            Debug.Log($"Created container: {id} ({width}x{height})");
            return container;
        }
        
        private void SetupContainerUI(string containerId, string gridElementName)
        {
            if (_root == null || !_containers.TryGetValue(containerId, out ContainerInstance container))
            {
                Debug.LogWarning($"Cannot setup container UI for {containerId}. Root or container not found.");
                return;
            }
            
            VisualElement gridElement = _root.Q(gridElementName);
            if (gridElement == null)
            {
                Debug.LogWarning($"Grid element {gridElementName} not found");
                return;
            }
            
            InventoryUIHelper.CreateContainerGrid(
                gridElement, 
                container, 
                gridCellTemplate, 
                OnCellMouseDown, 
                OnCellMouseEnter,
                _showGridHelpers
            );
            
            RefreshContainerUI(containerId);
            Debug.Log($"Set up container UI for {containerId}");
        }
        
        private void OnCellMouseDown(MouseDownEvent evt, string containerId, Vector2Int cellPosition)
        {
            Debug.Log($"Cell clicked at {containerId} {cellPosition}");
            
            if (_contextMenu != null)
            {
                _contextMenu.RemoveFromHierarchy();
                _contextMenu = null;
                evt.StopPropagation();
                Debug.Log("Context menu was showing - removed it and stopped propagation");
                return;
            }
            
            if (!_containers.TryGetValue(containerId, out ContainerInstance container))
            {
                Debug.Log($"Container {containerId} not found");
                return;
            }
            
            container.LogAllItems();
            
            ItemInstance item = container.GetItemAt(cellPosition);
            
            Debug.Log($"GetItemAt result: {(item != null ? "Found item" : "No item found")} at {containerId} {cellPosition}");
            
            if (item != null)
            {
                Debug.Log($"Found item at cell: {item.itemData.displayName}, Button: {evt.button}, ClickCount: {evt.clickCount}");
                
                if (evt.button == 1)
                {
                    Debug.Log("Right click detected - showing context menu");
                    ShowItemContextMenu(item, evt.mousePosition);
                    evt.StopPropagation();
                    return;
                }
                
                if (evt.button == 2 && item.itemData.canRotate)
                {
                    Debug.Log("Middle click detected - rotating item");
                    RotateItem(item);
                    evt.StopPropagation();
                    return;
                }
                
                if (evt.button == 0)
                {
                    Debug.Log($"Left click detected on item: {item.itemData.displayName}");
                    if (evt.clickCount == 2)
                    {
                        Debug.Log($"Double click detected - handling item double click");
                        HandleItemDoubleClick(item);
                    }
                    else
                    {
                        Debug.Log($"Single click detected - attempting to start drag");
                        StartDragItem(item, evt.mousePosition);
                        Debug.Log($"After StartDragItem call for: {item.itemData.displayName}");
                    }
                    
                    evt.StopPropagation();
                }
            }
            else
            {
                Debug.Log($"No item found at {containerId} {cellPosition}");
                if (_draggedItem != null)
                {
                    Debug.Log($"Attempting to place dragged item at {containerId} {cellPosition}");
                    TryPlaceItemAt(_draggedItem, containerId, cellPosition);
                    evt.StopPropagation();
                }
            }
        }
        
        private void OnCellMouseEnter(MouseEnterEvent evt, string containerId, Vector2Int cellPosition)
        {
            if (_draggedItem == null)
                return;
            
            Debug.Log($"Mouse entered cell at {containerId} position {cellPosition}");
            UpdatePlacementHighlighting(containerId, cellPosition);
        }
        
        private void UpdatePlacementHighlighting(string containerId, Vector2Int cellPosition)
        {
            if (_draggedItem == null || !_containers.TryGetValue(containerId, out ContainerInstance container))
                return;
            
            Dictionary<Vector2Int, bool> cellHighlighting = container.GetCellHighlightingForItem(_draggedItem, cellPosition);
            
            InventoryUIHelper.UpdateCellHighlighting(_root, containerId, cellHighlighting);
        }
        
        private void RefreshContainerUI(string containerId)
        {
            Debug.Log($"RefreshContainerUI: Refreshing container {containerId}");
            
            if (!_containers.TryGetValue(containerId, out ContainerInstance container))
            {
                Debug.LogError($"RefreshContainerUI: Container {containerId} not found");
                return;
            }
            
            VisualElement containerGrid = _root.Q($"{containerId}-grid");
            if (containerGrid == null)
            {
                Debug.LogError($"RefreshContainerUI: Container grid element {containerId}-grid not found");
                return;
            }
            
            // Log the current container items
            Debug.Log($"Container {containerId} contains {container.GetAllItems().Count} items");
            container.LogAllItems();
            
            // Get all item elements currently in the UI
            List<VisualElement> itemElements = new List<VisualElement>();
            containerGrid.Query(null, "inventory-item").ForEach(el => itemElements.Add(el));
            Debug.Log($"Found {itemElements.Count} item elements in UI for container {containerId}");
            
            // Remove all existing item elements
            foreach (var element in itemElements)
            {
                Debug.Log($"Removing item element: {element.name}");
                element.RemoveFromHierarchy();
            }
            
            // Create UI for all items in the container
            foreach (var item in container.GetAllItems())
            {
                Debug.Log($"Creating UI for item {item.itemData.displayName} at position {item.position}");
                CreateItemUI(item, containerId);
            }
            
            Debug.Log($"Container {containerId} UI refresh complete");
        }
        
        public bool HasSpaceForItem(ItemData itemData, string containerId)
        {
            if (!_containers.TryGetValue(containerId, out ContainerInstance container))
            {
                return false;
            }
            
            ItemInstance dummyItem = new ItemInstance(itemData, Vector2Int.zero, container);
            return container.FindAvailablePosition(dummyItem) != null;
        }
    }
}