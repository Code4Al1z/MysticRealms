Shader "Custom/TMP_ArcaneOverload"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Base Text Color", Color) = (0.1, 0.1, 0.1, 1)
        _BoltColor ("Lightning Color", Color) = (0.4, 0.7, 1.0, 1)
        _KickIntensity ("Lightning Intensity", Float) = 0
        _MagicBrightness ("Overall Glow", Float) = 1.0
        _ShimmerPos ("Wave Position", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One // Additive for HDR Bloom
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float _KickIntensity, _MagicBrightness, _ShimmerPos;
            float4 _BoltColor, _FaceColor;

            float hash(float n) { return frac(sin(n) * 43758.5453); }
            float noise(float2 x) {
                float2 p = floor(x); float2 f = frac(x);
                f = f*f*(3.0-2.0*f);
                float n = p.x + p.y*57.0;
                return lerp(lerp(hash(n+0.0), hash(n+1.0),f.x), lerp(hash(n+57.0), hash(n+58.0),f.x),f.y);
            }

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float sdf = tex2D(_MainTex, i.uv).a;
                float textMask = smoothstep(0.45, 0.5, sdf);

                // --- ENERGY WAVE (Shimmer) ---
                float wave = 1.0 - smoothstep(0.0, 0.12, abs(i.uv.x - _ShimmerPos));
                float3 waveGlow = _BoltColor.rgb * wave * textMask * 3.0 * _MagicBrightness;

                // --- BASE TEXT ---
                fixed4 col = _FaceColor * textMask * _MagicBrightness;

                col.rgb += waveGlow;
                col.a = textMask;

                return col;
            }
            ENDHLSL
        }
    }
}