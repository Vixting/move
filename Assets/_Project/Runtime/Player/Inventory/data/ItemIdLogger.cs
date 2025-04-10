// Create a new file called ItemIdLogger.cs in a different location
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

public class ItemIdLogger : MonoBehaviour
{
    [SerializeField] private bool logOnAwake = true;
    [SerializeField] private bool logWeaponsOnly = false;
    
    private void Awake()
    {
        if (logOnAwake)
        {
            LogAllItemIds();
        }
    }
    
    [ContextMenu("Log All Item IDs")]
    public void LogAllItemIds()
    {
        if (logWeaponsOnly)
        {
            LogWeaponItemIds();
        }
        else
        {
            LogAllItems();
        }
    }
    
    public void LogAllItems()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance not found!");
            return;
        }
        
        // Get all item IDs from your ItemDatabase through GameManager's GetItemById method
        // We'll list each item in inventory slots to see what items are registered
        List<string> loggedIds = new List<string>();
        
        Debug.Log("=== Starting Item ID Log ===");
        
        // Log all equipped weapons to get their IDs
        if (InventoryManager.Instance != null)
        {
            WeaponData[] equippedWeapons = InventoryManager.Instance.GetEquippedWeaponData();
            
            Debug.Log($"Equipped Weapons: {equippedWeapons.Length}");
            foreach (var weaponData in equippedWeapons)
            {
                string id = weaponData.inventoryItemId;
                if (!string.IsNullOrEmpty(id) && !loggedIds.Contains(id))
                {
                    ItemData item = GameManager.Instance.GetItemById(id);
                    if (item != null)
                    {
                        Debug.Log($"Item: {item.displayName} | ID: {item.id} | Category: {item.category}");
                        loggedIds.Add(id);
                    }
                }
            }
        }
        
        // Since we can't get all items directly, list registered weapons from GameManager
        WeaponData[] savedWeapons = GameManager.Instance.GetSavedWeapons();
        if (savedWeapons != null && savedWeapons.Length > 0)
        {
            Debug.Log($"Registered Weapons: {savedWeapons.Length}");
            foreach (var weaponData in savedWeapons)
            {
                string id = weaponData.inventoryItemId;
                if (!string.IsNullOrEmpty(id) && !loggedIds.Contains(id))
                {
                    ItemData item = GameManager.Instance.GetItemById(id);
                    if (item != null)
                    {
                        Debug.Log($"Item: {item.displayName} | ID: {item.id} | Category: {item.category}");
                        loggedIds.Add(id);
                    }
                }
            }
        }
        
        Debug.Log($"=== Total items logged: {loggedIds.Count} ===");
    }
    
    public void LogWeaponItemIds()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance not found!");
            return;
        }
        
        List<string> loggedIds = new List<string>();
        
        Debug.Log("=== Starting Weapon ID Log ===");
        
        // Log all equipped weapons first
        if (InventoryManager.Instance != null)
        {
            WeaponData[] equippedWeapons = InventoryManager.Instance.GetEquippedWeaponData();
            
            Debug.Log($"Equipped Weapons: {equippedWeapons.Length}");
            foreach (var weaponData in equippedWeapons)
            {
                string id = weaponData.inventoryItemId;
                if (!string.IsNullOrEmpty(id) && !loggedIds.Contains(id))
                {
                    ItemData item = GameManager.Instance.GetItemById(id);
                    if (item != null && item is WeaponItemData weaponItem)
                    {
                        LogWeaponDetails(weaponItem, weaponData);
                        loggedIds.Add(id);
                    }
                }
            }
        }
        
        // Log registered weapons from GameManager
        WeaponData[] savedWeapons = GameManager.Instance.GetSavedWeapons();
        if (savedWeapons != null && savedWeapons.Length > 0)
        {
            Debug.Log($"Registered Weapons: {savedWeapons.Length}");
            foreach (var weaponData in savedWeapons)
            {
                string id = weaponData.inventoryItemId;
                if (!string.IsNullOrEmpty(id) && !loggedIds.Contains(id))
                {
                    ItemData item = GameManager.Instance.GetItemById(id);
                    if (item != null && item is WeaponItemData weaponItem)
                    {
                        LogWeaponDetails(weaponItem, weaponData);
                        loggedIds.Add(id);
                    }
                }
            }
        }
        
        Debug.Log($"=== Total weapons logged: {loggedIds.Count} ===");
    }
    
    private void LogWeaponDetails(WeaponItemData weaponItem, WeaponData weaponData)
    {
        Debug.Log($"Weapon: {weaponItem.displayName} | ID: {weaponItem.id} | Type: {weaponItem.weaponType}");
        
        // Verify if the IDs match
        if (weaponItem.id != weaponData.inventoryItemId)
        {
            Debug.LogWarning($"  - WARNING: ID mismatch! ItemData.id ({weaponItem.id}) ≠ WeaponData.inventoryItemId ({weaponData.inventoryItemId})");
        }
        
        if (weaponItem.gameplayWeaponData != null)
        {
            if (weaponItem.gameplayWeaponData != weaponData)
            {
                Debug.LogWarning($"  - WARNING: Different WeaponData instances! ItemData.gameplayWeaponData ≠ Current WeaponData");
            }
        }
        else
        {
            Debug.LogWarning($"  - No gameplayWeaponData assigned to the WeaponItemData!");
        }
    }
    
    public static List<string> GetAllWeaponIds()
    {
        List<string> ids = new List<string>();
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance not found!");
            return ids;
        }
        
        // Get weapon IDs from registered weapons in GameManager
        WeaponData[] savedWeapons = GameManager.Instance.GetSavedWeapons();
        if (savedWeapons != null)
        {
            foreach (var weapon in savedWeapons)
            {
                if (!string.IsNullOrEmpty(weapon.inventoryItemId) && !ids.Contains(weapon.inventoryItemId))
                {
                    ids.Add(weapon.inventoryItemId);
                }
            }
        }
        
        return ids;
    }
}