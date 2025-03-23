using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private LayerMask shootableLayers;
    [SerializeField] private WeaponData[] availableWeapons;
   
    private PlayerCamera playerCamera;
    private PlayerCharacter playerCharacter;
    private List<Weapon> weapons = new List<Weapon>();
    private int currentWeaponIndex = -1;
    private PlayerInputActions inputActions;
    private WeaponHolder currentWeaponHolder;
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
       
        inputActions.Gameplay.WeaponSwitch.performed += ctx => HandleWeaponSwitch(ctx.ReadValue<float>());
        inputActions.Gameplay.Fire.started += _ => HandleFire(true);
        inputActions.Gameplay.Fire.canceled += _ => HandleFire(false);
        inputActions.Gameplay.Reload.performed += _ => HandleReload();
        inputActions.Gameplay.Aim.started += _ => SetAiming(true);
        inputActions.Gameplay.Aim.canceled += _ => SetAiming(false);
       
        InitializeWeapons();
    }

    private void InitializeWeapons()
    {
        foreach (var weaponData in availableWeapons)
        {
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
                    
                    weapons.Add(weapon);
                    weaponObj.SetActive(false);
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
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnFire(started);
        }
    }

    private void HandleReload()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            weapons[currentWeaponIndex].OnReload();
        }
    }
    
    public void SetAiming(bool isAiming)
    {
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
                
                // Reset the aiming state properly when switching weapons
                bool isCurrentlyAiming = false;
                if (inputActions != null && inputActions.Gameplay.Aim.IsPressed())
                {
                    isCurrentlyAiming = true;
                }
                
                // Apply the current aiming state to the new weapon
                if (isCurrentlyAiming)
                {
                    holder.SetAiming(true);
                }
                else
                {
                    holder.SetAiming(false);
                }
            }
            
            onWeaponChanged?.Invoke(availableWeapons[currentWeaponIndex], newWeapon.CurrentAmmo);
        }
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.WeaponSwitch.performed -= ctx => HandleWeaponSwitch(ctx.ReadValue<float>());
            inputActions.Gameplay.Fire.started -= _ => HandleFire(true);
            inputActions.Gameplay.Fire.canceled -= _ => HandleFire(false);
            inputActions.Gameplay.Reload.performed -= _ => HandleReload();
            inputActions.Gameplay.Aim.started -= _ => SetAiming(true);
            inputActions.Gameplay.Aim.canceled -= _ => SetAiming(false);
        }
    }
}