using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class OptionsMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument menuDocument;
    [SerializeField] private InputRebinder inputRebinder;
    [SerializeField] private GameObject mainMenuGameObject;
    
    private VisualElement audioPanel;
    private VisualElement graphicsPanel;
    private VisualElement controlsPanel;
    private VisualElement keybindingsPanel;
    
    private Button audioTabButton;
    private Button graphicsTabButton;
    private Button controlsTabButton;
    private Button keybindingsTabButton;
    
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private DropdownField resolutionDropdown;
    private DropdownField graphicsDropdown;
    private Toggle fullscreenToggle;
    private Slider sensitivitySlider;
    private Toggle invertYToggle;
    
    private ScrollView keybindingsScrollView;
    
    private Button applyButton;
    private Button resetButton;
    private Button backButton;
    private Button closeButton;
    private Button minimizeButton;
    
    private Dictionary<string, VisualElement> rebindButtons = new Dictionary<string, VisualElement>();
    private Dictionary<string, Label> rebindLabels = new Dictionary<string, Label>();
    
    private VisualElement rebindOverlay;
    private Label rebindPromptLabel;
    private Button rebindCancelButton;
    
    private Dictionary<string, List<InputRebinder.KeyBindingInfo>> allBindings;
    
    private bool rebindingInProgress = false;
    private bool hasUnsavedChanges = false;
    
    private Player playerReference;
    
    public void Initialize(UIDocument document, Player player)
    {
        Debug.Log("OptionsMenuController.Initialize called with document and player");
        menuDocument = document;
        playerReference = player;
        
        if (gameObject.activeInHierarchy)
        {
            InitializeUI();
        }
    }
    
    private void Awake()
    {
        if (inputRebinder == null)
        {
            inputRebinder = InputRebinder.Instance;
            Debug.Log("Getting InputRebinder instance in Awake");
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("OptionsMenuController.OnEnable called");
        InitializeUI();
        SetupInitialValues();
    }
    
    private void InitializeUI()
    {
        Debug.Log("OptionsMenuController.InitializeUI called");
        if (menuDocument == null)
        {
            Debug.LogError("Menu document is null in OptionsMenuController");
            return;
        }
        
        var root = menuDocument.rootVisualElement;
        
        // Get tabs and panels
        audioPanel = root.Q<VisualElement>("AudioPanel");
        graphicsPanel = root.Q<VisualElement>("GraphicsPanel");
        controlsPanel = root.Q<VisualElement>("ControlsPanel");
        keybindingsPanel = root.Q<VisualElement>("KeybindingsPanel");
        
        audioTabButton = root.Q<Button>("AudioTabButton");
        graphicsTabButton = root.Q<Button>("GraphicsTabButton");
        controlsTabButton = root.Q<Button>("ControlsTabButton");
        keybindingsTabButton = root.Q<Button>("KeybindingsTabButton");
        
        // Get settings controls
        musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
        sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");
        resolutionDropdown = root.Q<DropdownField>("ResolutionDropdown");
        graphicsDropdown = root.Q<DropdownField>("GraphicsDropdown");
        fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        sensitivitySlider = root.Q<Slider>("SensitivitySlider");
        invertYToggle = root.Q<Toggle>("InvertYToggle");
        
        keybindingsScrollView = root.Q<ScrollView>("KeybindingsScrollView");
        
        // Get buttons
        applyButton = root.Q<Button>("ApplyButton");
        resetButton = root.Q<Button>("ResetButton");
        backButton = root.Q<Button>("BackButton");
        closeButton = root.Q<Button>("CloseButton");
        minimizeButton = root.Q<Button>("MinimizeButton");
        
        // Get rebind overlay elements
        rebindOverlay = root.Q<VisualElement>("RebindOverlay");
        rebindPromptLabel = root.Q<Label>("RebindPromptLabel");
        rebindCancelButton = root.Q<Button>("RebindCancelButton");
        
        // Debug check UI elements
        if (audioPanel == null) Debug.LogError("AudioPanel not found");
        if (graphicsPanel == null) Debug.LogError("GraphicsPanel not found");
        if (controlsPanel == null) Debug.LogError("ControlsPanel not found");
        if (keybindingsPanel == null) Debug.LogError("KeybindingsPanel not found");
        
        // Set up tab navigation
        if (audioTabButton != null)
            audioTabButton.clicked += () => ShowPanel(audioPanel);
            
        if (graphicsTabButton != null)
            graphicsTabButton.clicked += () => ShowPanel(graphicsPanel);
            
        if (controlsTabButton != null)
            controlsTabButton.clicked += () => ShowPanel(controlsPanel);
            
        if (keybindingsTabButton != null)
            keybindingsTabButton.clicked += () => ShowPanel(keybindingsPanel);
        
        // Set up buttons
        if (applyButton != null)
            applyButton.clicked += ApplySettings;
            
        if (resetButton != null)
            resetButton.clicked += ResetSettings;
            
        if (backButton != null)
            backButton.clicked += GoBack;
            
        if (rebindCancelButton != null)
            rebindCancelButton.clicked += CancelRebinding;
            
        if (closeButton != null)
            closeButton.clicked += GoBack;
            
        if (minimizeButton != null)
            minimizeButton.clicked += () => Debug.Log("Minimize requested (not implemented)");
            
        // Register change events
        RegisterChangeEvents();
        
        // Set up rebind handler
        SetupRebindHandler();
        
        // Populate UI
        PopulateResolutionDropdown();
        PopulateQualityDropdown();
        PopulateKeybindingsList();
        
        // Show initial panel
        ShowPanel(audioPanel);
        
        // Hide rebind overlay
        if (rebindOverlay != null)
            rebindOverlay.style.display = DisplayStyle.None;
            
        Debug.Log("OptionsMenuController UI initialization complete");
    }
    
    private void RegisterChangeEvents()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.RegisterValueChangedCallback(evt => hasUnsavedChanges = true);
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.RegisterValueChangedCallback(evt => hasUnsavedChanges = true);
            
        if (resolutionDropdown != null)
            resolutionDropdown.RegisterValueChangedCallback(evt => hasUnsavedChanges = true);
            
        if (graphicsDropdown != null)
            graphicsDropdown.RegisterValueChangedCallback(evt => hasUnsavedChanges = true);
            
        if (fullscreenToggle != null)
            fullscreenToggle.RegisterValueChangedCallback(evt => hasUnsavedChanges = true);
            
        if (sensitivitySlider != null)
            sensitivitySlider.RegisterValueChangedCallback(evt => hasUnsavedChanges = true);
            
        if (invertYToggle != null)
            invertYToggle.RegisterValueChangedCallback(evt => hasUnsavedChanges = true);
    }
    
    private void SetupRebindHandler()
    {
        if (inputRebinder == null)
        {
            inputRebinder = InputRebinder.Instance;
            if (inputRebinder == null)
            {
                Debug.LogError("Failed to get InputRebinder instance");
                return;
            }
        }
        
        // Initialize input rebinder if needed
        if (playerReference != null && inputRebinder != null)
        {
            PlayerInputActions inputActions = playerReference.GetInputActions();
            if (inputActions != null)
            {
                inputRebinder.Initialize(inputActions);
            }
            else
            {
                Debug.LogWarning("Player input actions are null");
            }
        }
        else
        {
            Debug.LogWarning("Player reference is null: " + (playerReference == null));
        }
        
        // Register events
        if (inputRebinder != null)
        {
            inputRebinder.OnRebindStart -= HandleRebindStart;
            inputRebinder.OnRebindComplete -= HandleRebindComplete;
            inputRebinder.OnRebindCancel -= HandleRebindCancel;
            
            inputRebinder.OnRebindStart += HandleRebindStart;
            inputRebinder.OnRebindComplete += HandleRebindComplete;
            inputRebinder.OnRebindCancel += HandleRebindCancel;
            
            Debug.Log("Rebind handler setup complete");
        }
    }
    
    private void HandleRebindStart()
    {
        rebindingInProgress = true;
        if (rebindOverlay != null)
            rebindOverlay.style.display = DisplayStyle.Flex;
    }
    
    private void HandleRebindComplete(string actionName, string bindingPath)
    {
        rebindingInProgress = false;
        if (rebindOverlay != null)
            rebindOverlay.style.display = DisplayStyle.None;
        
        // Update UI
        PopulateKeybindingsList();
        hasUnsavedChanges = true;
    }
    
    private void HandleRebindCancel()
    {
        rebindingInProgress = false;
        if (rebindOverlay != null)
            rebindOverlay.style.display = DisplayStyle.None;
    }
    
    private void CancelRebinding()
    {
        if (rebindingInProgress && inputRebinder != null)
        {
            // This will trigger the OnRebindCancel event
            // which will hide the overlay and handle cleanup
        }
    }
    
    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
        List<string> options = new List<string>();
        Resolution[] resolutions = Screen.resolutions;
        int currentResolutionIndex = 0;
        
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRate}Hz";
            options.Add(option);
            
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.choices = options;
        
        if (options.Count > 0)
            resolutionDropdown.index = currentResolutionIndex;
    }
    
    private void PopulateQualityDropdown()
    {
        if (graphicsDropdown == null) return;
        
        List<string> options = new List<string>();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            options.Add(QualitySettings.names[i]);
        }
        
        graphicsDropdown.choices = options;
        
        if (options.Count > 0)
            graphicsDropdown.index = QualitySettings.GetQualityLevel();
    }
    
    private void PopulateKeybindingsList()
    {
        if (keybindingsScrollView == null)
        {
            Debug.LogError("KeybindingsScrollView is null");
            return;
        }
        
        if (inputRebinder == null)
        {
            Debug.LogError("InputRebinder is null");
            return;
        }
        
        // Clear existing elements
        keybindingsScrollView.Clear();
        rebindButtons.Clear();
        rebindLabels.Clear();
        
        // Get all bindings
        allBindings = inputRebinder.GetAllBindings();
        
        // Create header for Gameplay controls
        if (allBindings.ContainsKey("Gameplay"))
        {
            AddKeybindingSectionHeader("Gameplay Controls");
            AddKeybindingRows("Gameplay", allBindings["Gameplay"]);
        }
        
        // Create header for UI controls if needed
        if (allBindings.ContainsKey("UI"))
        {
            AddKeybindingSectionHeader("UI Controls");
            AddKeybindingRows("UI", allBindings["UI"]);
        }
    }
    
    private void AddKeybindingSectionHeader(string title)
    {
        VisualElement header = new VisualElement();
        header.AddToClassList("keybinding-section-header");
        
        Label headerLabel = new Label(title);
        headerLabel.AddToClassList("keybinding-section-title");
        header.Add(headerLabel);
        
        keybindingsScrollView.Add(header);
    }
    
    private void AddKeybindingRows(string actionMapName, List<InputRebinder.KeyBindingInfo> bindings)
    {
        foreach (var binding in bindings)
        {
            // Create row for each binding
            VisualElement row = new VisualElement();
            row.AddToClassList("keybinding-row");
            
            // Action name label
            Label actionLabel = new Label(binding.DisplayName);
            actionLabel.AddToClassList("keybinding-action-label");
            
            // Current binding label
            Label bindingLabel = new Label(binding.DisplayString);
            bindingLabel.AddToClassList("keybinding-value-label");
            
            // Rebind button
            Button rebindButton = new Button();
            rebindButton.text = "Rebind";
            rebindButton.AddToClassList("keybinding-rebind-button");
            
            // Store references for later updates
            string keyId = $"{actionMapName}_{binding.ActionName}_{binding.BindingIndex}";
            rebindLabels[keyId] = bindingLabel;
            rebindButtons[keyId] = rebindButton;
            
            // Setup rebind button click
            rebindButton.clicked += () => 
            {
                if (!rebindingInProgress)
                {
                    rebindPromptLabel.text = $"Press a key for '{binding.DisplayName}'";
                    inputRebinder.StartRebinding(actionMapName, binding.ActionName, binding.BindingIndex);
                }
            };
            
            // Add elements to row
            row.Add(actionLabel);
            row.Add(bindingLabel);
            row.Add(rebindButton);
            
            // Add row to list
            keybindingsScrollView.Add(row);
        }
    }
    
    private void SetupInitialValues()
    {
        if (OptionsManager.Instance == null)
        {
            Debug.LogError("OptionsManager.Instance is null");
            return;
        }
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = OptionsManager.Instance.MusicVolume;
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = OptionsManager.Instance.SFXVolume;
            
        if (resolutionDropdown != null && resolutionDropdown.choices.Count > 0)
            resolutionDropdown.index = Mathf.Clamp(OptionsManager.Instance.ResolutionIndex, 0, resolutionDropdown.choices.Count - 1);
            
        if (graphicsDropdown != null && graphicsDropdown.choices.Count > 0)
            graphicsDropdown.index = Mathf.Clamp(OptionsManager.Instance.QualityIndex, 0, graphicsDropdown.choices.Count - 1);
            
        if (fullscreenToggle != null)
            fullscreenToggle.value = OptionsManager.Instance.Fullscreen;
            
        if (sensitivitySlider != null)
            sensitivitySlider.value = OptionsManager.Instance.MouseSensitivity;
            
        if (invertYToggle != null)
            invertYToggle.value = OptionsManager.Instance.InvertYAxis;
            
        // Reset unsaved changes flag after loading values
        hasUnsavedChanges = false;
        
        Debug.Log("Initial values set from OptionsManager");
    }
    
    private void ShowPanel(VisualElement panelToShow)
    {
        if (audioPanel != null)
            audioPanel.style.display = DisplayStyle.None;
            
        if (graphicsPanel != null)
            graphicsPanel.style.display = DisplayStyle.None;
            
        if (controlsPanel != null)
            controlsPanel.style.display = DisplayStyle.None;
            
        if (keybindingsPanel != null)
            keybindingsPanel.style.display = DisplayStyle.None;
        
        if (panelToShow != null)
            panelToShow.style.display = DisplayStyle.Flex;
        
        if (audioTabButton != null)
            audioTabButton.RemoveFromClassList("selected-tab");
            
        if (graphicsTabButton != null)
            graphicsTabButton.RemoveFromClassList("selected-tab");
            
        if (controlsTabButton != null)
            controlsTabButton.RemoveFromClassList("selected-tab");
            
        if (keybindingsTabButton != null)
            keybindingsTabButton.RemoveFromClassList("selected-tab");
        
        if (panelToShow == audioPanel && audioTabButton != null)
            audioTabButton.AddToClassList("selected-tab");
        else if (panelToShow == graphicsPanel && graphicsTabButton != null)
            graphicsTabButton.AddToClassList("selected-tab");
        else if (panelToShow == controlsPanel && controlsTabButton != null)
            controlsTabButton.AddToClassList("selected-tab");
        else if (panelToShow == keybindingsPanel && keybindingsTabButton != null)
            keybindingsTabButton.AddToClassList("selected-tab");
    }
    
    private void ApplySettings()
    {
        if (OptionsManager.Instance == null)
        {
            Debug.LogError("OptionsManager.Instance is null when applying settings");
            return;
        }
        
        // Get current values
        float musicVolume = musicVolumeSlider != null ? musicVolumeSlider.value : 0.75f;
        float sfxVolume = sfxVolumeSlider != null ? sfxVolumeSlider.value : 0.75f;
        int resolutionIndex = resolutionDropdown != null ? resolutionDropdown.index : 0;
        int qualityIndex = graphicsDropdown != null ? graphicsDropdown.index : 0;
        bool fullscreen = fullscreenToggle != null ? fullscreenToggle.value : true;
        float sensitivity = sensitivitySlider != null ? sensitivitySlider.value : 1.0f;
        bool invertY = invertYToggle != null ? invertYToggle.value : false;
        
        // Get the current keybindings
        Dictionary<string, string> keyBindings = new Dictionary<string, string>();
        if (OptionsManager.Instance.KeyBindings != null)
        {
            keyBindings = new Dictionary<string, string>(OptionsManager.Instance.KeyBindings);
        }
        
        // Save settings
        OptionsManager.Instance.SaveSettings(
            musicVolume,
            sfxVolume,
            resolutionIndex,
            qualityIndex,
            fullscreen,
            sensitivity,
            invertY,
            keyBindings
        );
        
        // Apply to player if available
        if (playerReference != null && playerReference.GetInputActions() != null)
        {
            // If we have a player camera component, apply sensitivity
            var playerCamera = FindObjectOfType<PlayerCamera>();
            if (playerCamera != null)
            {
                // You'd implement sensitivity application here
                // playerCamera.SetSensitivity(sensitivity, invertY);
            }
        }
        
        hasUnsavedChanges = false;
        Debug.Log("Settings applied successfully");
        
        // Update status text
        SetStatusText("Settings applied");
    }
    
    private void ResetSettings()
    {
        // Reset audio values
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = 0.75f;
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = 0.75f;
        
        // Reset graphics values
        if (resolutionDropdown != null && resolutionDropdown.choices.Count > 0)
            resolutionDropdown.index = resolutionDropdown.choices.Count - 1;
            
        if (graphicsDropdown != null && graphicsDropdown.choices.Count > 0)
            graphicsDropdown.index = graphicsDropdown.choices.Count - 1;
            
        if (fullscreenToggle != null)
            fullscreenToggle.value = true;
        
        // Reset control values
        if (sensitivitySlider != null)
            sensitivitySlider.value = 1.0f;
            
        if (invertYToggle != null)
            invertYToggle.value = false;
        
        // Notify of changes
        hasUnsavedChanges = true;
        Debug.Log("Settings reset to defaults");
        
        // Update status text
        SetStatusText("Settings reset to defaults");
    }
    
    private void GoBack()
    {
        if (hasUnsavedChanges)
        {
            // In a real implementation, you might want to show a confirmation dialog here
            Debug.Log("Unsaved changes exist when going back to main menu");
        }
        
        // Hide this menu
        gameObject.SetActive(false);
        
        // Show the main menu
        if (mainMenuGameObject != null)
        {
            mainMenuGameObject.SetActive(true);
        }
        else
        {
            // Fallback to finding the main menu
            var mainMenu = FindObjectOfType<MainMenuController>();
            if (mainMenu != null)
            {
                mainMenu.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("Main menu reference not set and could not be found!");
            }
        }
    }
    
    private void SetStatusText(string text)
    {
        if (menuDocument == null) return;
        
        var statusText = menuDocument.rootVisualElement.Q<Label>("StatusText");
        if (statusText != null)
        {
            statusText.text = text;
        }
    }
}