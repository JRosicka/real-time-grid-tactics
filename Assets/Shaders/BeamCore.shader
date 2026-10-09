Shader "Custom/Beam Core"
{
    Properties
    {
        [MainTexture] _BaseMap("Beam Texture", 2D) = "white" {}

        [MainColor][HDR]
        _BaseColor("Beam Color", Color) = (1, 1, 1, 1)

        _AlphaStrength("Alpha Strength", Range(0, 5)) = 2

        _AlphaPower("Alpha Power", Range(0.1, 5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "BeamCore"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)

                float4 _BaseMap_ST;
                float4 _BaseColor;

                float _AlphaStrength;
                float _AlphaPower;

            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv =
                    TRANSFORM_TEX(input.uv, _BaseMap);

                output.color = input.color;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv);

                // Our texture is grayscale, so luminance becomes
                // the opacity mask. This completely ignores whatever
                // alpha channel the generated PNG happened to contain.
                half luminance = dot(
                    textureSample.rgb,
                    half3(0.299, 0.587, 0.114));

                half alpha =
                    pow(
                        saturate(luminance),
                        _AlphaPower);

                alpha *=
                    _AlphaStrength *
                    _BaseColor.a *
                    input.color.a;

                alpha = saturate(alpha);

                half3 color =
                    textureSample.rgb *
                    _BaseColor.rgb *
                    input.color.rgb;

                return half4(color, alpha);
            }

            ENDHLSL
        }
    }
}