using System;
using System.Collections.Generic;
using InventorySystem;
using UnityEngine;

[Serializable]
public class ItemInstance
{
    public string instanceId;
    
    [System.NonSerialized]
    public ItemData itemData;
    
    public string itemDataId;
    
    public Vector2Int position;
    
    [System.NonSerialized]
    public ContainerInstance container;
    
    public string containerId;
    
    public bool isRotated;
    public int stackCount = 1;
    public float currentDurability = 100f;
    public int currentAmmoCount;
    
    [SerializeField]
    private List<string> _attachedItemIds = new List<string>();
    
    [System.NonSerialized]
    private List<ItemInstance> _attachedItems = new List<ItemInstance>();
    
    public List<ItemInstance> attachedItems { 
        get { return _attachedItems; }
    }
    
    [System.NonSerialized]
    private Dictionary<string, object> _customData = new Dictionary<string, object>();
    public Dictionary<string, object> customData {
        get { 
            if (_customData == null)
                _customData = new Dictionary<string, object>();
            return _customData;
        }
    }
    
    public ItemInstance(ItemData data, Vector2Int pos, ContainerInstance containerRef)
    {
        instanceId = Guid.NewGuid().ToString();
        itemData = data;
        itemDataId = data?.id;
        position = pos;
        container = containerRef;
        containerId = containerRef?.containerData.id;
        isRotated = false;
        currentDurability = GetMaxDurability();
        
        if (data is WeaponItemData weaponData)
        {
            currentAmmoCount = weaponData.maxAmmoCount;
        }
    }
    
    public void AttachItem(ItemInstance attachedItem)
    {
        if (attachedItem != null && !_attachedItems.Contains(attachedItem))
        {
            _attachedItems.Add(attachedItem);
            if (!_attachedItemIds.Contains(attachedItem.instanceId))
            {
                _attachedItemIds.Add(attachedItem.instanceId);
            }
        }
    }
    
    public void DetachItem(ItemInstance attachedItem)
    {
        if (attachedItem != null)
        {
            _attachedItems.Remove(attachedItem);
            _attachedItemIds.Remove(attachedItem.instanceId);
        }
    }
    
    public void ReconnectItemData(Func<string, ItemData> itemDataProvider)
    {
        if (itemData == null && !string.IsNullOrEmpty(itemDataId))
        {
            itemData = itemDataProvider(itemDataId);
        }
    }
    
    public void ReconnectAttachedItems(Dictionary<string, ItemInstance> allItems)
    {
        _attachedItems.Clear();
        
        foreach (string id in _attachedItemIds)
        {
            if (allItems.TryGetValue(id, out ItemInstance item))
            {
                _attachedItems.Add(item);
            }
        }
    }
    
    public int GetWidth()
    {
        if (itemData is WeaponItemData weaponData && weaponData.folded)
        {
            return isRotated ? weaponData.foldedHeight : weaponData.foldedWidth;
        }
        
        return isRotated ? itemData.height : itemData.width;
    }
    
    public int GetHeight()
    {
        if (itemData is WeaponItemData weaponData && weaponData.folded)
        {
            return isRotated ? weaponData.foldedWidth : weaponData.foldedHeight;
        }
        
        return isRotated ? itemData.width : itemData.height;
    }
    
    public bool CanFitAt(ContainerInstance targetContainer, Vector2Int pos)
    {
        return targetContainer.CanPlaceItem(this, pos);
    }
    
    public bool TryPlace(ContainerInstance targetContainer, Vector2Int pos)
    {
        if (container != null)
        {
            container.RemoveItem(this);
        }
        
        bool success = targetContainer.AddItem(this, pos);
        if (!success && container != null)
        {
            container.AddItem(this, position);
        }
        
        return success;
    }
    
    public void ToggleRotation()
    {
        if (!itemData.canRotate) return;
        
        if (container != null)
        {
            Vector2Int originalPos = position;
            ContainerInstance originalContainer = container;
            
            container.RemoveItem(this);
            
            isRotated = !isRotated;
            
            if (!originalContainer.AddItem(this, originalPos))
            {
                isRotated = !isRotated;
                originalContainer.AddItem(this, originalPos);
            }
        }
        else
        {
            isRotated = !isRotated;
        }
    }
    
    public bool TryStack(ItemInstance other)
    {
        if (other == null || other.itemData.id != itemData.id || !itemData.canStack)
            return false;
                
        int maxStack = itemData.maxStackSize;
        if (stackCount >= maxStack)
            return false;
                
        int spaceLeft = maxStack - stackCount;
        int amountToAdd = Mathf.Min(spaceLeft, other.stackCount);
        
        stackCount += amountToAdd;
        other.stackCount -= amountToAdd;
        
        return other.stackCount == 0;
    }
    
    public ItemInstance SplitStack(int amount)
    {
        if (!itemData.canStack || amount <= 0 || amount >= stackCount)
            return null;
                
        ItemInstance newInstance = new ItemInstance(itemData, Vector2Int.zero, null);
        newInstance.stackCount = amount;
        this.stackCount -= amount;
        
        return newInstance;
    }
    
    public float GetMaxDurability()
    {
        if (itemData is WeaponItemData weaponData)
        {
            return weaponData.maxDurability;
        }
        else if (itemData is ArmorItemData armorData)
        {
            return armorData.maxDurability;
        }
        
        return 100f;
    }
    
    public bool ToggleFolded()
    {
        if (!(itemData is WeaponItemData weaponData) || !weaponData.foldable)
            return false;
        
        if (container != null)
        {
            Vector2Int originalPos = position;
            ContainerInstance originalContainer = container;
            
            container.RemoveItem(this);
            
            weaponData.folded = !weaponData.folded;
            
            if (!originalContainer.AddItem(this, originalPos))
            {
                weaponData.folded = !weaponData.folded;
                originalContainer.AddItem(this, originalPos);
                return false;
            }
            
            return true;
        }
        else
        {
            weaponData.folded = !weaponData.folded;
            return true;
        }
    }
    
    public bool CanEquipInSlot(EquipmentSlot slot)
    {
        return itemData.canEquip && itemData.compatibleSlots.Contains(slot);
    }
}