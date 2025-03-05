using UnityEngine;

public class BulletHoleMaterialCreator : MonoBehaviour
{
    public static Material CreateMaterial()
    {
        Material material = new Material(Shader.Find("Assets/_Project/Art/BulletHole.shader"));
        Texture2D texture = CreateBulletHoleTexture();
        material.mainTexture = texture;
        return material;
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return x * x * (3 - 2 * x);
    }

    private static Texture2D CreateBulletHoleTexture()
    {
        int resolution = 128;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x / (float)resolution - 0.5f;
                float dy = y / (float)resolution - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float alpha = Mathf.Clamp01(1f - SmoothStep(0.8f, 1f, distance));
                float darkness = Mathf.Lerp(0.1f, 0.6f, 1f - distance);

                Color color = new Color(darkness, darkness, darkness, alpha);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}