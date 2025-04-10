using System.Collections.Generic;
using SharedTypes;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Medicine Item", menuName = "Inventory/Items/Medicine")]
    public class MedicineItemData : ItemData
    {
        public float useTime = 3.0f;
        public float healthRestored = 0;
        public bool stopsBleed = false;
        public bool fixesFracture = false;
        public bool removesPain = false;
        public float duration = 0f;
       
        public override void OnValidate()
        {
            category = ItemCategory.Medicine;
            canStack = true;
            canUse = true;
            maxStackSize = 5;
           
            properties.Clear();
           
            if (healthRestored > 0)
            {
                AddOrUpdateProperty("Health", $"+{healthRestored}", "", Color.green);
            }
           
            if (stopsBleed)
            {
                AddOrUpdateProperty("Effect", "Stops Bleeding", "", Color.red);
            }
           
            if (fixesFracture)
            {
                AddOrUpdateProperty("Effect", "Fixes Fracture", "", Color.yellow);
            }
           
            if (removesPain)
            {
                AddOrUpdateProperty("Effect", "Removes Pain", "", Color.cyan);
            }
           
            if (duration > 0)
            {
                AddOrUpdateProperty("Duration", duration.ToString(), "s", Color.white);
            }
        }
       
        public override string GetItemType()
        {
            return "Medicine";
        }
        
        public override ItemData Clone()
        {
            MedicineItemData clone = CreateInstance<MedicineItemData>();
            
            clone.id = id;
            clone.displayName = displayName;
            clone.description = description;
            clone.icon = icon;
            clone.prefab = prefab;
            clone.category = category;
            clone.rarity = rarity;
            clone.width = width;
            clone.height = height;
            clone.weight = weight;
            clone.canRotate = canRotate;
            clone.canStack = canStack;
            clone.maxStackSize = maxStackSize;
            clone.properties = new List<ItemProperty>(properties);
            clone.needsExamination = needsExamination;
            clone.isExamined = isExamined;
            clone.tags = new List<string>(tags);
            clone.baseValue = baseValue;
            clone.canEquip = canEquip;
            clone.canUse = canUse;
            clone.compatibleSlots = new List<EquipmentSlot>(compatibleSlots);
            
            clone.useTime = useTime;
            clone.healthRestored = healthRestored;
            clone.stopsBleed = stopsBleed;
            clone.fixesFracture = fixesFracture;
            clone.removesPain = removesPain;
            clone.duration = duration;
            
            return clone;
        }
    }
}