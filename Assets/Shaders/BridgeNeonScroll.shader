Shader "Cathedral/BridgeNeonScroll"
{
    Properties
    {
        _Color ("Neon Color", Color) = (0.85, 0.2, 1, 1)
        _CoreColor ("Core Color", Color) = (1, 0.7, 1, 1)
        _Speed ("Scroll Speed", Float) = 2.5
        _StripeCount ("Stripe Density", Float) = 0.08
        _StripeWidth ("Stripe Width", Range(0.05, 0.5)) = 0.22
        _Emission ("Emission", Float) = 5
        _UseWorld ("Use World Pos", Float) = 1
        _WorldAxis ("World Axis 0=X 1=Z", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
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
                float2 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _CoreColor;
            float _Speed;
            float _StripeCount;
            float _StripeWidth;
            float _Emission;
            float _UseWorld;
            float _WorldAxis;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float coord;
                if (_UseWorld > 0.5)
                    coord = lerp(i.worldPos.x, i.worldPos.z, saturate(_WorldAxis));
                else
                    coord = i.uv.y;

                float t = coord * _StripeCount - _Time.y * _Speed;
                float band = abs(frac(t) - 0.5) * 2.0;
                float stripe = smoothstep(_StripeWidth, 0.0, band);
                float core = smoothstep(_StripeWidth * 0.4, 0.0, band);
                fixed3 rgb = _Color.rgb * stripe + _CoreColor.rgb * core;
                float a = saturate(stripe * 0.85 + core);
                return fixed4(rgb * _Emission * a, a);
            }
            ENDCG
        }
    }
}
