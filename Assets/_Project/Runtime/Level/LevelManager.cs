using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using InventorySystem; 

public class LevelManager : MonoBehaviour
{
    [Serializable]
    public class LevelData
    {
        public string levelName;
        public string sceneName;
        public Sprite previewImage;
    }

    [Header("Level Configuration")]
    [SerializeField] private List<LevelData> availableLevels = new List<LevelData>();
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float loadingFadeTime = 1.0f;
    
    [Header("References")]
    [SerializeField] private Player playerPrefab;
    [SerializeField] private CanvasGroup loadingScreenCanvasGroup;
    
    private static LevelManager _instance;
    private Player _currentPlayer;
    private int _currentLevelIndex = -1;
    private bool _isLoading = false;
    private AudioListener _mainAudioListener;

    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelManager>();
                
                if (_instance == null)
                {
                    Debug.LogError("No LevelManager found in the scene. Make sure to add one!");
                }
            }
            return _instance;
        }
    }
    
    public int CurrentLevelIndex => _currentLevelIndex;
    public List<LevelData> AvailableLevels => availableLevels;
    public bool IsLoading => _isLoading;
    
    private int _highestUnlockedLevel = 0;
    public int HighestUnlockedLevel => _highestUnlockedLevel;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        AudioListener selfListener = GetComponent<AudioListener>();
        if (selfListener != null)
        {
            Destroy(selfListener);
        }
        
        if (loadingScreenCanvasGroup != null)
        {
            loadingScreenCanvasGroup.alpha = 0;
            loadingScreenCanvasGroup.gameObject.SetActive(false);
        }
        
        LoadProgress();
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(CleanupAudioRoutine());
    }
    
    private IEnumerator CleanupAudioRoutine()
    {
        yield return null;
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            if (listeners.Length > 1)
            {
                AudioListener mainListener = mainCamera.GetComponent<AudioListener>();
                
                foreach (AudioListener listener in listeners)
                {
                    if (listener != mainListener)
                    {
                        listener.enabled = false;
                    }
                }
            }
            else if (listeners.Length == 0)
            {
                if (mainCamera.GetComponent<AudioListener>() == null)
                {
                    mainCamera.gameObject.AddComponent<AudioListener>();
                }
            }
        }
    }
    
    public void LoadProgress()
    {
        _highestUnlockedLevel = PlayerPrefs.GetInt("HighestUnlockedLevel", 0);
    }
    
    public void SaveProgress()
    {
        if (_currentLevelIndex > _highestUnlockedLevel)
        {
            _highestUnlockedLevel = _currentLevelIndex;
            PlayerPrefs.SetInt("HighestUnlockedLevel", _highestUnlockedLevel);
            PlayerPrefs.Save();
        }
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneRoutine(mainMenuSceneName, -1));
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= availableLevels.Count)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        StartCoroutine(LoadSceneRoutine(availableLevels[levelIndex].sceneName, levelIndex));
    }

    public void LoadNextLevel()
    {
        int nextLevel = _currentLevelIndex + 1;
        
        if (nextLevel >= availableLevels.Count)
        {
            LoadMainMenu();
            return;
        }
        
        LoadLevel(nextLevel);
    }

    public void RestartCurrentLevel()
    {
        if (_currentLevelIndex >= 0)
        {
            LoadLevel(_currentLevelIndex);
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName, int levelIndex)
    {
        if (_isLoading) yield break;
        _isLoading = true;
        
        if (loadingScreenCanvasGroup != null)
        {
            loadingScreenCanvasGroup.gameObject.SetActive(true);
            yield return FadeLoadingScreen(1);
        }

        // Save current state before loading
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }

        foreach (AudioListener listener in FindObjectsOfType<AudioListener>())
        {
            if (_mainAudioListener != null && listener == _mainAudioListener)
            {
                continue;
            }
            listener.enabled = false;
        }

        AsyncOperation asyncLoad = null;
        
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            if (asyncLoad == null)
            {
                Debug.LogError($"Failed to load scene: {sceneName}. Scene may not be in build settings.");
                _isLoading = false;
                yield break;
            }
            
            asyncLoad.allowSceneActivation = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading scene {sceneName}: {e.Message}");
            _isLoading = false;
            yield break;
        }

        while (asyncLoad != null && asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = true;
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        _currentLevelIndex = levelIndex;

        // Wait a bit longer to ensure everything is loaded
        yield return new WaitForSeconds(0.2f);

        bool isGameplayLevel = _currentLevelIndex >= 0;
        
        if (isGameplayLevel)
        {
            SpawnPlayer();
            SaveProgress();
            
            // Wait an additional frame to ensure player is fully initialized
            yield return null;
            
            // Explicitly re-establish inventory connections
            ReconnectInventorySystems();
        }
        
        if (GameManager.Instance != null)
        {
            // Trigger level loaded event
            GameManager.Instance.OnLevelLoaded(isGameplayLevel);
            
            // Load game state if in gameplay level
            if (isGameplayLevel)
            {
                GameManager.Instance.LoadGame();
            }
        }

        if (loadingScreenCanvasGroup != null)
        {
            yield return FadeLoadingScreen(0);
            loadingScreenCanvasGroup.gameObject.SetActive(false);
        }

        _isLoading = false;
    }

    private IEnumerator FadeLoadingScreen(float targetAlpha)
    {
        float startAlpha = loadingScreenCanvasGroup.alpha;
        float time = 0;

        while (time < loadingFadeTime)
        {
            time += Time.deltaTime;
            loadingScreenCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / loadingFadeTime);
            yield return null;
        }

        loadingScreenCanvasGroup.alpha = targetAlpha;
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned in LevelManager!");
            return;
        }

        Transform spawnPoint = FindSpawnPoint();
        
        if (spawnPoint == null)
        {
            GameObject defaultSpawn = new GameObject("DefaultSpawnPoint");
            spawnPoint = defaultSpawn.transform;
            spawnPoint.position = new Vector3(0, 2, 0);
        }

        if (_currentPlayer == null)
        {
            _currentPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterPlayer(_currentPlayer);
            }
        }
        else
        {
            _currentPlayer.Teleport(spawnPoint.position);
            _currentPlayer.transform.rotation = spawnPoint.rotation;
        }
        
        _currentPlayer.gameObject.SetActive(true);
        _currentPlayer.EnableGameplayMode(true);
    }
    
    private void ReconnectInventorySystems()
    {
        // Find the inventory manager
        var inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("Inventory Manager not found in the scene");
            return;
        }
        
        // Ensure inventory weapon bridge exists and works
        InventoryWeaponBridge bridge = FindObjectOfType<InventoryWeaponBridge>();
        if (bridge == null && _currentPlayer != null)
        {
            bridge = _currentPlayer.gameObject.AddComponent<InventoryWeaponBridge>();
        }
        
        // Wait a frame to ensure components are ready
        StartCoroutine(DelayedInventorySync(bridge));
    }

    private IEnumerator DelayedInventorySync(InventoryWeaponBridge bridge)
    {
        // Wait two frames to ensure everything is initialized
        yield return null;
        yield return null;
        
        if (bridge != null)
        {
            // Force syncing weapons with inventory
            bridge.MapAvailableWeapons();
            bridge.SyncWeaponsWithInventory();
            Debug.Log("Inventory systems reconnected successfully");
        }
        else
        {
            Debug.LogError("Failed to find or create InventoryWeaponBridge");
        }
    }
    
    private Transform FindSpawnPoint()
    {
        PlayerSpawnPoint[] spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
        
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[0].transform;
        }
        
        return null;
    }
}