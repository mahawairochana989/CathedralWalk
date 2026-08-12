Shader "Cathedral/SpacePlanet"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 0.85, 1, 1)
        _Emission ("Emission", Float) = 1.8
        _Pulse ("Pulse", Float) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _Emission;
            float _Pulse;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float pulse = 1.0 + sin(_Time.y * 2.2) * _Pulse;
                // Soft circular mask if square billboard
                float2 c = i.uv * 2 - 1;
                float mask = saturate(1.0 - length(c));
                mask = pow(mask, 0.55);
                fixed3 rgb = tex.rgb * _Tint.rgb * _Emission * pulse;
                float a = tex.a * mask;
                return fixed4(rgb * a, a);
            }
            ENDCG
        }
    }
}
