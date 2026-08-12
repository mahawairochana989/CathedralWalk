Shader "Cathedral/SpaceBackdrop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.32, 0.12, 0.5, 1)
        _Emission ("Emission", Float) = 1.35
        _Scroll ("Scroll Speed", Float) = 0.012
        _StarAmount ("Stars", Float) = 0.7
        _Pulse ("Pulse", Float) = 0.2
        _Softness ("Nebula Softness", Float) = 3.5
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest LEqual

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
            float _Scroll;
            float _StarAmount;
            float _Pulse;
            float _Softness;

            struct appdata
            {
                float4 vertex : POSITION;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 localDir : TEXCOORD0;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth value noise (no hard square tiles)
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    v += a * valueNoise(p);
                    p = p * 2.05 + 17.1;
                    a *= 0.5;
                }
                return v;
            }

            // Seamless equirectangular UV from direction
            float2 dirToEquirect(float3 dir)
            {
                dir = normalize(dir);
                float lon = atan2(dir.x, dir.z);
                float lat = asin(clamp(dir.y, -1.0, 1.0));
                return float2(lon / (2.0 * UNITY_PI) + 0.5, lat / UNITY_PI + 0.5);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Direction from sphere center — smooth across faces (no UV seams)
                o.localDir = normalize(v.vertex.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.localDir);
                float2 uv = dirToEquirect(dir);
                uv.x += _Time.y * _Scroll;
                uv.y += sin(_Time.y * _Scroll * 0.7) * 0.01;

                // Soft texture sample (seamless on sphere)
                fixed4 tex = tex2D(_MainTex, uv * _MainTex_ST.xy + _MainTex_ST.zw);
                // Blend a shifted sample to hide any remaining texture seams
                fixed4 tex2 = tex2D(_MainTex, uv * 0.85 + float2(0.37, 0.19));
                tex = lerp(tex, tex2, 0.35);

                float pulse = 1.0 + sin(_Time.y * 1.5) * _Pulse;

                // Soft nebula (fbm — no checkerboard)
                float2 nUV = uv * _Softness + _Time.y * 0.03;
                float neb = fbm(nUV);
                neb = smoothstep(0.35, 0.85, neb);

                // Stars in direction space (tiny, dense, no face-aligned blocks)
                float2 sp = uv * 220.0;
                float2 id = floor(sp);
                float2 f = frac(sp) - 0.5;
                float h = hash21(id);
                float star = 0.0;
                if (h > 1.0 - _StarAmount * 0.12)
                {
                    star = smoothstep(0.35, 0.0, length(f));
                    star *= 0.4 + 0.6 * sin(_Time.y * (3.0 + h * 8.0) + h * 30.0);
                }

                fixed3 col = _Tint.rgb * 0.25;
                col += tex.rgb * _Tint.rgb * 0.95;
                col += fixed3(0.45, 0.18, 0.85) * neb * 0.55;
                col += fixed3(0.25, 0.1, 0.4) * fbm(uv * 1.5 + 9.0) * 0.3;
                col += star * fixed3(1.0, 0.96, 1.0);
                col *= _Emission * pulse;

                // Slight vignette toward poles for depth, smooth
                float pole = abs(dir.y);
                col *= 1.0 - pole * 0.12;

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
