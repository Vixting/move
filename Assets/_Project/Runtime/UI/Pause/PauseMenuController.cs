using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using InventorySystem;
using System.Collections;
using System.Collections.Generic;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument pauseMenuDocument;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private InputRebinder inputRebinder;
    [SerializeField] private bool dontDestroyOnLoad = true;
    
    private VisualElement resumePanel;
    private VisualElement optionsPanel;
    
    private Button resumeButton;
    private Button inventoryButton;
    private Button optionsButton;
    private Button mainMenuButton;
    private Button exitButton;
    private Button quickResumeButton;
    private Button closeButton;
    private Button minimizeButton;
    
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
    private Button backOptionsButton;
    
    private VisualElement rebindOverlay;
    private Label rebindPromptLabel;
    private Button rebindCancelButton;
    
    private Dictionary<string, VisualElement> rebindButtons = new Dictionary<string, VisualElement>();
    private Dictionary<string, Label> rebindLabels = new Dictionary<string, Label>();
    
    private Dictionary<string, List<InputRebinder.KeyBindingInfo>> allBindings;
    
    private bool optionsInitialized = false;
    private bool isPauseMenuActive = false;
    private bool rebindingInProgress = false;
    
    private Player playerReference;
    
    public static PauseMenuController Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Make this object persist between scene loads if configured to do so
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
        
        if (inputRebinder == null)
        {
            inputRebinder = InputRebinder.Instance;
        }
        
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }
        
        if (pauseMenuDocument != null)
        {
            pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
        
        // Subscribe to scene load events to update references after scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from scene load events when destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Hide pause menu when a new scene is loaded
        if (isPauseMenuActive)
        {
            HidePauseMenu();
        }
        
        // Update references after scene change
        StartCoroutine(UpdateReferencesAfterSceneLoad());
    }
    
    private IEnumerator UpdateReferencesAfterSceneLoad()
    {
        // Wait for a frame to ensure all objects are loaded
        yield return null;
        
        // Update levelManager reference if needed
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }
        
        // Update player reference
        playerReference = FindObjectOfType<Player>();
        
        // Re-initialize UI if needed (especially when UI document is part of the canvas)
        if (pauseMenuDocument == null)
        {
            // Try to find the UI document in the scene
            UIDocument[] documents = FindObjectsOfType<UIDocument>();
            foreach (UIDocument doc in documents)
            {
                if (doc.name.Contains("PauseMenu") || doc.visualTreeAsset != null && doc.visualTreeAsset.name.Contains("PauseMenu"))
                {
                    pauseMenuDocument = doc;
                    break;
                }
            }
            
            if (pauseMenuDocument != null)
            {
                InitializeUI();
            }
        }
    }
    
    private void Start()
    {
        playerReference = FindObjectOfType<Player>();
        InitializeUI();
    }
    
    public void ShowPauseMenu()
    {
        if (pauseMenuDocument == null) return;
        
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        isPauseMenuActive = true;
        
        UnlockCursor();
        
        if (playerReference != null)
        {
            playerReference.EnableGameplayMode(false);
        }
        
        Time.timeScale = 0;
        
        ShowPanel(resumePanel);
        SetStatusText("Game Paused");
    }
    
    public void HidePauseMenu()
    {
        if (pauseMenuDocument == null) return;
        
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
        isPauseMenuActive = false;
        
        if (playerReference != null)
        {
            playerReference.EnableGameplayMode(true);
        }
        
        Time.timeScale = 1;
    }
    
    public bool IsPauseMenuActive()
    {
        return isPauseMenuActive;
    }
    
    private void InitializeUI()
    {
        if (pauseMenuDocument == null) return;
        
        var root = pauseMenuDocument.rootVisualElement;
        
        resumePanel = root.Q<VisualElement>("ResumePanel");
        optionsPanel = root.Q<VisualElement>("OptionsPanel");
        
        resumeButton = root.Q<Button>("ResumeButton");
        inventoryButton = root.Q<Button>("InventoryButton");
        optionsButton = root.Q<Button>("OptionsButton");
        mainMenuButton = root.Q<Button>("MainMenuButton");
        exitButton = root.Q<Button>("ExitButton");
        quickResumeButton = root.Q<Button>("QuickResumeButton");
        closeButton = root.Q<Button>("CloseButton");
        minimizeButton = root.Q<Button>("MinimizeButton");
        
        // Initialize the rebind overlay
        rebindOverlay = root.Q<VisualElement>("RebindOverlay");
        rebindPromptLabel = root.Q<Label>("RebindPromptLabel");
        rebindCancelButton = root.Q<Button>("RebindCancelButton");
        
        if (rebindOverlay != null)
        {
            rebindOverlay.style.display = DisplayStyle.None;
        }
        
        if (resumeButton != null)
            resumeButton.clicked += ResumeGame;
            
        if (inventoryButton != null)
            inventoryButton.clicked += OpenInventory;
            
        if (optionsButton != null)
            optionsButton.clicked += ShowOptionsMenu;
            
        if (mainMenuButton != null)
            mainMenuButton.clicked += ReturnToMainMenu;
            
        if (exitButton != null)
            exitButton.clicked += ExitGame;
        
        if (quickResumeButton != null)
            quickResumeButton.clicked += ResumeGame;
            
        if (closeButton != null)
            closeButton.clicked += ResumeGame;
            
        if (minimizeButton != null)
            minimizeButton.clicked += ResumeGame;
            
        if (rebindCancelButton != null)
            rebindCancelButton.clicked += CancelRebinding;
    }
    
    private void ShowOptionsMenu()
    {
        ShowPanel(optionsPanel);
        SetStatusText("Options Menu");
        
        if (!optionsInitialized)
        {
            InitializeOptionsUI();
            optionsInitialized = true;
        }
    }
    
    private void InitializeOptionsUI()
    {
        var root = pauseMenuDocument.rootVisualElement;
        
        audioTabButton = root.Q<Button>("AudioTabButton");
        graphicsTabButton = root.Q<Button>("GraphicsTabButton");
        controlsTabButton = root.Q<Button>("ControlsTabButton");
        keybindingsTabButton = root.Q<Button>("KeybindingsTabButton");
        
        audioPanel = root.Q<VisualElement>("AudioPanel");
        graphicsPanel = root.Q<VisualElement>("GraphicsPanel");
        controlsPanel = root.Q<VisualElement>("ControlsPanel");
        keybindingsPanel = root.Q<VisualElement>("KeybindingsPanel");
        
        musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
        sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");
        resolutionDropdown = root.Q<DropdownField>("ResolutionDropdown");
        graphicsDropdown = root.Q<DropdownField>("GraphicsDropdown");
        fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        sensitivitySlider = root.Q<Slider>("SensitivitySlider");
        invertYToggle = root.Q<Toggle>("InvertYToggle");
        
        keybindingsScrollView = root.Q<ScrollView>("KeybindingsScrollView");
        
        applyButton = root.Q<Button>("ApplyButton");
        resetButton = root.Q<Button>("ResetButton");
        backOptionsButton = root.Q<Button>("BackOptionsButton");
        
        if (audioTabButton != null)
            audioTabButton.clicked += () => ShowOptionsTab(audioPanel);
            
        if (graphicsTabButton != null)
            graphicsTabButton.clicked += () => ShowOptionsTab(graphicsPanel);
            
        if (controlsTabButton != null)
            controlsTabButton.clicked += () => ShowOptionsTab(controlsPanel);
            
        if (keybindingsTabButton != null)
            keybindingsTabButton.clicked += () => ShowOptionsTab(keybindingsPanel);
        
        if (applyButton != null)
            applyButton.clicked += ApplySettings;
            
        if (resetButton != null)
            resetButton.clicked += ResetSettings;
            
        if (backOptionsButton != null)
            backOptionsButton.clicked += () => ShowPanel(resumePanel);
        
        if (OptionsManager.Instance != null)
        {
            LoadCurrentSettings();
        }
        
        PopulateDropdowns();
        SetupRebindHandler();
        PopulateKeybindingsList();
    }
    
    private void SetupRebindHandler()
    {
        if (inputRebinder == null)
        {
            inputRebinder = InputRebinder.Instance;
            if (inputRebinder == null) return;
        }
        
        if (playerReference != null && inputRebinder != null)
        {
            PlayerInputActions inputActions = playerReference.GetInputActions();
            if (inputActions != null)
            {
                inputRebinder.Initialize(inputActions);
            }
        }
        
        if (inputRebinder != null)
        {
            // Remove any existing event handlers to prevent duplicates
            inputRebinder.OnRebindStart -= HandleRebindStart;
            inputRebinder.OnRebindComplete -= HandleRebindComplete;
            inputRebinder.OnRebindCancel -= HandleRebindCancel;
            
            // Register new handlers
            inputRebinder.OnRebindStart += HandleRebindStart;
            inputRebinder.OnRebindComplete += HandleRebindComplete;
            inputRebinder.OnRebindCancel += HandleRebindCancel;
        }
    }
    
    private void HandleRebindStart()
    {
        rebindingInProgress = true;
        if (rebindOverlay != null)
        {
            rebindOverlay.style.display = DisplayStyle.Flex;
        }
    }
    
    private void HandleRebindComplete(string actionName, string bindingPath)
    {
        rebindingInProgress = false;
        if (rebindOverlay != null)
        {
            rebindOverlay.style.display = DisplayStyle.None;
        }
        
        PopulateKeybindingsList();
    }
    
    private void HandleRebindCancel()
    {
        rebindingInProgress = false;
        if (rebindOverlay != null)
        {
            rebindOverlay.style.display = DisplayStyle.None;
        }
    }
    
    private void CancelRebinding()
    {
        if (rebindingInProgress && inputRebinder != null)
        {
            inputRebinder.CancelRebinding();
            rebindingInProgress = false;
            if (rebindOverlay != null)
            {
                rebindOverlay.style.display = DisplayStyle.None;
            }
        }
    }
    
    private void PopulateDropdowns()
    {
        if (resolutionDropdown != null)
        {
            resolutionDropdown.choices.Clear();
            
            Resolution[] resolutions = Screen.resolutions;
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRate}Hz";
                resolutionDropdown.choices.Add(option);
            }
            
            if (resolutionDropdown.choices.Count > 0)
            {
                resolutionDropdown.index = resolutionDropdown.choices.Count - 1;
            }
        }
        
        if (graphicsDropdown != null)
        {
            graphicsDropdown.choices.Clear();
            
            string[] qualityNames = QualitySettings.names;
            for (int i = 0; i < qualityNames.Length; i++)
            {
                graphicsDropdown.choices.Add(qualityNames[i]);
            }
            
            if (graphicsDropdown.choices.Count > 0)
            {
                graphicsDropdown.index = QualitySettings.GetQualityLevel();
            }
        }
    }
    
    private void PopulateKeybindingsList()
    {
        if (keybindingsScrollView == null || inputRebinder == null) return;
        
        keybindingsScrollView.Clear();
        rebindButtons.Clear();
        rebindLabels.Clear();
        
        allBindings = inputRebinder.GetAllBindings();
        
        if (allBindings != null)
        {
            if (allBindings.ContainsKey("Gameplay"))
            {
                AddKeybindingSectionHeader("Gameplay Controls");
                AddKeybindingRows("Gameplay", allBindings["Gameplay"]);
            }
            
            if (allBindings.ContainsKey("UI"))
            {
                AddKeybindingSectionHeader("UI Controls");
                AddKeybindingRows("UI", allBindings["UI"]);
            }
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
            VisualElement row = new VisualElement();
            row.AddToClassList("keybinding-row");
            
            Label actionLabel = new Label(binding.DisplayName);
            actionLabel.AddToClassList("keybinding-action-label");
            
            Label bindingLabel = new Label(binding.DisplayString);
            bindingLabel.AddToClassList("keybinding-value-label");
            
            Button rebindButton = new Button();
            rebindButton.text = "Rebind";
            rebindButton.AddToClassList("keybinding-rebind-button");
            
            string keyId = $"{actionMapName}_{binding.ActionName}_{binding.BindingIndex}";
            rebindLabels[keyId] = bindingLabel;
            rebindButtons[keyId] = rebindButton;
            
            rebindButton.clicked += () => 
            {
                if (!rebindingInProgress && inputRebinder != null)
                {
                    rebindPromptLabel.text = $"Press a key for '{binding.DisplayName}'";
                    inputRebinder.StartRebinding(actionMapName, binding.ActionName, binding.BindingIndex);
                }
            };
            
            row.Add(actionLabel);
            row.Add(bindingLabel);
            row.Add(rebindButton);
            
            keybindingsScrollView.Add(row);
        }
    }
    
    private void LoadCurrentSettings()
    {
        if (OptionsManager.Instance == null) return;
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = OptionsManager.Instance.MusicVolume;
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = OptionsManager.Instance.SFXVolume;
            
        if (sensitivitySlider != null)
            sensitivitySlider.value = OptionsManager.Instance.MouseSensitivity;
            
        if (invertYToggle != null)
            invertYToggle.value = OptionsManager.Instance.InvertYAxis;
        
        if (resolutionDropdown != null && resolutionDropdown.choices.Count > 0)
            resolutionDropdown.index = Mathf.Clamp(OptionsManager.Instance.ResolutionIndex, 0, resolutionDropdown.choices.Count - 1);
            
        if (graphicsDropdown != null && graphicsDropdown.choices.Count > 0)
            graphicsDropdown.index = Mathf.Clamp(OptionsManager.Instance.QualityIndex, 0, graphicsDropdown.choices.Count - 1);
            
        if (fullscreenToggle != null)
            fullscreenToggle.value = OptionsManager.Instance.Fullscreen;
    }
    
    private void ShowOptionsTab(VisualElement tabToShow)
    {
        if (audioPanel != null)
            audioPanel.style.display = DisplayStyle.None;
            
        if (graphicsPanel != null)
            graphicsPanel.style.display = DisplayStyle.None;
            
        if (controlsPanel != null)
            controlsPanel.style.display = DisplayStyle.None;
            
        if (keybindingsPanel != null)
            keybindingsPanel.style.display = DisplayStyle.None;
        
        if (tabToShow != null)
            tabToShow.style.display = DisplayStyle.Flex;
        
        if (audioTabButton != null)
            audioTabButton.RemoveFromClassList("selected-tab");
            
        if (graphicsTabButton != null)
            graphicsTabButton.RemoveFromClassList("selected-tab");
            
        if (controlsTabButton != null)
            controlsTabButton.RemoveFromClassList("selected-tab");
            
        if (keybindingsTabButton != null)
            keybindingsTabButton.RemoveFromClassList("selected-tab");
        
        if (tabToShow == audioPanel && audioTabButton != null)
            audioTabButton.AddToClassList("selected-tab");
        else if (tabToShow == graphicsPanel && graphicsTabButton != null)
            graphicsTabButton.AddToClassList("selected-tab");
        else if (tabToShow == controlsPanel && controlsTabButton != null)
            controlsTabButton.AddToClassList("selected-tab");
        else if (tabToShow == keybindingsPanel && keybindingsTabButton != null)
            keybindingsTabButton.AddToClassList("selected-tab");
    }
    
    private void ApplySettings()
    {
        if (OptionsManager.Instance == null) return;
        
        float musicVolume = musicVolumeSlider != null ? musicVolumeSlider.value : 0.75f;
        float sfxVolume = sfxVolumeSlider != null ? sfxVolumeSlider.value : 0.75f;
        int resolutionIndex = resolutionDropdown != null ? resolutionDropdown.index : 0;
        int qualityIndex = graphicsDropdown != null ? graphicsDropdown.index : 0;
        bool fullscreen = fullscreenToggle != null ? fullscreenToggle.value : true;
        float sensitivity = sensitivitySlider != null ? sensitivitySlider.value : 1.0f;
        bool invertY = invertYToggle != null ? invertYToggle.value : false;
        
        OptionsManager.Instance.SaveSettings(
            musicVolume,
            sfxVolume,
            resolutionIndex,
            qualityIndex,
            fullscreen,
            sensitivity,
            invertY,
            OptionsManager.Instance.KeyBindings
        );
        
        SetStatusText("Settings applied");
    }
    
    private void ResetSettings()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = 0.75f;
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = 0.75f;
        
        if (resolutionDropdown != null && resolutionDropdown.choices.Count > 0)
            resolutionDropdown.index = resolutionDropdown.choices.Count - 1;
            
        if (graphicsDropdown != null && graphicsDropdown.choices.Count > 0)
            graphicsDropdown.index = graphicsDropdown.choices.Count - 1;
            
        if (fullscreenToggle != null)
            fullscreenToggle.value = true;
        
        if (sensitivitySlider != null)
            sensitivitySlider.value = 1.0f;
            
        if (invertYToggle != null)
            invertYToggle.value = false;
        
        SetStatusText("Settings reset to defaults");
    }
    
    private void ShowPanel(VisualElement panelToShow)
    {
        if (resumePanel != null)
            resumePanel.style.display = DisplayStyle.None;
            
        if (optionsPanel != null)
            optionsPanel.style.display = DisplayStyle.None;
        
        if (panelToShow != null)
            panelToShow.style.display = DisplayStyle.Flex;
        
        if (resumeButton != null)
            resumeButton.RemoveFromClassList("selected-button");
            
        if (optionsButton != null)
            optionsButton.RemoveFromClassList("selected-button");
        
        if (panelToShow == resumePanel && resumeButton != null)
        {
            resumeButton.AddToClassList("selected-button");
            SetStatusText("Game Paused");
        }
        else if (panelToShow == optionsPanel && optionsButton != null)
        {
            optionsButton.AddToClassList("selected-button");
            SetStatusText("Game Options");
        }
    }
    
    private void SetStatusText(string text)
    {
        if (pauseMenuDocument == null) return;
        
        var statusText = pauseMenuDocument.rootVisualElement.Q<Label>("StatusText");
        if (statusText != null)
        {
            statusText.text = text;
        }
    }
    
    private void UnlockCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
    
    private void ResumeGame()
    {
        HidePauseMenu();
    }
    
    private void OpenInventory()
    {
        HidePauseMenu();
        
        InventoryManager inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.SetInventoryMode(InventoryMode.NoStash);
            inventoryManager.ShowInventory();
        }
    }
    
    private void ReturnToMainMenu()
    {
        Time.timeScale = 1;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }
        
        if (levelManager != null)
        {
            levelManager.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
    
    private void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}