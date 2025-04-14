using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager
    {
        [SerializeField] private float _dropForce = 5f;
        [SerializeField] private float _dropDistance = 1.5f;
        [SerializeField] private float _dropHeight = 1.0f;
        
        // Changed return type from bool to void to match expected signature
    }
}