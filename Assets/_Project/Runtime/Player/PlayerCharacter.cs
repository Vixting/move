using System;
using UnityEngine;
using KinematicCharacterController;

public enum CrouchInput { None, Hold, Release }
public enum Stance { Stand, Crouch, Slide }

public struct CharacterState {
    public bool Grounded;
    public Stance Stance;
    public float SlideMomentum;
    public float CoyoteTime;
    public float JumpBuffer;
    public bool IsGroundPounding;
}

public struct CharacterInput {
    public Quaternion Rotation;
    public Vector2 Move;
    public bool Jump;
    public bool JumpSustain;
    public CrouchInput Crouch;
}

[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerCharacter : MonoBehaviour, ICharacterController {
    [SerializeField] private KinematicCharacterMotor motor;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform root;
    
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float crouchSpeed = 4f;
    [SerializeField] private float walkResponse = 30f;
    [SerializeField] private float crouchResponse = 25f;
    [SerializeField] private float jumpSpeed = 12f;
    [SerializeField] private float jumpUpGravity = -25f;
    [SerializeField] private float jumpDownGravity = -35f;
    [SerializeField] private float jumpApexThreshold = 2f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float airSpeed = 20f;
    [SerializeField] private float airAcceleration = 105f;
    [SerializeField] private float jumpEndEarlyGravityModifier = 3f;
    
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
    
    [SerializeField] private float groundPoundForce = -50f;
    [SerializeField] private float groundPoundRadius = 3f;
    [SerializeField] private float groundPoundUpwardForce = 15f;
    [SerializeField] private float groundPoundHorizontalForce = 10f;
    [SerializeField] private LayerMask groundPoundLayers;
    
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchHeightResponse = 18f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float standCameraHeight = 0.9f;
    [SerializeField] private float crouchCameraHeight = 0.3f;

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
    
    public void Initialize() {
        _state.Stance = Stance.Stand;
        motor.CharacterController = this;
        _overlapResults = new Collider[8];
        _timeSinceLastSlide = 0f;
    }

    public void UpdateInput(CharacterInput input) {
        _requestRotation = input.Rotation;
        _requestedMovement = new Vector3(input.Move.x, 0f, input.Move.y);
        _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
        _requestedMovement = input.Rotation * _requestedMovement;
        _requestedJump = _requestedJump || input.Jump;
        _requestedJumpSustainedJump = input.JumpSustain;
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
        return (_state.Grounded || _state.CoyoteTime > 0) && _state.JumpBuffer > 0;
    }

    private void HandleJump(ref Vector3 currentVelocity) {
        _state.JumpBuffer = 0;
        _state.CoyoteTime = 0;
        motor.ForceUnground(0f);
        
        var currentVerticalVelocity = Vector3.Dot(currentVelocity, motor.CharacterUp);
        var verticalVelocityChange = jumpSpeed - currentVerticalVelocity;
        currentVelocity += motor.CharacterUp * verticalVelocityChange;
        
        if (_state.Stance == Stance.Slide) {
            currentVelocity += _slideDirection * (_slideVelocity * 0.3f);
            currentVelocity += motor.CharacterUp * (jumpSpeed * 0.2f);
        }
    }

    private void HandleGroundPoundImpact() {
        var hitColliders = Physics.OverlapSphere(transform.position, groundPoundRadius, groundPoundLayers);
        
        foreach (var hit in hitColliders) {
            if (hit.TryGetComponent<Rigidbody>(out var rb)) {
                var direction = (rb.position - transform.position).normalized;
                var distanceFactor = 1f - (Vector3.Distance(transform.position, rb.position) / groundPoundRadius);
                var horizontalDir = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                var force = (Vector3.up * groundPoundUpwardForce + horizontalDir * groundPoundHorizontalForce) * distanceFactor;
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        var groundNormal = motor.GroundingStatus.GroundNormal;
        var isGrounded = motor.GroundingStatus.IsStableOnGround;
        var groundedMovement = isGrounded ? motor.GetDirectionTangentToSurface(_requestedMovement, groundNormal) * _requestedMovement.magnitude : Vector3.zero;
        var slopeAngle = isGrounded ? Vector3.Angle(groundNormal, motor.CharacterUp) : 0f;
        var downhillDirection = isGrounded ? Vector3.ProjectOnPlane(-motor.CharacterUp, groundNormal).normalized : Vector3.zero;
        
        if (_state.IsGroundPounding && isGrounded) {
            HandleGroundPoundImpact();
            _state.IsGroundPounding = false;
        }

        if (_requestedJump) {
            _state.JumpBuffer = jumpBufferTime;
        }
        _state.JumpBuffer = Mathf.Max(0, _state.JumpBuffer - deltaTime);
        
        if (!isGrounded) {
            _airTime += deltaTime;
            _state.CoyoteTime = Mathf.Max(0, _state.CoyoteTime - deltaTime);
        } else {
            _airTime = 0f;
            _state.CoyoteTime = coyoteTime;
        }

        if (isGrounded) {
            var moving = groundedMovement.sqrMagnitude > 0f;
            var crouching = _state.Stance is Stance.Crouch;
            var wasStanding = _lastState.Stance is Stance.Stand;
            var wasInAir = !_lastState.Grounded;
            var hasHorizontalSpeed = Vector3.ProjectOnPlane(_lastVelocity, motor.CharacterUp).magnitude > 1f;
            
            if (crouching && (wasStanding || (wasInAir && hasHorizontalSpeed))) {
                _state.Stance = Stance.Slide;
                var preservedSpeed = wasInAir ? _lastVelocity.magnitude : currentVelocity.magnitude;
                _slideVelocity = Mathf.Max(slideStartSpeed, preservedSpeed * slideBoostMultiplier);
                _slideDirection = wasInAir ? Vector3.ProjectOnPlane(_lastVelocity, groundNormal).normalized : motor.CharacterForward;
                _state.SlideMomentum = 1f;
                _timeSinceLastSlide = 0f;
            }

            if (_state.Stance is Stance.Stand or Stance.Crouch) {
                var speed = _state.Stance is Stance.Stand ? walkSpeed : crouchSpeed;
                var response = _state.Stance is Stance.Stand ? walkResponse : crouchResponse;
                var targetVelocity = groundedMovement * speed;
                
                if (_state.SlideMomentum > 0) {
                    targetVelocity += _slideDirection * (_slideVelocity * _state.SlideMomentum);
                    _state.SlideMomentum = Mathf.Max(0, _state.SlideMomentum - deltaTime);
                }
                
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1f - Mathf.Exp(-response * deltaTime)
                );
            }
            else if (_state.Stance is Stance.Slide) {
                if (_requestedMovement.sqrMagnitude > 0.01f) {
                    var steerDirection = motor.GetDirectionTangentToSurface(_requestedMovement, groundNormal);
                    _slideDirection = Vector3.Lerp(_slideDirection, steerDirection, slideSteerResponse * deltaTime);
                    _slideDirection = motor.GetDirectionTangentToSurface(_slideDirection, groundNormal).normalized;
                }

                if (slopeAngle > 0f) {
                    float slopeFactor = Mathf.Clamp01((slopeAngle - minSlideAngle) / (maxSlopeAngle - minSlideAngle));
                    var slopeDir = Vector3.Dot(downhillDirection, _slideDirection);
                    
                    if (slopeDir > 0) {
                        _slideVelocity += slopeAcceleration * slopeFactor * deltaTime;
                        currentVelocity += downhillDirection * (slopeFactor * slopeAcceleration * deltaTime);
                    } else {
                        _slideVelocity -= (upSlopeFriction * slopeFactor + slideFriction) * deltaTime;
                    }
                    
                    _slideVelocity = Mathf.Min(_slideVelocity, maxSlideSpeed);
                } else {
                    _slideVelocity -= slideFriction * deltaTime;
                }

                if (_slideVelocity < slideEndSpeed) {
                    _state.Stance = Stance.Crouch;
                    _state.SlideMomentum = slideMomentumRetention;
                }

                currentVelocity = _slideDirection * _slideVelocity;
            }
        }
        else {
            var preserveSlide = _state.Stance is Stance.Slide && _airTime < slideAirTime;
            
            if (_requestedCrouch && !_state.IsGroundPounding && !isGrounded) {
                _state.IsGroundPounding = true;
                _state.Stance = Stance.Crouch;
                currentVelocity = Vector3.zero;
            }
            
            if (_state.IsGroundPounding) {
                currentVelocity = motor.CharacterUp * groundPoundForce;
                return;
            }
            
            if (preserveSlide) {
                currentVelocity = _slideDirection * _slideVelocity;
            }
            
            if (_requestedMovement.sqrMagnitude > 0f && !preserveSlide) {
                var planarMovement = Vector3.ProjectOnPlane(_requestedMovement, motor.CharacterUp);
                var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                var movementForce = planarMovement * airAcceleration * deltaTime;
                var targetPlanarVelocity = currentPlanarVelocity + movementForce;
                var maxSpeed = Mathf.Max(airSpeed, currentPlanarVelocity.magnitude);
                targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, maxSpeed);
                var currentUpVelocity = Vector3.Dot(currentVelocity, motor.CharacterUp) * motor.CharacterUp;
                currentVelocity = targetPlanarVelocity + currentUpVelocity;
            }

            var currentVerticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
            var gravity = jumpUpGravity;

            if (currentVerticalSpeed < 0f) {
                gravity = jumpDownGravity;
            }
            else if (currentVerticalSpeed < jumpApexThreshold) {
                gravity = jumpDownGravity;
            }
            else if (!_requestedJumpSustainedJump && currentVerticalSpeed > 0f) {
                gravity *= jumpEndEarlyGravityModifier;
            }

            currentVelocity += motor.CharacterUp * gravity * deltaTime;
        }

        _lastVelocity = currentVelocity;

        if (CanJump()) {
            _requestedJump = false;
            HandleJump(ref currentVelocity);
        }
    }

    public void BeforeCharacterUpdate(float deltaTime) {
        _tempState = _state;
        if (_requestedCrouch && _state.Stance is Stance.Stand) {
            _state.Stance = Stance.Crouch;
            motor.SetCapsuleDimensions(motor.Capsule.radius, crouchHeight, crouchHeight * 0.5f);
        }
    }

    public void AfterCharacterUpdate(float deltaTime) {
        if (!_requestedCrouch && _state.Stance is not Stance.Stand) {
            motor.SetCapsuleDimensions(motor.Capsule.radius, standHeight, standHeight * 0.5f);
            var pos = motor.TransientPosition;
            var rot = motor.TransientRotation;
            
            if (motor.CharacterOverlap(pos, rot, _overlapResults, motor.CollidableLayers, QueryTriggerInteraction.Ignore) > 0) {
                motor.SetCapsuleDimensions(motor.Capsule.radius, crouchHeight, crouchHeight * 0.5f);
            }
            else {
                _state.Stance = Stance.Stand;
            }
        }
        
        _state.Grounded = motor.GroundingStatus.IsStableOnGround;
        _lastState = _tempState;
    }

    public void PostGroundingUpdate(float deltaTime) { }
    
    public bool IsColliderValidForCollisions(Collider coll) => coll && coll.enabled && !coll.isTrigger;
    
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }
    
    public Transform GetCameraTarget() => cameraTarget;
    
    public void SetPosition(Vector3 position, bool killvelocity = true) {
        motor.SetPosition(position);
        if (killvelocity) motor.BaseVelocity = Vector3.zero;
    }

    public bool IsGrounded() {
        return motor.GroundingStatus.IsStableOnGround;
    }

    public Vector3 GetVelocity() {
        return motor.BaseVelocity;
    }
    public bool IsSliding()
    {
        return _state.Stance == Stance.Slide;
    }

    public Stance GetStance()
    {
        return _state.Stance;
    }
}