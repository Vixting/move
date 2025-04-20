using UnityEngine;
using System;
using System.Collections.Generic;
using SharedTypes;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [SerializeField] private string weaponId = "";
    public string WeaponId
    {
        get
        {
            if (string.IsNullOrEmpty(weaponId))
            {
                weaponId = Guid.NewGuid().ToString();
                Debug.Log($"Generated new weapon ID for {weaponName}: {weaponId}");
            }
            return weaponId;
        }
    }
    
    public string weaponName;
    public string description;
    public Sprite weaponIcon;
    public GameObject weaponPrefab;
    
    public WeaponType weaponType = WeaponType.AssaultRifle;
    
    [HideInInspector] public int weaponSlot = 0; 
    
    public string inventoryItemId = "";
    public int width = 4;
    public int height = 1;
    public float weight = 3.5f;
    public bool foldable = false;
    public int foldedWidth = 2;
    public int foldedHeight = 1;
    public bool folded = false;
    public float durability = 100f;
    public float maxDurability = 100f;
    
    public int maxAmmo = 30;
    public int currentAmmo;
    public AmmoType compatibleAmmoType;
    
    public float damage = 35f;
    public float fireRate = 0.1f;
    public float fireRateRPM = 600f;
    public float reloadTime = 2f;
    public bool isAutomatic = true;
    public float accuracy = 1.5f;
    public float ergonomics = 50f;
    
    public float recoilAmount = 0.1f;
    public float recoilVertical = 100f;
    public float recoilHorizontal = 350f;
    public float horizontalRecoilVariance = 0.3f;
    public float recoilRecoverySpeed = 5f;
    public float impactForce = 20f;
    public float bulletRange = 100f;
    public float horizontalKnockbackForce = 0f;
    public float verticalKnockbackForce = 0f;
    
    public float bulletHoleSize = 0.1f;
    public float bulletHoleLifetime = 10f;
    public Material bulletHoleMaterial;
    public int maxBulletHoles = 50;
    public ParticleSystem muzzleFlashPrefab;
    
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    public Vector3 hipPosition = new Vector3(0.2f, -0.15f, 0.4f);
    public Vector3 adsPosition = new Vector3(0f, -0.06f, 0.2f);
    public Vector3 hipRotation = Vector3.zero;
    public Vector3 adsRotation = Vector3.zero;
    
    public AttachmentPoint[] attachmentPoints;
    
    [SerializeField] private List<ItemProperty> _properties = new List<ItemProperty>();
    public List<ItemProperty> Properties => _properties;

    public void OnValidate()
    {
        if (string.IsNullOrEmpty(weaponId))
        {
            weaponId = Guid.NewGuid().ToString();
        }
        
        if (string.IsNullOrEmpty(inventoryItemId))
        {
            inventoryItemId = Guid.NewGuid().ToString();
        }
        
        if (fireRate > 0)
        {
            fireRateRPM = 60f / fireRate;
        }
        
        UpdateProperties();
    }
    
    public void UpdateProperties()
    {
        _properties.Clear();
        
        AddOrUpdateProperty("Damage", damage.ToString(), "", Color.red);
        AddOrUpdateProperty("Rate of Fire", fireRateRPM.ToString(), "RPM", Color.white);
        
        Color recoilColor = (recoilVertical > 150) ? Color.red : (recoilVertical < 100 ? Color.green : Color.yellow);
        AddOrUpdateProperty("Recoil", $"{recoilVertical}", "", recoilColor);
        
        Color ergoColor = (ergonomics > 70) ? Color.green : (ergonomics < 40 ? Color.red : Color.yellow);
        AddOrUpdateProperty("Ergonomics", ergonomics.ToString(), "", ergoColor);
        
        Color accuracyColor = (accuracy < 3) ? Color.green : (accuracy > 6 ? Color.red : Color.yellow);
        AddOrUpdateProperty("Accuracy", accuracy.ToString(), "MOA", accuracyColor);
        
        Color durabilityColor = durability > maxDurability * 0.7f ? Color.green : 
                               durability > maxDurability * 0.3f ? Color.yellow : Color.red;
        AddOrUpdateProperty("Durability", $"{durability}/{maxDurability}", "", durabilityColor);
    }
    
    public void AddOrUpdateProperty(string name, string value, string unit = "", Color? color = null)
    {
        for (int i = 0; i < _properties.Count; i++)
        {
            if (_properties[i].name == name)
            {
                _properties[i].value = value;
                _properties[i].unit = unit;
                if (color.HasValue) _properties[i].color = color.Value;
                return;
            }
        }
        
        _properties.Add(new ItemProperty(name, value, unit, color ?? Color.white));
    }
    
    public bool CanEquipInSlot(EquipmentSlot slot)
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
                return slot == EquipmentSlot.Holster || slot == EquipmentSlot.Secondary;
            case WeaponType.SMG:
                return slot == EquipmentSlot.Primary || slot == EquipmentSlot.Secondary;
            case WeaponType.AssaultRifle:
            case WeaponType.Shotgun:
            case WeaponType.SniperRifle:
                return slot == EquipmentSlot.Primary || slot == EquipmentSlot.Secondary;
            case WeaponType.Melee:
                return slot == EquipmentSlot.Melee;
            default:
                return false;
        }
    }
    
    public int GetCurrentWidth()
    {
        return folded ? foldedWidth : width;
    }
    
    public int GetCurrentHeight()
    {
        return folded ? foldedHeight : height;
    }
    
    public void ToggleFolded()
    {
        if (!foldable) return;
        folded = !folded;
    }
}