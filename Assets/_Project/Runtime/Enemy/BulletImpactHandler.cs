using UnityEngine;
using System.Collections;

public class BulletImpactEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Light impactLight;
    
    private Renderer[] renderers;
    
    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        if (impactLight == null)
        {
            impactLight = GetComponentInChildren<Light>();
            
            // If no light found but effect might need one, create it
            if (impactLight == null && Random.value > 0.5f)
            {
                GameObject lightObj = new GameObject("ImpactLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.zero;
                
                impactLight = lightObj.AddComponent<Light>();
                impactLight.type = LightType.Point;
                impactLight.range = Random.Range(1f, 3f);
                impactLight.intensity = Random.Range(0.5f, 2f);
                impactLight.color = new Color(1f, 0.7f, 0.3f); // Yellowish
            }
        }
        
        // Start lifetime countdown
        StartCoroutine(LifetimeRoutine());
    }
    
    private void Start()
    {
        // Play particles
        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }
        }
        
        // Play sound if available
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }
    
    private IEnumerator LifetimeRoutine()
    {
        // Wait for main lifetime
        float nonFadeLifetime = lifetime - (fadeOut ? fadeOutDuration : 0);
        yield return new WaitForSeconds(nonFadeLifetime);
        
        // Handle light fade
        if (impactLight != null && fadeOut)
        {
            float initialIntensity = impactLight.intensity;
            float fadeTime = 0;
            
            while (fadeTime < fadeOutDuration)
            {
                fadeTime += Time.deltaTime;
                float normalizedTime = fadeTime / fadeOutDuration;
                impactLight.intensity = Mathf.Lerp(initialIntensity, 0, normalizedTime);
                yield return null;
            }
            
            impactLight.intensity = 0;
        }
        
        // Handle renderer fade
        if (renderers != null && renderers.Length > 0 && fadeOut)
        {
            // Store initial alpha values
            float[] initialAlphas = new float[renderers.Length];
            Material[][] materials = new Material[renderers.Length][];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    materials[i] = renderers[i].materials;
                    initialAlphas[i] = 0;
                    
                    foreach (var material in materials[i])
                    {
                        if (material.HasProperty("_Color"))
                        {
                            Color color = material.color;
                            initialAlphas[i] = color.a;
                        }
                    }
                }
            }
            
            // Fade out
            float fadeTime = 0;
            while (fadeTime < fadeOutDuration)
            {
                fadeTime += Time.deltaTime;
                float normalizedTime = fadeTime / fadeOutDuration;
                
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        foreach (var material in materials[i])
                        {
                            if (material.HasProperty("_Color"))
                            {
                                Color color = material.color;
                                color.a = Mathf.Lerp(initialAlphas[i], 0, normalizedTime);
                                material.color = color;
                            }
                        }
                    }
                }
                
                yield return null;
            }
        }
        
        // Finally destroy the game object
        Destroy(gameObject);
    }
    
    // Can be called externally to set the impact effect's appearance based on surface
    public void SetEffectForSurface(string surfaceTag)
    {
        Color effectColor = Color.white;
        
        switch (surfaceTag)
        {
            case "Metal":
                effectColor = new Color(1f, 0.7f, 0.3f); // Orange/yellow sparks
                break;
                
            case "Wood":
                effectColor = new Color(0.7f, 0.5f, 0.3f); // Brown wood chips
                break;
                
            case "Concrete":
                effectColor = new Color(0.8f, 0.8f, 0.8f); // Grey dust
                break;
                
            case "Flesh":
            case "Enemy":
                effectColor = new Color(0.8f, 0.1f, 0.1f); // Red blood
                break;
        }
        
        // Apply color to particle systems if available
        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ParticleSystem.MainModule main = ps.main;
                    main.startColor = effectColor;
                }
            }
        }
        
        // Apply color to light if available
        if (impactLight != null)
        {
            impactLight.color = effectColor;
        }
    }
}