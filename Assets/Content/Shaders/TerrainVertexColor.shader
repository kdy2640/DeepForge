Shader "Deep Forge/Terrain Vertex Color"
{
    Properties
    {
        _BaseMap ("Rock Color", 2D) = "white" {}
        [Normal] _BumpMap ("Rock Normal", 2D) = "bump" {}
        _TileSize ("Tile Size (World Units)", Float) = 4
        _TextureStrength ("Texture Strength", Range(0, 1)) = 0.6
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
            float _TileSize;
            half _TextureStrength;
            half _NormalStrength;
            half _Smoothness;
        CBUFFER_END

        half3 SampleTriplanarAlbedo(float3 positionWS, float3 normalWS)
        {
            float3 uv = GetAbsolutePositionWS(positionWS) / max(_TileSize, 0.01);
            float3 weights = normalWS * normalWS;
            weights *= weights;
            weights /= weights.x + weights.y + weights.z;
            half3 x = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.zy).rgb;
            half3 y = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xz).rgb;
            half3 z = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy).rgb;
            return x * weights.x + y * weights.y + z * weights.z;
        }

        float3 SampleTriplanarNormal(float3 positionWS, float3 normalWS)
        {
            float3 uv = GetAbsolutePositionWS(positionWS) / max(_TileSize, 0.01);
            float3 weights = normalWS * normalWS;
            weights *= weights;
            weights /= weights.x + weights.y + weights.z;
            float3 x = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.zy), _NormalStrength);
            float3 y = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.xz), _NormalStrength);
            float3 z = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.xy), _NormalStrength);
            // Reorient each projection onto the surface, including inward-facing cave walls.
            // A flat normal map (or zero strength) preserves the original mesh normal.
            x = float3(x.xy + normalWS.zy, abs(x.z) * normalWS.x);
            y = float3(y.xy + normalWS.xz, abs(y.z) * normalWS.y);
            z = float3(z.xy + normalWS.xy, abs(z.z) * normalWS.z);
            return normalize(x.zyx * weights.x + y.xzy * weights.y + z.xyz * weights.z);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TerrainVertex
            #pragma fragment TerrainFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                half4 fogAndVertexLight : TEXCOORD2;
            };

            Varyings TerrainVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.fogAndVertexLight = half4(ComputeFogFactor(position.positionCS.z),
                    VertexLighting(position.positionWS, output.normalWS));
                return output;
            }

            half4 TerrainFragment(Varyings input) : SV_Target
            {
                float3 surfaceNormalWS = NormalizeNormalPerPixel(input.normalWS);
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = SampleTriplanarNormal(input.positionWS, surfaceNormalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    lighting.shadowCoord = float4(GetNormalizedScreenSpaceUV(input.positionCS), 0, 1);
                #else
                    lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                lighting.fogCoord = input.fogAndVertexLight.x;
                lighting.vertexLighting = input.fogAndVertexLight.yzw;
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = input.color.rgb * lerp(half3(1, 1, 1),
                    SampleTriplanarAlbedo(input.positionWS, surfaceNormalWS), _TextureStrength);
                surface.smoothness = _Smoothness;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                surface.alpha = 1;
                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, lighting.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            float4 DepthVertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }
            half DepthFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NormalVertex
            #pragma fragment NormalFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct NormalVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };
            NormalVaryings NormalVertex(float4 positionOS : POSITION, float3 normalOS : NORMAL)
            {
                NormalVaryings output;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.positionWS = TransformObjectToWorld(positionOS.xyz);
                return output;
            }
            half4 NormalFragment(NormalVaryings input) : SV_Target
            {
                float3 normal = SampleTriplanarNormal(input.positionWS, NormalizeNormalPerPixel(input.normalWS));
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormal = PackNormalOctQuadEncode(normal);
                    return half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0);
                #else
                    return half4(normal, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
