using UnityEngine;

public class DarkLightingController : MonoBehaviour
{
    [Header("Global Lighting Settings")]
    [Range(0f, 1f)]
    public float globalDarknessIntensity = 0.5f;
    [Range(0f, 1f)]
    public float ambientLightMultiplier = 0.3f;
    
    [Header("Post-processing")]
    public bool usePostProcessing = true;
    [Range(0f, 5f)]
    public float vignetteIntensity = 1.2f;
    [Range(-2f, 0f)]
    public float exposureAdjustment = -0.7f;
    
    [Header("Light Sources")]
    public Light[] sceneLights;
    [Range(0f, 1f)]
    public float lightIntensityMultiplier = 0.6f;
    
    // Original light intensities
    private float[] originalIntensities;
    
    // Original ambient light
    private Color originalAmbientLight;
    private float originalAmbientIntensity;
    
    void Start()
    {
        // Store original light settings
        originalAmbientLight = RenderSettings.ambientLight;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        
        // Find all lights in the scene if none assigned
        if (sceneLights == null || sceneLights.Length == 0)
        {
            sceneLights = FindObjectsOfType<Light>();
        }
        
        // Store original intensities
        originalIntensities = new float[sceneLights.Length];
        for (int i = 0; i < sceneLights.Length; i++)
        {
            originalIntensities[i] = sceneLights[i].intensity;
        }
        
        // Apply dark lighting
        ApplyDarkLighting();
    }
    
    public void ApplyDarkLighting()
    {
        // Adjust ambient lighting
        RenderSettings.ambientLight = originalAmbientLight * ambientLightMultiplier;
        RenderSettings.ambientIntensity = originalAmbientIntensity * ambientLightMultiplier;
        
        // Adjust light sources
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
            {
                sceneLights[i].intensity = originalIntensities[i] * lightIntensityMultiplier;
            }
        }
        
        // Apply post-processing if enabled
        if (usePostProcessing)
        {
            SetupPostProcessing();
        }
    }
    
    private void SetupPostProcessing()
    {
        // Note: This requires the Post Processing package
        // You'll need to implement this section based on which post-processing system you're using
        // (Unity's Post Processing Stack v2, URP Post Processing, or HDRP)
        
        // Example for Post Processing Stack v2:
        // UnityEngine.Rendering.PostProcessing.PostProcessVolume volume = GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
        // if (volume != null && volume.profile != null)
        // {
        //     if (volume.profile.TryGetSettings(out UnityEngine.Rendering.PostProcessing.Vignette vignette))
        //     {
        //         vignette.intensity.value = vignetteIntensity;
        //     }
        //     if (volume.profile.TryGetSettings(out UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading))
        //     {
        //         colorGrading.postExposure.value = exposureAdjustment;
        //     }
        // }
    }
    
    // Optional: Add this method to create a reset function
    public void ResetLighting()
    {
        RenderSettings.ambientLight = originalAmbientLight;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        
        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
            {
                sceneLights[i].intensity = originalIntensities[i];
            }
        }
    }
    
    // For runtime adjustments
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyDarkLighting();
        }
    }
}