Shader "Custom/TessellatedTerrain" {

	Properties {
		_TessellationEdgeLength ("Tessellation Edge Length", Range(0.01, 10)) = 0.5
		_TessellationEnableDistance ("Tessellation Enable Distance", Range(1, 1000)) = 50

		_SimplexNoiseFrequency ("Simplex Noise Frequency", Float) = 1
		_SimplexNoiseAmplitude ("Simplex Noise Amplitude", Float) = 1

		//_MainTex ("Main Texture", 2D) = "white" {}

		_PlainTexture ("Plain Texture", 2D) = "white" {}
		_PlainDetailTexture ("Plain Detail", 2D) = "white" {}
		_SlopeTexture ("Slope Texture", 2D) = "white" {}
		_SlopeDetailTexture ("Slope Texture", 2D) = "white" {}
		
		_SlopeAngleTreshold ("Slope Angle Treshold", Float) = 45
		_MountainTransitionStartAltitude ("Mountain Transition Start Altitude", Float) = 10
		_MountainTransitionEndAltitude ("Mountain Transition End Altitude", Float) = 20
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
			#pragma fragment TerrainFragment
			
			#include "Tessellation.cginc"
			#include "TerrainRendering.cginc"

			ENDCG
		}
	}
}