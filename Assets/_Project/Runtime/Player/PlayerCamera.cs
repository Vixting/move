using System;
using UnityEngine;
using KinematicCharacterController;

public struct CameraInput
{
    public Vector2 Look;
    public Vector2 Move;
}

public class PlayerCamera : MonoBehaviour
{
    [Header("Basic Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float characterEyeHeight = 1.7f;
    
    [Header("FOV Settings")]
    [SerializeField] private float baseFOV = 90f;
    [SerializeField] private float aimDownSightsFOV = 65f;
    [SerializeField] private float maxFOVIncrease = 15f;
    [SerializeField] private float minSpeedForFOV = 5f;
    [SerializeField] private float maxSpeedForFOV = 35f;
    [SerializeField] private float fovLerpSpeed = 10f;
    
    [Header("Camera Sway")]
    [SerializeField] private float swayAmount = 0.5f;
    [SerializeField] private float swaySpeed = 2f;
    [SerializeField] private float maxSwayAngle = 2f;
    [SerializeField] private float swayLerpSpeed = 15f;
    
    [Header("Camera Lean")]
    [SerializeField] private float maxLeanAngle = 15f;
    [SerializeField] private float leanSpeed = 4f;
    [SerializeField] private float movementLeanAmount = 10f;
    [SerializeField] private float slideLeanMultiplier = 1.5f;
    [SerializeField] private float leanSmoothTime = 0.2f;
    
    [Header("Impact Effects")]
    [SerializeField] private float landingImpactFOVKick = 5f;
    [SerializeField] private float impactRecoverySpeed = 8f;
    
    private Vector3 _eulerAngles;
    private CameraInput _input;
    private Vector3 _targetSwayRotation;
    private Vector3 _currentSwayRotation;
    private float _currentFOV;
    private float _targetFOV;
    private float _impactFOVOffset;
    private bool _wasGrounded;
    private PlayerCharacter _character;
    private Vector3 _lastPosition;
    private Vector3 _smoothDampVelocity;
    private float _targetLeanAngle;
    private float _currentLeanAngle;
    private float _leanVelocity;
    private float _smoothedMoveX;
    private float _moveXVelocity;
    private Quaternion _initialRotation;
    private Vector2 currentLookDelta;
    private Vector2 previousLookInput;
    private Vector2 currentMoveInput;
    private bool _isAiming;
    
    public void Initialize(Transform target, PlayerCharacter character)
    {
        transform.position = target.position;
        _eulerAngles = transform.eulerAngles = target.eulerAngles;
        _character = character;
        _lastPosition = transform.position;
        _currentFOV = baseFOV;
        _targetFOV = baseFOV;
        _initialRotation = transform.localRotation;
        
        if (mainCamera == null)
            mainCamera = GetComponent<Camera>();
            
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.gameObject.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }
    }

    public void UpdateInput(CameraInput input)
    {
        _input = input;
        
        Vector2 lookDelta = input.Look - previousLookInput;
        currentLookDelta = lookDelta;
        previousLookInput = input.Look;
        
        currentMoveInput = input.Move;
    }

    public void UpdateRotation()
    {
        float pitch = -_input.Look.y * mouseSensitivity;
        float yaw = _input.Look.x * mouseSensitivity;
        
        _eulerAngles.x += pitch;
        _eulerAngles.y += yaw;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -89f, 89f);
        
        var velocity = (transform.position - _lastPosition) / Time.deltaTime;
        var horizontalSpeed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
        
        float verticalAngleMultiplier = Mathf.Cos(Mathf.Deg2Rad * Mathf.Abs(_eulerAngles.x));
        
        _targetSwayRotation = new Vector3(
            Mathf.Sin(Time.time * swaySpeed) * swayAmount * horizontalSpeed * verticalAngleMultiplier,
            Mathf.Cos(Time.time * swaySpeed * 0.5f) * swayAmount * horizontalSpeed * verticalAngleMultiplier,
            0f
        );
        
        _targetSwayRotation = Vector3.ClampMagnitude(_targetSwayRotation, maxSwayAngle);
        _currentSwayRotation = Vector3.Lerp(_currentSwayRotation, _targetSwayRotation, Time.deltaTime * swayLerpSpeed);
        
        _smoothedMoveX = Mathf.SmoothDamp(_smoothedMoveX, _input.Move.x, ref _moveXVelocity, leanSmoothTime);
        _targetLeanAngle = -_smoothedMoveX * movementLeanAmount;
        
        if (_character != null && _character.IsSliding())
        {
            var slideDir = Vector3.Dot(_character.GetVelocity().normalized, transform.right);
            _targetLeanAngle += slideDir * movementLeanAmount * slideLeanMultiplier;
        }
        
        _targetLeanAngle = Mathf.Clamp(_targetLeanAngle, -maxLeanAngle, maxLeanAngle);
        _currentLeanAngle = Mathf.SmoothDamp(_currentLeanAngle, _targetLeanAngle, ref _leanVelocity, leanSmoothTime);
        
        transform.rotation = Quaternion.Euler(_eulerAngles);
        
        Quaternion swayRotation = Quaternion.Euler(_currentSwayRotation);
        Quaternion leanRotation = Quaternion.Euler(0f, 0f, _currentLeanAngle);
        
        Camera cam = mainCamera;
        if (cam != null && cam.transform != transform)
        {
            cam.transform.localRotation = swayRotation * leanRotation;
        }
        else
        {
            transform.rotation = transform.rotation * swayRotation * leanRotation;
        }
        
        _lastPosition = transform.position;
    }

    public void UpdatePosition(Transform target)
    {
        Vector3 targetPosition = target.position;
        Vector3 eyeOffset = new Vector3(0f, characterEyeHeight, 0f);
        transform.position = targetPosition + eyeOffset;
        
        if (_character != null)
        {
            var isGrounded = _character.IsGrounded();
            if (isGrounded && !_wasGrounded)
            {
                var verticalSpeed = Vector3.Dot(_character.GetVelocity(), Vector3.up);
                if (verticalSpeed < -5f)
                {
                    _impactFOVOffset = landingImpactFOVKick * Mathf.Abs(verticalSpeed / 20f);
                }
            }
            _wasGrounded = isGrounded;
        }
    }

    public void UpdateFOV()
    {
        if (mainCamera == null) return;
        
        float targetBaseFOV = _isAiming ? aimDownSightsFOV : baseFOV;
        
        var velocity = _character != null ? _character.GetVelocity() : Vector3.zero;
        var horizontalSpeed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
        var speedFactor = Mathf.InverseLerp(minSpeedForFOV, maxSpeedForFOV, horizontalSpeed);
        
        // If aiming, reduce or eliminate FOV increase from speed
        float fovIncrease = _isAiming ? maxFOVIncrease * 0.25f * speedFactor : maxFOVIncrease * speedFactor;
        var targetSpeedFOV = targetBaseFOV + fovIncrease;
        
        _impactFOVOffset = Mathf.Lerp(_impactFOVOffset, 0f, Time.deltaTime * impactRecoverySpeed);
        _targetFOV = targetSpeedFOV + _impactFOVOffset;
        
        _currentFOV = Mathf.Lerp(_currentFOV, _targetFOV, Time.deltaTime * fovLerpSpeed);
        mainCamera.fieldOfView = _currentFOV;
    }
    
    public void SetAiming(bool isAiming)
    {
        _isAiming = isAiming;
    }

    public Vector2 GetLookDelta()
    {
        return currentLookDelta;
    }

    public Vector2 GetMoveInput()
    {
        return currentMoveInput;
    }
}