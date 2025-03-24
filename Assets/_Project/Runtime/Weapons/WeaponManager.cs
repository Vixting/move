using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private LayerMask shootableLayers;
    [SerializeField] private WeaponData[] availableWeapons;
   
    private PlayerCamera playerCamera;
    private PlayerCharacter playerCharacter;
    private List<Weapon> weapons = new List<Weapon>();
    private Dictionary<int, int> slotToIndexMap = new Dictionary<int, int>();
    private int currentWeaponIndex = -1;
    private PlayerInputActions inputActions;
    private WeaponHolder currentWeaponHolder;
    private bool _isEnabled = true;
    public UnityEvent<WeaponData, int> onWeaponChanged = new UnityEvent<WeaponData, int>();
    private int lastWeaponIndex = -1;

    public void Initialize(PlayerCamera camera, PlayerInputActions sharedInputActions, PlayerCharacter character = null)
    {
        Debug.Log("[WM] Initialize called");
        playerCamera = camera;
        inputActions = sharedInputActions;
        playerCharacter = character;
        
        if (inputActions == null)
        {
            Debug.LogError("[WM] inputActions is null in Initialize!");
            return;
        }

        Debug.Log("[WM] Setting up input callbacks");
        
        inputActions.Gameplay.WeaponSwitch.performed += ctx => 
        {
            Debug.Log($"[WM] Scroll wheel value: {ctx.ReadValue<float>()}");
            HandleWeaponSwitch(ctx.ReadValue<float>());
        };
        
        inputActions.Gameplay.Weapon1.performed += _ => 
        {
            Debug.Log("[WM] Weapon1 key pressed");
            SelectWeaponBySlot(1);
        };
        
        inputActions.Gameplay.Weapon2.performed += _ => 
        {
            Debug.Log("[WM] Weapon2 key pressed");
            SelectWeaponBySlot(2);
        };
        
        inputActions.Gameplay.Weapon3.performed += _ => 
        {
            Debug.Log("[WM] Weapon3 key pressed");
            SelectWeaponBySlot(3);
        };
        
        inputActions.Gameplay.Fire.started += _ => HandleFire(true);
        inputActions.Gameplay.Fire.canceled += _ => HandleFire(false);
        inputActions.Gameplay.Reload.performed += _ => HandleReload();
        inputActions.Gameplay.Aim.started += _ => SetAiming(true);
        inputActions.Gameplay.Aim.canceled += _ => SetAiming(false);
        inputActions.Gameplay.LastWeapon.performed += _ => 
        {
            Debug.Log("[WM] LastWeapon key pressed");
            SwitchToLastWeapon();
        };
       
        InitializeWeapons();
    }

    private void SwitchToLastWeapon()
    {
        Debug.Log($"[WM] SwitchToLastWeapon - lastIndex: {lastWeaponIndex}, currentIndex: {currentWeaponIndex}");
        if (!_isEnabled) return;
        if (lastWeaponIndex >= 0 && lastWeaponIndex < weapons.Count && lastWeaponIndex != currentWeaponIndex)
        {
            SwitchWeapon(lastWeaponIndex);
        }
    }

    private void SelectWeaponBySlot(int slotNumber)
    {
        Debug.Log($"[WM] SelectWeaponBySlot({slotNumber}) - Available slots: {string.Join(", ", slotToIndexMap.Keys)}");
        if (!_isEnabled) 
        {
            Debug.Log("[WM] Weapon switching disabled");
            return;
        }
        
        if (slotToIndexMap.TryGetValue(slotNumber, out int weaponIndex))
        {
            Debug.Log($"[WM] Found weapon at index {weaponIndex} for slot {slotNumber}");
            if (weaponIndex != currentWeaponIndex)
            {
                SwitchWeapon(weaponIndex);
            }
            else
            {
                Debug.Log($"[WM] Already using weapon at index {weaponIndex}");
            }
        }
        else
        {
            Debug.Log($"[WM] No weapon found with slot {slotNumber}");
            Debug.Log($"[WM] Available mappings: {string.Join(", ", slotToIndexMap.Select(kv => $"{kv.Key}->{kv.Value}"))}");
        }
    }

    private void SelectWeapon(int weaponIndex)
    {
        Debug.Log($"[WM] SelectWeapon({weaponIndex}) - Total weapons: {weapons.Count}");
        if (!_isEnabled) return;
        if (weaponIndex >= 0 && weaponIndex < weapons.Count)
        {
            if (weaponIndex != currentWeaponIndex)
            {
                SwitchWeapon(weaponIndex);
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        Debug.Log($"[WM] SetEnabled({enabled})");
        
        if (!enabled && currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnFire(false);
        }
    }

    private void InitializeWeapons()
    {
        Debug.Log("[WM] InitializeWeapons - Available weapons: " + (availableWeapons != null ? availableWeapons.Length.ToString() : "null"));
        
        slotToIndexMap.Clear();
        weapons.Clear();
        
        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            Debug.LogError("[WM] No weapons to initialize!");
            return;
        }
        
        for (int i = 0; i < availableWeapons.Length; i++)
        {
            var weaponData = availableWeapons[i];
            if (weaponData != null && weaponData.weaponPrefab != null)
            {
                Debug.Log($"[WM] Creating weapon {i}: {weaponData.weaponName}, Slot: {weaponData.weaponSlot}");
                
                GameObject weaponObj = Instantiate(weaponData.weaponPrefab, weaponHolder);
                weaponObj.name = $"{weaponData.weaponName}_Weapon";
                
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = Quaternion.identity;
                weaponObj.transform.localScale = Vector3.one;
                
                // Add this back to fix rifle orientation
                FixChildModels(weaponObj.transform);
                
                Weapon weapon = weaponObj.GetComponent<Weapon>();
                if (weapon != null)
                {
                    weapon.Initialize(weaponData, playerCamera, shootableLayers, playerCharacter);
                    
                    WeaponHolder holder = weaponObj.GetComponentInParent<WeaponHolder>();
                    if (holder != null)
                    {
                        holder.Initialize(playerCamera.transform, playerCharacter);
                    }
                    
                    int weaponIndex = weapons.Count;
                    weapons.Add(weapon);
                    weaponObj.SetActive(false);
                    
                    int slotNumber = weaponData.weaponSlot > 0 ? weaponData.weaponSlot : (i + 1);
                    Debug.Log($"[WM] Adding mapping: Slot {slotNumber} -> Index {weaponIndex}");
                    
                    if (slotToIndexMap.ContainsKey(slotNumber))
                    {
                        Debug.LogWarning($"[WM] Multiple weapons using slot {slotNumber}! Overriding previous mapping.");
                    }
                    
                    slotToIndexMap[slotNumber] = weaponIndex;
                }
                else
                {
                    Debug.LogError($"[WM] Weapon component not found on prefab: {weaponData.weaponName}");
                }
            }
            else
            {
                Debug.LogError("[WM] Null weapon data or weapon prefab reference found at index " + i);
            }
        }
        
        Debug.Log($"[WM] Initialized {weapons.Count} weapons with mappings: {string.Join(", ", slotToIndexMap.Select(kv => $"Slot {kv.Key} -> Index {kv.Value}"))}");
        
        if (weapons.Count > 0)
        {
            SwitchWeapon(0);
        }
        else
        {
            Debug.LogError("[WM] No weapons were initialized successfully");
        }
    }
    
    private void FixChildModels(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains("Rifle"))
            {
                Debug.Log($"[WM] Fixing child model orientation: {child.name}");
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
            }
            
            FixChildModels(child);
        }
    }

    private void HandleWeaponSwitch(float scrollValue)
    {
        Debug.Log($"[WM] HandleWeaponSwitch({scrollValue})");
        if (!_isEnabled) return;
        if (weapons.Count == 0) return;
        
        int newIndex;
        if (scrollValue > 0)
        {
            newIndex = (currentWeaponIndex + 1) % weapons.Count;
        }
        else
        {
            newIndex = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;
        }
        SwitchWeapon(newIndex);
    }

    private void HandleFire(bool started)
    {
        if (!_isEnabled) return;
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnFire(started);
        }
    }

    private void HandleReload()
    {
        if (!_isEnabled) return;
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnReload();
        }
    }
    
    public void SetAiming(bool isAiming)
    {
        if (!_isEnabled) return;
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnAim(isAiming);
            
            WeaponHolder holder = weapons[currentWeaponIndex].GetComponentInParent<WeaponHolder>();
            if (holder != null)
            {
                holder.SetAiming(isAiming);
                currentWeaponHolder = holder;
            }
        }
    }

    private void SwitchWeapon(int newIndex)
    {
        Debug.Log($"[WM] SwitchWeapon({newIndex}) - Current: {currentWeaponIndex}, Total: {weapons.Count}");
        
        if (currentWeaponIndex == newIndex)
        {
            Debug.Log("[WM] Already using this weapon index");
            return;
        }
        
        if (currentWeaponIndex >= 0)
        {
            lastWeaponIndex = currentWeaponIndex;
            Debug.Log($"[WM] Stored lastWeaponIndex = {lastWeaponIndex}");
        }
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            Debug.Log($"[WM] Disabling weapon at index {currentWeaponIndex}");
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }
        
        currentWeaponIndex = newIndex;
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            Weapon newWeapon = weapons[currentWeaponIndex];
            Debug.Log($"[WM] Enabling weapon: {newWeapon.gameObject.name} at index {currentWeaponIndex}");
            newWeapon.gameObject.SetActive(true);
            
            WeaponHolder holder = newWeapon.GetComponentInParent<WeaponHolder>();
            if (holder != null)
            {
                currentWeaponHolder = holder;
                holder.Initialize(playerCamera.transform, playerCharacter);
                
                bool isCurrentlyAiming = false;
                if (inputActions != null && inputActions.Gameplay.Aim.IsPressed())
                {
                    isCurrentlyAiming = true;
                }
                
                holder.SetAiming(isCurrentlyAiming);
            }
            
            WeaponData weaponData = FindWeaponDataForCurrentWeapon();
            
            if (weaponData != null)
            {
                Debug.Log($"[WM] Invoking onWeaponChanged with {weaponData.weaponName}, ammo: {newWeapon.CurrentAmmo}");
                onWeaponChanged?.Invoke(weaponData, newWeapon.CurrentAmmo);
            }
            else
            {
                Debug.LogError($"[WM] Could not find WeaponData for weapon at index {currentWeaponIndex}");
            }
        }
        else
        {
            Debug.LogError($"[WM] Invalid weapon index: {currentWeaponIndex}");
        }
    }
    
    private WeaponData FindWeaponDataForCurrentWeapon()
    {
        if (currentWeaponIndex < 0 || currentWeaponIndex >= weapons.Count) return null;
        
        Weapon currentWeapon = weapons[currentWeaponIndex];
        string weaponName = currentWeapon.gameObject.name;
        
        Debug.Log($"[WM] Finding WeaponData for {weaponName}");
        
        // First try to find using the slot mapping (more reliable)
        foreach (var kvp in slotToIndexMap)
        {
            if (kvp.Value == currentWeaponIndex)
            {
                int slot = kvp.Key;
                foreach (var data in availableWeapons)
                {
                    if (data.weaponSlot == slot)
                    {
                        Debug.Log($"[WM] Found WeaponData by slot: {data.weaponName}");
                        return data;
                    }
                }
            }
        }
        
        // Fallback to name-based matching
        foreach (var data in availableWeapons)
        {
            if (weaponName.Contains(data.weaponName) || 
                (data.weaponPrefab != null && weaponName.Contains(data.weaponPrefab.name)))
            {
                Debug.Log($"[WM] Found WeaponData by name: {data.weaponName}");
                return data;
            }
        }
        
        // Final attempt: just use the array index
        if (currentWeaponIndex < availableWeapons.Length)
        {
            Debug.Log($"[WM] Using WeaponData by index: {availableWeapons[currentWeaponIndex].weaponName}");
            return availableWeapons[currentWeaponIndex];
        }
        
        return null;
    }

    private void OnDestroy()
    {
        Debug.Log("[WM] OnDestroy - Unsubscribing from input events");
        if (inputActions != null)
        {
            inputActions.Gameplay.WeaponSwitch.performed -= ctx => HandleWeaponSwitch(ctx.ReadValue<float>());
            inputActions.Gameplay.Weapon1.performed -= _ => SelectWeaponBySlot(1);
            inputActions.Gameplay.Weapon2.performed -= _ => SelectWeaponBySlot(2);
            inputActions.Gameplay.Weapon3.performed -= _ => SelectWeaponBySlot(3);
            inputActions.Gameplay.Fire.started -= _ => HandleFire(true);
            inputActions.Gameplay.Fire.canceled -= _ => HandleFire(false);
            inputActions.Gameplay.Reload.performed -= _ => HandleReload();
            inputActions.Gameplay.Aim.started -= _ => SetAiming(true);
            inputActions.Gameplay.Aim.canceled -= _ => SetAiming(false);
            inputActions.Gameplay.LastWeapon.performed -= _ => SwitchToLastWeapon();
        }
    }
    
    public int GetWeaponCount()
    {
        return weapons.Count;
    }
    
    public int GetCurrentWeaponIndex()
    {
        return currentWeaponIndex;
    }
    
    public int GetCurrentWeaponSlot()
    {
        foreach (var kvp in slotToIndexMap)
        {
            if (kvp.Value == currentWeaponIndex)
            {
                return kvp.Key;
            }
        }
        return -1;
    }
}