using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    public partial class InventoryManager
    {
        public void DropItemToWorld(ItemInstance item)
        {
            if (item == null || item.itemData == null || item.itemData.prefab == null)
            {
                Debug.LogWarning("Cannot drop item: item, item data or prefab is null");
                return;
            }

            Debug.Log($"Attempting to drop item {item.itemData.displayName} into the world");

            Vector3 dropPosition = GetDropPosition();
            
            GameObject droppedItem = Instantiate(item.itemData.prefab, dropPosition, Quaternion.identity);
            
            if (droppedItem != null)
            {
                Debug.Log($"Successfully instantiated item at {dropPosition}");
                ConfigureDroppedItem(droppedItem, item);
                ApplyDropForce(droppedItem);
                RemoveItem(item);
                
                Debug.Log($"Dropped item {item.itemData.displayName} into the world");
            }
            else
            {
                Debug.LogError($"Failed to instantiate prefab for {item.itemData.displayName}");
            }
        }
        
        private Vector3 GetDropPosition()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                return transform.position + Vector3.forward * _dropDistance;
            }
            
            Transform referenceTransform = Camera.main?.transform;
            if (referenceTransform == null)
            {
                referenceTransform = player.transform;
            }
            
            Vector3 dropPosition = player.transform.position + 
                                  referenceTransform.forward * _dropDistance + 
                                  Vector3.up * _dropHeight;
                                  
            return dropPosition;
        }
        
        private void ConfigureDroppedItem(GameObject droppedItem, ItemInstance item)
        {
            if (droppedItem.GetComponent<Collider>() == null)
            {
                BoxCollider collider = droppedItem.AddComponent<BoxCollider>();
                float width = item.GetWidth() * 0.2f;
                float height = item.GetHeight() * 0.2f;
                collider.size = new Vector3(width, height, Mathf.Min(width, height));
            }
            
            if (droppedItem.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = droppedItem.AddComponent<Rigidbody>();
                rb.mass = item.itemData.weight;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            
            WorldItem worldItem = droppedItem.GetComponent<WorldItem>();
            if (worldItem == null)
            {
                worldItem = droppedItem.AddComponent<WorldItem>();
            }
            worldItem.Initialize(item);
        }
        
        private void ApplyDropForce(GameObject droppedItem)
        {
            Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dropDirection = GetDropDirection();
                
                dropDirection += new Vector3(
                    UnityEngine.Random.Range(-0.2f, 0.2f),
                    UnityEngine.Random.Range(0.1f, 0.3f),
                    UnityEngine.Random.Range(-0.2f, 0.2f)
                ).normalized;
                
                rb.AddForce(dropDirection * _dropForce, ForceMode.Impulse);
                
                rb.AddTorque(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    ForceMode.Impulse
                );
            }
        }
        
        private Vector3 GetDropDirection()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                return Vector3.forward;
            }
            
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera.transform.forward;
            }
            
            return player.transform.forward;
        }
    }
}