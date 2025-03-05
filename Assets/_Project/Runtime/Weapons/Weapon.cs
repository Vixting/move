using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Pool;
using System.Collections;

public class Weapon : MonoBehaviour
{
    private WeaponData weaponData;
    private PlayerCamera playerCamera;
    private LayerMask shootableLayers;
    private RaycastHit rayHit;
    private bool isShooting;
    private bool isReloading;
    private float nextTimeToFire;
    private int currentAmmo;
    private ObjectPool<GameObject> bulletHolePool;
    private static readonly Vector3 BulletHoleOffset = new Vector3(0, 0, 0.001f);
    private Mesh bulletHoleMesh;
    private AudioSource audioSource;
    private ParticleSystem muzzleFlash;
    private Transform bulletOrigin;
    private WeaponHolder weaponHolder;
    
    // Rotation settings
    [Header("Rotation Settings")]
    [SerializeField] private bool enableCustomRotation = true;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float rotationSmoothing = 10f;
    
    // Rotation state
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Quaternion initialLocalRotation;
    
    public int CurrentAmmo => currentAmmo;
    public UnityEvent<int> onAmmoChanged = new UnityEvent<int>();
    public UnityEvent<bool> onReloadStateChanged = new UnityEvent<bool>();

    public void Initialize(WeaponData data, PlayerCamera camera, LayerMask layers, PlayerCharacter character = null)
    {
        weaponData = data;
        playerCamera = camera;
        shootableLayers = layers;
        
        // Store initial rotation
        initialLocalRotation = transform.localRotation;
        
        // Apply initial weapon-specific rotation if available in weapon data
        if (data.hipRotation != Vector3.zero)
        {
            targetRotation = data.hipRotation;
            currentRotation = data.hipRotation;
            transform.localRotation = initialLocalRotation * Quaternion.Euler(data.hipRotation);
        }
        
        weaponHolder = GetComponentInParent<WeaponHolder>();
        
        bulletOrigin = transform.Find("BulletOrigin");
        if (bulletOrigin == null)
        {
            GameObject originObj = new GameObject("BulletOrigin");
            bulletOrigin = originObj.transform;
            bulletOrigin.SetParent(transform);
            bulletOrigin.localPosition = new Vector3(0, 0, 0.5f);
            bulletOrigin.localRotation = Quaternion.identity;
        }
        else
        {
            bulletOrigin.localPosition = new Vector3(0, 0, 0.5f);
            bulletOrigin.localRotation = Quaternion.identity;
        }

        audioSource = gameObject.AddComponent<AudioSource>();

        Transform muzzlePoint = transform.Find("MuzzleFlash");
        if (muzzlePoint == null)
        {
            muzzlePoint = bulletOrigin;
        }
        else
        {
            muzzlePoint.localPosition = new Vector3(0, 0, 0.5f);
            muzzlePoint.localRotation = Quaternion.identity;
        }

        if (weaponData.muzzleFlashPrefab != null)
        {
            muzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, transform);
        }

        InitializeBulletHoleMesh();
        InitializeObjectPool();

