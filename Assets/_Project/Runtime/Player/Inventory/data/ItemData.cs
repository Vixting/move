// ItemData.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace InventorySystem
{
    [Serializable]
    public class ItemProperty
    {
        public string name;
        public string value;
        public string unit;
        public Color color = Color.white;
    }

    [Serializable]
    public class AttachmentPoint
    {
        public string pointName;
        public AttachmentType allowedTypes;
        public Transform attachmentTransform;
    }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string displayName;
        public string description;
        public int width = 1;
        public int height = 1;
        public float weight = 0.5f;
        public Sprite icon;
        public GameObject prefab;
        public ItemCategory category;
        public ItemRarity rarity;
        public List<ItemProperty> properties = new List<ItemProperty>();
        public List<string> tags = new List<string>();
        public bool rotatable = true;
        public bool stackable = false;
        public int maxStackSize = 1;
        
        [NonSerialized] public bool isRotated = false;
        
        public int GridWidth => isRotated ? height : width;
        public int GridHeight => isRotated ? width : height;
    }
}