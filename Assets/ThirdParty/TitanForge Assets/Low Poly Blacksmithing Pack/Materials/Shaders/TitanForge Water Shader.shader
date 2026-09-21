// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TitanForge/TitanForge Simple Water Shader"
{
	Properties
	{
		_AlbedoColor("Albedo Color", Color) = (0.3803922,0.7882354,0.9058824,1)
		_Transparency("Transparency", Range( 0 , 1)) = 0.5
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_EmissionColor("Emission Color", Color) = (0.3803922,0.7882354,0.9058824,1)
		_Emission("Emission", Range( 0 , 1)) = 0.25
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float3 worldPos;
		};

		uniform float4 _AlbedoColor;
		uniform float _Emission;
		uniform float4 _EmissionColor;
		uniform float _Smoothness;
		uniform float _Transparency;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertexNormal = v.normal.xyz;
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = UnityObjectToWorldNormal( v.normal );
			float dotResult438 = dot( ase_worldViewDir , ase_worldNormal );
			float3 lerpResult442 = lerp( ase_vertexNormal , -ase_vertexNormal , step( dotResult438 , 0.0 ));
			v.normal = lerpResult442;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Albedo = _AlbedoColor.rgb;
			o.Emission = ( _Emission * _EmissionColor ).rgb;
			o.Smoothness = _Smoothness;
			o.Alpha = _Transparency;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard alpha:fade keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float3 worldPos : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18500
36.8;138.4;1921;980;1647.504;935.3837;1.76764;True;False
Node;AmplifyShaderEditor.CommentaryNode;434;-494.781,-75.47816;Inherit;False;784.5191;427.8283;Two Sided Normals;8;442;441;440;439;438;437;436;435;;0.3957005,0.6320754,0.2832413,1;0;0
Node;AmplifyShaderEditor.WorldNormalVector;436;-444.7811,161.1644;Inherit;False;False;1;0;FLOAT3;0,0,1;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;435;-436.1004,-0.2582288;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;443;-386.8454,-667.7855;Inherit;False;668.4044;435.435;Smoothness & Emission;4;451;452;447;444;;0.510413,0.5607195,0.6981132,1;0;0
Node;AmplifyShaderEditor.DotProductOpNode;438;-182.9044,138.3089;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;439;-237.4823,237.3513;Inherit;False;Constant;_Float0;Float 0;15;0;Create;True;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalVertexDataNode;437;-260.3213,-25.47841;Inherit;False;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;447;-297.3961,-510.1214;Inherit;False;Property;_EmissionColor;Emission Color;3;0;Create;True;0;0;False;0;False;0.3803922,0.7882354,0.9058824,1;0,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;461;-83.32756,-223.2876;Inherit;False;366.9048;147.1541;Glass Transparency;1;455;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;298;-25.07711,-925.2857;Inherit;False;279.0974;254.0123;Main Shader;1;4;;0.2063012,0.7169812,0.6481563,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;444;-318.8075,-589.4649;Inherit;False;Property;_Emission;Emission;4;0;Create;True;0;0;False;0;False;0.25;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;440;-43.2464,53.93761;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StepOpNode;441;-48.86981,137.7865;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;451;-335.5988,-320.5017;Inherit;False;Property;_Smoothness;Smoothness;2;0;Create;True;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;4;0.02400517,-868.8964;Inherit;False;Property;_AlbedoColor;Albedo Color;0;0;Create;True;0;0;False;0;False;0.3803922,0.7882354,0.9058824,1;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;452;122.775,-536.0743;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;455;-20.32321,-173.2876;Inherit;False;Property;_Transparency;Transparency;1;0;Create;True;0;0;False;0;False;0.5;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;442;105.737,-23.48308;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;460;455.2245,-487.1883;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;TitanForge/TitanForge Simple Water Shader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.1;True;True;0;False;Transparent;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;438;0;435;0
WireConnection;438;1;436;0
WireConnection;440;0;437;0
WireConnection;441;0;438;0
WireConnection;441;1;439;0
WireConnection;452;0;444;0
WireConnection;452;1;447;0
WireConnection;442;0;437;0
WireConnection;442;1;440;0
WireConnection;442;2;441;0
WireConnection;460;0;4;0
WireConnection;460;2;452;0
WireConnection;460;4;451;0
WireConnection;460;9;455;0
WireConnection;460;12;442;0
ASEEND*/
//CHKSM=A3888EFB560E18078B1ED534E009F443399BB136