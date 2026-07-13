// Shared instanced material for ground-scatter decoration (grass blades, flower stems/blooms,
// pebbles). Replaces per-Transform wind sway (HexMapWindSway) with a vertex-shader bend driven by
// world position + time, so thousands of instances animate without any per-object Update() cost.
// Per-instance sway (amplitude/speed/phase) is carried via GPU instancing; a zero amplitude renders
// as a static (non-swaying) instance, which is how flowers/pebbles share this same shader.
Shader "Hex/HexNatureInstanced"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _WindAmplitudeScale ("Wind Amplitude Scale", Float) = 0.012
        _WindDirection ("Wind Direction", Vector) = (0.7, 0.3, 0, 0)
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
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Smoothness;
                float _WindAmplitudeScale;
                float4 _WindDirection;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(PerInstanceNature)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SwayParams) // x=amplitude(deg-ish), y=speed, z=phase
            UNITY_INSTANCING_BUFFER_END(PerInstanceNature)

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float4 sway = UNITY_ACCESS_INSTANCED_PROP(PerInstanceNature, _SwayParams);

                VertexPositionInputs positions = GetVertexPositionInputs(IN.positionOS.xyz);
                float3 positionWS = positions.positionWS;

                // Height-based falloff: object-space Y (0 at the planted base, up toward the tip)
                // so bases stay put and tips move, the same read as the old whole-root rotation.
                float falloff = saturate(IN.positionOS.y);
                float2 windDir = normalize(_WindDirection.xy + 1e-5);
                float bend = sin(_Time.y * sway.y + sway.z + dot(positionWS.xz, windDir) * 0.3) * sway.x * _WindAmplitudeScale * falloff;
                positionWS.xz += windDir * bend;

                VertexNormalInputs normals = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = normals.normalWS;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                half3 ambient = SampleSH(normalWS);
                half ndotl = saturate(dot(normalWS, mainLight.direction)) * 0.5h + 0.5h;
                half3 lighting = ambient + mainLight.color * ndotl * mainLight.shadowAttenuation;
                return half4(_BaseColor.rgb * lighting, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
