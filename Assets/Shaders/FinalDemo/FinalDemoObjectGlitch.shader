Shader "Hidden/BellRinger/FinalDemo/ObjectGlitch"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _GlitchColor("Glitch Color", Color) = (0.74,0.08,1,1)
        _Intensity("Intensity", Range(0,1)) = 0
        _SurfaceAlpha("Surface Alpha", Range(0,1)) = 1
        _BlockDensity("Block Density", Range(2,64)) = 18
        _ScanlineDensity("Scanline Density", Range(10,400)) = 120
        _ChannelSplit("Channel Split", Range(0,0.05)) = 0.008
        _Rim("Rim", Range(0,1)) = 0.28
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _GlitchColor;
                float _Intensity;
                float _SurfaceAlpha;
                float _BlockDensity;
                float _ScanlineDensity;
                float _ChannelSplit;
                float _Rim;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float timeSlice = floor(_Time.y * lerp(8.0, 28.0, _Intensity));
                float line = floor(input.positionOS.y * max(1.0, _ScanlineDensity * 0.08));
                float lateralJitter = (Hash21(float2(line, timeSlice)) - 0.5) * _Intensity * 0.08;
                float depthJitter = (Hash21(float2(line + 7.0, timeSlice + 5.0)) - 0.5) * _Intensity * 0.05;

                float3 positionOS = input.positionOS.xyz;
                positionOS.x += lateralJitter;
                positionOS.z += depthJitter;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float timeSlice = floor(_Time.y * 7.0);
                float2 blockUV = floor(uv * max(2.0, _BlockDensity));
                float blockNoise = Hash21(blockUV + timeSlice);
                float tear = step(0.82, blockNoise) * _Intensity;
                float scan = sin((uv.y + _Time.y * 0.45) * _ScanlineDensity) * 0.5 + 0.5;
                float split = _ChannelSplit * (0.35 + tear * 1.7 + scan * 0.25);
                float2 shift = float2(split * ((blockNoise - 0.5) * 2.0), 0.0);

                half4 sampleR = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + shift);
                half4 sampleG = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half4 sampleB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv - shift);

                half3 baseRgb = half3(sampleR.r, sampleG.g, sampleB.b) * _BaseColor.rgb;
                float alphaSource = max(sampleR.a, max(sampleG.a, sampleB.a));

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 2.5);
                float band = saturate(scan * 0.55 + tear * 0.95);
                half3 glitchRgb = _GlitchColor.rgb * (_Intensity * band + rim * _Rim * _Intensity);

                half alpha = saturate(alphaSource * _BaseColor.a * _SurfaceAlpha + tear * 0.18 + rim * 0.06 * _Intensity);
                return half4(saturate(baseRgb + glitchRgb), alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
