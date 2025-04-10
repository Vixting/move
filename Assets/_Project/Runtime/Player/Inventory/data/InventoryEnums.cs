using System;

namespace InventorySystem
{
    public enum EquipmentSlot
    {
        Head,
        Eyes,
        Ears,
        FaceCover,
        BodyArmor,
        TacticalRig,
        Primary,
        Secondary,
        Holster,
        Backpack,
        Pouch,
        Armband
    }

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

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum WeaponType
    {
        Pistol = 1,
        SMG = 2,
        AssaultRifle = 3,
        Shotgun = 4,
        SniperRifle = 5,
        Melee = 6
    }

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

    public enum ExaminationState
    {
        Unknown,
        Examining,
        Examined
    }

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
}