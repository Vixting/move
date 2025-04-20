using System.Collections.Generic;
using UnityEngine;
using InventorySystem;
using System.Linq;

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Game/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
    [SerializeField] private ItemDatabase itemDatabase;

    private Dictionary<string, WeaponData> _weaponsById = new Dictionary<string, WeaponData>();
    private Dictionary<string, WeaponItemData> _weaponItemsCache = new Dictionary<string, WeaponItemData>();
    private bool _initialized = false;

    /// <summary>
    /// Initialize the weapon database
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;

        _weaponsById.Clear();
        foreach (var weapon in availableWeapons)
        {
            if (weapon != null)
            {
                // Generate weapon ID if empty
                if (string.IsNullOrEmpty(weapon.WeaponId))
                {
                    Debug.LogWarning($"Weapon {weapon.weaponName} has no ID. Generating new ID.");
                    var serializedObject = new UnityEditor.SerializedObject(weapon);
                    var weaponIdProperty = serializedObject.FindProperty("weaponId");
                    weaponIdProperty.stringValue = System.Guid.NewGuid().ToString();
                    serializedObject.ApplyModifiedProperties();
                }

                // Generate inventory item ID if empty
                if (string.IsNullOrEmpty(weapon.inventoryItemId))
                {
                    Debug.LogWarning($"Weapon {weapon.weaponName} has no inventory item ID. Generating new ID.");
                    weapon.inventoryItemId = System.Guid.NewGuid().ToString();
                }

                // Add to dictionary
                _weaponsById[weapon.WeaponId] = weapon;
            }
        }

        _initialized = true;
    }

    /// <summary>
    /// Get all available weapons
    /// </summary>
    public WeaponData[] GetAllWeapons()
    {
        if (!_initialized) Initialize();
        return availableWeapons.ToArray();
    }

    /// <summary>
    /// Get a weapon by its unique ID
    /// </summary>
    public WeaponData GetWeaponById(string weaponId)
    {
        if (!_initialized) Initialize();
        
        if (string.IsNullOrEmpty(weaponId)) return null;
        
        if (_weaponsById.TryGetValue(weaponId, out WeaponData weapon))
        {
            return weapon;
        }
        
        return null;
    }

    /// <summary>
    /// Get a weapon by its inventory item ID
    /// </summary>
    public WeaponData GetWeaponByInventoryItemId(string inventoryItemId)
    {
        if (!_initialized) Initialize();
        
        if (string.IsNullOrEmpty(inventoryItemId)) return null;
        
        foreach (var weapon in availableWeapons)
        {
            if (weapon != null && weapon.inventoryItemId == inventoryItemId)
            {
                return weapon;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Ensures all weapons have corresponding ItemData entries in the item database
    /// </summary>
    public void EnsureWeaponItemsExist(RuntimeWeaponItemCreator weaponItemCreator = null)
    {
        if (!_initialized) Initialize();
        
        if (itemDatabase == null)
        {
            Debug.LogError("Cannot create weapon items: Item database not assigned");
            return;
        }
        
        // Find or create weapon item creator
        if (weaponItemCreator == null)
        {
            weaponItemCreator = GameObject.FindObjectOfType<RuntimeWeaponItemCreator>();
            
            if (weaponItemCreator == null)
            {
                Debug.LogError("Cannot create weapon items: RuntimeWeaponItemCreator not found");
                return;
            }
        }
        
        int createdCount = 0;
        
        foreach (var weapon in availableWeapons)
        {
            if (weapon == null) continue;
            
            // Skip if this weapon already has an item in the database
            ItemData existingItem = itemDatabase.GetItem(weapon.inventoryItemId);
            if (existingItem != null && existingItem is WeaponItemData)
            {
                continue;
            }
            
            // Create the weapon item
            WeaponItemData weaponItem = weaponItemCreator.CreateSingleWeaponItem(weapon);
            if (weaponItem != null)
            {
                createdCount++;
            }
        }
        
        if (createdCount > 0)
        {
            Debug.Log($"Created {createdCount} new weapon items in database");
        }
    }

    /// <summary>
    /// Add a new weapon to the database
    /// </summary>
    public void AddWeapon(WeaponData weapon)
    {
        if (weapon == null) return;
        
        if (!_initialized) Initialize();
        
        // Check if weapon is already in database
        if (availableWeapons.Contains(weapon))
        {
            Debug.Log($"Weapon {weapon.weaponName} already exists in database");
            return;
        }
        
        // Generate IDs if needed
        if (string.IsNullOrEmpty(weapon.WeaponId))
        {
            var serializedObject = new UnityEditor.SerializedObject(weapon);
            var weaponIdProperty = serializedObject.FindProperty("weaponId");
            weaponIdProperty.stringValue = System.Guid.NewGuid().ToString();
            serializedObject.ApplyModifiedProperties();
        }
        
        if (string.IsNullOrEmpty(weapon.inventoryItemId))
        {
            weapon.inventoryItemId = System.Guid.NewGuid().ToString();
        }
        
        // Add to collections
        availableWeapons.Add(weapon);
        _weaponsById[weapon.WeaponId] = weapon;
        
        Debug.Log($"Added weapon {weapon.weaponName} to database");
    }

    /// <summary>
    /// Get all weapons of a specific type
    /// </summary>
    public WeaponData[] GetWeaponsByType(WeaponType weaponType)
    {
        if (!_initialized) Initialize();
        
        return availableWeapons.Where(w => w != null && w.weaponType == weaponType).ToArray();
    }

    /// <summary>
    /// Get a weapon item data (inventory item) for a weapon
    /// </summary>
    public WeaponItemData GetWeaponItemForWeapon(WeaponData weapon)
    {
        if (weapon == null || string.IsNullOrEmpty(weapon.inventoryItemId)) 
            return null;
            
        // Try cache first
        if (_weaponItemsCache.TryGetValue(weapon.inventoryItemId, out WeaponItemData cachedItem))
            return cachedItem;
            
        // Try database
        if (itemDatabase != null)
        {
            ItemData itemData = itemDatabase.GetItem(weapon.inventoryItemId);
            if (itemData != null && itemData is WeaponItemData weaponItemData)
            {
                _weaponItemsCache[weapon.inventoryItemId] = weaponItemData;
                return weaponItemData;
            }
        }
        
        return null;
    }
}