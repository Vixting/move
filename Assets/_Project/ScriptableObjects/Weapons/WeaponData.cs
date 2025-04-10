using UnityEngine;
using System;
using System.Collections.Generic;
using SharedTypes;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    // Unique Identifier
    [SerializeField] private string weaponId = Guid.NewGuid().ToString();
    public string WeaponId => weaponId;
    
    // Basic Information
    public string weaponName;
    public string description;
    public Sprite weaponIcon;
    public GameObject weaponPrefab;
    
    // Categorization
    public WeaponType weaponType = WeaponType.AssaultRifle;
    public int weaponSlot = 1; // 1=Primary, 2=Secondary, 3=Holster
    
    // Inventory Properties
    public string inventoryItemId; // Used to link with inventory system
    public int width = 4;  // Size in inventory grid
    public int height = 1;
    public float weight = 3.5f;
    public bool foldable = false;
    public int foldedWidth = 2;
    public int foldedHeight = 1;
    public bool folded = false;
    public float durability = 100f;
    public float maxDurability = 100f;
    
    // Ammo Properties
    public int maxAmmo = 30;
    public int currentAmmo;
    public AmmoType compatibleAmmoType;
    
    // Weapon Performance Stats
    public float damage = 35f;  // Base damage per bullet
    public float fireRate = 0.1f; // Time between shots in seconds 
    public float fireRateRPM = 600f; // Rounds per minute (for display)
    public float reloadTime = 2f;
    public bool isAutomatic = true;
    public float accuracy = 1.5f; // In MOA (minutes of angle) - lower is better
    public float ergonomics = 50f; // 0-100 scale affecting weapon handling
    
    // Weapon Physics
    public float recoilAmount = 0.1f;
    public float recoilVertical = 100f; // 0-200 scale for UI display
    public float recoilHorizontal = 350f;
    public float horizontalRecoilVariance = 0.3f;
    public float recoilRecoverySpeed = 5f;
    public float impactForce = 20f;
    public float bulletRange = 100f;
    public float horizontalKnockbackForce = 0f;
    public float verticalKnockbackForce = 0f;
    
    // Visual Effects
    public float bulletHoleSize = 0.1f;
    public float bulletHoleLifetime = 10f;
    public Material bulletHoleMaterial;
    public int maxBulletHoles = 50;
    public ParticleSystem muzzleFlashPrefab;
    
    // Audio
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    // Transform Data
    public Vector3 hipPosition = new Vector3(0.2f, -0.15f, 0.4f);
    public Vector3 adsPosition = new Vector3(0f, -0.06f, 0.2f);
    public Vector3 hipRotation = Vector3.zero;
    public Vector3 adsRotation = Vector3.zero;
    
    // Attachment Data
    public AttachmentPoint[] attachmentPoints;
    
    // Item properties for UI display
    [SerializeField] private List<ItemProperty> _properties = new List<ItemProperty>();
    public List<ItemProperty> Properties => _properties;

    public void OnValidate()
    {
        if (string.IsNullOrEmpty(weaponId))
        {
            weaponId = Guid.NewGuid().ToString();
        }
        
        // Calculate fire rate in RPM
        if (fireRate > 0)
        {
            fireRateRPM = 60f / fireRate;
        }
        
        // Sync weapon properties for display
        UpdateProperties();
    }
    
    public void UpdateProperties()
    {
        _properties.Clear();
        
        // Add basic properties
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
    
    // Helper methods for weapon slot mapping
    public bool CanEquipInSlot(EquipmentSlot slot)
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
                return slot == EquipmentSlot.Holster;
            case WeaponType.SMG:
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