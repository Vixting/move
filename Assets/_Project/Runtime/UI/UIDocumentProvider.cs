using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Helper class to manage UI Document references and initialization
/// </summary>
public class UIDocumentProvider : MonoBehaviour
{
    [SerializeField] private UIDocument menuDocument;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LevelManager levelManager;
    
    private void Awake()
    {
        Debug.Log("UIDocumentProvider Awake started");
        
        if (menuDocument == null)
        {
            menuDocument = GetComponent<UIDocument>();
            if (menuDocument == null)
            {
                Debug.LogError("UIDocument not found on UIDocumentProvider");
                return;
            }
            Debug.Log("UIDocument found: " + menuDocument.name);
        }
        
        // Find or create GameManager if needed
        if (gameManager == null)
        {
            Debug.Log("Finding GameManager...");
            gameManager = GameManager.Instance;
            if (gameManager != null)
                Debug.Log("GameManager found");
            else
                Debug.LogWarning("GameManager not found");
        }
        
        // Find LevelManager if needed
        if (levelManager == null && gameManager != null)
        {
            Debug.Log("Finding LevelManager...");
            levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
                Debug.Log("LevelManager found");
            else
                Debug.LogWarning("LevelManager not found");
        }
        
        // Setup the UI
        Debug.Log("Setting up UI...");
        SetupUI();
    }
    
    private void SetupUI()
    {
        if (menuDocument == null) return;
        
        // Add MainMenuController component if needed
        MainMenuController menuController = gameObject.GetComponent<MainMenuController>();
        if (menuController == null)
        {
            menuController = gameObject.AddComponent<MainMenuController>();
        }
        
        // Initialize the menu
        menuController.SetupMenu(menuDocument, levelManager);
    }
}