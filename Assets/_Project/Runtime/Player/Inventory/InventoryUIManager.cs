using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using InventorySystem;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private UIDocument inventoryDocument;
    [SerializeField] private PlayerInputActions inputActions;
   
    private InventoryManager _inventoryManager;
    private Player _player;
    private bool _isGameplayMode = false;
    private bool _isInitialized = false;
   
    private void Awake()
    {
        _inventoryManager = GetComponent<InventoryManager>();
        _player = FindObjectOfType<Player>();
       
        if (_player != null && inputActions == null)
        {
            inputActions = _player.GetInputActions();
        }
        
        _isGameplayMode = SceneManager.GetActiveScene().name != "MainMenu";
    }
   
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelLoadedEvent += HandleLevelLoaded;
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        RegisterInputEvents();
    }
   
    private void OnDisable()
    {
        UnregisterInputEvents();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevelLoadedEvent -= HandleLevelLoaded;
        }
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReconnectAfterSceneLoad());
    }

    private System.Collections.IEnumerator ReconnectAfterSceneLoad()
    {
        yield return null;
        
        _player = FindObjectOfType<Player>();
        
        if (_player != null && inputActions == null)
        {
            inputActions = _player.GetInputActions();
        }
        
        UnregisterInputEvents();
        RegisterInputEvents();
        
        _isGameplayMode = SceneManager.GetActiveScene().name != "MainMenu";
        
        if (_isGameplayMode)
        {
            if (inventoryDocument != null)
            {
                inventoryDocument.gameObject.SetActive(true);
            }
            InitializeUI();
        }
        else
        {
            if (inventoryDocument != null)
            {
                inventoryDocument.gameObject.SetActive(false);
            }
        }
    }
    
    private void RegisterInputEvents()
    {
        if (inputActions != null && _isGameplayMode)
        {
            inputActions.Gameplay.Inventory.performed += OnInventoryAction;
        }
    }
    
    private void UnregisterInputEvents()
    {
        if (inputActions != null)
        {
            inputActions.Gameplay.Inventory.performed -= OnInventoryAction;
        }
    }
   
    private void HandleLevelLoaded(bool isGameplayLevel)
    {
        _isGameplayMode = isGameplayLevel;
        
        if (_player == null || !_player.gameObject.activeInHierarchy)
        {
            _player = FindObjectOfType<Player>();
            if (_player != null && inputActions == null)
            {
                inputActions = _player.GetInputActions();
            }
        }
        
        if (_inventoryManager == null)
        {
            _inventoryManager = GetComponent<InventoryManager>();
            if (_inventoryManager == null)
            {
                _inventoryManager = FindObjectOfType<InventoryManager>();
            }
        }
        
        UnregisterInputEvents();
        RegisterInputEvents();
        
        if (inventoryDocument != null)
        {
            inventoryDocument.gameObject.SetActive(_isGameplayMode);
        }
        
        if (_isGameplayMode && _inventoryManager != null)
        {
            _isInitialized = false;
            InitializeUI();
        }
    }
   
    private void OnInventoryAction(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_inventoryManager == null)
        {
            _inventoryManager = FindObjectOfType<InventoryManager>();
            if (_inventoryManager == null) return;
        }
        
        Debug.Log("Inventory toggled");
        _inventoryManager.ToggleInventory();
    }
   
    private void Start()
    {
        if (_isGameplayMode)
        {
            InitializeUI();
        }
    }
    
    private void Update()
    {
        if (_isGameplayMode && !_isInitialized)
        {
            EnsureConnections();
        }
    }
    
    private void EnsureConnections()
    {
        if (_player == null)
        {
            _player = FindObjectOfType<Player>();
            if (_player != null && inputActions == null)
            {
                inputActions = _player.GetInputActions();
                RegisterInputEvents();
            }
        }
        
        if (_inventoryManager == null)
        {
            _inventoryManager = FindObjectOfType<InventoryManager>();
        }
        
        if (_player != null && _inventoryManager != null && !_isInitialized)
        {
            InitializeUI();
        }
    }
   
    private void InitializeUI()
    {
        if (inventoryDocument == null || inventoryDocument.rootVisualElement == null)
        {
            Debug.LogWarning("Inventory document not set or root visual element not found");
            return;
        }
        
        VisualElement root = inventoryDocument.rootVisualElement.Q("inventory-root");
        if (root == null)
        {
            Debug.LogWarning("Inventory root element not found");
            return;
        }
       
        if (_player != null)
        {
            Label characterNameLabel = root.Q<Label>("character-name");
            if (characterNameLabel != null)
            {
                Character character = _player.GetComponent<Character>();
                characterNameLabel.text = character?.CharacterName ?? "PMC Character";
            }
        }
       
        SetupWeightIndicator();
        _isInitialized = true;
    }
   
    private void SetupWeightIndicator()
    {
        if (inventoryDocument == null || inventoryDocument.rootVisualElement == null) return;
        
        VisualElement root = inventoryDocument.rootVisualElement.Q("inventory-root");
        if (root == null) return;
        
        Label weightLabel = root.Q<Label>("weight-value");
        ProgressBar weightBar = root.Q<ProgressBar>("weight-bar");
       
        if (weightLabel != null && weightBar != null)
        {
            Character character = GetComponent<Character>();
            if (character == null && _player != null)
            {
                character = _player.GetComponent<Character>();
            }
            
            if (character == null)
            {
                character = FindObjectOfType<Character>();
            }
            
            if (character != null)
            {
                float currentWeight = character.CurrentWeight;
                float maxWeight = character.MaxWeight;
               
                weightLabel.text = $"{currentWeight:F1}/{maxWeight:F1} kg";
                weightBar.value = (currentWeight / maxWeight) * 100;
               
                character.OnWeightChanged -= UpdateWeightUI;
                character.OnWeightChanged += UpdateWeightUI;
            }
        }
    }
    
    private void UpdateWeightUI(float weight, float max)
    {
        if (inventoryDocument == null || inventoryDocument.rootVisualElement == null) return;
        
        VisualElement root = inventoryDocument.rootVisualElement.Q("inventory-root");
        if (root == null) return;
        
        Label weightLabel = root.Q<Label>("weight-value");
        ProgressBar weightBar = root.Q<ProgressBar>("weight-bar");
        
        if (weightLabel != null && weightBar != null)
        {
            weightLabel.text = $"{weight:F1}/{max:F1} kg";
            weightBar.value = (weight / max) * 100;
        }
    }
    
    public void OnValidate()
    {
        if (inventoryDocument == null)
        {
            inventoryDocument = GetComponent<UIDocument>();
        }
    }
}