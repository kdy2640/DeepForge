// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TitanForge/TitanForge Blacksmith Shader"
{
	Properties
	{
		_AlbedoColor("Albedo Color", Color) = (1,1,1,0)
		_TextureAlbedo("Texture Albedo", 2D) = "white" {}
		_Color1("Color 1", Color) = (0.6235294,0.6078432,0.5686275,1)
		_Color2("Color 2", Color) = (0.3529412,0.3686275,0.4,1)
		_Color3("Color 3", Color) = (0.4196079,0.3960785,0.345098,1)
		_Color4("Color 4", Color) = (0.1372549,0.3647059,0.6784314,1)
		_Color5("Color 5", Color) = (0.4078432,0.3294118,0.172549,1)
		_Color6("Color 6", Color) = (0.6117647,0.509804,0.3882353,1)
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_MetalSmoothness("Metal Smoothness", Range( 0 , 1)) = 0
		_ColorMask("Color Mask", 2D) = "white" {}
		_GemstoneSmoothness("Gemstone Smoothness", Range( 0 , 1)) = 0
		_MetallicMask("Metallic Mask", 2D) = "white" {}
		[HDR]_EmissionColor("Emission Color", Color) = (0,0,0,0)
		_Emission("Emission", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _ColorMask;
		uniform float4 _ColorMask_ST;
		uniform float4 _AlbedoColor;
		uniform sampler2D _TextureAlbedo;
		uniform float4 _TextureAlbedo_ST;
		uniform float4 _Color1;
		uniform float4 _Color2;
		uniform float4 _Color3;
		uniform float4 _Color6;
		uniform float4 _Color5;
		uniform float4 _Color4;
		SamplerState sampler_TextureAlbedo;
		uniform sampler2D _MetallicMask;
		SamplerState sampler_MetallicMask;
		uniform float4 _MetallicMask_ST;
		uniform float _Emission;
		uniform float4 _EmissionColor;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float _MetalSmoothness;
		uniform float _GemstoneSmoothness;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 appendResult41 = (float2(( ( 1.0 - i.uv_texcoord.x ) - 0.5 ) , i.uv_texcoord.y));
			float4 tex2DNode35 = tex2D( _ColorMask, appendResult41 );
			float3 appendResult66 = (float3(tex2DNode35.r , tex2DNode35.g , tex2DNode35.b));
			float2 uv_ColorMask = i.uv_texcoord * _ColorMask_ST.xy + _ColorMask_ST.zw;
			float4 tex2DNode23 = tex2D( _ColorMask, uv_ColorMask );
			float3 appendResult63 = (float3(tex2DNode23.r , tex2DNode23.g , tex2DNode23.b));
			float2 uv_TextureAlbedo = i.uv_texcoord * _TextureAlbedo_ST.xy + _TextureAlbedo_ST.zw;
			float4 tex2DNode10 = tex2D( _TextureAlbedo, uv_TextureAlbedo );
			float3 layeredBlendVar64 = appendResult63;
			float4 layeredBlend64 = ( lerp( lerp( lerp( ( _AlbedoColor * tex2DNode10 ) , _Color1 , layeredBlendVar64.x ) , _Color2 , layeredBlendVar64.y ) , _Color3 , layeredBlendVar64.z ) );
			float3 layeredBlendVar46 = appendResult66;
			float4 layeredBlend46 = ( lerp( lerp( lerp( layeredBlend64 , _Color6 , layeredBlendVar46.x ) , _Color5 , layeredBlendVar46.y ) , _Color4 , layeredBlendVar46.z ) );
			float4 lerpResult70 = lerp( layeredBlend46 , ( 1.5 * layeredBlend46 ) , tex2DNode10.a);
			float4 lerpResult73 = lerp( lerpResult70 , ( 0.66 * lerpResult70 ) , tex2DNode35.a);
			o.Albedo = lerpResult73.rgb;
			float2 uv_MetallicMask = i.uv_texcoord * _MetallicMask_ST.xy + _MetallicMask_ST.zw;
			float4 tex2DNode445 = tex2D( _MetallicMask, uv_MetallicMask );
			o.Emission = ( ( tex2DNode445.a * _Emission ) * _EmissionColor ).rgb;
			o.Metallic = ( _Metallic * tex2DNode445.r );
			float4 layeredBlendVar453 = tex2DNode445;
			float layeredBlend453 = ( lerp( lerp( lerp( lerp( _Smoothness , _MetalSmoothness , layeredBlendVar453.x ) , _GemstoneSmoothness , layeredBlendVar453.y ) , _Smoothness , layeredBlendVar453.z ) , 0.0 , layeredBlendVar453.w ) );
			o.Smoothness = layeredBlend453;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18500
0;106.4;1920;983;2834.931;1659.013;2.430309;True;False
Node;AmplifyShaderEditor.CommentaryNode;298;-1249.371,-1369.575;Inherit;False;1679.761;1184.645;Main Shader;33;40;73;421;72;70;67;68;425;46;71;64;47;66;49;48;27;424;17;28;35;433;41;63;423;432;23;5;464;4;21;10;465;29;;0.2063012,0.7169812,0.6481563,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-425.4215,-1304.802;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;40;-162.5389,-1311.165;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;4;-1118.974,-800.4869;Inherit;False;Property;_AlbedoColor;Albedo Color;0;0;Create;True;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;21;-1106.549,-1199.327;Inherit;True;Property;_ColorMask;Color Mask;11;0;Create;True;0;0;False;0;False;f96a52e5205936e4491382f5dbec5885;2ab6b1e255f617245b7e1203668a268d;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;10;-1199.158,-631.5624;Inherit;True;Property;_TextureAlbedo;Texture Albedo;1;0;Create;True;0;0;False;0;False;-1;d672eef6fcab5b241a04c350821dc2bb;bcd74a2ece0b9704aa045c742a8a6e53;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;465;-370.8089,-1177.999;Inherit;False;Constant;_Float0;Float 0;16;0;Create;True;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;464;-155.2089,-1244.798;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;23;-1108.995,-1005.235;Inherit;True;Property;_TextureSample1;Texture Sample 1;5;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-860.225,-697.5192;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;41;-159.9449,-1152.48;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;63;-841.3512,-805.4545;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WireNode;423;-461.106,-1111.069;Inherit;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.WireNode;432;-752.0952,-588.9742;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;35;-239.1653,-1039.858;Inherit;True;Property;_TextureSample0;Texture Sample 0;9;0;Create;True;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;27;-717.3244,-557.1871;Inherit;False;Property;_Color2;Color 2;3;0;Create;True;0;0;False;0;False;0.3529412,0.3686275,0.4,1;0.3529411,0.3686274,0.3999999,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;424;-471.3893,-750.3226;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;17;-714.3007,-726.894;Inherit;False;Property;_Color1;Color 1;2;0;Create;True;0;0;False;0;False;0.6235294,0.6078432,0.5686275,1;0.4705882,0.5607843,0.764706,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;28;-711.2884,-392.4763;Inherit;False;Property;_Color3;Color 3;4;0;Create;True;0;0;False;0;False;0.4196079,0.3960785,0.345098,1;0.4196078,0.3960784,0.3450979,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WireNode;433;-501.0952,-579.9742;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;48;-712.5851,-1106.288;Inherit;False;Property;_Color5;Color 5;6;0;Create;True;0;0;False;0;False;0.4078432,0.3294118,0.172549,1;0.4078431,0.3294117,0.1725489,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;47;-713.2714,-941.153;Inherit;False;Property;_Color4;Color 4;5;0;Create;True;0;0;False;0;False;0.1372549,0.3647059,0.6784314,1;0.3764706,0.3137255,0.7333333,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;49;-715.3576,-1297.257;Inherit;False;Property;_Color6;Color 6;7;0;Create;True;0;0;False;0;False;0.6117647,0.509804,0.3882353,1;0.6117647,0.5098039,0.3882352,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;66;-161.6634,-849.6603;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LayeredBlendNode;64;-410.2173,-593.0673;Inherit;False;6;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LayeredBlendNode;46;-191.9956,-718.4534;Inherit;False;6;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;71;28.89107,-724.7148;Inherit;False;Constant;_LightValue;Light Value;11;0;Create;True;0;0;False;0;False;1.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;443;-660.8664,-161.6656;Inherit;False;972.1091;657.4537;Metallic & Emission;11;454;453;452;451;450;449;448;447;446;445;444;;0.510413,0.5607195,0.6981132,1;0;0
Node;AmplifyShaderEditor.WireNode;425;-691.4229,-211.2041;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;43.09988,-644.4821;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;444;-610.8665,380.3653;Inherit;False;Property;_Emission;Emission;15;0;Create;True;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;445;-597.5342,-37.42329;Inherit;True;Property;_MetallicMask;Metallic Mask;13;0;Create;True;0;0;False;0;False;-1;4e49e37ca919c904cabb418073c19c14;bbd3ea7d050076d4899eeb62955fa14c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;70;41.51922,-538.902;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;67;209.5757,-677.0851;Inherit;False;Constant;_DarkValue;Dark Value;11;0;Create;True;0;0;False;0;False;0.66;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;451;-603.7913,158.2051;Inherit;False;Property;_Smoothness;Smoothness;8;0;Create;True;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;446;-604.7574,305.7882;Inherit;False;Property;_GemstoneSmoothness;Gemstone Smoothness;12;0;Create;True;0;0;False;0;False;0;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;449;-151.8659,191.3654;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;447;-170.7568,288.7882;Inherit;False;Property;_EmissionColor;Emission Color;14;1;[HDR];Create;True;0;0;False;0;False;0,0,0,0;0,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;223.3135,-591.5803;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;448;-602.7574,230.788;Inherit;False;Property;_MetalSmoothness;Metal Smoothness;10;0;Create;True;0;0;False;0;False;0;0.75;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;450;-587.8431,-111.6656;Inherit;False;Property;_Metallic;Metallic;9;0;Create;True;0;0;False;0;False;0;0.8;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;421;159.7728,-766.5733;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;454;-137.7568,-95.21182;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LayeredBlendNode;453;-163.5339,8.576727;Inherit;False;6;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;452;142.243,93.78819;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;73;224.9134,-500.1028;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;455.2245,-487.1883;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;TitanForge/TitanForge Blacksmith Shader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;455;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;40;0;29;1
WireConnection;464;0;40;0
WireConnection;464;1;465;0
WireConnection;23;0;21;0
WireConnection;5;0;4;0
WireConnection;5;1;10;0
WireConnection;41;0;464;0
WireConnection;41;1;29;2
WireConnection;63;0;23;1
WireConnection;63;1;23;2
WireConnection;63;2;23;3
WireConnection;423;0;21;0
WireConnection;432;0;5;0
WireConnection;35;0;423;0
WireConnection;35;1;41;0
WireConnection;424;0;63;0
WireConnection;433;0;432;0
WireConnection;66;0;35;1
WireConnection;66;1;35;2
WireConnection;66;2;35;3
WireConnection;64;0;424;0
WireConnection;64;1;433;0
WireConnection;64;2;17;0
WireConnection;64;3;27;0
WireConnection;64;4;28;0
WireConnection;46;0;66;0
WireConnection;46;1;64;0
WireConnection;46;2;49;0
WireConnection;46;3;48;0
WireConnection;46;4;47;0
WireConnection;425;0;10;4
WireConnection;68;0;71;0
WireConnection;68;1;46;0
WireConnection;70;0;46;0
WireConnection;70;1;68;0
WireConnection;70;2;425;0
WireConnection;449;0;445;4
WireConnection;449;1;444;0
WireConnection;72;0;67;0
WireConnection;72;1;70;0
WireConnection;421;0;35;4
WireConnection;454;0;450;0
WireConnection;454;1;445;1
WireConnection;453;0;445;0
WireConnection;453;1;451;0
WireConnection;453;2;448;0
WireConnection;453;3;446;0
WireConnection;453;4;451;0
WireConnection;452;0;449;0
WireConnection;452;1;447;0
WireConnection;73;0;70;0
WireConnection;73;1;72;0
WireConnection;73;2;421;0
WireConnection;0;0;73;0
WireConnection;0;2;452;0
WireConnection;0;3;454;0
WireConnection;0;4;453;0
ASEEND*/
//CHKSM=91F65CDD2A79DE2B665FFF046E0DFA67B275693B