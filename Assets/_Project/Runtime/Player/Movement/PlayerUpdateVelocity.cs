using System;
using UnityEngine;
using KinematicCharacterController;

public partial class PlayerCharacter : MonoBehaviour {
    
    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        var groundNormal = motor.GroundingStatus.GroundNormal;
        var isGrounded = motor.GroundingStatus.IsStableOnGround;
        var groundedMovement = isGrounded ? motor.GetDirectionTangentToSurface(_requestedMovement, groundNormal) * _requestedMovement.magnitude : Vector3.zero;
        var slopeAngle = isGrounded ? Vector3.Angle(groundNormal, motor.CharacterUp) : 0f;
        var downhillDirection = isGrounded ? Vector3.ProjectOnPlane(-motor.CharacterUp, groundNormal).normalized : Vector3.zero;
        
        bool airDashed = false;
        if (_requestedMovement.sqrMagnitude > 0.9f && _requestedCrouch) {
            airDashed = TryAirDash(ref currentVelocity);
        }
        
        bool wallSliding = false;
        if (!airDashed) {
            wallSliding = CheckWallSlide(ref currentVelocity);
        }
        
        if (!airDashed && !wallSliding) {
            CheckRampBoost(ref currentVelocity);
        }
        
        if (_knockbackTimeRemaining > 0 && _hasKnockbackVelocity) {
            Vector3 combinedKnockback = _horizontalKnockbackVelocity + _verticalKnockbackVelocity;
            float knockbackFalloff = 1.0f - (knockbackRecoveryRate * deltaTime);
            
            _horizontalKnockbackVelocity *= knockbackFalloff;
            _verticalKnockbackVelocity *= knockbackFalloff;
            
            currentVelocity += combinedKnockback * ((isGrounded) ? 0.7f : 1.0f);
            
            if (combinedKnockback.magnitude > 5f) {
                groundedMovement *= 0.5f;
            }
        }

        if (_requestedJump) {
            _state.JumpBuffer = jumpBufferTime;
        }
        _state.JumpBuffer = Mathf.Max(0, _state.JumpBuffer - deltaTime);
        
        bool wasGrounded = _state.Grounded;
        
        if (!isGrounded) {
            _airTime += deltaTime;
            _state.CoyoteTime = Mathf.Max(0, _state.CoyoteTime - deltaTime);
        } else {
            if (!wasGrounded) {
                float landingSpeed = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp).magnitude;
                
                if (enableBunnyHopping) {
                    _state.BhopWindow = bhopWindowTime;
                    
                    Vector3 landingHorizontalVel = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                    _state.BhopVelocity = landingHorizontalVel;
                    
                    if (_requestedJumpSustainedJump && landingSpeed > walkSpeed) {
                        _state.JumpBuffer = 0.05f;
                    }
                    
                    if (_hasKnockbackVelocity) {
                        Vector3 landingKnockback = Vector3.ProjectOnPlane(_horizontalKnockbackVelocity, motor.CharacterUp);
                        if (landingKnockback.magnitude > 0) {
                            _state.KnockbackMomentum = Mathf.Max(_state.KnockbackMomentum, knockbackMomentumRetention);
                        }
                    }
                }
            }
            
