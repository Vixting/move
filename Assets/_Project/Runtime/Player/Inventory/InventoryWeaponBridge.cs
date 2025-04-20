using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using InventorySystem;

namespace InventorySystem
{
    public class InventoryWeaponBridge : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private float initializationDelay = 0.5f;
        [SerializeField] private int maxRetryAttempts = 5;
        [SerializeField] private float validationInterval = 0.5f;
        [SerializeField] private float characterFindInterval = 1.0f;
        
        // Scene context checks
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool disableInMainMenu = true;

        private Dictionary<string, WeaponData> _weaponDataMap = new Dictionary<string, WeaponData>();
        private Dictionary<EquipmentSlot, int> _equipSlotToWeaponSlot = new Dictionary<EquipmentSlot, int>();
        private Dictionary<int, int> _weaponSlotAmmoState = new Dictionary<int, int>();
        private bool _initialized = false;
        private bool _subscribed = false;
        private int _retryCount = 0;
        private float _lastValidationTime = 0f;
        private float _lastCharacterFindTime = 0f;
        private Character _cachedCharacter = null;
        private bool _isInGameplay = false;

        private void Awake()
        {
            _equipSlotToWeaponSlot[EquipmentSlot.Primary] = 1;
            _equipSlotToWeaponSlot[EquipmentSlot.Secondary] = 2;
            _equipSlotToWeaponSlot[EquipmentSlot.Holster] = 3;
            
            TryFindRequiredComponents();
            CheckCurrentSceneContext();
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                TryInitialize();
            }
            else if (!_subscribed)
            {
                SubscribeToEvents();
            }
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable()
        {
            UnsubscribeFromEvents();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckCurrentSceneContext();
        }
        
        private void CheckCurrentSceneContext()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            _isInGameplay = (currentScene.name != mainMenuSceneName);
            
            if (!_isInGameplay && disableInMainMenu && weaponManager != null)
            {
                // Clear all weapons if we're in the main menu
                weaponManager.ClearWeapons();
                Debug.Log("[InventoryWeaponBridge] Main menu detected, cleared all weapons");
            }
            
