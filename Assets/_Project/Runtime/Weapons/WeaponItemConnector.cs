using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using InventorySystem;

#if UNITY_EDITOR
public class WeaponItemConnector : EditorWindow
{
    private ItemDatabase itemDatabase;
    private List<ItemData> weaponItems = new List<ItemData>();
    private List<WeaponData> weaponDatas = new List<WeaponData>();
    private Vector2 scrollPosition;
    private bool showLinked = true;
    private bool showUnlinked = true;
    private string searchFilter = "";

    [MenuItem("Tools/Weapons/Connect Weapons")]
    public static void ShowWindow()
    {
        GetWindow<WeaponItemConnector>("Weapon Connector");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Weapon Item to Weapon Data Connector", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        itemDatabase = (ItemDatabase)EditorGUILayout.ObjectField("Item Database", itemDatabase, typeof(ItemDatabase), false);
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Refresh Weapons"))
        {
            RefreshWeaponLists();
        }
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        showLinked = EditorGUILayout.Toggle("Show Linked", showLinked);
        showUnlinked = EditorGUILayout.Toggle("Show Unlinked", showUnlinked);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        if (itemDatabase != null && weaponItems.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            int linkedCount = 0;
            int unlinkedCount = 0;
            
            foreach (var item in weaponItems)
            {
                if (!(item is WeaponItemData weaponItem))
                    continue;
                
                bool isLinked = weaponItem.gameplayWeaponData != null;
                
                if ((isLinked && !showLinked) || (!isLinked && !showUnlinked))
                    continue;
                    
                if (!string.IsNullOrEmpty(searchFilter) && !weaponItem.displayName.ToLower().Contains(searchFilter.ToLower()))
                    continue;
                    
                if (isLinked)
                    linkedCount++;
                else
                    unlinkedCount++;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(weaponItem.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"ID: {weaponItem.id}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                WeaponData currentData = weaponItem.gameplayWeaponData;
                WeaponData newData = (WeaponData)EditorGUILayout.ObjectField("Gameplay Weapon Data", currentData, typeof(WeaponData), false);
                
                if (newData != currentData)
                {
                    // We can't use Undo.RecordObject for WeaponItemData since it's not a UnityEngine.Object
                    
                    // Clear old link if necessary
                    if (currentData != null && currentData.inventoryItemId == weaponItem.id)
                    {
                        Undo.RecordObject(currentData, "Clear Weapon Data Link");
                        currentData.inventoryItemId = "";
                        EditorUtility.SetDirty(currentData);
                    }
                    
                    // Set new link
                    weaponItem.gameplayWeaponData = newData;
                    
                    // Update the inventoryItemId in the WeaponData
                    if (newData != null)
                    {
                        Undo.RecordObject(newData, "Set Weapon Data Link");
                        newData.inventoryItemId = weaponItem.id;
                        EditorUtility.SetDirty(newData);
                    }
                    
                    // Since we can't directly record the WeaponItemData for undo,
                    // we need to mark the ItemDatabase dirty instead
                    EditorUtility.SetDirty(itemDatabase);
                }
                
                if (currentData != null)
                {
                    EditorGUILayout.Space(5);
                    
                    // Show weapon data details
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Weapon Name: {currentData.weaponName}");
                    EditorGUILayout.LabelField($"Weapon Slot: {currentData.weaponSlot}");
                    EditorGUILayout.LabelField($"Ammo: {currentData.maxAmmo}");
                    
                    // Check if IDs match
                    if (currentData.inventoryItemId != weaponItem.id)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.HelpBox("ID Mismatch! WeaponData.inventoryItemId doesn't match WeaponItemData.id", MessageType.Warning);
                        
                        if (GUILayout.Button("Fix ID Mismatch"))
                        {
                            Undo.RecordObject(currentData, "Fix ID Mismatch");
                            currentData.inventoryItemId = weaponItem.id;
                            EditorUtility.SetDirty(currentData);
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Showing {linkedCount} linked and {unlinkedCount} unlinked weapons", EditorStyles.miniLabel);
            
            if (GUILayout.Button("Save All Changes"))
            {
                AssetDatabase.SaveAssets();
            }
        }
        else if (itemDatabase != null)
        {
            EditorGUILayout.HelpBox("No weapon items found in the database.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Please select an Item Database asset.", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void RefreshWeaponLists()
    {
        weaponItems.Clear();
        weaponDatas.Clear();
        
        if (itemDatabase != null)
        {
            weaponItems.AddRange(itemDatabase.GetAllItems());
        }
        
        string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            
            if (weaponData != null)
            {
                weaponDatas.Add(weaponData);
            }
        }
    }
}
#endif