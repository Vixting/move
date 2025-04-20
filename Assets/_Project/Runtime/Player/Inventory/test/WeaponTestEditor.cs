// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
// using InventorySystem;

// #if UNITY_EDITOR
// [CustomEditor(typeof(MonoBehaviour), true)]
// public class CreateWeaponsEditor : Editor
// {
//     [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
    
//     private bool showWeapons = true;
//     private InventoryManager inventoryManager;
    
//     public override void OnInspectorGUI()
//     {
//         // Draw the default inspector
//         DrawDefaultInspector();
        
//         // Find the inventory manager if needed
//         if (Application.isPlaying && inventoryManager == null)
//         {
//             inventoryManager = FindObjectOfType<InventoryManager>();
//         }
        
//         // Add a section for weapon creation
//         EditorGUILayout.Space(10);
//         showWeapons = EditorGUILayout.Foldout(showWeapons, "Create Weapons", true, EditorStyles.foldoutHeader);
        
//         if (showWeapons)
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
//             // Show button to find weapons in the project
//             if (GUILayout.Button("Find Weapons in Project"))
//             {
//                 FindWeapons();
//             }
            
//             // List available weapons with buttons to add them
//             if (availableWeapons.Count > 0)
//             {
//                 EditorGUILayout.LabelField("Available Weapons:", EditorStyles.boldLabel);
                
//                 foreach (var weapon in availableWeapons)
//                 {
//                     if (weapon != null)
//                     {
//                         EditorGUILayout.BeginHorizontal();
                        
//                         // Show weapon icon if available
//                         if (weapon.weaponIcon != null)
//                         {
//                             GUILayout.Label(new GUIContent(AssetPreview.GetAssetPreview(weapon.weaponIcon)), 
//                                 GUILayout.Width(30), GUILayout.Height(30));
//                         }
                        
//                         // Show weapon name and type
//                         EditorGUILayout.LabelField($"{weapon.weaponName} ({weapon.weaponType})", 
//                             EditorStyles.boldLabel, GUILayout.Width(150));
                        
//                         // Show ID for reference
//                         EditorGUILayout.LabelField($"ID: {weapon.inventoryItemId}", 
//                             GUILayout.Width(100));
                        
//                         // Button to add the weapon to inventory
//                         GUI.enabled = Application.isPlaying && inventoryManager != null;
//                         if (GUILayout.Button("Add to Inventory", GUILayout.Height(24)))
//                         {
//                             AddWeaponToInventory(weapon);
//                         }
//                         GUI.enabled = true;
                        
//                         EditorGUILayout.EndHorizontal();
//                     }
//                 }
//             }
//             else
//             {
//                 EditorGUILayout.HelpBox("No weapons found. Click 'Find Weapons in Project' to search for weapon assets.", 
//                     MessageType.Info);
//             }
            
//             if (!Application.isPlaying)
//             {
//                 EditorGUILayout.HelpBox("Enter Play Mode to add weapons to inventory", 
//                     MessageType.Info);
//             }
            
//             EditorGUILayout.EndVertical();
//         }
//     }
    
//     private void FindWeapons()
//     {
//         availableWeapons.Clear();
//         string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        
//         foreach (string guid in guids)
//         {
//             string path = AssetDatabase.GUIDToAssetPath(guid);
//             WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            
//             if (weapon != null)
//             {
//                 availableWeapons.Add(weapon);
//             }
//         }
        
//         Debug.Log($"Found {availableWeapons.Count} weapons in the project");
//     }
    
//     private void AddWeaponToInventory(WeaponData weapon)
//     {
//         if (!Application.isPlaying || inventoryManager == null || weapon == null)
//             return;
            
//         if (string.IsNullOrEmpty(weapon.inventoryItemId))
//         {
//             Debug.LogError($"Weapon {weapon.name} has no inventory item ID set!");
//             return;
//         }
        
//         inventoryManager.AddWeaponToInventory(weapon.inventoryItemId, true);
//         Debug.Log($"Added weapon {weapon.weaponName} to inventory (ID: {weapon.inventoryItemId})");
//     }
// }
// #endif