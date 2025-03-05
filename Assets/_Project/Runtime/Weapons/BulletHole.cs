using UnityEngine;
using System.Collections;

public class BulletHole : MonoBehaviour
{
    private Material materialInstance;
    private float fadeTime;
    private MeshRenderer meshRenderer;
    private static readonly int AlphaProperty = Shader.PropertyToID("_Alpha");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private Coroutine fadeCoroutine;

    public void Initialize(Material material, float lifetime)
    {
        fadeTime = lifetime;
        meshRenderer = GetComponent<MeshRenderer>();
        materialInstance = new Material(material);
        
        // Set initial material properties
        materialInstance.SetFloat(AlphaProperty, 1f);
        materialInstance.SetColor(ColorProperty, new Color(0.1f, 0.1f, 0.1f, 1f));
        
        meshRenderer.material = materialInstance;
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0;
        float startTime = Time.time;

        while (elapsedTime < fadeTime)
        {
            elapsedTime = Time.time - startTime;
            float normalizedTime = elapsedTime / fadeTime;
            float alpha = 1f - normalizedTime;
            materialInstance.SetFloat(AlphaProperty, alpha);
            yield return null;
        }

        CleanupAndDestroy();
    }

    private void CleanupAndDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
        
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }

    private void OnDestroy()
    {
        CleanupAndDestroy();
    }
}