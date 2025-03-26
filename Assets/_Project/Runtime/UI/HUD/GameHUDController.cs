using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameHUDController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float hitMarkerDuration = 0.2f;
    [SerializeField] private float checkPlayerInterval = 0.5f;
    
    private VisualElement root;
    private Label weaponNameLabel;
    private Label currentAmmoLabel;
    private Label maxAmmoLabel;
    private VisualElement weaponIcon;
    private VisualElement healthBar;
    private Label healthValueLabel;
    private Label stanceTextLabel;
    private VisualElement crosshair;
    private VisualElement[] crosshairLines = new VisualElement[4];
    private VisualElement[] weaponSlots = new VisualElement[3];
    private VisualElement hitMarker;
    private VisualElement reloadIndicator;
    private VisualElement reloadProgressFill;
    
    private Player player;
    private WeaponManager weaponManager;
    private PlayerHealth playerHealth;
    private PlayerCharacter playerCharacter;
    
    private int currentHealth = 100;
    private int maxHealth = 100;
    private float currentCrosshairSpread = 10f;
    private bool isReloading = false;
    private Coroutine reloadCoroutine;
    private bool isInitialized = false;
    private string lastStanceText = "";
    
    private void Awake() => DontDestroyOnLoad(gameObject);
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        StartCoroutine(InitializeUIWithDelay());
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (reloadCoroutine != null) { StopCoroutine(reloadCoroutine); reloadCoroutine = null; }
        UnsubscribeFromEvents();
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(HandleSceneChange());
    
    private IEnumerator HandleSceneChange()
    {
        yield return new WaitForSeconds(0.5f);
        bool isGameplayScene = IsGameplayScene();
        ToggleHUD(isGameplayScene);
        
        if (isGameplayScene) StartCoroutine(FindPlayerAndComponents());
        else { UnsubscribeFromEvents(); player = null; weaponManager = null; playerHealth = null; playerCharacter = null; }
    }
    
    private IEnumerator InitializeUIWithDelay()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            InitializeUIReferences();
            isInitialized = true;
            ToggleHUD(IsGameplayScene());
            if (IsGameplayScene()) StartCoroutine(FindPlayerAndComponents());
        }
    }
    
    private IEnumerator FindPlayerAndComponents()
    {
        float startTime = Time.time;
        float timeout = 10f;
        
        while (Time.time - startTime < timeout)
        {
            player = FindObjectOfType<Player>();
            
            if (player != null)
            {
                yield return new WaitForSeconds(0.2f);
                
                weaponManager = player.GetComponentInChildren<WeaponManager>();
                playerHealth = player.GetComponent<PlayerHealth>();
                playerCharacter = player.GetComponentInChildren<PlayerCharacter>();
                
                if (weaponManager != null && playerCharacter != null)
                {
                    SubscribeToEvents();
                    RefreshHUDData();
                    yield break;
                }
            }
            
            yield return new WaitForSeconds(checkPlayerInterval);
        }
    }
    
    private void Update()
    {
        if (!IsGameplayScene() || !isInitialized) return;
        
        if (player == null || weaponManager == null || playerCharacter == null)
        {
            if (!IsSearchingForPlayer()) StartCoroutine(FindPlayerAndComponents());
            return;
        }
        
        UpdateCrosshair();
        UpdateStanceUI();
    }
    
    private bool IsSearchingForPlayer()
    {
        return System.Array.Exists(StartCoroutine("").ToString().Split('_'), x => x.Contains("FindPlayerAndComponents"));
    }
    
    private bool IsGameplayScene()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevelIndex >= 0) return true;
        return !IsMenuActive();
    }
    
    private bool IsMenuActive()
    {
        return FindObjectOfType<MainMenuController>() != null || FindObjectOfType<LevelSelectionUI>() != null;
    }
    
    private void ToggleHUD(bool visible)
    {
        if (uiDocument == null) return;
        uiDocument.enabled = visible;
        if (root != null) root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    private void InitializeUIReferences()
    {
        root = uiDocument.rootVisualElement.Q<VisualElement>("root");
        if (root == null) return;
        
        weaponNameLabel = root.Q<Label>("weapon-name");
        currentAmmoLabel = root.Q<Label>("current-ammo");
        maxAmmoLabel = root.Q<Label>("max-ammo");
        weaponIcon = root.Q<VisualElement>("weapon-icon");
        
        healthBar = root.Q<VisualElement>("health-bar");
        healthValueLabel = root.Q<Label>("health-value");
        stanceTextLabel = root.Q<Label>("stance-text");
        
        hitMarker = root.Q<VisualElement>("hit-marker");
        reloadIndicator = root.Q<VisualElement>("reload-indicator");
        if (reloadIndicator != null) reloadProgressFill = reloadIndicator.Q<VisualElement>("reload-progress-fill");
        
        crosshair = root.Q<VisualElement>("crosshair");
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
    
    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();
        
        if (weaponManager != null && weaponManager.onWeaponChanged != null)
            weaponManager.onWeaponChanged.AddListener(OnWeaponChanged);
        
        if (playerHealth != null && playerHealth.onHealthChanged != null)
            playerHealth.onHealthChanged.AddListener(OnHealthChanged);
        
        SetupWeaponListeners();
    }
    
    private void UnsubscribeFromEvents()
    {
        if (weaponManager != null && weaponManager.onWeaponChanged != null)
            weaponManager.onWeaponChanged.RemoveListener(OnWeaponChanged);
            
        if (playerHealth != null && playerHealth.onHealthChanged != null)
            playerHealth.onHealthChanged.RemoveListener(OnHealthChanged);
            
        if (weaponManager != null)
        {
            try
            {
                int weaponCount = weaponManager.GetWeaponCount();
                for (int i = 0; i < weaponCount; i++)
                {
                    var weapon = weaponManager.transform.GetChild(i).GetComponent<Weapon>();
                    if (weapon != null)
                    {
                        if (weapon.onAmmoChanged != null)
                            weapon.onAmmoChanged.RemoveListener(OnAmmoChanged);
                            
                        if (weapon.onReloadStateChanged != null)
                            weapon.onReloadStateChanged.RemoveListener(OnReloadStateChanged);
                    }
                }
            }
            catch { }
        }
    }
    
    private void SetupWeaponListeners()
    {
        if (weaponManager == null) return;
        
        try
        {
            int weaponCount = weaponManager.GetWeaponCount();
            for (int i = 0; i < weaponCount; i++)
            {
                var weaponTransform = weaponManager.transform.GetChild(i);
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
        catch { }
    }
    
    private void RefreshHUDData()
    {
        UpdateWeaponInfo();
        UpdateWeaponSlots();
        UpdateHealthUI();
        UpdateStanceUI();
    }
    
    public void OnWeaponChanged(WeaponData weaponData, int currentAmmo)
    {
        SetupWeaponListeners();
        UpdateWeaponInfo();
        UpdateWeaponSlots();
    }
    
    public void OnAmmoChanged(int currentAmmo)
    {
        if (currentAmmoLabel != null) currentAmmoLabel.text = currentAmmo.ToString();
    }
    
    public void OnReloadStateChanged(bool isReloading)
    {
        this.isReloading = isReloading;
        if (reloadIndicator != null)
            reloadIndicator.style.display = isReloading ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (isReloading)
        {
            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
            reloadCoroutine = StartCoroutine(UpdateReloadProgress());
        }
        else if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }
    }
    
    public void OnHealthChanged(int newHealth, int maxHealth)
    {
        currentHealth = newHealth;
        this.maxHealth = maxHealth;
        UpdateHealthUI();
    }
    
    public void ShowHitMarker()
    {
        if (hitMarker == null) return;
        hitMarker.style.display = DisplayStyle.Flex;
        StartCoroutine(HideHitMarker());
    }
    
    private IEnumerator HideHitMarker()
    {
        yield return new WaitForSeconds(hitMarkerDuration);
        if (hitMarker != null) hitMarker.style.display = DisplayStyle.None;
    }
    
    private IEnumerator UpdateReloadProgress()
    {
        if (weaponManager == null || reloadProgressFill == null) yield break;
        
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
        catch { }
        
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
        if (weaponManager == null || weaponNameLabel == null || currentAmmoLabel == null || maxAmmoLabel == null) return;
        
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
            
            var weapon = weaponManager.transform.GetChild(currentWeaponIndex).GetComponent<Weapon>();
            if (weapon != null)
            {
                currentAmmoLabel.text = weapon.CurrentAmmo.ToString();
                maxAmmoLabel.text = weaponData.maxAmmo.ToString();
            }
        }
        catch { }
    }
    
    private void UpdateWeaponSlots()
    {
        if (weaponManager == null) return;
        
        try
        {
            int currentSlot = weaponManager.GetCurrentWeaponSlot();
            if (currentSlot <= 0) return;
            
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null) continue;
                
                bool isActive = (i + 1) == currentSlot;
                weaponSlots[i].RemoveFromClassList("weapon-slot-active");
                if (isActive) weaponSlots[i].AddToClassList("weapon-slot-active");
                
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
        catch { }
    }
    
    private void UpdateHealthUI()
    {
        if (healthBar == null || healthValueLabel == null) return;
        
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
        catch { }
    }
    
    private void UpdateStanceUI()
    {
        if (playerCharacter == null || stanceTextLabel == null) return;
        
        try
        {
            string stanceText = "STANDING";
            
            if (playerCharacter.IsSliding())
                stanceText = "SLIDING";
            else if (playerCharacter.GetStance() == Stance.Crouch)
                stanceText = "CROUCHING";
            
            if (stanceText != lastStanceText)
            {
                stanceTextLabel.text = stanceText;
                lastStanceText = stanceText;
            }
        }
        catch { }
    }
    
    private void UpdateCrosshair()
    {
        if (player == null || crosshair == null || crosshairLines[0] == null) return;
        
        try
        {
            bool isAiming = player._inputActions != null && player._inputActions.Gameplay.Aim.IsPressed();
            Vector3 velocity = playerCharacter != null ? playerCharacter.GetVelocity() : Vector3.zero;
            float velocityMagnitude = velocity.magnitude;
            
            float targetSpread = 10f;
            
            if (!isAiming)
            {
                targetSpread += Mathf.Min(velocityMagnitude * 2f, 30f);
                
                if (player._inputActions != null && player._inputActions.Gameplay.Fire.IsPressed())
                    targetSpread += 15f;
                
                bool isGrounded = playerCharacter != null && playerCharacter.IsGrounded();
                if (!isGrounded)
                    targetSpread += 10f;
            }
            else
                targetSpread = 6f;
            
            currentCrosshairSpread = Mathf.Lerp(currentCrosshairSpread, targetSpread, Time.deltaTime * 10f);
            
            for (int i = 0; i < crosshairLines.Length; i++)
            {
                if (crosshairLines[i] == null) continue;
                
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
        catch { }
    }
}