// using UnityEngine;
// using UnityEngine.UI;
// using InventorySystem;

// /// <summary>
// /// Test script for adding weapons to inventory.
// /// Attach to any GameObject in your scene.
// /// </summary>
// public class WeaponTester : MonoBehaviour
// {
//     [Header("Weapon IDs to Test")]
//     [SerializeField] private string[] weaponIds = { "1" };
//     [SerializeField] private bool equipImmediately = true;
    
//     [Header("UI Elements (Optional)")]
//     [SerializeField] private Button addWeaponButton;
//     [SerializeField] private Button clearWeaponsButton;
    
//     [Header("Debug")]
//     [SerializeField] private bool showDebugLogs = true;
    
//     private InventoryManager _inventoryManager;
//     private int _currentWeaponIndex = 0;
    
//     private void Start()
//     {
//         // Find the inventory manager if it's not already set
//         _inventoryManager = FindObjectOfType<InventoryManager>();
        
//         // Set up UI buttons if available
//         if (addWeaponButton != null)
//         {
//             addWeaponButton.onClick.AddListener(AddNextWeapon);
//         }
        
//         if (clearWeaponsButton != null)
//         {
//             clearWeaponsButton.onClick.AddListener(RemoveAllWeapons);
//         }
        
//         LogMessage("WeaponTester initialized.");
//     }
    
//     /// <summary>
//     /// Adds the next weapon in the weaponIds array
//     /// </summary>
//     public void AddNextWeapon()
//     {
//         if (_inventoryManager == null)
//         {
//             _inventoryManager = FindObjectOfType<InventoryManager>();
//             if (_inventoryManager == null)
//             {
//                 LogError("Inventory Manager not found in scene!");
//                 return;
//             }
//         }
        
//         if (weaponIds.Length == 0)
//         {
//             LogError("No weapon IDs configured!");
//             return;
//         }
        
//         // Get the next weapon ID from the array
//         string weaponId = weaponIds[_currentWeaponIndex];
//         _currentWeaponIndex = (_currentWeaponIndex + 1) % weaponIds.Length;
        
//         // Add the weapon to inventory
//         _inventoryManager.AddWeaponToInventory(weaponId, equipImmediately);
        
//         // Log for feedback
//         LogMessage($"Added weapon with ID {weaponId} to inventory.");
//     }
    
//     /// <summary>
//     /// Adds a specific weapon by ID
//     /// </summary>
//     public void AddWeapon(string weaponId)
//     {
//         if (_inventoryManager == null)
//         {
//             _inventoryManager = FindObjectOfType<InventoryManager>();
//             if (_inventoryManager == null)
//             {
//                 LogError("Inventory Manager not found in scene!");
//                 return;
//             }
//         }
        
//         // Add the weapon to inventory
//         _inventoryManager.AddWeaponToInventory(weaponId, equipImmediately);
        
//         // Log for feedback
//         LogMessage($"Added weapon with ID {weaponId} to inventory.");
//     }
    
//     /// <summary>
//     /// Removes all weapons by unequipping them
//     /// </summary>
//     public void RemoveAllWeapons()
//     {
//         if (_inventoryManager == null)
//         {
//             _inventoryManager = FindObjectOfType<InventoryManager>();
//             if (_inventoryManager == null)
//             {
//                 LogError("Inventory Manager not found in scene!");
//                 return;
//             }
//         }
        
//         // Unequip weapons from all slots
//         _inventoryManager.UnequipItem(EquipmentSlot.Primary);
//         _inventoryManager.UnequipItem(EquipmentSlot.Secondary);
//         _inventoryManager.UnequipItem(EquipmentSlot.Holster);
        
//         LogMessage("Removed all equipped weapons.");
//     }

//     // Methods for adding specific weapons by index
//     public void AddWeapon0()
//     {
//         if (weaponIds.Length > 0)
//             AddWeapon(weaponIds[0]);
//     }
    
//     public void AddWeapon1()
//     {
//         if (weaponIds.Length > 1)
//             AddWeapon(weaponIds[1]);
//     }
    
//     public void AddWeapon2()
//     {
//         if (weaponIds.Length > 2)
//             AddWeapon(weaponIds[2]);
//     }
    
//     private void LogMessage(string message)
//     {
//         if (showDebugLogs)
//         {
//             Debug.Log($"[WeaponTester] {message}");
//         }
//     }
    
//     private void LogError(string message)
//     {
//         Debug.LogError($"[WeaponTester] {message}");
//     }
// }