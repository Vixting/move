using UnityEngine;
using System.Collections.Generic;

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
    }
   
    public WeaponData ToWeaponData()
    {
        if (gameplayWeaponData != null)
        {
            return gameplayWeaponData;
        }
       
        WeaponData newWeaponData = ScriptableObject.CreateInstance<WeaponData>();
        newWeaponData.weaponName = displayName;
        newWeaponData.weaponPrefab = prefab;
        newWeaponData.weaponIcon = icon;
        newWeaponData.weaponSlot = (int)weaponType;
       
        return newWeaponData;
    }
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

[System.Serializable]
public class AttachmentPoint
{
    public string pointName;
    public AttachmentType allowedTypes;
    public Transform attachmentTransform;
}

[System.Flags]
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