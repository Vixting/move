using UnityEngine;
using InventorySystem;

public class TestItemCreator : MonoBehaviour
{
    [SerializeField] private Sprite medkitSprite;
    [SerializeField] private Sprite bandageSprite;
    [SerializeField] private Sprite painkillerSprite;
    [SerializeField] private GameObject medkitPrefab;
    [SerializeField] private GameObject bandagePrefab;
    [SerializeField] private GameObject painkillerPrefab;
        
    // Debug options
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    public void CreateMedkit()
    {
        if (showDebugLogs)
        {
            Debug.Log("Creating medkit...");
            Debug.Log($"Medkit sprite assigned: {medkitSprite != null}");
        }
        
        MedicineItemData medkit = ScriptableObject.CreateInstance<MedicineItemData>();
        medkit.id = "med_medkit";
        medkit.displayName = "Medkit";
        medkit.description = "A comprehensive medical kit for treating various injuries. Restores a significant amount of health.";
        medkit.icon = medkitSprite;
        medkit.prefab = medkitPrefab;
        medkit.rarity = ItemRarity.Rare;
        medkit.width = 2;
        medkit.height = 2;
        medkit.weight = 1.5f;
        medkit.baseValue = 8000;
        medkit.healthRestored = 70f;
        medkit.useTime = 5f;
        medkit.stopsBleed = true;
        
        // Properties will be automatically populated by OnValidate()
        medkit.OnValidate();
        
        if (showDebugLogs)
        {
            Debug.Log($"Medkit created. Icon null? {medkit.icon == null}");
        }
        
        AddItemToPlayerInventory(medkit);
    }
    
    public void CreateBandage()
    {
        if (showDebugLogs)
        {
            Debug.Log("Creating bandage...");
            Debug.Log($"Bandage sprite assigned: {bandageSprite != null}");
        }
        
        MedicineItemData bandage = ScriptableObject.CreateInstance<MedicineItemData>();
        bandage.id = "med_bandage";
        bandage.displayName = "Bandage";
        bandage.description = "A simple bandage for treating minor wounds. Stops bleeding and restores a small amount of health.";
        bandage.icon = bandageSprite;
        bandage.rarity = ItemRarity.Common;
        bandage.width = 1;
        bandage.height = 1;
        bandage.weight = 0.2f;
        bandage.baseValue = 500;
        bandage.healthRestored = 15f;
        bandage.useTime = 2f;
        bandage.stopsBleed = true;
        
        // Properties will be automatically populated by OnValidate()
        bandage.OnValidate();
        
        AddItemToPlayerInventory(bandage);
    }
    
    public void CreatePainkillers()
    {
        if (showDebugLogs)
        {
            Debug.Log("Creating painkillers...");
            Debug.Log($"Painkillers sprite assigned: {painkillerSprite != null}");
        }
        
        MedicineItemData painkillers = ScriptableObject.CreateInstance<MedicineItemData>();
        painkillers.id = "med_painkillers";
        painkillers.displayName = "Painkillers";
        painkillers.description = "Over-the-counter painkillers that provide temporary relief from pain.";
        painkillers.icon = painkillerSprite;
        painkillers.rarity = ItemRarity.Common;
        painkillers.width = 1;
        painkillers.height = 1;
        painkillers.weight = 0.1f;
        painkillers.baseValue = 300;
        painkillers.healthRestored = 5f;
        painkillers.useTime = 1.5f;
        painkillers.removesPain = true;
        painkillers.duration = 60f; // Pain relief for 60 seconds
        
        // Properties will be automatically populated by OnValidate()
        painkillers.OnValidate();
        
        AddItemToPlayerInventory(painkillers);
    }
    
    private void AddItemToPlayerInventory(ItemData itemData)
    {
        // Get the inventory manager
        InventoryManager inventoryManager = InventoryManager.Instance;
        
        if (inventoryManager == null)
        {
            Debug.LogError("InventoryManager instance not found!");
            return;
        }
        
        // First try to add it to the player's backpack
        if (inventoryManager.HasSpaceForItem(itemData, "backpack"))
        {
            Vector2Int? position = FindSpaceInContainer(inventoryManager, "backpack", itemData);
            if (position.HasValue)
            {
                var item = inventoryManager.AddItemToContainer(itemData, "backpack", position.Value);
                if (showDebugLogs)
                {
                    Debug.Log($"Added {itemData.displayName} to backpack at position {position.Value}");
                    Debug.Log($"Item instance created: {item != null}");
                    if (item != null)
                    {
                        Debug.Log($"Item ID: {item.instanceId}, Container: {item.container?.containerData.id}, Position: {item.position}");
                    }
                }
                return;
            }
        }
        
        // If no space in backpack, try tactical rig
        if (inventoryManager.HasSpaceForItem(itemData, "tactical-rig"))
        {
            Vector2Int? position = FindSpaceInContainer(inventoryManager, "tactical-rig", itemData);
            if (position.HasValue)
            {
                var item = inventoryManager.AddItemToContainer(itemData, "tactical-rig", position.Value);
                if (showDebugLogs)
                {
                    Debug.Log($"Added {itemData.displayName} to tactical rig at position {position.Value}");
                    Debug.Log($"Item instance created: {item != null}");
                }
                return;
            }
        }
        
        // If still no space, put in stash
        if (inventoryManager.HasSpaceForItem(itemData, "stash"))
        {
            Vector2Int? position = FindSpaceInContainer(inventoryManager, "stash", itemData);
            if (position.HasValue)
            {
                var item = inventoryManager.AddItemToContainer(itemData, "stash", position.Value);
                if (showDebugLogs)
                {
                    Debug.Log($"Added {itemData.displayName} to stash at position {position.Value}");
                    Debug.Log($"Item instance created: {item != null}");
                }
                return;
            }
        }
        
        Debug.LogWarning($"No space found for {itemData.displayName} in any container!");
    }
    
    private Vector2Int? FindSpaceInContainer(InventoryManager inventoryManager, string containerId, ItemData itemData)
    {
        var containers = inventoryManager.GetContainers();
        if (containers.TryGetValue(containerId, out ContainerInstance container))
        {
            ItemInstance dummyItem = new ItemInstance(itemData, Vector2Int.zero, container);
            return container.FindAvailablePosition(dummyItem);
        }
        return null;
    }
}