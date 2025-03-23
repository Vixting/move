using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // For URP

// This script works with URP (Universal Render Pipeline)
public class DarkScreenEffect : MonoBehaviour, IVolume
{
    [Header("Dark Effect Settings")]
    [Range(0f, 1f)]
    public float darkIntensity = 0.7f;
    
    [Range(0f, 2f)]
    public float contrast = 1.2f;
    
    [Range(-1f, 0f)]
    public float brightnessOffset = -0.3f;
    
    [Range(0f, 1f)]
    public float shadowsMultiplier = 0.6f;
    
    [Header("Color Adjustments")]
    public bool desaturate = true;
    [Range(0f, 1f)]
    public float desaturationAmount = 0.3f;
    
    public Color shadowTint = new Color(0.05f, 0.05f, 0.1f);
    
    // For manual rendering with a material
    private Material darkEffectMaterial;

    public bool isGlobal { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public List<Collider> colliders => throw new System.NotImplementedException();

    void Start()
    {
        // Create material for screen effect if needed
        if (darkEffectMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/DarkScreenEffect");
            if (shader == null)
            {
                Debug.LogError("DarkScreenEffect shader not found!");
                enabled = false;
                return;
            }
            
            darkEffectMaterial = new Material(shader);
        }
        
        // URP specific setup - for camera data
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            // Get the camera data
            UniversalAdditionalCameraData cameraData = cam.GetUniversalAdditionalCameraData();
            
            // Setup renderer features if needed
            // Note: This part depends on how you've structured your render pipeline assets
        }
    }
    
    // If using a custom render feature with URP
    // This is a simplified example - actual implementation depends on your render pipeline setup
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (darkEffectMaterial != null)
        {
            // Set shader parameters
            darkEffectMaterial.SetFloat("_DarkIntensity", darkIntensity);
            darkEffectMaterial.SetFloat("_Contrast", contrast);
            darkEffectMaterial.SetFloat("_BrightnessOffset", brightnessOffset);
            darkEffectMaterial.SetFloat("_ShadowsMultiplier", shadowsMultiplier);
            darkEffectMaterial.SetFloat("_Desaturation", desaturate ? desaturationAmount : 0);
            darkEffectMaterial.SetColor("_ShadowTint", shadowTint);
            
            // Apply the effect
            Graphics.Blit(source, destination, darkEffectMaterial);
        }
        else
        {
            // Fallback if material is missing
            Graphics.Blit(source, destination);
        }
    }
}

// Here's a sample shader for the dark screen effect
/* 
Shader "Hidden/DarkScreenEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DarkIntensity ("Dark Intensity", Range(0, 1)) = 0.7
        _Contrast ("Contrast", Range(0, 2)) = 1.2
        _BrightnessOffset ("Brightness Offset", Range(-1, 0)) = -0.3
        _ShadowsMultiplier ("Shadows Multiplier", Range(0, 1)) = 0.6
        _Desaturation ("Desaturation", Range(0, 1)) = 0.3
        _ShadowTint ("Shadow Tint", Color) = (0.05, 0.05, 0.1, 1)
    }
    
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float _DarkIntensity;
            float _Contrast;
            float _BrightnessOffset;
            float _ShadowsMultiplier;
            float _Desaturation;
            fixed4 _ShadowTint;
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Calculate luminance
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // Apply brightness offset and contrast
                col.rgb = (col.rgb + _BrightnessOffset) * _Contrast;
                
                // Darken shadows more than highlights
                float shadowFactor = 1.0 - luminance;
                col.rgb = lerp(col.rgb, col.rgb * _ShadowsMultiplier, shadowFactor * _DarkIntensity);
                
                // Apply desaturation
                col.rgb = lerp(col.rgb, float3(luminance, luminance, luminance), _Desaturation);
                
                // Apply shadow tint
                col.rgb = lerp(col.rgb, col.rgb * _ShadowTint.rgb, shadowFactor * _DarkIntensity);
                
                return col;
            }
            ENDCG
        }
    }
}
*/