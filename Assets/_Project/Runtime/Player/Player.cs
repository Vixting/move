using UnityEngine;
using UnityEngine.InputSystem;
using InventorySystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Character characterData;
    
    public PlayerInputActions _inputActions;
    
    private bool _isInputEnabled = true;
    private bool _initialized = false;
    private bool _isPaused = false;
   
    void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (_initialized) return;
        
        Debug.Log("Initializing Player");
        
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        
        if (characterData == null)
        {
            characterData = GetComponent<Character>();
            if (characterData == null)
            {
                characterData = gameObject.AddComponent<Character>();
                Debug.Log("Created Character component on Player");
            }
        }
        
        if (playerCharacter != null)
            playerCharacter.Initialize();
            
        if (playerCamera != null && playerCharacter != null)
            playerCamera.Initialize(playerCharacter.GetCameraTarget(), playerCharacter);
       
        WeaponData[] existingWeapons = null;
        
        if (GameManager.Instance != null)
        {
            existingWeapons = GameManager.Instance.GetSavedWeapons();
            
            if (existingWeapons == null || existingWeapons.Length == 0)
            {
                Debug.Log("No weapons found, creating default weapons");
                
                WeaponData defaultWeapon = GameManager.Instance.CreateWeapon(
                    "Player Handgun", 
                    WeaponType.Pistol, 
                    15f, 
                    12
                );
                
                if (defaultWeapon != null)
                {
                    if (string.IsNullOrEmpty(defaultWeapon.WeaponId))
                    {
                        string id = defaultWeapon.WeaponId;
                    }
                    
                    if (string.IsNullOrEmpty(defaultWeapon.inventoryItemId))
                    {
                        defaultWeapon.inventoryItemId = System.Guid.NewGuid().ToString();
                        Debug.Log($"Generated inventory item ID for default weapon: {defaultWeapon.inventoryItemId}");
                    }
                    
                    existingWeapons = new WeaponData[] { defaultWeapon };
                    GameManager.Instance.RegisterWeapons(existingWeapons);
                }
            }
        }
        
        if (weaponManager != null && playerCamera != null)
        {
            try
            {
                weaponManager.Initialize(playerCamera, _inputActions, playerCharacter, existingWeapons);
                
                if (GameManager.Instance != null && existingWeapons == null)
                {
                    WeaponData[] availableWeapons = weaponManager.GetAvailableWeapons();
                    
                    if (availableWeapons != null && availableWeapons.Length > 0)
                    {
                        foreach (var weapon in availableWeapons)
                        {
                            if (weapon != null)
                            {
                                string weaponId = weapon.WeaponId;
                                
                                if (string.IsNullOrEmpty(weapon.inventoryItemId))
                                {
                                    weapon.inventoryItemId = System.Guid.NewGuid().ToString();
                                }
                            }
                        }
                        
                        GameManager.Instance.RegisterWeapons(availableWeapons);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error initializing WeaponManager: {e.Message}\n{e.StackTrace}");
            }
        }
        
        if (inventoryManager != null && characterData != null)
        {
            if (weaponManager != null)
            {
                inventoryManager.SetWeaponManager(weaponManager);
            }
        }
       
        if (_inputActions != null)
        {
            _inputActions.Gameplay.Aim.started += OnAimStarted;
            _inputActions.Gameplay.Aim.canceled += OnAimCanceled;
            _inputActions.Gameplay.Inventory.performed += OnInventoryToggle;
            _inputActions.UI.InventoryClose.performed += OnInventoryToggle;
            _inputActions.Gameplay.Pause.performed += OnPausePerformed;
        }
        
        if (IsGameplayScene())
        {
            EnableGameplayMode(true);
        }
        else
        {
            EnableGameplayMode(false);
        }
        
        _initialized = true;
    }
    
    private void OnInventoryToggle(InputAction.CallbackContext context)
    {
        Debug.Log("OnInventoryToggle method called");
        if (inventoryManager != null)
        {
            Debug.Log("Inventory toggle triggered");
            
            if (_inputActions != null)
            {
                if (_inputActions.Gameplay.enabled)
                {
                    inventoryManager.ShowInventory();
                }
                else if (_inputActions.UI.enabled)
                {
                    inventoryManager.HideInventory();
                    EnableGameplayMode(true);
                }
            }
        }
    }
   
    void Update()
    {
        if (!_initialized)
        {
            Initialize();
            return;
        }
        
        if (!_isInputEnabled || _inputActions == null || _isPaused)
            return;
            
        var input = _inputActions.Gameplay;
        var deltaTime = Time.deltaTime;
       
        var lookInput = input.Look.ReadValue<Vector2>();
       
        var cameraInput = new CameraInput
        {
            Look = lookInput,
            Move = input.Move.ReadValue<Vector2>()
        };
       
        if (playerCamera != null)
        {
            playerCamera.UpdateInput(cameraInput);
            playerCamera.UpdateRotation();
            playerCamera.UpdateFOV();
        }
       
        if (playerCharacter != null)
        {
            CrouchInput crouchState = CrouchInput.None;
            if (input.Crouch.IsPressed())
            {
                crouchState = CrouchInput.Hold;
            }
            else if (input.Crouch.WasReleasedThisFrame())
            {
                crouchState = CrouchInput.Release;
            }
           
            var CharacterInput = new CharacterInput
            {
                Rotation = playerCamera != null ? playerCamera.transform.rotation : transform.rotation,
                Move = input.Move.ReadValue<Vector2>(),
                Jump = input.Jump.WasPressedThisFrame(),
                JumpSustain = input.Jump.IsPressed(),
                Crouch = crouchState
            };
           
            playerCharacter.UpdateInput(CharacterInput);
            playerCharacter.UpdateBody(deltaTime);
        }
    }
   
    void LateUpdate()
    {
        if (!_isInputEnabled || playerCamera == null || playerCharacter == null || _isPaused)
            return;
            
        playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
    }
   
    void OnDestroy()
    {
        if (_inputActions != null)
        {
            _inputActions.Gameplay.Aim.started -= OnAimStarted;
            _inputActions.Gameplay.Aim.canceled -= OnAimCanceled;
            _inputActions.Gameplay.Inventory.performed -= OnInventoryToggle;
            _inputActions.Gameplay.Pause.performed -= OnPausePerformed;
            _inputActions.Dispose();
        }
    }
   
    private void OnAimStarted(InputAction.CallbackContext context)
    {
        if (!_isInputEnabled)
            return;
            
        if (weaponManager != null)
            weaponManager.SetAiming(true);
            
        if (playerCamera != null)
            playerCamera.SetAiming(true);
    }
   
    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        if (!_isInputEnabled)
            return;
            
        if (weaponManager != null)
            weaponManager.SetAiming(false);
            
        if (playerCamera != null)
            playerCamera.SetAiming(false);
    }
    
    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        PauseMenuController pauseMenuController = FindObjectOfType<PauseMenuController>();
        
        if (pauseMenuController != null)
        {
            if (pauseMenuController.IsPauseMenuActive())
            {
                pauseMenuController.HidePauseMenu();
                _isPaused = false;
            }
            else
            {
                pauseMenuController.ShowPauseMenu();
                _isPaused = true;
            }
        }
        else
        {
            Debug.LogWarning("PauseMenuController not found in scene");
        }
    }
   
    public void Teleport(Vector3 position)
    {
        if (playerCharacter != null)
            playerCharacter.SetPosition(position);
    }
    
    public void EnableGameplayMode(bool enable)
    {
        _isInputEnabled = enable;
        _isPaused = !enable;
        
        if (weaponManager != null)
        {
            weaponManager.SetEnabled(enable);
        }
        
        if (_inputActions != null)
        {
            if (enable)
            {
                _inputActions.Gameplay.Enable();
                _inputActions.UI.Disable();
            }
            else
            {
                _inputActions.Gameplay.Disable();
                _inputActions.UI.Enable();
            }
        }
        
        if (enable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    private bool IsGameplayScene()
    {
        return FindObjectOfType<MainMenuController>() == null && 
               FindObjectOfType<LevelSelectionUI>() == null;
    }

    public void SetInventoryManager(InventoryManager manager)
    {
        inventoryManager = manager;
        
        if (weaponManager != null && inventoryManager != null)
        {
            inventoryManager.SetWeaponManager(weaponManager);
        }
    }
        
    public PlayerInputActions GetInputActions()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerInputActions();
            _inputActions.Enable();
        }
        return _inputActions;
    }
    
    public Character GetCharacter()
    {
        return characterData;
    }
    
    public WeaponManager GetWeaponManager()
    {
        return weaponManager;
    }
}