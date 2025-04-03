// WeaponItemData.cs
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Weapon Item", menuName = "Inventory/Items/Weapon")]
    public class WeaponItemData : ItemData
    {
        public WeaponType weaponType;
        public float damage;
        public float fireRate;
        public float recoil;
        public float ergonomics;
        public float accuracy;
        public int magazineSize;
        public AmmoType compatibleAmmo;
        public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();
       
        public bool foldable = false;
        public bool folded = false;
        public int durability = 100;
        public int maxDurability = 100;
       
        public int foldedWidth = 1;
        public int foldedHeight = 1;
       
        public WeaponData gameplayWeaponData;

        [HideInInspector]
        public int currentAmmoCount;
       
        public void OnValidate()
        {
            category = ItemCategory.Weapon;
            stackable = false;
           
            properties.Clear();
           
            properties.Add(new ItemProperty {
                name = "Damage",
                value = damage.ToString(),
                unit = "",
                color = Color.red
            });
           
            properties.Add(new ItemProperty {
                name = "Rate of Fire",
                value = fireRate.ToString(),
                unit = "rpm",
                color = Color.white
            });
           
            properties.Add(new ItemProperty {
                name = "Recoil",
                value = recoil.ToString(),
                unit = "",
                color = (recoil > 150) ? Color.red : (recoil < 100 ? Color.green : Color.yellow)
            });
           
            properties.Add(new ItemProperty {
                name = "Ergonomics",
                value = ergonomics.ToString(),
                unit = "",
                color = (ergonomics > 70) ? Color.green : (ergonomics < 40 ? Color.red : Color.yellow)
            });
           
            properties.Add(new ItemProperty {
                name = "Accuracy",
                value = accuracy.ToString(),
                unit = "MOA",
                color = (accuracy < 3) ? Color.green : (accuracy > 6 ? Color.red : Color.yellow)
            });
            
            properties.Add(new ItemProperty {
                name = "Durability", 
                value = $"{durability}/{maxDurability}",
                unit = "",
                color = durability > maxDurability * 0.7f ? Color.green : 
                       durability > maxDurability * 0.3f ? Color.yellow : Color.red
            });
            
            if (gameplayWeaponData != null)
            {
                gameplayWeaponData.inventoryItemId = id;
            }
        }
       
        public WeaponData ToWeaponData()
        {
            if (gameplayWeaponData != null)
            {
                gameplayWeaponData.maxAmmo = magazineSize;
                gameplayWeaponData.recoilAmount = recoil / 1000f;
                gameplayWeaponData.impactForce = damage * 0.5f;
                gameplayWeaponData.weaponSlot = (int)weaponType;
                gameplayWeaponData.inventoryItemId = id;
                return gameplayWeaponData;
            }
           
            WeaponData newWeaponData = ScriptableObject.CreateInstance<WeaponData>();
            newWeaponData.weaponName = displayName;
            newWeaponData.weaponPrefab = prefab;
            newWeaponData.weaponIcon = icon;
            newWeaponData.weaponSlot = (int)weaponType;
            newWeaponData.inventoryItemId = id;
            
            newWeaponData.maxAmmo = magazineSize;
            newWeaponData.fireRate = 60f / fireRate;
            newWeaponData.recoilAmount = recoil / 1000f;
            
            newWeaponData.bulletRange = CalculateBulletRange();
            newWeaponData.impactForce = damage * 0.5f;
            
            newWeaponData.isAutomatic = weaponType == WeaponType.SMG || 
                                     weaponType == WeaponType.AssaultRifle;
            newWeaponData.reloadTime = 2.5f * (1f + (70f - ergonomics) / 70f);
            
            return newWeaponData;
        }
        
        private float CalculateBulletRange()
        {
            switch (weaponType)
            {
                case WeaponType.SniperRifle: return 300f;
                case WeaponType.AssaultRifle: return 200f;
                case WeaponType.SMG: return 100f;
                case WeaponType.Pistol: return 50f;
                case WeaponType.Shotgun: return 25f;
                default: return 100f;
            }
        }
        
        public void ToggleFolded()
        {
            if (!foldable) return;
            folded = !folded;
        }
    }
}