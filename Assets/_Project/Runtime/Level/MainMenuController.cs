using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument menuDocument;
    [SerializeField] private LevelManager levelManager;
   
    // UI Elements references
    private VisualElement playPanel;
    private VisualElement levelsPanel;
   
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
   
    public void SetupMenu(UIDocument document, LevelManager manager)
    {
        menuDocument = document;
        levelManager = manager;
        
        if (gameObject.activeInHierarchy)
        {
            InitializeUI();
        }
    }
    
    private void OnEnable()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        if (menuDocument == null) return;
        
        var root = menuDocument.rootVisualElement;
       
        // Get panel references
        playPanel = root.Q<VisualElement>("PlayPanel");
        levelsPanel = root.Q<VisualElement>("LevelsPanel");
       
        // Get button references
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
       
        // Register button event handlers
        if (playButton != null)
            playButton.clicked += () => ShowPanel(playPanel);
            
        if (levelsButton != null)
            levelsButton.clicked += () => ShowPanel(levelsPanel);
            
        if (optionsButton != null)
            optionsButton.clicked += () => SetStatusText("Options not implemented yet");
            
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
       
        // Show default panel
        ShowPanel(playPanel);
        SetStatusText("Ready");
       
        // Unlock cursor for menu
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        // Disable gameplay mode for player if present
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.EnableGameplayMode(false);
        }
        
        // Update level buttons based on progress
        UpdateLevelButtonsState();
    }
    
    private void UpdateLevelButtonsState()
    {
        if (levelManager == null) return;
        
        // Make sure progress is loaded
        levelManager.LoadProgress();
        int highestUnlockedLevel = levelManager.HighestUnlockedLevel;
        
        // Update level buttons based on unlock status
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
        // Hide all panels
        if (playPanel != null)
            playPanel.style.display = DisplayStyle.None;
            
        if (levelsPanel != null)
            levelsPanel.style.display = DisplayStyle.None;
       
        // Show the selected panel
        if (panelToShow != null)
            panelToShow.style.display = DisplayStyle.Flex;
       
        // Update button states
        if (playButton != null)
            playButton.RemoveFromClassList("selected-button");
            
        if (levelsButton != null)
            levelsButton.RemoveFromClassList("selected-button");
            
        if (optionsButton != null)
            optionsButton.RemoveFromClassList("selected-button");
            
        if (creditsButton != null)
            creditsButton.RemoveFromClassList("selected-button");
       
        // Highlight the active button
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
        // This is just visual - you can implement minimize behavior here
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