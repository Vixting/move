using UnityEngine;
using System.Collections;
using InventorySystem;

public class EnemyWeaponIntegration : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform bulletOrigin;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private float damageMultiplier = 1.0f;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo = 30;
    [SerializeField] private float reloadTime = 2f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject bulletImpactPrefab;
    [SerializeField] private TrailRenderer bulletTrailPrefab;
    [SerializeField] private float bulletSpeed = 100f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip emptySound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioSource audioSource;
    
    private Weapon attachedWeapon;
    private EnemyAI enemyAI;
    private Camera mainCamera;
    private bool isReloading = false;
    private float accuracyMultiplier = 1.0f;
    private Coroutine reloadCoroutine;
    
    private void Awake()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
        attachedWeapon = GetComponent<Weapon>();
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.maxDistance = 30f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
        
        if (bulletOrigin == null)
        {
            bulletOrigin = transform.Find("BulletOrigin");
            if (bulletOrigin == null)
            {
                GameObject originObj = new GameObject("BulletOrigin");
                bulletOrigin = originObj.transform;
                bulletOrigin.SetParent(transform);
                bulletOrigin.localPosition = new Vector3(0, 0, 0.5f);
            }
        }
        
        if (weaponData == null && attachedWeapon != null)
        {
            weaponData = attachedWeapon.GetWeaponData();
            if (weaponData != null)
            {
                maxAmmo = weaponData.maxAmmo;
                currentAmmo = maxAmmo;
            }
        }
        
        if (weaponData != null && weaponData.muzzleFlashPrefab != null && muzzleFlash == null)
        {
            muzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, bulletOrigin.position, bulletOrigin.rotation, transform);
        }
        
        mainCamera = Camera.main;
    }
    
    public void Fire(bool isFiring, Vector3 targetPosition)
    {
        if (!isFiring || isReloading) return;
        
        if (currentAmmo <= 0)
        {
            PlayEmptySound();
            StartCoroutine(Reload());
            return;
        }
        
        currentAmmo--;
        
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        PlayShootSound();
        
        Vector3 fireDirection;
        if (targetPosition != Vector3.zero)
        {
            fireDirection = (targetPosition - bulletOrigin.position).normalized;
        }
        else if (mainCamera != null)
        {
            fireDirection = mainCamera.transform.forward;
        }
        else
        {
            fireDirection = bulletOrigin.forward;
        }
        
        fireDirection = AddInaccuracy(fireDirection);
        
        if (bulletTrailPrefab != null)
        {
            StartCoroutine(FireVisibleBullet(bulletOrigin.position, fireDirection));
        }
        else
        {
            FireRaycast(bulletOrigin.position, fireDirection);
        }
        
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }
    
    private Vector3 AddInaccuracy(Vector3 direction)
    {
        float inaccuracy = 0.05f * (1 - accuracyMultiplier);
        Vector3 spread = new Vector3(
            Random.Range(-inaccuracy, inaccuracy),
            Random.Range(-inaccuracy, inaccuracy),
            0
        );
        
        return Quaternion.Euler(spread) * direction;
    }
    
    private void FireRaycast(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        float damage = CalculateDamage();
        float range = weaponData != null ? weaponData.bulletRange : 100f;
        
        if (Physics.Raycast(origin, direction, out hit, range, targetLayers))
        {
            CreateImpactEffect(hit);
            
            Player player = hit.collider.GetComponent<Player>();
            if (player != null)
            {
                Character playerCharacter = player.GetCharacter();
                if (playerCharacter != null)
                {
                    playerCharacter.TakeDamage(damage);
                }
            }
            
            Rigidbody hitRigidbody = hit.collider.GetComponent<Rigidbody>();
            if (hitRigidbody != null && !hitRigidbody.isKinematic)
            {
                float force = weaponData != null ? weaponData.impactForce : 10f;
                hitRigidbody.AddForceAtPosition(direction * force, hit.point, ForceMode.Impulse);
            }
        }
    }
    
    private IEnumerator FireVisibleBullet(Vector3 origin, Vector3 direction)
    {
        float damage = CalculateDamage();
        float range = weaponData != null ? weaponData.bulletRange : 100f;
        
        TrailRenderer trail = Instantiate(bulletTrailPrefab, origin, Quaternion.identity);
        trail.AddPosition(origin);
        
        Vector3 targetPoint = origin + direction * range;
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, range, targetLayers))
        {
            targetPoint = hit.point;
        }
        
        float distance = Vector3.Distance(origin, targetPoint);
        float duration = distance / bulletSpeed;
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            float timeRatio = (Time.time - startTime) / duration;
            Vector3 bulletPosition = Vector3.Lerp(origin, targetPoint, timeRatio);
            trail.transform.position = bulletPosition;
            yield return null;
        }
        
        if (hit.collider != null)
        {
            CreateImpactEffect(hit);
            
            Player player = hit.collider.GetComponent<Player>();
            if (player != null)
            {
                Character playerCharacter = player.GetCharacter();
                if (playerCharacter != null)
                {
                    playerCharacter.TakeDamage(damage);
                }
            }
            
            Rigidbody hitRigidbody = hit.collider.GetComponent<Rigidbody>();
            if (hitRigidbody != null && !hitRigidbody.isKinematic)
            {
                float force = weaponData != null ? weaponData.impactForce : 10f;
                hitRigidbody.AddForceAtPosition(direction * force, hit.point, ForceMode.Impulse);
            }
        }
        
        Destroy(trail.gameObject, trail.time);
    }
    
    private void CreateImpactEffect(RaycastHit hit)
    {
        if (bulletImpactPrefab == null) return;
        
        GameObject impact = Instantiate(bulletImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        impact.transform.SetParent(hit.transform, true);
        
        Destroy(impact, 3f);
    }
    
    private float CalculateDamage()
    {
        float baseDamage = weaponData != null ? weaponData.damage : 10f;
        return baseDamage * damageMultiplier;
    }
    
    private IEnumerator Reload()
    {
        if (isReloading) yield break;
        
        isReloading = true;
        
        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
        
        yield return new WaitForSeconds(reloadTime);
        
        currentAmmo = maxAmmo;
        isReloading = false;
    }
    
    private void PlayShootSound()
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else if (weaponData != null && weaponData.shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(weaponData.shootSound);
        }
    }
    
    private void PlayEmptySound()
    {
        if (emptySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(emptySound);
        }
        else if (weaponData != null && weaponData.emptySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(weaponData.emptySound);
        }
    }
    
    public void SetAccuracyMultiplier(float multiplier)
    {
        accuracyMultiplier = Mathf.Clamp01(multiplier);
    }
    
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }
    
    public int GetMaxAmmo()
    {
        return maxAmmo;
    }
    
    public bool IsReloading()
    {
        return isReloading;
    }
    
    public void ForceReload()
    {
        if (isReloading) return;
        
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }
        
        reloadCoroutine = StartCoroutine(Reload());
    }
    
    public void SetWeaponData(WeaponData newWeaponData)
    {
        if (newWeaponData == null) return;
        
        weaponData = newWeaponData;
        maxAmmo = weaponData.maxAmmo;
        currentAmmo = maxAmmo;
    }
    
    private void OnDisable()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }
    }
}