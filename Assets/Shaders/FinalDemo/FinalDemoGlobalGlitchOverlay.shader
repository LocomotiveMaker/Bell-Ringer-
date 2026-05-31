Shader "Hidden/BellRinger/FinalDemo/GlobalGlitchOverlay"
{
    Properties
    {
        _Intensity("Intensity", Range(0,1)) = 0
        _RegularColor("Regular Color", Color) = (0.74,0.08,1,1)
        _BossColor("Boss Color", Color) = (1,0.12,0.66,1)
        _BossMode("Boss Mode", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Overlay"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float4 _RegularColor;
                float4 _BossColor;
                float _BossMode;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 123.78));
                p += dot(p, p + 45.45);
                return frac(p.x * p.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                float scan = sin((uv.y + time * 0.42) * 420.0) * 0.5 + 0.5;
                float scanMask = saturate(scan * 0.65 + 0.15) * _Intensity;

                float2 coarse = floor(uv * float2(18.0, 12.0) + float2(time * 3.0, time * 2.0));
                float blockNoise = Hash21(coarse);
                float blockMask = step(0.87, blockNoise) * _Intensity;

                float edgeVignette = smoothstep(0.0, 0.18, uv.x) * smoothstep(0.0, 0.18, 1.0 - uv.x);
                edgeVignette *= smoothstep(0.0, 0.08, uv.y) * smoothstep(0.0, 0.08, 1.0 - uv.y);

                float bar = smoothstep(0.0, 0.015, abs(frac(time * 0.28 + uv.y * 1.9) - 0.5));
                float pulse = saturate(scanMask * 0.35 + blockMask * 0.85 + (1.0 - bar) * _Intensity * 0.12);

                half3 color = lerp(_RegularColor.rgb, _BossColor.rgb, _BossMode);
                half alpha = saturate((pulse + scanMask * 0.18) * edgeVignette);
                return half4(color, alpha * 0.55);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
