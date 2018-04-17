#if !defined(BASE_STRUCTS_INCLUDED)
#define BASE_STRUCTS_INCLUDED

#include "AutoLight.cginc"

struct VertexData {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
};

struct FragmentData {
	float4 pos : SV_POSITION;
	float4 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 worldPosition : TEXCOORD2;

	SHADOW_COORDS(3)
};

#endif