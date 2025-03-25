using System;
using UnityEngine;
using KinematicCharacterController;

[RequireComponent(typeof(KinematicCharacterMotor))]
public partial class PlayerCharacter : MonoBehaviour, ICharacterController {
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;
    
    [Header("Basic Movement")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float crouchSpeed = 4f;
    [SerializeField] private float walkResponse = 30f;
    [SerializeField] private float crouchResponse = 25f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float jumpUpGravity = -38f;
    [SerializeField] private float jumpDownGravity = -45f;
    [SerializeField] private float jumpApexThreshold = 1f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float airSpeed = 7f;
    [SerializeField] private float airAcceleration = 85f;
    [SerializeField] private float jumpEndEarlyGravityModifier = 3f;
    
    [Header("Sliding")]
    [SerializeField] private float slideStartSpeed = 30f;
    [SerializeField] private float slideEndSpeed = 12f;
    [SerializeField] private float slideFriction = 15f;
    [SerializeField] private float upSlopeFriction = 25f;
    [SerializeField] private float slideSteerResponse = 3.5f;
    [SerializeField] private float maxSlopeAngle = 50f;
    [SerializeField] private float minSlideAngle = 5f;
    [SerializeField] private float slopeAcceleration = 35f;
    [SerializeField] private float slideAirTime = 0.35f;
    [SerializeField] private float slideBoostMultiplier = 1.5f;
    [SerializeField] private float slideMomentumRetention = 0.8f;
    [SerializeField] private float maxSlideSpeed = 45f;
    
    [Header("Character Dimensions")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 18f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float standCameraHeight = 0.9f;
    [SerializeField] private float crouchCameraHeight = 0.3f;
    
    [Header("Knockback")]
    [SerializeField] private float knockbackRecoveryRate = 10f;
    [SerializeField] private float maxKnockbackTime = 0.5f;
    [SerializeField] private float knockbackMomentumRetention = 0.8f;
    
    [Header("Bunny Hopping")]
    [SerializeField] private bool enableBunnyHopping = true;
    [SerializeField] private float bhopMomentumRetention = 0.98f;
    [SerializeField] private float bhopBoostMultiplier = 1.08f;
    [SerializeField] private float bhopWindowTime = 0.15f;
    [SerializeField] private float airFriction = 0.05f;
    [SerializeField] private float groundFriction = 0.7f;
    [SerializeField] private float surfSlipperiness = 0.2f;
    [SerializeField] private float airControlMultiplier = 0.5f;
    [SerializeField] private float speedAccumulationFactor = 1.2f;
    [SerializeField] private float maxAirSpeed = 14f;
    
    [Header("Advanced Movement")]
    [SerializeField] private bool enableAirDash = true;
    [SerializeField] private float airDashForce = 20f;
    [SerializeField] private float airDashCooldown = 0.8f;
    [SerializeField] private bool enableWallJump = true;
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallJumpUpwardsForce = 8f;
    [SerializeField] private float wallSlidingSpeed = 3f;
    [SerializeField] private float wallStickiness = 0.4f; // How much character "sticks" to walls
    [SerializeField] private float wallRunSpeed = 8f; // Speed for running along walls
    [SerializeField] private float wallCheckDistance = 1.2f; // Distance to check for walls
    [SerializeField] private float wallRunVerticalLimit = 0.3f; // Limit for what counts as a vertical wall
    [SerializeField] private bool enableRampSliding = true;
    [SerializeField] private float rampSlidingBoost = 1.3f;
    [SerializeField] private bool enableCrouchJump = true;
    [SerializeField] private float crouchJumpBoost = 1.25f;

    private CharacterState _state;
    private CharacterState _lastState;
    private CharacterState _tempState;
    private Vector3 _requestedMovement;
    private Quaternion _requestRotation;
    private bool _requestedJump;
    private bool _requestedJumpSustainedJump;
    private bool _requestedCrouch;
    private Collider[] _overlapResults;
    private Vector3 _slideDirection;
    private float _slideVelocity;
    private float _airTime;
    private Vector3 _lastVelocity;
    private float _timeSinceLastSlide;
    
    private Vector3 _horizontalKnockbackVelocity = Vector3.zero;
    private Vector3 _verticalKnockbackVelocity = Vector3.zero;
    private float _knockbackTimeRemaining = 0f;
    private bool _hasKnockbackVelocity = false;
    
    private float _airDashCooldownRemaining = 0f;
    private bool _hasAirDashed = false;
    private Vector3 _lastWallNormal = Vector3.zero;
    private float _wallSlideTimer = 0f;
    private bool _isWallSliding = false;
    private float _rampBoostTimer = 0f;
    private bool _isCrouchJumping = false;
    
    public void Initialize() {
        _state.Stance = Stance.Stand;
        motor.CharacterController = this;
        _overlapResults = new Collider[8];
        _timeSinceLastSlide = 0f;
        _airDashCooldownRemaining = 0f;
        _hasAirDashed = false;
        _wallSlideTimer = 0f;
        _isWallSliding = false;
    }
    
    public void ApplyKnockback(Vector3 direction, float force) {
        _horizontalKnockbackVelocity = direction.normalized * force;
        _verticalKnockbackVelocity = Vector3.zero;
        _knockbackTimeRemaining = maxKnockbackTime;
        _hasKnockbackVelocity = true;
        _state.KnockbackMomentum = 1.0f;
        
        if (_state.Grounded && force > 10f) {
            motor.ForceUnground(0.1f);
        }
    }
    
    public void ApplyDirectionalKnockback(Vector3 horizontalForce, Vector3 verticalForce) {
        _horizontalKnockbackVelocity = horizontalForce;
        _verticalKnockbackVelocity = verticalForce;
        _knockbackTimeRemaining = maxKnockbackTime;
        _hasKnockbackVelocity = true;
        _state.KnockbackMomentum = 1.0f;
        
        float totalForce = horizontalForce.magnitude + Mathf.Abs(verticalForce.y);
        if (_state.Grounded && totalForce > 10f) {
            motor.ForceUnground(0.1f);
        }
    }

    public void UpdateInput(CharacterInput input) {
        _requestRotation = input.Rotation;
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;
        _requestedJump = _requestedJump || input.Jump;
        _requestedJumpSustainedJump = input.JumpSustain;
        
        if (input.Crouch == CrouchInput.Hold && _state.Grounded && _requestedJump) {
            _isCrouchJumping = true;
        }
        
        _requestedCrouch = input.Crouch switch {
            CrouchInput.Hold => true,
            CrouchInput.Release => false,
            CrouchInput.None => _requestedCrouch,
            _ => _requestedCrouch
        };
    }

    public void UpdateBody(float deltaTime) {
        if (!root || !cameraTarget) return;
        
        var currentHeight = motor.Capsule.height;
        var normalizedHeight = currentHeight / standHeight;
        var cameraTargetHeight = currentHeight * (_state.Stance is Stance.Stand ? standCameraHeight : crouchCameraHeight);
        var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);
        
        cameraTarget.localPosition = Vector3.Lerp(
            cameraTarget.localPosition,
            new Vector3(0f, cameraTargetHeight, 0f),
            1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
        
        root.localScale = Vector3.Lerp(
            root.localScale,
            rootTargetScale,
            1f - Mathf.Exp(-crouchHeightResponse * deltaTime)
        );
        
        _timeSinceLastSlide += deltaTime;
        
        if (_knockbackTimeRemaining > 0) {
            _knockbackTimeRemaining -= deltaTime;
            if (_knockbackTimeRemaining <= 0) {
                _knockbackTimeRemaining = 0;
                _horizontalKnockbackVelocity = Vector3.zero;
                _verticalKnockbackVelocity = Vector3.zero;
                _hasKnockbackVelocity = false;
            }
        }
        
        if (_state.KnockbackMomentum > 0) {
            _state.KnockbackMomentum = Mathf.Max(0, _state.KnockbackMomentum - deltaTime * (1 - knockbackMomentumRetention));
        }
        
        if (_state.BhopWindow > 0) {
            _state.BhopWindow -= deltaTime * 1.2f;
        }
        
        if (_airDashCooldownRemaining > 0) {
            _airDashCooldownRemaining -= deltaTime;
        }
        
        if (_state.Grounded) {
            _hasAirDashed = false;
        }
        
        if (_isWallSliding) {
            _wallSlideTimer += deltaTime;
        } else {
            _wallSlideTimer = 0f;
        }
        
        if (_rampBoostTimer > 0) {
            _rampBoostTimer -= deltaTime;
        }
        
        if (_state.Grounded) {
            _isCrouchJumping = false;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        var forward = Vector3.ProjectOnPlane(_requestRotation * Vector3.forward, motor.CharacterUp).normalized;
        if (forward.sqrMagnitude > 0.001f) {
            var targetRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
            float rotationSpeed = _state.Stance == Stance.Slide ? slideSteerResponse : 15f;
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationSpeed * deltaTime);
        }
    }

    private bool CanJump() {
        bool standardJump = (_state.Grounded || _state.CoyoteTime > 0 || (_state.BhopWindow > 0 && enableBunnyHopping)) && _state.JumpBuffer > 0;
        bool wallJump = enableWallJump && !_state.Grounded && _isWallSliding && _state.JumpBuffer > 0;
        
        return standardJump || wallJump;
    }

    private void HandleJump(ref Vector3 currentVelocity) {
        _state.JumpBuffer = 0;
        _state.CoyoteTime = 0;
        motor.ForceUnground(0f);
        
        if (enableWallJump && _isWallSliding) {
            Vector3 jumpDir = _lastWallNormal.normalized;
            
            // Calculate wall jump direction based on input and wall normal
            Vector3 inputDir = _requestedMovement.sqrMagnitude > 0.1f ? _requestedMovement.normalized : motor.CharacterForward;
            Vector3 alongWallDir = Vector3.Cross(_lastWallNormal, motor.CharacterUp).normalized;
            float inputAlongWall = Vector3.Dot(inputDir, alongWallDir);
            
            // Combine wall normal with along-wall direction based on input
            Vector3 wallJumpDir = (jumpDir + alongWallDir * Mathf.Sign(inputAlongWall) * 0.5f).normalized;
            
            // Create the wall jump velocity with horizontal and vertical components
            Vector3 wallJumpVelocity = wallJumpDir * wallJumpForce + motor.CharacterUp * wallJumpUpwardsForce;
            
            // Add a bit of the current horizontal velocity for smoother transitions
            Vector3 horizontalVel = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            float preservedSpeed = horizontalVel.magnitude * 0.3f;
            
            // Combine everything for final wall jump velocity
            currentVelocity = wallJumpVelocity + horizontalVel.normalized * preservedSpeed;
            
            // Reset wall sliding state
            _isWallSliding = false;
            _wallSlideTimer = 0f;
            return;
        }
        
        if (enableCrouchJump && _state.Stance == Stance.Crouch && _state.Grounded) {
            _isCrouchJumping = true;
        }
        
        if (_state.BhopWindow > 0 && enableBunnyHopping) {
            Vector3 horizInput = Vector3.ProjectOnPlane(_requestedMovement, motor.CharacterUp).normalized;
            Vector3 horizVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            float currentSpeed = horizVelocity.magnitude;
            
            if (currentSpeed > 0) {
                float timingBonus = 1.0f + (1.0f - (_state.BhopWindow / bhopWindowTime)) * 0.05f;
                float boostMultiplier = bhopBoostMultiplier * timingBonus;
                
                if (_isCrouchJumping && enableCrouchJump) {
                    boostMultiplier *= crouchJumpBoost;
                }
                
                if (_rampBoostTimer > 0 && enableRampSliding) {
                    boostMultiplier *= rampSlidingBoost;
                }
                
                if (currentSpeed > walkSpeed * 1.5f) {
                    float speedRatio = (currentSpeed - walkSpeed * 1.5f) / (maxAirSpeed * 2.5f);
                    boostMultiplier = Mathf.Lerp(boostMultiplier, 1.0f, speedRatio * 0.7f);
                }
                
                Vector3 newDirection;
                if (_requestedMovement.sqrMagnitude > 0.1f) {
                    float inputInfluence = Mathf.Lerp(0.3f, 0.1f, Mathf.Min(1.0f, currentSpeed / (walkSpeed * 2)));
                    newDirection = Vector3.Lerp(horizVelocity.normalized, horizInput, inputInfluence).normalized;
                } else {
                    newDirection = horizVelocity.normalized;
                }
                
                float newSpeed = Mathf.Min(currentSpeed * boostMultiplier, maxAirSpeed * 2.3f);
                
                if (_state.KnockbackMomentum > 0) {
                    newSpeed *= (1.0f + _state.KnockbackMomentum * 0.25f);
                }
                
                Vector3 newHorizVelocity = newDirection * newSpeed;
                
                float hopFactor = 1.0f;
                if (currentSpeed > walkSpeed * 1.5f) {
                    hopFactor = 0.95f;
                }
                
                Vector3 newVertVelocity = jumpSpeed * hopFactor * motor.CharacterUp;
                
                if (_hasKnockbackVelocity && _verticalKnockbackVelocity.y > 0) {
                    newVertVelocity += _verticalKnockbackVelocity * _state.KnockbackMomentum;
                }
                
                currentVelocity = newHorizVelocity + newVertVelocity;
                _state.BhopWindow = 0;
                return;
            }
        }
        
        float jumpMultiplier = 1.0f;
        if (_isCrouchJumping && enableCrouchJump) {
            jumpMultiplier = crouchJumpBoost;
        }
        
        var currentVerticalVelocity = Vector3.Dot(currentVelocity, motor.CharacterUp);
        var verticalVelocityChange = jumpSpeed * jumpMultiplier - currentVerticalVelocity;
        
        if (_hasKnockbackVelocity && _verticalKnockbackVelocity.y > 0) {
            verticalVelocityChange += _verticalKnockbackVelocity.y * _state.KnockbackMomentum;
        }
        
        currentVelocity += motor.CharacterUp * verticalVelocityChange;
        
        if (_state.Stance == Stance.Slide) {
            currentVelocity += _slideDirection * (_slideVelocity * 0.3f);
            currentVelocity += motor.CharacterUp * (jumpSpeed * 0.2f);
        }
        
        if (enableBunnyHopping) {
            _state.BhopVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
        }
    }
}