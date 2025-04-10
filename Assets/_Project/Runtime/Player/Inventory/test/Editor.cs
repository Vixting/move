#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TestItemCreator))]
public class TestItemCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TestItemCreator creator = (TestItemCreator)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create Test Items", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Create Medkit", GUILayout.Height(30)))
        {
            creator.CreateMedkit();
        }
        
        if (GUILayout.Button("Create Bandage", GUILayout.Height(30)))
        {
            creator.CreateBandage();
        }
        
        if (GUILayout.Button("Create Painkillers", GUILayout.Height(30)))
        {
            creator.CreatePainkillers();
        }
        
        EditorGUILayout.EndHorizontal();
    }
}
#endif