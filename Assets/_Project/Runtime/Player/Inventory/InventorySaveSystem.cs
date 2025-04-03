using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class InventoryState
    {
        public List<SavedItemData> ContainerItems = new List<SavedItemData>();
        public List<SavedEquipmentData> EquippedItems = new List<SavedEquipmentData>();
    }

    [Serializable]
    public class SavedItemData
    {
        public string ItemId;
        public string ContainerId;
        public int X;
        public int Y;
        public bool IsRotated;
        public int CurrentStack;
        public float CurrentDurability;
        public Dictionary<string, string> CustomData = new Dictionary<string, string>();
    }

    [Serializable]
    public class SavedEquipmentData
    {
        public string ItemId;
        public int SlotIndex;
        public bool IsRotated;
        public int CurrentStack;
        public float CurrentDurability;
        public Dictionary<string, string> CustomData = new Dictionary<string, string>();
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [Serializable]
        public class Entry
        {
            public TKey Key;
            public TValue Value;
        }

        public List<Entry> Entries = new List<Entry>();

        public Dictionary<TKey, TValue> ToDictionary()
        {
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();
            foreach (var entry in Entries)
            {
                result[entry.Key] = entry.Value;
            }
            return result;
        }

        public void FromDictionary(Dictionary<TKey, TValue> dictionary)
        {
            Entries.Clear();
            foreach (var kvp in dictionary)
            {
                Entries.Add(new Entry { Key = kvp.Key, Value = kvp.Value });
            }
        }
    }

    [Serializable]
    public class StringDictionary : SerializableDictionary<string, string> { }
}