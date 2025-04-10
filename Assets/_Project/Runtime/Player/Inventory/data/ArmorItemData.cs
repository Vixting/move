using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Armor Item", menuName = "Inventory/Items/Armor")]
    public class ArmorItemData : ItemData
    {
        public ArmorClass armorClass;
        public ArmorMaterial material;
        public float durability = 100f;
        public float maxDurability = 100f;
        
        public float movementPenalty = 0f;
        public float turnPenalty = 0f;
        public float ergoPenalty = 0f;
        
        public List<string> armoredAreas = new List<string>();
        
        public override void OnValidate()
        {
            canEquip = true;
            compatibleSlots.Clear();
            
            if (category == ItemCategory.Helmet)
            {
                compatibleSlots.Add(EquipmentSlot.Head);
            }
            else
            {
                category = ItemCategory.Armor;
                compatibleSlots.Add(EquipmentSlot.BodyArmor);
            }
            
            canStack = false;
            
            properties.Clear();
            
            AddOrUpdateProperty(
                "Class",
                ((int)armorClass).ToString(),
                "",
                GetArmorClassColor(armorClass)
            );
            
            AddOrUpdateProperty(
                "Material", 
                material.ToString(), 
                "", 
                Color.white
            );
            
            AddOrUpdateProperty(
                "Durability",
                $"{durability}/{maxDurability}",
                "",
                GetDurabilityColor(durability, maxDurability)
            );
            
            if (movementPenalty > 0)
            {
                AddOrUpdateProperty(
                    "Movement",
                    $"-{movementPenalty}",
                    "%",
                    Color.red
                );
            }
            
            if (turnPenalty > 0)
            {
                AddOrUpdateProperty(
                    "Turn Speed",
                    $"-{turnPenalty}",
                    "%",
                    Color.red
                );
            }
            
            if (ergoPenalty > 0)
            {
                AddOrUpdateProperty(
                    "Ergonomics",
                    $"-{ergoPenalty}",
                    "",
                    Color.red
                );
            }
            
            if (!tags.Contains("armor"))
            {
                tags.Add("armor");
            }
            
            if (category == ItemCategory.Helmet && !tags.Contains("helmet"))
            {
                tags.Add("helmet");
            }
        }
        
        public override string GetItemType()
        {
            return category == ItemCategory.Helmet ? "Helmet" : "Body Armor";
        }
        
        private Color GetArmorClassColor(ArmorClass cls)
        {
            switch (cls)
            {
                case ArmorClass.Class1:
                case ArmorClass.Class2:
                    return Color.white;
                case ArmorClass.Class3:
                case ArmorClass.Class4:
                    return Color.green;
                case ArmorClass.Class5:
                case ArmorClass.Class6:
                    return Color.yellow;
                default:
                    return Color.white;
            }
        }
        
        private Color GetDurabilityColor(float current, float max)
        {
            float percentage = current / max;
            
            if (percentage > 0.7f)
                return Color.green;
            else if (percentage > 0.3f)
                return Color.yellow;
            else
                return Color.red;
        }
    }
}