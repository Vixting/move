using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Armor Item", menuName = "Inventory/Items/Armor")]
    public class ArmorItemData : ItemData
    {
        public enum ArmorClass
        {
            Class1 = 1,
            Class2 = 2,
            Class3 = 3,
            Class4 = 4,
            Class5 = 5,
            Class6 = 6
        }
        
        public enum ArmorMaterial
        {
            Cloth,
            Kevlar,
            Ceramic,
            Steel,
            Titanium,
            UHMWPE,
            Combined
        }
        
        public ArmorClass armorClass;
        public ArmorMaterial material;
        public float durability = 100f;
        public float maxDurability = 100f;
        public float movementPenalty = 0f;
        public float turnPenalty = 0f;
        public float ergoPenalty = 0f;
        
        public List<string> armoredAreas = new List<string>();
        
        public void OnValidate()
        {
            if (category != ItemCategory.Armor && category != ItemCategory.Helmet)
            {
                category = ItemCategory.Armor;
            }
            
            stackable = false;
            
            properties.Clear();
            
            properties.Add(new ItemProperty {
                name = "Class",
                value = ((int)armorClass).ToString(),
                unit = "",
                color = GetArmorClassColor(armorClass)
            });
            
            properties.Add(new ItemProperty {
                name = "Material",
                value = material.ToString(),
                unit = "",
                color = Color.white
            });
            
            properties.Add(new ItemProperty {
                name = "Durability",
                value = $"{durability}/{maxDurability}",
                unit = "",
                color = GetDurabilityColor(durability, maxDurability)
            });
            
            if (movementPenalty > 0)
            {
                properties.Add(new ItemProperty {
                    name = "Movement",
                    value = $"-{movementPenalty}",
                    unit = "%",
                    color = Color.red
                });
            }
            
            if (turnPenalty > 0)
            {
                properties.Add(new ItemProperty {
                    name = "Turn Speed",
                    value = $"-{turnPenalty}",
                    unit = "%",
                    color = Color.red
                });
            }
            
            if (ergoPenalty > 0)
            {
                properties.Add(new ItemProperty {
                    name = "Ergonomics",
                    value = $"-{ergoPenalty}",
                    unit = "",
                    color = Color.red
                });
            }
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