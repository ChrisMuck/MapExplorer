// One shared material for the entire chunked terrain layer. Per-tile color variety comes from
// baked vertex color (see HexMapChunkGeometryBuilder), not from material variants, so every chunk
// draws in a single draw call regardless of how many terrain kinds it contains.
Shader "Hex/HexTerrain"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _NoiseTiling ("Noise Tiling", Float) = 0.35
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.18
        _Smoothness ("Smoothness", Range(0,1)) = 0.15
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float _NoiseTiling;
                float _NoiseStrength;
                float _Smoothness;
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
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 noiseUv = IN.uv * _NoiseTiling;
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv).r;
                half3 albedo = IN.color.rgb * lerp(1.0h, noise * 2.0h, _NoiseStrength);

                float3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                half3 ambient = SampleSH(normalWS);
                half ndotl = saturate(dot(normalWS, mainLight.direction)) * 0.5h + 0.5h;
                half3 lighting = ambient + mainLight.color * ndotl * mainLight.shadowAttenuation;

                half3 color = albedo * lighting;
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
