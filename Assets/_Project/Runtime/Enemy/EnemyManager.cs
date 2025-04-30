using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnPoint
    {
        public Transform point;
        public GameObject enemyPrefab;
        public bool spawnOnStart = true;
        public float respawnDelay = 0f;
        [HideInInspector] public float nextSpawnTime = 0f;
    }
    
    [Header("Spawning")]
    [SerializeField] private SpawnPoint[] spawnPoints;
    [SerializeField] private int maxEnemiesAlive = 5;
    [SerializeField] private bool enableRespawning = true;
    
    [Header("Difficulty")]
    [SerializeField] private float enemyHealthMultiplier = 1.0f;
    [SerializeField] private float enemyDamageMultiplier = 1.0f;
    [SerializeField] private bool dynamicDifficulty = true;
    [SerializeField] private float playerPerformanceWeight = 0.5f;
    
    [Header("Events")]
    public UnityEvent<EnemyAI> onEnemySpawned;
    public UnityEvent<EnemyAI> onEnemyKilled;
    public UnityEvent<int> onAllEnemiesDefeated;
    
    private List<EnemyAI> activeEnemies = new List<EnemyAI>();
    private int totalEnemiesKilled = 0;
    private int totalEnemiesSpawned = 0;
    private float difficultyFactor = 1.0f;
    
    private static EnemyManager _instance;
    public static EnemyManager Instance => _instance;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    
    private void Start()
    {
        SpawnInitialEnemies();
        StartCoroutine(ManageEnemiesRoutine());
    }
    
    private void SpawnInitialEnemies()
    {
        foreach (var point in spawnPoints)
        {
            if (point.spawnOnStart && point.enemyPrefab != null)
            {
                SpawnEnemy(point);
            }
        }
    }
    
    private IEnumerator ManageEnemiesRoutine()
    {
        while (true)
        {
            // Clean up list of dead enemies
            activeEnemies.RemoveAll(e => e == null);
            
            // Check for respawns
            if (enableRespawning)
            {
                foreach (var point in spawnPoints)
                {
                    if (point.respawnDelay > 0 && Time.time >= point.nextSpawnTime && 
                        activeEnemies.Count < maxEnemiesAlive)
                    {
                        SpawnEnemy(point);
                    }
                }
            }
            
            // Check if all enemies are defeated
            if (activeEnemies.Count == 0 && totalEnemiesSpawned > 0)
            {
                onAllEnemiesDefeated?.Invoke(totalEnemiesKilled);
            }
            
            yield return new WaitForSeconds(1.0f);
        }
    }
    
    private void SpawnEnemy(SpawnPoint point)
    {
        if (point.enemyPrefab == null) return;
        
        GameObject enemyObj = Instantiate(point.enemyPrefab, point.point.position, point.point.rotation);
        EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
        
        if (enemyAI != null)
        {
            // Apply difficulty settings
            EnemyCharacter character = enemyObj.GetComponent<EnemyCharacter>();
            if (character != null && difficultyFactor != 1.0f)
            {
                // Use reflection to modify private fields
                var healthField = typeof(EnemyCharacter).GetField("maxHealth", 
                                System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                if (healthField != null)
                {
                    float originalHealth = (float)healthField.GetValue(character);
                    float adjustedHealth = originalHealth * difficultyFactor * enemyHealthMultiplier;
                    healthField.SetValue(character, adjustedHealth);
                    
                    var currentHealthField = typeof(EnemyCharacter).GetField("currentHealth", 
                                          System.Reflection.BindingFlags.NonPublic | 
                                          System.Reflection.BindingFlags.Instance);
                    if (currentHealthField != null)
                    {
                        currentHealthField.SetValue(character, adjustedHealth);
                    }
                }
            }
            
            // Track the enemy
            activeEnemies.Add(enemyAI);
            totalEnemiesSpawned++;
            
            // Subscribe to death event
            if (character != null)
            {
                character.onDeath.AddListener(() => OnEnemyKilled(enemyAI, point));
            }
            
            onEnemySpawned?.Invoke(enemyAI);
        }
        
        // Set next spawn time
        point.nextSpawnTime = Time.time + point.respawnDelay;
    }
    
    private void OnEnemyKilled(EnemyAI enemy, SpawnPoint sourcePoint)
    {
        activeEnemies.Remove(enemy);
        totalEnemiesKilled++;
        
        onEnemyKilled?.Invoke(enemy);
        
        if (dynamicDifficulty)
        {
            UpdateDifficultyFactor();
        }
    }
    
    private void UpdateDifficultyFactor()
    {
        if (totalEnemiesSpawned == 0) return;
        
        // Calculate player performance (kill ratio)
        float killRatio = (float)totalEnemiesKilled / totalEnemiesSpawned;
        
        // If player is doing well (high kill ratio), increase difficulty
        // If player is struggling (low kill ratio), decrease difficulty
        float targetDifficulty;
        if (killRatio > 0.8f)
        {
            // Player is doing very well, increase difficulty
            targetDifficulty = 1.2f;
        }
        else if (killRatio > 0.6f)
        {
            // Player is doing well, slightly increase difficulty
            targetDifficulty = 1.1f;
        }
        else if (killRatio < 0.3f)
        {
            // Player is struggling, decrease difficulty
            targetDifficulty = 0.8f;
        }
        else if (killRatio < 0.5f)
        {
            // Player is doing ok but below average, slightly decrease difficulty
            targetDifficulty = 0.9f;
        }
        else
        {
            // Player is doing average, keep normal difficulty
            targetDifficulty = 1.0f;
        }
        
        // Gradually adjust the difficulty factor
        difficultyFactor = Mathf.Lerp(difficultyFactor, targetDifficulty, playerPerformanceWeight);
    }
    
    public void AlertAllEnemiesTo(Transform target)
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.AlertToPlayer(target);
            }
        }
    }
    
    public void AlertEnemiesInRadius(Vector3 position, float radius, Transform target = null)
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;
            
            float distance = Vector3.Distance(enemy.transform.position, position);
            if (distance <= radius)
            {
                if (target != null)
                {
                    enemy.AlertToPlayer(target);
                }
                else
                {
                    enemy.AlertToPosition(position);
                }
            }
        }
    }
    
    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }
    
    public int GetTotalKills()
    {
        return totalEnemiesKilled;
    }
    
    public void SetMaxEnemies(int max)
    {
        maxEnemiesAlive = Mathf.Max(1, max);
    }
    
    public void SetEnemyHealthMultiplier(float multiplier)
    {
        enemyHealthMultiplier = Mathf.Max(0.1f, multiplier);
    }
    
    public void SetEnemyDamageMultiplier(float multiplier)
    {
        enemyDamageMultiplier = Mathf.Max(0.1f, multiplier);
    }
}