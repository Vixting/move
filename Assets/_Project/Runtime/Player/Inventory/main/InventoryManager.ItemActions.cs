using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager
    {
        private void ShowItemContextMenu(ItemInstance item, Vector2 position)
        {
            if (_contextMenu != null)
            {
                _contextMenu.RemoveFromHierarchy();
            }
            
            _contextMenu = InventoryUIHelper.CreateItemContextMenu(
                item,
                position,
                UseItem,
                DiscardItem,
                SplitItem,
                FoldItem,
                ExamineItem,
                DropItemToWorld
            );
            
            // Ensure the menu is visible
            _contextMenu.style.display = DisplayStyle.Flex;
            _contextMenu.style.visibility = Visibility.Visible;
            _contextMenu.style.opacity = 1;
            
            _root.Add(_contextMenu);
            
            // Debug the menu's position
            Debug.Log($"Context menu created at position: ({position.x}, {position.y})");
        }
        
        private void HandleItemDoubleClick(ItemInstance item)
        {
            if (item == null) return;
            
            if (item.itemData.canEquip)
            {
                foreach (var slot in item.itemData.compatibleSlots)
                {
                    if (EquipItem(item, slot))
                    {
                        return;
                    }
                }
            }
            else if (item.itemData.canUse)
            {
                UseItem(item);
            }
            else
            {
                ShowItemInfo(item);
            }
        }
        
        private void UseItem(ItemInstance item)
        {
            if (item == null || !item.itemData.canUse) return;
            
            if (item.itemData is MedicineItemData medicine)
            {
                if (_character != null)
                {
                    _character.Heal(medicine.healthRestored);
                    
                    if (item.stackCount <= 1)
                    {
                        RemoveItem(item);
                    }
                    else
                    {
                        item.stackCount--;
                        RefreshItemUI(item, item.container.containerData.id);
                    }
                    
                    onInventoryChanged?.Invoke();
                }
            }
            else if (item.itemData is AmmoItemData ammo)
            {
                onInventoryChanged?.Invoke();
            }
        }
        
        private void SplitItem(ItemInstance item)
        {
            if (item == null || !item.itemData.canStack || item.stackCount <= 1) return;
            
            int halfStack = Mathf.CeilToInt(item.stackCount / 2f);
            ItemInstance newStack = item.SplitStack(halfStack);
            
            if (newStack != null)
            {
                Vector2Int? availablePos = item.container.FindAvailablePosition(newStack);
                if (availablePos.HasValue)
                {
                    item.container.AddItem(newStack, availablePos.Value);
                    _items[newStack.instanceId] = newStack;
                    
                    RefreshItemUI(item, item.container.containerData.id);
                    CreateItemUI(newStack, item.container.containerData.id);
                    
                    onInventoryChanged?.Invoke();
                }
                else
                {
                    item.stackCount += newStack.stackCount;
                    RefreshItemUI(item, item.container.containerData.id);
                }
            }
        }
        
        private void FoldItem(ItemInstance item)
        {
            if (item == null || !(item.itemData is WeaponItemData weaponData) || !weaponData.foldable)
                return;
                
            if (item.ToggleFolded())
            {
                RefreshItemUI(item, item.container.containerData.id);
                onInventoryChanged?.Invoke();
            }
        }
        
        private void ExamineItem(ItemInstance item)
        {
            if (item == null || !item.itemData.needsExamination) return;
            
            item.itemData.isExamined = true;
            
            RefreshItemUI(item, item.container.containerData.id);
            onInventoryChanged?.Invoke();
        }
        
        private void DiscardItem(ItemInstance item)
        {
            if (item == null) return;
            
            RemoveItem(item);
        }
        
        public void RemoveItem(ItemInstance item)
        {
            if (item == null) return;
            
            if (item.container != null)
            {
                string containerId = item.container.containerData.id;
                item.container.RemoveItem(item);
                _items.Remove(item.instanceId);
                
                RefreshContainerUI(containerId);
                onInventoryChanged?.Invoke();
            }
            else
            {
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    if (_character?.GetEquippedItem(slot) == item)
                    {
                        UnequipItem(slot);
                        break;
                    }
                }
            }
        }
        
        private void ShowItemInfo(ItemInstance item)
        {
            if (_root == null || item == null) return;
            
            VisualElement infoPanel = _root.Q("item-info-panel");
            if (infoPanel == null) return;
            
            infoPanel.Clear();
            
            VisualElement header = new VisualElement();
            header.AddToClassList("item-info-header");
            
            VisualElement itemIcon = new VisualElement();
            itemIcon.AddToClassList("item-info-icon");
            if (item.itemData.icon != null)
            {
                itemIcon.style.backgroundImage = new StyleBackground(item.itemData.icon);
            }
            
            Label itemName = new Label(item.itemData.displayName);
            itemName.AddToClassList("item-info-name");
            
            Label itemCategory = new Label(item.itemData.GetItemType());
            itemCategory.AddToClassList("item-info-category");
            
            header.Add(itemIcon);
            header.Add(itemName);
            header.Add(itemCategory);
            
            infoPanel.Add(header);
            
            if (!string.IsNullOrEmpty(item.itemData.description))
            {
                Label description = new Label(item.itemData.description);
                description.AddToClassList("item-info-description");
                infoPanel.Add(description);
            }
            
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
                    
                    Label propertyValue = new Label(property.value + (string.IsNullOrEmpty(property.unit) ? "" : " " + property.unit));
                    propertyValue.AddToClassList("property-value");
                    propertyValue.style.color = property.color;
                    
                    propertyRow.Add(propertyName);
                    propertyRow.Add(propertyValue);
                    propertiesContainer.Add(propertyRow);
                }
                
                infoPanel.Add(propertiesContainer);
            }
            
            Label weightInfo = new Label($"Weight: {item.itemData.weight} kg");
            weightInfo.AddToClassList("item-info-weight");
            infoPanel.Add(weightInfo);
            
            Label dimensionsInfo = new Label($"Size: {item.GetWidth()}x{item.GetHeight()} cells");
            dimensionsInfo.AddToClassList("item-info-dimensions");
            infoPanel.Add(dimensionsInfo);
            
            VisualElement actionButtons = new VisualElement();
            actionButtons.AddToClassList("item-action-buttons");
            
            if (item.itemData.canUse)
            {
                Button useButton = new Button(() => UseItem(item));
                useButton.text = "Use";
                useButton.AddToClassList("item-action-button");
                actionButtons.Add(useButton);
            }
            
            if (item.itemData.canRotate)
            {
                Button rotateButton = new Button(() => RotateItem(item));
                rotateButton.text = "Rotate";
                rotateButton.AddToClassList("item-action-button");
                actionButtons.Add(rotateButton);
            }
            
            if (item.itemData is WeaponItemData weaponData && weaponData.foldable)
            {
                Button foldButton = new Button(() => FoldItem(item));
                foldButton.text = weaponData.folded ? "Unfold" : "Fold";
                foldButton.AddToClassList("item-action-button");
                actionButtons.Add(foldButton);
            }
            
            // Add drop to world button
            if (item.itemData.prefab != null)
            {
                Button dropButton = new Button(() => DropItemToWorld(item));
                dropButton.text = "Drop";
                dropButton.AddToClassList("item-action-button");
                actionButtons.Add(dropButton);
            }
            
            Button closeButton = new Button(() => {
                infoPanel.style.display = DisplayStyle.None;
            });
            closeButton.text = "Close";
            closeButton.AddToClassList("item-action-button");
            actionButtons.Add(closeButton);
            
            infoPanel.Add(actionButtons);
            
            infoPanel.style.display = DisplayStyle.Flex;
        }
        
        // Implementation for dropping item to world
        public void DropItemToWorld(ItemInstance item)
        {
            if (item == null || item.itemData == null || item.itemData.prefab == null)
            {
                Debug.LogWarning("Cannot drop item: item, item data or prefab is null");
                return;
            }

            Debug.Log($"Attempting to drop item {item.itemData.displayName} into the world");

            Vector3 dropPosition = GetDropPosition();
            
            // Actually instantiate the object in the scene
            GameObject droppedItem = Instantiate(item.itemData.prefab, dropPosition, Quaternion.identity);
            
            if (droppedItem != null)
            {
                Debug.Log($"Successfully instantiated item at {dropPosition}");
                ConfigureDroppedItem(droppedItem, item);
                ApplyDropForce(droppedItem);
                RemoveItem(item);
                
                Debug.Log($"Dropped item {item.itemData.displayName} into the world");
            }
            else
            {
                Debug.LogError($"Failed to instantiate prefab for {item.itemData.displayName}");
            }
        }
        
        private Vector3 GetDropPosition()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                return transform.position + Vector3.forward * _dropDistance;
            }
            
            Transform referenceTransform = Camera.main?.transform;
            if (referenceTransform == null)
            {
                referenceTransform = player.transform;
            }
            
            Vector3 dropPosition = player.transform.position + 
                                  referenceTransform.forward * _dropDistance + 
                                  Vector3.up * _dropHeight;
                                  
            return dropPosition;
        }
        
        private void ConfigureDroppedItem(GameObject droppedItem, ItemInstance item)
        {
            if (droppedItem.GetComponent<Collider>() == null)
            {
                BoxCollider collider = droppedItem.AddComponent<BoxCollider>();
                float width = item.GetWidth() * 0.2f;
                float height = item.GetHeight() * 0.2f;
                collider.size = new Vector3(width, height, Mathf.Min(width, height));
            }
            
            if (droppedItem.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = droppedItem.AddComponent<Rigidbody>();
                rb.mass = item.itemData.weight;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            
            WorldItem worldItem = droppedItem.GetComponent<WorldItem>();
            if (worldItem == null)
            {
                worldItem = droppedItem.AddComponent<WorldItem>();
            }
            worldItem.Initialize(item);
        }
        
        private void ApplyDropForce(GameObject droppedItem)
        {
            Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dropDirection = GetDropDirection();
                
                dropDirection += new Vector3(
                    UnityEngine.Random.Range(-0.2f, 0.2f),
                    UnityEngine.Random.Range(0.1f, 0.3f),
                    UnityEngine.Random.Range(-0.2f, 0.2f)
                ).normalized;
                
                rb.AddForce(dropDirection * _dropForce, ForceMode.Impulse);
                
                rb.AddTorque(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    ForceMode.Impulse
                );
            }
        }
        
        private Vector3 GetDropDirection()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                return Vector3.forward;
            }
            
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera.transform.forward;
            }
            
            return player.transform.forward;
        }
        
        public List<ItemInstance> FindItemsByCategory(ItemCategory category)
        {
            List<ItemInstance> result = new List<ItemInstance>();
            
            foreach (var container in _containers.Values)
            {
                foreach (var item in container.GetAllItems())
                {
                    if (item.itemData.category == category)
                    {
                        result.Add(item);
                    }
                }
            }
            
            return result;
        }
    }
}