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
        [SerializeField] protected string characterName = "PMC Character";
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth = 100f;
        [SerializeField] protected float maxEnergy = 100f;
        [SerializeField] protected float currentEnergy = 100f;
        [SerializeField] protected float maxHydration = 100f;
        [SerializeField] protected float currentHydration = 100f;
        [SerializeField] protected float maxWeight = 70f;
        
        protected Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();
        protected float _currentWeight = 0f;
        protected bool _isDead = false;
        
        public CharacterInventoryData InventoryData { get; protected set; }
        
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
        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        public virtual void LoadInventory()
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
        
        protected virtual void Awake()
        {
            InventoryData = new CharacterInventoryData();
            RecalculateWeight();
        }
        
        public virtual void TakeDamage(float damage)
        {
            if (_isDead) return;
            
            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            if (currentHealth <= 0 && previousHealth > 0)
            {
                _isDead = true;
                OnDeath?.Invoke();
            }
        }
        
        public virtual void Heal(float amount)
        {
            if (_isDead) return;
            
            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            
            if (currentHealth != previousHealth)
            {
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
        
        public virtual void ConsumeEnergy(float amount)
        {
            currentEnergy = Mathf.Max(0, currentEnergy - amount);
        }
        
        public virtual void RestoreEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        }
        
        public virtual void ConsumeHydration(float amount)
        {
            currentHydration = Mathf.Max(0, currentHydration - amount);
        }
        
        public virtual void RestoreHydration(float amount)
        {
            currentHydration = Mathf.Min(currentHydration + amount, maxHydration);
        }
        
        public virtual bool IsDead()
        {
            return _isDead;
        }
        
        public virtual float GetHealthPercent()
        {
            return Mathf.Clamp01(currentHealth / maxHealth);
        }
        
        public virtual ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            if (_equippedItems.TryGetValue(slot, out ItemInstance item))
            {
                return item;
            }
            return null;
        }
        
        public virtual bool EquipItem(ItemInstance item, EquipmentSlot slot)
        {
            if (item == null || !item.CanEquipInSlot(slot))
            {
                Debug.LogWarning($"[Character] Cannot equip item: invalid item or slot {slot}");
                return false;
            }
            
            ItemInstance currentItem = GetEquippedItem(slot);
            if (currentItem != null)
            {
                Debug.Log($"[Character] Slot {slot} already has {currentItem.itemData.displayName} equipped - removing it first");
                currentItem.IsEquipped = false;
                currentItem.EquippedSlot = null;
                
                _equippedItems.Remove(slot);
            }
            
            Debug.Log($"[Character] Equipping {item.itemData.displayName} to slot {slot}");
            _equippedItems[slot] = item;
            
            item.IsEquipped = true;
            item.EquippedSlot = slot;
            
            if (item.itemData is WeaponItemData weaponItem)
            {
                Debug.Log($"[Character] Equipped weapon {weaponItem.displayName} with {weaponItem.currentAmmoCount} ammo");
            }
            
            if (item.container != null)
            {
                Debug.Log($"[Character] Removing item from container {item.container.containerData.id}");
                item.container.RemoveItem(item);
                item.container = null;
            }
            
            RecalculateWeight();
            
            Debug.Log("[Character] Current equipped items:");
            foreach (var kvp in _equippedItems)
            {
                Debug.Log($"  Slot {kvp.Key}: {(kvp.Value != null ? kvp.Value.itemData.displayName : "None")}");
            }
            
            return true;
        }
                
        public virtual ItemInstance UnequipItem(EquipmentSlot slot)
        {
            if (!_equippedItems.TryGetValue(slot, out ItemInstance item))
            {
                return null;
            }
            
            Debug.Log($"[Character] Unequipping {item.itemData.displayName} from slot {slot}");
            
            item.IsEquipped = false;
            item.EquippedSlot = null;
            
            _equippedItems.Remove(slot);
            RecalculateWeight();
            
            return item;
        }
        
        protected virtual void RecalculateWeight()
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
        
        public virtual void SaveInventory()
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
        
        public virtual Dictionary<string, ContainerInstance> GetContainers()
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