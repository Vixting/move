using System;
using UnityEngine;

// This file contains shared enum definitions to be used across both weapon and inventory systems
namespace SharedTypes
{
    // Shared weapon type enum
    public enum WeaponType
    {
        Pistol = 1,
        SMG = 2,
        AssaultRifle = 3,
        Shotgun = 4,
        SniperRifle = 5,
        Melee = 6
    }
    
    // Shared ammo type enum
    public enum AmmoType
    {
        Pistol_9x19,
        Pistol_45ACP,
        Rifle_556x45,
        Rifle_762x39,
        Rifle_762x51,
        Rifle_762x54R,
        Shotgun_12Gauge,
        Shotgun_20Gauge
    }
    
    // Equipment slot enum
    public enum EquipmentSlot
    {
        None,
        Head,
        Eyes,
        Ears,
        FaceCover,
        BodyArmor,
        TacticalRig,
        Primary,
        Secondary,
        Holster,
        Melee,
        Backpack,
        Pouch,
        Armband
    }
    
    // Item category enum
    public enum ItemCategory
    {
        Weapon,
        Ammunition,
        Armor,
        Helmet,
        Medicine,
        Food,
        Drink,
        Key,
        Container,
        Mod,
        Valuable,
        Barter,
        Quest,
        Special,
        Misc
    }
    
    // Item rarity enum
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    [Flags]
    public enum AttachmentType
    {
        None = 0,
        Optic = 1,
        Barrel = 2,
        Muzzle = 4,
        Magazine = 8,
        Grip = 16,
        Stock = 32,
        Handguard = 64,
        Foregrip = 128,
        Tactical = 256,
        Mount = 512
    }
    
    [Serializable]
    public class AttachmentPoint
    {
        public string pointName;
        public AttachmentType allowedTypes;
        public Transform attachmentTransform;
    }
    
    [Serializable]
    public class ItemProperty
    {
        public string name;
        public string value;
        public string unit;
        public Color color = Color.white;
        
        public ItemProperty() { }
        
        public ItemProperty(string name, string value, string unit = "", Color color = default)
        {
            this.name = name;
            this.value = value;
            this.unit = unit;
            this.color = color == default ? Color.white : color;
        }
    }
    
    // Armor-specific enums
    public enum ArmorClass
    {
        Class1 = 1,
        Class2 = 2,
        Class3 = 3,
        Class4 = 4,
        Class5 = 5,
        Class6 = 6
    }
    
    public enum ArmorMaterial
    {
        Cloth,
        Kevlar,
        Ceramic,
        Steel,
        Titanium,
        UHMWPE,
        Combined
    }
    
    // Helper class to translate between weapon types and compatible ammo types
    public static class WeaponAmmoHelper
    {
        public static AmmoType GetDefaultAmmoTypeForWeapon(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.AssaultRifle: return AmmoType.Rifle_556x45;
                case WeaponType.SMG: return AmmoType.Pistol_9x19;
                case WeaponType.Pistol: return AmmoType.Pistol_9x19;
                case WeaponType.SniperRifle: return AmmoType.Rifle_762x51;
                case WeaponType.Shotgun: return AmmoType.Shotgun_12Gauge;
                default: return AmmoType.Pistol_9x19;
            }
        }
       
        public static string GetAmmoTypeDisplayName(AmmoType ammoType)
        {
            switch (ammoType)
            {
                case AmmoType.Pistol_9x19: return "9x19mm";
                case AmmoType.Pistol_45ACP: return ".45 ACP";
                case AmmoType.Rifle_556x45: return "5.56x45mm";
                case AmmoType.Rifle_762x39: return "7.62x39mm";
                case AmmoType.Rifle_762x51: return "7.62x51mm";
                case AmmoType.Rifle_762x54R: return "7.62x54R";
                case AmmoType.Shotgun_12Gauge: return "12 Gauge";
                case AmmoType.Shotgun_20Gauge: return "20 Gauge";
                default: return "Unknown";
            }
        }
       
        public static Color GetAmmoColor(AmmoType ammoType)
        {
            switch (ammoType)
            {
                case AmmoType.Rifle_556x45: return new Color(0.8f, 0.4f, 0.0f); // Orange
                case AmmoType.Rifle_762x39:
                case AmmoType.Rifle_762x51:
                case AmmoType.Rifle_762x54R: return new Color(0.8f, 0.0f, 0.0f); // Red
                case AmmoType.Pistol_9x19:
                case AmmoType.Pistol_45ACP: return new Color(0.9f, 0.9f, 0.0f); // Yellow
                case AmmoType.Shotgun_12Gauge:
                case AmmoType.Shotgun_20Gauge: return new Color(0.5f, 0.0f, 0.5f); // Purple
                default: return Color.gray;
            }
        }
    }
}