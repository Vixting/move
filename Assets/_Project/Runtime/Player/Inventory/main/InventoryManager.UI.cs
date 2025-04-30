using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace InventorySystem
{
    public partial class InventoryManager : MonoBehaviour
    {
        private void InitializeUI()
        {
            if (inventoryDocument == null || inventoryDocument.rootVisualElement == null)
            {
                Debug.LogWarning("Inventory document missing or not properly set up");
                return;
            }
            
            _root = inventoryDocument.rootVisualElement.Q("inventory-root");
            if (_root == null)
            {
                Debug.LogWarning("Inventory root element not found");
                return;
            }
            
            _inventoryContent = _root.Q("inventory-content");
            
            SetupCloseButton();
            SetupEquipmentUI();
            //CreateSettingsUI();
            
            _root.RegisterCallback<PointerDownEvent>(evt => {
                _root.CapturePointer(evt.pointerId);
            });
            
            _root.RegisterCallback<PointerUpEvent>(evt => {
                _root.ReleasePointer(evt.pointerId);
            });
            
            _root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _root.RegisterCallback<MouseUpEvent>(OnMouseUp);
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            
            _root.RegisterCallback<MouseDownEvent>(evt => {
                if (_contextMenu != null)
                {
                    _contextMenu.RemoveFromHierarchy();
                    _contextMenu = null;
                }
            });
            
            UpdateHealthAndStats();
        }

        private void CreateSettingsUI()
        {
            if (_root == null) return;
            
            VisualElement settingsContainer = _root.Q("inventory-settings-container");
            if (settingsContainer == null)
            {
                settingsContainer = new VisualElement();
                settingsContainer.name = "inventory-settings-container";
                settingsContainer.style.position = Position.Absolute;
                settingsContainer.style.top = 10;
                settingsContainer.style.right = 10;
                settingsContainer.style.width = 200;
                settingsContainer.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.8f));
                settingsContainer.style.paddingTop = 5;
                settingsContainer.style.paddingBottom = 5;
                settingsContainer.style.paddingLeft = 10;
                settingsContainer.style.paddingRight = 10;
                _root.Add(settingsContainer);
            }
            
            Label settingsTitle = new Label("Inventory Settings");
            settingsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            settingsTitle.style.marginBottom = 10;
            settingsContainer.Add(settingsTitle);
            
            AddToggleSetting(settingsContainer, "Show Grid Helpers", _showGridHelpers, (evt) => {
                _showGridHelpers = evt.newValue;
            });
            
            AddToggleSetting(settingsContainer, "Enable Grid Snapping", _enableSnapping, (evt) => {
                _enableSnapping = evt.newValue;
            });
            
            AddToggleSetting(settingsContainer, "Enable Magnetic Drop", _enableMagneticDrop, (evt) => {
                _enableMagneticDrop = evt.newValue;
                _magneticDrop = evt.newValue;
            });
        }

        private void AddToggleSetting(VisualElement container, string label, bool initialValue, EventCallback<ChangeEvent<bool>> callback)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 5;
            
            Label settingLabel = new Label(label);
            settingLabel.style.width = new Length(70, LengthUnit.Percent);
            
            Toggle toggle = new Toggle();
            toggle.value = initialValue;
            toggle.RegisterValueChangedCallback(callback);
            toggle.style.width = new Length(30, LengthUnit.Percent);
            
            row.Add(settingLabel);
            row.Add(toggle);
            container.Add(row);
        }
        
        private void SetupCloseButton()
        {
            Button closeButton = _root.Q<Button>("close-button");
            if (closeButton != null)
            {
                closeButton.clicked += () => 
                {
                    HideInventory();
                    if (_isInMainMenu)
                    {
                        _isInMainMenu = false;
                        _onStashCloseCallback?.Invoke();
                    }
                    else
                    {
                        Player player = FindObjectOfType<Player>();
                        if (player != null)
                        {
                            player.EnableGameplayMode(true);
                        }
                    }
                };
            }
        }
    }
}