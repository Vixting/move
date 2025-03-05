Shader "Custom/BulletHole"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.1,0.1,0.1,1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.1
        _RimPower ("Rim Power", Range(0.1, 8.0)) = 3.0
        _RimColor ("Rim Color", Color) = (0.2,0.2,0.2,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Alpha;
            float _EdgeSoftness;
            float _RimPower;
            float4 _RimColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                float distSqr = dot(centeredUV, centeredUV) * 4;
                float circle = 1 - smoothstep(0.5 - _EdgeSoftness, 0.5 + _EdgeSoftness, distSqr);
                
                float rim = 1.0 - saturate(dot(i.viewDir, i.worldNormal));
                float rimIntensity = pow(rim, _RimPower);
                
                fixed4 col = lerp(_Color, _RimColor, rimIntensity);
                fixed4 texCol = tex2D(_MainTex, i.uv);
                col *= texCol;
                col.a = circle * _Alpha;
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}