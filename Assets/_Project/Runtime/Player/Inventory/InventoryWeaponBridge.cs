using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace InventorySystem
{
    public class InventoryWeaponBridge : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private WeaponManager weaponManager;

        private Dictionary<string, WeaponData> _weaponDataMap = new Dictionary<string, WeaponData>();
        private Dictionary<int, int> _slotAmmoState = new Dictionary<int, int>();
        private bool _initialized = false;
        private bool _subscribed = false;

        private void Awake()
        {
            TryFindRequiredComponents();
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
        }

        private void Start()
        {
            StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForSeconds(0.2f);
            
            TryInitialize();
            
            if (_initialized)
            {
                MapAvailableWeapons();
                
                yield return new WaitForSeconds(0.1f);
                
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
        }

        private IEnumerator RetryInitialization()
        {
            float elapsedTime = 0f;
            float maxWaitTime = 5f;
            
            while (!_initialized && elapsedTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.5f);
                elapsedTime += 0.5f;
                
                TryFindRequiredComponents();
                
                if (inventoryManager != null && weaponManager != null)
                {
                    TryInitialize();
                    
                    if (_initialized)
                    {
                        MapAvailableWeapons();
                        yield return new WaitForSeconds(0.2f);
                        SyncWeaponsWithInventory();
                        break;
                    }
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
                
                inventoryManager.onEquipWeapon += OnEquipWeapon;
                inventoryManager.onUnequipWeapon += OnUnequipWeapon;
                inventoryManager.onUpdateWeaponAmmo += OnUpdateWeaponAmmo;
            }

            if (weaponManager != null)
            {
                weaponManager.onWeaponChanged.RemoveListener(OnWeaponChanged);
                
                weaponManager.onWeaponChanged.AddListener(OnWeaponChanged);
                
                if (weaponManager is IWeaponAmmoEvents ammoEvents)
                {
                    ammoEvents.onWeaponAmmoChanged -= OnWeaponAmmoChanged;
                    ammoEvents.onWeaponAmmoChanged += OnWeaponAmmoChanged;
                }
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
            }

            if (weaponManager != null)
            {
                weaponManager.onWeaponChanged.RemoveListener(OnWeaponChanged);
                
                if (weaponManager is IWeaponAmmoEvents ammoEvents)
                {
                    ammoEvents.onWeaponAmmoChanged -= OnWeaponAmmoChanged;
                }
            }
            
            _subscribed = false;
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        public void MapAvailableWeapons()
        {
            _weaponDataMap.Clear();

            if (weaponManager == null) 
            {
                return;
            }

            WeaponData[] weapons = weaponManager.GetAvailableWeapons();
            if (weapons == null) 
            {
                return;
            }
            
            foreach (var weapon in weapons)
            {
                if (weapon != null && !string.IsNullOrEmpty(weapon.inventoryItemId))
                {
                    _weaponDataMap[weapon.inventoryItemId] = weapon;
                }
            }
        }

        public void SyncWeaponsWithInventory()
        {
            if (!_initialized)
            {
                TryInitialize();
                if (!_initialized) return;
            }
            
            if (inventoryManager == null || weaponManager == null)
            {
                return;
            }

            try
            {
                InventorySystem.Character character = null;
                int retryCount = 0;
                
                while (character == null && retryCount < 3)
                {
                    character = inventoryManager.GetCharacter();
                    if (character == null)
                    {
                        retryCount++;
                        System.Threading.Thread.Sleep(100);
                    }
                }
                
                if (character == null)
                {
                    return;
                }

                List<WeaponData> equippedWeapons = new List<WeaponData>();
                Dictionary<int, int> slotAmmoCount = new Dictionary<int, int>();

                foreach (EquipmentSlot slot in new[] { EquipmentSlot.Primary, EquipmentSlot.Secondary, EquipmentSlot.Holster })
                {
                    ItemInstance item = character.GetEquippedItem(slot);
                    if (item != null && item.itemData is WeaponItemData weaponItem)
                    {
                        WeaponData weaponData = GetWeaponDataForItem(weaponItem);
                        if (weaponData != null)
                        {
                            int weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
                            weaponData.weaponSlot = weaponSlot;
                            
                            weaponData.currentAmmo = weaponItem.currentAmmoCount;
                            
                            equippedWeapons.Add(weaponData);
                            slotAmmoCount[weaponSlot] = weaponItem.currentAmmoCount;
                            
                            _slotAmmoState[weaponSlot] = weaponItem.currentAmmoCount;
                        }
                    }
                }

                if (equippedWeapons.Count > 0)
                {
                    weaponManager.UpdateWeaponsFromInventory(equippedWeapons.ToArray(), slotAmmoCount);
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

            MapAvailableWeapons();
            
            if (_weaponDataMap.TryGetValue(itemData.id, out WeaponData remappedData))
            {
                return remappedData;
            }

            return itemData.ToWeaponData();
        }

        private void OnEquipWeapon(WeaponItemData weaponItem, EquipmentSlot slot)
        {
            if (weaponManager == null) return;

            WeaponData weaponData = GetWeaponDataForItem(weaponItem);
            if (weaponData != null)
            {
                int weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
                weaponData.weaponSlot = weaponSlot;
                weaponData.currentAmmo = weaponItem.currentAmmoCount;

                WeaponData[] currentWeapons = weaponManager.GetAvailableWeapons();
                List<WeaponData> updatedWeapons = new List<WeaponData>();

                bool weaponAdded = false;
                for (int i = 0; i < currentWeapons.Length; i++)
                {
                    if (currentWeapons[i].weaponSlot == weaponSlot)
                    {
                        updatedWeapons.Add(weaponData);
                        weaponAdded = true;
                    }
                    else
                    {
                        updatedWeapons.Add(currentWeapons[i]);
                    }
                }

                if (!weaponAdded)
                {
                    updatedWeapons.Add(weaponData);
                }

                _slotAmmoState[weaponSlot] = weaponItem.currentAmmoCount;

                Dictionary<int, int> slotAmmoCount = new Dictionary<int, int>(_slotAmmoState);
                weaponManager.UpdateWeaponsFromInventory(updatedWeapons.ToArray(), slotAmmoCount);
            }
        }

        private void OnUnequipWeapon(EquipmentSlot slot)
        {
            if (weaponManager == null) return;

            int weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
            WeaponData[] currentWeapons = weaponManager.GetAvailableWeapons();
            List<WeaponData> updatedWeapons = new List<WeaponData>();

            for (int i = 0; i < currentWeapons.Length; i++)
            {
                if (currentWeapons[i].weaponSlot != weaponSlot)
                {
                    updatedWeapons.Add(currentWeapons[i]);
                }
            }

            if (_slotAmmoState.ContainsKey(weaponSlot))
            {
                _slotAmmoState.Remove(weaponSlot);
            }

            Dictionary<int, int> slotAmmoCount = new Dictionary<int, int>(_slotAmmoState);
            weaponManager.UpdateWeaponsFromInventory(updatedWeapons.ToArray(), slotAmmoCount);
        }

        private void OnUpdateWeaponAmmo(WeaponItemData weaponItem)
        {
            if (weaponManager == null) return;

            foreach (EquipmentSlot slot in new[] { EquipmentSlot.Primary, EquipmentSlot.Secondary, EquipmentSlot.Holster })
            {
                int weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
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
                        _slotAmmoState[weaponSlot] = weaponItem.currentAmmoCount;
                        break;
                    }
                }
            }
        }

        private void OnWeaponChanged(WeaponData weaponData, int currentAmmo)
        {
            if (inventoryManager == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) return;

            InventorySystem.Character character = inventoryManager.GetCharacter();
            if (character == null) return;

            EquipmentSlot slot = GetEquipmentSlotFromWeaponSlot(weaponData.weaponSlot);
            ItemInstance equippedItem = character.GetEquippedItem(slot);

            if (equippedItem != null && equippedItem.itemData is WeaponItemData weaponItem)
            {
                if (weaponItem.id == weaponData.inventoryItemId)
                {
                    weaponItem.currentAmmoCount = currentAmmo;
                    _slotAmmoState[weaponData.weaponSlot] = currentAmmo;
                }
            }
        }

        private void OnWeaponAmmoChanged(WeaponData weaponData, int newAmmo)
        {
            if (inventoryManager == null || string.IsNullOrEmpty(weaponData.inventoryItemId)) return;

            InventorySystem.Character character = inventoryManager.GetCharacter();
            if (character == null) return;

            EquipmentSlot slot = GetEquipmentSlotFromWeaponSlot(weaponData.weaponSlot);
            ItemInstance equippedItem = character.GetEquippedItem(slot);

            if (equippedItem != null && equippedItem.itemData is WeaponItemData weaponItem)
            {
                if (weaponItem.id == weaponData.inventoryItemId)
                {
                    weaponItem.currentAmmoCount = newAmmo;
                    _slotAmmoState[weaponData.weaponSlot] = newAmmo;
                }
            }
        }

        private int GetWeaponSlotFromEquipmentSlot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Primary:
                    return 1;
                case EquipmentSlot.Secondary:
                    return 2;
                case EquipmentSlot.Holster:
                    return 3;
                default:
                    return -1;
            }
        }

        private EquipmentSlot GetEquipmentSlotFromWeaponSlot(int weaponSlot)
        {
            switch (weaponSlot)
            {
                case 1:
                    return EquipmentSlot.Primary;
                case 2:
                    return EquipmentSlot.Secondary;
                case 3:
                    return EquipmentSlot.Holster;
                default:
                    return EquipmentSlot.Primary;
            }
        }
        
        private void Update()
        {
            if (!_initialized)
            {
                TryInitialize();
            }
        }
    }
    
    public interface IWeaponAmmoEvents
    {
        event System.Action<WeaponData, int> onWeaponAmmoChanged;
    }
}