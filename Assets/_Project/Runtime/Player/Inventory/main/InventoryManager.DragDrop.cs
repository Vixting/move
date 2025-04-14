using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine.InputSystem;

namespace InventorySystem
{
    public partial class InventoryManager
    {
        private void StartDragItem(ItemInstance item, Vector2 mousePosition)
        {
            if (item == null)
            {
                Debug.LogError("StartDragItem called with null item");
                return;
            }
            
            Debug.Log($"StartDragItem: Beginning drag operation for {item.itemData.displayName} (ID: {item.instanceId})");
            
            _draggedItem = item;
            
            _draggedItemElement = InventoryUIHelper.CreateItemVisualElement(item, itemTemplate, true);
            if (_draggedItemElement == null)
            {
                Debug.LogError("Failed to create dragged item visual element");
                _draggedItem = null;
                return;
            }
            
            _draggedItemElement.AddToClassList("dragged-item");
            
            int cellSize = 50;
            UQueryBuilder<VisualElement> cellQuery = _root.Query(null, "grid-cell");
            if (cellQuery.First() != null)
            {
                cellSize = (int)cellQuery.First().resolvedStyle.width;
                Debug.Log($"Detected cell size: {cellSize}px");
            }
            
            int itemWidth = item.GetWidth() * cellSize;
            int itemHeight = item.GetHeight() * cellSize;
            
            _draggedItemElement.style.width = itemWidth;
            _draggedItemElement.style.height = itemHeight;
            
            _dragOffset = new Vector2(itemWidth / 2, itemHeight / 2);
            
            _root.Add(_draggedItemElement);
            
            _draggedItemElement.style.position = Position.Absolute;
            _draggedItemElement.style.left = mousePosition.x - _dragOffset.x;
            _draggedItemElement.style.top = mousePosition.y - _dragOffset.y;
            
            Debug.Log($"Dragged item element created: Position=({_draggedItemElement.style.left.value}, {_draggedItemElement.style.top.value}), Size={itemWidth}x{itemHeight}");
            
            if (item.container != null)
            {
                VisualElement existingElement = _root.Q(item.instanceId);
                if (existingElement != null)
                {
                    Debug.Log($"Setting original item element opacity to 0");
                    existingElement.style.opacity = 0;
                }
                else
                {
                    Debug.LogWarning($"Original item element with ID {item.instanceId} not found in the UI");
                }
                
                InventoryUIHelper.ClearCellHighlighting(_root);
            }
            
            Debug.Log($"StartDragItem completed for {item.itemData.displayName}");
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_draggedItem != null && _draggedItemElement != null)
            {
                Vector2 newPosition = evt.mousePosition - _dragOffset;
                _draggedItemElement.style.left = newPosition.x;
                _draggedItemElement.style.top = newPosition.y;
                
                if (_enableSnapping)
                {
                    if (_snapTimer <= 0)
                    {
                        SnapDraggedItemToGrid();
                        _snapTimer = _snapDelayTime;
                    }
                    else
                    {
                        _snapTimer -= Time.deltaTime;
                    }
                }
                
                if (UnityEngine.Random.value < 0.01f)
                {
                    Debug.Log($"Moving dragged item to: {newPosition}");
                }
            }
        }
        
        private void SnapDraggedItemToGrid()
        {
            if (_draggedItem == null || _draggedItemElement == null)
                return;
            
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            Vector2? closestCellPosition = null;
            float closestDistance = float.MaxValue;
            string closestContainerId = null;
            Vector2Int closestCellCoord = Vector2Int.zero;
            
            foreach (var container in _containers.Values)
            {
                VisualElement containerGrid = _root.Q($"{container.containerData.id}-grid");
                if (containerGrid == null)
                    continue;
                
                Rect containerRect = containerGrid.worldBound;
                if (!containerRect.Contains(mousePosition))
                    continue;
                
                for (int y = 0; y < container.height; y++)
                {
                    for (int x = 0; x < container.width; x++)
                    {
                        string cellName = $"cell_{container.containerData.id}_{x}_{y}";
                        VisualElement cell = containerGrid.Q(cellName);
                        if (cell == null)
                            continue;
                        
                        Vector2 cellCenter = new Vector2(
                            cell.worldBound.x + cell.worldBound.width / 2,
                            cell.worldBound.y + cell.worldBound.height / 2
                        );
                        
                        float distance = Vector2.Distance(mousePosition, cellCenter);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestCellPosition = cellCenter;
                            closestContainerId = container.containerData.id;
                            closestCellCoord = new Vector2Int(x, y);
                        }
                    }
                }
            }
            
