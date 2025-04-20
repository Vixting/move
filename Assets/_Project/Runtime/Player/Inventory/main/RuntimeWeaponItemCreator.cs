using UnityEngine;
using System.Collections.Generic;
using InventorySystem;
using SharedTypes;
using System;

/// <summary>
/// Create weapon items at runtime to match WeaponData assets
/// </summary>
public class RuntimeWeaponItemCreator : MonoBehaviour
{
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private bool createOnStart = true;
    [SerializeField] private bool addToInventoryAfterCreation = false;
    [SerializeField] private bool updateExistingItems = true;
    [SerializeField] private bool logDebugInfo = true;
    
    private void Awake()
    {
        if (itemDatabase == null)
        {
            itemDatabase = FindObjectOfType<ItemDatabase>();
            if (itemDatabase == null)
            {
                Debug.LogError("ItemDatabase not found! Please assign one in the inspector or make sure one exists in the scene.");
            }
        }
    }
    
    private void Start()
    {
        if (createOnStart && itemDatabase != null)
        {
            CreateWeaponItemsFromGameManager();
        }
    }
    
    public void CreateWeaponItemsFromGameManager()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance not found!");
            return;
        }
        
        if (itemDatabase == null)
        {
            Debug.LogError("Item Database not assigned!");
            return;
        }
        
        WeaponData[] weaponDatas = GameManager.Instance.GetSavedWeapons();
        if (weaponDatas == null || weaponDatas.Length == 0)
        {
            Debug.LogWarning("No weapon data found in GameManager");
            return;
        }
        
        int createdCount = 0;
        int updatedCount = 0;
        List<WeaponItemData> processedItems = new List<WeaponItemData>();
        
        foreach (var weaponData in weaponDatas)
        {
            if (weaponData == null)
            {
                LogDebug("Skipping null WeaponData");
                continue;
            }
            
            // Generate inventory item ID if not already set
            if (string.IsNullOrEmpty(weaponData.inventoryItemId))
            {
                weaponData.inventoryItemId = Guid.NewGuid().ToString();
                LogDebug($"Generated new inventory ID for {weaponData.weaponName}: {weaponData.inventoryItemId}");
            }
            
            // Check if an item with this ID already exists
            ItemData existingItem = itemDatabase.GetItem(weaponData.inventoryItemId);
            
            if (existingItem != null)
            {
                if (existingItem is WeaponItemData existingWeapon)
                {
                    if (updateExistingItems)
                    {
                        // Update the existing weapon item with new data
                        UpdateWeaponItemFromData(existingWeapon, weaponData);
                        itemDatabase.UpdateItem(existingWeapon);
                        updatedCount++;
                        processedItems.Add(existingWeapon);
                        LogDebug($"Updated existing weapon item: {existingWeapon.displayName}");
                    }
                    else
                    {
                        LogDebug($"Item with ID '{weaponData.inventoryItemId}' already exists. Skipping (update is disabled).");
                    }
                }
                else
                {
                    Debug.LogWarning($"Item with ID '{weaponData.inventoryItemId}' exists but is not a WeaponItemData. Cannot update.");
                }
            }
            else
            {
                // Create a new WeaponItemData
                WeaponItemData weaponItem = CreateWeaponItemFromData(weaponData);
                if (weaponItem != null)
                {
                    // Add to database
                    bool added = itemDatabase.AddItem(weaponItem);
                    if (added)
                    {
                        createdCount++;
                        processedItems.Add(weaponItem);
                        LogDebug($"Created new weapon item: {weaponItem.displayName}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to add weapon {weaponData.weaponName} to database.");
                    }
                }
            }
        }
        
        Debug.Log($"Processed weapons in item database: {createdCount} created, {updatedCount} updated");
        
        // Optionally add the created weapons to inventory
        if (addToInventoryAfterCreation && processedItems.Count > 0)
        {
            StartCoroutine(AddCreatedWeaponsToInventory(processedItems));
        }
    }
    
    private System.Collections.IEnumerator AddCreatedWeaponsToInventory(List<WeaponItemData> weapons)
    {
        // Wait a frame to ensure item database is fully updated
        yield return null;
        
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager not found!");
            yield break;
        }
        
        foreach (var weaponItem in weapons)
        {
            inventoryManager.AddWeaponToInventory(weaponItem.id, false);
            LogDebug($"Added weapon {weaponItem.displayName} to inventory");
            
            // Add small delay between adds
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    public void CreateWeaponItemsFromScene()
    {
        if (itemDatabase == null)
        {
            Debug.LogError("Item Database not assigned!");
            return;
        }
        
        // Find weapons through weapon managers in the scene
        WeaponManager[] weaponManagers = FindObjectsOfType<WeaponManager>();
        if (weaponManagers == null || weaponManagers.Length == 0)
        {
            Debug.LogWarning("No WeaponManager found in the scene");
            return;
        }
        
        HashSet<WeaponData> allWeapons = new HashSet<WeaponData>(); // Using HashSet to avoid duplicates
        
        foreach (var manager in weaponManagers)
        {
            WeaponData[] weapons = manager.GetAvailableWeapons();
            if (weapons != null && weapons.Length > 0)
            {
                foreach (var weapon in weapons)
                {
                    if (weapon != null)
                    {
                        allWeapons.Add(weapon);
                    }
                }
            }
        }
        
        if (allWeapons.Count == 0)
        {
            Debug.LogWarning("No weapons found in WeaponManagers");
            return;
        }
        
        int createdCount = 0;
        int updatedCount = 0;
        List<WeaponItemData> processedItems = new List<WeaponItemData>();
        
        foreach (var weaponData in allWeapons)
        {
            // Generate inventory item ID if not already set
            if (string.IsNullOrEmpty(weaponData.inventoryItemId))
            {
                weaponData.inventoryItemId = Guid.NewGuid().ToString();
                LogDebug($"Generated new inventory ID for {weaponData.weaponName}: {weaponData.inventoryItemId}");
            }
            
            // Check if an item with this ID already exists
            ItemData existingItem = itemDatabase.GetItem(weaponData.inventoryItemId);
            
            if (existingItem != null)
            {
                if (existingItem is WeaponItemData existingWeapon && updateExistingItems)
                {
                    // Update the existing weapon item with new data
                    UpdateWeaponItemFromData(existingWeapon, weaponData);
                    itemDatabase.UpdateItem(existingWeapon);
                    updatedCount++;
                    processedItems.Add(existingWeapon);
                    LogDebug($"Updated existing weapon item: {existingWeapon.displayName}");
                }
                else
                {
                    LogDebug($"Item with ID '{weaponData.inventoryItemId}' already exists in the database");
                }
            }
            else
            {
                // Create a new WeaponItemData
                WeaponItemData weaponItem = CreateWeaponItemFromData(weaponData);
                if (weaponItem != null)
                {
                    // Add to database
                    bool added = itemDatabase.AddItem(weaponItem);
                    if (added)
                    {
                        createdCount++;
                        processedItems.Add(weaponItem);
                        LogDebug($"Created new weapon item: {weaponItem.displayName}");
                    }
                }
            }
        }
        
        Debug.Log($"Processed weapons from scene: {createdCount} created, {updatedCount} updated");
        
        // Optionally add the created weapons to inventory
        if (addToInventoryAfterCreation && processedItems.Count > 0)
        {
            StartCoroutine(AddCreatedWeaponsToInventory(processedItems));
        }
    }
    
    private WeaponItemData CreateWeaponItemFromData(WeaponData weaponData)
    {
        if (weaponData == null) return null;
        
        // Create a new WeaponItemData
        WeaponItemData weaponItem = ScriptableObject.CreateInstance<WeaponItemData>();
        
        // Set data
        UpdateWeaponItemFromData(weaponItem, weaponData);
        
        LogDebug($"Created new WeaponItemData for '{weaponData.weaponName}' with ID '{weaponData.inventoryItemId}'");
        
        return weaponItem;
    }
    
    private void UpdateWeaponItemFromData(WeaponItemData weaponItem, WeaponData weaponData)
    {
        if (weaponItem == null || weaponData == null) return;
        
        // Set basic ItemData properties
        weaponItem.id = weaponData.inventoryItemId;
        weaponItem.displayName = weaponData.weaponName;
        weaponItem.description = weaponData.description;
        weaponItem.icon = weaponData.weaponIcon;
        weaponItem.prefab = weaponData.weaponPrefab;
        weaponItem.width = weaponData.width;
        weaponItem.height = weaponData.height;
        weaponItem.weight = weaponData.weight;
        weaponItem.canEquip = true;
        weaponItem.canRotate = true;
        weaponItem.category = InventorySystem.ItemCategory.Weapon;
        weaponItem.canStack = false; // Weapons don't stack
        
        // Set WeaponItemData-specific properties
        weaponItem.weaponType = MapToInventoryWeaponType(weaponData.weaponType);
        weaponItem.damage = weaponData.damage;
        weaponItem.fireRate = weaponData.fireRateRPM;
        weaponItem.recoilVertical = weaponData.recoilVertical;
        weaponItem.recoilHorizontal = weaponData.recoilHorizontal;
        weaponItem.ergonomics = weaponData.ergonomics;
        weaponItem.accuracy = weaponData.accuracy;
        weaponItem.maxAmmoCount = weaponData.maxAmmo;
        weaponItem.currentAmmoCount = weaponData.currentAmmo;
        weaponItem.compatibleAmmo = MapToInventoryAmmoType(weaponData.compatibleAmmoType);
        weaponItem.foldable = weaponData.foldable;
        weaponItem.folded = weaponData.folded;
        weaponItem.foldedWidth = weaponData.foldedWidth;
        weaponItem.foldedHeight = weaponData.foldedHeight;
        weaponItem.durability = (int)weaponData.durability;
        weaponItem.maxDurability = (int)weaponData.maxDurability;
        
        // Link back to the WeaponData asset
        weaponItem.gameplayWeaponData = weaponData;
        weaponItem.weaponId = weaponData.WeaponId;
        
        // Define compatible slots based on weapon type
        weaponItem.compatibleSlots = new List<InventorySystem.EquipmentSlot>();
        
        switch (weaponItem.weaponType)
        {
            case InventorySystem.WeaponType.Pistol:
                weaponItem.compatibleSlots.Add(InventorySystem.EquipmentSlot.Holster);
                weaponItem.compatibleSlots.Add(InventorySystem.EquipmentSlot.Secondary);
                break;
            case InventorySystem.WeaponType.SMG:
                weaponItem.compatibleSlots.Add(InventorySystem.EquipmentSlot.Primary);
                weaponItem.compatibleSlots.Add(InventorySystem.EquipmentSlot.Secondary);
                break;
            case InventorySystem.WeaponType.AssaultRifle:
            case InventorySystem.WeaponType.Shotgun:
            case InventorySystem.WeaponType.SniperRifle:
                weaponItem.compatibleSlots.Add(InventorySystem.EquipmentSlot.Primary);
                weaponItem.compatibleSlots.Add(InventorySystem.EquipmentSlot.Secondary);
                break;
            case InventorySystem.WeaponType.Melee:
                weaponItem.compatibleSlots.Add(InventorySystem.EquipmentSlot.Holster);
                break;
        }
        
        // Update properties for display in the UI
        weaponItem.properties.Clear();
        
        weaponItem.AddOrUpdateProperty("Damage", weaponData.damage.ToString(), "", Color.red);
        weaponItem.AddOrUpdateProperty("Rate of Fire", weaponData.fireRateRPM.ToString(), "rpm", Color.white);
        
        Color recoilColor = (weaponData.recoilVertical > 150) ? Color.red : (weaponData.recoilVertical < 100 ? Color.green : Color.yellow);
        weaponItem.AddOrUpdateProperty("Recoil", $"{weaponData.recoilVertical}", "", recoilColor);
        
        Color ergoColor = (weaponData.ergonomics > 70) ? Color.green : (weaponData.ergonomics < 40 ? Color.red : Color.yellow);
        weaponItem.AddOrUpdateProperty("Ergonomics", weaponData.ergonomics.ToString(), "", ergoColor);
        
        Color accuracyColor = (weaponData.accuracy < 3) ? Color.green : (weaponData.accuracy > 6 ? Color.red : Color.yellow);
        weaponItem.AddOrUpdateProperty("Accuracy", weaponData.accuracy.ToString(), "MOA", accuracyColor);
        
        Color durabilityColor = weaponData.durability > weaponData.maxDurability * 0.7f ? Color.green : 
                               weaponData.durability > weaponData.maxDurability * 0.3f ? Color.yellow : Color.red;
        weaponItem.AddOrUpdateProperty("Durability", $"{weaponData.durability}/{weaponData.maxDurability}", "", durabilityColor);
    }
    
    // Modified mapping method - directly maps the int value to avoid namespace issues
    private InventorySystem.WeaponType MapToInventoryWeaponType(WeaponType gameplayWeaponType)
    {
        // Cast the gameplay weapon type to int, then to the inventory system weapon type
        return (InventorySystem.WeaponType)((int)gameplayWeaponType);
    }
    
    // Modified mapping method - directly maps the int value to avoid namespace issues
    private InventorySystem.AmmoType MapToInventoryAmmoType(AmmoType gameplayAmmoType)
    {
        // Cast the gameplay ammo type to int, then to the inventory system ammo type
        return (InventorySystem.AmmoType)((int)gameplayAmmoType);
    }
    
    public void CreateAndEquipWeaponsFromWeaponManager()
    {
        WeaponManager weaponManager = FindObjectOfType<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogError("WeaponManager not found in scene!");
            return;
        }
        
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager not found in scene!");
            return;
        }
        
        WeaponData[] weaponDatas = weaponManager.GetAvailableWeapons();
        if (weaponDatas == null || weaponDatas.Length == 0)
        {
            Debug.LogWarning("No weapon data found in WeaponManager");
            return;
        }
        
        // First create all weapon items in database
        CreateWeaponItemsFromScene();
        
        // Then equip them to appropriate slots
        StartCoroutine(EquipWeaponsIntoSlots(weaponDatas, inventoryManager));
    }
    
    private System.Collections.IEnumerator EquipWeaponsIntoSlots(WeaponData[] weaponDatas, InventoryManager inventoryManager)
    {
        // Wait for database to update
        yield return new WaitForSeconds(0.2f);
        
        foreach (var weaponData in weaponDatas)
        {
            if (weaponData == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) continue;
            
            // Get the item from the database
            ItemData itemData = itemDatabase.GetItem(weaponData.inventoryItemId);
            if (itemData == null || !(itemData is WeaponItemData weaponItem))
            {
                Debug.LogWarning($"Weapon item not found in database: {weaponData.weaponName}");
                continue;
            }
            
            // Add to inventory first
            bool added = inventoryManager.AddItemToInventory(weaponItem.id);
            if (!added)
            {
                Debug.LogWarning($"Failed to add weapon {weaponItem.displayName} to inventory");
                continue;
            }
            
            // Determine the appropriate slot based on weapon type and weapon slot
            InventorySystem.EquipmentSlot targetSlot = InventorySystem.EquipmentSlot.Primary; // Default
            
            // Use weapon slot to determine equipment slot
            switch (weaponData.weaponSlot)
            {
                case 1: // Primary slot
                    targetSlot = InventorySystem.EquipmentSlot.Primary;
                    break;
                case 2: // Secondary slot
                    targetSlot = InventorySystem.EquipmentSlot.Secondary;
                    break;
                case 3: // Holster slot
                    targetSlot = InventorySystem.EquipmentSlot.Holster;
                    break;
                default:
                    // If no specific slot, use the weapon type to determine slot
                    if (weaponItem.weaponType == InventorySystem.WeaponType.Pistol)
                        targetSlot = InventorySystem.EquipmentSlot.Holster;
                    else if (weaponItem.weaponType == InventorySystem.WeaponType.SMG)
                        targetSlot = InventorySystem.EquipmentSlot.Secondary;
                    else
                        targetSlot = InventorySystem.EquipmentSlot.Primary;
                    break;
            }
            
            // Equip to the selected slot
            inventoryManager.EquipItemFromInventory(weaponItem.id, targetSlot);
            LogDebug($"Equipped weapon {weaponItem.displayName} to {targetSlot}");
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void LogDebug(string message)
    {
        if (logDebugInfo)
        {
            Debug.Log($"[WeaponCreator] {message}");
        }
    }
    
    public WeaponItemData CreateSingleWeaponItem(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            Debug.LogError("Cannot create weapon item from null WeaponData");
            return null;
        }
        
        if (string.IsNullOrEmpty(weaponData.inventoryItemId))
        {
            weaponData.inventoryItemId = Guid.NewGuid().ToString();
            LogDebug($"Generated new inventory ID for {weaponData.weaponName}: {weaponData.inventoryItemId}");
        }
        
        // Create a new WeaponItemData
        WeaponItemData weaponItem = CreateWeaponItemFromData(weaponData);
        if (weaponItem != null && itemDatabase != null)
        {
            // Add to database
            ItemData existingItem = itemDatabase.GetItem(weaponData.inventoryItemId);
            if (existingItem != null)
            {
                if (existingItem is WeaponItemData existingWeapon && updateExistingItems)
                {
                    // Update the existing weapon item
                    UpdateWeaponItemFromData(existingWeapon, weaponData);
                    itemDatabase.UpdateItem(existingWeapon);
                    LogDebug($"Updated existing weapon item: {existingWeapon.displayName}");
                    return existingWeapon;
                }
                else
                {
                    LogDebug($"Item with ID '{weaponData.inventoryItemId}' already exists in the database");
                    return existingItem as WeaponItemData;
                }
            }
            else
            {
                bool added = itemDatabase.AddItem(weaponItem);
                if (added)
                {
                    LogDebug($"Added new weapon item to database: {weaponItem.displayName}");
                }
                else
                {
                    Debug.LogError($"Failed to add weapon {weaponItem.displayName} to database");
                }
            }
        }
        
        return weaponItem;
    }
}