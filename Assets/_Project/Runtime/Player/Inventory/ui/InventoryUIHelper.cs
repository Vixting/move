using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace InventorySystem
{
    public static class InventoryUIHelper
    {
        private const int CELL_SIZE = 50;
        
        public static void CreateContainerGrid(VisualElement gridElement, ContainerInstance container, VisualTreeAsset gridCellTemplate, System.Action<MouseDownEvent, string, Vector2Int> onCellMouseDown, System.Action<MouseEnterEvent, string, Vector2Int> onCellMouseEnter, bool showHelpers = true)
        {
            Debug.Log($"Creating grid for container: {container?.containerData?.id ?? "null"}");
            
            if (gridElement == null || container == null || gridCellTemplate == null)
                return;
            
            gridElement.Clear();
            
            gridElement.style.width = container.width * CELL_SIZE;
            gridElement.style.height = container.height * CELL_SIZE;
            
            for (int y = 0; y < container.height; y++)
            {
                for (int x = 0; x < container.width; x++)
                {
                    VisualElement cell = gridCellTemplate.Instantiate();
                    cell.name = $"cell_{container.containerData.id}_{x}_{y}";
                    cell.AddToClassList("grid-cell");
                    
                    if (showHelpers)
                    {
                        cell.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.4f, 0.2f));
                    }
                    
                    cell.style.position = Position.Absolute;
                    cell.style.left = x * CELL_SIZE;
                    cell.style.top = y * CELL_SIZE;
                    cell.style.width = CELL_SIZE;
                    cell.style.height = CELL_SIZE;
                    
                    Vector2Int cellPosition = new Vector2Int(x, y);
                    cell.userData = cellPosition;
                    
                    cell.pickingMode = PickingMode.Position;
                    
                    Vector2Int capturedPosition = cellPosition;
                    string capturedContainerId = container.containerData.id;
                    
                    if (showHelpers)
                    {
                        Label debugLabel = new Label($"{x},{y}");
                        debugLabel.style.fontSize = 8;
                        debugLabel.style.color = new StyleColor(Color.white);
                        debugLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        cell.Add(debugLabel);
                    }
                    
                    cell.RegisterCallback<MouseDownEvent>(evt => {
                        Debug.Log($"Direct cell click at {capturedContainerId} {capturedPosition}");
                        onCellMouseDown?.Invoke(evt, capturedContainerId, capturedPosition);
                    });
                    
                    if (onCellMouseEnter != null)
                    {
                        cell.RegisterCallback<MouseEnterEvent>(evt => onCellMouseEnter(evt, capturedContainerId, capturedPosition));
                    }
                    
                    gridElement.Add(cell);
                }
            }
            
            Debug.Log($"Container grid created with {container.width * container.height} cells");
        }
        
        public static VisualElement CreateItemVisualElement(ItemInstance item, VisualTreeAsset itemTemplate, bool isDragging = false)
        {
            if (item == null || itemTemplate == null)
                return null;
            
            if (item.itemData == null)
                return null;
            
            int width = item.GetWidth() * CELL_SIZE;
            int height = item.GetHeight() * CELL_SIZE;
            
            Debug.Log($"Creating item visual element: {item.itemData.displayName}, Size: {item.GetWidth()}x{item.GetHeight()} cells, Pixels: {width}x{height}");
            
            VisualElement itemElement = itemTemplate.Instantiate();
            itemElement.name = item.instanceId;
            
            itemElement.style.position = Position.Absolute;
            
            if (!isDragging)
            {
                itemElement.style.left = item.position.x * CELL_SIZE;
                itemElement.style.top = item.position.y * CELL_SIZE;
            }
            
            itemElement.style.width = width;
            itemElement.style.height = height;
                        
            VisualElement itemBorder = itemElement.Q("item-border");
            if (itemBorder == null)
            {
                itemBorder = new VisualElement();
                itemBorder.name = "item-border";
                itemBorder.AddToClassList("item-border");
                itemElement.Add(itemBorder);
            }
            
            itemBorder.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            itemBorder.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            itemBorder.pickingMode = PickingMode.Position;
            
            VisualElement itemIcon = itemBorder.Q("item-icon");
            if (itemIcon == null)
            {
                itemIcon = new VisualElement();
                itemIcon.name = "item-icon";
                itemIcon.AddToClassList("item-icon");
                itemBorder.Add(itemIcon);
            }
            
            itemIcon.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            itemIcon.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            itemIcon.pickingMode = PickingMode.Position;
            
            Label itemName = itemBorder.Q<Label>("item-name");
            if (itemName == null)
            {
                itemName = new Label();
                itemName.name = "item-name";
                itemName.AddToClassList("item-name");
                itemBorder.Add(itemName);
            }
            
            Label stackCount = itemBorder.Q<Label>("stack-count");
            if (stackCount == null)
            {
                stackCount = new Label();
                stackCount.name = "stack-count";
                stackCount.AddToClassList("stack-count");
                itemBorder.Add(stackCount);
            }
            
            itemBorder.AddToClassList(GetRarityClass(item.itemData.rarity));
            
            if (item.itemData.needsExamination && !item.itemData.isExamined)
            {
                itemBorder.AddToClassList("unexamined-item");
            }
            
            itemIcon.Clear();
            
            TextElement itemDisplay = new TextElement();
            itemDisplay.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            itemDisplay.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            itemDisplay.style.fontSize = 14;
            itemDisplay.style.unityTextAlign = TextAnchor.MiddleCenter;
            itemDisplay.style.color = Color.white;
            
            string itemDisplayText = item.itemData.displayName;
            if (itemDisplayText.Length > 3)
            {
                itemDisplayText = itemDisplayText.Substring(0, 3);
            }
            
            itemDisplay.text = itemDisplayText.ToUpper();
            
            switch (item.itemData.rarity)
            {
                case ItemRarity.Common:
                    itemIcon.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                    break;
                case ItemRarity.Uncommon:
                    itemIcon.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f));
                    break;
                case ItemRarity.Rare:
                    itemIcon.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.5f));
                    break;
                case ItemRarity.Epic:
                    itemIcon.style.backgroundColor = new StyleColor(new Color(0.5f, 0.2f, 0.5f));
                    break;
                case ItemRarity.Legendary:
                    itemIcon.style.backgroundColor = new StyleColor(new Color(0.6f, 0.5f, 0.1f));
                    break;
                default:
                    itemIcon.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                    break;
            }
            
            itemIcon.Add(itemDisplay);
            
            itemName.text = item.itemData.isExamined ? item.itemData.displayName : "Unknown Item";
            
            if (item.itemData.canStack && item.stackCount > 1)
            {
                stackCount.text = item.stackCount.ToString();
                stackCount.style.display = DisplayStyle.Flex;
            }
            else
            {
                stackCount.style.display = DisplayStyle.None;
            }
            
            if (item.itemData is WeaponItemData)
            {
                VisualElement ammoCounter = new VisualElement();
                ammoCounter.AddToClassList("ammo-counter");
                
                Label ammoText = new Label($"{item.currentAmmoCount}");
                ammoText.AddToClassList("ammo-text");
                
                ammoCounter.Add(ammoText);
                itemBorder.Add(ammoCounter);
            }
            
            if (item.isRotated)
            {
                VisualElement rotationIndicator = new VisualElement();
                rotationIndicator.AddToClassList("rotation-indicator");
                itemBorder.Add(rotationIndicator);
            }
            
            itemElement.AddToClassList("inventory-item");
            
            if (isDragging)
            {
                itemElement.style.opacity = 0.8f;
            }
            
            Debug.Log($"Item element created with dimensions: {width}x{height} pixels at position ({itemElement.style.left.value}, {itemElement.style.top.value})");
            
            return itemElement;
        }
        
        public static void UpdateCellHighlighting(VisualElement root, string containerId, Dictionary<Vector2Int, bool> cellStates)
        {
            if (root == null)
                return;
            
            ClearCellHighlighting(root);
            
            bool canPlace = true;
            
            foreach (var state in cellStates.Values)
            {
                if (!state)
                {
                    canPlace = false;
                    break;
                }
            }
            
            foreach (var cellState in cellStates)
            {
                string cellSelector = $"cell_{containerId}_{cellState.Key.x}_{cellState.Key.y}";
                VisualElement cell = root.Q(cellSelector);
                
                if (cell != null)
                {
                    if (canPlace)
                    {
                        cell.style.backgroundColor = new StyleColor(new Color(0.0f, 0.8f, 0.0f, 0.4f));
                        cell.AddToClassList("valid-placement");
                    }
                    else
                    {
                        cell.style.backgroundColor = new StyleColor(new Color(0.8f, 0.0f, 0.0f, 0.4f));
                        cell.AddToClassList("invalid-placement");
                    }
                }
                else
                {
                    Debug.LogWarning($"Cell not found: {cellSelector}");
                }
            }
        }
        
        public static void ClearCellHighlighting(VisualElement root)
        {
            if (root == null)
                return;
                
            UQueryBuilder<VisualElement> cells = root.Query(null, "grid-cell");
            cells.ForEach(cell => 
            {
                cell.RemoveFromClassList("valid-placement");
                cell.RemoveFromClassList("invalid-placement");
                
                // Reset the background color for all cells
                cell.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.4f, 0.2f));
            });
        }
        
        public static Vector2Int? FindClosestValidCell(VisualElement root, ContainerInstance container, ItemInstance item, Vector2 mousePosition, float maxDistance = 150f)
        {
            if (root == null || container == null || item == null)
                return null;
                
            Vector2Int? closestValidPos = null;
            float closestDistance = float.MaxValue;
            
            for (int y = 0; y <= container.height - item.GetHeight(); y++)
            {
                for (int x = 0; x <= container.width - item.GetWidth(); x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    if (container.CanPlaceItem(item, pos))
                    {
                        VisualElement cell = root.Q($"cell_{container.containerData.id}_{x}_{y}");
                        if (cell != null)
                        {
                            Vector2 cellCenter = new Vector2(
                                cell.worldBound.x + cell.worldBound.width / 2,
                                cell.worldBound.y + cell.worldBound.height / 2
                            );
                            
                            float distance = Vector2.Distance(mousePosition, cellCenter);
                            
                            if (distance < closestDistance && distance <= maxDistance)
                            {
                                closestDistance = distance;
                                closestValidPos = pos;
                            }
                        }
                    }
                }
            }
            
            return closestValidPos;
        }
        
        public static void HighlightValidPlacementArea(VisualElement root, string containerId, Vector2Int position, int width, int height, bool isValid)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    VisualElement cell = root.Q($"cell_{containerId}_{position.x + x}_{position.y + y}");
                    if (cell != null)
                    {
                        if (isValid)
                        {
                            cell.style.backgroundColor = new StyleColor(new Color(0.0f, 0.8f, 0.0f, 0.4f));
                            cell.AddToClassList("valid-placement");
                        }
                        else
                        {
                            cell.style.backgroundColor = new StyleColor(new Color(0.8f, 0.0f, 0.0f, 0.4f));
                            cell.AddToClassList("invalid-placement");
                        }
                    }
                }
            }
        }
        
        public static VisualElement CreateItemContextMenu(ItemInstance item, Vector2 position, System.Action<ItemInstance> onUse, System.Action<ItemInstance> onDiscard, System.Action<ItemInstance> onSplit, System.Action<ItemInstance> onFold, System.Action<ItemInstance> onExamine)
        {
            VisualElement menu = new VisualElement();
            menu.name = "item-context-menu";
            menu.AddToClassList("context-menu");
            menu.style.left = position.x;
            menu.style.top = position.y;
            
            Label nameLabel = new Label(item.itemData.displayName);
            nameLabel.AddToClassList("context-menu-header");
            menu.Add(nameLabel);
            
            if (item.itemData.needsExamination && !item.itemData.isExamined)
            {
                Button examineButton = new Button(() => {
                    onExamine?.Invoke(item);
                    menu.RemoveFromHierarchy();
                });
                examineButton.text = "Examine";
                examineButton.AddToClassList("context-menu-button");
                menu.Add(examineButton);
            }
            
            if (item.itemData.canUse)
            {
                Button useButton = new Button(() => {
                    onUse?.Invoke(item);
                    menu.RemoveFromHierarchy();
                });
                useButton.text = "Use";
                useButton.AddToClassList("context-menu-button");
                menu.Add(useButton);
            }
            
            if (item.itemData is WeaponItemData weaponData && weaponData.foldable)
            {
                Button foldButton = new Button(() => {
                    onFold?.Invoke(item);
                    menu.RemoveFromHierarchy();
                });
                foldButton.text = weaponData.folded ? "Unfold" : "Fold";
                foldButton.AddToClassList("context-menu-button");
                menu.Add(foldButton);
            }
            
            if (item.itemData.canStack && item.stackCount > 1)
            {
                Button splitButton = new Button(() => {
                    onSplit?.Invoke(item);
                    menu.RemoveFromHierarchy();
                });
                splitButton.text = "Split Stack";
                splitButton.AddToClassList("context-menu-button");
                menu.Add(splitButton);
            }
            
            Button discardButton = new Button(() => {
                onDiscard?.Invoke(item);
                menu.RemoveFromHierarchy();
            });
            discardButton.text = "Discard";
            discardButton.AddToClassList("context-menu-button");
            discardButton.AddToClassList("discard-button");
            menu.Add(discardButton);
            
            return menu;
        }
        
        private static string GetRarityClass(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return "common-item";
                case ItemRarity.Uncommon:
                    return "uncommon-item";
                case ItemRarity.Rare:
                    return "rare-item";
                case ItemRarity.Epic:
                    return "epic-item";
                case ItemRarity.Legendary:
                    return "legendary-item";
                default:
                    return "common-item";
            }
        }
    }
}