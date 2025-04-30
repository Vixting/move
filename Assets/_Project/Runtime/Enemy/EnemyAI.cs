using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using InventorySystem;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    TakeCover,
    Investigate,
    Stunned,
    Dead
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyCharacter))]
public class EnemyAI : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private EnemyCharacter characterData;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponData weaponData;
    
    [Header("Perception")]
    [SerializeField] private float sightRange = 20f;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fieldOfView = 120f;
    [SerializeField] private float hearingRange = 10f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float investigationDuration = 5f;

    [Header("Combat Settings")]
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float burstFireDuration = 0.5f;
    [SerializeField] private int burstShotCount = 3;
    [SerializeField] private float accuracy = 0.8f;
    [SerializeField] private bool shouldTakeCover = true;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private bool useMeleeAttacks = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] alertSounds;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioSource audioSource;

    private EnemyState currentState = EnemyState.Idle;
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPosition;
    private int currentPatrolIndex = 0;
    private float stateChangeTimer = 0f;
    private float nextAttackTime = 0f;
    private Vector3 investigatePosition;
    private Weapon currentWeapon;
    private EnemyWeaponIntegration weaponIntegration;
    private float currentAccuracyModifier = 1f;
    private bool isPlayerDetected = false;
    private float originalStoppingDistance;
    private float nextStateChangeTime = 0f;
    private Coroutine currentBurstCoroutine;
    
    private const float MIN_TIME_BETWEEN_STATES = 0.5f;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (characterData == null) characterData = GetComponent<EnemyCharacter>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        originalStoppingDistance = agent.stoppingDistance;
        
        if (headTransform == null)
        {
            Debug.LogWarning("Head transform not assigned, using this transform as head");
            headTransform = transform;
        }
        
        InitializeWeapon();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
        }
    }

    private void Start()
    {
        playerTransform = FindObjectOfType<Player>()?.transform;
        SetState(EnemyState.Idle);
        
        StartCoroutine(PerformBehaviorTree());
        
        if (characterData != null)
        {
            characterData.OnDeath += () => SetState(EnemyState.Dead);
        }
    }

    private void InitializeWeapon()
    {
        if (weaponData == null || weaponHolder == null)
        {
            Debug.LogWarning("No weapon assigned to enemy or weapon holder is missing");
            return;
        }

        if (weaponData.weaponPrefab != null)
        {
            GameObject weaponObj = Instantiate(weaponData.weaponPrefab, weaponHolder);
            weaponObj.transform.localPosition = Vector3.zero;
            weaponObj.transform.localRotation = Quaternion.identity;
            
            currentWeapon = weaponObj.GetComponent<Weapon>();
            if (currentWeapon != null)
            {
                currentWeapon.Initialize(weaponData, null, obstacleLayer);
            }
            else
            {
                Debug.LogError("Failed to get Weapon component from prefab");
            }
            
            weaponIntegration = weaponObj.GetComponent<EnemyWeaponIntegration>();
            if (weaponIntegration == null)
            {
                weaponIntegration = weaponObj.AddComponent<EnemyWeaponIntegration>();
            }
        }
    }

    private IEnumerator PerformBehaviorTree()
    {
        while (true)
        {
            if (characterData != null && characterData.IsDead())
            {
                SetState(EnemyState.Dead);
                yield break;
            }

            switch (currentState)
            {
                case EnemyState.Idle:
                    UpdateIdleState();
                    break;
                    
                case EnemyState.Patrol:
                    UpdatePatrolState();
                    break;
                    
                case EnemyState.Investigate:
                    UpdateInvestigateState();
                    break;
                    
                case EnemyState.Chase:
                    UpdateChaseState();
                    break;
                    
                case EnemyState.Attack:
                    UpdateAttackState();
                    break;
                    
                case EnemyState.TakeCover:
                    UpdateTakeCoverState();
                    break;
                    
                case EnemyState.Stunned:
                    break;
                    
                case EnemyState.Dead:
                    HandleDeadState();
                    break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Update()
    {
        DetectPlayer();
        UpdateAnimator();
    }
    
    private void SetState(EnemyState newState)
    {
        if (newState == currentState || Time.time < nextStateChangeTime)
            return;
            
        nextStateChangeTime = Time.time + MIN_TIME_BETWEEN_STATES;
        
        Debug.Log($"{gameObject.name} changing state from {currentState} to {newState}");
        
        switch (currentState)
        {
            case EnemyState.Attack:
                if (currentBurstCoroutine != null)
                {
                    StopCoroutine(currentBurstCoroutine);
                    currentBurstCoroutine = null;
                }
                if (currentWeapon != null)
                    currentWeapon.OnFire(false);
                break;
        }
        
        EnemyState oldState = currentState;
        currentState = newState;
        
        switch (newState)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                agent.stoppingDistance = originalStoppingDistance;
                break;
                
            case EnemyState.Patrol:
                agent.isStopped = false;
                agent.stoppingDistance = originalStoppingDistance;
                
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    if (currentPatrolIndex >= patrolPoints.Length)
                        currentPatrolIndex = 0;
                        
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                break;
                
            case EnemyState.Investigate:
                agent.isStopped = false;
                agent.stoppingDistance = 1f;
                stateChangeTimer = investigationDuration;
                agent.SetDestination(investigatePosition);
                
                PlaySound(alertSounds);
                break;
                
            case EnemyState.Chase:
                agent.isStopped = false;
                agent.stoppingDistance = attackRange * 0.7f;
                
                PlaySound(alertSounds);
                break;
                
            case EnemyState.Attack:
                agent.isStopped = true;
                break;
                
            case EnemyState.TakeCover:
                agent.isStopped = false;
                agent.stoppingDistance = 0.5f;
                FindCoverPosition();
                break;
                
            case EnemyState.Stunned:
                agent.isStopped = true;
                break;
                
            case EnemyState.Dead:
                agent.isStopped = true;
                if (currentWeapon != null)
                    currentWeapon.OnFire(false);
                
                if (currentWeapon != null && currentWeapon.gameObject != null)
                    currentWeapon.gameObject.SetActive(false);
                break;
        }
        
        if (animator != null)
        {
            animator.SetInteger("State", (int)currentState);
        }
    }
    
    private void UpdateIdleState()
    {
        stateChangeTimer -= Time.deltaTime;
        
        if (isPlayerDetected)
        {
            if (IsPlayerInAttackRange())
            {
                SetState(EnemyState.Attack);
            }
            else
            {
                SetState(EnemyState.Chase);
            }
        }
        else if (stateChangeTimer <= 0)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                SetState(EnemyState.Patrol);
            }
            stateChangeTimer = Random.Range(2f, 5f);
        }
    }
    
    private void UpdatePatrolState()
    {
        if (isPlayerDetected)
        {
            if (IsPlayerInAttackRange())
            {
                SetState(EnemyState.Attack);
            }
            else
            {
                SetState(EnemyState.Chase);
            }
            return;
        }
        
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
            {
                stateChangeTimer -= Time.deltaTime;
                
                if (stateChangeTimer <= 0)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    stateChangeTimer = patrolWaitTime;
                }
            }
        }
        else
        {
            SetState(EnemyState.Idle);
        }
    }
    
    private void UpdateInvestigateState()
    {
        if (isPlayerDetected)
        {
            if (IsPlayerInAttackRange())
            {
                SetState(EnemyState.Attack);
            }
            else
            {
                SetState(EnemyState.Chase);
            }
            return;
        }
        
        stateChangeTimer -= Time.deltaTime;
        
        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            transform.Rotate(0, Time.deltaTime * 45, 0);
        }
        
        if (stateChangeTimer <= 0)
        {
            SetState(EnemyState.Patrol);
        }
    }
    
    private void UpdateChaseState()
    {
        if (!isPlayerDetected)
        {
            stateChangeTimer = investigationDuration;
            investigatePosition = lastKnownPlayerPosition;
            SetState(EnemyState.Investigate);
            return;
        }
        
        if (IsPlayerInAttackRange())
        {
            SetState(EnemyState.Attack);
            return;
        }
        
        if (useMeleeAttacks && IsPlayerInMeleeRange())
        {
            PerformMeleeAttack();
        }
        
        agent.SetDestination(playerTransform.position);
        lastKnownPlayerPosition = playerTransform.position;
    }
    
    private void UpdateAttackState()
    {
        if (!isPlayerDetected)
        {
            investigatePosition = lastKnownPlayerPosition;
            SetState(EnemyState.Investigate);
            return;
        }
        
        if (!IsPlayerInAttackRange())
        {
            SetState(EnemyState.Chase);
            return;
        }
        
        if (useMeleeAttacks && IsPlayerInMeleeRange())
        {
            PerformMeleeAttack();
            return;
        }
        
        lastKnownPlayerPosition = playerTransform.position;
        
        Vector3 targetDirection = (playerTransform.position - headTransform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        
        if (Time.time >= nextAttackTime)
        {
            currentAccuracyModifier = Random.Range(1f - (1f - accuracy), 1f);
            
            bool shouldFindCover = false;
            
            if (shouldTakeCover && characterData != null)
            {
                float healthPercent = characterData.GetHealthPercent();
                if (healthPercent <= lowHealthThreshold)
                {
                    shouldFindCover = Random.value > healthPercent;
                }
                else
                {
                    shouldFindCover = Random.value > 0.7f;
                }
            }
            
            if (shouldFindCover)
            {
                SetState(EnemyState.TakeCover);
                return;
            }
            
            if (currentBurstCoroutine != null)
            {
                StopCoroutine(currentBurstCoroutine);
            }
            
            PlaySound(attackSounds);
            currentBurstCoroutine = StartCoroutine(FireBurst());
            nextAttackTime = Time.time + attackRate + Random.Range(-0.3f, 0.5f);
        }
    }
    
    private void UpdateTakeCoverState()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (isPlayerDetected && IsPlayerInAttackRange())
            {
                SetState(EnemyState.Attack);
            }
            else if (isPlayerDetected)
            {
                SetState(EnemyState.Chase);
            }
            else
            {
                investigatePosition = lastKnownPlayerPosition;
                SetState(EnemyState.Investigate);
            }
        }
    }
    
    private void HandleDeadState()
    {
        if (agent != null) agent.isStopped = true;
        
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
        {
            rb.isKinematic = false;
        }
        
        this.enabled = false;
    }
    
    private IEnumerator FireBurst()
    {
        if (currentWeapon == null && weaponIntegration == null)
            yield break;
            
        if (currentWeapon != null)
        {
            currentWeapon.OnFire(true);
        }
        
        if (weaponIntegration != null)
        {
            Vector3 targetPosition = CalculateTargetPosition();
            weaponIntegration.Fire(true, targetPosition);
        }
        
        for (int i = 0; i < burstShotCount; i++)
        {
            if (currentState != EnemyState.Attack || (characterData != null && characterData.IsDead()))
            {
                if (currentWeapon != null)
                    currentWeapon.OnFire(false);
                yield break;
            }
            
            AimAtPlayer();
            
            yield return new WaitForSeconds(burstFireDuration / burstShotCount);
        }
        
        if (currentWeapon != null)
        {
            currentWeapon.OnFire(false);
        }
    }
    
    private Vector3 CalculateTargetPosition()
    {
        if (playerTransform == null || headTransform == null) 
            return Vector3.zero;
        
        Vector3 directionToPlayer = (playerTransform.position - headTransform.position).normalized;
        
        if (currentAccuracyModifier < 1f)
        {
            float spreadX = Random.Range(-0.1f, 0.1f) * (1f - currentAccuracyModifier);
            float spreadY = Random.Range(-0.1f, 0.1f) * (1f - currentAccuracyModifier);
            
            directionToPlayer = Quaternion.Euler(spreadY, spreadX, 0) * directionToPlayer;
        }
        
        return headTransform.position + directionToPlayer * 50f;
    }
    
    private void AimAtPlayer()
    {
        if (playerTransform == null || headTransform == null) return;
        
        Vector3 directionToPlayer = (playerTransform.position - headTransform.position).normalized;
        
        if (currentAccuracyModifier < 1f)
        {
            float spreadX = Random.Range(-0.1f, 0.1f) * (1f - currentAccuracyModifier);
            float spreadY = Random.Range(-0.1f, 0.1f) * (1f - currentAccuracyModifier);
            
            directionToPlayer = Quaternion.Euler(spreadY, spreadX, 0) * directionToPlayer;
        }
        
        headTransform.rotation = Quaternion.LookRotation(directionToPlayer);
        
        if (weaponIntegration != null)
        {
            Vector3 targetPosition = headTransform.position + directionToPlayer * 50f;
            weaponIntegration.Fire(true, targetPosition);
        }
    }
    
    private void PerformMeleeAttack()
    {
        if (characterData == null || playerTransform == null) return;
        
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        
        characterData.PerformMeleeAttack(playerTransform, playerLayer);
        
        nextAttackTime = Time.time + 2f;
    }
    
    private void DetectPlayer()
    {
        isPlayerDetected = false;
        
        if (playerTransform == null || (characterData != null && characterData.IsDead()))
            return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= sightRange)
        {
            Vector3 directionToPlayer = (playerTransform.position - headTransform.position).normalized;
            float angleToPlayer = Vector3.Angle(headTransform.forward, directionToPlayer);
            
            if (angleToPlayer <= fieldOfView * 0.5f)
            {
                if (!Physics.Raycast(headTransform.position, directionToPlayer, distanceToPlayer, obstacleLayer))
                {
                    isPlayerDetected = true;
                    lastKnownPlayerPosition = playerTransform.position;
                }
            }
        }
        
        if (distanceToPlayer <= hearingRange)
        {
            float hearingProbability = 1f - (distanceToPlayer / hearingRange);
            if (Random.value < hearingProbability * 0.3f)
            {
                investigatePosition = playerTransform.position;
                if (currentState == EnemyState.Idle || currentState == EnemyState.Patrol)
                {
                    SetState(EnemyState.Investigate);
                }
            }
        }
    }
    
    private bool IsPlayerInAttackRange()
    {
        if (playerTransform == null)
            return false;
            
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        return distanceToPlayer <= attackRange;
    }
    
    private bool IsPlayerInMeleeRange()
    {
        if (playerTransform == null)
            return false;
            
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        return distanceToPlayer <= meleeRange;
    }
    
    private void FindCoverPosition()
    {
        if (playerTransform == null) return;
        
        Vector3 directionFromPlayer = (transform.position - playerTransform.position).normalized;
        Vector3 coverPos = transform.position + directionFromPlayer * 5f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(coverPos, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    private void UpdateAnimator()
    {
        if (animator == null) return;
        
        animator.SetFloat("Speed", agent.velocity.magnitude);
        animator.SetBool("IsPlayerDetected", isPlayerDetected);
        
        if (characterData != null)
        {
            animator.SetFloat("HealthPercent", characterData.GetHealthPercent());
        }
    }
    
    private void PlaySound(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (characterData != null && characterData.IsDead()) return;
        
        if (!isPlayerDetected)
        {
            investigatePosition = transform.position + hitDirection * 5f;
            SetState(EnemyState.Investigate);
        }
        
        float stunChance = Mathf.Clamp01(damage / 50f);
        if (Random.value < stunChance)
        {
            StartCoroutine(ApplyStun(stunChance));
        }
    }
    
    public IEnumerator ApplyStun(float stunAmount)
    {
        SetState(EnemyState.Stunned);
        yield return new WaitForSeconds(stunAmount * 2f);
        
        if (currentState == EnemyState.Stunned && characterData != null && !characterData.IsDead())
        {
            if (isPlayerDetected)
            {
                if (IsPlayerInAttackRange())
                {
                    SetState(EnemyState.Attack);
                }
                else
                {
                    SetState(EnemyState.Chase);
                }
            }
            else
            {
                SetState(EnemyState.Investigate);
            }
        }
    }
    
    public void AlertToPosition(Vector3 position)
    {
        if (currentState == EnemyState.Dead || currentState == EnemyState.Stunned) return;
            
        investigatePosition = position;
        
        if (currentState == EnemyState.Idle || currentState == EnemyState.Patrol)
        {
            SetState(EnemyState.Investigate);
        }
    }
    
    public void AlertToPlayer(Transform player)
    {
        if (currentState == EnemyState.Dead || currentState == EnemyState.Stunned) return;
            
        if (player != null)
        {
            playerTransform = player;
            isPlayerDetected = true;
            lastKnownPlayerPosition = player.position;
            
            if (IsPlayerInAttackRange())
            {
                SetState(EnemyState.Attack);
            }
            else
            {
                SetState(EnemyState.Chase);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, meleeRange);
        
        if (headTransform != null)
        {
            Gizmos.color = Color.white;
            float halfFOV = fieldOfView * 0.5f;
            Vector3 leftRayDirection = Quaternion.Euler(0, -halfFOV, 0) * headTransform.forward;
            Vector3 rightRayDirection = Quaternion.Euler(0, halfFOV, 0) * headTransform.forward;
            Gizmos.DrawRay(headTransform.position, leftRayDirection * sightRange);
            Gizmos.DrawRay(headTransform.position, rightRayDirection * sightRange);
        }
    }
}