using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager
    {
        private void SetupEquipmentUI()
        {
            if (_root == null) return;
            
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                string slotName = GetSlotElementName(slot);
                VisualElement slotElement = _root.Q(slotName);
                
                if (slotElement != null)
                {
                    EquipmentSlot capturedSlot = slot;
                    slotElement.RegisterCallback<MouseDownEvent>(evt => 
                    {
                        if (evt.button == 0)
                        {
                            OnEquipmentSlotClicked(capturedSlot);
                            evt.StopPropagation();
                        }
                        else if (evt.button == 1)
                        {
                            ItemInstance equippedItem = _character?.GetEquippedItem(capturedSlot);
                            if (equippedItem != null)
                            {
                                UnequipItem(capturedSlot);
                                evt.StopPropagation();
                            }
                        }
                    });
                    
                    if (_character != null)
                    {
                        ItemInstance equippedItem = _character.GetEquippedItem(slot);
                        if (equippedItem != null)
                        {
                            UpdateEquipmentSlotUI(slot, equippedItem);
                        }
                    }
                }
            }
        }
        
        private void OnEquipmentSlotClicked(EquipmentSlot slot)
        {
            if (_draggedItem != null)
            {
                if (_draggedItem.CanEquipInSlot(slot))
                {
                    EquipItem(_draggedItem, slot);
                }
            }
            else
            {
                ItemInstance equippedItem = _character?.GetEquippedItem(slot);
                if (equippedItem != null)
                {
                    ShowItemInfo(equippedItem);
                }
            }
        }
        
    private void UpdateEquipmentSlotUI(EquipmentSlot slot, ItemInstance item)
    {
        string slotName = GetSlotElementName(slot);
        VisualElement slotElement = _root?.Q(slotName);
        
        if (slotElement == null)
        {
            Debug.LogError($"[InventoryManager] UpdateEquipmentSlotUI: Slot element '{slotName}' not found");
            return;
        }
        
        slotElement.style.visibility = Visibility.Visible;
        slotElement.style.display = DisplayStyle.Flex;
        slotElement.style.opacity = 1;
        
        VisualElement existingItemVisual = slotElement.Q(null, "equipment-item-icon");
        if (existingItemVisual != null)
        {
            Debug.Log($"[InventoryManager] Removing existing item icon from slot {slot}");
            existingItemVisual.RemoveFromHierarchy();
        }
        
        if (item != null)
        {
            Debug.Log($"[InventoryManager] Creating UI for equipped item: {item.itemData.displayName} in slot {slot}");
            
            VisualElement itemIcon = new VisualElement();
            itemIcon.name = "equipment-item-icon-" + item.instanceId;
            itemIcon.AddToClassList("equipment-item-icon");
            
            itemIcon.style.visibility = Visibility.Visible;
            itemIcon.style.display = DisplayStyle.Flex;
            itemIcon.style.opacity = 1;
            
            itemIcon.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            itemIcon.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            
            if (item.itemData.icon != null)
            {
                Debug.Log($"[InventoryManager] Item has icon, setting background image");
                itemIcon.style.backgroundImage = new StyleBackground(item.itemData.icon);
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] Item {item.itemData.displayName} has no icon! Creating fallback display");
                
                Label fallbackText = new Label(item.itemData.displayName.Substring(0, Math.Min(3, item.itemData.displayName.Length)).ToUpper());
                fallbackText.style.unityTextAlign = TextAnchor.MiddleCenter;
                fallbackText.style.fontSize = 14;
                fallbackText.style.color = new StyleColor(Color.white);
                
                fallbackText.style.visibility = Visibility.Visible;
                fallbackText.style.display = DisplayStyle.Flex;
                fallbackText.style.opacity = 1;
                
                itemIcon.Add(fallbackText);
            }
            
            if (item.itemData is WeaponItemData weaponItem && 
                (slot == EquipmentSlot.Primary || slot == EquipmentSlot.Secondary || slot == EquipmentSlot.Holster))
            {
                Label ammoLabel = new Label($"{weaponItem.currentAmmoCount}");
                ammoLabel.AddToClassList("weapon-ammo-counter");
                ammoLabel.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.7f));
                ammoLabel.style.color = new StyleColor(Color.white);
                ammoLabel.style.paddingLeft = 5;
                ammoLabel.style.paddingRight = 5;
                ammoLabel.style.fontSize = 10;
                ammoLabel.style.position = Position.Absolute;
                ammoLabel.style.bottom = 2;
                ammoLabel.style.right = 2;
                
                ammoLabel.style.visibility = Visibility.Visible;
                ammoLabel.style.display = DisplayStyle.Flex;
                ammoLabel.style.opacity = 1;
                
                itemIcon.Add(ammoLabel);
            }
            
            Debug.Log($"[InventoryManager] Adding item icon to slot element");
            slotElement.Add(itemIcon);
        }
        else
        {
            Debug.Log($"[InventoryManager] Slot {slot} is now empty");
        }
    }
        
        private string GetSlotElementName(EquipmentSlot slot)
        {
            return $"slot-{slot.ToString().ToLower()}";
        }
        
        private void UpdateHealthAndStats()
        {
            if (_root == null || _character == null) return;
            
            ProgressBar healthBar = _root.Q<ProgressBar>("health-bar");
            if (healthBar != null)
            {
                healthBar.value = (_character.CurrentHealth / _character.MaxHealth) * 100;
            }
            
            ProgressBar energyBar = _root.Q<ProgressBar>("energy-bar");
            if (energyBar != null)
            {
                energyBar.value = (_character.Energy / _character.MaxEnergy) * 100;
            }
            
            ProgressBar hydrationBar = _root.Q<ProgressBar>("hydration-bar");
            if (hydrationBar != null)
            {
                hydrationBar.value = (_character.Hydration / _character.MaxHydration) * 100;
            }
            
            Label weightLabel = _root.Q<Label>("weight-value");
            ProgressBar weightBar = _root.Q<ProgressBar>("weight-bar");
            
            if (weightLabel != null && weightBar != null)
            {
                weightLabel.text = $"{_character.CurrentWeight:F1}/{_character.MaxWeight:F1} kg";
                weightBar.value = (_character.CurrentWeight / _character.MaxWeight) * 100;
            }
        }
        
        public bool EquipItem(ItemInstance item, EquipmentSlot slot)
        {
            if (item == null || !item.CanEquipInSlot(slot))
            {
                Debug.LogWarning($"[InventoryManager] Cannot equip item: item is null or cannot be equipped in slot {slot}");
                return false;
            }
            
            if (_character == null)
            {
                Debug.LogError("[InventoryManager] Cannot equip item: character is null");
                return false;
            }
            
            bool isWeapon = item.itemData is WeaponItemData;
            Debug.Log($"[InventoryManager] Equipping {(isWeapon ? "weapon" : "item")} {item.itemData.displayName} to slot {slot}");
            
            ItemInstance currentItem = _character.GetEquippedItem(slot);
            if (currentItem != null)
            {
                Debug.Log($"[InventoryManager] Unequipping current item in slot {slot}: {currentItem.itemData.displayName}");
                UnequipItem(slot);
            }
            
            bool equipped = _character.EquipItem(item, slot);
            
            if (equipped)
            {
                Debug.Log($"[InventoryManager] Successfully equipped {item.itemData.displayName} to slot {slot}");
                
                if (item.container != null)
                {
                    string containerId = item.container.containerData.id;
                    Debug.Log($"[InventoryManager] Removing item from container {containerId}");
                    item.container.RemoveItem(item);
                    RefreshContainerUI(containerId);
                }
                
                Debug.Log($"[InventoryManager] Updating UI for slot {slot} with item {item.itemData.displayName}");
                UpdateEquipmentSlotUI(slot, item);
                
                if (item.itemData is WeaponItemData weaponItem)
                {
                    Debug.Log($"[InventoryManager] Invoking onEquipWeapon event for {weaponItem.displayName} in slot {slot}");
                    onEquipWeapon?.Invoke(weaponItem, slot);
                }
                
                Debug.Log("[InventoryManager] Invoking onInventoryChanged event");
                onInventoryChanged?.Invoke();
            }
            else
            {
                Debug.LogError($"[InventoryManager] Character.EquipItem failed for item {item.itemData.displayName} in slot {slot}");
            }
            
            return equipped;
        }
        
        public ItemInstance UnequipItem(EquipmentSlot slot)
        {
            if (_character == null) return null;
            
            ItemInstance item = _character.UnequipItem(slot);
            
            if (item != null)
            {
                // Determine target container based on current inventory mode
                string targetContainerId = "stash";
                
                // If in gameplay mode (NoStash), add to backpack instead
                if (_currentMode == InventoryMode.NoStash)
                {
                    targetContainerId = "backpack";
                }
                
                if (!_containers.TryGetValue(targetContainerId, out ContainerInstance container))
                {
                    // Fallback to backpack if main target isn't available
                    if (!_containers.TryGetValue("backpack", out container))
                    {
                        Debug.LogError($"Neither {targetContainerId} nor backpack container found");
                        _character.EquipItem(item, slot); // Re-equip the item
                        return null;
                    }
                }
                
                Vector2Int? availablePos = container.FindAvailablePosition(item);
                if (availablePos.HasValue)
                {
                    container.AddItem(item, availablePos.Value);
                    CreateItemUI(item, targetContainerId);
                    
                    if (item.itemData is WeaponItemData)
                    {
                        onUnequipWeapon?.Invoke(slot);
                    }
                    
                    UpdateEquipmentSlotUI(slot, null);
                    onInventoryChanged?.Invoke();
                    return item;
                }
                else
                {
                    // If the primary target is full, try the other container
                    string alternateContainer = (targetContainerId == "stash") ? "backpack" : "stash";
                    
                    // Only try alternate container if it's accessible in current mode
                    if (_currentMode != InventoryMode.NoStash || alternateContainer != "stash")
                    {
                        if (_containers.TryGetValue(alternateContainer, out ContainerInstance altContainer))
                        {
                            availablePos = altContainer.FindAvailablePosition(item);
                            if (availablePos.HasValue)
                            {
                                altContainer.AddItem(item, availablePos.Value);
                                CreateItemUI(item, alternateContainer);
                                
                                if (item.itemData is WeaponItemData)
                                {
                                    onUnequipWeapon?.Invoke(slot);
                                }
                                
                                UpdateEquipmentSlotUI(slot, null);
                                onInventoryChanged?.Invoke();
                                return item;
                            }
                        }
                    }
                    
                    Debug.LogWarning("No available space in containers for unequipped item");
                    _character.EquipItem(item, slot); // Re-equip the item since there's nowhere to put it
                    return null;
                }
            }
            
            return null;
        }
        
        public void UpdateWeaponAmmo(WeaponItemData weapon, int newAmmoCount)
        {
            weapon.currentAmmoCount = newAmmoCount;
            onUpdateWeaponAmmo?.Invoke(weapon);
            onInventoryChanged?.Invoke();
        }
        
        private int GetWeaponSlotFromEquipmentSlot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Primary:
                    return 1;
                case EquipmentSlot.Secondary:
                    return 2;
                case EquipmentSlot.Holster:
                    return 3;
                default:
                    return -1;
            }
        }
        
        private EquipmentSlot GetEquipmentSlotFromName(string slotName)
        {
            switch (slotName.ToLower())
            {
                case "head": return EquipmentSlot.Head;
                case "eyes": return EquipmentSlot.Eyes;
                case "ears": return EquipmentSlot.Ears;
                case "facecover": return EquipmentSlot.FaceCover;
                case "bodyarmor": return EquipmentSlot.BodyArmor;
                case "tacticalrig": return EquipmentSlot.TacticalRig;
                case "primary": return EquipmentSlot.Primary;
                case "secondary": return EquipmentSlot.Secondary;
                case "holster": return EquipmentSlot.Holster;
                case "backpack": return EquipmentSlot.Backpack;
                case "pouch": return EquipmentSlot.Pouch;
                case "armband": return EquipmentSlot.Armband;
                default: return EquipmentSlot.Primary;
            }
        }
        
        public WeaponData[] GetEquippedWeaponData()
        {
            List<WeaponData> weaponDataList = new List<WeaponData>();
            
            if (_character == null) return weaponDataList.ToArray();
            
            foreach (EquipmentSlot slot in new[] { EquipmentSlot.Primary, EquipmentSlot.Secondary, EquipmentSlot.Holster })
            {
                ItemInstance item = _character.GetEquippedItem(slot);
                if (item != null && item.itemData is WeaponItemData weaponItem)
                {
                    WeaponData weaponData = weaponItem.ToWeaponData();
                    if (weaponData != null)
                    {
                        weaponData.weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
                        weaponData.currentAmmo = weaponItem.currentAmmoCount;
                        weaponDataList.Add(weaponData);
                    }
                }
            }
            
            return weaponDataList.ToArray();
        }
        
        public Dictionary<int, int> GetWeaponAmmoStates()
        {
            Dictionary<int, int> ammoStates = new Dictionary<int, int>();
            
            if (_character == null) return ammoStates;
            
            foreach (EquipmentSlot slot in new[] { EquipmentSlot.Primary, EquipmentSlot.Secondary, EquipmentSlot.Holster })
            {
                ItemInstance item = _character.GetEquippedItem(slot);
                if (item != null && item.itemData is WeaponItemData weaponItem)
                {
                    int weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
                    ammoStates[weaponSlot] = weaponItem.currentAmmoCount;
                }
            }
            
            return ammoStates;
        }
        
        public void AddWeaponToInventory(string weaponId, bool equipImmediately = false)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found");
                return;
            }
            
            ItemData itemData = GameManager.Instance.GetItemById(weaponId);
            if (itemData == null || !(itemData is WeaponItemData weaponItemData))
            {
                Debug.LogError($"Weapon item data not found for ID: {weaponId}");
                return;
            }
            
            if (!_containers.TryGetValue("stash", out ContainerInstance stash))
            {
                Debug.LogError("Stash container not found");
                return;
            }
            
            ItemInstance dummyItem = new ItemInstance(weaponItemData, Vector2Int.zero, stash);
            Vector2Int? availablePosition = stash.FindAvailablePosition(dummyItem);
            
            if (!availablePosition.HasValue)
            {
                Debug.LogWarning("No available space in stash for new weapon");
                return;
            }
            
            ItemInstance weaponItem = AddItemToContainer(weaponItemData, "stash", availablePosition.Value);
            
            if (weaponItem != null)
            {
                if (equipImmediately)
                {
                    EquipItem(weaponItem, EquipmentSlot.Primary);
                }
                
                onInventoryChanged?.Invoke();
                Debug.Log($"Added weapon {weaponItemData.displayName} to inventory");
            }
        }
        
        public void AddWeaponCommand(string[] parameters)
        {
            if (parameters.Length < 1)
            {
                Debug.LogError("Missing weapon ID parameter");
                return;
            }
            
            string weaponId = parameters[0];
            bool equip = parameters.Length > 1 && parameters[1].ToLower() == "true";
            
            InventoryManager.Instance.AddWeaponToInventory(weaponId, equip);
        }
    }
}