            _airTime = 0f;
            _state.CoyoteTime = coyoteTime;
        }

        if (isGrounded) {
            HandleGroundedMovement(ref currentVelocity, groundedMovement, groundNormal, slopeAngle, downhillDirection, deltaTime);
        }
        else {
            HandleAirborneMovement(ref currentVelocity, airDashed, wallSliding, deltaTime);
        }

        _lastVelocity = currentVelocity;

        if (CanJump()) {
            _requestedJump = false;
            HandleJump(ref currentVelocity);
        }
    }
    
    private void HandleGroundedMovement(ref Vector3 currentVelocity, Vector3 groundedMovement, Vector3 groundNormal, float slopeAngle, Vector3 downhillDirection, float deltaTime) {
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
            
            if (_state.KnockbackMomentum > 0 && _state.BhopWindow > 0) {
                Vector3 knockbackDir = _horizontalKnockbackVelocity.normalized;
                if (knockbackDir.magnitude > 0.1f) {
                    float currentSpeed = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp).magnitude;
                    targetVelocity += knockbackDir * (currentSpeed * _state.KnockbackMomentum * 0.5f);
                }
            }
            
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            float speed2D = horizontalVelocity.magnitude;
            
            if (speed2D > 0.1f) {
                float frictionFactor = _state.BhopWindow > 0 ? surfSlipperiness : groundFriction;
                float frictionAmount = speed2D * frictionFactor * deltaTime;
                
                if (frictionAmount > speed2D) {
                    frictionAmount = speed2D;
                }
                
                if (speed2D > 0) {
                    horizontalVelocity *= (speed2D - frictionAmount) / speed2D;
                }
            }
            
            Vector3 verticalVelocity = Vector3.Dot(currentVelocity, motor.CharacterUp) * motor.CharacterUp;
            currentVelocity = horizontalVelocity + verticalVelocity;
            
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
    
    private void HandleAirborneMovement(ref Vector3 currentVelocity, bool airDashed, bool wallSliding, float deltaTime) {
        var preserveSlide = _state.Stance is Stance.Slide && _airTime < slideAirTime;
        
        if (preserveSlide) {
            currentVelocity = _slideDirection * _slideVelocity;
        }
        
        if (_requestedMovement.sqrMagnitude > 0f && !preserveSlide && !wallSliding && !airDashed) {
            Vector3 horizontalVel = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            float currentSpeed = horizontalVel.magnitude;
            
            if (currentSpeed > 0) {
                float friction = 1f - (airFriction * 0.5f * deltaTime);
                horizontalVel *= friction;
            }
            
            Vector3 wishDir = Vector3.ProjectOnPlane(_requestedMovement, motor.CharacterUp).normalized;
            Vector3 currentDir = currentSpeed > 0.1f ? horizontalVel.normalized : wishDir;
            
            Vector3 strafeDir = Vector3.Cross(motor.CharacterUp, currentDir).normalized;
            float strafeComponent = Vector3.Dot(wishDir, strafeDir);
            
            float forwardComponent = Vector3.Dot(wishDir, currentDir);
            
            float airControl = airControlMultiplier * 2.5f;
            float strafeControl = airControlMultiplier * 3.5f;
            
            if (Mathf.Abs(strafeComponent) > 0.2f) {
                Vector3 strafePush = strafeDir * strafeComponent;
                float strafePower = Mathf.Lerp(strafeControl, strafeControl * 1.5f, 
                                              Mathf.Abs(strafeComponent));
                
                horizontalVel += strafePush * (airAcceleration * strafePower * deltaTime);
                
                if (currentSpeed < maxAirSpeed * 1.5f && Mathf.Abs(strafeComponent) > 0.7f) {
                    float speedBoost = 1.0f + (0.04f * Mathf.Abs(strafeComponent));
                    horizontalVel *= speedBoost;
                }
            }
            
            if (forwardComponent > 0) {
                float accelPower = airControl * forwardComponent;
                float addSpeed = airAcceleration * accelPower * deltaTime;
                
                if (currentSpeed > maxAirSpeed) {
                    float speedFactor = Mathf.Max(0.1f, 1.0f - (currentSpeed - maxAirSpeed) / (maxAirSpeed * 0.5f));
                    addSpeed *= speedFactor;
                }
                
                horizontalVel += wishDir * addSpeed;
            }
            
            if (currentSpeed < airSpeed * 0.5f) {
                horizontalVel += wishDir * (airAcceleration * deltaTime * 0.5f);
            }
            
            Vector3 verticalComponent = Vector3.Dot(currentVelocity, motor.CharacterUp) * motor.CharacterUp;
            currentVelocity = horizontalVel + verticalComponent;
        } else if (!preserveSlide && !wallSliding && !airDashed) {
            Vector3 horizontalVel = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
            float currentSpeed = horizontalVel.magnitude;
            
            if (currentSpeed > 0) {
                float friction = 1f - (airFriction * 0.5f * deltaTime);
                horizontalVel *= friction;
                
                Vector3 verticalComponent = Vector3.Dot(currentVelocity, motor.CharacterUp) * motor.CharacterUp;
                currentVelocity = horizontalVel + verticalComponent;
            }
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

        if (wallSliding) {
            gravity *= 0.4f;
            
            // Allow additional control during wall running
            if (_requestedMovement.sqrMagnitude > 0.1f) {
                // Apply slight force in direction of input along the wall
                Vector3 wallTangent = Vector3.Cross(_lastWallNormal, motor.CharacterUp).normalized;
                float inputAlongWall = Vector3.Dot(_requestedMovement.normalized, wallTangent);
                
                // Add additional horizontal control
                currentVelocity += wallTangent * (inputAlongWall * wallStickiness * airAcceleration * deltaTime);
            }
        }

        currentVelocity += motor.CharacterUp * gravity * deltaTime;
    }
}