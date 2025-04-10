using System.Collections.Generic;
using SharedTypes;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Ammo Item", menuName = "Inventory/Items/Ammo")]
    public class AmmoItemData : ItemData
    {
        public AmmoType ammoType;
       
        public float baseDamage = 50f;
        public float armorPenetration = 20f;
        public float fragmentationChance = 0.1f;
        public float accuracy;
        public float recoil;
       
        public bool subsonic = false;
        public bool tracer = false;
        public Color tracerColor = Color.red;
        public string caliber;
       
        public override void OnValidate()
        {
            category = ItemCategory.Ammunition;
            canStack = true;
            maxStackSize = 60;
           
            width = 1;
            height = 1;
           
            properties.Clear();
           
            AddOrUpdateProperty(
                "Damage",
                baseDamage.ToString(),
                "",
                baseDamage > 60 ? Color.red : Color.white
            );
           
            AddOrUpdateProperty(
                "Penetration",
                armorPenetration.ToString(),
                "",
                armorPenetration > 30 ? Color.green : Color.white
            );
           
            AddOrUpdateProperty(
                "Fragmentation",
                (fragmentationChance * 100).ToString(),
                "%",
                fragmentationChance > 0.15f ? Color.green : Color.white
            );
           
            if (string.IsNullOrEmpty(caliber))
            {
                caliber = GetCaliberFromAmmoType();
            }
           
            if (string.IsNullOrEmpty(displayName) || displayName == "New Ammo Item")
            {
                string ammoTypeName = GetCaliberFromAmmoType();
                displayName = $"{ammoTypeName} {(tracer ? "Tracer " : "")}{(subsonic ? "Subsonic " : "")}Ammo";
            }
           
            if (tracer)
            {
                AddOrUpdateProperty("Tracer", "Yes", "", tracerColor);
            }
           
            if (subsonic)
            {
                AddOrUpdateProperty("Subsonic", "Yes", "", Color.cyan);
            }
        }
       
        private string GetCaliberFromAmmoType()
        {
            switch (ammoType)
            {
                case AmmoType.Pistol_9x19: return "9x19mm";
                case AmmoType.Pistol_45ACP: return ".45 ACP";
                case AmmoType.Rifle_556x45: return "5.56x45mm";
                case AmmoType.Rifle_762x39: return "7.62x39mm";
                case AmmoType.Rifle_762x51: return "7.62x51mm";
                case AmmoType.Rifle_762x54R: return "7.62x54R";
                case AmmoType.Shotgun_12Gauge: return "12 Gauge";
                case AmmoType.Shotgun_20Gauge: return "20 Gauge";
                default: return "Unknown";
            }
        }
       
        public override string GetItemType()
        {
            return $"Ammo - {caliber}";
        }
       
        public Color GetAmmoColor()
        {
            if (tracer)
            {
                return tracerColor;
            }
           
            switch (ammoType)
            {
                case AmmoType.Rifle_556x45:
                    return new Color(0.8f, 0.4f, 0.0f);
                case AmmoType.Rifle_762x39:
                case AmmoType.Rifle_762x51:
                case AmmoType.Rifle_762x54R:
                    return new Color(0.8f, 0.0f, 0.0f);
                case AmmoType.Pistol_9x19:
                case AmmoType.Pistol_45ACP:
                    return new Color(0.9f, 0.9f, 0.0f);
                case AmmoType.Shotgun_12Gauge:
                case AmmoType.Shotgun_20Gauge:
                    return new Color(0.5f, 0.0f, 0.5f);
                default:
                    return Color.gray;
            }
        }
        
        public override ItemData Clone()
        {
            AmmoItemData clone = CreateInstance<AmmoItemData>();
            
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
            
            clone.ammoType = ammoType;
            clone.baseDamage = baseDamage;
            clone.armorPenetration = armorPenetration;
            clone.fragmentationChance = fragmentationChance;
            clone.accuracy = accuracy;
            clone.recoil = recoil;
            clone.subsonic = subsonic;
            clone.tracer = tracer;
            clone.tracerColor = tracerColor;
            clone.caliber = caliber;
            
            return clone;
        }
    }
}