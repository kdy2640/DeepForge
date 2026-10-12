Shader "Deep Forge/Mining Anchor"
{
    Properties
    {
        _RingColor ("Ring Color", Color) = (1, 0.8, 0.05, 1)
        _LineWidth ("Line Width (World Units)", Range(0.005, 0.2)) = 0.035
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            Name "SurfaceRing"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex AnchorVertex
            #pragma fragment AnchorFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RingColor;
                float _LineWidth;
            CBUFFER_END

            float4 AnchorVertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 AnchorFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(positionCS);
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if (depth <= 0.0) discard;
                #else
                    if (depth >= 1.0) discard;
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
                #endif

                // The cube only bounds the draw area. Evaluate the visible surface, not the cube.
                float3 surfaceWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 surfaceOS = TransformWorldToObject(surfaceWS);
                // PlayerMiner scales the anchor uniformly to the brush diameter.
                float diameter = length(TransformObjectToWorldDir(float3(1, 0, 0), false));
                float radius = diameter * 0.5;
                float distanceToCenter = length(surfaceOS) * diameter;
                float antialias = max(fwidth(distanceToCenter), 0.0001);
                float ring = smoothstep(radius - _LineWidth - antialias,
                    radius - _LineWidth + antialias, distanceToCenter);
                ring *= 1.0 - smoothstep(radius - antialias, radius, distanceToCenter);
                return half4(_RingColor.rgb, _RingColor.a * ring);
            }
            ENDHLSL
        }
    }
}
