using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using InventorySystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Database References")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private WeaponDatabase weaponDatabase;
    
    [Header("Game Systems")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private GameObject mainMenuObject;
    [SerializeField] private RuntimeWeaponItemCreator weaponItemCreator;
    
    [Header("Weapon Integration")]
    [SerializeField] private bool autoSyncWeaponsOnStart = true;
    
    private Dictionary<string, WeaponData> _registeredWeapons = new Dictionary<string, WeaponData>();
    private MainMenuController _menuController;
    private Player _playerInstance;
    private WeaponData[] _savedWeapons;
    private InventoryManager _inventoryManager;
    
    public event Action<bool> OnLevelLoadedEvent;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (itemDatabase == null)
        {
            itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
        }
        
        if (weaponDatabase == null)
        {
            weaponDatabase = Resources.Load<WeaponDatabase>("WeaponDatabase");
        }
        
        if (weaponItemCreator == null)
        {
            weaponItemCreator = FindObjectOfType<RuntimeWeaponItemCreator>();
            
            if (weaponItemCreator == null && itemDatabase != null)
            {
                GameObject creatorObj = new GameObject("WeaponItemCreator");
                creatorObj.transform.SetParent(transform);
                weaponItemCreator = creatorObj.AddComponent<RuntimeWeaponItemCreator>();
            }
        }
        
        if (weaponDatabase != null)
        {
            weaponDatabase.Initialize();
            
            if (autoSyncWeaponsOnStart && weaponItemCreator != null)
            {
                weaponDatabase.EnsureWeaponItemsExist(weaponItemCreator);
            }
        }
        

        if (mainMenuObject != null)
        {
            _menuController = mainMenuObject.GetComponent<MainMenuController>();
            if (_menuController == null)
            {
                _menuController = mainMenuObject.AddComponent<MainMenuController>();
            }
           
            _menuController.SetupMenu(mainMenuDocument, levelManager);
        }
        else if (mainMenuDocument != null)
        {
            GameObject menuObj = mainMenuDocument.gameObject;
            _menuController = menuObj.GetComponent<MainMenuController>();
            if (_menuController == null)
            {
                _menuController = menuObj.AddComponent<MainMenuController>();
            }
           
            _menuController.SetupMenu(mainMenuDocument, levelManager);
        }
    }
    

    
    private void Start()
    {
        if (_menuController != null && mainMenuDocument != null)
        {
            _menuController.SetupMenu(mainMenuDocument, levelManager);
        }
    }
    
    public void RegisterPlayer(Player player)
    {
        _playerInstance = player;
        
        if (_inventoryManager == null)
        {
            _inventoryManager = FindObjectOfType<InventoryManager>();
        }
    }
    
    public void RegisterWeapons(WeaponData[] weapons)
    {
        if (weapons == null) return;
        
        Debug.Log($"Registering {weapons.Length} weapons");
        
        foreach (var weapon in weapons)
        {
            if (weapon == null) continue;
            
            if (string.IsNullOrEmpty(weapon.inventoryItemId))
            {
                weapon.inventoryItemId = System.Guid.NewGuid().ToString();
                Debug.Log($"Generated new inventory ID for weapon {weapon.weaponName}: {weapon.inventoryItemId}");
            }
            
            _registeredWeapons[weapon.inventoryItemId] = weapon;
            
            if (weaponDatabase != null)
            {
                weaponDatabase.AddWeapon(weapon);
            }
        }
        
        _savedWeapons = weapons;
        
        EnsureWeaponItemsExist();
    }
    
    public WeaponData[] GetSavedWeapons()
    {
        if (weaponDatabase != null)
        {
            WeaponData[] dbWeapons = weaponDatabase.GetAllWeapons();
            if (dbWeapons != null && dbWeapons.Length > 0)
            {
                return dbWeapons;
            }
        }
        
        if (_registeredWeapons.Count > 0)
        {
            WeaponData[] result = new WeaponData[_registeredWeapons.Count];
            _registeredWeapons.Values.CopyTo(result, 0);
            return result;
        }
        
        return _savedWeapons;
    }
    
    public ItemData GetItemById(string id)
    {
        return itemDatabase?.GetItem(id);
    }
    
    public void EnsureWeaponItemsExist()
    {
        if (weaponItemCreator == null)
        {
            return;
        }
        
        if (weaponDatabase != null)
        {
            weaponDatabase.EnsureWeaponItemsExist(weaponItemCreator);
        }
        else
        {
            weaponItemCreator.CreateWeaponItemsFromGameManager();
        }
    }
    
    public WeaponData GetWeaponByInventoryItemId(string inventoryItemId)
    {
        if (string.IsNullOrEmpty(inventoryItemId)) return null;
        
        if (weaponDatabase != null)
        {
            WeaponData weapon = weaponDatabase.GetWeaponByInventoryItemId(inventoryItemId);
            if (weapon != null) return weapon;
        }
        
        if (_registeredWeapons.TryGetValue(inventoryItemId, out WeaponData registeredWeapon))
        {
            return registeredWeapon;
        }
        
        return null;
    }
    
    public void SaveGame()
    {
        if (_inventoryManager != null)
        {
            InventorySystem.Character playerCharacter = _inventoryManager.GetCharacter();
            if (playerCharacter != null)
            {
                playerCharacter.SaveInventory();
            }
        }
        
        if (_playerInstance != null)
        {
            WeaponManager weaponManager = _playerInstance.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                _savedWeapons = weaponManager.GetAvailableWeapons();
                RegisterWeapons(_savedWeapons);
            }
        }
    }
    
    public void LoadGame()
    {
        if (_inventoryManager != null)
        {
            InventorySystem.Character playerCharacter = _inventoryManager.GetCharacter();
            if (playerCharacter != null)
            {
                playerCharacter.LoadInventory();
            }
        }
        
        if (_playerInstance != null)
        {
            WeaponManager weaponManager = _playerInstance.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                WeaponData[] weaponsToLoad = GetSavedWeapons();
                EnsureWeaponItemsExist();
                weaponManager.SetAvailableWeapons(weaponsToLoad);
            }
        }
    }
    
    public void OnLevelLoaded(bool isGameplayLevel)
    {
        if (mainMenuDocument != null)
        {
            mainMenuDocument.gameObject.SetActive(!isGameplayLevel);
        }
    
        if (_playerInstance != null)
        {
            _playerInstance.EnableGameplayMode(isGameplayLevel);
        }
        
        if (isGameplayLevel)
        {
            _inventoryManager = FindObjectOfType<InventoryManager>();
            

        }
        
        OnLevelLoadedEvent?.Invoke(isGameplayLevel);
    }
    
    public void AddWeaponToInventory(string weaponId)
    {
        if (_inventoryManager == null || string.IsNullOrEmpty(weaponId))
        {
            return;
        }
        
        EnsureWeaponItemsExist();
        _inventoryManager.AddWeaponToInventory(weaponId);
    }
    
    public void EquipWeapon(string weaponId, EquipmentSlot slot)
    {
        if (_inventoryManager == null || string.IsNullOrEmpty(weaponId))
        {
            return;
        }
        
        _inventoryManager.EquipItemFromInventory(weaponId, slot);
    }
    
    public void ReloadAllWeapons()
    {
        if (_playerInstance == null) return;
        
        WeaponManager weaponManager = _playerInstance.GetComponent<WeaponManager>();
        if (weaponManager == null) return;
        
        WeaponData[] weapons = weaponManager.GetAvailableWeapons();
        
        foreach (var weapon in weapons)
        {
            if (weapon != null)
            {
                ItemData itemData = GetItemById(weapon.inventoryItemId);
                if (itemData != null && itemData is WeaponItemData weaponItem)
                {
                    weaponItem.currentAmmoCount = weapon.maxAmmo;
                    weapon.currentAmmo = weapon.maxAmmo;
                }
            }
        }
        

    }
    
    public bool HasAmmoForWeapon(WeaponData weapon)
    {
        if (weapon == null || _inventoryManager == null) return false;
        
        return _inventoryManager.HasAmmoForWeapon(weapon.compatibleAmmoType.ToString());
    }
    
    public bool UseAmmoForWeapon(WeaponData weapon, int amount)
    {
        if (weapon == null || _inventoryManager == null) return false;
        
        return _inventoryManager.UseAmmoForWeapon(weapon.compatibleAmmoType.ToString(), amount);
    }
    
    public WeaponData CreateWeapon(string weaponName, WeaponType type, float damage, int maxAmmo)
    {
        WeaponData weaponData = ScriptableObject.CreateInstance<WeaponData>();
        weaponData.weaponName = weaponName;
        weaponData.weaponType = type;
        weaponData.damage = damage;
        weaponData.maxAmmo = maxAmmo;
        weaponData.currentAmmo = maxAmmo;
        weaponData.inventoryItemId = System.Guid.NewGuid().ToString();
        
        RegisterWeapons(new WeaponData[] { weaponData });
        
        return weaponData;
    }
}