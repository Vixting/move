using System;
using System.Collections.Generic;
using UnityEngine;
using SharedTypes;

namespace InventorySystem
{
    [Serializable]
    public class ItemData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        public GameObject prefab;
        public ItemCategory category;
        public ItemRarity rarity = ItemRarity.Common;
       
        public int width = 1;
        public int height = 1;
       
        public float weight = 0.5f;
       
        public bool canRotate = true;
        public bool canStack = false;
        public int maxStackSize = 1;
       
        public List<ItemProperty> properties = new List<ItemProperty>();
       
        public bool needsExamination = false;
        public bool isExamined = true;
       
        public List<string> tags = new List<string>();
        public int baseValue = 0;
       
        public bool canEquip = false;
        public bool canUse = false;
        public List<EquipmentSlot> compatibleSlots = new List<EquipmentSlot>();
       
        public virtual string GetItemType()
        {
            return "Item";
        }
       
        public virtual string GetTooltip()
        {
            string tooltip = $"<b>{displayName}</b>\n";
            tooltip += $"<color=#888888>{GetItemType()}</color>\n";
           
            if (width > 1 || height > 1)
            {
                tooltip += $"Size: {width}x{height}\n";
            }
           
            tooltip += $"Weight: {weight} kg";
           
            return tooltip;
        }
       
        public string GetRarityColorHex()
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return "#BBBBBB";
                case ItemRarity.Uncommon:
                    return "#55CC55";
                case ItemRarity.Rare:
                    return "#5555EE";
                case ItemRarity.Epic:
                    return "#CC55CC";
                case ItemRarity.Legendary:
                    return "#FFCC00";
                default:
                    return "#FFFFFF";
            }
        }
       
        public void AddOrUpdateProperty(string name, string value, string unit = "", Color? color = null)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].name == name)
                {
                    properties[i].value = value;
                    properties[i].unit = unit;
                    if (color.HasValue) properties[i].color = color.Value;
                    return;
                }
            }
           
            properties.Add(new ItemProperty(name, value, unit, (Color)color));
        }
       
        public string GetCssClassName()
        {
            return $"{rarity.ToString().ToLower()}-item";
        }
       
        public virtual ItemData Clone()
        {
            ItemData clone = CreateInstance<ItemData>();
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
            return clone;
        }
       
        public virtual void OnValidate()
        {
        }
    }
}