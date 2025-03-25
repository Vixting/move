using System;
using UnityEngine;
using KinematicCharacterController;

public partial class PlayerCharacter : MonoBehaviour {
    
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
    
    public bool IsSliding() {
        return _state.Stance == Stance.Slide;
    }

    public Stance GetStance() {
        return _state.Stance;
    }
}