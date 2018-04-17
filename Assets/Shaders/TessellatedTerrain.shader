﻿Shader "Custom/TessellatedTerrain" {

	Properties {
		_TessellationEdgeLength ("Tessellation Edge Length", Range(0.001, 10)) = 0.5
		_TessellationEnableDistance ("Tessellation Enable Distance", Range(1, 10000)) = 50

		_TessellatedShadowsEdgeLength("Tessellated Shadows Edge Length", Range(0.001, 10)) = 0.5
		_TessellatedShadowsEnableDistance("Tessellated Shadows Edge Distance", Range(1, 10000)) = 50

		_SimplexNoiseFrequency ("Simplex Noise Frequency", Float) = 1
		_SimplexNoiseAmplitude ("Simplex Noise Amplitude", Float) = 1

		//_MainTex ("Main Texture", 2D) = "white" {}

		_PlainTexture ("Plain Texture", 2D) = "white" {}
		[NoScaleOffset] _PlainNormalMap("Plain Normals", 2D) = "bump" {}
		_PlainDetailTexture ("Plain Detail", 2D) = "white" {}
		[NoScaleOffset] _PlainDetailNormalMap("Plain Detail Normals", 2D) = "bump" {}
		_SlopeTexture ("Slope Texture", 2D) = "white" {}
		[NoScaleOffset] _SlopeNormalMap("Slope Normals", 2D) = "bump" {}
		_SlopeDetailTexture ("Slope Texture", 2D) = "white" {}
		[NoScaleOffset] _SlopeDetailNormalMap("Slope Detail Normals", 2D) = "bump" {}
		_NormalBumpScale ("Normals Bump", Float) = 1
		
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

			#pragma multi_compile_fog
			#pragma multi_compile _ SHADOWS_SCREEN

			#define TESSELLATION_VIEW_DISTANCE_BASED
			#define SHADOW_RECEIVE_PASS
			
			#include "Tessellation.cginc"
			#include "TerrainRendering.cginc"

			ENDCG
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
		}

			CGPROGRAM

			#pragma target 4.6

			#pragma vertex TessellationVertex
			#pragma hull TessellationHull
			#pragma domain TessellationDomain
			#pragma fragment ShadowCasterFragment

			#define SHADOW_CAST_PASS

			#define _TessellationEdgeLength _TessellatedShadowsEdgeLength
			#define _TessellationEnableDistance _TessellatedShadowsEnableDistance

			#include "Shadows.cginc"
			#include "Tessellation.cginc"

			ENDCG
		}
	}
}