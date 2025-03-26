using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum EquipmentSlot
{
  Head,
  Eyes,
  Ears,
  FaceCover,
  BodyArmor,
  TacticalRig,
  Primary,
  Secondary,
  Holster,
  Backpack,
  Pouch,
  Armband
}

public class InventoryManager : MonoBehaviour
{
  [SerializeField] private ContainerData playerBackpackData;
  [SerializeField] private ContainerData playerPocketsData;
  [SerializeField] private ContainerData playerVestData;
  [SerializeField] private List<ItemData> allItems = new List<ItemData>();
  
  public ContainerInstance playerBackpack;
  public ContainerInstance playerPockets;
  public ContainerInstance playerVest;
  public ContainerInstance groundContainer;
  public ContainerInstance playerStash;
  
  public List<ContainerInstance> allContainers = new List<ContainerInstance>();
  
  public UnityEvent<ItemInstance> onItemAdded = new UnityEvent<ItemInstance>();
  public UnityEvent<ItemInstance> onItemRemoved = new UnityEvent<ItemInstance>();
  public UnityEvent<ItemInstance, ContainerInstance, ContainerInstance> onItemMoved = new UnityEvent<ItemInstance, ContainerInstance, ContainerInstance>();
  public UnityEvent<ItemInstance> onItemEquipped = new UnityEvent<ItemInstance>();
  public UnityEvent<ItemInstance> onItemUnequipped = new UnityEvent<ItemInstance>();
  public UnityEvent<ItemInstance> onItemUsed = new UnityEvent<ItemInstance>();
  public UnityEvent onInventoryChanged = new UnityEvent();
  
  public Dictionary<EquipmentSlot, ItemInstance> equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();
  
  private static InventoryManager _instance;
  public static InventoryManager Instance => _instance;
  
  private ItemInstance _currentDraggedItem;
  private bool _initialized = false;
  
  private void Awake()
  {
      if (_instance == null)
      {
          _instance = this;
          DontDestroyOnLoad(gameObject);
      }
      else
      {
          Destroy(gameObject);
      }
  }
  
  private void OnEnable()
  {
      SceneManager.sceneLoaded += OnSceneLoaded;
  }
  
