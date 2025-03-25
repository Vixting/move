using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private float healthRegenDelay = 5f;
    [SerializeField] private int healthRegenAmount = 1;
    [SerializeField] private float healthRegenInterval = 0.5f;
    
    [Header("Damage Settings")]
    [SerializeField] private float damageIndicatorDuration = 0.5f;
    [SerializeField] private float lowHealthThreshold = 30f;
    [SerializeField] private AudioClip damageSoundEffect;
    [SerializeField] private AudioClip healSoundEffect;
    [SerializeField] private AudioClip deathSoundEffect;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameHUDController hudController;
    [SerializeField] private PlayerCharacter playerCharacter;
    
    // Events
    public UnityEvent<int, int> onHealthChanged = new UnityEvent<int, int>(); // current, max
    public UnityEvent onDeath = new UnityEvent();
    public UnityEvent onDamage = new UnityEvent();
    public UnityEvent onHeal = new UnityEvent();
    public UnityEvent onLowHealth = new UnityEvent();
    
    // Private variables
    private float lastDamageTime;
    private float healthRegenTimer;
    private bool isRegenerating = false;
    private bool isLowHealth = false;
    
    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        if (hudController == null)
        {
            hudController = FindObjectOfType<GameHUDController>();
        }
        
        if (playerCharacter == null)
        {
            playerCharacter = GetComponent<PlayerCharacter>();
        }
        
        // Register with HUD
        if (hudController != null)
        {
            onHealthChanged.AddListener(hudController.OnHealthChanged);
            hudController.OnHealthChanged(currentHealth, maxHealth);
        }
    }
    
    private void Update()
    {
        // Handle health regeneration
        if (currentHealth < maxHealth && Time.time > lastDamageTime + healthRegenDelay)
        {
            healthRegenTimer += Time.deltaTime;
            
            if (healthRegenTimer >= healthRegenInterval)
            {
                Heal(healthRegenAmount);
                healthRegenTimer = 0f;
                
                if (!isRegenerating)
                {
                    isRegenerating = true;
                    // Could trigger regeneration effect here
                }
            }
        }
        else
        {
            isRegenerating = false;
        }
    }
    
    public void TakeDamage(int damageAmount, Vector3 damageDirection = default)
    {
        if (currentHealth <= 0) return;
        
        lastDamageTime = Time.time;
        isRegenerating = false;
        healthRegenTimer = 0f;
        
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);
        
        // Apply knockback if we have a direction and character
        if (damageDirection != default && playerCharacter != null)
        {
            float knockbackForce = Mathf.Min(damageAmount * 0.5f, 10f);
            playerCharacter.ApplyKnockback(damageDirection, knockbackForce);
        }
        
        // Play sound effect
        if (audioSource != null && damageSoundEffect != null)
        {
            audioSource.PlayOneShot(damageSoundEffect);
        }
        
        // Notify listeners of health change
        onHealthChanged.Invoke(currentHealth, maxHealth);
        onDamage.Invoke();
        
        // Check for low health state
        CheckLowHealthState();
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int healAmount)
    {
        if (currentHealth >= maxHealth || currentHealth <= 0) return;
        
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        
        if (currentHealth != oldHealth)
        {
            // Play sound effect
            if (audioSource != null && healSoundEffect != null)
            {
                audioSource.PlayOneShot(healSoundEffect);
            }
            
            // Notify listeners of health change
            onHealthChanged.Invoke(currentHealth, maxHealth);
            onHeal.Invoke();
            
            // Check if we've recovered from low health
            CheckLowHealthState();
        }
    }
    
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        onHealthChanged.Invoke(currentHealth, maxHealth);
        
        CheckLowHealthState();
    }
    
    private void CheckLowHealthState()
    {
        bool wasLowHealth = isLowHealth;
        isLowHealth = currentHealth <= lowHealthThreshold;
        
        if (isLowHealth && !wasLowHealth)
        {
            onLowHealth.Invoke();
        }
    }
    
    private void Die()
    {
        // Play death sound
        if (audioSource != null && deathSoundEffect != null)
        {
            audioSource.PlayOneShot(deathSoundEffect);
        }
        
        // Notify listeners of death
        onDeath.Invoke();
        
        // Here you would typically implement respawn logic or game over
        Debug.Log("Player died!");
        
        // Example: Disable player movement
        if (playerCharacter != null)
        {
            // Disable player character controller
        }
        
        // Example: Show death screen through game manager
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // gameManager.OnPlayerDeath();
        }
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    public bool IsLowHealth()
    {
        return isLowHealth;
    }
}