using UnityEngine;
using UnityEngine.InputSystem;
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private WeaponManager weaponManager;
    
    // Changed from private to public to allow access from WeaponSelectionUI
    public PlayerInputActions _inputActions;
    
    private bool _isInputEnabled = true;
   
    void Start()
    {
        if (IsGameplayScene())
        {
            EnableGameplayMode(true);
        }
        else
        {
            EnableGameplayMode(false);
        }
       
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();
       
        playerCharacter.Initialize();
        playerCamera.Initialize(playerCharacter.GetCameraTarget(), playerCharacter);
       
        weaponManager.Initialize(playerCamera, _inputActions, playerCharacter);
       
        _inputActions.Gameplay.Aim.started += OnAimStarted;
        _inputActions.Gameplay.Aim.canceled += OnAimCanceled;
    }
   
    void Update()
    {
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
       
        playerCamera.UpdateInput(cameraInput);
        playerCamera.UpdateRotation();
        playerCamera.UpdateFOV();
       
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
       
        #if UNITY_EDITOR
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit))
            {
                Teleport(hit.point);
            }
        }
        
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EnableGameplayMode(!_isInputEnabled);
        }
        #endif
    }
   
    void LateUpdate()
    {
        if (!_isInputEnabled)
            return;
            
        playerCamera.UpdatePosition(playerCharacter.GetCameraTarget());
    }
   
    void OnDestroy()
    {
        if (_inputActions != null)
        {
            _inputActions.Gameplay.Aim.started -= OnAimStarted;
            _inputActions.Gameplay.Aim.canceled -= OnAimCanceled;
            _inputActions.Dispose();
        }
    }
   
    private void OnAimStarted(InputAction.CallbackContext context)
    {
        if (!_isInputEnabled)
            return;
            
        weaponManager.SetAiming(true);
        playerCamera.SetAiming(true);
    }
   
    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        if (!_isInputEnabled)
            return;
            
        weaponManager.SetAiming(false);
        playerCamera.SetAiming(false);
    }
   
    public void Teleport(Vector3 position)
    {
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
        return !FindAnyObjectByType<LevelSelectionUI>();
    }
    
    // Helper method to retrieve input actions from outside classes
    public PlayerInputActions GetInputActions()
    {
        return _inputActions;
    }
}