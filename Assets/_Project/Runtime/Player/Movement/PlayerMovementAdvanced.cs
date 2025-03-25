using System;
using UnityEngine;
using KinematicCharacterController;

public partial class PlayerCharacter : MonoBehaviour {
    
    private bool TryAirDash(ref Vector3 currentVelocity) {
        if (!enableAirDash || _hasAirDashed || _airDashCooldownRemaining > 0 || _state.Grounded) {
            return false;
        }
        
        Vector3 dashDirection;
        if (_requestedMovement.sqrMagnitude > 0.1f) {
            dashDirection = _requestedMovement.normalized;
        } else {
            dashDirection = motor.CharacterForward;
        }
        
        Vector3 horizontalDash = Vector3.ProjectOnPlane(dashDirection, motor.CharacterUp) * airDashForce;
        currentVelocity = horizontalDash + Vector3.Dot(currentVelocity, motor.CharacterUp) * motor.CharacterUp;
        
        _hasAirDashed = true;
        _airDashCooldownRemaining = airDashCooldown;
        
        return true;
    }
    
    private bool CheckWallSlide(ref Vector3 currentVelocity) {
        if (!enableWallJump || _state.Grounded) {
            _isWallSliding = false;
            return false;
        }
        
        // More comprehensive wall detection with multiple raycasts
        RaycastHit hit;
        Vector3 forward = motor.CharacterForward;
        Vector3 right = Vector3.Cross(motor.CharacterUp, forward).normalized;
        Vector3 characterCenter = motor.TransientPosition + motor.CharacterUp * (motor.Capsule.height * 0.5f);
        float checkDistance = motor.Capsule.radius * 1.8f;
        int layerMask = motor.CollidableLayers;
        
        // Check more directions, including slightly up and down the wall
        bool hitWall = false;
        
        // Front direction
        if (Physics.Raycast(characterCenter, forward, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore)) {
            hitWall = true;
        }
        // Forward-right diagonal
        else if (Physics.Raycast(characterCenter, Vector3.Lerp(forward, right, 0.5f).normalized, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore)) {
            hitWall = true;
        }
        // Forward-left diagonal
        else if (Physics.Raycast(characterCenter, Vector3.Lerp(forward, -right, 0.5f).normalized, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore)) {
            hitWall = true;
        }
        // Right direction (for strafing against walls)
        else if (Physics.Raycast(characterCenter, right, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore)) {
            hitWall = true;
        }
        // Left direction (for strafing against walls)
        else if (Physics.Raycast(characterCenter, -right, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore)) {
            hitWall = true;
        }
        
        // Check above and below for better vertical wall detection
        if (!hitWall) {
            Vector3 upCheck = characterCenter + motor.CharacterUp * (motor.Capsule.height * 0.3f);
            Vector3 downCheck = characterCenter - motor.CharacterUp * (motor.Capsule.height * 0.3f);
            
            // Up checks
            if (Physics.Raycast(upCheck, forward, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(upCheck, Vector3.Lerp(forward, right, 0.5f).normalized, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore) ||
                Physics.Raycast(upCheck, Vector3.Lerp(forward, -right, 0.5f).normalized, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore)) {
                hitWall = true;
            }
            // Down checks
            else if (Physics.Raycast(downCheck, forward, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore) ||
                     Physics.Raycast(downCheck, Vector3.Lerp(forward, right, 0.5f).normalized, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore) ||
                     Physics.Raycast(downCheck, Vector3.Lerp(forward, -right, 0.5f).normalized, out hit, checkDistance, layerMask, QueryTriggerInteraction.Ignore)) {
                hitWall = true;
            }
        }
        
        if (hitWall) {
            // Validate wall normal - must be mostly horizontal
            float wallVerticalAlignment = Vector3.Dot(hit.normal, motor.CharacterUp);
            if (Mathf.Abs(wallVerticalAlignment) > 0.3f) {
                // Too horizontal to be a proper wall
                _isWallSliding = false;
                return false;
            }
            
            _lastWallNormal = hit.normal;
            
            // Check if moving toward the wall
            float approachDot = Vector3.Dot(currentVelocity.normalized, -hit.normal);
            
            // More forgiving approach angle check
            if (approachDot > 0.1f) {
                _isWallSliding = true;
                
                // Calculate sliding direction along the wall
                Vector3 wallTangent = Vector3.Cross(hit.normal, motor.CharacterUp).normalized;
                Vector3 moveDirection = _requestedMovement.normalized;
                float wallDirAlignment = Vector3.Dot(moveDirection, wallTangent);
                
                // Determine sliding direction based on input
                Vector3 slideDir = wallTangent * Mathf.Sign(wallDirAlignment);
                float slideSpeed = Mathf.Max(3f, Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp).magnitude * 0.6f);
                
                // Allow some input control while wall sliding
                Vector3 horizontalVel = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                
                // Calculate final sliding velocity
                Vector3 wallSlideVel = slideDir * slideSpeed * Mathf.Abs(wallDirAlignment);
                
                // More gradual slowdown for smoother wall sliding
                float downwardSpeed = wallSlidingSpeed * (1.0f - Mathf.Max(0f, Vector3.Dot(moveDirection, hit.normal)));
                
                // Apply wall sliding velocity
                currentVelocity = wallSlideVel * 0.8f - motor.CharacterUp * downwardSpeed;
                
                return true;
            }
        }
        
        _isWallSliding = false;
        return false;
    }
    
    private void CheckRampBoost(ref Vector3 currentVelocity) {
        if (!enableRampSliding || !_state.Grounded) {
            return;
        }
        
        float slopeAngle = Vector3.Angle(motor.GroundingStatus.GroundNormal, motor.CharacterUp);
        
        if (slopeAngle > minSlideAngle && slopeAngle < maxSlopeAngle) {
            Vector3 slopeDir = Vector3.ProjectOnPlane(-motor.CharacterUp, motor.GroundingStatus.GroundNormal).normalized;
            float alignmentWithSlope = Vector3.Dot(motor.CharacterForward, slopeDir);
            
            if (alignmentWithSlope > 0.7f && _state.Stance == Stance.Crouch) {
                _rampBoostTimer = 0.5f;
                
                if (_state.Stance != Stance.Slide) {
                    _state.Stance = Stance.Slide;
                    _slideDirection = slopeDir;
                    _slideVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp).magnitude * 1.2f;
                }
            }
        }
    }
    
    public bool IsWallSliding() {
        return _isWallSliding;
    }
    
    public Vector3 GetWallNormal() {
        return _lastWallNormal;
    }
    
    public float GetWallSlideTime() {
        return _wallSlideTimer;
    }
    
    public bool CanAirDash() {
        return !_hasAirDashed && _airDashCooldownRemaining <= 0 && !_state.Grounded;
    }
    
    public float GetAirDashCooldown() {
        return _airDashCooldownRemaining;
    }
    
    public bool HasRampBoost() {
        return _rampBoostTimer > 0;
    }
}