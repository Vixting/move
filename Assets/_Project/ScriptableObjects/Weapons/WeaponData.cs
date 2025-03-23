using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Settings")]
    public string weaponName;
    public GameObject weaponPrefab;
    public Sprite weaponIcon;
    [Tooltip("Slot number for weapon selection (1-9)")]
    public int weaponSlot = 1;
   
    [Header("Performance Settings")]
    public float bulletRange = 100f;
    public float fireRate = 0.1f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;
    public bool isAutomatic = true;
    public float impactForce = 20f;
   
    [Header("Visual Settings")]
    public float bulletHoleSize = 0.1f;
    public float bulletHoleLifetime = 10f;
    public Material bulletHoleMaterial;
    public int maxBulletHoles = 50;
    public ParticleSystem muzzleFlashPrefab;
   
    [Header("Audio Settings")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
   
    [Header("Transform Settings")]
    public Transform bulletSpawnPoint;
   
    [Header("Weapon Positioning")]
    public Vector3 hipPosition = new Vector3(0.2f, -0.15f, 0.4f);
    public Vector3 adsPosition = new Vector3(0f, -0.06f, 0.2f);
   
    [Header("Weapon Rotation")]
    [Tooltip("Default rotation when held at hip (in Euler angles)")]
    public Vector3 hipRotation = Vector3.zero;
   
    [Tooltip("Default rotation when aiming down sights (in Euler angles)")]
    public Vector3 adsRotation = Vector3.zero;
   
    [Header("Recoil Settings")]
    public float recoilAmount = 0.1f;
    public float horizontalRecoilVariance = 0.3f;
    public float recoilRecoverySpeed = 5f;
}