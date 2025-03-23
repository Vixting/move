using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelSelectionUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    
    private ScrollView levelsScrollView;
    private List<Button> levelButtons = new List<Button>();
    
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument not found on LevelSelectionUI");
                return;
            }
        }
        
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        if (uiDocument == null) return;
        
        var root = uiDocument.rootVisualElement;
        
        // Get UI elements
        levelsScrollView = root.Q<ScrollView>("LevelsList");
        
        if (levelsScrollView == null)
        {
            Debug.LogError("Could not find LevelsList ScrollView in UXML");
            return;
        }
        
        // We don't need to populate here as the buttons are already in the UXML
        // but we do need to set up their click handlers
        UpdateLevelButtons();
    }
    
    private void UpdateLevelButtons()
    {
        // Get available levels from LevelManager
        var levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }
        
        // Make sure progress is loaded
        levelManager.LoadProgress();
        int highestUnlockedLevel = levelManager.HighestUnlockedLevel;
        
        // Find all the level buttons in the ScrollView
        levelButtons.Clear();
        
        // Get all buttons that have "LevelButton" in their name
        levelsScrollView.Query<Button>().ForEach(button => {
            if (button.name.Contains("Level"))
            {
                levelButtons.Add(button);
            }
        });
        
        // If no buttons were found, log an error
        if (levelButtons.Count == 0)
        {
            Debug.LogError("No level buttons found in LevelsScrollView");
            return;
        }
        
        // Set up each button
        for (int i = 0; i < levelButtons.Count; i++)
        {
            Button button = levelButtons[i];
            int levelIndex = i; // Capture for lambda
            
            // Clear previous event listeners
            button.clicked -= () => { }; // This doesn't actually clear all listeners
            
            // Enable/disable button based on unlock status
            bool isUnlocked = levelIndex <= highestUnlockedLevel;
            button.SetEnabled(isUnlocked);
            
            if (isUnlocked)
            {
                button.RemoveFromClassList("disabled-button");
                
                // Update level text if it's unlocked
                if (levelIndex > 0) // Skip level 1 which is already named correctly
                {
                    string levelName = levelManager.AvailableLevels.Count > levelIndex 
                        ? levelManager.AvailableLevels[levelIndex].levelName 
                        : $"Level {levelIndex + 1}";
                    
                    button.text = $"Level {levelIndex + 1}: {levelName}";
                }
            }
            else
            {
                button.AddToClassList("disabled-button");
                
                // Keep the "Coming Soon" text for locked levels
                if (!button.text.Contains("Coming Soon"))
                {
                    button.text = $"Level {levelIndex + 1}: Coming Soon";
                }
            }
            
            // Set button action
            button.clicked += () => OnLevelButtonClicked(levelIndex);
        }
    }
    
    private void OnLevelButtonClicked(int levelIndex)
    {
        // Disable all buttons to prevent multiple clicks during loading
        foreach (var button in levelButtons)
        {
            button.SetEnabled(false);
        }
        
        // Load the selected level
        LevelManager.Instance.LoadLevel(levelIndex);
    }
}