            Debug.Log($"[InventoryWeaponBridge] Scene context: {currentScene.name}, IsGameplay: {_isInGameplay}");
        }

        private void Start()
        {
            StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(initializationDelay);
            
            TryInitialize();
            
            if (_initialized)
            {
                // Only proceed if we're in a gameplay scene
                if (_isInGameplay)
                {
                    MapAvailableWeapons();
                    
                    yield return new WaitForSeconds(0.1f);
                    
                    // Keep trying to sync weapons until character is found
                    StartCoroutine(RetryCharacterFind());
                }
                else
                {
                    Debug.Log("[InventoryWeaponBridge] Not in gameplay scene, skipping weapon initialization");
                }
            }
        }

        private IEnumerator RetryCharacterFind()
        {
            if (!_isInGameplay) yield break;
            
            int attempts = 0;
            while (attempts < 10)
            {
                if (FindCharacter() != null)
                {
                    SyncWeaponsWithInventory();
                    break;
                }
                
                yield return new WaitForSeconds(0.5f);
                attempts++;
            }
        }

        private void Update()
        {
            if (!_initialized || !_isInGameplay) return;
            
            // Periodically check for character
            if (Time.time - _lastCharacterFindTime > characterFindInterval)
            {
                var character = FindCharacter();
                if (character != null)
                {
                    if (_cachedCharacter == null)
                    {
                        // Character was found for the first time
                        Debug.Log("[InventoryWeaponBridge] Character found, forcing weapon sync");
                        SyncWeaponsWithInventory();
                    }
                    
                    _cachedCharacter = character;
                }
                _lastCharacterFindTime = Time.time;
            }
            
            // Periodically validate weapons if we have a character
            if (_cachedCharacter != null && Time.time - _lastValidationTime > validationInterval)
            {
                ValidateWeaponsWithInventory();
                _lastValidationTime = Time.time;
            }
        }

        private Character FindCharacter()
        {
            if (inventoryManager == null)
                return null;
                
            return inventoryManager.GetCharacter();
        }

        private void ValidateWeaponsWithInventory()
        {
            if (!_initialized || !_isInGameplay || inventoryManager == null || weaponManager == null)
                return;

            Character character = _cachedCharacter;
            if (character == null)
            {
                character = FindCharacter();
                if (character == null)
                    return;
                    
                _cachedCharacter = character;
            }

            bool needsSync = false;
            WeaponData[] activeWeapons = weaponManager.GetAvailableWeapons();

            if (activeWeapons == null || activeWeapons.Length == 0)
                return;

            // Check if any active weapon is not properly equipped
            foreach (WeaponData weaponData in activeWeapons)
            {
                if (weaponData == null || string.IsNullOrEmpty(weaponData.inventoryItemId))
                    continue;

                bool foundInEquipment = false;
                foreach (EquipmentSlot slot in _equipSlotToWeaponSlot.Keys)
                {
                    if (_equipSlotToWeaponSlot[slot] == weaponData.weaponSlot)
                    {
                        ItemInstance item = character.GetEquippedItem(slot);
                        if (item != null && item.itemData is WeaponItemData weaponItem && 
                            weaponItem.id == weaponData.inventoryItemId)
                        {
                            foundInEquipment = true;
                            break;
                        }
                    }
                }

                if (!foundInEquipment)
                {
                    Debug.LogWarning($"[InventoryWeaponBridge] Weapon {weaponData.weaponName} (slot {weaponData.weaponSlot}) is not in the inventory equipment slots! Forcing sync.");
                    needsSync = true;
                    break;
                }
            }

            // If validation failed, re-sync from inventory to weapons
            if (needsSync)
            {
                SyncWeaponsWithInventory();
            }
        }

        private void TryFindRequiredComponents()
        {
            if (inventoryManager == null)
                inventoryManager = FindObjectOfType<InventoryManager>();
                
            if (weaponManager == null)
            {
                weaponManager = GetComponent<WeaponManager>();
                if (weaponManager == null)
                    weaponManager = FindObjectOfType<WeaponManager>();
            }
            
            if (itemDatabase == null)
                itemDatabase = FindObjectOfType<ItemDatabase>();
        }

        private void TryInitialize()
        {
            if (inventoryManager == null || weaponManager == null)
            {
                TryFindRequiredComponents();
                
                if (inventoryManager == null || weaponManager == null)
                {
                    StartCoroutine(RetryInitialization());
                    return;
                }
            }
            
            SubscribeToEvents();
            
            _initialized = true;
            _retryCount = 0;
            
            // Check scene context on initialization
            CheckCurrentSceneContext();
            
            // Clear weapons if in main menu
            if (!_isInGameplay && disableInMainMenu && weaponManager != null)
            {
                weaponManager.ClearWeapons();
            }
        }

        private IEnumerator RetryInitialization()
        {
            _retryCount++;
            float waitTime = 0.5f * _retryCount;
            
            Debug.Log($"Retrying initialization (attempt {_retryCount}/{maxRetryAttempts}) in {waitTime} seconds");
            
            if (_retryCount >= maxRetryAttempts)
            {
                Debug.LogError("Failed to initialize InventoryWeaponBridge after maximum retry attempts");
                yield break;
            }
            
            yield return new WaitForSeconds(waitTime);
            
            TryFindRequiredComponents();
            
            if (inventoryManager != null && weaponManager != null)
            {
                TryInitialize();
                
                if (_initialized && _isInGameplay)
                {
                    MapAvailableWeapons();
                    yield return new WaitForSeconds(0.2f);
                    SyncWeaponsWithInventory();
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (inventoryManager != null)
            {
                inventoryManager.onEquipWeapon -= OnEquipWeapon;
                inventoryManager.onUnequipWeapon -= OnUnequipWeapon;
                inventoryManager.onUpdateWeaponAmmo -= OnUpdateWeaponAmmo;
                inventoryManager.onInventoryChanged.RemoveListener(OnInventoryChanged);
                
                inventoryManager.onEquipWeapon += OnEquipWeapon;
                inventoryManager.onUnequipWeapon += OnUnequipWeapon;
                inventoryManager.onUpdateWeaponAmmo += OnUpdateWeaponAmmo;
                inventoryManager.onInventoryChanged.AddListener(OnInventoryChanged);
            }

            if (weaponManager != null)
            {
                weaponManager.onWeaponChanged.RemoveListener(OnWeaponChanged);
                
                weaponManager.onWeaponChanged.AddListener(OnWeaponChanged);
            }
            
            _subscribed = true;
        }

        private void UnsubscribeFromEvents()
        {
            if (inventoryManager != null)
            {
                inventoryManager.onEquipWeapon -= OnEquipWeapon;
                inventoryManager.onUnequipWeapon -= OnUnequipWeapon;
                inventoryManager.onUpdateWeaponAmmo -= OnUpdateWeaponAmmo;
                inventoryManager.onInventoryChanged.RemoveListener(OnInventoryChanged);
            }

            if (weaponManager != null)
            {
                weaponManager.onWeaponChanged.RemoveListener(OnWeaponChanged);
            }
            
            _subscribed = false;
        }
        
        private void OnInventoryChanged()
        {
            if (!_isInGameplay) return;
            StartCoroutine(DelayedInventorySync());
        }
        
        private IEnumerator DelayedInventorySync()
        {
            yield return new WaitForSeconds(0.1f);
            if (_isInGameplay)
            {
                SyncWeaponsWithInventory();
            }
        }

        public void MapAvailableWeapons()
        {
            if (!_isInGameplay) return;
            
            _weaponDataMap.Clear();

            if (GameManager.Instance != null)
            {
                if (itemDatabase != null)
                {
                    Debug.Log("Using ItemDatabase to map weapons");
                    var allItems = itemDatabase.GetAllItems();
                    foreach (var item in allItems)
                    {
                        if (item is WeaponItemData weaponItem)
                        {
                            WeaponData weaponData = weaponItem.ToWeaponData();
                            if (weaponData != null && !string.IsNullOrEmpty(weaponData.inventoryItemId))
                            {
                                _weaponDataMap[weaponData.inventoryItemId] = weaponData;
                                Debug.Log($"Mapped weapon: {weaponData.weaponName} (ID: {weaponData.inventoryItemId})");
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("No ItemDatabase found, using GameManager to map weapons");
                    List<ItemData> weaponItems = new List<ItemData>();
                    
                    foreach (WeaponData weaponData in GameManager.Instance.GetSavedWeapons())
                    {
                        if (!string.IsNullOrEmpty(weaponData.inventoryItemId))
                        {
                            ItemData itemData = GameManager.Instance.GetItemById(weaponData.inventoryItemId);
                            if (itemData != null && itemData is WeaponItemData)
                            {
                                weaponItems.Add(itemData);
                            }
                            
                            _weaponDataMap[weaponData.inventoryItemId] = weaponData;
                            Debug.Log($"Mapped weapon from GameManager: {weaponData.weaponName} (ID: {weaponData.inventoryItemId})");
                        }
                    }
                }
            }
            
            Debug.Log($"Total mapped weapons: {_weaponDataMap.Count}");
        }

        public void SyncWeaponsWithInventory()
        {
            // Skip syncing if we're in the main menu
            if (!_isInGameplay)
            {
                if (weaponManager != null && disableInMainMenu)
                {
                    weaponManager.ClearWeapons();
                }
                return;
            }
            
            if (!_initialized)
            {
                TryInitialize();
                if (!_initialized) 
                {
                    if (_retryCount < maxRetryAttempts) 
                    {
                        _retryCount++;
                        TryFindRequiredComponents();
                        TryInitialize();
                    }
                    
                    if (!_initialized)
                    {
                        Debug.LogWarning("Cannot sync weapons: InventoryWeaponBridge not initialized yet");
                        return;
                    }
                }
            }
            
            if (inventoryManager == null || weaponManager == null)
            {
                Debug.LogWarning("Cannot sync weapons: Missing InventoryManager or WeaponManager");
                return;
            }

            try
            {
                Character character = _cachedCharacter;
                if (character == null)
                {
                    character = FindCharacter();
                }
                
                if (character == null)
                {
                    Debug.LogWarning("Could not find character to sync weapons");
                    return;
                }
                
                _cachedCharacter = character;
                Debug.Log("[InventoryWeaponBridge] Found character, syncing weapons");

                List<WeaponData> equippedWeapons = new List<WeaponData>();
                Dictionary<int, int> slotAmmoCount = new Dictionary<int, int>();
                _weaponSlotAmmoState.Clear();

                // Get only equipped weapons
                foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                {
                    if (!_equipSlotToWeaponSlot.ContainsKey(slot))
                        continue;
                    
                    ItemInstance item = character.GetEquippedItem(slot);
                    if (item != null && item.itemData is WeaponItemData weaponItem)
                    {
                        WeaponData weaponData = GetWeaponDataForItem(weaponItem);
                        if (weaponData != null)
                        {
                            // Assign weapon slot based on equipment slot
                            int weaponSlot = _equipSlotToWeaponSlot[slot];
                            weaponData.weaponSlot = weaponSlot;
                            
                            // Update ammo
                            weaponData.currentAmmo = weaponItem.currentAmmoCount;
                            
                            equippedWeapons.Add(weaponData);
                            slotAmmoCount[weaponSlot] = weaponItem.currentAmmoCount;
                            
                            _weaponSlotAmmoState[weaponSlot] = weaponItem.currentAmmoCount;
                            
                            Debug.Log($"Synced weapon {weaponItem.displayName} from {slot} to weapon slot {weaponSlot}");
                        }
                        else
                        {
                            Debug.LogWarning($"Could not get WeaponData for item {weaponItem.displayName} (ID: {weaponItem.id})");
                        }
                    }
                }

                // Update weapon manager with ONLY equipped weapons
                Debug.Log($"Updating WeaponManager with {equippedWeapons.Count} equipped weapons");
                
                if (equippedWeapons.Count > 0)
                {
                    weaponManager.UpdateWeaponsFromInventory(equippedWeapons.ToArray(), slotAmmoCount);
                }
                else
                {
                    // No weapons equipped, clear all weapons
                    weaponManager.UpdateWeaponsFromInventory(new WeaponData[0], new Dictionary<int, int>());
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error syncing weapons with inventory: {e.Message}\n{e.StackTrace}");
            }
        }

        private WeaponData GetWeaponDataForItem(WeaponItemData itemData)
        {
            if (_weaponDataMap.TryGetValue(itemData.id, out WeaponData data))
            {
                return data;
            }

            // If not found, try remapping
            MapAvailableWeapons();
            
            if (_weaponDataMap.TryGetValue(itemData.id, out WeaponData remappedData))
            {
                return remappedData;
            }

            // Last resort - convert directly
            Debug.LogWarning($"Converting WeaponItemData to WeaponData directly for {itemData.displayName} (ID: {itemData.id})");
            return itemData.ToWeaponData();
        }

        private void OnEquipWeapon(WeaponItemData weaponItem, EquipmentSlot slot)
        {
            if (!_isInGameplay || weaponManager == null || !_equipSlotToWeaponSlot.ContainsKey(slot)) return;

            Debug.Log($"Weapon {weaponItem.displayName} equipped to slot {slot}");
            
            // Trigger a full sync to update all weapons
            SyncWeaponsWithInventory();
        }

        private void OnUnequipWeapon(EquipmentSlot slot)
        {
            if (!_isInGameplay || weaponManager == null || !_equipSlotToWeaponSlot.ContainsKey(slot)) return;

            Debug.Log($"Weapon unequipped from slot {slot}");
            
            // Trigger a full sync to update all weapons
            SyncWeaponsWithInventory();
        }

        private void OnUpdateWeaponAmmo(WeaponItemData weaponItem)
        {
            if (!_isInGameplay || weaponManager == null) return;

            Character character = _cachedCharacter;
            if (character == null)
            {
                character = FindCharacter();
                if (character == null) return;
                _cachedCharacter = character;
            }
            
            foreach (EquipmentSlot slot in _equipSlotToWeaponSlot.Keys)
            {
                ItemInstance equippedItem = character.GetEquippedItem(slot);
                if (equippedItem != null && equippedItem.itemData is WeaponItemData && equippedItem.itemData.id == weaponItem.id)
                {
                    int weaponSlot = _equipSlotToWeaponSlot[slot];
                    WeaponData[] currentWeapons = weaponManager.GetAvailableWeapons();

                    foreach (var weapon in currentWeapons)
                    {
                        if (weapon.inventoryItemId == weaponItem.id && weapon.weaponSlot == weaponSlot)
                        {
                            Weapon activeWeapon = weaponManager.GetActiveWeapon();
                            if (activeWeapon != null && activeWeapon.GetWeaponData() == weapon)
                            {
                                activeWeapon.SetAmmo(weaponItem.currentAmmoCount);
                            }
                            
                            weapon.currentAmmo = weaponItem.currentAmmoCount;
                            _weaponSlotAmmoState[weaponSlot] = weaponItem.currentAmmoCount;
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private void OnWeaponChanged(WeaponData weaponData, int currentAmmo)
        {
            if (!_isInGameplay || inventoryManager == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) return;

            Character character = _cachedCharacter;
            if (character == null)
            {
                character = FindCharacter();
                if (character == null) return;
                _cachedCharacter = character;
            }

            foreach (EquipmentSlot slot in _equipSlotToWeaponSlot.Keys)
            {
                int weaponSlot = _equipSlotToWeaponSlot[slot];
                if (weaponData.weaponSlot == weaponSlot)
                {
                    ItemInstance equippedItem = character.GetEquippedItem(slot);
                    if (equippedItem != null && equippedItem.itemData is WeaponItemData weaponItem)
                    {
                        if (weaponItem.id == weaponData.inventoryItemId)
                        {
                            weaponItem.currentAmmoCount = currentAmmo;
                            _weaponSlotAmmoState[weaponSlot] = currentAmmo;
                        }
                    }
                    break;
                }
            }
        }

        private void OnWeaponAmmoChanged(WeaponData weaponData, int newAmmo)
        {
            if (!_isInGameplay || inventoryManager == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) return;

            Character character = _cachedCharacter;
            if (character == null)
            {
                character = FindCharacter();
                if (character == null) return;
                _cachedCharacter = character;
            }

            foreach (EquipmentSlot slot in _equipSlotToWeaponSlot.Keys)
            {
                int weaponSlot = _equipSlotToWeaponSlot[slot];
                if (weaponData.weaponSlot == weaponSlot)
                {
                    ItemInstance equippedItem = character.GetEquippedItem(slot);
                    if (equippedItem != null && equippedItem.itemData is WeaponItemData weaponItem)
                    {
                        if (weaponItem.id == weaponData.inventoryItemId)
                        {
                            weaponItem.currentAmmoCount = newAmmo;
                            _weaponSlotAmmoState[weaponSlot] = newAmmo;
                            inventoryManager.UpdateWeaponAmmo(weaponItem, newAmmo);
                        }
                    }
                    break;
                }
            }
        }

        public event System.Action<WeaponData, int> onWeaponAmmoChanged;
    }
}