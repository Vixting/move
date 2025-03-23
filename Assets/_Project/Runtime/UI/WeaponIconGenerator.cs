using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
public class WeaponIconGenerator : MonoBehaviour
{
    [Header("Icon Generation Settings")]
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private int iconResolution = 256;
    [SerializeField] private Camera renderCamera;
    [SerializeField] private Light iconLight;
    [SerializeField] private string savePath = "Assets/Icons/Weapons";
    [SerializeField] private Color backgroundColor = Color.clear;
    [SerializeField] private Vector3 cameraPosition = new Vector3(0, 0, -1);
    [SerializeField] private Vector3 cameraRotation = new Vector3(0, 0, 0);
    [SerializeField] private float cameraSize = 0.5f;
    [SerializeField] private bool useCustomBackground = false;
    [SerializeField] private Material backgroundMaterial;

    [Header("Optional")]
    [SerializeField] private bool autoAssignToWeaponData = true;
    
    private GameObject weaponPreviewObject;
    private GameObject backgroundQuad;

    [MenuItem("Tools/Weapons/Create Weapon Icon Generator")]
    public static void CreateWeaponIconGenerator()
    {
        GameObject generatorObj = new GameObject("WeaponIconGenerator");
        WeaponIconGenerator generator = generatorObj.AddComponent<WeaponIconGenerator>();
        
        // Create and set up the camera
        GameObject cameraObj = new GameObject("IconCamera");
        cameraObj.transform.SetParent(generatorObj.transform);
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.orthographic = true;
        cam.orthographicSize = 0.5f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 10f;
        cam.transform.localPosition = new Vector3(0, 0, -1);
        cam.transform.localRotation = Quaternion.Euler(0, 0, 0);
        
        // Create and set up the light
        GameObject lightObj = new GameObject("IconLight");
        lightObj.transform.SetParent(generatorObj.transform);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.localRotation = Quaternion.Euler(45, 45, 0);
        
        // Set reference
        generator.renderCamera = cam;
        generator.iconLight = light;
        
        // Create the render texture
        generator.CreateRenderTexture();
        
        // Create the directory if it doesn't exist
        if (!Directory.Exists(generator.savePath))
        {
            Directory.CreateDirectory(generator.savePath);
        }
        
        Debug.Log("Weapon Icon Generator created. Select the generator game object and use the custom inspector to generate icons.");
        Selection.activeGameObject = generatorObj;
    }
    
    private void CreateRenderTexture()
    {
        renderTexture = new RenderTexture(iconResolution, iconResolution, 24, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 4;
        renderTexture.Create();
        
        if (renderCamera != null)
        {
            renderCamera.targetTexture = renderTexture;
        }
    }
    
    // Generates an icon for a specific weapon prefab
    public Sprite GenerateIconForWeapon(GameObject weaponPrefab, string weaponName = null)
    {
        if (weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab is null!");
            return null;
        }
        
        // Ensure we have a render texture
        if (renderTexture == null || renderTexture.width != iconResolution)
        {
            CreateRenderTexture();
        }
        
        // Set up camera if needed
        if (renderCamera != null)
        {
            renderCamera.targetTexture = renderTexture;
            renderCamera.orthographicSize = cameraSize;
            renderCamera.transform.localPosition = cameraPosition;
            renderCamera.transform.localRotation = Quaternion.Euler(cameraRotation);
            renderCamera.backgroundColor = backgroundColor;
        }
        else
        {
            Debug.LogError("Render camera not assigned!");
            return null;
        }
        
        // Clean up any previous preview objects
        CleanupPreviewObjects();
        
        // Create background quad if needed
        if (useCustomBackground && backgroundMaterial != null)
        {
            backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backgroundQuad.transform.SetParent(transform);
            backgroundQuad.transform.localPosition = new Vector3(0, 0, 0.5f);
            backgroundQuad.transform.localScale = new Vector3(2, 2, 1);
            
            Renderer quadRenderer = backgroundQuad.GetComponent<Renderer>();
            quadRenderer.material = backgroundMaterial;
        }
        
        // Instantiate the weapon prefab as a child of this generator
        weaponPreviewObject = Instantiate(weaponPrefab, transform);
        
        // Reset transform
        weaponPreviewObject.transform.localPosition = Vector3.zero;
        weaponPreviewObject.transform.localRotation = Quaternion.identity;
        
        // Disable any scripts and colliders
        DisableComponentsInChildren<MonoBehaviour>(weaponPreviewObject);
        DisableComponentsInChildren<Collider>(weaponPreviewObject);
        
        // Auto-orient the weapon
        AutoOrientWeapon(weaponPreviewObject);
        
        // Render the icon
        RenderTexture prevRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        renderCamera.Render();
        
        // Create a texture from the render texture
        Texture2D iconTexture = new Texture2D(iconResolution, iconResolution, TextureFormat.RGBA32, false);
        iconTexture.ReadPixels(new Rect(0, 0, iconResolution, iconResolution), 0, 0);
        iconTexture.Apply();
        
        // Restore previous active render texture
        RenderTexture.active = prevRT;
        
        // Generate the sprite
        Sprite iconSprite = Sprite.Create(
            iconTexture, 
            new Rect(0, 0, iconTexture.width, iconTexture.height), 
            new Vector2(0.5f, 0.5f), 
            100f
        );
        
        // Save the texture as a PNG
        if (!string.IsNullOrEmpty(weaponName))
        {
            string fileName = string.IsNullOrEmpty(weaponName) ? weaponPrefab.name : weaponName;
            SaveIconToDisk(iconTexture, fileName);
            
            // Auto-assign to weapon data if enabled
            if (autoAssignToWeaponData)
            {
                TryAssignIconToWeaponData(fileName, iconSprite);
            }
        }
        
        // Clean up
        CleanupPreviewObjects();
        
        return iconSprite;
    }
    
    private void SaveIconToDisk(Texture2D texture, string weaponName)
    {
        string directoryPath = savePath;
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        string filePath = Path.Combine(directoryPath, $"{weaponName}_Icon.png");
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);
        
        AssetDatabase.Refresh();
        Debug.Log($"Icon saved to: {filePath}");
        
        // Set texture import settings
        string assetPath = filePath.Replace(Application.dataPath, "Assets");
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
    
    private void TryAssignIconToWeaponData(string weaponName, Sprite iconSprite)
    {
        // Find all weapon data assets
        string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            
            // If the weapon name matches and there's no icon already assigned
            if (weaponData != null && weaponData.weaponName == weaponName && weaponData.weaponIcon == null)
            {
                // Find the sprite asset we just created
                string iconPath = Path.Combine(savePath, $"{weaponName}_Icon.png");
                string assetPath = iconPath.Replace(Application.dataPath, "Assets");
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                
                if (sprite != null)
                {
                    weaponData.weaponIcon = sprite;
                    EditorUtility.SetDirty(weaponData);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Assigned icon to {weaponName} weapon data");
                }
            }
        }
    }
    
