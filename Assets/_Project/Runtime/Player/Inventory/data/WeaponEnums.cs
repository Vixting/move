using System;
using UnityEngine;

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

[Serializable]
public class AttachmentPoint
{
    public string pointName;
    public AttachmentType allowedTypes;
    public Transform attachmentTransform;
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