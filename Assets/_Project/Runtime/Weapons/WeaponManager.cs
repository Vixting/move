using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using SharedTypes;
using InventorySystem;

public class WeaponManager : MonoBehaviour, IWeaponAmmoEvents
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
    private Dictionary<int, bool> weaponMalfunctions = new Dictionary<int, bool>();
    public UnityEvent<WeaponData, int> onWeaponChanged = new UnityEvent<WeaponData, int>();
    public event System.Action<WeaponData, int> onWeaponAmmoChanged;
    private int lastWeaponIndex = -1;
    private bool _isInitialized = false;
    
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> weaponSwitchAction;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> weapon1Action;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> weapon2Action;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> weapon3Action;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> fireStartAction;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> fireEndAction;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> reloadAction;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> aimStartAction;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> aimEndAction;
    private System.Action<UnityEngine.InputSystem.InputAction.CallbackContext> lastWeaponAction;

    public WeaponData[] GetAvailableWeapons()
    {
        return availableWeapons;
    }

    public void Initialize(PlayerCamera camera, PlayerInputActions sharedInputActions, PlayerCharacter character = null, WeaponData[] existingWeapons = null)
    {
        if (_isInitialized) 
            return;
        
        _isInitialized = true;
        
        playerCamera = camera;
        inputActions = sharedInputActions;
        playerCharacter = character;
        
        if (existingWeapons != null && existingWeapons.Length > 0)
            availableWeapons = existingWeapons;
        
        if (inputActions == null)
        {
            Debug.LogError("[WM] inputActions is null in Initialize!");
            return;
        }
        
        weaponSwitchAction = ctx => HandleWeaponSwitch(ctx.ReadValue<float>());
        weapon1Action = _ => SelectWeaponBySlot(1);
        weapon2Action = _ => SelectWeaponBySlot(2);
        weapon3Action = _ => SelectWeaponBySlot(3);
        fireStartAction = _ => HandleFire(true);
        fireEndAction = _ => HandleFire(false);
        reloadAction = _ => HandleReload();
        aimStartAction = _ => SetAiming(true);
        aimEndAction = _ => SetAiming(false);
        lastWeaponAction = _ => SwitchToLastWeapon();
        
        inputActions.Gameplay.WeaponSwitch.performed += weaponSwitchAction;
        inputActions.Gameplay.Weapon1.performed += weapon1Action;
        inputActions.Gameplay.Weapon2.performed += weapon2Action;
        inputActions.Gameplay.Weapon3.performed += weapon3Action;
        inputActions.Gameplay.Fire.started += fireStartAction;
        inputActions.Gameplay.Fire.canceled += fireEndAction;
        inputActions.Gameplay.Reload.performed += reloadAction;
        inputActions.Gameplay.Aim.started += aimStartAction;
        inputActions.Gameplay.Aim.canceled += aimEndAction;
        inputActions.Gameplay.LastWeapon.performed += lastWeaponAction;
        
        InitializeWeapons();
    }

    private void SwitchToLastWeapon()
    {
        if (!_isEnabled) return;
        if (lastWeaponIndex >= 0 && lastWeaponIndex < weapons.Count && lastWeaponIndex != currentWeaponIndex)
        {
            SwitchWeapon(lastWeaponIndex);
        }
    }

    public void SelectWeaponBySlot(int slotNumber)
    {
        if (!_isEnabled) 
            return;
        
        if (slotToIndexMap.TryGetValue(slotNumber, out int weaponIndex))
        {
            if (weaponIndex != currentWeaponIndex)
            {
                SwitchWeapon(weaponIndex);
            }
        }
    }

    private void SelectWeapon(int weaponIndex)
    {
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
        
        if (!enabled && currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnFire(false);
        }
    }

    private void InitializeWeapons()
    {
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
                GameObject weaponObj = Instantiate(weaponData.weaponPrefab, weaponHolder);
                weaponObj.name = $"{weaponData.weaponName}_Weapon";
                
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = Quaternion.identity;
                weaponObj.transform.localScale = Vector3.one;
                
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
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
            }
            
            FixChildModels(child);
        }
    }

    private void HandleWeaponSwitch(float scrollValue)
    {
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
            Weapon weapon = weapons[currentWeaponIndex];
            int oldAmmo = weapon.CurrentAmmo;
            
            weapon.OnFire(started);
            
            if (oldAmmo != weapon.CurrentAmmo)
            {
                WeaponData weaponData = FindWeaponDataForCurrentWeapon();
                if (weaponData != null)
                {
                    weaponData.currentAmmo = weapon.CurrentAmmo;
                    onWeaponAmmoChanged?.Invoke(weaponData, weapon.CurrentAmmo);
                }
            }
        }
    }

    public void HandleReload()
    {
        if (!_isEnabled) return;
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            Weapon weapon = weapons[currentWeaponIndex];
            int oldAmmo = weapon.CurrentAmmo;
            
            weapon.OnReload();
            
            if (oldAmmo != weapon.CurrentAmmo)
            {
                WeaponData weaponData = FindWeaponDataForCurrentWeapon();
                if (weaponData != null)
                {
                    weaponData.currentAmmo = weapon.CurrentAmmo;
                    onWeaponAmmoChanged?.Invoke(weaponData, weapon.CurrentAmmo);
                }
            }
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
        if (currentWeaponIndex == newIndex)
            return;
        
        if (currentWeaponIndex >= 0)
            lastWeaponIndex = currentWeaponIndex;
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].CancelReload();
            weapons[currentWeaponIndex].OnFire(false);
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }
        
        currentWeaponIndex = newIndex;
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            Weapon newWeapon = weapons[currentWeaponIndex];
            newWeapon.gameObject.SetActive(true);
            newWeapon.ResetState();
            
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
        
        foreach (var kvp in slotToIndexMap)
        {
            if (kvp.Value == currentWeaponIndex)
            {
                int slot = kvp.Key;
                foreach (var data in availableWeapons)
                {
                    if (data.weaponSlot == slot)
                        return data;
                }
            }
        }
        
        foreach (var data in availableWeapons)
        {
            if (weaponName.Contains(data.weaponName) || 
                (data.weaponPrefab != null && weaponName.Contains(data.weaponPrefab.name)))
                return data;
        }
        
        if (currentWeaponIndex < availableWeapons.Length)
            return availableWeapons[currentWeaponIndex];
        
        return null;
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.WeaponSwitch.performed -= weaponSwitchAction;
            inputActions.Gameplay.Weapon1.performed -= weapon1Action;
            inputActions.Gameplay.Weapon2.performed -= weapon2Action;
            inputActions.Gameplay.Weapon3.performed -= weapon3Action;
            inputActions.Gameplay.Fire.started -= fireStartAction;
            inputActions.Gameplay.Fire.canceled -= fireEndAction;
            inputActions.Gameplay.Reload.performed -= reloadAction;
            inputActions.Gameplay.Aim.started -= aimStartAction;
            inputActions.Gameplay.Aim.canceled -= aimEndAction;
            inputActions.Gameplay.LastWeapon.performed -= lastWeaponAction;
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
    
    public void SetWeaponMalfunction(int slotNumber, bool malfunctioning)
    {
        weaponMalfunctions[slotNumber] = malfunctioning;
        
        if (malfunctioning && GetCurrentWeaponSlot() == slotNumber)
        {
            Weapon activeWeapon = GetActiveWeapon();
            if (activeWeapon != null)
            {
                activeWeapon.SetMalfunctioning(true);
            }
        }
    }

    public bool IsWeaponMalfunctioning(int slotNumber)
    {
        return weaponMalfunctions.TryGetValue(slotNumber, out bool state) && state;
    }

    public Weapon GetActiveWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            return weapons[currentWeaponIndex];
        }
        return null;
    }

    public void UpdateWeaponsFromInventory(WeaponData[] weaponDatas, Dictionary<int, int> slotAmmoCount)
    {
        if (weaponDatas == null || weaponDatas.Length == 0) return;
        
        SetAvailableWeapons(weaponDatas);
        
        foreach (var pair in slotAmmoCount)
        {
            foreach (var weapon in weapons)
            {
                WeaponData weaponData = weapon.GetWeaponData();
                if (weaponData != null && weaponData.weaponSlot == pair.Key)
                {
                    weapon.SetAmmo(pair.Value);
                    break;
                }
            }
        }
    }
    
    public void SetAvailableWeapons(WeaponData[] newWeapons)
    {
        if (newWeapons == null || newWeapons.Length == 0) return;
        
        if (availableWeapons != null && availableWeapons.Length == newWeapons.Length)
        {
            bool allMatch = true;
            for (int i = 0; i < availableWeapons.Length; i++)
            {
                if (availableWeapons[i].inventoryItemId != newWeapons[i].inventoryItemId)
                {
                    allMatch = false;
                    break;
                }
            }
            
            if (allMatch) return;
        }
        
        int currentSlot = -1;
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            foreach (var pair in slotToIndexMap)
            {
                if (pair.Value == currentWeaponIndex)
                {
                    currentSlot = pair.Key;
                    break;
                }
            }
        }
        
        Dictionary<string, int> ammoStates = new Dictionary<string, int>();
        foreach (var weapon in weapons)
        {
            WeaponData data = weapon.GetWeaponData();
            if (data != null)
            {
                ammoStates[data.inventoryItemId] = weapon.CurrentAmmo;
            }
        }
        
        availableWeapons = newWeapons;
        ClearWeapons();
        InitializeWeapons();
        
        foreach (var weapon in weapons)
        {
            WeaponData data = weapon.GetWeaponData();
            if (data != null && ammoStates.TryGetValue(data.inventoryItemId, out int ammo))
            {
                weapon.SetAmmo(ammo);
            }
        }
        
        if (currentSlot >= 0 && slotToIndexMap.ContainsKey(currentSlot))
        {
            SwitchWeapon(slotToIndexMap[currentSlot]);
        }
        else if (weapons.Count > 0)
        {
            SwitchWeapon(0);
        }
    }
    
    public void ClearWeapons()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }
        
        foreach (var weapon in weapons)
        {
            Destroy(weapon.gameObject);
        }
        
        weapons.Clear();
        slotToIndexMap.Clear();
        weaponMalfunctions.Clear();
        currentWeaponIndex = -1;
        lastWeaponIndex = -1;
    }
    
    public Dictionary<int, int> GetAmmoStates()
    {
        Dictionary<int, int> ammoStates = new Dictionary<int, int>();
        
        for (int i = 0; i < weapons.Count; i++)
        {
            Weapon weapon = weapons[i];
            int slotNumber = -1;
            
            foreach (var kvp in slotToIndexMap)
            {
                if (kvp.Value == i)
                {
                    slotNumber = kvp.Key;
                    break;
                }
            }
            
            if (slotNumber != -1)
            {
                ammoStates[slotNumber] = weapon.CurrentAmmo;
            }
        }
        
        return ammoStates;
    }
    
    public void UpdateAmmoCount(int newAmmo)
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            Weapon currentWeapon = weapons[currentWeaponIndex];
            currentWeapon.SetAmmo(newAmmo);
            
            WeaponData weaponData = FindWeaponDataForCurrentWeapon();
            if (weaponData != null)
            {
                weaponData.currentAmmo = newAmmo;
                onWeaponAmmoChanged?.Invoke(weaponData, newAmmo);
            }
        }
    }
    
    public string GetCurrentWeaponId()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            WeaponData weaponData = FindWeaponDataForCurrentWeapon();
            if (weaponData != null)
            {
                return weaponData.inventoryItemId;
            }
        }
        
        return string.Empty;
    }
}