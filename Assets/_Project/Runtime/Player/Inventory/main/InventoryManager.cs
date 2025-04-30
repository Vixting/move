using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace InventorySystem
{
    public partial class InventoryManager : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset gridCellTemplate;
        [SerializeField] private VisualTreeAsset itemTemplate;
        [SerializeField] private UIDocument inventoryDocument;
        
        [SerializeField] private bool _showGridHelpers = true;
        [SerializeField] private bool _enableSnapping = true;
        [SerializeField] private bool _enableMagneticDrop = true;
        [SerializeField] private float _magneticDropRadius = 150f;
        [SerializeField] private float _snapDelayTime = 0.1f;
        [SerializeField] private float _dropDistance = 1.5f;
        [SerializeField] private float _dropHeight = 1.0f;
        [SerializeField] private float _dropForce = 2.0f;
        
        [SerializeField] private InventoryMode _currentMode = InventoryMode.Full;
        private bool _isInMainMenu = false;
        private Action _onStashCloseCallback;
        
        private float _snapTimer = 0f;
        private bool _magneticDrop = true;
        private float _magneticDropDistance = 150f;
        
        private VisualElement _root;
        private VisualElement _inventoryContent;
        private Dictionary<string, ContainerInstance> _containers = new Dictionary<string, ContainerInstance>();
        private Dictionary<string, ItemInstance> _items = new Dictionary<string, ItemInstance>();
        
        private ItemInstance _draggedItem;
        private Vector2 _dragOffset;
        private VisualElement _draggedItemElement;
        private VisualElement _contextMenu;
        
        public event Action<WeaponItemData, EquipmentSlot> onEquipWeapon;
        public event Action<EquipmentSlot> onUnequipWeapon;
        public event Action<WeaponItemData> onUpdateWeaponAmmo;
        
        public UnityEvent<WeaponData, int> onWeaponChanged = new UnityEvent<WeaponData, int>();
        public UnityEvent onInventoryChanged = new UnityEvent();
        
        private Character _character;
        private WeaponManager _weaponManager;
        
        private static InventoryManager _instance;
        public static InventoryManager Instance => _instance;
        
        private bool _initialized = false;
        
        public InventoryMode CurrentMode => _currentMode;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _character = GetComponent<Character>();
            if (_character == null)
            {
                _character = FindObjectOfType<Character>();
            }
            
            _magneticDrop = _enableMagneticDrop;
            _magneticDropDistance = _magneticDropRadius;
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
        {
            StartCoroutine(ReinitializeAfterSceneLoad());
        }
        
        private System.Collections.IEnumerator ReinitializeAfterSceneLoad()
        {
            yield return null;
            
            bool isGameplayScene = SceneManager.GetActiveScene().name != "MainMenu";
            
            if (isGameplayScene)
            {
                if (!_initialized)
                {
                    InitializeUI();
                    CreateDefaultContainers();
                    LoadPlayerInventory();
                    _initialized = true;
                }
                else
                {
                    ReconnectAllReferences();
                }
                
                Player player = FindObjectOfType<Player>();
                if (player != null)
                {
                    _weaponManager = player.GetComponent<WeaponManager>();
                    if (_weaponManager != null)
                    {
                        SetWeaponManager(_weaponManager);
                    }
                }
                
                if (inventoryDocument != null)
                {
                    inventoryDocument.gameObject.SetActive(true);
                    HideInventory();
                }
                
                SetInventoryMode(InventoryMode.NoStash);
            }
            else
            {
                if (inventoryDocument != null)
                {
                    inventoryDocument.gameObject.SetActive(false);
                }
                
                SetInventoryMode(InventoryMode.MainMenuMode);
            }
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void Start()
        {
            InitializeUI();
            CreateDefaultContainers();
            LoadPlayerInventory();
            ReconnectAllReferences();
            _initialized = true;
        }
        
        public void SetInventoryMode(InventoryMode mode)
        {
            _currentMode = mode;
            
            switch (mode)
            {
                case InventoryMode.Full:
                    _isInMainMenu = false;
                    ShowAllPanels();
                    break;
                    
                case InventoryMode.MainMenuMode:
                    _isInMainMenu = true;
                    ShowMainMenuMode();
                    break;
                    
                case InventoryMode.LootingOnly:
                    _isInMainMenu = false;
                    break;
                    
                case InventoryMode.NoStash:
                    _isInMainMenu = false;
                    ShowGameplayMode();
                    break;
            }
            
            Debug.Log($"Inventory mode set to: {_currentMode}");
        }

        private void ShowAllPanels()
        {
            if (_root == null) return;
            
            _root.RemoveFromClassList("main-menu-mode");
            
            VisualElement equipmentPanel = _root.Q("equipment-panel");
            VisualElement playerInventoryPanel = _root.Q("player-inventory-panel");
            VisualElement stashPanel = _root.Q("stash-panel");
            
            if (equipmentPanel != null)
                equipmentPanel.style.display = DisplayStyle.Flex;
                        
            if (playerInventoryPanel != null)
            {
                playerInventoryPanel.style.display = DisplayStyle.Flex;
                playerInventoryPanel.style.width = new StyleLength(new Length(35, LengthUnit.Percent));
                playerInventoryPanel.RemoveFromClassList("expanded");
            }
                        
            if (stashPanel != null)
            {
                stashPanel.style.display = DisplayStyle.Flex;
                stashPanel.style.width = new StyleLength(new Length(40, LengthUnit.Percent));
            }
                        
            Label characterName = _root.Q<Label>("character-name");
            if (characterName != null)
                characterName.text = "PMC Character";
                        
            Button closeButton = _root.Q<Button>("close-button");
            if (closeButton != null)
            {
                closeButton.clicked -= OnStashCloseButtonClicked;
                closeButton.clicked += () => 
                {
                    HideInventory();
                    Player player = FindObjectOfType<Player>();
                    if (player != null)
                    {
                        player.EnableGameplayMode(true);
                    }
                };
            }
            
            VisualElement inventoryContent = _root.Q("inventory-content");
            if (inventoryContent != null)
            {
                inventoryContent.style.flexDirection = FlexDirection.Row;
            }
        }
        
        private void ShowMainMenuMode()
        {
            if (_root == null) return;
            
            _root.AddToClassList("main-menu-mode");
            
            VisualElement equipmentPanel = _root.Q("equipment-panel");
            VisualElement playerInventoryPanel = _root.Q("player-inventory-panel");
            VisualElement stashPanel = _root.Q("stash-panel");
            
            // Show equipment and player inventory panels, as well as stash
            if (equipmentPanel != null)
                equipmentPanel.style.display = DisplayStyle.Flex;
                
            if (playerInventoryPanel != null)
                playerInventoryPanel.style.display = DisplayStyle.Flex;
                
            if (stashPanel != null)
            {
                stashPanel.style.display = DisplayStyle.Flex;
                stashPanel.style.width = new StyleLength(new Length(40, LengthUnit.Percent));
            }
            
            // Adjust the widths of the panels for better layout
            if (playerInventoryPanel != null)
            {
                playerInventoryPanel.style.width = new StyleLength(new Length(35, LengthUnit.Percent));
            }
            
            if (equipmentPanel != null)
            {
                equipmentPanel.style.width = new StyleLength(new Length(25, LengthUnit.Percent));
            }
            
            // Update title
            Label characterName = _root.Q<Label>("character-name");
            if (characterName != null)
                characterName.text = "Character & Stash Management";
                
            // Update close button action
            Button closeButton = _root.Q<Button>("close-button");
            if (closeButton != null)
            {
                closeButton.clicked -= () => HideInventory();
                closeButton.clicked += OnStashCloseButtonClicked;
            }
            
            // Reorganize the inventory content for main menu layout
            VisualElement inventoryContent = _root.Q("inventory-content");
            if (inventoryContent != null)
            {
                inventoryContent.style.flexDirection = FlexDirection.Row;
            }
        }
        
        private void ShowGameplayMode()
        {
            if (_root == null) return;
            
            _root.RemoveFromClassList("main-menu-mode");
            
            VisualElement equipmentPanel = _root.Q("equipment-panel");
            VisualElement playerInventoryPanel = _root.Q("player-inventory-panel");
            VisualElement stashPanel = _root.Q("stash-panel");
            
            if (equipmentPanel != null)
                equipmentPanel.style.display = DisplayStyle.Flex;
                
            if (playerInventoryPanel != null)
            {
                playerInventoryPanel.style.display = DisplayStyle.Flex;
                playerInventoryPanel.style.width = new StyleLength(new Length(60, LengthUnit.Percent));
                
                playerInventoryPanel.AddToClassList("expanded");
            }
                
            if (stashPanel != null)
                stashPanel.style.display = DisplayStyle.None;
            
            Label characterName = _root.Q<Label>("character-name");
            if (characterName != null)
                characterName.text = "PMC Character";
                
            Button closeButton = _root.Q<Button>("close-button");
            if (closeButton != null)
            {
                closeButton.clicked -= OnStashCloseButtonClicked;
                closeButton.clicked += () => 
                {
                    HideInventory();
                    Player player = FindObjectOfType<Player>();
                    if (player != null)
                    {
                        player.EnableGameplayMode(true);
                    }
                };
            }
            
            VisualElement inventoryContent = _root.Q("inventory-content");
            if (inventoryContent != null)
            {
                inventoryContent.style.flexDirection = FlexDirection.Row;
            }
        }
        
        private void OnStashCloseButtonClicked()
        {
            HideInventory();
            
            if (_isInMainMenu)
            {
                _isInMainMenu = false;
                
                _onStashCloseCallback?.Invoke();
            }
        }
        
        public void SetStashCloseCallback(Action callback)
        {
            _onStashCloseCallback = callback;
        }
        
        private void ReconnectAllReferences()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("Cannot reconnect references: GameManager.Instance is null");
                return;
            }

            foreach (var item in _items.Values)
            {
                if (item.itemData == null && !string.IsNullOrEmpty(item.itemDataId))
                {
                    item.itemData = GameManager.Instance.GetItemById(item.itemDataId);
                    if (item.itemData == null)
                    {
                        Debug.LogWarning($"Failed to reconnect itemData for item with ID {item.instanceId} (itemDataId: {item.itemDataId})");
                    }
                }
            }
            
            foreach (var container in _containers.Values)
            {
                foreach (var item in container.GetAllItems())
                {
                    item.container = container;
                    item.containerId = container.containerData.id;
                }
            }
            
            if (_draggedItem != null)
            {
                if (_draggedItem.itemData == null && !string.IsNullOrEmpty(_draggedItem.itemDataId))
                {
                    _draggedItem.itemData = GameManager.Instance.GetItemById(_draggedItem.itemDataId);
                }
                
                if (!string.IsNullOrEmpty(_draggedItem.containerId) && _draggedItem.container == null)
                {
                    if (_containers.TryGetValue(_draggedItem.containerId, out ContainerInstance container))
                    {
                        _draggedItem.container = container;
                    }
                }
            }
            
            Debug.Log("All references reconnected successfully");
        }

        public void SetWeaponManager(WeaponManager weaponManager)
        {
            _weaponManager = weaponManager;
        }

        public Character GetCharacter()
        {
            return _character;
        }
        
        public Dictionary<string, ContainerInstance> GetContainers()
        {
            return new Dictionary<string, ContainerInstance>(_containers);
        }
        
        public void OnValidate()
        {
            if (inventoryDocument == null)
            {
                inventoryDocument = GetComponent<UIDocument>();
            }
        }
        
        public void ShowInventory()
        {
            ReconnectAllReferences();
           
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _root.Focus();
                
                switch (_currentMode)
                {
                    case InventoryMode.Full:
                        ShowAllPanels();
                        break;
                        
                    case InventoryMode.MainMenuMode:
                        ShowMainMenuMode();
                        break;
                        
                    case InventoryMode.NoStash:
                        ShowGameplayMode();
                        break;
                        
                    case InventoryMode.LootingOnly:
                        break;
                }
            }
           
            if (!_isInMainMenu)
            {
                Player player = FindObjectOfType<Player>();
                if (player != null)
                {
                    player.EnableGameplayMode(false);
                }
            }
        }
       
        public void HideInventory()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
               
                VisualElement infoPanel = _root.Q("item-info-panel");
                if (infoPanel != null)
                {
                    infoPanel.style.display = DisplayStyle.None;
                }
               
                if (_contextMenu != null)
                {
                    _contextMenu.RemoveFromHierarchy();
                    _contextMenu = null;
                }
            }
        }
       
        public void ToggleInventory()
        {
            if (_root == null)
            {
                Debug.LogError("Inventory root is null in ToggleInventory, cannot toggle inventory");
                return;
            }
           
            Debug.Log($"ToggleInventory called - current display: {_root.style.display}");
           
            if (_root.style.display == DisplayStyle.None)
            {
                ShowInventory();
            }
            else
            {
                HideInventory();
                
                if (_isInMainMenu && _onStashCloseCallback != null)
                {
                    _isInMainMenu = false;
                    _onStashCloseCallback.Invoke();
                }
                else if (!_isInMainMenu)
                {
                    Player player = FindObjectOfType<Player>();
                    if (player != null)
                    {
                        player.EnableGameplayMode(true);
                    }
                }
            }
        }

        public bool AddItemToInventory(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || GameManager.Instance == null)
            {
                Debug.LogWarning("AddItemToInventory: Invalid item ID or GameManager not found");
                return false;
            }
            
            ItemData itemData = GameManager.Instance.GetItemById(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"AddItemToInventory: Item with ID '{itemId}' not found");
                return false;
            }
            
            if (_containers.TryGetValue("backpack", out ContainerInstance backpack))
            {
                ItemInstance dummyItem = new ItemInstance(itemData, Vector2Int.zero, backpack);
                Vector2Int? availablePos = backpack.FindAvailablePosition(dummyItem);
                
                if (availablePos.HasValue)
                {
                    ItemInstance item = AddItemToContainer(itemData, "backpack", availablePos.Value);
                    return item != null;
                }
            }
            
            if (_containers.TryGetValue("stash", out ContainerInstance stash))
            {
                ItemInstance dummyItem = new ItemInstance(itemData, Vector2Int.zero, stash);
                Vector2Int? availablePos = stash.FindAvailablePosition(dummyItem);
                
                if (availablePos.HasValue)
                {
                    ItemInstance item = AddItemToContainer(itemData, "stash", availablePos.Value);
                    return item != null;
                }
            }
            
            Debug.LogWarning($"AddItemToInventory: No space for item '{itemData.displayName}' in any container");
            return false;
        }
        
        public bool EquipItemFromInventory(string itemId, EquipmentSlot slot)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("EquipItemFromInventory: Invalid item ID");
                return false;
            }
            
            foreach (var container in _containers.Values)
            {
                foreach (var item in container.GetAllItems())
                {
                    if (item.itemData.id == itemId)
                    {
                        return EquipItem(item, slot);
                    }
                }
            }
            
            if (GameManager.Instance != null)
            {
                ItemData itemData = GameManager.Instance.GetItemById(itemId);
                if (itemData != null)
                {
                    if (_containers.TryGetValue("stash", out ContainerInstance stash))
                    {
                        ItemInstance dummyItem = new ItemInstance(itemData, Vector2Int.zero, stash);
                        Vector2Int? availablePos = stash.FindAvailablePosition(dummyItem);
                        
                        if (availablePos.HasValue)
                        {
                            ItemInstance item = AddItemToContainer(itemData, "stash", availablePos.Value);
                            if (item != null)
                            {
                                return EquipItem(item, slot);
                            }
                        }
                    }
                    
                    Debug.LogWarning($"EquipItemFromInventory: No space to add item '{itemData.displayName}' before equipping");
                }
                else
                {
                    Debug.LogWarning($"EquipItemFromInventory: Item with ID '{itemId}' not found");
                }
            }
            
            return false;
        }
        
        public bool HasAmmoForWeapon(string ammoTypeStr)
        {
            if (string.IsNullOrEmpty(ammoTypeStr)) return false;
            
            List<ItemInstance> ammoItems = FindItemsByCategory(ItemCategory.Ammunition);
            
            foreach (var ammo in ammoItems)
            {
                if (ammo.itemData is AmmoItemData ammoData)
                {
                    if (ammoData.ammoType.ToString() == ammoTypeStr)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public bool UseAmmoForWeapon(string ammoTypeStr, int amount)
        {
            if (string.IsNullOrEmpty(ammoTypeStr) || amount <= 0) return false;
            
            List<ItemInstance> ammoItems = FindItemsByCategory(ItemCategory.Ammunition);
            int ammoFound = 0;
            
            foreach (var ammo in ammoItems)
            {
                if (ammo.itemData is AmmoItemData ammoData && ammoData.ammoType.ToString() == ammoTypeStr)
                {
                    ammoFound += ammo.stackCount;
                }
            }
            
            if (ammoFound < amount)
            {
                return false;
            }
            
            int ammoToUse = amount;
            foreach (var ammo in new List<ItemInstance>(ammoItems))
            {
                if (ammoToUse <= 0) break;
                
                if (ammo.itemData is AmmoItemData ammoData && ammoData.ammoType.ToString() == ammoTypeStr)
                {
                    if (ammo.stackCount <= ammoToUse)
                    {
                        ammoToUse -= ammo.stackCount;
                        RemoveItem(ammo);
                    }
                    else
                    {
                        ammo.stackCount -= ammoToUse;
                        ammoToUse = 0;
                        RefreshItemUI(ammo, ammo.container.containerData.id);
                    }
                }
            }
            
            onInventoryChanged?.Invoke();
            return true;
        }
        
        public List<ItemInstance> FindItemsByCategory(ItemCategory category)
        {
            List<ItemInstance> result = new List<ItemInstance>();
            
            foreach (var container in _containers.Values)
            {
                foreach (var item in container.GetAllItems())
                {
                    if (item.itemData.category == category)
                    {
                        result.Add(item);
                    }
                }
            }
            
            return result;
        }

        public ItemInstance GetItemById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _items == null)
                return null;
                
            if (_items.TryGetValue(itemId, out ItemInstance item))
                return item;
                
            return null;
        }

        public bool IsItemInStash(string itemInstanceId)
        {
            if (string.IsNullOrEmpty(itemInstanceId))
                return false;
                
            if (_items.TryGetValue(itemInstanceId, out ItemInstance item))
            {
                return item.containerId == "stash";
            }
            
            return false;
        }

        public bool IsItemInPlayerInventory(string itemInstanceId)
        {
            if (string.IsNullOrEmpty(itemInstanceId))
                return false;
                
            if (_items.TryGetValue(itemInstanceId, out ItemInstance item))
            {
                return item.containerId != "stash";
            }
            
            return false;
        }
    }
}