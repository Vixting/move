using System.Collections.Generic;
using SharedTypes;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Weapon Item", menuName = "Inventory/Items/Weapon")]
    public class WeaponItemData : ItemData
    {
        public WeaponType weaponType;
        public float damage;
        public float fireRate;
        public float recoilVertical = 100f;
        public float recoilHorizontal = 350f;
        public float ergonomics = 50f;
        public float accuracy = 0f;
        
        public int maxAmmoCount;
        public int currentAmmoCount;
        public AmmoType compatibleAmmo;
        public string ammoType;
        
        public bool foldable = false;
        public bool folded = false;
        public int foldedWidth = 1;
        public int foldedHeight = 1;
        
        public int durability = 100;
        public int maxDurability = 100;
        
        public bool hasAttachments = false;
        public List<AttachmentPoint> attachmentPoints = new List<AttachmentPoint>();
        
        public WeaponData gameplayWeaponData;
        public string weaponId;

        public override void OnValidate()
        {
            category = ItemCategory.Weapon;
            canStack = false;
            canEquip = true;
            
            compatibleSlots.Clear();
            switch (weaponType)
            {
                case WeaponType.Pistol:
                    compatibleSlots.Add(EquipmentSlot.Holster);
                    break;
                case WeaponType.SMG:
                case WeaponType.AssaultRifle:
                case WeaponType.Shotgun:
                case WeaponType.SniperRifle:
                    compatibleSlots.Add(EquipmentSlot.Primary);
                    compatibleSlots.Add(EquipmentSlot.Secondary);
                    break;
                case WeaponType.Melee:
                    break;
            }
            
            properties.Clear();
            
            AddOrUpdateProperty("Damage", damage.ToString(), "", Color.red);
            AddOrUpdateProperty("Rate of Fire", fireRate.ToString(), "rpm", Color.white);
            
            Color recoilColor = (recoilVertical > 150) ? Color.red : (recoilVertical < 100 ? Color.green : Color.yellow);
            AddOrUpdateProperty("Recoil", $"{recoilVertical}", "", recoilColor);
            
            Color ergoColor = (ergonomics > 70) ? Color.green : (ergonomics < 40 ? Color.red : Color.yellow);
            AddOrUpdateProperty("Ergonomics", ergonomics.ToString(), "", ergoColor);
            
            Color accuracyColor = (accuracy < 3) ? Color.green : (accuracy > 6 ? Color.red : Color.yellow);
            AddOrUpdateProperty("Accuracy", accuracy.ToString(), "MOA", accuracyColor);
            
            Color durabilityColor = durability > maxDurability * 0.7f ? Color.green : 
                                   durability > maxDurability * 0.3f ? Color.yellow : Color.red;
            AddOrUpdateProperty("Durability", $"{durability}/{maxDurability}", "", durabilityColor);
            
            UpdateLinkedWeaponData();
        }
        
        private void UpdateLinkedWeaponData()
        {
            if (gameplayWeaponData != null)
            {
                gameplayWeaponData.inventoryItemId = id;
                gameplayWeaponData.maxAmmo = maxAmmoCount;
                gameplayWeaponData.currentAmmo = currentAmmoCount;
                gameplayWeaponData.recoilAmount = recoilVertical / 1000f;
                gameplayWeaponData.impactForce = damage * 0.5f;
                gameplayWeaponData.weaponSlot = MapWeaponTypeToSlot(weaponType);
                gameplayWeaponData.compatibleAmmoType = (global::AmmoType)compatibleAmmo;
            }
        }
        
        private int MapWeaponTypeToSlot(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Pistol: return 3;
                case WeaponType.SMG: return 2;
                case WeaponType.AssaultRifle: return 1;
                case WeaponType.Shotgun: return 4;
                case WeaponType.SniperRifle: return 5;
                case WeaponType.Melee: return 6;
                default: return 1;
            }
        }
        
        public override string GetItemType()
        {
            return $"Weapon - {weaponType}";
        }
        
        public override string GetTooltip()
        {
            string tooltip = base.GetTooltip();
            tooltip += $"\nAmmo: {currentAmmoCount}/{maxAmmoCount}";
            tooltip += $"\nFire Rate: {fireRate} RPM";
            
            return tooltip;
        }
        
        public WeaponData ToWeaponData()
        {
            if (gameplayWeaponData != null)
            {
                UpdateLinkedWeaponData();
                return gameplayWeaponData;
            }
           
            WeaponData newWeaponData = ScriptableObject.CreateInstance<WeaponData>();
            newWeaponData.weaponName = displayName;
            newWeaponData.weaponPrefab = prefab;
            newWeaponData.weaponIcon = icon;
            newWeaponData.weaponSlot = MapWeaponTypeToSlot(weaponType);
            newWeaponData.inventoryItemId = id;
            
            newWeaponData.maxAmmo = maxAmmoCount;
            newWeaponData.currentAmmo = currentAmmoCount;
            newWeaponData.fireRate = 60f / fireRate;
            newWeaponData.recoilAmount = recoilVertical / 1000f;
            newWeaponData.compatibleAmmoType = (global::AmmoType)compatibleAmmo;
            
            newWeaponData.bulletRange = CalculateBulletRange();
            newWeaponData.impactForce = damage * 0.5f;
            
            newWeaponData.isAutomatic = weaponType == WeaponType.SMG || 
                                     weaponType == WeaponType.AssaultRifle;
            newWeaponData.reloadTime = 2.5f * (1f + (70f - ergonomics) / 70f);
            
            gameplayWeaponData = newWeaponData;
            
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
        
        public int GetCurrentWidth()
        {
            return folded ? foldedWidth : width;
        }
        
        public int GetCurrentHeight()
        {
            return folded ? foldedHeight : height;
        }
        
        public override ItemData Clone()
        {
            WeaponItemData clone = CreateInstance<WeaponItemData>();
            
            clone.id = id;
            clone.displayName = displayName;
            clone.description = description;
            clone.icon = icon;
            clone.prefab = prefab;
            clone.category = category;
            clone.rarity = rarity;
            clone.width = width;
            clone.height = height;
            clone.weight = weight;
            clone.canRotate = canRotate;
            clone.canStack = canStack;
            clone.maxStackSize = maxStackSize;
            clone.properties = new List<ItemProperty>(properties);
            clone.needsExamination = needsExamination;
            clone.isExamined = isExamined;
            clone.tags = new List<string>(tags);
            clone.baseValue = baseValue;
            clone.canEquip = canEquip;
            clone.canUse = canUse;
            clone.compatibleSlots = new List<EquipmentSlot>(compatibleSlots);
            
            clone.weaponType = weaponType;
            clone.damage = damage;
            clone.fireRate = fireRate;
            clone.recoilVertical = recoilVertical;
            clone.recoilHorizontal = recoilHorizontal;
            clone.ergonomics = ergonomics;
            clone.accuracy = accuracy;
            clone.maxAmmoCount = maxAmmoCount;
            clone.currentAmmoCount = currentAmmoCount;
            clone.compatibleAmmo = compatibleAmmo;
            clone.ammoType = ammoType;
            clone.foldable = foldable;
            clone.folded = folded;
            clone.foldedWidth = foldedWidth;
            clone.foldedHeight = foldedHeight;
            clone.durability = durability;
            clone.maxDurability = maxDurability;
            clone.hasAttachments = hasAttachments;
            clone.attachmentPoints = new List<AttachmentPoint>(attachmentPoints);
            clone.weaponId = weaponId;
            
            return clone;
        }
    }
}