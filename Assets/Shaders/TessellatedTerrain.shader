Shader "Custom/TessellatedTerrain" {

	Properties {
		_TessellationEdgeLength ("Tessellation Edge Length", Range(0.01, 10)) = 0.5
		_TessellationEnableDistance ("Tessellation Enable Distance", Range(1, 1000)) = 50

		_SimplexNoiseFrequency ("Simplex Noise Frequency", Float) = 1
		_SimplexNoiseAmplitude ("Simplex Noise Amplitude", Float) = 1

		_MainTex ("Main Texture", 2D) = "white" {}

		_PlainTexture ("Plain Texture", 2D) = "white" {}
		_SlopeTexture ("Slope Texture", 2D) = "white" {}
	}

	SubShader {

		Pass {
			Tags {
				"LightMode" = "ForwardBase"
			}

			CGPROGRAM

			#pragma target 4.6

			#pragma vertex TessellationVertex
			#pragma hull TessellationHull
			#pragma domain TessellationDomain
			#pragma geometry GeometryCalculateNormals
			#pragma fragment TerrainFragment
			
			#include "Tessellation.cginc"
			#include "GeometryNormals.cginc"
			#include "TerrainRendering.cginc"

			ENDCG
		}
	}
}