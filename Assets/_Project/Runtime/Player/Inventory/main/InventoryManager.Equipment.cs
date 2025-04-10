using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager //InventoryManager.Equipment.cs
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
            
            if (slotElement == null) return;
            
            VisualElement existingItemVisual = slotElement.Q(null, "equipment-item-icon");
            if (existingItemVisual != null)
            {
                existingItemVisual.RemoveFromHierarchy();
            }
            
            if (item != null)
            {
                VisualElement itemIcon = new VisualElement();
                itemIcon.AddToClassList("equipment-item-icon");
                
                if (item.itemData.icon != null)
                {
                    itemIcon.style.backgroundImage = new StyleBackground(item.itemData.icon);
                }
                
                slotElement.Add(itemIcon);
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
                return false;
            }
            
            if (_character == null) return false;
            
            ItemInstance currentItem = _character.GetEquippedItem(slot);
            if (currentItem != null)
            {
                UnequipItem(slot);
            }
            
            bool equipped = _character.EquipItem(item, slot);
            
            if (equipped)
            {
                if (item.container != null)
                {
                    string containerId = item.container.containerData.id;
                    item.container.RemoveItem(item);
                    RefreshContainerUI(containerId);
                }
                
                UpdateEquipmentSlotUI(slot, item);
                
                if (item.itemData is WeaponItemData weaponItem)
                {
                    onEquipWeapon?.Invoke(weaponItem, slot);
                }
                
                onInventoryChanged?.Invoke();
            }
            
            return equipped;
        }
        
        public ItemInstance UnequipItem(EquipmentSlot slot)
        {
            if (_character == null) return null;
            
            ItemInstance item = _character.UnequipItem(slot);
            
            if (item != null)
            {
                if (!_containers.TryGetValue("stash", out ContainerInstance container))
                {
                    Debug.LogError("Stash container not found");
                    return null;
                }
                
                Vector2Int? availablePos = container.FindAvailablePosition(item);
                if (availablePos.HasValue)
                {
                    container.AddItem(item, availablePos.Value);
                    CreateItemUI(item, "stash");
                    
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
                    Debug.LogWarning("No available space in stash for unequipped item");
                    _character.EquipItem(item, slot);
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