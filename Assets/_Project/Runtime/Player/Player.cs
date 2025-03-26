using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private WeaponManager weaponManager;
    
    public PlayerInputActions _inputActions;
    
    private bool _isInputEnabled = true;
    private bool _initialized = false;
   
    void Start()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        if (_initialized) return;
        
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
        
        if (playerCharacter != null)
            playerCharacter.Initialize();
            
        if (playerCamera != null && playerCharacter != null)
            playerCamera.Initialize(playerCharacter.GetCameraTarget(), playerCharacter);
       
        WeaponData[] existingWeapons = GameManager.Instance?.GetSavedWeapons();
        
        if (weaponManager != null && playerCamera != null)
        {
            weaponManager.Initialize(playerCamera, _inputActions, playerCharacter, existingWeapons);
            
            if (GameManager.Instance != null && existingWeapons == null)
            {
                GameManager.Instance.RegisterWeapons(weaponManager.GetAvailableWeapons());
            }
        }
       
        _inputActions.Gameplay.Aim.started += OnAimStarted;
        _inputActions.Gameplay.Aim.canceled += OnAimCanceled;
        _inputActions.Gameplay.Inventory.performed += OnInventoryToggle;
        
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
        Debug.Log("Player received inventory toggle input");
        
        var inventoryManager = FindObjectOfType<InventoryUIManager>();
        if (inventoryManager != null)
        {
            inventoryManager.ToggleInventory(context);
        }
        else
        {
            Debug.LogWarning("No InventoryUIManager found in scene");
        }
    }
   
    void Update()
    {
        if (!_initialized)
        {
            Initialize();
            return;
        }
        
        if (!_isInputEnabled)
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
                Rotation = playerCamera.transform.rotation,
                Move = input.Move.ReadValue<Vector2>(),
                Jump = input.Jump.WasPressedThisFrame(),
                JumpSustain = input.Jump.IsPressed(),
                Crouch = crouchState
            };
           
            playerCharacter.UpdateInput(CharacterInput);
            playerCharacter.UpdateBody(deltaTime);
        }
       
        #if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit))
            {
                Teleport(hit.point);
            }
        }
        #endif
    }
   
    void LateUpdate()
    {
        if (!_isInputEnabled || playerCamera == null || playerCharacter == null)
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
   
    public void Teleport(Vector3 position)
    {
        if (playerCharacter != null)
            playerCharacter.SetPosition(position);
    }
    
    public void EnableGameplayMode(bool enable)
    {
        _isInputEnabled = enable;
        
        if (weaponManager != null)
        {
            weaponManager.SetEnabled(enable);
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
    
    public PlayerInputActions GetInputActions()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerInputActions();
            _inputActions.Enable();
        }
        return _inputActions;
    }
}