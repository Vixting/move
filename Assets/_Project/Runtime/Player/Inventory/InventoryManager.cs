using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using InventorySystem;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset gridCellTemplate;
    [SerializeField] private VisualTreeAsset itemTemplate;
    [SerializeField] private UIDocument inventoryDocument;
    
    private VisualElement _root;
    private VisualElement _inventoryContent;
    private Dictionary<string, ContainerInstance> _containers = new Dictionary<string, ContainerInstance>();
    private Dictionary<string, ItemInstance> _items = new Dictionary<string, ItemInstance>();
    
    private ItemInstance _draggedItem;
    private Vector2 _dragOffset;
    private VisualElement _draggedItemElement;
    
    public event Action<WeaponItemData, EquipmentSlot> onEquipWeapon;
    public event Action<EquipmentSlot> onUnequipWeapon;
    public event Action<WeaponItemData> onUpdateWeaponAmmo;
    
    public UnityEvent<WeaponData, int> onWeaponChanged = new UnityEvent<WeaponData, int>();
    public UnityEvent onInventoryChanged = new UnityEvent();
    
    private InventorySystem.Character _character;
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
        
        _character = GetComponent<InventorySystem.Character>();
        if (_character == null)
        {
            _character = FindObjectOfType<InventorySystem.Character>();
        }
        
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
        _initialized = true;
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
        
        _root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _root.RegisterCallback<MouseUpEvent>(OnMouseUp);
        
        UpdateHealthAndStats();
    }
    
    private void SetupCloseButton()
    {
        Button closeButton = _root.Q<Button>("close-button");
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
    
    private void CreateDefaultContainers()
    {
        CreateContainer("stash", "Stash", 10, 60);
        CreateContainer("backpack", "Backpack", 5, 5);
        CreateContainer("tactical-rig", "Tactical Rig", 4, 4);
        CreateContainer("pockets", "Pockets", 2, 2);
        
        SetupContainerUI("stash", "stash-grid");
        SetupContainerUI("backpack", "backpack-grid");
        SetupContainerUI("tactical-rig", "vest-grid");
        SetupContainerUI("pockets", "pockets-grid");
    }
    
    private void LoadPlayerInventory()
    {
        if (_character != null && _character.InventoryData != null)
        {
            foreach (var itemData in _character.InventoryData.Items)
            {
                AddItemToContainer(itemData.ItemData, itemData.ContainerId, new Vector2Int(itemData.X, itemData.Y), itemData.IsRotated);
            }
        }
        else
        {
            AddSampleItems();
        }
    }
    
    private void AddSampleItems()
    {
        // Sample items implementation here
    }
    
    private ContainerInstance CreateContainer(string id, string displayName, int width, int height)
    {
        ContainerData containerData = new ContainerData
        {
            id = id,
            displayName = displayName,
            width = width,
            height = height
        };
        
        ContainerInstance container = new ContainerInstance(containerData);
        _containers[id] = container;
        
        return container;
    }
    
    private void SetupContainerUI(string containerId, string gridElementName)
    {
        // Container UI setup implementation here
    }
    
    private void OnCellMouseDown(MouseDownEvent evt, string containerId, Vector2Int cellPosition)
    {
        // Cell mouse down implementation here
    }
    
    private void StartDragItem(ItemInstance item, Vector2 mousePosition)
    {
        // Drag item implementation here
    }
    
    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (_draggedItem != null && _draggedItemElement != null)
        {
            Vector2 newPosition = evt.mousePosition - _dragOffset;
            _draggedItemElement.style.left = newPosition.x;
            _draggedItemElement.style.top = newPosition.y;
        }
    }
    
    private void OnMouseUp(MouseUpEvent evt)
    {
        if (_draggedItem != null)
        {
            DropItem(evt.mousePosition);
        }
    }
    
    private void DropItem(Vector2 mousePosition)
    {
        // Drop item implementation here
    }
    
    public ItemInstance AddItemToContainer(ItemData itemData, string containerId, Vector2Int position, bool rotated = false)
    {
        if (!_containers.TryGetValue(containerId, out ContainerInstance container))
        {
            Debug.LogError($"Container {containerId} not found");
            return null;
        }
        
        ItemInstance item = new ItemInstance(itemData, position, container);
        item.isRotated = rotated;
        
        if (item.CanFitAt(container, position))
        {
            container.AddItem(item, position);
            _items[item.instanceId] = item;
            
            if (_initialized)
            {
                CreateItemUI(item, containerId);
            }
            
            return item;
        }
        
        return null;
    }
    
    private void CreateItemUI(ItemInstance item, string containerId)
    {
        // Item UI creation implementation here
    }
    
    private VisualElement CreateItemVisualElement(ItemInstance item, bool isDragging = false)
    {
        // Item visual element creation implementation here
        return null;
    }
    
    private void RefreshItemUI(ItemInstance item, string containerId)
    {
        if (_root == null) return;
        
        VisualElement existingElement = _root.Q(item.instanceId);
        if (existingElement != null)
        {
            existingElement.RemoveFromHierarchy();
        }
        
        CreateItemUI(item, containerId);
    }
    
    private void ShowItemInfo(ItemInstance item)
    {
        // Item info implementation here
    }
    
    private void DiscardItem(ItemInstance item)
    {
        // Discard item implementation here
    }
    
    public void ShowInventory()
    {
        if (_root != null)
        {
            _root.style.display = DisplayStyle.Flex;
        }
        
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            player.EnableGameplayMode(false);
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
        }
    }
    
    public void ToggleInventory()
    {
        if (_root == null)
        {
            Debug.LogWarning("Inventory root is null, cannot toggle inventory");
            return;
        }
        
        if (_root.style.display == DisplayStyle.None)
        {
            ShowInventory();
        }
        else
        {
            HideInventory();
        }
    }
    
    public InventorySystem.Character GetCharacter()
    {
        return _character;
    }
    
    public void SetWeaponManager(WeaponManager weaponManager)
    {
        _weaponManager = weaponManager;
    }
    
    public WeaponData[] GetEquippedWeaponData()
    {
        List<WeaponData> weaponDataList = new List<WeaponData>();
        InventorySystem.Character character = GetCharacter();
        
        if (character == null) return weaponDataList.ToArray();
        
        foreach (EquipmentSlot slot in new[] { EquipmentSlot.Primary, EquipmentSlot.Secondary, EquipmentSlot.Holster })
        {
            ItemInstance item = character.GetEquippedItem(slot);
            if (item != null && item.itemData is WeaponItemData weaponItem)
            {
                WeaponData weaponData = weaponItem.ToWeaponData();
                if (weaponData != null)
                {
                    weaponData.weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
                    weaponData.currentAmmo = weaponItem.currentAmmoCount;
                    weaponDataList.Add(weaponData);
                }
            }
        }
        
        return weaponDataList.ToArray();
    }
    
    public Dictionary<int, int> GetWeaponAmmoStates()
    {
        Dictionary<int, int> ammoStates = new Dictionary<int, int>();
        InventorySystem.Character character = GetCharacter();
        
        if (character == null) return ammoStates;
        
        foreach (EquipmentSlot slot in new[] { EquipmentSlot.Primary, EquipmentSlot.Secondary, EquipmentSlot.Holster })
        {
            ItemInstance item = character.GetEquippedItem(slot);
            if (item != null && item.itemData is WeaponItemData weaponItem)
            {
                int weaponSlot = GetWeaponSlotFromEquipmentSlot(slot);
                ammoStates[weaponSlot] = weaponItem.currentAmmoCount;
            }
        }
        
        return ammoStates;
    }
    
    public bool EquipWeapon(ItemInstance item, EquipmentSlot slot)
    {
        if (item == null || !(item.itemData is WeaponItemData))
        {
            return false;
        }
        
        InventorySystem.Character character = GetCharacter();
        if (character == null) return false;
        
        ItemInstance currentItem = character.GetEquippedItem(slot);
        if (currentItem != null)
        {
            UnequipItem(slot);
        }
        
        bool equipped = character.EquipItem(item, slot);
        
        if (equipped)
        {
            if (item.container != null)
            {
                item.container.RemoveItem(item);
                RefreshItemUI(item, item.container.containerData.id);
            }
            
            WeaponItemData weaponItem = (WeaponItemData)item.itemData;
            onEquipWeapon?.Invoke(weaponItem, slot);
            onInventoryChanged?.Invoke();
        }
        
        return equipped;
    }
    
    public ItemInstance UnequipItem(EquipmentSlot slot)
    {
        InventorySystem.Character character = GetCharacter();
        if (character == null) return null;
        
        ItemInstance item = character.UnequipItem(slot);
        
        if (item != null)
        {
            if (!_containers.TryGetValue("stash", out ContainerInstance container))
            {
                Debug.LogError("Stash container not found");
                return null;
            }
            
            Vector2Int? availablePos = container.FindAvailablePosition(item);
            if (availablePos.HasValue)
            {
                container.AddItem(item, availablePos.Value);
                CreateItemUI(item, "stash");
                
                if (item.itemData is WeaponItemData)
                {
                    onUnequipWeapon?.Invoke(slot);
                }
                
                onInventoryChanged?.Invoke();
                return item;
            }
            else
            {
                Debug.LogWarning("No available space in stash for unequipped item");
                character.EquipItem(item, slot);
                return null;
            }
        }
        
        return null;
    }
    
    public void UpdateWeaponAmmo(WeaponItemData weapon, int newAmmoCount)
    {
        weapon.currentAmmoCount = newAmmoCount;
        onUpdateWeaponAmmo?.Invoke(weapon);
        onInventoryChanged?.Invoke();
    }
    
    private int GetWeaponSlotFromEquipmentSlot(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Primary:
                return 1;
            case EquipmentSlot.Secondary:
                return 2;
            case EquipmentSlot.Holster:
                return 3;
            default:
                return -1;
        }
    }
    
    private EquipmentSlot GetEquipmentSlotFromWeaponSlot(int weaponSlot)
    {
        switch (weaponSlot)
        {
            case 1:
                return EquipmentSlot.Primary;
            case 2:
                return EquipmentSlot.Secondary;
            case 3:
                return EquipmentSlot.Holster;
            default:
                return EquipmentSlot.Primary;
        }
    }
    
    private void SetupEquipmentUI()
    {
        // Equipment UI setup implementation here
    }
    
    private void OnEquipmentSlotClicked(EquipmentSlot slot)
    {
        // Equipment slot clicked implementation here
    }
    
    private bool CanEquipInSlot(ItemData itemData, EquipmentSlot slot)
    {
        // Can equip implementation here
        return false;
    }
    
    private void UpdateEquipmentSlotUI(EquipmentSlot slot, ItemInstance item)
    {
        // Update equipment slot UI implementation here
    }
    
    private string GetSlotName(EquipmentSlot slot)
    {
        // Get slot name implementation here
        return "";
    }
    
    private void UpdateHealthAndStats()
    {
        // Update health and stats implementation here
    }
    
    public bool HasSpaceForItem(ItemData itemData, string containerId)
    {
        if (!_containers.TryGetValue(containerId, out ContainerInstance container))
        {
            return false;
        }
        
        ItemInstance dummyItem = new ItemInstance(itemData, Vector2Int.zero, container);
        return container.FindAvailablePosition(dummyItem) != null;
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
    
    public void OnValidate()
    {
        if (inventoryDocument == null)
        {
            inventoryDocument = GetComponent<UIDocument>();
        }
    }

    public void AddWeaponToInventory(string weaponId, bool equipImmediately = false)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found");
            return;
        }
        
        ItemData itemData = GameManager.Instance.GetItemById(weaponId);
        if (itemData == null || !(itemData is WeaponItemData weaponItemData))
        {
            Debug.LogError($"Weapon item data not found for ID: {weaponId}");
            return;
        }
        
        if (!_containers.TryGetValue("stash", out ContainerInstance stash))
        {
            Debug.LogError("Stash container not found");
            return;
        }
        
        ItemInstance dummyItem = new ItemInstance(weaponItemData, Vector2Int.zero, stash);
        Vector2Int? availablePosition = stash.FindAvailablePosition(dummyItem);
        
        if (!availablePosition.HasValue)
        {
            Debug.LogWarning("No available space in stash for new weapon");
            return;
        }
        
        ItemInstance weaponItem = AddItemToContainer(weaponItemData, "stash", availablePosition.Value);
        
        if (weaponItem != null)
        {
            if (equipImmediately)
            {
                EquipWeapon(weaponItem, EquipmentSlot.Primary);
            }
            
            onInventoryChanged?.Invoke();
            Debug.Log($"Added weapon {weaponItemData.displayName} to inventory");
        }
    }

    public void AddWeaponCommand(string[] parameters)
    {
        if (parameters.Length < 1)
        {
            Debug.LogError("Missing weapon ID parameter");
            return;
        }
        
        string weaponId = parameters[0];
        bool equip = parameters.Length > 1 && parameters[1].ToLower() == "true";
        
        InventoryManager.Instance.AddWeaponToInventory(weaponId, equip);
    }
}