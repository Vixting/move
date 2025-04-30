using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class InputRebinder : MonoBehaviour
{
    private static InputRebinder _instance;
    public static InputRebinder Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InputRebinder>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("InputRebinder");
                    _instance = obj.AddComponent<InputRebinder>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }
    
    private PlayerInputActions _inputActions;
    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
    private string _actionMapName;
    private string _actionName;
    private int _bindingIndex;
    
    public delegate void RebindStartEvent();
    public delegate void RebindCompleteEvent(string actionName, string bindingPath);
    public delegate void RebindCancelEvent();

    public event RebindStartEvent OnRebindStart;
    public event RebindCompleteEvent OnRebindComplete;
    public event RebindCancelEvent OnRebindCancel;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void Initialize(PlayerInputActions inputActions)
    {
        _inputActions = inputActions;
        ApplySavedBindings();
    }
    
    public void ApplySavedBindings()
    {
        if (_inputActions == null || OptionsManager.Instance == null) return;
        
        var bindings = OptionsManager.Instance.KeyBindings;
        if (bindings == null || bindings.Count == 0) return;
        
        foreach (var actionMap in _inputActions.asset.actionMaps)
        {
            foreach (var action in actionMap.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    string compositeKey = action.bindings[i].isComposite ? $"{action.name}_Composite" : action.name;
                    string partKey = action.bindings[i].isPartOfComposite ? $"{action.name}_{action.bindings[i].name}" : action.name;
                    
                    if (action.bindings[i].isPartOfComposite)
                    {
                        string key = $"{partKey}";
                        if (bindings.TryGetValue(key, out string bindingPath))
                        {
                            action.ApplyBindingOverride(i, bindingPath);
                        }
                    }
                    else if (!action.bindings[i].isComposite)
                    {
                        string key = $"{compositeKey}";
                        if (bindings.TryGetValue(key, out string bindingPath))
                        {
                            action.ApplyBindingOverride(i, bindingPath);
                        }
                    }
                }
            }
        }
    }
    
    public string GetBindingPath(string actionMapName, string actionName, int bindingIndex = 0)
    {
        InputActionMap actionMap = _inputActions.asset.FindActionMap(actionMapName);
        if (actionMap == null) return string.Empty;
        
        InputAction action = actionMap.FindAction(actionName);
        if (action == null) return string.Empty;
        
        if (bindingIndex >= action.bindings.Count) return string.Empty;
        
        return action.bindings[bindingIndex].effectivePath;
    }
    
    public string GetBindingDisplayString(string actionMapName, string actionName, int bindingIndex = 0)
    {
        InputActionMap actionMap = _inputActions.asset.FindActionMap(actionMapName);
        if (actionMap == null) return string.Empty;
        
        InputAction action = actionMap.FindAction(actionName);
        if (action == null) return string.Empty;
        
        if (bindingIndex >= action.bindings.Count) return string.Empty;
        
        return InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
    
    public void StartRebinding(string actionMapName, string actionName, int bindingIndex = 0)
    {
        _actionMapName = actionMapName;
        _actionName = actionName;
        _bindingIndex = bindingIndex;
        
        InputActionMap actionMap = _inputActions.asset.FindActionMap(actionMapName);
        if (actionMap == null) return;
        
        InputAction action = actionMap.FindAction(actionName);
        if (action == null) return;
        
        if (bindingIndex >= action.bindings.Count) return;
        
        // Disable the action map temporarily
        actionMap.Disable();
        
        OnRebindStart?.Invoke();
        
        _rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation => OnRebindCanceled())
            .OnComplete(operation => OnRebindFinished())
            .Start();
    }
    
    // Add method to cancel an in-progress rebinding
    public void CancelRebinding()
    {
        if (_rebindingOperation != null)
        {
            _rebindingOperation.Cancel();
        }
    }
    
    private void OnRebindFinished()
    {
        if (_rebindingOperation == null) return;
        
        InputActionMap actionMap = _inputActions.asset.FindActionMap(_actionMapName);
        if (actionMap == null) return;
        
        InputAction action = actionMap.FindAction(_actionName);
        if (action == null) return;
        
        // Get the new binding path
        string newBindingPath = action.bindings[_bindingIndex].effectivePath;
        
        // Determine the key name based on whether it's part of a composite
        string keyName = action.bindings[_bindingIndex].isPartOfComposite ? 
            $"{_actionName}_{action.bindings[_bindingIndex].name}" : 
            _actionName;
        
        // Update the binding in options manager
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetKeyBinding(keyName, newBindingPath);
        }
        
        // Clean up
        _rebindingOperation.Dispose();
        _rebindingOperation = null;
        
        // Re-enable the action map
        actionMap.Enable();
        
        OnRebindComplete?.Invoke(_actionName, newBindingPath);
    }
    
    private void OnRebindCanceled()
    {
        if (_rebindingOperation == null) return;
        
        InputActionMap actionMap = _inputActions.asset.FindActionMap(_actionMapName);
        if (actionMap == null) return;
        
        // Clean up
        _rebindingOperation.Dispose();
        _rebindingOperation = null;
        
        // Re-enable the action map
        actionMap.Enable();
        
        OnRebindCancel?.Invoke();
    }
    
    public Dictionary<string, List<KeyBindingInfo>> GetAllBindings()
    {
        Dictionary<string, List<KeyBindingInfo>> result = new Dictionary<string, List<KeyBindingInfo>>();
        
        if (_inputActions == null || _inputActions.asset == null)
            return result;
            
        foreach (var actionMap in _inputActions.asset.actionMaps)
        {
            List<KeyBindingInfo> bindings = new List<KeyBindingInfo>();
            
            foreach (var action in actionMap.actions)
            {
                // Skip UI actions for rebinding
                if (actionMap.name == "UI" && action.name != "InventoryClose")
                    continue;
                
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    // Skip composites, we'll handle their parts
                    if (action.bindings[i].isComposite)
                        continue;
                    
                    // For part of composites, give them a clear name
                    string displayName = action.bindings[i].isPartOfComposite ?
                        $"{action.name} {action.bindings[i].name}" :
                        action.name;
                    
                    bindings.Add(new KeyBindingInfo
                    {
                        ActionName = action.name,
                        DisplayName = displayName,
                        BindingIndex = i,
                        BindingPath = action.bindings[i].effectivePath,
                        DisplayString = InputControlPath.ToHumanReadableString(
                            action.bindings[i].effectivePath,
                            InputControlPath.HumanReadableStringOptions.OmitDevice),
                        IsPartOfComposite = action.bindings[i].isPartOfComposite,
                        CompositeName = action.bindings[i].isPartOfComposite ? action.bindings[i].name : string.Empty
                    });
                }
            }
            
            result[actionMap.name] = bindings;
        }
        
        return result;
    }
    
    public class KeyBindingInfo
    {
        public string ActionName;
        public string DisplayName;
        public int BindingIndex;
        public string BindingPath;
        public string DisplayString;
        public bool IsPartOfComposite;
        public string CompositeName;
    }
}