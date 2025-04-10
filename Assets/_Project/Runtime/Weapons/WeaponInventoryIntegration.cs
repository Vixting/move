using UnityEngine;
using InventorySystem;
using System.Collections.Generic;

public class WeaponInventoryIntegration : MonoBehaviour
{
    private static WeaponInventoryIntegration _instance;
    public static WeaponInventoryIntegration Instance => _instance;

    [SerializeField] private InventoryManager inventoryManager;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this);
        }
        
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
        }
    }
    
    public void SyncWeaponToInventory(Weapon weapon)
    {
        if (weapon == null || inventoryManager == null) return;
        
        WeaponData weaponData = weapon.GetWeaponData();
        if (weaponData == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) return;
        
        ItemData itemData = GameManager.Instance?.GetItemById(weaponData.inventoryItemId);
        if (itemData != null && itemData is WeaponItemData weaponItemData)
        {
            weaponItemData.currentAmmoCount = weapon.CurrentAmmo;
            inventoryManager.UpdateWeaponAmmo(weaponItemData, weapon.CurrentAmmo);
        }
    }
    
    public int GetAmmoForWeapon(WeaponData weaponData)
    {
        if (weaponData == null || string.IsNullOrEmpty(weaponData.inventoryItemId) || inventoryManager == null) return 0;
        
        ItemData itemData = GameManager.Instance?.GetItemById(weaponData.inventoryItemId);
        if (itemData != null && itemData is WeaponItemData weaponItemData)
        {
            return weaponItemData.currentAmmoCount;
        }
        
        return 0;
    }
    
    public void UpdateAmmo(int currentAmmo)
    {
        Weapon weapon = GetComponentInParent<Weapon>();
        if (weapon != null)
        {
            SyncWeaponToInventory(weapon);
        }
    }
    
    public bool TryReloadFromInventory(Weapon weapon, out int ammoLoaded)
    {
        ammoLoaded = weapon.GetWeaponData()?.maxAmmo ?? 30;
        
        if (weapon == null || inventoryManager == null) return false;
        
        WeaponData weaponData = weapon.GetWeaponData();
        if (weaponData == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) return false;
        
        ItemData itemData = GameManager.Instance?.GetItemById(weaponData.inventoryItemId);
        if (!(itemData is WeaponItemData weaponItemData)) return false;
        
        // Check if we have ammo items for this weapon
        InventorySystem.Character character = inventoryManager.GetCharacter();
        if (character == null) return false;
        
        List<ItemInstance> ammoItems = inventoryManager.FindItemsByCategory(ItemCategory.Ammunition);        bool foundCompatibleAmmo = false;
        
        foreach (var ammoItem in ammoItems)
        {
            if (ammoItem.itemData is AmmoItemData ammoData)
            {
                // Check if ammo type matches using ToString comparison
                if (ammoData.ammoType.ToString() == weaponData.compatibleAmmoType.ToString())
                {
                    foundCompatibleAmmo = true;
                    
                    // Found compatible ammo - full reload
                    weaponItemData.currentAmmoCount = weaponData.maxAmmo;
                    inventoryManager.UpdateWeaponAmmo(weaponItemData, weaponData.maxAmmo);
                    ammoLoaded = weaponData.maxAmmo;
                    
                    // If we want to consume ammo items:
                    // inventoryManager.ConsumeAmmo(ammoItem, weaponData.maxAmmo - weapon.CurrentAmmo);
                    
                    return true;
                }
            }
        }
        
        // No compatible ammo found, but still reload to max (gameplay consideration)
        weaponItemData.currentAmmoCount = weaponData.maxAmmo;
        inventoryManager.UpdateWeaponAmmo(weaponItemData, weaponData.maxAmmo);
        
        return true;
    }
    
    public void DecreaseDurability(Weapon weapon, float amount)
    {
        if (weapon == null || inventoryManager == null) return;
        
        WeaponData weaponData = weapon.GetWeaponData();
        if (weaponData == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) return;
        
        ItemData itemData = GameManager.Instance?.GetItemById(weaponData.inventoryItemId);
        if (!(itemData is WeaponItemData weaponItemData)) return;
        
        // Implement durability decrease logic if your WeaponItemData has durability
    }
}