            if (closestCellPosition.HasValue && !string.IsNullOrEmpty(closestContainerId))
            {
                _draggedItemElement.style.left = closestCellPosition.Value.x - _dragOffset.x;
                _draggedItemElement.style.top = closestCellPosition.Value.y - _dragOffset.y;
                
                UpdatePlacementHighlighting(closestContainerId, closestCellCoord);
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            Debug.Log($"Mouse up detected at position: {evt.mousePosition}");
            
            if (_draggedItem != null)
            {
                Debug.Log($"Dropping item: {_draggedItem.itemData.displayName}");
                DropItem(evt.mousePosition);
            }
        }
        
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.R && _draggedItem != null && _draggedItem.itemData.canRotate)
            {
                _draggedItem.ToggleRotation();
                
                if (_draggedItemElement != null)
                {
                    _draggedItemElement.RemoveFromHierarchy();
                    _draggedItemElement = InventoryUIHelper.CreateItemVisualElement(_draggedItem, itemTemplate, true);
                    _draggedItemElement.AddToClassList("dragged-item");
                    
                    Vector2 mousePosition = Mouse.current.position.ReadValue();
                    _draggedItemElement.style.left = mousePosition.x - _dragOffset.x;
                    _draggedItemElement.style.top = mousePosition.y - _dragOffset.y;
                    
                    _root.Add(_draggedItemElement);
                }
                
                evt.StopPropagation();
            }
        }
            
        private void DropItem(Vector2 mousePosition)
        {
            if (_draggedItem == null)
                return;
            
            Debug.Log($"Dropping item {_draggedItem.itemData.displayName} at position {mousePosition}");
            
            bool itemPlaced = false;
            
            if (_draggedItem.container != null)
            {
                VisualElement existingElement = _root.Q(_draggedItem.instanceId);
                if (existingElement != null)
                {
                    existingElement.style.opacity = 1;
                }
            }
            
            List<(VisualElement cell, string containerId, int x, int y, bool canPlace)> allCells = new List<(VisualElement, string, int, int, bool)>();
            
            foreach (var container in _containers.Values)
            {
                if (container.containerData.restrictedCategories.Contains(_draggedItem.itemData.category))
                    continue;
                    
                VisualElement containerGrid = _root.Q($"{container.containerData.id}-grid");
                if (containerGrid == null)
                    continue;
                
                for (int y = 0; y <= container.height - _draggedItem.GetHeight(); y++)
                {
                    for (int x = 0; x <= container.width - _draggedItem.GetWidth(); x++)
                    {
                        Vector2Int position = new Vector2Int(x, y);
                        bool canPlace = container.CanPlaceItem(_draggedItem, position);
                        
                        string cellName = $"cell_{container.containerData.id}_{x}_{y}";
                        VisualElement cell = containerGrid.Q(cellName);
                        if (cell != null)
                        {
                            allCells.Add((cell, container.containerData.id, x, y, canPlace));
                        }
                    }
                }
            }
            
            Debug.Log($"Found {allCells.Count} potential cells, filtering for valid placements");
            
            var validCells = allCells.Where(c => c.canPlace).ToList();
            Debug.Log($"Found {validCells.Count} valid cells for placement");
            
            if (_magneticDrop && validCells.Count > 0)
            {
                validCells.Sort((a, b) => {
                    Rect aRect = a.cell.worldBound;
                    Rect bRect = b.cell.worldBound;
                    float aDistance = Vector2.Distance(mousePosition, new Vector2(aRect.center.x, aRect.center.y));
                    float bDistance = Vector2.Distance(mousePosition, new Vector2(bRect.center.x, bRect.center.y));
                    return aDistance.CompareTo(bDistance);
                });
                
                var closestValidCell = validCells[0];
                float distance = Vector2.Distance(
                    mousePosition, 
                    new Vector2(
                        closestValidCell.cell.worldBound.center.x,
                        closestValidCell.cell.worldBound.center.y
                    )
                );
                
                if (distance <= _magneticDropDistance)
                {
                    Debug.Log($"Magnetic drop: Distance to closest valid cell: {distance:F2} (threshold: {_magneticDropDistance})");
                    Debug.Log($"Trying magnetic placement at {closestValidCell.containerId} ({closestValidCell.x}, {closestValidCell.y})");
                    
                    itemPlaced = TryPlaceItemAt(_draggedItem, closestValidCell.containerId, new Vector2Int(closestValidCell.x, closestValidCell.y));
                    
                    if (itemPlaced)
                    {
                        Debug.Log($"Successfully placed with magnetic drop at {closestValidCell.containerId} ({closestValidCell.x}, {closestValidCell.y})");
                    }
                }
                else
                {
                    Debug.Log($"Closest valid cell is too far for magnetic drop: {distance:F2} > {_magneticDropDistance}");
                }
            }
            
            if (!itemPlaced)
            {
                bool foundCellContainingMouse = false;
                foreach (var cellInfo in allCells)
                {
                    if (!cellInfo.canPlace)
                        continue;
                        
                    var cell = cellInfo.cell;
                    var containerId = cellInfo.containerId;
                    var x = cellInfo.x;
                    var y = cellInfo.y;
                    
                    Rect cellRect = cell.worldBound;
                    
                    if (cellRect.Contains(mousePosition))
                    {
                        foundCellContainingMouse = true;
                        Debug.Log($"Mouse is inside cell {cell.name}, bounds: {cellRect}");
                        
                        Debug.Log($"Trying to place at {containerId} ({x}, {y})");
                        itemPlaced = TryPlaceItemAt(_draggedItem, containerId, new Vector2Int(x, y));
                        
                        if (itemPlaced)
                        {
                            Debug.Log($"Successfully placed at {containerId} ({x}, {y})");
                            break;
                        }
                        else
                        {
                            Debug.Log($"Failed to place at {containerId} ({x}, {y})");
                        }
                    }
                }
                
                if (!foundCellContainingMouse && !itemPlaced)
                {
                    Debug.Log("Mouse was not inside any cell. Checking closest valid cells");
                    
                    int cellsToCheck = Math.Min(5, validCells.Count);
                    for (int i = 0; i < cellsToCheck; i++)
                    {
                        var cellInfo = validCells[i];
                        var cell = cellInfo.cell;
                        var containerId = cellInfo.containerId;
                        var x = cellInfo.x;
                        var y = cellInfo.y;
                        
                        Rect cellRect = cell.worldBound;
                        Debug.Log($"Checking close valid cell {i}: {cell.name}, distance: {Vector2.Distance(mousePosition, new Vector2(cellRect.center.x, cellRect.center.y))}");
                        
                        Debug.Log($"Trying to place at {containerId} ({x}, {y})");
                        itemPlaced = TryPlaceItemAt(_draggedItem, containerId, new Vector2Int(x, y));
                        
                        if (itemPlaced)
                        {
                            Debug.Log($"Successfully placed at {containerId} ({x}, {y})");
                            break;
                        }
                    }
                }
            }
            
            if (!itemPlaced)
            {
                UQueryBuilder<VisualElement> equipmentSlots = _root.Query(null, "equipment-slot");
                equipmentSlots.ForEach(slot => 
                {
                    if (itemPlaced)
                        return;
                    
                    Rect slotRect = slot.worldBound;
                    
                    Debug.Log($"Checking equipment slot {slot.name}, bounds: {slotRect}, contains mouse: {slotRect.Contains(mousePosition)}");
                    
                    if (slotRect.Contains(mousePosition))
                    {
                        string slotName = slot.name;
                        if (slotName.StartsWith("slot-"))
                        {
                            string slotType = slotName.Substring(5);
                            EquipmentSlot equipSlot = GetEquipmentSlotFromName(slotType);
                            
                            if (_draggedItem.CanEquipInSlot(equipSlot))
                            {
                                Debug.Log($"Trying to equip in slot {slotType}");
                                itemPlaced = EquipItem(_draggedItem, equipSlot);
                                if (itemPlaced)
                                {
                                    Debug.Log($"Successfully equipped in slot {slotType}");
                                }
                                else
                                {
                                    Debug.Log($"Failed to equip in slot {slotType}");
                                }
                            }
                            else
                            {
                                Debug.Log($"Item cannot be equipped in slot {slotType}");
                            }
                        }
                    }
                });
            }
            
            if (!itemPlaced)
            {
                Debug.Log("Item wasn't placed, returning to original position");
                
                if (_draggedItem.container != null)
                {
                    Debug.Log($"Refreshing item UI in original container {_draggedItem.container.containerData.id}");
                    RefreshItemUI(_draggedItem, _draggedItem.container.containerData.id);
                }
                else if (_character != null)
                {
                    foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                    {
                        if (_character.GetEquippedItem(slot) == _draggedItem)
                        {
                            _character.EquipItem(_draggedItem, slot);
                            break;
                        }
                    }
                }
            }
            
            if (_draggedItemElement != null)
            {
                _draggedItemElement.RemoveFromHierarchy();
                _draggedItemElement = null;
            }
            
            _draggedItem = null;
            InventoryUIHelper.ClearCellHighlighting(_root);
        }
    }
}