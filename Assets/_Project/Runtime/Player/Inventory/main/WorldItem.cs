using System;
using System.Collections;
using UnityEngine;
using InventorySystem;

/// <summary>
/// Component that represents an item dropped in the world
/// </summary>
public class WorldItem : MonoBehaviour
{
    private ItemData _itemData;
    private int _stackCount = 1;
    private bool _isRotated = false;
    private float _currentDurability = 100f;
    private int _currentAmmoCount = 0;
    
    [SerializeField] private float _interactionRadius = 2.0f;
    [SerializeField] private float _pickupCooldown = 0.5f;
    [SerializeField] private GameObject _interactionPrompt;
    
    private bool _canPickup = false;
    
    /// <summary>
    /// Initialize the world item with data from an inventory item instance
    /// </summary>
    public void Initialize(ItemInstance item)
    {
        if (item == null || item.itemData == null)
        {
            Debug.LogError("Cannot initialize WorldItem with null item or item data");
            return;
        }
        
        _itemData = item.itemData;
        _stackCount = item.stackCount;
        _isRotated = item.isRotated;
        _currentDurability = item.currentDurability;
        
        if (item.itemData is WeaponItemData weaponData)
        {
            _currentAmmoCount = item.currentAmmoCount;
        }
        
        // Set the name for easier debugging
        gameObject.name = $"WorldItem_{_itemData.displayName}";
        
        // Create pickup interaction trigger
        SetupInteractionTrigger();
        
        // Start cooldown to prevent immediate pickup
        StartCoroutine(EnablePickupAfterDelay());
    }
    
    private void SetupInteractionTrigger()
    {
        // Create a sphere collider for interaction
        SphereCollider interactionTrigger = gameObject.AddComponent<SphereCollider>();
        interactionTrigger.radius = _interactionRadius;
        interactionTrigger.isTrigger = true;
        
        // Create a pickup prompt if specified
        if (_interactionPrompt != null)
        {
            GameObject prompt = Instantiate(_interactionPrompt, transform);
            prompt.SetActive(false);
        }
    }
    
    private IEnumerator EnablePickupAfterDelay()
    {
        _canPickup = false;
        yield return new WaitForSeconds(_pickupCooldown);
        _canPickup = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the interaction zone
        if (_canPickup && other.CompareTag("Player"))
        {
            // Show pickup prompt
            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(true);
            }
            
            // Enable pickup interaction
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                // Subscribe to input event for pickup
                // This would depend on your input system implementation
                // For example, you might want to register with the player's input actions
                PlayerInputActions inputActions = player.GetInputActions();
                if (inputActions != null)
                {
                    inputActions.Gameplay.Interact.performed += OnInteractPerformed;
                }
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        // Check if the player exited the interaction zone
        if (other.CompareTag("Player"))
        {
            // Hide pickup prompt
            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(false);
            }
            
            // Disable pickup interaction
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                // Unsubscribe from input event
                PlayerInputActions inputActions = player.GetInputActions();
                if (inputActions != null)
                {
                    inputActions.Gameplay.Interact.performed -= OnInteractPerformed;
                }
            }
        }
    }
    
    private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // Attempt to pick up the item
        PickupItem();
    }
    
    private void PickupItem()
    {
        if (!_canPickup || _itemData == null) return;
        
        // Get the nearest player
        Player player = FindObjectOfType<Player>();
        if (player == null) return;
        
        // Get inventory manager
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null) return;
        
        // Create a new ItemInstance
        ItemInstance newItem = new ItemInstance(_itemData, Vector2Int.zero, null);
        newItem.stackCount = _stackCount;
        newItem.currentDurability = _currentDurability;
        newItem.isRotated = _isRotated;
        
        if (_itemData is WeaponItemData)
        {
            newItem.currentAmmoCount = _currentAmmoCount;
        }
        
        // Add to proper container (backpack first, then stash)
        bool added = false;
        if (inventoryManager.GetContainers().TryGetValue("backpack", out ContainerInstance backpack))
        {
            Vector2Int? availablePos = backpack.FindAvailablePosition(newItem);
            if (availablePos.HasValue)
            {
                added = inventoryManager.AddItemToContainer(_itemData, "backpack", availablePos.Value, _isRotated) != null;
            }
        }
        
        // Try stash if backpack fails
        if (!added && inventoryManager.GetContainers().TryGetValue("stash", out ContainerInstance stash))
        {
            Vector2Int? availablePos = stash.FindAvailablePosition(newItem);
            if (availablePos.HasValue)
            {
                added = inventoryManager.AddItemToContainer(_itemData, "stash", availablePos.Value, _isRotated) != null;
            }
        }
        
        if (added)
        {
            // Play pickup sound if available
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && audioSource.clip != null)
            {
                AudioSource.PlayClipAtPoint(audioSource.clip, transform.position);
            }
            
            // Destroy the world item
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Cannot pick up item: no space in inventory");
            // Could show a message to the player here
        }
    }
    
    // You might want to add a method to highlight the item when looking at it
    public void Highlight(bool active)
    {
        // Implement highlighting logic here
        // For example, outline shader, change material, etc.
    }
}