using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [SerializeField] private string weaponId = Guid.NewGuid().ToString();
    public string weaponName;
    public GameObject weaponPrefab;
    public Sprite weaponIcon;
    public int weaponSlot = 1;
   
    public float bulletRange = 100f;
    public float fireRate = 0.1f;
    public int maxAmmo = 30;
    public int currentAmmo;
    public float reloadTime = 2f;
    public bool isAutomatic = true;
    public float impactForce = 20f;
    public AmmoType compatibleAmmoType;
   
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
   
    public Transform bulletSpawnPoint;
   
    public Vector3 hipPosition = new Vector3(0.2f, -0.15f, 0.4f);
    public Vector3 adsPosition = new Vector3(0f, -0.06f, 0.2f);
   
    public Vector3 hipRotation = Vector3.zero;
    public Vector3 adsRotation = Vector3.zero;
   
    public float recoilAmount = 0.1f;
    public float horizontalRecoilVariance = 0.3f;
    public float recoilRecoverySpeed = 5f;
   
    public AttachmentPoint[] attachmentPoints;
   
    public string inventoryItemId;
   
    public string WeaponId => weaponId;
   
    public void OnValidate()
    {
        if (string.IsNullOrEmpty(weaponId))
        {
            weaponId = Guid.NewGuid().ToString();
        }
    }
}