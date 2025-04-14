using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Linq;

namespace InventorySystem
{
    public partial class InventoryManager : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset gridCellTemplate;
        [SerializeField] private VisualTreeAsset itemTemplate;
        [SerializeField] private UIDocument inventoryDocument;
        
        // Drag and drop settings
        [SerializeField] private bool _showGridHelpers = true;
        [SerializeField] private bool _enableSnapping = true;
        [SerializeField] private bool _enableMagneticDrop = true;
        [SerializeField] private float _magneticDropRadius = 150f;
        [SerializeField] private float _snapDelayTime = 0.1f;
        
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
            
            // Set up drag and drop settings
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
                    
                    InventoryWeaponBridge bridge = player.GetComponent<InventoryWeaponBridge>();
                    if (bridge != null)
                    {
                        bridge.MapAvailableWeapons();
                        bridge.SyncWeaponsWithInventory();
                    }
                }
                
                if (inventoryDocument != null)
                {
                    inventoryDocument.gameObject.SetActive(true);
                    HideInventory();
                }
            }
            else
            {
                if (inventoryDocument != null)
                {
                    inventoryDocument.gameObject.SetActive(false);
                }
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

        private void InitializeUI()
        {
            if (inventoryDocument == null || inventoryDocument.rootVisualElement == null)
            {
                Debug.LogWarning("Inventory document missing or not properly set up");
                return;
            }
            
            _root = inventoryDocument.rootVisualElement.Q("inventory-root");
            if (_root == null)
            {
                Debug.LogWarning("Inventory root element not found");
                return;
            }
            
            _inventoryContent = _root.Q("inventory-content");
            
            SetupCloseButton();
            SetupEquipmentUI();
            CreateSettingsUI();
            
            _root.RegisterCallback<PointerDownEvent>(evt => {
                _root.CapturePointer(evt.pointerId);
            });
            
            _root.RegisterCallback<PointerUpEvent>(evt => {
                _root.ReleasePointer(evt.pointerId);
            });
            
            _root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _root.RegisterCallback<MouseUpEvent>(OnMouseUp);
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            _root.RegisterCallback<MouseDownEvent>(evt => {
                if (_contextMenu != null)
                {
                    _contextMenu.RemoveFromHierarchy();
                    _contextMenu = null;
                }
            });
            
            UpdateHealthAndStats();
        }

        private void CreateSettingsUI()
        {
            if (_root == null) return;
            
            VisualElement settingsContainer = _root.Q("inventory-settings-container");
            if (settingsContainer == null)
            {
                settingsContainer = new VisualElement();
                settingsContainer.name = "inventory-settings-container";
                settingsContainer.style.position = Position.Absolute;
                settingsContainer.style.top = 10;
                settingsContainer.style.right = 10;
                settingsContainer.style.width = 200;
                settingsContainer.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.8f));
                settingsContainer.style.paddingTop = 5;
                settingsContainer.style.paddingBottom = 5;
                settingsContainer.style.paddingLeft = 10;
                settingsContainer.style.paddingRight = 10;
                _root.Add(settingsContainer);
            }
            
            Label settingsTitle = new Label("Inventory Settings");
            settingsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            settingsTitle.style.marginBottom = 10;
            settingsContainer.Add(settingsTitle);
            
            // Grid helpers toggle
            AddToggleSetting(settingsContainer, "Show Grid Helpers", _showGridHelpers, (evt) => {
                _showGridHelpers = evt.newValue;
                RefreshAllContainerUI();
            });
            
            // Snapping toggle
            AddToggleSetting(settingsContainer, "Enable Grid Snapping", _enableSnapping, (evt) => {
                _enableSnapping = evt.newValue;
            });
            
            // Magnetic drop toggle
            AddToggleSetting(settingsContainer, "Enable Magnetic Drop", _enableMagneticDrop, (evt) => {
                _enableMagneticDrop = evt.newValue;
                _magneticDrop = evt.newValue;
            });
        }
        
        private void AddToggleSetting(VisualElement container, string label, bool initialValue, EventCallback<ChangeEvent<bool>> callback)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 5;
            
            Label settingLabel = new Label(label);
            settingLabel.style.width = new Length(70, LengthUnit.Percent);
            
            Toggle toggle = new Toggle();
            toggle.value = initialValue;
            toggle.RegisterValueChangedCallback(callback);
            toggle.style.width = new Length(30, LengthUnit.Percent);
            
            row.Add(settingLabel);
            row.Add(toggle);
            container.Add(row);
        }
        
        private void RefreshAllContainerUI()
        {
            foreach (var containerId in _containers.Keys)
            {
                RefreshContainerUI(containerId);
            }
        }
        
        private void SetupCloseButton()
        {
            Button closeButton = _root.Q<Button>("close-button");
            if (closeButton != null)
            {
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
        
    }
}