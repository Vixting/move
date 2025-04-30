using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using InventorySystem;

public class EnemyCharacter : Character
{
    [Header("Enemy Settings")]
    [SerializeField] private bool isArmored = false;
    [SerializeField] private float armorDamageReduction = 0.3f;
    [SerializeField] private float headShotMultiplier = 2.0f;
    [SerializeField] private float meleeAttackDamage = 20f;
    [SerializeField] private float meleeAttackRadius = 1.5f;
    [SerializeField] private float meleeAttackCooldown = 2f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem bloodEffect;
    [SerializeField] private GameObject ragdollPrefab;
    [SerializeField] private Transform hitEffectPoint;
    
    [Header("Audio")]
    [SerializeField] private AudioClip[] hurtSounds;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Loot")]
    [SerializeField] private GameObject[] possibleLoot;
    [SerializeField] private float lootDropChance = 0.3f;
    
    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float, Vector3, Vector3> onDamageReceived;
    public UnityEvent onMeleeAttack;
    
    private EnemyAI enemyAI;
    private Animator animator;
    private Collider mainCollider;
    private float lastMeleeAttackTime;
    private int hitCount = 0;
    private bool isStaggered = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        currentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();
        animator = GetComponentInChildren<Animator>();
        mainCollider = GetComponent<Collider>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
        }
        
        if (hitEffectPoint == null)
        {
            hitEffectPoint = transform;
        }
        
        // Subscribe to death event
        OnDeath += HandleDeath;
    }
    
    private void Start()
    {
        // Additional initialization if needed
    }
    
    public override void TakeDamage(float damage)
    {
        // Forward to the more detailed method with default parameters
        TakeDamageExtended(damage, transform.position, -transform.forward);
    }
    
    public void TakeDamageExtended(float damage, Vector3 hitPoint, Vector3 hitDirection, bool isHeadshot = false)
    {
        if (IsDead()) return;
        
        // Apply headshot multiplier if applicable
        if (isHeadshot)
        {
            damage *= headShotMultiplier;
        }
        
        // Apply armor reduction if armored
        if (isArmored)
        {
            damage *= (1f - armorDamageReduction);
        }
        
        // Increment hit counter for potential stagger
        hitCount++;
        
        // Spawn hit effects
        SpawnHitEffects(hitPoint, hitDirection);
        
        // Play hurt sound
        PlayHurtSound();
        
        // Call base class damage method
        base.TakeDamage(damage);
        
        // Invoke damage event
        onDamageReceived?.Invoke(damage, hitPoint, hitDirection);
        
        // Inform the AI about being hit
        if (enemyAI != null)
        {
            enemyAI.TakeDamage(damage, hitPoint, hitDirection);
        }
        
        // Handle stagger check
        CheckForStagger(damage);
        
        // Trigger hit animation if not dead
        if (!IsDead() && animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }
    
    private void SpawnHitEffects(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (bloodEffect != null)
        {
            ParticleSystem blood = Instantiate(bloodEffect, hitPoint, Quaternion.LookRotation(hitDirection));
            blood.Play();
            Destroy(blood.gameObject, 2f);
        }
    }
    
    private void PlayHurtSound()
    {
        if (hurtSounds != null && hurtSounds.Length > 0 && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSounds[Random.Range(0, hurtSounds.Length)]);
        }
    }
    
    private void CheckForStagger(float damage)
    {
        // Potential stagger based on damage
        if (!isStaggered && (damage > maxHealth * 0.2f || hitCount >= 3))
        {
            StartCoroutine(ApplyStagger());
        }
    }
    
    private IEnumerator ApplyStagger()
    {
        isStaggered = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Stagger");
        }
        
        // Inform AI
        if (enemyAI != null)
        {
            enemyAI.StartCoroutine(enemyAI.ApplyStun(1.0f));
        }
        
        // Reset hit counter
        hitCount = 0;
        
        yield return new WaitForSeconds(1.0f);
        
        isStaggered = false;
    }
    
    private void HandleDeath()
    {
        if (IsDead())
        {
            // Play death sound
            if (deathSounds != null && deathSounds.Length > 0 && audioSource != null)
            {
                audioSource.PlayOneShot(deathSounds[Random.Range(0, deathSounds.Length)]);
            }
            
            // Trigger death animation if applicable
            if (animator != null)
            {
                animator.SetTrigger("Death");
                animator.enabled = false; // Disable after triggering death animation
            }
            
            // Disable AI component
            if (enemyAI != null)
            {
                enemyAI.enabled = false;
            }
            
            // Invoke custom death event
            onDeath?.Invoke();
            
            // Handle ragdoll
            HandleRagdoll();
            
            // Drop loot
            DropLoot();
            
            // Schedule destruction
            if (ragdollPrefab == null)
            {
                Destroy(gameObject, 10f);
            }
        }
    }
    
    private void HandleRagdoll()
    {
        // Spawn ragdoll if available
        if (ragdollPrefab != null)
        {
            GameObject ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
            
            // Apply random force to ragdoll in appropriate direction
            Rigidbody[] rbs = ragdoll.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rbs)
            {
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = Mathf.Abs(randomDirection.y); // Make sure it goes somewhat upward
                rb.AddForce(randomDirection * 500f, ForceMode.Impulse);
            }
            
            // Disable this enemy's renderers but keep the GameObject active for a bit
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }
            
            // Disable colliders
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Schedule destruction
            Destroy(gameObject, 2f);
        }
    }
    
    private void DropLoot()
    {
        // Drop loot with random chance
        if (possibleLoot.Length > 0 && Random.value <= lootDropChance)
        {
            GameObject loot = possibleLoot[Random.Range(0, possibleLoot.Length)];
            if (loot != null)
            {
                Instantiate(loot, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }
        }
    }
    
    public void PerformMeleeAttack(Transform target, LayerMask targetLayers)
    {
        if (IsDead() || Time.time - lastMeleeAttackTime < meleeAttackCooldown)
            return;
        
        lastMeleeAttackTime = Time.time;
        
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Play attack sound
        if (attackSounds != null && attackSounds.Length > 0 && audioSource != null)
        {
            audioSource.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)]);
        }
        
        // Send event
        onMeleeAttack?.Invoke();
        
        // Perform actual attack after a small delay to match animation
        StartCoroutine(DelayedMeleeAttack(targetLayers));
    }
    
    private IEnumerator DelayedMeleeAttack(LayerMask targetLayers)
    {
        // Wait for animation to reach attack point (adjust based on your animation)
        yield return new WaitForSeconds(0.3f);
        
        // Perform overlap sphere check for targets
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * meleeAttackRadius * 0.5f, 
                                                        meleeAttackRadius, targetLayers);
        
        bool hitSomething = false;
        
        foreach (var hitCollider in hitColliders)
        {
            // Check if we hit a player
            Player player = hitCollider.GetComponent<Player>();
            if (player != null)
            {
                Character playerCharacter = player.GetCharacter();
                if (playerCharacter != null)
                {
                    playerCharacter.TakeDamage(meleeAttackDamage);
                    hitSomething = true;
                }
            }
        }
        
        // Visual feedback if we hit something
        if (hitSomething)
        {
            Debug.Log("Enemy melee attack hit target!");
            // You could add a hit effect here
        }
    }
    
    // Helper method to visualize attack radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * meleeAttackRadius * 0.5f, meleeAttackRadius);
    }
}