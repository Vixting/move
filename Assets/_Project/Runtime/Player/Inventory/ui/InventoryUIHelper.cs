using System;
using System.Collections.Generic;
using System.Reflection;
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
                        onCellMouseDown?.Invoke(evt, capturedContainerId, capturedPosition);
                    });
                    
                    if (onCellMouseEnter != null)
                    {
                        cell.RegisterCallback<MouseEnterEvent>(evt => onCellMouseEnter(evt, capturedContainerId, capturedPosition));
                    }
                    
                    gridElement.Add(cell);
                }
            }
        }
        
        public static VisualElement CreateItemVisualElement(ItemInstance item, VisualTreeAsset itemTemplate, bool isDragging = false)
        {
            if (item == null || itemTemplate == null || item.itemData == null)
                return null;
            
            int width = item.GetWidth() * CELL_SIZE;
            int height = item.GetHeight() * CELL_SIZE;
            
            VisualElement itemElement = itemTemplate.Instantiate().ElementAt(0);
            if (itemElement == null)
                return null;

            itemElement.name = item.instanceId;
            
            if (!itemElement.ClassListContains("inventory-item"))
                itemElement.AddToClassList("inventory-item");
            
            itemElement.style.position = Position.Absolute;
            itemElement.style.width = width;
            itemElement.style.height = height;
            
            if (!isDragging)
            {
                itemElement.style.left = item.position.x * CELL_SIZE;
                itemElement.style.top = item.position.y * CELL_SIZE;
            }
            
            VisualElement itemBorder = itemElement.Q("item-border");
            if (itemBorder == null)
                return null;
            
            VisualElement itemIcon = itemBorder.Q("item-icon");
            if (itemIcon == null)
                return null;
            
            itemIcon.Clear();
            
            Sprite itemSprite = null;
            bool hasSpriteProperty = false;
            
            if (item.itemData.icon != null)
            {
                itemSprite = item.itemData.icon;
                hasSpriteProperty = true;
            }
            else
            {
                try {
                    string[] propertyNames = new string[] { "sprite", "itemSprite", "image", "itemIcon" };
                    foreach (var propName in propertyNames) {
                        var property = item.itemData.GetType().GetProperty(propName);
                        if (property != null && property.PropertyType == typeof(Sprite)) {
                            itemSprite = property.GetValue(item.itemData) as Sprite;
                            if (itemSprite != null) {
                                hasSpriteProperty = true;
                                break;
                            }
                        }
                    }
                } catch (System.Exception) { }
            }
            
            if (hasSpriteProperty && itemSprite != null)
            {
                VisualElement spriteContainer = new VisualElement();
                spriteContainer.name = "sprite-container";
                spriteContainer.AddToClassList("sprite-container");
                
                spriteContainer.style.position = Position.Absolute;
                spriteContainer.style.left = 0;
                spriteContainer.style.top = 0;
                spriteContainer.style.right = 0;
                spriteContainer.style.bottom = 0;
                spriteContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                spriteContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                
                spriteContainer.style.paddingLeft = 2;
                spriteContainer.style.paddingRight = 2;
                spriteContainer.style.paddingTop = 2;
                spriteContainer.style.paddingBottom = 2;
                
                itemIcon.Add(spriteContainer);
                
                Image spriteImage = new Image();
                spriteImage.name = "item-sprite";
                spriteImage.AddToClassList("item-sprite");
                spriteImage.sprite = itemSprite;
                spriteImage.scaleMode = ScaleMode.ScaleToFit;
                spriteImage.tintColor = Color.white;
                
                spriteImage.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                spriteImage.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                spriteImage.style.position = Position.Absolute;
                spriteImage.style.left = 0;
                spriteImage.style.top = 0;
                spriteImage.style.right = 0;
                spriteImage.style.bottom = 0;
                
                spriteContainer.Add(spriteImage);
                
                ForceElementVisibility(spriteContainer);
                ForceElementVisibility(spriteImage);
            }
            else
            {
                CreateFallbackDisplay(itemIcon, item);
            }
            
            Label itemName = itemBorder.Q<Label>("item-name");
            if (itemName != null)
            {
                itemName.text = item.itemData.isExamined ? item.itemData.displayName : "Unknown Item";
                ForceElementVisibility(itemName);
            }
            
            Label stackCount = itemBorder.Q<Label>("stack-count");
            if (stackCount != null)
            {
                if (item.itemData.canStack && item.stackCount > 1)
                {
                    stackCount.text = item.stackCount.ToString();
                    stackCount.style.display = DisplayStyle.Flex;
                    ForceElementVisibility(stackCount);
                }
                else
                {
                    stackCount.style.display = DisplayStyle.None;
                }
            }
            
            if (itemBorder != null)
            {
                itemBorder.AddToClassList(GetRarityClass(item.itemData.rarity));
                
                switch (item.itemData.rarity)
                {
                    case ItemRarity.Common:
                        itemBorder.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                        break;
                    case ItemRarity.Uncommon:
                        itemBorder.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 0.2f));
                        break;
                    case ItemRarity.Rare:
                        itemBorder.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.5f));
                        break;
                    case ItemRarity.Epic:
                        itemBorder.style.backgroundColor = new StyleColor(new Color(0.5f, 0.2f, 0.5f));
                        break;
                    case ItemRarity.Legendary:
                        itemBorder.style.backgroundColor = new StyleColor(new Color(0.6f, 0.5f, 0.1f));
                        break;
                    default:
                        itemBorder.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                        break;
                }
            }
            
            if (item.itemData is WeaponItemData)
            {
                VisualElement ammoCounter = new VisualElement();
                ammoCounter.AddToClassList("ammo-counter");
                
                Label ammoText = new Label($"{item.currentAmmoCount}");
                ammoText.AddToClassList("ammo-text");
                ammoText.style.color = new StyleColor(Color.white);
                ForceElementVisibility(ammoText);
                
                ammoCounter.Add(ammoText);
                itemBorder.Add(ammoCounter);
                ForceElementVisibility(ammoCounter);
            }
            
            if (item.isRotated)
            {
                VisualElement rotationIndicator = new VisualElement();
                rotationIndicator.AddToClassList("rotation-indicator");
                itemBorder.Add(rotationIndicator);
                ForceElementVisibility(rotationIndicator);
            }
            
            Label sizeLabel = new Label($"{item.GetWidth()}x{item.GetHeight()}");
            sizeLabel.style.position = Position.Absolute;
            sizeLabel.style.bottom = 5;
            sizeLabel.style.left = 5;
            sizeLabel.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.7f));
            sizeLabel.style.color = new StyleColor(Color.yellow);
            sizeLabel.style.fontSize = 10;
            sizeLabel.style.paddingLeft = 3;
            sizeLabel.style.paddingRight = 3;
            itemBorder.Add(sizeLabel);
            ForceElementVisibility(sizeLabel);
            
            if (isDragging)
            {
                itemElement.style.opacity = 0.8f;
            }
            
            ForceOpacityOnElementAndChildren(itemElement);
            
            return itemElement;
        }

        private static void ForceElementVisibility(VisualElement element) 
        {
            if (element == null) return;
            
            element.style.opacity = 1;
            element.style.visibility = Visibility.Visible;
            element.style.display = DisplayStyle.Flex;
            
            if (element is Image image)
            {
                image.tintColor = Color.white;
            }
            
            if (element is TextElement textElement)
            {
                textElement.style.color = new StyleColor(Color.white);
            }
        }

        private static void CreateFallbackDisplay(VisualElement itemIcon, ItemInstance item)
        {
            TextElement itemDisplay = new TextElement();
            itemDisplay.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            itemDisplay.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            itemDisplay.style.fontSize = 14;
            itemDisplay.style.unityTextAlign = TextAnchor.MiddleCenter;
            itemDisplay.style.color = new StyleColor(Color.white);
            
            string itemDisplayText = item.itemData.displayName;
            if (itemDisplayText.Length > 3)
            {
                itemDisplayText = itemDisplayText.Substring(0, 3);
            }
            
            itemDisplay.text = itemDisplayText.ToUpper();
            itemIcon.Add(itemDisplay);
            ForceElementVisibility(itemDisplay);
        }
        
        private static void ForceOpacityOnElementAndChildren(VisualElement element)
        {
            if (element == null)
                return;
                
            element.style.opacity = 1;
            element.style.visibility = Visibility.Visible;
            element.style.display = DisplayStyle.Flex;
            
            element.pickingMode = PickingMode.Position;
            
            if (element is Image image)
            {
                image.tintColor = Color.white;
            }
            
            if (element is TextElement textElement)
            {
                textElement.style.color = new StyleColor(Color.white);
            }
            
            foreach (var child in element.Children())
            {
                ForceOpacityOnElementAndChildren(child);
            }
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
        
        public static VisualElement CreateItemContextMenu(ItemInstance item, Vector2 position, System.Action<ItemInstance> onUse, System.Action<ItemInstance> onDiscard, System.Action<ItemInstance> onSplit, System.Action<ItemInstance> onFold, System.Action<ItemInstance> onExamine, System.Action<ItemInstance> onDropToWorld = null)
        {
            VisualElement menu = new VisualElement();
            menu.name = "item-context-menu";
            menu.AddToClassList("context-menu");
            
            Vector2 adjustedPosition = position;
            
            if (adjustedPosition.x > Screen.width - 200)
            {
                adjustedPosition.x = Screen.width - 200;
            }
            
            if (adjustedPosition.y > Screen.height - 300)
            {
                adjustedPosition.y = Screen.height - 300;
            }
            
            menu.style.position = Position.Absolute;
            menu.style.left = adjustedPosition.x;
            menu.style.top = adjustedPosition.y;
            menu.style.width = 180;
            menu.style.backgroundColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f, 0.95f));
            menu.style.paddingTop = 5;
            menu.style.paddingBottom = 5;
            
            Label nameLabel = new Label(item.itemData.displayName);
            nameLabel.AddToClassList("context-menu-header");
            nameLabel.style.display = DisplayStyle.Flex;
            nameLabel.style.visibility = Visibility.Visible;
            nameLabel.style.opacity = 1;
            nameLabel.style.color = new StyleColor(new Color(1f, 0.8f, 0.4f));
            nameLabel.style.paddingLeft = 10;
            nameLabel.style.paddingRight = 10;
            nameLabel.style.paddingTop = 5;
            nameLabel.style.paddingBottom = 5;
            nameLabel.style.fontSize = 14;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            
            menu.Add(nameLabel);
            
            VisualElement separator = new VisualElement();
            separator.style.height = 1;
            separator.style.marginTop = 2;
            separator.style.marginBottom = 2;
            separator.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f));
            menu.Add(separator);
            
            if (item.itemData.needsExamination && !item.itemData.isExamined)
            {
                AddContextButton(menu, "Examine", () => onExamine?.Invoke(item));
            }
            
            if (item.itemData.canUse)
            {
                AddContextButton(menu, "Use", () => onUse?.Invoke(item));
            }
            
            if (item.itemData is WeaponItemData weaponData && weaponData.foldable)
            {
                AddContextButton(menu, weaponData.folded ? "Unfold" : "Fold", () => onFold?.Invoke(item));
            }
            
            if (item.itemData.canStack && item.stackCount > 1)
            {
                AddContextButton(menu, "Split Stack", () => onSplit?.Invoke(item));
            }
            
            if (onDropToWorld != null && item.itemData.prefab != null)
            {
                AddContextButton(menu, "Drop to World", () => onDropToWorld?.Invoke(item));
            }
            
            VisualElement btnDiscard = AddContextButton(menu, "Discard", () => onDiscard?.Invoke(item));
            btnDiscard.AddToClassList("discard-button");
            btnDiscard.style.color = new StyleColor(new Color(0.9f, 0.3f, 0.3f));
            
            ForceElementVisibility(menu);
            
            return menu;
        }

        private static VisualElement AddContextButton(VisualElement menu, string text, System.Action onClick)
        {
            VisualElement button = new VisualElement();
            button.AddToClassList("context-menu-button");
            button.style.display = DisplayStyle.Flex;
            button.style.visibility = Visibility.Visible;
            button.style.opacity = 1;
            button.style.paddingLeft = 10;
            button.style.paddingRight = 10;
            button.style.paddingTop = 6;
            button.style.paddingBottom = 6;
            button.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0f));

            
            Label label = new Label(text);
            label.style.display = DisplayStyle.Flex;
            label.style.visibility = Visibility.Visible;
            label.style.opacity = 1;
            label.style.color = new StyleColor(Color.white);
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.fontSize = 12;
            
            button.Add(label);
            
            button.RegisterCallback<MouseEnterEvent>(evt => {
                button.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f));
            });
            
            button.RegisterCallback<MouseLeaveEvent>(evt => {
                button.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0f));
            });
            
            button.RegisterCallback<MouseDownEvent>(evt => {
                onClick?.Invoke();
                if (menu != null && menu.parent != null)
                {
                    menu.RemoveFromHierarchy();
                }
                evt.StopPropagation();
            });
            
            menu.Add(button);
            return button;
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