using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using InventorySystem;

public class WeaponSyncFix : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private RuntimeWeaponItemCreator weaponItemCreator;
    
    [Header("Existing Weapons")]
    [SerializeField] private WeaponData rifleData;
    [SerializeField] private WeaponData shotgunData;
    
    [Header("Equipment Settings")]
    [SerializeField] private EquipmentSlot rifleSlot = EquipmentSlot.Primary;
    [SerializeField] private EquipmentSlot shotgunSlot = EquipmentSlot.Secondary;
    
    [Header("Settings")]
    [SerializeField] private float initializationDelay = 2.0f;
    [SerializeField] private float weaponSyncDelay = 0.5f;
    [SerializeField] private int maxRetryAttempts = 5;
    
    private int _retryCount = 0;
    
    private void Start()
    {
        StartCoroutine(DelayedInitialization());
    }
    
    private IEnumerator DelayedInitialization()
    {
        Debug.Log("[WeaponSyncFix] Starting weapon initialization...");
        yield return new WaitForSeconds(initializationDelay);
        
        // Step 1: Fix weapon IDs if needed
        FixWeaponIds();
        
        // Step 2: Register weapons with GameManager
        RegisterWeaponsWithGameManager();
        
        // Step 3: Create inventory items
        yield return StartCoroutine(CreateInventoryItems());
        
        // Step 4: Add weapons to inventory
        yield return StartCoroutine(AddWeaponsToInventory());
        
        // Step 5: Equip weapons - THIS STEP WAS MISSING
        yield return StartCoroutine(EquipWeapons());

        // Step 6: Final sync with weapon manager
        yield return StartCoroutine(SyncWithWeaponManager());
    }
    
    private void FixWeaponIds()
    {
        if (rifleData != null)
        {
            // Ensure weapon ID is valid
            string weaponId = rifleData.WeaponId;
            
            // Ensure inventory ID is valid
            if (string.IsNullOrEmpty(rifleData.inventoryItemId) || rifleData.inventoryItemId == "0")
            {
                rifleData.inventoryItemId = System.Guid.NewGuid().ToString();
                Debug.Log($"[WeaponSyncFix] Fixed rifle inventory ID: {rifleData.inventoryItemId}");
            }
            
            if (rifleData.weaponSlot <= 0)
            {
                rifleData.weaponSlot = 1;
            }
        }
        
        if (shotgunData != null)
        {
            // Ensure weapon ID is valid
            string weaponId = shotgunData.WeaponId;
            
            // Ensure inventory ID is valid
            if (string.IsNullOrEmpty(shotgunData.inventoryItemId) || shotgunData.inventoryItemId == "1")
            {
                shotgunData.inventoryItemId = System.Guid.NewGuid().ToString();
                Debug.Log($"[WeaponSyncFix] Fixed shotgun inventory ID: {shotgunData.inventoryItemId}");
            }
            
            if (shotgunData.weaponSlot <= 0)
            {
                shotgunData.weaponSlot = 2;
            }
        }
    }
    
    private void RegisterWeaponsWithGameManager()
    {
        if (GameManager.Instance == null) return;
        
        List<WeaponData> weapons = new List<WeaponData>();
        if (rifleData != null) weapons.Add(rifleData);
        if (shotgunData != null) weapons.Add(shotgunData);
        
        if (weapons.Count > 0)
        {
            Debug.Log($"[WeaponSyncFix] Registering {weapons.Count} weapons with GameManager");
            GameManager.Instance.RegisterWeapons(weapons.ToArray());
        }
    }
    
    private IEnumerator CreateInventoryItems()
    {
        if (weaponDatabase != null && weaponItemCreator != null)
        {
            Debug.Log("[WeaponSyncFix] Creating inventory items for weapons");
            weaponDatabase.EnsureWeaponItemsExist(weaponItemCreator);
            yield return new WaitForSeconds(weaponSyncDelay);
        }
    }
    
    private IEnumerator AddWeaponsToInventory()
    {
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null) 
        {
            Debug.LogError("[WeaponSyncFix] Cannot find InventoryManager");
            yield break;
        }
        
        // Add rifle to inventory
        if (rifleData != null && !string.IsNullOrEmpty(rifleData.inventoryItemId))
        {
            Debug.Log($"[WeaponSyncFix] Adding rifle to inventory: {rifleData.inventoryItemId}");
            inventoryManager.AddWeaponToInventory(rifleData.inventoryItemId, false);
            yield return new WaitForSeconds(0.2f);
        }
        
        // Add shotgun to inventory
        if (shotgunData != null && !string.IsNullOrEmpty(shotgunData.inventoryItemId))
        {
            Debug.Log($"[WeaponSyncFix] Adding shotgun to inventory: {shotgunData.inventoryItemId}");
            inventoryManager.AddWeaponToInventory(shotgunData.inventoryItemId, false);
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    
    private IEnumerator EquipWeapons()
    {
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null) 
        {
            Debug.LogError("[WeaponSyncFix] Cannot find InventoryManager for equipping weapons");
            yield break;
        }
        
        // Make sure we have a character to equip items to
        Character character = inventoryManager.GetCharacter();
        if (character == null)
        {
            Debug.Log("[WeaponSyncFix] Character not found from InventoryManager, searching for Player...");
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                // Try to get Character component
                character = player.GetCharacter();
                if (character == null)
                {
                    // Try to add Character component
                    character = player.gameObject.GetComponent<Character>();
                    if (character == null)
                    {
                        Debug.Log("[WeaponSyncFix] Adding Character component to Player");
                        character = player.gameObject.AddComponent<Character>();
                        yield return null; // Wait a frame for initialization
                        
                        // Important: Set the character on the inventory manager
                        var characterField = typeof(InventoryManager).GetField("_character", 
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (characterField != null)
                        {
                            characterField.SetValue(inventoryManager, character);
                            Debug.Log("[WeaponSyncFix] Set character reference on InventoryManager");
                        }
                    }
                }
            }
        }
        
        if (character == null)
        {
            Debug.LogError("[WeaponSyncFix] Cannot find or create Character component");
            yield break;
        }
        
        // Ensure the character has inventory data
        if (character.InventoryData == null)
        {
            Debug.Log("[WeaponSyncFix] Initializing character inventory data");
            character.LoadInventory();
            yield return null;
        }
        
        // Now actually equip the weapons to their proper slots
        Debug.Log("[WeaponSyncFix] Equipping weapons to their slots");
        
        bool rifleEquipped = false;
        bool shotgunEquipped = false;
        
        // Get items from inventory
        if (rifleData != null && !string.IsNullOrEmpty(rifleData.inventoryItemId))
        {
            // Find rifle item in inventory
            ItemInstance rifleItem = FindItemInInventory(inventoryManager, rifleData.inventoryItemId);
            if (rifleItem != null)
            {
                // Ensure item is a weapon and can be equipped
                if (rifleItem.itemData is WeaponItemData && rifleItem.CanEquipInSlot(rifleSlot))
                {
                    bool equipped = inventoryManager.EquipItem(rifleItem, rifleSlot);
                    Debug.Log($"[WeaponSyncFix] Equipped rifle to {rifleSlot}: {equipped}");
                    rifleEquipped = equipped;
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    Debug.LogWarning($"[WeaponSyncFix] Rifle item cannot be equipped in slot {rifleSlot}");
                }
            }
            else
            {
                Debug.LogWarning($"[WeaponSyncFix] Could not find rifle item in inventory: {rifleData.inventoryItemId}");
            }
        }
        
        if (shotgunData != null && !string.IsNullOrEmpty(shotgunData.inventoryItemId))
        {
            // Find shotgun item in inventory
            ItemInstance shotgunItem = FindItemInInventory(inventoryManager, shotgunData.inventoryItemId);
            if (shotgunItem != null)
            {
                // Ensure item is a weapon and can be equipped
                if (shotgunItem.itemData is WeaponItemData && shotgunItem.CanEquipInSlot(shotgunSlot))
                {
                    bool equipped = inventoryManager.EquipItem(shotgunItem, shotgunSlot);
                    Debug.Log($"[WeaponSyncFix] Equipped shotgun to {shotgunSlot}: {equipped}");
                    shotgunEquipped = equipped;
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    Debug.LogWarning($"[WeaponSyncFix] Shotgun item cannot be equipped in slot {shotgunSlot}");
                }
            }
            else
            {
                Debug.LogWarning($"[WeaponSyncFix] Could not find shotgun item in inventory: {shotgunData.inventoryItemId}");
            }
        }
        
        // Force inventory changed event to trigger bridge update
        inventoryManager.onInventoryChanged?.Invoke();
        
        // Return true if at least one weapon was equipped
        yield return (rifleEquipped || shotgunEquipped);
    }
    
    private ItemInstance FindItemInInventory(InventoryManager inventoryManager, string itemId)
    {
        // Check all containers for the item
        foreach (var container in inventoryManager.GetContainers().Values)
        {
            foreach (var item in container.GetAllItems())
            {
                if (item.itemData.id == itemId)
                {
                    Debug.Log($"[WeaponSyncFix] Found item {item.itemData.displayName} in container {container.containerData.id}");
                    return item;
                }
            }
        }
        
        // Try direct lookup by ID
        ItemInstance directItem = inventoryManager.GetItemById(itemId);
        if (directItem != null)
        {
            Debug.Log($"[WeaponSyncFix] Found item {directItem.itemData.displayName} via direct lookup");
            return directItem;
        }
        
        Debug.LogWarning($"[WeaponSyncFix] Could not find item with ID {itemId} in any container");
        return null;
    }
    
    private IEnumerator SyncWithWeaponManager()
    {
        Player player = FindObjectOfType<Player>();
        if (player == null) 
        {
            Debug.LogError("[WeaponSyncFix] Cannot find Player for final sync");
            yield break;
        }
        
        WeaponManager weaponManager = player.GetComponent<WeaponManager>();
        if (weaponManager == null) 
        {
            Debug.LogError("[WeaponSyncFix] Cannot find WeaponManager for final sync");
            yield break;
        }
        
        // Find inventory weapon bridge
        InventoryWeaponBridge weaponBridge = player.GetComponent<InventoryWeaponBridge>();
        if (weaponBridge == null)
        {
            Debug.Log("[WeaponSyncFix] Creating InventoryWeaponBridge component");
            weaponBridge = player.gameObject.AddComponent<InventoryWeaponBridge>();
            yield return null; // Wait a frame to ensure initialization
        }
        
        // Force a synchronization
        Debug.Log("[WeaponSyncFix] Forcing final weapon synchronization");
        
        // Find InventoryManager to get equipped weapons
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null)
        {
            // Update references in case they're missing
            var weaponManagerField = typeof(InventoryWeaponBridge).GetField("weaponManager", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            var inventoryManagerField = typeof(InventoryWeaponBridge).GetField("inventoryManager", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            var itemDatabaseField = typeof(InventoryWeaponBridge).GetField("itemDatabase",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
            if (weaponManagerField != null)
                weaponManagerField.SetValue(weaponBridge, weaponManager);
                
            if (inventoryManagerField != null)
                inventoryManagerField.SetValue(weaponBridge, inventoryManager);
                
            if (itemDatabaseField != null && itemDatabase != null)
                itemDatabaseField.SetValue(weaponBridge, itemDatabase);
            
            // Force bridge operations
            weaponBridge.MapAvailableWeapons();
            weaponBridge.SyncWeaponsWithInventory();
            
            // Get equipped weapon data from inventory
            WeaponData[] equippedWeapons = inventoryManager.GetEquippedWeaponData();
            
            if (equippedWeapons != null && equippedWeapons.Length > 0)
            {
                Debug.Log($"[WeaponSyncFix] Found {equippedWeapons.Length} equipped weapons in inventory");
                
                // Validate weapons have prefabs
                bool allValid = true;
                foreach (var weapon in equippedWeapons)
                {
                    if (weapon == null)
                    {
                        Debug.LogError("[WeaponSyncFix] Null weapon in equipped weapons array!");
                        allValid = false;
                        continue;
                    }
                    
                    if (weapon.weaponPrefab == null)
                    {
                        Debug.LogError($"[WeaponSyncFix] Weapon {weapon.weaponName} has null prefab!");
                        allValid = false;
                    }
                }
                
                // Final validation that equipped weapons match inventory
                if (allValid)
                {
                    Debug.Log("[WeaponSyncFix] Weapon setup complete - all weapons valid");
                }
                else
                {
                    Debug.LogError("[WeaponSyncFix] Some weapons have invalid data");
                }
            }
            else
            {
                Debug.LogWarning("[WeaponSyncFix] No equipped weapons found in inventory after setup");
            }
        }
        else
        {
            Debug.LogError("[WeaponSyncFix] Could not find InventoryManager for final sync");
        }
        
        Debug.Log("[WeaponSyncFix] Weapon setup complete");
    }
}