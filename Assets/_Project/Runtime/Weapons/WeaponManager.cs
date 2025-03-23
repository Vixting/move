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

    public void Initialize(PlayerCamera camera, PlayerInputActions sharedInputActions, PlayerCharacter character = null)
    {
        playerCamera = camera;
        inputActions = sharedInputActions;
        playerCharacter = character;
        
        if (playerCharacter == null)
        {
            Debug.LogError("WeaponManager initialized with null PlayerCharacter! Weapon sway will not work properly.");
        }
        else
        {
            Debug.Log($"WeaponManager initialized with character: {playerCharacter.name}");
        }
       
        // Keep scroll wheel as one option for weapon switching
        inputActions.Gameplay.WeaponSwitch.performed += ctx => HandleWeaponSwitch(ctx.ReadValue<float>());
        
        // Add number key listeners for direct weapon selection (Half-Life style)
        inputActions.Gameplay.Weapon1.performed += _ => SelectWeaponBySlot(1);
        inputActions.Gameplay.Weapon2.performed += _ => SelectWeaponBySlot(2);
        inputActions.Gameplay.Weapon3.performed += _ => SelectWeaponBySlot(3);
        inputActions.Gameplay.Weapon4.performed += _ => SelectWeaponBySlot(4);
        inputActions.Gameplay.Weapon5.performed += _ => SelectWeaponBySlot(5);
        inputActions.Gameplay.Weapon6.performed += _ => SelectWeaponBySlot(6);
        inputActions.Gameplay.Weapon7.performed += _ => SelectWeaponBySlot(7);
        inputActions.Gameplay.Weapon8.performed += _ => SelectWeaponBySlot(8);
        inputActions.Gameplay.Weapon9.performed += _ => SelectWeaponBySlot(9);
        
        // Other input bindings
        inputActions.Gameplay.Fire.started += _ => HandleFire(true);
        inputActions.Gameplay.Fire.canceled += _ => HandleFire(false);
        inputActions.Gameplay.Reload.performed += _ => HandleReload();
        inputActions.Gameplay.Aim.started += _ => SetAiming(true);
        inputActions.Gameplay.Aim.canceled += _ => SetAiming(false);
        
        // Add quick switch to last weapon (Q key)
        inputActions.Gameplay.LastWeapon.performed += _ => SwitchToLastWeapon();
       
        InitializeWeapons();
    }

    // Track last used weapon for quick switching
    private int lastWeaponIndex = -1;

    // Switch to previous weapon (Q key functionality)
    private void SwitchToLastWeapon()
    {
        if (!_isEnabled) return;
        if (lastWeaponIndex >= 0 && lastWeaponIndex < weapons.Count && lastWeaponIndex != currentWeaponIndex)
        {
            SwitchWeapon(lastWeaponIndex);
        }
    }

    // Select a weapon by its slot number (1-9)
    private void SelectWeaponBySlot(int slotNumber)
    {
        if (!_isEnabled) return;
        
        // Debug statement to help troubleshoot
        Debug.Log($"Trying to select weapon with slot {slotNumber}. Available slots: {string.Join(", ", slotToIndexMap.Keys)}");
        
        if (slotToIndexMap.TryGetValue(slotNumber, out int weaponIndex))
        {
            // Only switch if it's a different weapon
            if (weaponIndex != currentWeaponIndex)
            {
                Debug.Log($"Switching to weapon at index {weaponIndex} with slot {slotNumber}");
                SwitchWeapon(weaponIndex);
            }
        }
        else
        {
            Debug.Log($"No weapon found with slot {slotNumber}");
        }
    }

    // Direct weapon selection with array indices (legacy method, kept for compatibility)
    private void SelectWeapon(int weaponIndex)
    {
        if (!_isEnabled) return;
        if (weaponIndex >= 0 && weaponIndex < weapons.Count)
        {
            // Only switch if it's a different weapon
            if (weaponIndex != currentWeaponIndex)
            {
                SwitchWeapon(weaponIndex);
            }
        }
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        
        // Force stop firing if disabled
        if (!enabled && currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnFire(false);
        }
    }

    private void InitializeWeapons()
    {
        // Clear any existing maps and weapons
        slotToIndexMap.Clear();
        weapons.Clear();
        
        for (int i = 0; i < availableWeapons.Length; i++)
        {
            var weaponData = availableWeapons[i];
            if (weaponData != null && weaponData.weaponPrefab != null)
            {
                GameObject weaponObj = Instantiate(weaponData.weaponPrefab, weaponHolder);
                
                weaponObj.transform.localPosition = Vector3.zero;
                weaponObj.transform.localRotation = Quaternion.identity;
                weaponObj.transform.localScale = Vector3.one;
                
                FixChildModels(weaponObj.transform);
                
                Weapon weapon = weaponObj.GetComponent<Weapon>();
                if (weapon != null)
                {
                    Debug.Log($"Initializing weapon: {weaponData.weaponName} with character: {(playerCharacter != null ? playerCharacter.name : "NULL")}");
                    weapon.Initialize(weaponData, playerCamera, shootableLayers, playerCharacter);
                    
                    WeaponHolder holder = weaponObj.GetComponentInParent<WeaponHolder>();
                    if (holder != null)
                    {
                        holder.Initialize(playerCamera.transform, playerCharacter);
                    }
                    
                    // Add the weapon to our list
                    int weaponIndex = weapons.Count;
                    weapons.Add(weapon);
                    weaponObj.SetActive(false);
                    
                    // Map the weapon slot to its index
                    int slotNumber = weaponData.weaponSlot > 0 ? weaponData.weaponSlot : (i + 1);
                    Debug.Log($"Mapping weapon {weaponData.weaponName} to slot {slotNumber} at index {weaponIndex}");
                    slotToIndexMap[slotNumber] = weaponIndex;
                }
                else
                {
                    Debug.LogError($"Weapon component not found on prefab: {weaponData.weaponName}");
                }
            }
            else
            {
                Debug.LogError("Null weapon data or weapon prefab reference found");
            }
        }
        
        // Log all mapped slots for debugging
        Debug.Log($"Weapon slot mappings: {string.Join(", ", slotToIndexMap.Select(kv => $"Slot {kv.Key} -> Index {kv.Value}"))}");
        
        if (weapons.Count > 0)
        {
            SwitchWeapon(0);
        }
        else
        {
            Debug.LogError("No weapons were initialized successfully");
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
        if (currentWeaponIndex == newIndex) return;
        
        // Store the previous weapon index before switching
        if (currentWeaponIndex >= 0)
        {
            lastWeaponIndex = currentWeaponIndex;
        }
        
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }
        
        currentWeaponIndex = newIndex;
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            Weapon newWeapon = weapons[currentWeaponIndex];
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
            
            // Find the corresponding weapon data
            WeaponData weaponData = null;
            for (int i = 0; i < availableWeapons.Length; i++)
            {
                if (weapons[currentWeaponIndex].gameObject.name.Contains(availableWeapons[i].weaponPrefab.name))
                {
                    weaponData = availableWeapons[i];
                    break;
                }
            }
            
            if (weaponData != null)
            {
                onWeaponChanged?.Invoke(weaponData, newWeapon.CurrentAmmo);
            }
            else
            {
                Debug.LogError($"Could not find WeaponData for weapon at index {currentWeaponIndex}");
            }
        }
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            // Unsubscribe from scroll wheel weapon switching
            inputActions.Gameplay.WeaponSwitch.performed -= ctx => HandleWeaponSwitch(ctx.ReadValue<float>());
            
            // Unsubscribe from number key weapon selection
            inputActions.Gameplay.Weapon1.performed -= _ => SelectWeaponBySlot(1);
            inputActions.Gameplay.Weapon2.performed -= _ => SelectWeaponBySlot(2);
            inputActions.Gameplay.Weapon3.performed -= _ => SelectWeaponBySlot(3);
            inputActions.Gameplay.Weapon4.performed -= _ => SelectWeaponBySlot(4);
            inputActions.Gameplay.Weapon5.performed -= _ => SelectWeaponBySlot(5);
            inputActions.Gameplay.Weapon6.performed -= _ => SelectWeaponBySlot(6);
            inputActions.Gameplay.Weapon7.performed -= _ => SelectWeaponBySlot(7);
            inputActions.Gameplay.Weapon8.performed -= _ => SelectWeaponBySlot(8);
            inputActions.Gameplay.Weapon9.performed -= _ => SelectWeaponBySlot(9);
            
            // Unsubscribe from quick switch
            inputActions.Gameplay.LastWeapon.performed -= _ => SwitchToLastWeapon();
            
            // Unsubscribe from other actions
            inputActions.Gameplay.Fire.started -= _ => HandleFire(true);
            inputActions.Gameplay.Fire.canceled -= _ => HandleFire(false);
            inputActions.Gameplay.Reload.performed -= _ => HandleReload();
            inputActions.Gameplay.Aim.started -= _ => SetAiming(true);
            inputActions.Gameplay.Aim.canceled -= _ => SetAiming(false);
        }
    }
    
    // Return the total number of weapons
    public int GetWeaponCount()
    {
        return weapons.Count;
    }
    
    // Get the current weapon index
    public int GetCurrentWeaponIndex()
    {
        return currentWeaponIndex;
    }
    
    // Get current weapon slot
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