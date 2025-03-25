using System;
using UnityEngine;
using KinematicCharacterController;

public partial class PlayerCharacter : ICharacterController {
    
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
}