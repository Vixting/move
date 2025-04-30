using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieAI : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private float health = 100f;
    
    [Header("Perception")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float losePlayerRange = 20f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float fieldOfViewAngle = 120f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Transform eyePosition;
    
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.0f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private bool useRootMotion = false;
    [SerializeField] private float idleTimeMin = 2f;
    [SerializeField] private float idleTimeMax = 5f;
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float spawnPointRadius = 20f;
    [SerializeField] private Transform spawnPoint;
    
    [Header("Attack")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackAnimDuration = 0.8f;
    [SerializeField] private float animCrossFadeTime = 0.25f;
    [SerializeField] private float hitTriggerTime = 0.5f;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] dieSounds;
    
    [Header("Advanced")]
    [SerializeField] private float obstacleAvoidanceWeight = 0.5f;
    [SerializeField] private float pathRecalculationTime = 0.5f;
    [SerializeField] private bool usePathPrediction = true;
    [SerializeField] private float stunRecoveryTime = 1.5f;
    
    private enum ZombieState { Idle, Wander, Alert, Chase, Attack, Stunned, ReturnToSpawn, Dead }
    private ZombieState currentState;
    
    private Transform player;
    private Vector3 lastKnownPlayerPosition;
    private float nextAttackTime;
    private Vector3 wanderDestination;
    private float stateTimer;
    private float pathUpdateTimer;
    private Vector3 spawnPosition;
    private bool isAttacking;
    private AudioSource audioSource;
    private float stunEndTime;
    private float lastDetectionCheckTime;
    private const float DETECTION_CHECK_INTERVAL = 0.2f;
    private Vector3 predictedPlayerPosition;
    
    private const string ANIM_IDLE = "Z_Idle";
    private const string ANIM_IDLE_LOOK = "Z_Idle_Looking";
    private const string ANIM_ATTACK = "Z_Attack";
    private const string ANIM_ATTACK_ALT = "Z_Attack_Right";
    private const string ANIM_WALK1 = "Z_Walk1_InPlace";
    private const string ANIM_WALK2 = "Z_Walk_InPlace";
    private const string ANIM_RUN = "Z_Run_InPlace";
    private const string ANIM_STUNNED = "Z_Idle";
    private const string ANIM_ALERT = "Z_Idle_Looking";
    private const string ANIM_FALLING_BACK = "Z_FallingBack";
    private const string ANIM_FALLING_FORWARD = "Z_FallingForward";
    
    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (eyePosition == null) eyePosition = transform;
        
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        
        if (useRootMotion)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }
        
        SetupAgent();
        SetState(ZombieState.Idle);
        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
    }
    
    private void SetupAgent()
    {
        agent.speed = walkSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(40, 60);
    }
    
    private void Update()
    {
        if (currentState == ZombieState.Dead) return;
        
        if (Time.time >= lastDetectionCheckTime + DETECTION_CHECK_INTERVAL)
        {
            CheckForPlayer();
            lastDetectionCheckTime = Time.time;
        }
        
        UpdateState();
        
        if (useRootMotion && agent.enabled)
        {
            agent.nextPosition = transform.position;
        }
    }
    
    private void OnAnimatorMove()
    {
        if (useRootMotion && agent.enabled && animator.enabled)
        {
            Vector3 rootPosition = animator.rootPosition;
            rootPosition.y = agent.nextPosition.y;
            agent.nextPosition = rootPosition;
            
            transform.rotation = animator.rootRotation;
        }
    }
    
    private void CheckForPlayer()
    {
        if (player == null) return;
        
        bool wasPlayerDetected = false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            Vector3 directionToPlayer = (player.position - eyePosition.position).normalized;
            float angleToPlayer = Vector3.Angle(eyePosition.forward, directionToPlayer);
            
            if (angleToPlayer <= fieldOfViewAngle * 0.5f)
            {
                if (!Physics.Linecast(eyePosition.position, player.position, obstacleLayer))
                {
                    wasPlayerDetected = true;
                    lastKnownPlayerPosition = player.position;
                    
                    if (usePathPrediction)
                    {
                        PredictPlayerPosition();
                    }
                }
            }
        }
        
        if (wasPlayerDetected)
        {
            HandlePlayerDetection(distanceToPlayer);
        }
        else if (currentState == ZombieState.Chase || currentState == ZombieState.Attack)
        {
            if (distanceToPlayer > losePlayerRange)
            {
                LosePlayerTarget();
            }
        }
    }
    
    private void PredictPlayerPosition()
    {
        if (player.GetComponent<CharacterController>() != null)
        {
            Vector3 playerVelocity = player.GetComponent<CharacterController>().velocity;
            float distance = Vector3.Distance(transform.position, player.position);
            float predictionTime = distance / agent.speed;
            predictedPlayerPosition = player.position + playerVelocity * predictionTime * 0.5f;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(predictedPlayerPosition, out hit, 5f, NavMesh.AllAreas))
            {
                predictedPlayerPosition = hit.position;
            }
            else
            {
                predictedPlayerPosition = player.position;
            }
        }
        else
        {
            predictedPlayerPosition = player.position;
        }
    }
    
    private void HandlePlayerDetection(float distanceToPlayer)
    {
        switch (currentState)
        {
            case ZombieState.Idle:
            case ZombieState.Wander:
            case ZombieState.ReturnToSpawn:
                SetState(ZombieState.Alert);
                break;
                
            case ZombieState.Alert:
                SetState(ZombieState.Chase);
                break;
                
            case ZombieState.Chase:
                if (distanceToPlayer <= attackRange)
                {
                    SetState(ZombieState.Attack);
                }
                UpdateChaseDestination();
                break;
                
            case ZombieState.Attack:
                if (distanceToPlayer > attackRange)
                {
                    SetState(ZombieState.Chase);
                }
                else
                {
                    FaceTarget(player.position);
                }
                break;
                
            case ZombieState.Stunned:
                break;
        }
    }
    
    private void LosePlayerTarget()
    {
        // First move to the last known player position
        if (Vector3.Distance(transform.position, lastKnownPlayerPosition) > 1.5f)
        {
            agent.SetDestination(lastKnownPlayerPosition);
            SetState(ZombieState.Chase);
            stateTimer = Random.Range(5f, 10f); // Time to search the last known area
        }
        // After reaching last known position or timeout, return to spawn if too far
        else if (Vector3.Distance(transform.position, spawnPosition) > spawnPointRadius)
        {
            SetState(ZombieState.ReturnToSpawn);
        }
        // Otherwise go back to idle
        else
        {
            stateTimer = Random.Range(idleTimeMin, idleTimeMax);
            SetState(ZombieState.Idle);
        }
    }
    
    private void UpdateState()
    {
        stateTimer -= Time.deltaTime;
        pathUpdateTimer -= Time.deltaTime;
        
        switch (currentState)
        {
            case ZombieState.Idle:
                if (stateTimer <= 0)
                {
                    SetWanderDestination();
                    SetState(ZombieState.Wander);
                }
                break;
                
            case ZombieState.Wander:
                if (IsDestinationReached())
                {
                    stateTimer = Random.Range(idleTimeMin, idleTimeMax);
                    SetState(ZombieState.Idle);
                }
                else if (stateTimer <= 0)
                {
                    SetWanderDestination();
                    stateTimer = Random.Range(idleTimeMin * 2, idleTimeMax * 2);
                }
                break;
                
            case ZombieState.Alert:
                FaceTarget(lastKnownPlayerPosition);
                if (stateTimer <= 0)
                {
                    SetState(ZombieState.Chase);
                }
                break;
                
            case ZombieState.Chase:
                if (pathUpdateTimer <= 0)
                {
                    UpdateChaseDestination();
                    pathUpdateTimer = pathRecalculationTime;
                }
                
                // If we've reached the last known player position and player isn't visible
                if (IsDestinationReached() && Vector3.Distance(transform.position, lastKnownPlayerPosition) < 1f)
                {
                    // Search the area for a bit
                    if (stateTimer <= 0)
                    {
                        // After searching, return to normal behavior
                        if (Vector3.Distance(transform.position, spawnPosition) > spawnPointRadius)
                        {
                            SetState(ZombieState.ReturnToSpawn);
                        }
                        else
                        {
                            stateTimer = Random.Range(idleTimeMin, idleTimeMax);
                            SetState(ZombieState.Idle);
                        }
                    }
                    else if (Random.value < 0.1f)
                    {
                        // Occasionally pick a random nearby point to search while at last known position
                        Vector3 searchPoint = lastKnownPlayerPosition + Random.insideUnitSphere * 3f;
                        searchPoint.y = transform.position.y;
                        agent.SetDestination(searchPoint);
                    }
                }
                break;
                
            case ZombieState.Attack:
                if (!isAttacking && Time.time >= nextAttackTime)
                {
                    PerformAttack();
                }
                break;
                
            case ZombieState.Stunned:
                if (Time.time >= stunEndTime)
                {
                    SetState(ZombieState.Chase);
                }
                break;
                
            case ZombieState.ReturnToSpawn:
                if (pathUpdateTimer <= 0)
                {
                    agent.SetDestination(spawnPosition);
                    pathUpdateTimer = pathRecalculationTime * 2;
                }
                
                if (IsDestinationReached())
                {
                    stateTimer = Random.Range(idleTimeMin, idleTimeMax);
                    SetState(ZombieState.Idle);
                }
                break;
        }
    }
    
    private void UpdateChaseDestination()
    {
        if (player == null) return;
        
        Vector3 targetPosition = usePathPrediction ? predictedPlayerPosition : lastKnownPlayerPosition;
        agent.SetDestination(targetPosition);
    }
    
    private bool IsDestinationReached()
    {
        return !agent.pathPending && (agent.remainingDistance <= agent.stoppingDistance ||
               !agent.hasPath || agent.velocity.sqrMagnitude < 0.1f);
    }
    
    private void PerformAttack()
    {
        isAttacking = true;
        string attackAnim = Random.value > 0.5f ? ANIM_ATTACK : ANIM_ATTACK_ALT;
        PlayAnimation(attackAnim);
        
        if (attackSounds.Length > 0)
        {
            AudioClip clip = attackSounds[Random.Range(0, attackSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
        
        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(AttackSequence());
    }
    
    private IEnumerator AttackSequence()
    {
        yield return new WaitForSeconds(hitTriggerTime);
        OnAttackHit();
        
        yield return new WaitForSeconds(attackAnimDuration - hitTriggerTime);
        isAttacking = false;
        
        if (currentState == ZombieState.Attack)
        {
            if (player != null && Vector3.Distance(transform.position, player.position) > attackRange)
            {
                SetState(ZombieState.Chase);
            }
        }
    }
    
    public void OnAttackHit()
    {
        if (player == null || currentState == ZombieState.Dead) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange * 1.2f)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            
            if (angleToPlayer <= 60f)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage((int)attackDamage, transform.forward);
                }
                else
                {
                    // Fallback to old system if PlayerHealth component not found
                    IDamageable damageable = player.GetComponent<IDamageable>();
                    damageable?.TakeDamage(attackDamage, gameObject);
                }
            }
        }
    }
    
    private void SetWanderDestination()
    {
        NavMeshHit hit;
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection.y = 0;
        randomDirection += transform.position;
        
        int attempts = 0;
        bool destinationFound = false;
        
        while (!destinationFound && attempts < 30)
        {
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        wanderDestination = hit.position;
                        agent.SetDestination(wanderDestination);
                        destinationFound = true;
                    }
                }
            }
            
            if (!destinationFound)
            {
                randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection.y = 0;
                randomDirection += transform.position;
                attempts++;
            }
        }
        
        if (!destinationFound)
        {
            agent.ResetPath();
            stateTimer = Random.Range(idleTimeMin, idleTimeMax);
        }
    }
    
    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    
    private void SetState(ZombieState newState)
    {
        if (currentState == newState) return;
        
        ZombieState previousState = currentState;
        currentState = newState;
        
        switch (newState)
        {
            case ZombieState.Idle:
                agent.isStopped = true;
                agent.ResetPath();
                PlayAnimation(ANIM_IDLE);
                stateTimer = Random.Range(idleTimeMin, idleTimeMax);
                break;
                
            case ZombieState.Wander:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                PlayAnimation(Random.value > 0.5f ? ANIM_WALK1 : ANIM_WALK2);
                stateTimer = Random.Range(10f, 20f);
                break;
                
            case ZombieState.Alert:
                agent.isStopped = true;
                PlayAnimation(ANIM_ALERT);
                stateTimer = Random.Range(0.5f, 1.5f);
                break;
                
            case ZombieState.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                PlayAnimation(ANIM_RUN);
                break;
                
            case ZombieState.Attack:
                agent.isStopped = true;
                if (!isAttacking)
                {
                    PerformAttack();
                }
                break;
                
            case ZombieState.Stunned:
                agent.isStopped = true;
                PlayAnimation(ANIM_STUNNED);
                break;
                
            case ZombieState.ReturnToSpawn:
                agent.isStopped = false;
                agent.speed = walkSpeed;
                PlayAnimation(Random.value > 0.5f ? ANIM_WALK1 : ANIM_WALK2);
                agent.SetDestination(spawnPosition);
                break;
                
            case ZombieState.Dead:
                StopAllCoroutines();
                agent.isStopped = true;
                agent.enabled = false;
                
                Collider[] colliders = GetComponents<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = false;
                }
                
                PlayAnimation(Random.value > 0.5f ? ANIM_FALLING_BACK : ANIM_FALLING_FORWARD);
                
                if (dieSounds.Length > 0)
                {
                    AudioClip clip = dieSounds[Random.Range(0, dieSounds.Length)];
                    audioSource.PlayOneShot(clip);
                }
                
                Destroy(gameObject, 10f);
                break;
        }
    }
    
    private void PlayAnimation(string animName)
    {
        animator.CrossFade(animName, animCrossFadeTime);
    }
    
    public void TakeDamage(float amount, GameObject damageSource = null)
    {
        if (currentState == ZombieState.Dead) return;
        
        health -= amount;
        
        if (hurtSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            audioSource.PlayOneShot(clip);
        }
        
        if (health <= 0)
        {
            Die();
        }
        else
        {
            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack)
            {
                if (damageSource != null && damageSource.CompareTag("Player"))
                {
                    lastKnownPlayerPosition = damageSource.transform.position;
                    SetState(ZombieState.Chase);
                }
                else
                {
                    SetState(ZombieState.Alert);
                }
            }
            
            if (amount >= 30f && Random.value < 0.3f)
            {
                Stun(stunRecoveryTime);
            }
        }
    }
    
    public void Stun(float duration)
    {
        stunEndTime = Time.time + duration;
        SetState(ZombieState.Stunned);
    }
    
    public void Die()
    {
        SetState(ZombieState.Dead);
    }
    
    public void StabilizePosition()
    {
        if (agent != null && agent.enabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (eyePosition != null)
        {
            Gizmos.color = Color.blue;
            Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, Vector3.up) * eyePosition.forward;
            Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, Vector3.up) * eyePosition.forward;
            
            Gizmos.DrawRay(eyePosition.position, fovLine1 * detectionRange);
            Gizmos.DrawRay(eyePosition.position, fovLine2 * detectionRange);
        }
        
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, spawnPointRadius);
        }
    }
    
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject damager = null);
    }
}