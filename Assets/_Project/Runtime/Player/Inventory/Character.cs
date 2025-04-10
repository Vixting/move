using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class SavedItemData
    {
        public ItemData ItemData;
        public string ContainerId;
        public int X;
        public int Y;
        public bool IsRotated;
        public int StackCount = 1;
        public float CurrentDurability = 100f;
        public int CurrentAmmoCount;
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();
    }
    
    [Serializable]
    public class CharacterInventoryData
    {
        public List<SavedItemData> Items = new List<SavedItemData>();
        public Dictionary<EquipmentSlot, SavedItemData> EquippedItems = new Dictionary<EquipmentSlot, SavedItemData>();
    }
    
    public class Character : MonoBehaviour
    {
        [SerializeField] private string characterName = "PMC Character";
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float currentEnergy = 100f;
        [SerializeField] private float maxHydration = 100f;
        [SerializeField] private float currentHydration = 100f;
        [SerializeField] private float maxWeight = 70f;
        
        private Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();
        private float _currentWeight = 0f;
        
        public CharacterInventoryData InventoryData { get; private set; }
        
        public string CharacterName => characterName;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MaxEnergy => maxEnergy;
        public float Energy => currentEnergy;
        public float MaxHydration => maxHydration;
        public float Hydration => currentHydration;
        public float MaxWeight => maxWeight;
        public float CurrentWeight => _currentWeight;
        
        public event Action<float, float> OnWeightChanged;

        public void LoadInventory()
{
        // If you have saved inventory data, load it here
        
        // Example implementation:
        if (InventoryData == null || InventoryData.Items.Count == 0)
        {
            // Initialize with default items if needed
            InventoryData = new CharacterInventoryData();
            
            // You can add default starting items here if desired
        }
        
        // Notify inventory manager to refresh
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.onInventoryChanged?.Invoke();
        }
    }
        
        private void Awake()
        {
            InventoryData = new CharacterInventoryData();
            RecalculateWeight();
        }
        
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }
        
        public void ConsumeEnergy(float amount)
        {
            currentEnergy = Mathf.Max(0, currentEnergy - amount);
        }
        
        public void RestoreEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        }
        
        public void ConsumeHydration(float amount)
        {
            currentHydration = Mathf.Max(0, currentHydration - amount);
        }
        
        public void RestoreHydration(float amount)
        {
            currentHydration = Mathf.Min(currentHydration + amount, maxHydration);
        }
        
        public ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            if (_equippedItems.TryGetValue(slot, out ItemInstance item))
            {
                return item;
            }
            return null;
        }
        
        public bool EquipItem(ItemInstance item, EquipmentSlot slot)
        {
            if (item == null || !item.CanEquipInSlot(slot))
            {
                return false;
            }
            
            ItemInstance currentItem = GetEquippedItem(slot);
            if (currentItem != null)
            {
                UnequipItem(slot);
            }
            
            _equippedItems[slot] = item;
            
            if (item.container != null)
            {
                item.container.RemoveItem(item);
                item.container = null;
            }
            
            RecalculateWeight();
            return true;
        }
        
        public ItemInstance UnequipItem(EquipmentSlot slot)
        {
            if (!_equippedItems.TryGetValue(slot, out ItemInstance item))
            {
                return null;
            }
            
            _equippedItems.Remove(slot);
            RecalculateWeight();
            
            return item;
        }
        
        private void RecalculateWeight()
        {
            float weight = 0f;
            
            foreach (var item in _equippedItems.Values)
            {
                weight += item.itemData.weight;
            }
            
            InventoryManager inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                foreach (var container in new[] { "backpack", "tactical-rig", "pockets" })
                {
                    Dictionary<string, ContainerInstance> containers = inventoryManager.GetContainers();
                    if (containers.TryGetValue(container, out ContainerInstance containerInstance))
                    {
                        foreach (var item in containerInstance.GetAllItems())
                        {
                            weight += item.itemData.weight * item.stackCount;
                        }
                    }
                }
            }
            
            _currentWeight = weight;
            OnWeightChanged?.Invoke(_currentWeight, maxWeight);
        }
        
        public void SaveInventory()
        {
            InventoryData = new CharacterInventoryData();
            
            foreach (var pair in _equippedItems)
            {
                SavedItemData savedItem = new SavedItemData
                {
                    ItemData = pair.Value.itemData,
                    ContainerId = "equipped",
                    X = 0,
                    Y = 0,
                    IsRotated = pair.Value.isRotated,
                    StackCount = pair.Value.stackCount,
                    CurrentDurability = pair.Value.currentDurability,
                    CustomData = pair.Value.customData
                };
                
                if (pair.Value.itemData is WeaponItemData)
                {
                    savedItem.CurrentAmmoCount = pair.Value.currentAmmoCount;
                }
                
                InventoryData.EquippedItems[pair.Key] = savedItem;
            }
            
            InventoryManager inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                Dictionary<string, ContainerInstance> containers = inventoryManager.GetContainers();
                foreach (var containerPair in containers)
                {
                    ContainerInstance container = containerPair.Value;
                    foreach (var item in container.GetAllItems())
                    {
                        SavedItemData savedItem = new SavedItemData
                        {
                            ItemData = item.itemData,
                            ContainerId = containerPair.Key,
                            X = item.position.x,
                            Y = item.position.y,
                            IsRotated = item.isRotated,
                            StackCount = item.stackCount,
                            CurrentDurability = item.currentDurability,
                            CustomData = item.customData
                        };
                        
                        if (item.itemData is WeaponItemData)
                        {
                            savedItem.CurrentAmmoCount = item.currentAmmoCount;
                        }
                        
                        InventoryData.Items.Add(savedItem);
                    }
                }
            }
        }
        
        public Dictionary<string, ContainerInstance> GetContainers()
        {
            InventoryManager inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                return inventoryManager.GetContainers();
            }
            return null;
        }
    }
}