using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameHUDController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Player player;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float crosshairSpreadMultiplier = 2f;
    [SerializeField] private float crosshairDefaultSize = 10f;
    [SerializeField] private float maxCrosshairSpread = 30f;
    [SerializeField] private float hitMarkerDuration = 0.2f;
    
    private VisualElement root;
    private Label weaponNameLabel;
    private Label currentAmmoLabel;
    private Label maxAmmoLabel;
    private VisualElement weaponIcon;
    private VisualElement healthBar;
    private Label healthValueLabel;
    private Label stanceTextLabel;
    private VisualElement stanceIcon;
    private VisualElement hitMarker;
    private VisualElement[] crosshairLines = new VisualElement[4];
    private VisualElement[] weaponSlots = new VisualElement[3];
    
    private int currentHealth = 100;
    private int maxHealth = 100;
    private int currentWeaponSlot = 1;
    private float currentCrosshairSpread = 0f;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;
    private bool isInitialized = false;
    private Coroutine findPlayerCoroutine;
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        
        StartCoroutine(InitializeWithDelay());
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
        
        if (findPlayerCoroutine != null)
        {
            StopCoroutine(findPlayerCoroutine);
            findPlayerCoroutine = null;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(CheckGameplayStateAfterLoad());
    }
    
    private IEnumerator CheckGameplayStateAfterLoad()
    {
        // Wait for the scene to stabilize
        yield return new WaitForSeconds(0.5f);
        
        bool isGameplayScene = IsGameplayScene();
        SetHUDActive(isGameplayScene);
        
        // Reset references since player might be re-instantiated
        if (isGameplayScene)
        {
            player = null;
            weaponManager = null;
            playerHealth = null;
            
            // Start the reference finding coroutine
            if (findPlayerCoroutine != null)
                StopCoroutine(findPlayerCoroutine);
                
            findPlayerCoroutine = StartCoroutine(FindPlayerWithRetry());
        }
    }
    
    private IEnumerator FindPlayerWithRetry()
    {
        // Keep trying to find the player and required components for up to 5 seconds
        float timeoutLimit = 5f;
        float elapsed = 0f;
        
        while (elapsed < timeoutLimit)
        {
            if (player == null)
                player = FindObjectOfType<Player>();
                
            if (player != null)
            {
                // Found player, now get components
                if (weaponManager == null)
                    weaponManager = player.GetComponentInChildren<WeaponManager>();
                    
                if (playerHealth == null)
                    playerHealth = player.GetComponent<PlayerHealth>();
                
                // If we have all required components, stop searching
                if (weaponManager != null && playerHealth != null)
                {
                    // We found all components, refresh HUD
                    SetupEventListeners();
                    
                    if (!isInitialized)
                        StartCoroutine(InitializeWithDelay());
                    else
                        RefreshHUDData();
                        
                    // Log what we found for debugging
                    Debug.Log($"[HUD] Found player and components. Player: {player != null}, WeaponManager: {weaponManager != null}, PlayerHealth: {playerHealth != null}");
                    
                    yield break;
                }
            }
            
            // Wait a bit before trying again
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }
        
        Debug.LogWarning("[HUD] Timed out waiting for player references.");
    }
    
    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            InitializeUIReferences();
            SetupEventListeners();
            isInitialized = true;
            
            // Check if we should be active right now
            SetHUDActive(IsGameplayScene());
        }
    }
    
    private void Update()
    {
        // Continuously look for player if we don't have one but should be in gameplay
        if (IsGameplayScene())
        {
            if (player == null || weaponManager == null || playerHealth == null)
            {
                RefreshReferences();
            }
            else if (isInitialized && crosshairLines[0] != null)
            {
                UpdateCrosshair();
            }
        }
    }
    
    private bool IsGameplayScene()
    {
        // First check LevelManager for a definitive answer
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevelIndex >= 0)
        {
            return true;
        }
        
        // Use GameManager's state if available
        if (GameManager.Instance != null)
        {
            // If the GameManager thinks we're in a gameplay level, trust it
            bool noMenusPresent = FindObjectOfType<MainMenuController>() == null && 
                                  FindObjectOfType<LevelSelectionUI>() == null;
            return noMenusPresent;
        }
        
        return false;
    }
    
    private void SetHUDActive(bool active)
    {
        if (uiDocument != null)
        {
            uiDocument.enabled = active;
            
            if (active && uiDocument.rootVisualElement != null)
            {
                VisualElement rootElement = uiDocument.rootVisualElement.Q<VisualElement>("root");
                if (rootElement != null)
                    rootElement.style.display = DisplayStyle.Flex;
            }
        }
    }
    
    private void RefreshReferences()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
            
        if (player != null)
        {
            if (weaponManager == null)
                weaponManager = player.GetComponentInChildren<WeaponManager>();
                
            if (playerHealth == null)
                playerHealth = player.GetComponent<PlayerHealth>();
                
            if (weaponManager != null && playerHealth != null)
                SetupEventListeners();
        }
    }
    
    private void RefreshHUDData()
    {
        UpdateWeaponInfo();
        UpdateHealthUI();
        UpdateStanceUI();
    }
    
    private void InitializeUIReferences()
    {
        root = uiDocument.rootVisualElement;
        
        weaponNameLabel = root.Q<Label>("weapon-name");
        currentAmmoLabel = root.Q<Label>("current-ammo");
        maxAmmoLabel = root.Q<Label>("max-ammo");
        weaponIcon = root.Q<VisualElement>("weapon-icon");
        
        healthBar = root.Q<VisualElement>("health-bar");
        healthValueLabel = root.Q<Label>("health-value");
        stanceTextLabel = root.Q<Label>("stance-text");
        stanceIcon = root.Q<VisualElement>("stance-icon");
        
        hitMarker = root.Q<VisualElement>("hit-marker");
        
        var crosshair = root.Q<VisualElement>("crosshair");
        if (crosshair != null)
        {
            crosshairLines[0] = root.Q<VisualElement>("crosshair-top");
            crosshairLines[1] = root.Q<VisualElement>("crosshair-right");
            crosshairLines[2] = root.Q<VisualElement>("crosshair-bottom");
            crosshairLines[3] = root.Q<VisualElement>("crosshair-left");
        }
        
        weaponSlots[0] = root.Q<VisualElement>("weapon-slot-1");
        weaponSlots[1] = root.Q<VisualElement>("weapon-slot-2");
        weaponSlots[2] = root.Q<VisualElement>("weapon-slot-3");
    }
    
    private void SetupEventListeners()
    {
        if (weaponManager != null && weaponManager.onWeaponChanged != null)
        {
            weaponManager.onWeaponChanged.RemoveListener(OnWeaponChanged);
            weaponManager.onWeaponChanged.AddListener(OnWeaponChanged);
        }
        
        if (playerHealth != null && playerHealth.onHealthChanged != null)
        {
            playerHealth.onHealthChanged.RemoveListener(OnHealthChanged);
            playerHealth.onHealthChanged.AddListener(OnHealthChanged);
        }
        
        SetupWeaponListeners();
    }
    
    private void SetupWeaponListeners()
    {
        if (weaponManager == null) return;
        
        try
        {
            int weaponCount = weaponManager.GetWeaponCount();
            for (int i = 0; i < weaponCount; i++)
            {
                Transform weaponTransform = weaponManager.transform.GetChild(i);
                if (weaponTransform == null) continue;
                
                var weapon = weaponTransform.GetComponent<Weapon>();
                if (weapon != null)
                {
                    if (weapon.onAmmoChanged != null)
                    {
                        weapon.onAmmoChanged.RemoveListener(OnAmmoChanged);
                        weapon.onAmmoChanged.AddListener(OnAmmoChanged);
                    }
                    
                    if (weapon.onReloadStateChanged != null)
                    {
                        weapon.onReloadStateChanged.RemoveListener(OnReloadStateChanged);
                        weapon.onReloadStateChanged.AddListener(OnReloadStateChanged);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HUD] Error setting up weapon listeners: {e.Message}");
        }
    }
    
    public void OnWeaponChanged(WeaponData weaponData, int currentAmmo)
    {
        if (!IsGameplayScene()) return;
        
        SetupWeaponListeners();
        UpdateWeaponInfo();
        UpdateWeaponSlots();
    }
    
    public void OnAmmoChanged(int currentAmmo)
    {
        if (!IsGameplayScene() || currentAmmoLabel == null) return;
        
        currentAmmoLabel.text = currentAmmo.ToString();
    }
    
    public void OnReloadStateChanged(bool isReloading)
    {
        if (!IsGameplayScene()) return;
        
        this.isReloading = isReloading;
        
        VisualElement reloadIndicator = root.Q<VisualElement>("reload-indicator");
        if (reloadIndicator != null)
            reloadIndicator.style.display = isReloading ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (isReloading)
        {
            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);
                
            reloadCoroutine = StartCoroutine(UpdateReloadProgress());
        }
        else
        {
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }
        }
    }
    
    public void OnHealthChanged(int newHealth, int maxHealth)
    {
        if (!IsGameplayScene()) return;
        
        currentHealth = newHealth;
        this.maxHealth = maxHealth;
        UpdateHealthUI();
    }
    
    public void ShowHitMarker()
    {
        if (!IsGameplayScene() || hitMarker == null) return;
        
        hitMarker.style.display = DisplayStyle.Flex;
        StartCoroutine(HideHitMarker());
    }
    
    private IEnumerator HideHitMarker()
    {
        yield return new WaitForSeconds(hitMarkerDuration);
        
        if (hitMarker != null)
            hitMarker.style.display = DisplayStyle.None;
    }
    
    private IEnumerator UpdateReloadProgress()
    {
        if (weaponManager == null) yield break;
        
        float reloadTime = 2.0f;
        try
        {
            int currentWeaponIndex = weaponManager.GetCurrentWeaponIndex();
            if (currentWeaponIndex >= 0)
            {
                foreach (var data in weaponManager.GetAvailableWeapons())
                {
                    if (data.weaponSlot == weaponManager.GetCurrentWeaponSlot())
                    {
                        reloadTime = data.reloadTime;
                        break;
                    }
                }
            }
        }
        catch {}
        
        VisualElement reloadProgressFill = root.Q<VisualElement>("reload-progress-fill");
        if (reloadProgressFill == null) yield break;
        
        float elapsed = 0f;
        while (elapsed < reloadTime && isReloading)
        {
            float progress = elapsed / reloadTime;
            reloadProgressFill.style.height = new StyleLength(new Length(progress * 100f, LengthUnit.Percent));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        reloadProgressFill.style.height = new StyleLength(new Length(0f, LengthUnit.Percent));
    }
    
    private void UpdateWeaponInfo()
    {
        if (!IsGameplayScene() || weaponManager == null || weaponNameLabel == null || 
            currentAmmoLabel == null || maxAmmoLabel == null) return;
        
        try
        {
            int weaponCount = weaponManager.GetWeaponCount();
            if (weaponCount <= 0) return;
            
            int currentWeaponIndex = weaponManager.GetCurrentWeaponIndex();
            if (currentWeaponIndex < 0) return;
            
            WeaponData weaponData = null;
            foreach (var data in weaponManager.GetAvailableWeapons())
            {
                if (data.weaponSlot == weaponManager.GetCurrentWeaponSlot())
                {
                    weaponData = data;
                    break;
                }
            }
            
            if (weaponData == null) return;
            
            weaponNameLabel.text = weaponData.weaponName;
            
            Transform weaponTransform = weaponManager.transform.GetChild(currentWeaponIndex);
            if (weaponTransform == null) return;
            
            var weapon = weaponTransform.GetComponent<Weapon>();
            if (weapon != null)
            {
                currentAmmoLabel.text = weapon.CurrentAmmo.ToString();
                maxAmmoLabel.text = weaponData.maxAmmo.ToString();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HUD] Error updating weapon info: {e.Message}");
        }
    }
    
    private void UpdateWeaponSlots()
    {
        if (!IsGameplayScene() || weaponManager == null) return;
        
        try
        {
            int currentSlot = weaponManager.GetCurrentWeaponSlot();
            if (currentSlot <= 0) return;
            
            currentWeaponSlot = currentSlot;
            
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null) continue;
                
                bool isActive = (i + 1) == currentSlot;
                
                weaponSlots[i].RemoveFromClassList("weapon-slot-active");
                
                if (isActive)
                    weaponSlots[i].AddToClassList("weapon-slot-active");
                
                Label slotNameLabel = weaponSlots[i].Q<Label>("slot-name");
                if (slotNameLabel != null)
                {
                    foreach (var data in weaponManager.GetAvailableWeapons())
                    {
                        if (data.weaponSlot == (i + 1))
                        {
                            slotNameLabel.text = data.weaponName;
                            break;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HUD] Error updating weapon slots: {e.Message}");
        }
    }
    
    private void UpdateHealthUI()
    {
        if (!IsGameplayScene() || healthBar == null || healthValueLabel == null) return;
        
        try
        {
            float healthPercentage = (float)currentHealth / maxHealth;
            healthBar.style.width = new StyleLength(new Length(healthPercentage * 100f, LengthUnit.Percent));
            
            if (healthPercentage <= 0.2f)
                healthBar.style.backgroundColor = new StyleColor(new Color(1f, 0.3f, 0.3f));
            else if (healthPercentage <= 0.5f)
                healthBar.style.backgroundColor = new StyleColor(new Color(1f, 0.6f, 0.2f));
            else
                healthBar.style.backgroundColor = new StyleColor(new Color(0f, 0.9f, 0.46f));
            
            healthValueLabel.text = currentHealth.ToString();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HUD] Error updating health UI: {e.Message}");
        }
    }
    
    private void UpdateStanceUI()
    {
        if (!IsGameplayScene() || player == null || stanceTextLabel == null) return;
        
        try
        {
            PlayerCharacter character = player.GetComponentInChildren<PlayerCharacter>();
            if (character == null) return;
            
            Stance currentStance = Stance.Stand;
            // Try to get actual stance if method available
            // if (character.GetStance != null) currentStance = character.GetStance();
            
            string stanceText = "STANDING";
            
            switch (currentStance)
            {
                case Stance.Crouch: stanceText = "CROUCHING"; break;
                case Stance.Slide: stanceText = "SLIDING"; break;
            }
            
            stanceTextLabel.text = stanceText;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HUD] Error updating stance UI: {e.Message}");
        }
    }
    
    private void UpdateCrosshair()
    {
        if (!IsGameplayScene() || player == null || crosshairLines == null) return;
        if (crosshairLines[0] == null || crosshairLines[1] == null || 
            crosshairLines[2] == null || crosshairLines[3] == null) return;
        
        try
        {
            PlayerCharacter character = player.GetComponentInChildren<PlayerCharacter>();
            if (character == null) return;
            
            bool isAiming = player._inputActions != null && player._inputActions.Gameplay.Aim.IsPressed();
            Vector3 velocity = Vector3.zero;
            float velocityMagnitude = velocity.magnitude;
            
            float targetSpread = crosshairDefaultSize;
            
            if (!isAiming)
            {
                targetSpread += Mathf.Min(velocityMagnitude * crosshairSpreadMultiplier, maxCrosshairSpread);
                
                bool isGrounded = true;
                if (!isGrounded) targetSpread += 10f;
                
                if (player._inputActions != null && player._inputActions.Gameplay.Fire.IsPressed())
                    targetSpread += 15f;
            }
            else
            {
                targetSpread = crosshairDefaultSize * 0.6f;
            }
            
            currentCrosshairSpread = Mathf.Lerp(currentCrosshairSpread, targetSpread, Time.deltaTime * 10f);
            
            for (int i = 0; i < crosshairLines.Length; i++)
            {
                float positionOffset = currentCrosshairSpread * 0.5f;
                
                if (i % 2 == 0)
                {
                    if (i == 0)
                        crosshairLines[i].style.bottom = new StyleLength(new Length(50f + positionOffset, LengthUnit.Percent));
                    else
                        crosshairLines[i].style.top = new StyleLength(new Length(50f + positionOffset, LengthUnit.Percent));
                }
                else
                {
                    if (i == 3)
                        crosshairLines[i].style.right = new StyleLength(new Length(50f + positionOffset, LengthUnit.Percent));
                    else
                        crosshairLines[i].style.left = new StyleLength(new Length(50f + positionOffset, LengthUnit.Percent));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[HUD] Error updating crosshair: {e.Message}");
        }
    }
}