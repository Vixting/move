using UnityEngine;
using UnityEngine.UIElements;

public class UIDocumentLoader : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset visualTreeAsset;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private PanelSettings panelSettings;
    
    [SerializeField] private bool loadAtRuntime = true;
    [SerializeField] private string uxmlAssetPath = "UI/HUD/GameHUD";
    [SerializeField] private string ussAssetPath = "UI/HUD/GameHUD";
    
    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
                Debug.Log("Added UIDocument component");
            }
        }
        
        // Set Visual Tree Asset if provided
        if (visualTreeAsset != null)
        {
            uiDocument.visualTreeAsset = visualTreeAsset;
        }
        else if (loadAtRuntime && !string.IsNullOrEmpty(uxmlAssetPath))
        {
            // Try to load from Resources
            VisualTreeAsset loadedAsset = Resources.Load<VisualTreeAsset>(uxmlAssetPath);
            if (loadedAsset != null)
            {
                uiDocument.visualTreeAsset = loadedAsset;
                Debug.Log($"Loaded UXML from Resources: {uxmlAssetPath}");
            }
            else
            {
                Debug.LogWarning($"Failed to load UXML from Resources: {uxmlAssetPath}");
            }
        }
        
        // Set Panel Settings if provided
        if (panelSettings != null)
        {
            uiDocument.panelSettings = panelSettings;
        }
    }
    
    private void OnEnable()
    {
        // Ensure we have a root element
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            Debug.LogWarning("UIDocument or root element is null");
            return;
        }
        
        // Try to apply style sheet
        if (styleSheet != null)
        {
            AddStyleSheet(styleSheet);
        }
        else if (loadAtRuntime && !string.IsNullOrEmpty(ussAssetPath))
        {
            // Try to load StyleSheet from Resources
            StyleSheet loadedStyleSheet = Resources.Load<StyleSheet>(ussAssetPath);
            if (loadedStyleSheet != null)
            {
                AddStyleSheet(loadedStyleSheet);
                Debug.Log($"Loaded USS from Resources: {ussAssetPath}");
            }
            else
            {
                Debug.LogWarning($"Failed to load USS from Resources: {ussAssetPath}");
            }
        }
    }
    
    private void AddStyleSheet(StyleSheet sheet)
    {
        try
        {
            VisualElement root = uiDocument.rootVisualElement;
            
            // Check if already added
            bool alreadyAdded = false;
            for (int i = 0; i < root.styleSheets.count; i++)
            {
                if (root.styleSheets[i] == sheet)
                {
                    alreadyAdded = true;
                    break;
                }
            }
            
            if (!alreadyAdded)
            {
                root.styleSheets.Add(sheet);
                Debug.Log("Added StyleSheet to UIDocument");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error adding StyleSheet: {e.Message}");
        }
    }
}