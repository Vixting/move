using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    public class Character : MonoBehaviour
    {
        [SerializeField] private string characterName = "PMC Character";
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float maxHydration = 100f;
        [SerializeField] private float maxWeight = 70f;
        
        public string CharacterName => characterName;
        
        private float _currentHealth;
        private float _currentEnergy;
        private float _currentHydration;
        private float _currentWeight;
        
        public float CurrentHealth => _currentHealth;
        public float CurrentEnergy => _currentEnergy;
        public float CurrentHydration => _currentHydration;
        public float CurrentWeight => _currentWeight;
        public float MaxHealth => maxHealth;
        public float MaxEnergy => maxEnergy;
        public float MaxHydration => maxHydration;
        public float MaxWeight => maxWeight;
        
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnEnergyChanged;
        public event Action<float, float> OnHydrationChanged;
        public event Action<float, float> OnWeightChanged;
        
        private Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();
        public CharacterInventoryData InventoryData { get; private set; }
        
        private void Awake()
        {
            _currentHealth = maxHealth;
            _currentEnergy = maxEnergy;
            _currentHydration = maxHydration;
            _currentWeight = 0f;
            
            InventoryData = new CharacterInventoryData();
        }
        
        public bool EquipItem(ItemInstance item, EquipmentSlot slot)
        {
            if (_equippedItems.ContainsKey(slot))
            {
                return false;
            }
            
            _equippedItems[slot] = item;
            _currentWeight += item.itemData.weight;
            OnWeightChanged?.Invoke(_currentWeight, maxWeight);
            
            return true;
        }
        
        public ItemInstance UnequipItem(EquipmentSlot slot)
        {
            if (!_equippedItems.TryGetValue(slot, out ItemInstance item))
            {
                return null;
            }
            
            _equippedItems.Remove(slot);
            _currentWeight -= item.itemData.weight;
            OnWeightChanged?.Invoke(_currentWeight, maxWeight);
            
            return item;
        }
        
        public ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            _equippedItems.TryGetValue(slot, out ItemInstance item);
            return item;
        }
        
        public void AddItemWeight(float weight)
        {
            _currentWeight += weight;
            OnWeightChanged?.Invoke(_currentWeight, maxWeight);
        }
        
        public void RemoveItemWeight(float weight)
        {
            _currentWeight -= weight;
            _currentWeight = Mathf.Max(0, _currentWeight);
            OnWeightChanged?.Invoke(_currentWeight, maxWeight);
        }
        
        public void UpdateHealth(float amount)
        {
            _currentHealth += amount;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }
        
        public void UpdateEnergy(float amount)
        {
            _currentEnergy += amount;
            _currentEnergy = Mathf.Clamp(_currentEnergy, 0, maxEnergy);
            OnEnergyChanged?.Invoke(_currentEnergy, maxEnergy);
        }
        
        public void UpdateHydration(float amount)
        {
            _currentHydration += amount;
            _currentHydration = Mathf.Clamp(_currentHydration, 0, maxHydration);
            OnHydrationChanged?.Invoke(_currentHydration, maxHydration);
        }
        
        public void SaveInventory()
        {
            InventoryData.SaveEquipment(_equippedItems);
        }
        
        public void LoadInventory()
        {
        }
        
        public bool HasSpaceForItem(ItemData itemData, string containerId)
        {
            InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
                return false;
                
            return inventoryManager.HasSpaceForItem(itemData, containerId);
        }
        
        public List<ItemInstance> FindItemsByCategory(ItemCategory category)
        {
            List<ItemInstance> result = new List<ItemInstance>();
            InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
            
            if (inventoryManager == null)
                return result;
                
            return inventoryManager.FindItemsByCategory(category);
        }
        
        public bool ConsumeItem(ItemInstance item)
        {
            if (item == null)
                return false;
                
            if (item.itemData.category == ItemCategory.Medicine)
            {
                float healAmount = 20f;
                
                if (item.customData.TryGetValue("healAmount", out object healObj) && healObj is float healValue)
                {
                    healAmount = healValue;
                }
                
                UpdateHealth(healAmount);
                return true;
            }
            
            if (item.itemData.category == ItemCategory.Food)
            {
                float energyAmount = 15f;
                
                if (item.customData.TryGetValue("energyAmount", out object energyObj) && energyObj is float energyValue)
                {
                    energyAmount = energyValue;
                }
                
                UpdateEnergy(energyAmount);
                return true;
            }
            
            if (item.itemData.category == ItemCategory.Drink)
            {
                float hydrationAmount = 15f;
                
                if (item.customData.TryGetValue("hydrationAmount", out object hydrationObj) && hydrationObj is float hydrationValue)
                {
                    hydrationAmount = hydrationValue;
                }
                
                UpdateHydration(hydrationAmount);
                return true;
            }
            
            return false;
        }
        
        public bool CanEquipItemInSlot(ItemData item, EquipmentSlot slot)
        {
            if (item == null)
                return false;
                
            switch (slot)
            {
                case EquipmentSlot.Head:
                    return item.category == ItemCategory.Helmet;
                    
                case EquipmentSlot.BodyArmor:
                    return item.category == ItemCategory.Armor;
                    
                case EquipmentSlot.Primary:
                case EquipmentSlot.Secondary:
                case EquipmentSlot.Holster:
                    if (!(item is WeaponItemData weaponItem))
                        return false;
                        
                    if (slot == EquipmentSlot.Primary)
                        return weaponItem.weaponType == WeaponType.AssaultRifle || 
                               weaponItem.weaponType == WeaponType.SniperRifle;
                               
                    if (slot == EquipmentSlot.Secondary)
                        return weaponItem.weaponType == WeaponType.SMG || 
                               weaponItem.weaponType == WeaponType.Shotgun;
                               
                    if (slot == EquipmentSlot.Holster)
                        return weaponItem.weaponType == WeaponType.Pistol;
                        
                    return false;
                    
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public class CharacterInventoryData
    {
        [Serializable]
        public class SavedItemData
        {
            public ItemData ItemData;
            public string ContainerId;
            public int X;
            public int Y;
            public bool IsRotated;
        }
        
        public List<SavedItemData> Items = new List<SavedItemData>();
        public Dictionary<EquipmentSlot, string> EquippedItemIds = new Dictionary<EquipmentSlot, string>();
        
        public void SaveEquipment(Dictionary<EquipmentSlot, ItemInstance> equippedItems)
        {
            EquippedItemIds.Clear();
            
            foreach (var pair in equippedItems)
            {
                EquippedItemIds[pair.Key] = pair.Value.instanceId;
            }
        }
    }
}