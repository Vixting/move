using UnityEngine;
using System.Collections;

public class WeaponHolder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerCharacter playerCharacter;
    
    [Header("Aim Settings")]
    [SerializeField] private float aimSmoothing = 8f;
    [SerializeField] private Vector3 aimOffset = Vector3.zero;
    [SerializeField] private bool followAimDirection = true;
    [SerializeField] private float verticalPositionMultiplier = 0.02f;
    
    [Header("Position Settings")]
    [SerializeField] private Vector3 hipPosition = new Vector3(0.2f, -0.15f, 0.4f);
    [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.06f, 0.2f);
    [SerializeField] private float positionSmoothing = 12f;
    
    [Header("Bobbing Settings")]
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private float crouchBobMultiplier = 0.5f;
    [SerializeField] private float sprintBobMultiplier = 2f;
    [SerializeField] private bool enableBobbing = true;
    
    [Header("Recoil Settings")]
    [SerializeField] private float recoilAmount = 0.1f;
    [SerializeField] private float recoilRecoverySpeed = 5f;
    [SerializeField] private float horizontalRecoilVariance = 0.3f;
    
    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float maxSwayAmount = 0.06f;
    [SerializeField] private float swaySmoothing = 8f;
    [SerializeField] private bool enableSway = true;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool enableManualRotation = true;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float rotationSmoothing = 10f;
    [SerializeField] private Vector3 hipRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 adsRotationOffset = Vector3.zero;
    
    [Header("Weapon Animation")]
    [SerializeField] private float drawDuration = 0.5f;
    [SerializeField] private Vector3 drawStartPosition = new Vector3(0.1f, -0.3f, 0.1f);
    [SerializeField] private float holsterDuration = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private bool isAiming = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float bobTimer = 0f;
    private Vector3 bobOffset = Vector3.zero;
    private Vector3 recoilOffset = Vector3.zero;
    private Vector3 swayOffset = Vector3.zero;
    private Vector2 previousLookInput = Vector2.zero;
    
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    
    private bool isDrawing = false;
    private bool isHolstering = false;
    
    private Vector3 currentRotationOffset = Vector3.zero;
    private Vector3 targetRotationOffset = Vector3.zero;
    
    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        targetPosition = hipPosition;
        currentRotationOffset = hipRotationOffset;
    }
    
    public void Initialize(Transform camera, PlayerCharacter character = null)
    {
        cameraTransform = camera;
        playerCharacter = character;
        
        if (debugMode)
        {
            Debug.Log($"WeaponHolder initialized with camera: {(camera != null ? camera.name : "NULL")}, " +
                      $"character: {(character != null ? character.name : "NULL")}");
        }
        
        StartCoroutine(DrawWeapon());
    }
    
    private void Update()
    {
        if (cameraTransform == null) return;
        if (isHolstering) return;
        
        if (followAimDirection && !isDrawing)
        {
            UpdateAimDirection();
        }
        
        UpdatePosition();
        
        if (enableBobbing && !isDrawing)
        {
            UpdateBobbing();
        }
        
        if (enableSway && !isDrawing)
        {
            UpdateSway();
        }
        
        UpdateRecoil();
        
        if (enableManualRotation && !isDrawing)
        {
            UpdateManualRotation();
        }
    }
    
    private void UpdateAimDirection()
    {
        Vector3 forwardDir = cameraTransform.forward;
        Vector3 upDir = cameraTransform.up;
        
        targetRotation = Quaternion.LookRotation(forwardDir, upDir);
        
        Vector3 finalRotationOffset = currentRotationOffset;
        Quaternion offsetRotation = Quaternion.Euler(finalRotationOffset);
        Quaternion finalRotation = targetRotation * offsetRotation;
        
        float currentAimSmoothing = aimSmoothing;
        
        float cameraPitch = cameraTransform.eulerAngles.x;
        if (cameraPitch > 180f) cameraPitch -= 360f;
        
        if (Mathf.Abs(cameraPitch) > 30f)
        {
            currentAimSmoothing = aimSmoothing * 1.5f;
        }
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            Time.deltaTime * currentAimSmoothing
        );
    }
    
    private void UpdateManualRotation()
    {
        currentRotationOffset = Vector3.Lerp(
            currentRotationOffset,
            targetRotationOffset,
            Time.deltaTime * rotationSmoothing
        );
    }
    
    private void UpdatePosition()
    {
        Vector3 targetPos = isAiming ? adsPosition : hipPosition;
        
        Vector3 pitchAdjustment = Vector3.zero;
        if (cameraTransform != null)
        {
            float cameraPitch = cameraTransform.eulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
            
            if (cameraPitch < -10f)
            {
                pitchAdjustment.z = Mathf.Abs(cameraPitch) * 0.003f;
                pitchAdjustment.y = -cameraPitch * 0.001f;
            }
            else
            {
                pitchAdjustment.y = -cameraPitch * verticalPositionMultiplier * 0.5f;
            }
        }
        
        Vector3 combinedOffset = bobOffset + recoilOffset + swayOffset + aimOffset + pitchAdjustment;
        
        if (isDrawing)
        {
            return;
        }
        
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos + combinedOffset,
            Time.deltaTime * positionSmoothing
        );
    }
    
    private void UpdateBobbing()
    {
        if (playerCharacter == null) return;
        
        Vector3 velocity = playerCharacter.GetVelocity();
        float horizontalSpeed = Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude;
        
        if (horizontalSpeed > 0.1f)
        {
            float bobbingMultiplier = 1f;
            
            if (playerCharacter.IsSliding())
            {
                bobbingMultiplier = sprintBobMultiplier;
            }
            else if (playerCharacter.GetStance() == Stance.Crouch)
            {
                bobbingMultiplier = crouchBobMultiplier;
            }
            
            float speedFactor = Mathf.Clamp01(horizontalSpeed / 5f);
            bobTimer += Time.deltaTime * bobSpeed * speedFactor;
            
            float xBob = Mathf.Sin(bobTimer) * bobAmount * bobbingMultiplier;
            float yBob = Mathf.Sin(bobTimer * 2) * bobAmount * 0.5f * bobbingMultiplier;
            
            bobOffset = new Vector3(xBob, yBob, 0);
        }
        else
        {
            bobOffset = Vector3.Lerp(bobOffset, Vector3.zero, Time.deltaTime * 5f);
        }
    }
    
    private void UpdateSway()
    {
        Vector2 lookInput = Vector2.zero;
        
        if (cameraTransform.GetComponent<PlayerCamera>() != null)
        {
            lookInput = cameraTransform.GetComponent<PlayerCamera>().GetLookDelta();
        }
        
        Vector2 lookDelta = lookInput - previousLookInput;
        previousLookInput = lookInput;
        
        float xSway = -lookDelta.x * swayAmount;
        float ySway = -lookDelta.y * swayAmount;
        
        xSway = Mathf.Clamp(xSway, -maxSwayAmount, maxSwayAmount);
        ySway = Mathf.Clamp(ySway, -maxSwayAmount, maxSwayAmount);
        
        Vector3 targetSway = new Vector3(xSway, ySway, 0);
        
        swayOffset = Vector3.Lerp(swayOffset, targetSway, Time.deltaTime * swaySmoothing);
    }
    
    private void UpdateRecoil()
    {
        if (recoilOffset.magnitude > 0.001f)
        {
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
        }
        else
        {
            recoilOffset = Vector3.zero;
        }
    }
    
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
        targetPosition = aiming ? adsPosition : hipPosition;
        targetRotationOffset = aiming ? adsRotationOffset : hipRotationOffset;
    }
    
    public void AddRecoil(float amount = -1f)
    {
        if (amount < 0) amount = recoilAmount;
        
        float horizontalRecoil = Random.Range(-horizontalRecoilVariance, horizontalRecoilVariance);
        recoilOffset = new Vector3(horizontalRecoil, -amount, 0);
    }
    
    private IEnumerator DrawWeapon()
    {
        isDrawing = true;
        
        transform.localPosition = drawStartPosition;
        
        float elapsed = 0f;
        while (elapsed < drawDuration)
        {
            float t = elapsed / drawDuration;
            float smoothT = t * t * (3f - 2f * t);
            
            transform.localPosition = Vector3.Lerp(drawStartPosition, hipPosition, smoothT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = hipPosition;
        isDrawing = false;
    }
    
    public IEnumerator HolsterWeapon()
    {
        isHolstering = true;
        
        Vector3 startPos = transform.localPosition;
        
        float elapsed = 0f;
        while (elapsed < holsterDuration)
        {
            float t = elapsed / holsterDuration;
            float smoothT = t * t * (3f - 2f * t);
            
            transform.localPosition = Vector3.Lerp(startPos, drawStartPosition, smoothT);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = drawStartPosition;
        isHolstering = false;
    }
    
    public void Reset()
    {
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        bobTimer = 0f;
        bobOffset = Vector3.zero;
        recoilOffset = Vector3.zero;
        swayOffset = Vector3.zero;
        isAiming = false;
        isDrawing = false;
        isHolstering = false;
        currentRotationOffset = hipRotationOffset;
        targetRotationOffset = hipRotationOffset;
    }
    
    public bool IsHolstering()
    {
        return isHolstering;
    }
    
    public bool IsDrawing()
    {
        return isDrawing;
    }
    
    public void RotateWeapon(Vector3 rotationDelta)
    {
        if (!enableManualRotation) return;
        
        Vector3 scaledDelta = rotationDelta * rotationSpeed * Time.deltaTime;
        targetRotationOffset += scaledDelta;
    }
    
    public void SetWeaponRotation(Vector3 rotation, bool instant = false)
    {
        if (!enableManualRotation) return;
        
        targetRotationOffset = rotation;
        
        if (instant)
        {
            currentRotationOffset = rotation;
        }
    }
    
    public void ResetWeaponRotation(bool instant = false)
    {
        Vector3 defaultRotation = isAiming ? adsRotationOffset : hipRotationOffset;
        SetWeaponRotation(defaultRotation, instant);
    }
    
    public void SetPositionPresets(Vector3 hipPos, Vector3 adsPos)
    {
        hipPosition = hipPos;
        adsPosition = adsPos;
        targetPosition = isAiming ? adsPosition : hipPosition;
    }
    
    public void SetRotationPresets(Vector3 hipRot, Vector3 adsRot)
    {
        hipRotationOffset = hipRot;
        adsRotationOffset = adsRot;
        targetRotationOffset = isAiming ? adsRotationOffset : hipRotationOffset;
        
        if (!isDrawing && !isHolstering)
        {
            currentRotationOffset = targetRotationOffset;
        }
    }
    
    public void SetRecoilSettings(float amount, float horizontalVariance, float recoverySpeed)
    {
        recoilAmount = amount;
        horizontalRecoilVariance = horizontalVariance;
        recoilRecoverySpeed = recoverySpeed;
    }
    
    public Vector3 GetCurrentRotationOffset()
    {
        return currentRotationOffset;
    }
    
    public Vector3 GetTargetRotationOffset()
    {
        return targetRotationOffset;
    }
    
    public void SetManualRotationEnabled(bool enabled)
    {
        enableManualRotation = enabled;
    }
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    public void SetRotationSmoothing(float smoothing)
    {
        rotationSmoothing = Mathf.Max(0.1f, smoothing);
    }
}