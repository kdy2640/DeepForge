Shader "Deep Forge/UI/Stone Background Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Stone Texture", 2D) = "white" {}
        _BlurRadius ("Blur Radius (Texture Pixels)", Range(0, 64)) = 32
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurRadius;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                const float weights[5] = { 1, 4, 6, 4, 1 };
                float2 stepUV = _MainTex_TexelSize.xy * (_BlurRadius * 0.5);
                float mipLevel = log2(max(_BlurRadius * 0.5, 1.0));
                half4 blurred = 0;
                [unroll]
                for (int y = 0; y < 5; y++)
                {
                    [unroll]
                    for (int x = 0; x < 5; x++)
                    {
                        float2 offset = float2(x - 2, y - 2) * stepUV;
                        blurred += tex2Dlod(_MainTex, float4(input.uv + offset, 0, mipLevel)) * (weights[x] * weights[y]);
                    }
                }
                return (blurred / 256.0) * input.color;
            }
            ENDCG
        }
    }
}