        currentAmmo = weaponData.maxAmmo;
        onAmmoChanged?.Invoke(currentAmmo);
    }
    
    private void Update()
    {
        // Handle automatic firing
        if (isShooting && weaponData != null && weaponData.isAutomatic && !isReloading && Time.time >= nextTimeToFire)
        {
            PerformShot();
        }
        
        // Update weapon rotation
        if (enableCustomRotation && currentRotation != targetRotation)
        {
            // Smoothly interpolate current rotation towards target
            currentRotation = Vector3.Lerp(
                currentRotation,
                targetRotation,
                Time.deltaTime * rotationSmoothing
            );
            
            // Apply the rotation
            transform.localRotation = initialLocalRotation * Quaternion.Euler(currentRotation);
        }
    }
    
    public void OnAim(bool isAiming)
    {
        // Change rotation based on aim state
        if (enableCustomRotation)
        {
            targetRotation = isAiming ? weaponData.adsRotation : weaponData.hipRotation;
        }
    }

    public void OnFire(bool started)
    {
        isShooting = started;
        if (started && weaponData != null && !weaponData.isAutomatic)
        {
            PerformShot();
        }
        if (!started)
        {
            nextTimeToFire = 0f;
        }
    }

    public void OnReload()
    {
        TryReload();
    }

    private void PerformShot()
    {
        if (currentAmmo <= 0 || isReloading || playerCamera == null)
        {
            PlaySound(weaponData.emptySound);
            TryReload();
            return;
        }

        nextTimeToFire = Time.time + weaponData.fireRate;
        currentAmmo--;
        onAmmoChanged?.Invoke(currentAmmo);

        PlaySound(weaponData.shootSound);
        
        // Apply recoil through the weapon holder if available
        if (weaponHolder != null)
        {
            // Use the recoil amount from weapon data if available
            float recoilAmount = weaponData.recoilAmount > 0 ? weaponData.recoilAmount : 0.1f;
            weaponHolder.AddRecoil(recoilAmount);
        }
        
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        Vector3 rayOrigin = bulletOrigin.position;
        
        if (Physics.Raycast(rayOrigin, playerCamera.transform.forward, out rayHit, weaponData.bulletRange, shootableLayers))
        {
            HandleHit();
            CreateBulletHole();
        }
    }

    private void HandleHit()
    {
        Rigidbody hitRigidbody = rayHit.collider.GetComponent<Rigidbody>();
        if (hitRigidbody != null && !hitRigidbody.isKinematic)
        {
            hitRigidbody.AddForceAtPosition(playerCamera.transform.forward * weaponData.impactForce, rayHit.point, ForceMode.Impulse);
        }
    }

    private void CreateBulletHole()
    {
        if (weaponData.bulletHoleMaterial == null) return;

        Vector3 adjustedPosition = rayHit.point + rayHit.normal * 0.001f;
        Quaternion rotation = Quaternion.LookRotation(rayHit.normal);
        
        RaycastHit[] hits = Physics.RaycastAll(adjustedPosition - rayHit.normal * 0.1f, rayHit.normal, 0.2f);
        foreach (var hit in hits)
        {
            if (hit.transform.GetComponent<BulletHole>() != null)
            {
                Vector3 randomOffset = Random.insideUnitCircle * weaponData.bulletHoleSize * 0.3f;
                adjustedPosition += Vector3.ProjectOnPlane(randomOffset, rayHit.normal);
                break;
            }
        }

        GameObject bulletHole = bulletHolePool.Get();
        bulletHole.transform.position = adjustedPosition;
        bulletHole.transform.rotation = rotation;
        
        float randomScale = Random.Range(0.8f, 1.2f) * weaponData.bulletHoleSize;
        bulletHole.transform.localScale = new Vector3(randomScale, randomScale, 1f);

        bulletHole.GetComponent<BulletHole>()?.Initialize(weaponData.bulletHoleMaterial, weaponData.bulletHoleLifetime);
    }

    private void InitializeBulletHoleMesh()
    {
        bulletHoleMesh = new Mesh();
        float size = 0.5f;
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-size, -size, 0),
            new Vector3(size, -size, 0),
            new Vector3(size, size, 0),
            new Vector3(-size, size, 0)
        };

        int[] triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 2, 1,
            0, 3, 2
        };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        bulletHoleMesh.vertices = vertices;
        bulletHoleMesh.triangles = triangles;
        bulletHoleMesh.uv = uvs;
        bulletHoleMesh.RecalculateNormals();
    }

    private void InitializeObjectPool()
    {
        bulletHolePool = new ObjectPool<GameObject>(
            createFunc: CreateBulletHoleObject,
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            defaultCapacity: 20,
            maxSize: weaponData.maxBulletHoles
        );
    }

    private GameObject CreateBulletHoleObject()
    {
        GameObject bulletHole = new GameObject("BulletHole");
        bulletHole.layer = 8;
        
        MeshFilter meshFilter = bulletHole.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = bulletHoleMesh;
        
        MeshRenderer meshRenderer = bulletHole.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        
        bulletHole.AddComponent<BulletHole>();
        return bulletHole;
    }

    private void TryReload()
    {
        if (!isReloading && currentAmmo < weaponData.maxAmmo)
        {
            isReloading = true;
            onReloadStateChanged?.Invoke(true);
            PlaySound(weaponData.reloadSound);
            StartCoroutine(ReloadCoroutine());
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(weaponData.reloadTime);
        currentAmmo = weaponData.maxAmmo;
        isReloading = false;
        onAmmoChanged?.Invoke(currentAmmo);
        onReloadStateChanged?.Invoke(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnDestroy()
    {
        if (bulletHoleMesh != null)
        {
            Destroy(bulletHoleMesh);
        }
    }
    
    // -- Weapon Rotation Methods --
    
    // Rotate the weapon incrementally
    public void RotateWeapon(Vector3 rotationDelta)
    {
        if (!enableCustomRotation) return;
        
        // Apply rotation based on speed
        Vector3 scaledDelta = rotationDelta * rotationSpeed * Time.deltaTime;
        targetRotation += scaledDelta;
    }
    
    // Set an absolute rotation for the weapon
    public void SetWeaponRotation(Vector3 rotation, bool instant = false)
    {
        if (!enableCustomRotation) return;
        
        targetRotation = rotation;
        
        if (instant)
        {
            currentRotation = rotation;
            transform.localRotation = initialLocalRotation * Quaternion.Euler(rotation);
        }
    }
    
    // Reset weapon rotation to default (based on current aim state)
    public void ResetWeaponRotation(bool instant = false)
    {
        // Default to hip rotation - for aim state you'll need to track that in this class
        // or use the OnAim method
        SetWeaponRotation(weaponData.hipRotation, instant);
    }
    
    // Enable or disable custom rotation
    public void SetCustomRotationEnabled(bool enabled)
    {
        enableCustomRotation = enabled;
    }
}