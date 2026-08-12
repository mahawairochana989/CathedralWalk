Shader "Cathedral/BridgeGlass"
{
    Properties
    {
        _Color ("Color", Color) = (0.55, 0.35, 0.95, 0.22)
        _Glow ("Glow", Color) = (0.7, 0.3, 1, 1)
        _GlowStrength ("Glow Strength", Range(0, 3)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldN : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _Glow;
            float _GlowStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldN = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float fresnel = pow(1.0 - saturate(dot(normalize(i.worldN), normalize(i.viewDir))), 2.5);
                fixed4 col = _Color;
                col.rgb += _Glow.rgb * fresnel * _GlowStrength;
                col.a = saturate(_Color.a + fresnel * 0.25);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}
