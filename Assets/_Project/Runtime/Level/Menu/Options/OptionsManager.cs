using UnityEngine;
using System.Collections.Generic;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager Instance { get; private set; }
    
    // Audio settings
    public float MusicVolume { get; private set; } = 0.75f;
    public float SFXVolume { get; private set; } = 0.75f;
    
    // Graphics settings
    public int ResolutionIndex { get; private set; } = 0;
    public int QualityIndex { get; private set; } = 0;
    public bool Fullscreen { get; private set; } = true;
    
    // Control settings
    public float MouseSensitivity { get; private set; } = 1.0f;
    public bool InvertYAxis { get; private set; } = false;
    
    // Keybinding settings
    public Dictionary<string, string> KeyBindings { get; private set; } = new Dictionary<string, string>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void LoadSettings()
    {
        // Load audio settings
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        
        // Load graphics settings
        ResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", Screen.resolutions.Length - 1);
        QualityIndex = PlayerPrefs.GetInt("QualityIndex", QualitySettings.GetQualityLevel());
        Fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        
        // Load control settings
        MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        InvertYAxis = PlayerPrefs.GetInt("InvertYAxis", 0) == 1;
        
        // Load keybindings
        KeyBindings = new Dictionary<string, string>();
        
        // Load default keybindings or from PlayerPrefs
        LoadKeyBinding("Move_Up", "<Keyboard>/w");
        LoadKeyBinding("Move_Down", "<Keyboard>/s");
        LoadKeyBinding("Move_Left", "<Keyboard>/a");
        LoadKeyBinding("Move_Right", "<Keyboard>/d");
        LoadKeyBinding("Jump", "<Keyboard>/space");
        LoadKeyBinding("Crouch", "<Keyboard>/leftCtrl");
        LoadKeyBinding("Fire", "<Mouse>/leftButton");
        LoadKeyBinding("Aim", "<Mouse>/rightButton");
        LoadKeyBinding("Reload", "<Keyboard>/r");
        LoadKeyBinding("Interact", "<Keyboard>/e");
        LoadKeyBinding("Inventory", "<Keyboard>/i");
        
        // Apply loaded settings
        ApplySettings();
    }
    
    private void LoadKeyBinding(string actionName, string defaultBinding)
    {
        string savedBinding = PlayerPrefs.GetString("KeyBinding_" + actionName, defaultBinding);
        KeyBindings[actionName] = savedBinding;
    }
    
    public void SaveSettings(float musicVolume, float sfxVolume, int resolutionIndex, int qualityIndex, bool fullscreen, 
                            float mouseSensitivity, bool invertYAxis, Dictionary<string, string> keyBindings)
    {
        // Store new values
        MusicVolume = musicVolume;
        SFXVolume = sfxVolume;
        ResolutionIndex = resolutionIndex;
        QualityIndex = qualityIndex;
        Fullscreen = fullscreen;
        MouseSensitivity = mouseSensitivity;
        InvertYAxis = invertYAxis;
        KeyBindings = new Dictionary<string, string>(keyBindings);
        
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.SetInt("ResolutionIndex", ResolutionIndex);
        PlayerPrefs.SetInt("QualityIndex", QualityIndex);
        PlayerPrefs.SetInt("Fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.SetFloat("MouseSensitivity", MouseSensitivity);
        PlayerPrefs.SetInt("InvertYAxis", InvertYAxis ? 1 : 0);
        
        // Save keybindings
        foreach (var binding in KeyBindings)
        {
            PlayerPrefs.SetString("KeyBinding_" + binding.Key, binding.Value);
        }
        
        PlayerPrefs.Save();
        
        // Apply new settings
        ApplySettings();
    }
    
    private void ApplySettings()
    {
        // Apply graphics settings
        if (ResolutionIndex >= 0 && ResolutionIndex < Screen.resolutions.Length)
        {
            Resolution resolution = Screen.resolutions[ResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Fullscreen);
        }
        
        if (QualityIndex >= 0 && QualityIndex < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(QualityIndex);
        }
        
        // Note: KeyBindings will be applied through the InputRebinder when game starts
    }
    
    public void SetKeyBinding(string actionName, string bindingPath)
    {
        KeyBindings[actionName] = bindingPath;
        PlayerPrefs.SetString("KeyBinding_" + actionName, bindingPath);
        PlayerPrefs.Save();
    }
}