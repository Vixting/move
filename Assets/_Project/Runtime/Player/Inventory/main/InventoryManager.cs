using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.InputSystem;

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
    }
}