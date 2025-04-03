using UnityEngine;
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
    private AudioSource tempAudioSource;
    private ParticleSystem muzzleFlash;
    private Transform bulletOrigin;
    private WeaponHolder weaponHolder;
    private Coroutine reloadCoroutine;
    private PlayerCharacter playerCharacter;
    private WeaponInventoryIntegration _inventoryIntegration;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool enableCustomRotation = true;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float rotationSmoothing = 10f;
    
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Quaternion initialLocalRotation;
    
    private bool isMalfunctioning = false;
    private float lastSyncTime = 0f;
    
    public int CurrentAmmo => currentAmmo;
    public UnityEvent<int> onAmmoChanged = new UnityEvent<int>();
    public UnityEvent<bool> onReloadStateChanged = new UnityEvent<bool>();

    public void Initialize(WeaponData data, PlayerCamera camera, LayerMask layers, PlayerCharacter character = null)
    {
        weaponData = data;
        playerCamera = camera;
        shootableLayers = layers;
        playerCharacter = character;
        
        // Get or add WeaponInventoryIntegration
        _inventoryIntegration = GetComponent<WeaponInventoryIntegration>();
        if (_inventoryIntegration == null)
        {
            _inventoryIntegration = gameObject.AddComponent<WeaponInventoryIntegration>();
        }
        
        initialLocalRotation = transform.localRotation;
        
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

        SetupAudioSource();

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

        if (weaponData.muzzleFlashPrefab != null && muzzleFlash == null)
        {
            muzzleFlash = Instantiate(weaponData.muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, transform);
        }

        InitializeBulletHoleMesh();
        InitializeObjectPool();

        currentAmmo = weaponData.maxAmmo;
        onAmmoChanged?.Invoke(currentAmmo);
        
        if (_inventoryIntegration != null)
        {
            int loadedAmmo = _inventoryIntegration.GetAmmoForWeapon(weaponData);
            if (loadedAmmo > 0)
            {
                currentAmmo = loadedAmmo;
                onAmmoChanged?.Invoke(currentAmmo);
            }
        }
    }
    
    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
            audioSource.dopplerLevel = 0f;
            audioSource.priority = 128;
            audioSource.playOnAwake = false;
        }
    }
    
    private void OnDisable()
    {
        if (_inventoryIntegration != null)
        {
            _inventoryIntegration.SyncWeaponToInventory(this);
        }
        
        if (tempAudioSource != null)
        {
            Destroy(tempAudioSource);
            tempAudioSource = null;
        }
        
        CancelReload();
    }
    
    private void Update()
    {
        if (isShooting && weaponData != null && weaponData.isAutomatic && !isReloading && !isMalfunctioning && Time.time >= nextTimeToFire)
        {
            PerformShot();
        }
        
        if (enableCustomRotation && currentRotation != targetRotation)
        {
            currentRotation = Vector3.Lerp(
                currentRotation,
                targetRotation,
                Time.deltaTime * rotationSmoothing
            );
            
            transform.localRotation = initialLocalRotation * Quaternion.Euler(currentRotation);
        }
        
        if (_inventoryIntegration != null && Time.time - lastSyncTime > 5f)
        {
            _inventoryIntegration.SyncWeaponToInventory(this);
            lastSyncTime = Time.time;
        }
    }
    
    public void ResetState()
    {
        isShooting = false;
        isReloading = false;
        nextTimeToFire = 0f;
    }
    
    public void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        
        isReloading = false;
        onReloadStateChanged?.Invoke(false);
    }
    
    public void OnAim(bool isAiming)
    {
        if (enableCustomRotation)
        {
            targetRotation = isAiming ? weaponData.adsRotation : weaponData.hipRotation;
        }
    }

    public void OnFire(bool started)
    {
        isShooting = started;
        if (started && weaponData != null)
        {
            if (isMalfunctioning)
            {
                PlaySound(weaponData.emptySound);
                return;
            }
            
            if (!weaponData.isAutomatic)
            {
                PerformShot();
            }
        }
        if (!started)
        {
            nextTimeToFire = 0f;
        }
    }

    public void OnReload()
    {
        if (isMalfunctioning)
        {
            StartCoroutine(ClearMalfunctionCoroutine());
        }
        else
        {
            TryReload();
        }
    }

    private void PerformShot()
    {
        if (isMalfunctioning)
        {
            PlaySound(weaponData.emptySound);
            return;
        }
        
        if (currentAmmo <= 0 || isReloading || playerCamera == null)
        {
            PlaySound(weaponData.emptySound);
            TryReload();
            return;
        }

        nextTimeToFire = Time.time + weaponData.fireRate;
        currentAmmo--;
        onAmmoChanged?.Invoke(currentAmmo);

        if (_inventoryIntegration != null)
        {
            _inventoryIntegration.SyncWeaponToInventory(this);
            _inventoryIntegration.DecreaseDurability(this, 0.2f);
        }
        
        PlaySound(weaponData.shootSound);
        
        if (weaponHolder != null)
        {
            float recoilAmount = weaponData.recoilAmount > 0 ? weaponData.recoilAmount : 0.1f;
            weaponHolder.AddRecoil(recoilAmount);
        }
        
        if (playerCharacter != null && (weaponData.horizontalKnockbackForce > 0 || weaponData.verticalKnockbackForce != 0))
        {
            Vector3 knockbackDirection = -playerCamera.transform.forward;
            
            Vector3 horizontalKnockback = knockbackDirection * weaponData.horizontalKnockbackForce;
            Vector3 verticalKnockback = Vector3.up * weaponData.verticalKnockbackForce;
            
            playerCharacter.ApplyDirectionalKnockback(horizontalKnockback, verticalKnockback);
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

        bulletHole.GetComponent<BulletHole>()?.Initialize(weaponData.bulletHoleMaterial, weaponData.bulletHoleLifetime, bulletHolePool);
    }

    private void InitializeBulletHoleMesh()
    {
        if (bulletHoleMesh != null) return;
        
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
        if (bulletHolePool != null) return;
        
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
            reloadCoroutine = StartCoroutine(ReloadCoroutine());
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(weaponData.reloadTime);
        
        if (isReloading)
        {
            if (_inventoryIntegration != null &&
                _inventoryIntegration.TryReloadFromInventory(this, out int ammoLoaded))
            {
                currentAmmo = ammoLoaded;
            }
            else
            {
                currentAmmo = weaponData.maxAmmo;
            }
            
            isReloading = false;
            onAmmoChanged?.Invoke(currentAmmo);
            onReloadStateChanged?.Invoke(false);
            
            if (_inventoryIntegration != null)
            {
                _inventoryIntegration.DecreaseDurability(this, 0.05f);
            }
        }
        
        reloadCoroutine = null;
    }
    
    private IEnumerator ClearMalfunctionCoroutine()
    {
        isReloading = true;
        onReloadStateChanged?.Invoke(true);
        
        if (weaponData.reloadSound != null)
        {
            PlaySound(weaponData.reloadSound);
        }
        
        yield return new WaitForSeconds(weaponData.reloadTime * 1.5f);
        
        isMalfunctioning = false;
        isReloading = false;
        onReloadStateChanged?.Invoke(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        
        if (clip.ambisonic)
        {
            if (tempAudioSource == null)
            {
                tempAudioSource = gameObject.AddComponent<AudioSource>();
                tempAudioSource.spatialBlend = 1.0f;
                tempAudioSource.priority = 128;
                tempAudioSource.playOnAwake = false;
                tempAudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            }
            
            AudioClip nonAmbisonicClip = AudioClip.Create(
                clip.name + "_nonAmbisonic",
                clip.samples,
                clip.channels,
                clip.frequency,
                false
            );
            
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            nonAmbisonicClip.SetData(samples, 0);
            
            tempAudioSource.clip = nonAmbisonicClip;
            tempAudioSource.Play();
        }
        else
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
        
        if (tempAudioSource != null)
        {
            Destroy(tempAudioSource);
        }
    }
    
    public void RotateWeapon(Vector3 rotationDelta)
    {
        if (!enableCustomRotation) return;
        
        Vector3 scaledDelta = rotationDelta * rotationSpeed * Time.deltaTime;
        targetRotation += scaledDelta;
    }
    
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
    
    public void ResetWeaponRotation(bool instant = false)
    {
        SetWeaponRotation(weaponData.hipRotation, instant);
    }
    
    public void SetCustomRotationEnabled(bool enabled)
    {
        enableCustomRotation = enabled;
    }
    
    public WeaponData GetWeaponData()
    {
        return weaponData;
    }
    
    public void SetAmmo(int ammoCount)
    {
        currentAmmo = Mathf.Clamp(ammoCount, 0, weaponData?.maxAmmo ?? 30);
        onAmmoChanged?.Invoke(currentAmmo);
    }

    public void SetMalfunctioning(bool state)
    {
        isMalfunctioning = state;
    }

    public bool IsMalfunctioning()
    {
        return isMalfunctioning;
    }
}