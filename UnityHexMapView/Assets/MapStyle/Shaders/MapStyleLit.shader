// Flat-shaded, vertex-colored lit shader for the low-poly map style prototype (V0).
// Knowledge is expressed through _Pale: 0 = confirmed (full color), 1 = reported (desaturated, paper tint).
// Emission is additive vertex color so fires, windows and lanterns stay readable at night.
Shader "MapStyle/LowPolyLit"
{
    Properties
    {
        _Pale ("Pale (knowledge)", Range(0, 1)) = 0
        _PaperColor ("Paper color", Color) = (0.85, 0.83, 0.76, 1)
        _Emissive ("Emissive", Range(0, 2)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Pale;
                float4 _PaperColor;
                float _Emissive;
            CBUFFER_END

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = positions.positionCS;
                OUT.positionWS = positions.positionWS;
                OUT.normalWS = normals.normalWS;
                OUT.color = IN.color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half3 albedo = IN.color.rgb;
                half luminance = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                half3 pale = lerp(half3(luminance, luminance, luminance), _PaperColor.rgb, 0.55h);
                albedo = lerp(albedo, pale, _Pale);

                float3 normalWS = normalize(IN.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 lighting = SampleSH(normalWS) + mainLight.color * saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);
                    lighting += light.color * (light.distanceAttenuation * saturate(dot(normalWS, light.direction)));
                }
                #endif

                half3 color = albedo * lighting + albedo * _Emissive;
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
    }

    FallBack "Universal Render Pipeline/Lit"
}
