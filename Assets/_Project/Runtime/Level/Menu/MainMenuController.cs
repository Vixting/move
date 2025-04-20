using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument menuDocument;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private InputRebinder inputRebinder;
    
    private VisualElement playPanel;
    private VisualElement levelsPanel;
    private VisualElement optionsPanel;
    
    private Button playButton;
    private Button levelsButton;
    private Button optionsButton;
    private Button creditsButton;
    private Button exitButton;
    private Button quickStartButton;
    private Button level1Button;
    private Button level2Button;
    private Button level3Button;
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
    
    private bool rebindingInProgress = false;
    private bool hasUnsavedChanges = false;
    private bool optionsInitialized = false;
    
    private Player playerReference;
    
    private void Awake()
    {
        UnlockCursor();
        
        if (inputRebinder == null)
        {
            inputRebinder = InputRebinder.Instance;
        }
    }
    
    public void SetupMenu(UIDocument document, LevelManager manager)
    {
        menuDocument = document;
        levelManager = manager;
        
        if (gameObject.activeInHierarchy)
        {
            InitializeUI();
            UnlockCursor();
        }
    }
    
    private void OnEnable()
    {
        InitializeUI();
        UnlockCursor();
        DisablePlayerInput();
    }
    
    private void Update()
    {
        if (UnityEngine.Cursor.lockState != CursorLockMode.None)
        {
            UnlockCursor();
        }
    }
    
    private void UnlockCursor()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
    
    private void DisablePlayerInput()
    {
        playerReference = FindObjectOfType<Player>();
        if (playerReference != null)
        {
            playerReference.EnableGameplayMode(false);
        }
    }
    
    private void InitializeUI()
    {
        if (menuDocument == null) return;
        
        var root = menuDocument.rootVisualElement;
        
        playPanel = root.Q<VisualElement>("PlayPanel");
        levelsPanel = root.Q<VisualElement>("LevelsPanel");
        optionsPanel = root.Q<VisualElement>("OptionsPanel");
        
        playButton = root.Q<Button>("PlayButton");
        levelsButton = root.Q<Button>("LevelsButton");
        optionsButton = root.Q<Button>("OptionsButton");
        creditsButton = root.Q<Button>("CreditsButton");
        exitButton = root.Q<Button>("ExitButton");
        quickStartButton = root.Q<Button>("QuickStartButton");
        level1Button = root.Q<Button>("Level1Button");
        level2Button = root.Q<Button>("Level2Button");
        level3Button = root.Q<Button>("Level3Button");
        closeButton = root.Q<Button>("CloseButton");
        minimizeButton = root.Q<Button>("MinimizeButton");
        
        if (playButton != null)
            playButton.clicked += () => ShowPanel(playPanel);
            
        if (levelsButton != null)
            levelsButton.clicked += () => ShowPanel(levelsPanel);
            
        if (optionsButton != null)
            optionsButton.clicked += () => ShowOptionsMenu();
            
        if (creditsButton != null)
            creditsButton.clicked += () => SetStatusText("Credits not implemented yet");
            
        if (exitButton != null)
            exitButton.clicked += ExitGame;
        
        if (quickStartButton != null)
            quickStartButton.clicked += () => StartGame(0);
            
        if (level1Button != null)
            level1Button.clicked += () => StartGame(0);
            
        if (level2Button != null && level2Button.enabledSelf)
            level2Button.clicked += () => StartGame(1);
            
        if (level3Button != null && level3Button.enabledSelf)
            level3Button.clicked += () => StartGame(2);
            
        if (closeButton != null)
            closeButton.clicked += ExitGame;
            
        if (minimizeButton != null)
            minimizeButton.clicked += MinimizeWindow;
        
        ShowPanel(playPanel);
        SetStatusText("Ready");
        
        UpdateLevelButtonsState();
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
        var root = menuDocument.rootVisualElement;
        
        audioPanel = root.Q<VisualElement>("AudioPanel");
        graphicsPanel = root.Q<VisualElement>("GraphicsPanel");
        controlsPanel = root.Q<VisualElement>("ControlsPanel");
        keybindingsPanel = root.Q<VisualElement>("KeybindingsPanel");
        
        audioTabButton = root.Q<Button>("AudioTabButton");
        graphicsTabButton = root.Q<Button>("GraphicsTabButton");
        controlsTabButton = root.Q<Button>("ControlsTabButton");
        keybindingsTabButton = root.Q<Button>("KeybindingsTabButton");
        
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
        
        rebindOverlay = root.Q<VisualElement>("RebindOverlay");
        rebindPromptLabel = root.Q<Label>("RebindPromptLabel");
        rebindCancelButton = root.Q<Button>("RebindCancelButton");
        
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
            backOptionsButton.clicked += () => ShowPanel(playPanel);
            
        if (rebindCancelButton != null)
            rebindCancelButton.clicked += CancelRebinding;
        
        RegisterOptionsChangeEvents();
        SetupRebindHandler();
        
        PopulateResolutionDropdown();
        PopulateQualityDropdown();
        PopulateKeybindingsList();
        
        ShowOptionsTab(audioPanel);
        
        if (rebindOverlay != null)
            rebindOverlay.style.display = DisplayStyle.None;
        
        SetupInitialValues();
    }
    
    private void RegisterOptionsChangeEvents()
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
            inputRebinder.OnRebindStart -= HandleRebindStart;
            inputRebinder.OnRebindComplete -= HandleRebindComplete;
            inputRebinder.OnRebindCancel -= HandleRebindCancel;
            
            inputRebinder.OnRebindStart += HandleRebindStart;
            inputRebinder.OnRebindComplete += HandleRebindComplete;
            inputRebinder.OnRebindCancel += HandleRebindCancel;
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
        if (keybindingsScrollView == null || inputRebinder == null) return;
        
        keybindingsScrollView.Clear();
        rebindButtons.Clear();
        rebindLabels.Clear();
        
        allBindings = inputRebinder.GetAllBindings();
        
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
                if (!rebindingInProgress)
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
    
    private void SetupInitialValues()
    {
        if (OptionsManager.Instance == null) return;
        
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
            
        hasUnsavedChanges = false;
    }
    
    private void UpdateLevelButtonsState()
    {
        if (levelManager == null) return;
        
        levelManager.LoadProgress();
        int highestUnlockedLevel = levelManager.HighestUnlockedLevel;
        
        if (level1Button != null)
        {
            level1Button.SetEnabled(0 <= highestUnlockedLevel);
        }
        
        if (level2Button != null)
        {
            bool isUnlocked = 1 <= highestUnlockedLevel;
            level2Button.SetEnabled(isUnlocked);
            
            if (isUnlocked)
            {
                level2Button.RemoveFromClassList("disabled-button");
                level2Button.text = "Level 2: Adventure";
            }
            else
            {
                level2Button.AddToClassList("disabled-button");
                level2Button.text = "Level 2: Coming Soon";
            }
        }
        
        if (level3Button != null)
        {
            bool isUnlocked = 2 <= highestUnlockedLevel;
            level3Button.SetEnabled(isUnlocked);
            
            if (isUnlocked)
            {
                level3Button.RemoveFromClassList("disabled-button");
                level3Button.text = "Level 3: Challenge";
            }
            else
            {
                level3Button.AddToClassList("disabled-button");
                level3Button.text = "Level 3: Coming Soon";
            }
        }
    }
    
    private void ShowPanel(VisualElement panelToShow)
    {
        if (playPanel != null)
            playPanel.style.display = DisplayStyle.None;
            
        if (levelsPanel != null)
            levelsPanel.style.display = DisplayStyle.None;
            
        if (optionsPanel != null)
            optionsPanel.style.display = DisplayStyle.None;
        
        if (panelToShow != null)
            panelToShow.style.display = DisplayStyle.Flex;
        
        if (playButton != null)
            playButton.RemoveFromClassList("selected-button");
            
        if (levelsButton != null)
            levelsButton.RemoveFromClassList("selected-button");
            
        if (optionsButton != null)
            optionsButton.RemoveFromClassList("selected-button");
            
        if (creditsButton != null)
            creditsButton.RemoveFromClassList("selected-button");
        
        if (panelToShow == playPanel && playButton != null)
        {
            playButton.AddToClassList("selected-button");
            SetStatusText("Main Menu");
        }
        else if (panelToShow == levelsPanel && levelsButton != null)
        {
            levelsButton.AddToClassList("selected-button");
            SetStatusText("Select a level to play");
        }
        else if (panelToShow == optionsPanel && optionsButton != null)
        {
            optionsButton.AddToClassList("selected-button");
            SetStatusText("Game Options");
        }
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
        
        Dictionary<string, string> keyBindings = new Dictionary<string, string>();
        if (OptionsManager.Instance.KeyBindings != null)
        {
            keyBindings = new Dictionary<string, string>(OptionsManager.Instance.KeyBindings);
        }
        
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
        
        if (playerReference != null && playerReference.GetInputActions() != null)
        {
            var playerCamera = FindObjectOfType<PlayerCamera>();
            if (playerCamera != null)
            {
            }
        }
        
        hasUnsavedChanges = false;
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
        
        hasUnsavedChanges = true;
        SetStatusText("Settings reset to defaults");
    }
    
    private void StartGame(int levelIndex)
    {
        if (levelManager != null)
        {
            SetStatusText("Loading level...");
            levelManager.LoadLevel(levelIndex);
        }
        else
        {
            Debug.LogError("LevelManager reference not set in MainMenuController");
            SetStatusText("Error: LevelManager not found");
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
    
    private void MinimizeWindow()
    {
        SetStatusText("Minimize requested (not implemented)");
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