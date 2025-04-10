using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager //InventoryManager.ItemActions.cs
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
                ExamineItem
            );
            
            _root.Add(_contextMenu);
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
            
            Button closeButton = new Button(() => {
                infoPanel.style.display = DisplayStyle.None;
            });
            closeButton.text = "Close";
            closeButton.AddToClassList("item-action-button");
            actionButtons.Add(closeButton);
            
            infoPanel.Add(actionButtons);
            
            infoPanel.style.display = DisplayStyle.Flex;
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