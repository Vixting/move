using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using InventorySystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private GameObject mainMenuObject;
    
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
            Debug.LogWarning("Item database not assigned to GameManager");
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
        
        foreach (var weapon in weapons)
        {
            if (!string.IsNullOrEmpty(weapon.inventoryItemId))
            {
                _registeredWeapons[weapon.inventoryItemId] = weapon;
            }
        }
        
        _savedWeapons = weapons;
    }
    
    public WeaponData[] GetSavedWeapons()
    {
        if (_registeredWeapons.Count == 0) return _savedWeapons;
        
        WeaponData[] result = new WeaponData[_registeredWeapons.Count];
        _registeredWeapons.Values.CopyTo(result, 0);
        return result;
    }
    
    public ItemData GetItemById(string id)
    {
        return itemDatabase?.GetItem(id);
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
        
        if (_playerInstance != null && _savedWeapons != null)
        {
            WeaponManager weaponManager = _playerInstance.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                weaponManager.SetAvailableWeapons(_savedWeapons);
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
            
            if (_inventoryManager != null && _playerInstance != null)
            {
                InventoryWeaponBridge bridge = FindObjectOfType<InventoryWeaponBridge>();
                if (bridge == null)
                {
                    bridge = _playerInstance.gameObject.AddComponent<InventoryWeaponBridge>();
                }
                
                bridge.SyncWeaponsWithInventory();
            }
        }
        
        // Notify listeners about the level change
        OnLevelLoadedEvent?.Invoke(isGameplayLevel);
    }
}