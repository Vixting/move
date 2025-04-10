using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class ContainerData
    {
        public string id;
        public string displayName;
        public int width;
        public int height;
        public bool allowsFoldedWeapons = true;
        public List<ItemCategory> restrictedCategories = new List<ItemCategory>();
       
        public int Size => width * height;
    }
   
    [CreateAssetMenu(fileName = "New Container Item", menuName = "Inventory/Items/Container")]
    public class ContainerItemData : ItemData
    {
        public int containerWidth = 4;
        public int containerHeight = 4;
        public bool canBeOpened = true;
        public List<ItemCategory> restrictedCategories = new List<ItemCategory>();
       
        public override void OnValidate()
        {
            category = ItemCategory.Container;
            canStack = false;
           
            properties.Clear();
           
            AddOrUpdateProperty("Capacity", $"{containerWidth}x{containerHeight}", "slots", Color.white);
           
            if (restrictedCategories.Count > 0)
            {
                string restrictions = "";
                foreach (var category in restrictedCategories)
                {
                    if (!string.IsNullOrEmpty(restrictions))
                    {
                        restrictions += ", ";
                    }
                    restrictions += category.ToString();
                }
               
                AddOrUpdateProperty("Restrictions", restrictions, "", Color.yellow);
            }
        }
    }
}