  private void OnDisable()
  {
      SceneManager.sceneLoaded -= OnSceneLoaded;
  }
  
  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
      var uiManager = FindObjectOfType<InventoryUIManager>();
      if (uiManager != null)
      {
          uiManager.SetupUI();
      }
  }
  
  private void Start()
  {
      if (_instance == this && !_initialized)
      {
          InitializeInventory();
          _initialized = true;
      }
  }
  
  public void SaveInventoryState()
  {
      DontDestroyOnLoad(gameObject);
  }
  
  private void InitializeInventory()
  {
      try
      {
          playerBackpack = new ContainerInstance(playerBackpackData ?? CreateDefaultContainerData("backpack", 5, 5));
          playerPockets = new ContainerInstance(playerPocketsData ?? CreateDefaultContainerData("pockets", 2, 2));
          playerVest = new ContainerInstance(playerVestData ?? CreateDefaultContainerData("vest", 4, 4));
          
          var groundData = CreateDefaultContainerData("ground", 10, 10);
          groundContainer = new ContainerInstance(groundData);
          
          var stashData = CreateDefaultContainerData("stash", 10, 20);
          playerStash = new ContainerInstance(stashData);
          
          allContainers.Clear();
          allContainers.Add(playerBackpack);
          allContainers.Add(playerPockets);
          allContainers.Add(playerVest);
          allContainers.Add(groundContainer);
          allContainers.Add(playerStash);
          
          foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
          {
              equippedItems[slot] = null;
          }
      }
      catch (Exception e)
      {
          Debug.LogError($"Error initializing inventory: {e.Message}");
      }
  }
  
   private ContainerData CreateDefaultContainerData(string id, int width, int height)
   {
       var data = new ContainerData
       {
           id = id,
           displayName = char.ToUpper(id[0]) + id.Substring(1),
           width = width,
           height = height
       };
       return data;
   }
  
  public ItemData GetItemDataById(string id)
  {
      return allItems.Find(item => item.id == id);
  }
  
  public ContainerInstance GetContainerById(string id)
  {
      return allContainers.Find(container => container.instanceId == id || container.containerData.id == id);
  }
  
  public ItemInstance CreateItem(string itemId, ContainerInstance targetContainer = null, Vector2Int? position = null)
  {
      if (!_initialized) InitializeInventory();
      
      var itemData = GetItemDataById(itemId);
      if (itemData == null) return null;
      
      var container = targetContainer ?? playerStash;
      
      var tempInstance = new ItemInstance(itemData, Vector2Int.zero, null);
      var pos = position ?? container.FindAvailablePosition(tempInstance);
      
      if (!pos.HasValue) return null;
      
      var item = new ItemInstance(itemData, pos.Value, container);
      
      if (container.AddItem(item, pos))
      {
          onItemAdded.Invoke(item);
          onInventoryChanged.Invoke();
          return item;
      }
      
      return null;
  }
  
  public bool MoveItem(ItemInstance item, ContainerInstance targetContainer, Vector2Int targetPosition)
  {
      if (item == null || targetContainer == null) return false;
      
      if (!item.CanFitAt(targetContainer, targetPosition)) return false;
      
      var originalContainer = item.container;
      
      if (originalContainer != null)
      {
          originalContainer.RemoveItem(item);
      }
      
      if (targetContainer.AddItem(item, targetPosition))
      {
          onItemMoved.Invoke(item, originalContainer, targetContainer);
          onInventoryChanged.Invoke();
          return true;
      }
      
      if (originalContainer != null)
      {
          originalContainer.AddItem(item, item.position);
      }
      
      return false;
  }
  
  public bool SwapItems(ItemInstance itemA, ItemInstance itemB)
  {
      if (itemA == null || itemB == null) return false;
      
      var containerA = itemA.container;
      var containerB = itemB.container;
      var positionA = itemA.position;
      var positionB = itemB.position;
      
      bool canAFitAtB = itemA.CanFitAt(containerB, positionB);
      bool canBFitAtA = itemB.CanFitAt(containerA, positionA);
      
      if (!canAFitAtB || !canBFitAtA) return false;
      
      containerA.RemoveItem(itemA);
      containerB.RemoveItem(itemB);
      
      bool success = containerB.AddItem(itemA, positionB) && 
                     containerA.AddItem(itemB, positionA);
      
      if (!success)
      {
          containerA.AddItem(itemA, positionA);
          containerB.AddItem(itemB, positionB);
          return false;
      }
      
      onInventoryChanged.Invoke();
      return true;
  }
  
  public bool RemoveItem(ItemInstance item)
  {
      if (item == null || item.container == null) return false;
      
      var container = item.container;
      if (container.RemoveItem(item))
      {
          onItemRemoved.Invoke(item);
          onInventoryChanged.Invoke();
          return true;
      }
      
      return false;
  }
  
  public bool EquipItem(ItemInstance item, EquipmentSlot slot)
  {
      if (item == null) return false;
      
      if (!IsItemValidForSlot(item, slot)) return false;
      
      if (item.container != null)
      {
          item.container.RemoveItem(item);
      }
      
      if (equippedItems[slot] != null)
      {
          UnequipItem(slot);
      }
      
      equippedItems[slot] = item;
      item.container = null;
      
      onItemEquipped.Invoke(item);
      onInventoryChanged.Invoke();
      
      if ((slot == EquipmentSlot.Primary || slot == EquipmentSlot.Secondary || slot == EquipmentSlot.Holster) &&
          item.itemData.category == ItemCategory.Weapon)
      {
          try
          {
              UpdateWeaponManagerWithEquippedWeapons();
          }
          catch (Exception e)
          {
              Debug.LogError($"Error updating weapon manager: {e.Message}");
          }
      }
      
      return true;
  }
  
  private void UpdateWeaponManagerWithEquippedWeapons()
  {
      var weaponManager = FindObjectOfType<WeaponManager>();
      if (weaponManager == null) return;
      
      List<WeaponData> equippedWeapons = new List<WeaponData>();
      
      TryAddWeaponFromSlot(EquipmentSlot.Primary, equippedWeapons);
      TryAddWeaponFromSlot(EquipmentSlot.Secondary, equippedWeapons);
      TryAddWeaponFromSlot(EquipmentSlot.Holster, equippedWeapons);
      
      if (equippedWeapons.Count > 0 && GameManager.Instance != null)
      {
          GameManager.Instance.RegisterWeapons(equippedWeapons.ToArray());
      }
  }
  
  private void TryAddWeaponFromSlot(EquipmentSlot slot, List<WeaponData> weapons)
  {
      var item = equippedItems[slot];
      if (item != null && item.itemData.category == ItemCategory.Weapon)
      {
          var weaponData = ConvertItemDataToWeaponData(item.itemData);
          if (weaponData != null) weapons.Add(weaponData);
      }
  }
  
  private WeaponData ConvertItemDataToWeaponData(ItemData itemData)
  {
      if (itemData is WeaponItemData weaponItemData)
      {
          return weaponItemData.ToWeaponData();
      }
      
      return null;
  }
  
  public bool UnequipItem(EquipmentSlot slot)
  {
      if (equippedItems[slot] == null) return false;
      
      var item = equippedItems[slot];
      equippedItems[slot] = null;
      
      bool placed = false;
      
      switch (slot)
      {
          case EquipmentSlot.Primary:
          case EquipmentSlot.Secondary:
          case EquipmentSlot.Holster:
              placed = playerBackpack.AddItem(item);
              break;
              
          case EquipmentSlot.TacticalRig:
              placed = playerBackpack.AddItem(item);
              break;
              
          default:
              if (item.itemData.width <= 1 && item.itemData.height <= 1)
              {
                  placed = playerPockets.AddItem(item);
              }
              
              if (!placed && playerVest != null)
              {
                  placed = playerVest.AddItem(item);
              }
              
              if (!placed)
              {
                  placed = playerBackpack.AddItem(item);
              }
              break;
      }
      
      if (!placed)
      {
          placed = playerStash.AddItem(item);
      }
      
      if (!placed)
      {
          placed = groundContainer.AddItem(item);
      }
      
      onItemUnequipped.Invoke(item);
      onInventoryChanged.Invoke();
      
      if (slot == EquipmentSlot.Primary || slot == EquipmentSlot.Secondary || slot == EquipmentSlot.Holster)
      {
          try
          {
              UpdateWeaponManagerWithEquippedWeapons();
          }
          catch (Exception e)
          {
              Debug.LogError($"Error updating weapon manager: {e.Message}");
          }
      }
      
      return true;
  }
  
  private bool IsItemValidForSlot(ItemInstance item, EquipmentSlot slot)
  {
      switch (slot)
      {
          case EquipmentSlot.Primary:
          case EquipmentSlot.Secondary:
          case EquipmentSlot.Holster:
              return item.itemData.category == ItemCategory.Weapon;
              
          case EquipmentSlot.Head:
              return item.itemData.category == ItemCategory.Helmet;
              
          case EquipmentSlot.BodyArmor:
              return item.itemData.category == ItemCategory.Armor;
              
          case EquipmentSlot.TacticalRig:
          case EquipmentSlot.Backpack:
              return item.itemData.category == ItemCategory.Container;
              
          case EquipmentSlot.Eyes:
          case EquipmentSlot.Ears:
          case EquipmentSlot.FaceCover:
          case EquipmentSlot.Pouch:
          case EquipmentSlot.Armband:
              return true;
              
          default:
              return false;
      }
  }
  
  public ItemInstance GetDraggedItem()
  {
      return _currentDraggedItem;
  }
  
  public void SetDraggedItem(ItemInstance item)
  {
      _currentDraggedItem = item;
  }
  
  public void UseItem(ItemInstance item)
  {
      if (item == null) return;
      
      bool consumed = false;
      
      switch (item.itemData.category)
      {
          case ItemCategory.Medicine:
              Debug.Log($"Using medical item: {item.itemData.displayName}");
              consumed = true;
              break;
              
          case ItemCategory.Food:
          case ItemCategory.Drink:
              Debug.Log($"Consuming: {item.itemData.displayName}");
              consumed = true;
              break;
              
          case ItemCategory.Key:
              Debug.Log("Keys are used automatically when approaching the correct door");
              break;
              
          default:
              Debug.Log($"Cannot use this item: {item.itemData.displayName}");
              break;
      }
      
      if (consumed)
      {
          if (item.itemData.stackable && item.currentStack > 1)
          {
              item.currentStack--;
          }
          else
          {
              RemoveItem(item);
          }
          
          onItemUsed.Invoke(item);
          onInventoryChanged.Invoke();
      }
  }
}