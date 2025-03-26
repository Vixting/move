using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class InventoryUIDocumentLoader : MonoBehaviour
{
    [SerializeField] private UIDocument inventoryUIDocument;
    [SerializeField] private VisualTreeAsset inventoryScreenAsset;
    [SerializeField] private VisualTreeAsset gridCellTemplateAsset;
    [SerializeField] private VisualTreeAsset itemTemplateAsset;
    [SerializeField] private StyleSheet inventoryStyleSheet;
    [SerializeField] private Sprite placeholderSprite;
   
    private static InventoryUIDocumentLoader _instance;
    public static InventoryUIDocumentLoader Instance => _instance;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
        SetupInventoryUI();
        ConnectPlayerInput();
    }
    
    private void Start()
    {
        SetupInventoryUI();
        ConnectPlayerInput();
    }
    
    public void SetupInventoryUI()
    {
        if (inventoryUIDocument == null)
        {
            inventoryUIDocument = GetComponentInChildren<UIDocument>();
            if (inventoryUIDocument == null)
            {
                GameObject uiDocumentObj = new GameObject("InventoryUIDocument");
                uiDocumentObj.transform.SetParent(transform);
                inventoryUIDocument = uiDocumentObj.AddComponent<UIDocument>();
            }
        }
        
        if (inventoryScreenAsset != null)
        {
            inventoryUIDocument.visualTreeAsset = inventoryScreenAsset;
        }
        
        // Fixed stylesheet assignment
        if (inventoryStyleSheet != null && inventoryUIDocument.rootVisualElement != null)
        {
            inventoryUIDocument.rootVisualElement.styleSheets.Clear();
            inventoryUIDocument.rootVisualElement.styleSheets.Add(inventoryStyleSheet);
        }
        
        inventoryUIDocument.rootVisualElement.style.display = DisplayStyle.None;
    
        InventoryUIManager uiManager = FindObjectOfType<InventoryUIManager>();
        if (uiManager != null)
        {
            uiManager.SetInventoryUIReferences(
                inventoryUIDocument,
                gridCellTemplateAsset,
                itemTemplateAsset,
                inventoryStyleSheet,
                placeholderSprite
            );
            uiManager.SetupUI();
        }
        else
        {
            Debug.LogWarning("No InventoryUIManager found in scene. Creating one.");
            GameObject uiManagerObj = new GameObject("InventoryUIManager");
            uiManager = uiManagerObj.AddComponent<InventoryUIManager>();
            uiManager.SetInventoryUIReferences(
                inventoryUIDocument,
                gridCellTemplateAsset,
                itemTemplateAsset,
                inventoryStyleSheet,
                placeholderSprite
            );
            uiManager.SetupUI();
        }
    }
    
    public void ConnectPlayerInput()
    {
        Player player = FindObjectOfType<Player>();
        InventoryUIManager uiManager = FindObjectOfType<InventoryUIManager>();
       
        if (player != null && uiManager != null)
        {
            var inputActions = player.GetInputActions();
            if (inputActions != null)
            {
                inputActions.Gameplay.Inventory.performed -= uiManager.ToggleInventory;
                inputActions.Gameplay.Inventory.performed += uiManager.ToggleInventory;
                Debug.Log("Successfully connected inventory input to player");
            }
            else
            {
                Debug.LogError("Player input actions are null");
            }
        }
        else
        {
            if (player == null) Debug.LogWarning("No Player found in scene");
            if (uiManager == null) Debug.LogWarning("No InventoryUIManager found in scene");
        }
    }
    
    public void EnsureInventoryManager()
    {
        if (InventoryManager.Instance == null)
        {
            GameObject invManagerObj = new GameObject("InventoryManager");
            invManagerObj.AddComponent<InventoryManager>();
            Debug.Log("Created InventoryManager");
        }
    }
}