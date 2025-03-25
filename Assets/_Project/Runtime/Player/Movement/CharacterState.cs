using System;
using UnityEngine;

public struct CharacterState {
    public bool Grounded;
    public Stance Stance;
    public float SlideMomentum;
    public float CoyoteTime;
    public float JumpBuffer;
    public float BhopWindow;
    public Vector3 BhopVelocity;
    public float KnockbackMomentum;
}