    // Automatically orient the weapon to face the camera properly
    private void AutoOrientWeapon(GameObject weapon)
    {
        // Get all renderers to find the bounds
        Renderer[] renderers = weapon.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        
        // Calculate the combined bounds
        Bounds combinedBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            combinedBounds.Encapsulate(renderer.bounds);
        }
        
        // Center the weapon based on the bounds
        Vector3 offset = -combinedBounds.center;
        weapon.transform.position += offset;
        
        // Scale the weapon to fit in the camera view
        float maxSize = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        float scale = 0.8f / maxSize; // Scale to fit with some margin
        weapon.transform.localScale *= scale;
        
        // Rotate the weapon to face the camera (can be customized based on your weapon orientations)
        weapon.transform.localRotation = Quaternion.Euler(0, -30, 0);
    }
    
    // Disable all components of type T in the game object and its children
    private void DisableComponentsInChildren<T>(GameObject gameObject) where T : Component
    {
        T[] components = gameObject.GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            if (component is Behaviour behaviour)
            {
                behaviour.enabled = false;
            }
        }
    }
    
    private void CleanupPreviewObjects()
    {
        if (weaponPreviewObject != null)
        {
            DestroyImmediate(weaponPreviewObject);
        }
        
        if (backgroundQuad != null)
        {
            DestroyImmediate(backgroundQuad);
        }
    }
    
    private void OnDestroy()
    {
        CleanupPreviewObjects();
        
        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }
    }
}

[CustomEditor(typeof(WeaponIconGenerator))]
public class WeaponIconGeneratorEditor : Editor
{
    private GameObject selectedWeapon;
    private string customWeaponName = "";
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        WeaponIconGenerator generator = (WeaponIconGenerator)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Generate Single Icon", EditorStyles.boldLabel);
        
        selectedWeapon = EditorGUILayout.ObjectField("Weapon Prefab", selectedWeapon, typeof(GameObject), false) as GameObject;
        customWeaponName = EditorGUILayout.TextField("Weapon Name (Optional)", customWeaponName);
        
        if (GUILayout.Button("Generate Icon") && selectedWeapon != null)
        {
            string weaponName = string.IsNullOrEmpty(customWeaponName) ? selectedWeapon.name : customWeaponName;
            generator.GenerateIconForWeapon(selectedWeapon, weaponName);
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Batch Generate Icons", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Icons For All Weapon Data"))
        {
            BatchGenerateIcons(generator);
        }
    }
    
    private void BatchGenerateIcons(WeaponIconGenerator generator)
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponData");
        int count = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            
            if (weaponData != null && weaponData.weaponPrefab != null)
            {
                generator.GenerateIconForWeapon(weaponData.weaponPrefab, weaponData.weaponName);
                count++;
            }
        }
        
        Debug.Log($"Generated {count} weapon icons.");
    }
}
#endif