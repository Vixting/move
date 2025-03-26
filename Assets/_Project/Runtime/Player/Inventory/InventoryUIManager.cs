using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class InventoryUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument inventoryDocument;
    [SerializeField] private VisualTreeAsset gridCellTemplate;
    [SerializeField] private VisualTreeAsset itemTemplate;
    [SerializeField] private StyleSheet inventoryStyleSheet;
    [SerializeField] private Sprite placeholderSprite;
    
    private VisualElement rootElement;
    private VisualElement playerInventoryPanel;
    private VisualElement stashPanel;
    private VisualElement equipmentPanel;
    private VisualElement itemInfoPanel;
    private VisualElement draggedItemElement;
    
    private Dictionary<string, VisualElement> containerGrids = new Dictionary<string, VisualElement>();
    private Dictionary<string, VisualElement> itemElements = new Dictionary<string, VisualElement>();
    private Dictionary<EquipmentSlot, VisualElement> equipmentSlotElements = new Dictionary<EquipmentSlot, VisualElement>();
    
    private ItemInstance draggedItem;
    private Vector2 dragOffset;
    private ContainerInstance sourceContainer;
    private Vector2Int originalPosition;
    private bool isInventoryVisible = false;
    
    private void Awake()
    {
        if (inventoryDocument != null)
        {
            inventoryDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
    
    public void SetInventoryUIReferences(
        UIDocument document,
        VisualTreeAsset gridTemplate,
        VisualTreeAsset itemTemplate,
        StyleSheet styleSheet,
        Sprite placeholder)
    {
        inventoryDocument = document;
        gridCellTemplate = gridTemplate;
        itemTemplate = itemTemplate;
        inventoryStyleSheet = styleSheet;
        placeholderSprite = placeholder;
    }
    
    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged.AddListener(RefreshInventoryUI);
            InventoryManager.Instance.onItemAdded.AddListener(OnItemAdded);
            InventoryManager.Instance.onItemRemoved.AddListener(OnItemRemoved);
            InventoryManager.Instance.onItemMoved.AddListener(OnItemMoved);
            InventoryManager.Instance.onItemEquipped.AddListener(OnItemEquipped);
            InventoryManager.Instance.onItemUnequipped.AddListener(OnItemUnequipped);
        }
    }
    
    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged.RemoveListener(RefreshInventoryUI);
            InventoryManager.Instance.onItemAdded.RemoveListener(OnItemAdded);
            InventoryManager.Instance.onItemRemoved.RemoveListener(OnItemRemoved);
            InventoryManager.Instance.onItemMoved.RemoveListener(OnItemMoved);
            InventoryManager.Instance.onItemEquipped.RemoveListener(OnItemEquipped);
            InventoryManager.Instance.onItemUnequipped.RemoveListener(OnItemUnequipped);
        }
    }
    
    private void Start()
    {
        SetupUI();
    }
    
    public void ToggleInventory(InputAction.CallbackContext context)
    {
        Debug.Log($"[INVENTORY] Toggle called. Current state: {isInventoryVisible}");
        isInventoryVisible = !isInventoryVisible;
        
        if (isInventoryVisible)
        {
            Debug.Log("[INVENTORY] Showing inventory");
            ShowInventory();
        }
        else
        {
            Debug.Log("[INVENTORY] Hiding inventory");
            HideInventory();
        }
        
        var player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.EnableGameplayMode(!isInventoryVisible);
        }
    }
    
    private void ShowInventory()
    {
        if (inventoryDocument != null)
        {
            Debug.Log($"[INVENTORY] Document exists, setting display to Flex");
            
            if (inventoryDocument.rootVisualElement != null)
            {
                inventoryDocument.rootVisualElement.style.display = DisplayStyle.Flex;
                
                // Check if panels exist
                Debug.Log($"[INVENTORY] Player inventory panel exists: {playerInventoryPanel != null}");
                Debug.Log($"[INVENTORY] Stash panel exists: {stashPanel != null}");
                Debug.Log($"[INVENTORY] Equipment panel exists: {equipmentPanel != null}");
                
                // Set some easily visible styles
                if (rootElement != null)
                {
                    rootElement.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
                    rootElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                    rootElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                }
                
                RefreshInventoryUI();
            }
            else
            {
                Debug.LogError("[INVENTORY] Root visual element is null!");
            }
        }
        else
        {
            Debug.LogError("[INVENTORY] Inventory document is null!");
        }
    }
    
    private void HideInventory()
    {
        if (inventoryDocument != null)
        {
            inventoryDocument.rootVisualElement.style.display = DisplayStyle.None;
            CancelDrag();
        }
    }
    
    public void SetupUI()
    {
        if (inventoryDocument == null) return;
        
        rootElement = inventoryDocument.rootVisualElement;
        
        if (inventoryStyleSheet != null)
        {
            rootElement.styleSheets.Add(inventoryStyleSheet);
        }
        
        playerInventoryPanel = rootElement.Q<VisualElement>("player-inventory-panel");
        stashPanel = rootElement.Q<VisualElement>("stash-panel");
        equipmentPanel = rootElement.Q<VisualElement>("equipment-panel");
        itemInfoPanel = rootElement.Q<VisualElement>("item-info-panel");
        
        if (InventoryManager.Instance != null)
        {
            SetupContainerGrid(InventoryManager.Instance.playerBackpack, "backpack-grid");
            SetupContainerGrid(InventoryManager.Instance.playerPockets, "pockets-grid");
            SetupContainerGrid(InventoryManager.Instance.playerVest, "vest-grid");
            SetupContainerGrid(InventoryManager.Instance.playerStash, "stash-grid");
        }
        
        SetupEquipmentSlots();
        
        Button closeButton = rootElement.Q<Button>("close-button");
        if (closeButton != null)
        {
            closeButton.clicked += () => {
                HideInventory();
            };
        }
        
        draggedItemElement = new VisualElement();
        draggedItemElement.AddToClassList("dragged-item");
        draggedItemElement.style.position = Position.Absolute;
        draggedItemElement.style.visibility = Visibility.Hidden;
        rootElement.Add(draggedItemElement);
    }
    
    private void SetupContainerGrid(ContainerInstance container, string gridName)
    {
        if (container == null) return;
        
        VisualElement gridContainer = rootElement.Q<VisualElement>(gridName);
        if (gridContainer == null)
        {
            Debug.LogWarning($"Could not find grid element with name: {gridName}");
            return;
        }
        
        gridContainer.Clear();
        
        for (int y = 0; y < container.height; y++)
        {
            for (int x = 0; x < container.width; x++)
            {
                VisualElement cell = gridCellTemplate.Instantiate();
                cell.AddToClassList("grid-cell");
                
                Vector2Int cellPos = new Vector2Int(x, y);
                cell.userData = cellPos;
                
                cell.style.width = 50;
                cell.style.height = 50;
                
                cell.style.position = Position.Absolute;
                cell.style.left = x * 50;
                cell.style.top = y * 50;
                
                RegisterCellDropHandlers(cell, container);
                
                gridContainer.Add(cell);
            }
        }
        
        containerGrids[container.instanceId] = gridContainer;
        
        gridContainer.style.width = container.width * 50;
        gridContainer.style.height = container.height * 50;
    }
    
    private void SetupEquipmentSlots()
    {
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            string slotName = $"slot-{slot.ToString().ToLower()}";
            VisualElement slotElement = rootElement.Q<VisualElement>(slotName);
            
            if (slotElement != null)
            {
                equipmentSlotElements[slot] = slotElement;
                RegisterEquipmentSlotDropHandlers(slotElement, slot);
            }
        }
    }
    
    private void RegisterCellDropHandlers(VisualElement cell, ContainerInstance container)
    {
        cell.RegisterCallback<PointerDownEvent>(evt => {
            Vector2Int cellPos = (Vector2Int)cell.userData;
            
            foreach (var item in container.items.Values)
            {
                if (IsPositionWithinItem(cellPos, item))
                {
                    StartDragItem(item, evt.localPosition);
                    evt.StopPropagation();
                    return;
                }
            }
        });
        
        cell.RegisterCallback<PointerUpEvent>(evt => {
            if (draggedItem != null)
            {
                Vector2Int cellPos = (Vector2Int)cell.userData;
                TryPlaceItem(draggedItem, container, cellPos);
                evt.StopPropagation();
            }
        });
    }
    
    private void RegisterEquipmentSlotDropHandlers(VisualElement slotElement, EquipmentSlot slot)
    {
        slotElement.RegisterCallback<PointerDownEvent>(evt => {
            if (InventoryManager.Instance == null) return;
            
            var equippedItem = InventoryManager.Instance.equippedItems[slot];
            if (equippedItem != null)
            {
                StartDragItem(equippedItem, evt.localPosition);
                evt.StopPropagation();
            }
        });
        
        slotElement.RegisterCallback<PointerUpEvent>(evt => {
            if (draggedItem != null && InventoryManager.Instance != null)
            {
                if (InventoryManager.Instance.EquipItem(draggedItem, slot))
                {
                    EndDrag();
                }
                else
                {
                    CancelDrag();
                }
                evt.StopPropagation();
            }
        });
    }
    
    private bool IsPositionWithinItem(Vector2Int position, ItemInstance item)
    {
        int startX = item.position.x;
        int startY = item.position.y;
        int endX = startX + (item.isRotated ? item.itemData.height : item.itemData.width);
        int endY = startY + (item.isRotated ? item.itemData.width : item.itemData.height);
        
        return position.x >= startX && position.x < endX && 
               position.y >= startY && position.y < endY;
    }
    
    private void StartDragItem(ItemInstance item, Vector2 localClickPosition)
    {
        draggedItem = item;
        sourceContainer = item.container;
        originalPosition = item.position;
        
        Vector2 itemPosition = new Vector2(
            item.position.x * 50,
            item.position.y * 50
        );
        dragOffset = localClickPosition - itemPosition;
        
        if (item.container != null)
        {
            item.container.RemoveItem(item);
        }
        else if (InventoryManager.Instance != null)
        {
            foreach (var pair in InventoryManager.Instance.equippedItems)
            {
                if (pair.Value == item)
                {
                    InventoryManager.Instance.UnequipItem(pair.Key);
                    break;
                }
            }
        }
        
        draggedItemElement.Clear();
        
        VisualElement itemVisual = CreateItemVisual(item);
        draggedItemElement.Add(itemVisual);
        
        draggedItemElement.style.visibility = Visibility.Visible;
        
        rootElement.RegisterCallback<PointerMoveEvent>(OnDragMove);
        rootElement.RegisterCallback<PointerUpEvent>(OnDragEnd);
        
        draggedItemElement.RegisterCallback<KeyDownEvent>(evt => {
            if (evt.keyCode == KeyCode.R && draggedItem.itemData.rotatable)
            {
                draggedItem.Rotate();
                
                draggedItemElement.Clear();
                VisualElement rotatedItemVisual = CreateItemVisual(draggedItem);
                draggedItemElement.Add(rotatedItemVisual);
                
                evt.StopPropagation();
            }
        });
        
        draggedItemElement.Focus();
    }
    
    private void OnDragMove(PointerMoveEvent evt)
    {
        if (draggedItem == null) return;
        
        draggedItemElement.style.left = evt.localPosition.x - dragOffset.x;
        draggedItemElement.style.top = evt.localPosition.y - dragOffset.y;
    }
    
    private void OnDragEnd(PointerUpEvent evt)
    {
        CancelDrag();
    }
    
    private void TryPlaceItem(ItemInstance item, ContainerInstance targetContainer, Vector2Int targetPos)
    {
        if (InventoryManager.Instance == null) return;
        
        ItemInstance existingItem = null;
        foreach (var containerItem in targetContainer.items.Values)
        {
            if (IsPositionWithinItem(targetPos, containerItem))
            {
                existingItem = containerItem;
                break;
            }
        }
        
        if (existingItem != null)
        {
            if (InventoryManager.Instance.SwapItems(item, existingItem))
            {
                EndDrag();
            }
            else
            {
                CancelDrag();
            }
        }
        else
        {
            if (InventoryManager.Instance.MoveItem(item, targetContainer, targetPos))
            {
                EndDrag();
            }
            else
            {
                CancelDrag();
            }
        }
    }
    
    private void EndDrag()
    {
        draggedItemElement.style.visibility = Visibility.Hidden;
        
        draggedItem = null;
        sourceContainer = null;
        
        rootElement.UnregisterCallback<PointerMoveEvent>(OnDragMove);
        rootElement.UnregisterCallback<PointerUpEvent>(OnDragEnd);
        
        RefreshInventoryUI();
    }
    
    private void CancelDrag()
    {
        if (draggedItem != null && InventoryManager.Instance != null)
        {
            if (sourceContainer != null)
            {
                sourceContainer.AddItem(draggedItem, originalPosition);
            }
            else
            {
                if (!InventoryManager.Instance.playerBackpack.AddItem(draggedItem))
                {
                    if (!InventoryManager.Instance.playerPockets.AddItem(draggedItem))
                    {
                        if (!InventoryManager.Instance.playerVest.AddItem(draggedItem))
                        {
                            InventoryManager.Instance.playerStash.AddItem(draggedItem);
                        }
                    }
                }
            }
        }
        
        EndDrag();
    }
    
    public void RefreshInventoryUI()
    {
        if (InventoryManager.Instance == null) return;
        
        foreach (var element in itemElements.Values)
        {
            if (element.parent != null)
            {
                element.parent.Remove(element);
            }
        }
        itemElements.Clear();
        
        RefreshContainerGrid(InventoryManager.Instance.playerBackpack);
        RefreshContainerGrid(InventoryManager.Instance.playerPockets);
        RefreshContainerGrid(InventoryManager.Instance.playerVest);
        RefreshContainerGrid(InventoryManager.Instance.playerStash);
        
        RefreshEquipmentSlots();
    }
    
    private void RefreshContainerGrid(ContainerInstance container)
    {
        if (container == null || !containerGrids.ContainsKey(container.instanceId)) return;
        
        VisualElement gridElement = containerGrids[container.instanceId];
        
        foreach (var item in container.items.Values)
        {
            VisualElement itemElement = CreateItemVisual(item);
            
            itemElement.style.position = Position.Absolute;
            itemElement.style.left = item.position.x * 50;
            itemElement.style.top = item.position.y * 50;
            
            gridElement.Add(itemElement);
            
            itemElements[item.instanceId] = itemElement;
            
            itemElement.RegisterCallback<ClickEvent>(evt => {
                ShowItemInfo(item);
                evt.StopPropagation();
            });
        }
    }
    
    private VisualElement CreateItemVisual(ItemInstance item)
    {
        VisualElement itemElement = itemTemplate.Instantiate();
        itemElement.AddToClassList("inventory-item");
        
        int width = item.isRotated ? item.itemData.height : item.itemData.width;
        int height = item.isRotated ? item.itemData.width : item.itemData.height;
        
        itemElement.style.width = width * 50;
        itemElement.style.height = height * 50;
        
        VisualElement iconElement = itemElement.Q<VisualElement>("item-icon");
        if (iconElement != null)
        {
            if (item.itemData.icon != null)
            {
                iconElement.style.backgroundImage = new StyleBackground(item.itemData.icon);
            }
            else
            {
                iconElement.style.backgroundImage = new StyleBackground(placeholderSprite);
            }
        }
        
        Label nameLabel = itemElement.Q<Label>("item-name");
        if (nameLabel != null)
        {
            nameLabel.text = item.itemData.displayName;
        }
        
        Label countLabel = itemElement.Q<Label>("stack-count");
        if (countLabel != null)
        {
            if (item.itemData.stackable && item.currentStack > 1)
            {
                countLabel.text = item.currentStack.ToString();
                countLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                countLabel.style.display = DisplayStyle.None;
            }
        }
        
        Color rarityColor = GetRarityColor(item.itemData.rarity);
        VisualElement borderElement = itemElement.Q<VisualElement>("item-border");
        if (borderElement != null)
        {
            borderElement.style.borderTopColor = rarityColor;
            borderElement.style.borderRightColor = rarityColor;
            borderElement.style.borderBottomColor = rarityColor;
            borderElement.style.borderLeftColor = rarityColor;
        }
        
        return itemElement;
    }
    
    private void RefreshEquipmentSlots()
    {
        if (InventoryManager.Instance == null) return;
        
        foreach (var pair in equipmentSlotElements)
        {
            var slot = pair.Key;
            var slotElement = pair.Value;
            
            slotElement.Clear();
            
            var equippedItem = InventoryManager.Instance.equippedItems[slot];
            if (equippedItem != null)
            {
                VisualElement itemIconElement = new VisualElement();
                itemIconElement.AddToClassList("equipment-item-icon");
                
                if (equippedItem.itemData.icon != null)
                {
                    itemIconElement.style.backgroundImage = new StyleBackground(equippedItem.itemData.icon);
                }
                else
                {
                    itemIconElement.style.backgroundImage = new StyleBackground(placeholderSprite);
                }
                
                slotElement.Add(itemIconElement);
                
                itemIconElement.RegisterCallback<ClickEvent>(evt => {
                    ShowItemInfo(equippedItem);
                    evt.StopPropagation();
                });
            }
        }
    }
    
    private void ShowItemInfo(ItemInstance item)
    {
        if (item == null || itemInfoPanel == null) return;
        
        itemInfoPanel.Clear();
        
        VisualElement itemHeader = new VisualElement();
        itemHeader.AddToClassList("item-info-header");
        
        VisualElement itemIcon = new VisualElement();
        itemIcon.AddToClassList("item-info-icon");
        if (item.itemData.icon != null)
        {
            itemIcon.style.backgroundImage = new StyleBackground(item.itemData.icon);
        }
        else
        {
            itemIcon.style.backgroundImage = new StyleBackground(placeholderSprite);
        }
        itemHeader.Add(itemIcon);
        
        Label itemName = new Label(item.itemData.displayName);
        itemName.AddToClassList("item-info-name");
        itemName.style.color = GetRarityColor(item.itemData.rarity);
        itemHeader.Add(itemName);
        
        Label itemCategory = new Label(item.itemData.category.ToString());
        itemCategory.AddToClassList("item-info-category");
        itemHeader.Add(itemCategory);
        
        itemInfoPanel.Add(itemHeader);
        
        Label itemDesc = new Label(item.itemData.description);
        itemDesc.AddToClassList("item-info-description");
        itemInfoPanel.Add(itemDesc);
        
        if (item.itemData.properties.Count > 0)
        {
            VisualElement propertiesContainer = new VisualElement();
            propertiesContainer.AddToClassList("item-properties-container");
            
            foreach (var property in item.itemData.properties)
            {
                VisualElement propertyRow = new VisualElement();
                propertyRow.AddToClassList("item-property-row");
                
                Label propertyName = new Label(property.name);
                propertyName.AddToClassList("property-name");
                
                Label propertyValue = new Label($"{property.value} {property.unit}");
                propertyValue.AddToClassList("property-value");
                propertyValue.style.color = property.color;
                
                propertyRow.Add(propertyName);
                propertyRow.Add(propertyValue);
                propertiesContainer.Add(propertyRow);
            }
            
            itemInfoPanel.Add(propertiesContainer);
        }
        
        Label itemWeight = new Label($"Weight: {item.itemData.weight.ToString("F2")} kg");
        itemWeight.AddToClassList("item-info-weight");
        itemInfoPanel.Add(itemWeight);
        
        Label itemDimensions = new Label($"Size: {item.itemData.width}x{item.itemData.height}");
        itemDimensions.AddToClassList("item-info-dimensions");
        itemInfoPanel.Add(itemDimensions);
        
        VisualElement actionButtons = new VisualElement();
        actionButtons.AddToClassList("item-action-buttons");
        
        if (item.itemData.category == ItemCategory.Medicine || 
            item.itemData.category == ItemCategory.Food || 
            item.itemData.category == ItemCategory.Drink)
        {
            Button useButton = new Button(() => {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.UseItem(item);
                }
            });
            useButton.text = "Use";
            useButton.AddToClassList("item-action-button");
            actionButtons.Add(useButton);
        }
        
        Button examineButton = new Button(() => {
            Debug.Log($"Examining item: {item.itemData.displayName}");
        });
        examineButton.text = "Examine";
        examineButton.AddToClassList("item-action-button");
        actionButtons.Add(examineButton);
        
        Button discardButton = new Button(() => {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(item);
            }
        });
        discardButton.text = "Discard";
        discardButton.AddToClassList("item-action-button");
        actionButtons.Add(discardButton);
        
        itemInfoPanel.Add(actionButtons);
        
        itemInfoPanel.style.display = DisplayStyle.Flex;
    }
    
    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),
            ItemRarity.Uncommon => new Color(0.3f, 0.8f, 0.3f),
            ItemRarity.Rare => new Color(0.3f, 0.3f, 0.9f),
            ItemRarity.Epic => new Color(0.8f, 0.3f, 0.8f),
            ItemRarity.Legendary => new Color(1.0f, 0.8f, 0.0f),
            _ => Color.white
        };
    }
    
    private void OnItemAdded(ItemInstance item) {}
    private void OnItemRemoved(ItemInstance item) {}
    private void OnItemMoved(ItemInstance item, ContainerInstance source, ContainerInstance target) {}
    private void OnItemEquipped(ItemInstance item) {}
    private void OnItemUnequipped(ItemInstance item) {}
}