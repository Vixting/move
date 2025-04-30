using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieManager : MonoBehaviour
{
    [System.Serializable]
    public class ZombieSpawnGroup
    {
        public string groupName = "Default Group";
        public GameObject[] zombiePrefabs;
        public Transform[] spawnPoints;
        public int minZombiesPerPoint = 1;
        public int maxZombiesPerPoint = 3;
        public float spawnRadius = 5f;
        public float spawnDelay = 0.5f;
        public bool spawnOnStart = true;
        public bool respawnZombies = false;
        public float respawnTime = 120f;
        [Range(0f, 1f)]
        public float respawnProbability = 0.5f;
    }

    [Header("Zombie Configuration")]
    [SerializeField] private ZombieSpawnGroup[] spawnGroups;
    [SerializeField] private GameObject defaultZombiePrefab;
    [SerializeField] private string defaultZombiePrefabPath = "Prefabs/Zombie";
    [SerializeField] private float navMeshSampleDistance = 5f;

    [Header("Dynamic Spawning")]
    [SerializeField] private bool enableDynamicSpawning = false;
    [SerializeField] private int maxTotalZombies = 30;
    [SerializeField] private float dynamicSpawnDistance = 50f;
    [SerializeField] private float minDistanceFromPlayer = 20f;
    [SerializeField] private float dynamicSpawnInterval = 30f;

    [Header("References")]
    [SerializeField] private Transform zombieContainer;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool enableDebugLogs = true;

    private static ZombieManager _instance;
    private List<GameObject> _activeZombies = new List<GameObject>();
    private Dictionary<GameObject, ZombieSpawnData> _zombieSpawnData = new Dictionary<GameObject, ZombieSpawnData>();
    private Transform _playerTransform;
    private float _nextDynamicSpawnTime;
    private bool _initialized = false;

    private class ZombieSpawnData
    {
        public Vector3 spawnPosition;
        public ZombieSpawnGroup spawnGroup;
        public float deathTime;
        public bool markedForRespawn;
    }

    public static ZombieManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ZombieManager>();
            }
            return _instance;
        }
    }

    private void OnEnable()
    {
        RestoreZombiePrefab();
    }
    
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ZombieManager] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[ZombieManager] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ZombieManager] {message}");
    }
    
    private void RestoreZombiePrefab()
    {
        if (defaultZombiePrefab == null)
        {
            Log($"Attempting to load zombie prefab from path: {defaultZombiePrefabPath}");
            defaultZombiePrefab = Resources.Load<GameObject>(defaultZombiePrefabPath);
            if (defaultZombiePrefab != null)
            {
                Log($"Successfully loaded zombie prefab: {defaultZombiePrefab.name}");
            }
            else
            {
                LogError($"Failed to load zombie prefab from path: {defaultZombiePrefabPath}");
            }
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Log($"Duplicate ZombieManager found, destroying this instance");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        Log("ZombieManager initialized as singleton instance");
        
        if (dontDestroyOnLoad)
        {
            Log("Setting ZombieManager to persist between scenes");
            DontDestroyOnLoad(gameObject);
        }

        if (zombieContainer == null)
        {
            Log("Creating zombie container");
            zombieContainer = new GameObject("ZombieContainer").transform;
            zombieContainer.SetParent(transform);
        }
        
        RestoreZombiePrefab();
    }

    private void Start()
    {
        if (!_initialized)
        {
            Log("Auto-initializing manager on Start");
            InitializeManager();
        }
    }
    
    public void InitializeManager()
    {
        RestoreZombiePrefab();
        
        if (defaultZombiePrefab == null)
        {
            LogError("No default zombie prefab assigned and couldn't load from resources!");
        }
        
        FindPlayer();
        
        if (spawnGroups == null || spawnGroups.Length == 0)
        {
            Log("No spawn groups defined, collecting from scene");
            CollectSpawnPoints();
        }
        
        Log($"Processing {(spawnGroups != null ? spawnGroups.Length : 0)} spawn groups");
        if (spawnGroups != null)
        {
            foreach (ZombieSpawnGroup group in spawnGroups)
            {
                if (group.spawnOnStart)
                {
                    Log($"Starting spawn routine for group: {group.groupName}");
                    StartCoroutine(SpawnGroupRoutine(group));
                }
            }
        }

        _nextDynamicSpawnTime = Time.time + dynamicSpawnInterval;
        _initialized = true;
        
        Log($"Manager initialization complete. Dynamic spawning: {(enableDynamicSpawning ? "Enabled" : "Disabled")}");
    }
    
    public void FindPlayer()
    {
        if (_playerTransform == null)
        {
            Log("Searching for player in scene");
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                _playerTransform = player.transform;
                Log($"Found player: {player.name} at position {_playerTransform.position}");
            }
            else
            {
                LogWarning("No Player found in scene");
            }
        }
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            FindPlayer();
        }
        
        if (enableDynamicSpawning && _playerTransform != null && Time.time >= _nextDynamicSpawnTime)
        {
            if (_activeZombies.Count < maxTotalZombies)
            {
                Log($"Triggering dynamic spawn. Current zombies: {_activeZombies.Count}/{maxTotalZombies}");
                TryDynamicSpawn();
            }
            else
            {
                Log($"Skipping dynamic spawn. At max capacity: {_activeZombies.Count}/{maxTotalZombies}");
            }
            _nextDynamicSpawnTime = Time.time + dynamicSpawnInterval;
        }

        for (int i = _activeZombies.Count - 1; i >= 0; i--)
        {
            if (i >= _activeZombies.Count) continue;
            
            if (_activeZombies[i] == null)
            {
                GameObject zombie = _activeZombies[i];
                _activeZombies.RemoveAt(i);
                Log($"Removed destroyed zombie from active list. Remaining: {_activeZombies.Count}");

                if (zombie != null && _zombieSpawnData.TryGetValue(zombie, out ZombieSpawnData spawnData))
                {
                    if (spawnData.spawnGroup.respawnZombies && Random.value <= spawnData.spawnGroup.respawnProbability)
                    {
                        spawnData.deathTime = Time.time;
                        spawnData.markedForRespawn = true;
                        Log($"Scheduling zombie respawn from group '{spawnData.spawnGroup.groupName}' in {spawnData.spawnGroup.respawnTime} seconds");
                        StartCoroutine(RespawnZombieAfterDelay(spawnData));
                    }
                    
                    _zombieSpawnData.Remove(zombie);
                }
            }
        }
    }
    
    public void CollectSpawnPoints()
    {
        Log("Collecting ZombieSpawnPoint components from scene");
        ZombieSpawnPoint[] spawnPointComponents = FindObjectsOfType<ZombieSpawnPoint>();
        
        if (spawnPointComponents != null && spawnPointComponents.Length > 0)
        {
            Log($"Found {spawnPointComponents.Length} ZombieSpawnPoint components in scene");
            
            List<ZombieSpawnGroup> newGroups = new List<ZombieSpawnGroup>();
            
            foreach (ZombieSpawnPoint spawnPoint in spawnPointComponents)
            {
                if (spawnPoint == null) continue;
                
                bool hasCustomPrefabs = spawnPoint.CustomZombiePrefabs != null && spawnPoint.CustomZombiePrefabs.Length > 0;
                
                ZombieSpawnGroup group = new ZombieSpawnGroup
                {
                    groupName = "Auto Group " + newGroups.Count,
                    zombiePrefabs = hasCustomPrefabs ? 
                                   spawnPoint.CustomZombiePrefabs : 
                                   (defaultZombiePrefab != null ? new GameObject[] { defaultZombiePrefab } : new GameObject[0]),
                    spawnPoints = new Transform[] { spawnPoint.transform },
                    minZombiesPerPoint = spawnPoint.MinZombies,
                    maxZombiesPerPoint = spawnPoint.MaxZombies,
                    spawnRadius = spawnPoint.SpawnRadius,
                    spawnDelay = 0.5f,
                    spawnOnStart = spawnPoint.SpawnOnStart,
                    respawnZombies = spawnPoint.RespawnZombies,
                    respawnTime = spawnPoint.RespawnTime,
                    respawnProbability = 0.5f
                };
                
                newGroups.Add(group);
                
                Log($"Added spawn group '{group.groupName}' at {spawnPoint.transform.position} with " +
                   $"{(hasCustomPrefabs ? "custom prefabs" : "default prefab")} and {spawnPoint.MinZombies}-{spawnPoint.MaxZombies} zombies");
            }
            
            if (newGroups.Count > 0)
            {
                if (spawnGroups == null || spawnGroups.Length == 0)
                {
                    spawnGroups = newGroups.ToArray();
                    Log($"Created {spawnGroups.Length} new spawn groups from scene points");
                }
                else
                {
                    List<ZombieSpawnGroup> combinedGroups = new List<ZombieSpawnGroup>(spawnGroups);
                    combinedGroups.AddRange(newGroups);
                    spawnGroups = combinedGroups.ToArray();
                    Log($"Added {newGroups.Count} spawn groups to existing {combinedGroups.Count - newGroups.Count} groups");
                }
            }
        }
        else
        {
            LogWarning("No ZombieSpawnPoint components found in scene");
        }
    }
    
    public void SpawnInitialZombies()
    {
        Log("SpawnInitialZombies called");
        RestoreZombiePrefab();
        
        if (spawnGroups == null || spawnGroups.Length == 0)
        {
            Log("No spawn groups defined, collecting from scene");
            CollectSpawnPoints();
        }
        
        if (spawnGroups == null || spawnGroups.Length == 0)
        {
            LogWarning("No spawn groups available for spawning after collection");
            return;
        }
        
        int groupsToSpawn = 0;
        foreach (ZombieSpawnGroup group in spawnGroups)
        {
            if (group.spawnOnStart)
            {
                groupsToSpawn++;
                Log($"Starting spawn routine for group: {group.groupName}");
                StartCoroutine(SpawnGroupRoutine(group));
            }
        }
        
        Log($"Initiated spawning for {groupsToSpawn} groups out of {spawnGroups.Length} total");
    }

    private IEnumerator SpawnGroupRoutine(ZombieSpawnGroup group)
    {
        if (group.zombiePrefabs == null || group.zombiePrefabs.Length == 0)
        {
            Log($"Group '{group.groupName}' has no prefabs, attempting to use default");
            RestoreZombiePrefab();
            
            if (defaultZombiePrefab != null)
            {
                group.zombiePrefabs = new GameObject[] { defaultZombiePrefab };
                Log($"Using default zombie prefab for group '{group.groupName}'");
            }
            else
            {
                LogWarning($"Zombie group '{group.groupName}' has no zombie prefabs assigned and no default available");
                yield break;
            }
        }

        if (group.spawnPoints == null || group.spawnPoints.Length == 0)
        {
            LogWarning($"Zombie group '{group.groupName}' has no spawn points assigned");
            yield break;
        }

        int totalZombiesSpawned = 0;
        
        foreach (Transform spawnPoint in group.spawnPoints)
        {
            if (spawnPoint == null)
            {
                LogWarning($"Null spawn point in group '{group.groupName}'");
                continue;
            }

            int zombieCount = Random.Range(group.minZombiesPerPoint, group.maxZombiesPerPoint + 1);
            Log($"Spawning {zombieCount} zombies at point {spawnPoint.name} ({spawnPoint.position})");

            for (int i = 0; i < zombieCount; i++)
            {
                Vector3 spawnPosition = GetSpawnPosition(spawnPoint.position, group.spawnRadius);
                
                if (spawnPosition != Vector3.zero)
                {
                    GameObject zombiePrefab = GetRandomZombiePrefab(group);
                    GameObject zombie = SpawnZombie(zombiePrefab, spawnPosition, group);
                    
                    if (zombie != null)
                    {
                        totalZombiesSpawned++;
                        _zombieSpawnData[zombie] = new ZombieSpawnData
                        {
                            spawnPosition = spawnPosition,
                            spawnGroup = group,
                            deathTime = 0,
                            markedForRespawn = false
                        };
                        
                        Log($"Spawned zombie #{totalZombiesSpawned} at {spawnPosition}");
                    }
                    else
                    {
                        LogWarning($"Failed to spawn zombie at {spawnPosition}");
                    }

                    yield return new WaitForSeconds(group.spawnDelay);
                }
                else
                {
                    LogWarning($"Failed to find valid spawn position near {spawnPoint.name}");
                }
            }
        }
        
        Log($"Finished spawning {totalZombiesSpawned} zombies for group '{group.groupName}'");
    }

    private Vector3 GetSpawnPosition(Vector3 center, float radius)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                if (_playerTransform != null)
                {
                    if (Vector3.Distance(hit.position, _playerTransform.position) < minDistanceFromPlayer)
                    {
                        continue; // Too close to player
                    }
                }
                
                return hit.position;
            }
        }
        
        LogWarning($"Failed to find valid NavMesh position near {center} after 30 attempts");
        return Vector3.zero;
    }

    private GameObject GetRandomZombiePrefab(ZombieSpawnGroup group)
    {
        if (group.zombiePrefabs == null || group.zombiePrefabs.Length == 0)
        {
            if (defaultZombiePrefab == null)
            {
                RestoreZombiePrefab();
            }
            return defaultZombiePrefab;
        }
        
        return group.zombiePrefabs[Random.Range(0, group.zombiePrefabs.Length)];
    }

    private GameObject SpawnZombie(GameObject zombiePrefab, Vector3 position, ZombieSpawnGroup group)
    {
        if (zombiePrefab == null)
        {
            LogWarning("Null zombie prefab passed to SpawnZombie, attempting to use default");
            RestoreZombiePrefab();
            
            if (defaultZombiePrefab == null)
            {
                LogError("Failed to spawn zombie - no prefab available");
                return null;
            }
            zombiePrefab = defaultZombiePrefab;
        }

        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        
        GameObject zombie = Instantiate(zombiePrefab, position, rotation, zombieContainer);
        
        if (zombie == null)
        {
            LogError($"Failed to instantiate zombie from prefab {zombiePrefab.name}");
            return null;
        }
        
        _activeZombies.Add(zombie);
        
        ZombieAI zombieAI = zombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            StartCoroutine(InitializeZombieAI(zombieAI));
        }
        else
        {
            LogWarning($"Spawned zombie does not have ZombieAI component: {zombie.name}");
        }
        
        return zombie;
    }

    private IEnumerator InitializeZombieAI(ZombieAI zombieAI)
    {
        yield return new WaitForSeconds(0.1f);
        
        if (zombieAI != null)
        {
            zombieAI.StabilizePosition();
            Log($"Stabilized zombie at {zombieAI.transform.position}");
        }
    }

    private IEnumerator RespawnZombieAfterDelay(ZombieSpawnData spawnData)
    {
        if (spawnData == null || spawnData.spawnGroup == null) 
        {
            LogWarning("Invalid spawn data passed to respawn routine");
            yield break;
        }
        
        Log($"Waiting {spawnData.spawnGroup.respawnTime}s to respawn zombie");
        yield return new WaitForSeconds(spawnData.spawnGroup.respawnTime);
        
        if (_activeZombies.Count >= maxTotalZombies)
        {
            Log($"Skipping respawn due to max zombie limit: {_activeZombies.Count}/{maxTotalZombies}");
            yield break;
        }
        
        if (_playerTransform != null && 
            Vector3.Distance(spawnData.spawnPosition, _playerTransform.position) < minDistanceFromPlayer)
        {
            Log("Rescheduling respawn - player too close to spawn position");
            StartCoroutine(RespawnZombieAfterDelay(spawnData));
            yield break;
        }
        
        GameObject zombiePrefab = GetRandomZombiePrefab(spawnData.spawnGroup);
        GameObject newZombie = SpawnZombie(zombiePrefab, spawnData.spawnPosition, spawnData.spawnGroup);
        
        if (newZombie != null)
        {
            Log($"Respawned zombie at {spawnData.spawnPosition}");
            _zombieSpawnData[newZombie] = new ZombieSpawnData
            {
                spawnPosition = spawnData.spawnPosition,
                spawnGroup = spawnData.spawnGroup,
                deathTime = 0,
                markedForRespawn = false
            };
        }
        else
        {
            LogWarning("Failed to respawn zombie");
        }
    }

    private void TryDynamicSpawn()
    {
        if (_playerTransform == null)
        {
            LogWarning("Cannot dynamic spawn without player reference");
            return;
        }
        
        Vector3 playerPos = _playerTransform.position;
        
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistanceFromPlayer, dynamicSpawnDistance);
        Vector3 potentialSpawnPos = playerPos + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        Log($"Trying dynamic spawn at {potentialSpawnPos} (distance from player: {Vector3.Distance(playerPos, potentialSpawnPos)})");
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(potentialSpawnPos, out hit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            Log($"Found valid NavMesh position: {hit.position}");
            
            RestoreZombiePrefab();
            
            if (defaultZombiePrefab == null)
            {
                LogError("No default zombie prefab for dynamic spawning");
                return;
            }
            
            ZombieSpawnGroup tempGroup = new ZombieSpawnGroup
            {
                groupName = "Dynamic Group",
                zombiePrefabs = spawnGroups != null && spawnGroups.Length > 0 && 
                                spawnGroups[0].zombiePrefabs != null && spawnGroups[0].zombiePrefabs.Length > 0 
                              ? spawnGroups[0].zombiePrefabs 
                              : new GameObject[] { defaultZombiePrefab },
                minZombiesPerPoint = 1,
                maxZombiesPerPoint = 3,
                spawnRadius = 10f,
                respawnZombies = false
            };
            
            int zombieCount = Random.Range(tempGroup.minZombiesPerPoint, tempGroup.maxZombiesPerPoint + 1);
            Log($"Dynamic spawning {zombieCount} zombies around {hit.position}");
            
            int spawnedCount = 0;
            for (int i = 0; i < zombieCount; i++)
            {
                Vector3 spawnPos = GetSpawnPosition(hit.position, tempGroup.spawnRadius);
                if (spawnPos != Vector3.zero)
                {
                    GameObject zombiePrefab = GetRandomZombiePrefab(tempGroup);
                    GameObject zombie = SpawnZombie(zombiePrefab, spawnPos, tempGroup);
                    
                    if (zombie != null)
                    {
                        spawnedCount++;
                        _zombieSpawnData[zombie] = new ZombieSpawnData
                        {
                            spawnPosition = spawnPos,
                            spawnGroup = tempGroup,
                            deathTime = 0,
                            markedForRespawn = false
                        };
                    }
                }
            }
            
            Log($"Dynamically spawned {spawnedCount}/{zombieCount} zombies");
        }
        else
        {
            LogWarning($"No valid NavMesh position found near {potentialSpawnPos}");
        }
    }

    public void SpawnZombiesAtPoint(Vector3 position, int count, float radius)
    {
        Log($"Manually spawning {count} zombies at {position} with radius {radius}");
        
        RestoreZombiePrefab();
        
        if (defaultZombiePrefab == null)
        {
            LogError("No default zombie prefab assigned to ZombieManager");
            return;
        }
        
        ZombieSpawnGroup tempGroup = new ZombieSpawnGroup
        {
            groupName = "Manual Spawn",
            zombiePrefabs = new GameObject[] { defaultZombiePrefab },
            minZombiesPerPoint = count,
            maxZombiesPerPoint = count,
            spawnRadius = radius,
            respawnZombies = false
        };
        
        int spawnedCount = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(position, radius);
            if (spawnPos != Vector3.zero)
            {
                GameObject zombie = SpawnZombie(defaultZombiePrefab, spawnPos, tempGroup);
                
                if (zombie != null)
                {
                    spawnedCount++;
                    _zombieSpawnData[zombie] = new ZombieSpawnData
                    {
                        spawnPosition = spawnPos,
                        spawnGroup = tempGroup,
                        deathTime = 0,
                        markedForRespawn = false
                    };
                }
            }
        }
        
        Log($"Manually spawned {spawnedCount}/{count} zombies");
    }

    public void ClearAllZombies()
    {
        Log("Clearing all zombies");
        StopAllCoroutines();
        
        int zombieCount = _activeZombies.Count;
        foreach (GameObject zombie in _activeZombies)
        {
            if (zombie != null)
            {
                Destroy(zombie);
            }
        }
        
        _activeZombies.Clear();
        _zombieSpawnData.Clear();
        Log($"Cleared {zombieCount} zombies");
    }

    public int GetActiveZombieCount()
    {
        return _activeZombies.Count;
    }
}