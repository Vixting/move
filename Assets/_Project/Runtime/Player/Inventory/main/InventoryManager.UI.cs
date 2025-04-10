using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager //InventoryManager.UI.cs
    {
        public void ShowInventory()
        {
            ReconnectAllReferences();
            
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _root.Focus();
            }
            
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                player.EnableGameplayMode(false);
            }
        }
        
        public void HideInventory()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                
                VisualElement infoPanel = _root.Q("item-info-panel");
                if (infoPanel != null)
                {
                    infoPanel.style.display = DisplayStyle.None;
                }
                
                if (_contextMenu != null)
                {
                    _contextMenu.RemoveFromHierarchy();
                    _contextMenu = null;
                }
            }
        }
        
        public void ToggleInventory()
        {
            if (_root == null)
            {
                Debug.LogError("Inventory root is null in ToggleInventory, cannot toggle inventory");
                return;
            }
            
            Debug.Log($"ToggleInventory called - current display: {_root.style.display}");
            
            if (_root.style.display == DisplayStyle.None)
            {
                ShowInventory();
            }
            else
            {
                HideInventory();
            }
        }
    }
}