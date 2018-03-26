#if !defined(TESSELLATION_INCLUDED)
#define TESSELLATION_INCLUDED

#include "My Lighting Input.cginc"
#include "My Lighting.cginc"
#include "SimplexNoise.cginc"

float _TessellationEdgeLength;
float _TessellationEnableDistance;
float _SimplexNoiseFrequency;
float _SimplexNoiseAmplitude;

struct TessellationFactors {
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

struct TessellationControlPoint {
	float4 vertex : INTERNALTESSPOS;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
};

InterpolatorsVertex MyVertexProgramSimplex (VertexData v, int isTessellating) {
	InterpolatorsVertex i;
	UNITY_INITIALIZE_OUTPUT(InterpolatorsVertex, i);
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, i);

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
	float displacement = noise3D(worldPos.x * _SimplexNoiseFrequency, worldPos.y * _SimplexNoiseFrequency, worldPos.z * _SimplexNoiseFrequency);
	float sign = ((displacement > 0) - (displacement < 0));
	displacement = displacement * displacement * sign;
	displacement *= isTessellating; // Disable displacement if is not tessellating
	//displacement = ((displacement > 0) - (displacement < 0)) - displacement; // Creates cool canyons
	float3 normal = normalize(v.normal);
	//float3 normal; normal.x = 0; normal.y = 1; normal.z = 0;
	v.vertex.xyz += normal * displacement * _SimplexNoiseAmplitude;

	i.pos = UnityObjectToClipPos(v.vertex);
	i.worldPos.xyz = mul(unity_ObjectToWorld, v.vertex);
	#if FOG_DEPTH
		i.worldPos.w = i.pos.z;
	#endif
	i.normal = UnityObjectToWorldNormal(v.normal);

	#if defined(BINORMAL_PER_FRAGMENT)
		i.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
	#else
		i.tangent = UnityObjectToWorldDir(v.tangent.xyz);
		i.binormal = CreateBinormal(i.normal, i.tangent, v.tangent.w);
	#endif

	i.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
	i.uv.zw = TRANSFORM_TEX(v.uv, _DetailTex);

	#if defined(LIGHTMAP_ON) || ADDITIONAL_MASKED_DIRECTIONAL_SHADOWS
		i.lightmapUV = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
	#endif

	#if defined(DYNAMICLIGHTMAP_ON)
		i.dynamicLightmapUV =
			v.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif

	UNITY_TRANSFER_SHADOW(i, v.uv1);

	ComputeVertexLightColor(i);

	#if defined (_PARALLAX_MAP)
		#if defined(PARALLAX_SUPPORT_SCALED_DYNAMIC_BATCHING)
			v.tangent.xyz = normalize(v.tangent.xyz);
			v.normal = normalize(v.normal);
		#endif
		float3x3 objectToTangent = float3x3(
			v.tangent.xyz,
			cross(v.normal, v.tangent.xyz) * v.tangent.w,
			v.normal
		);
		i.tangentViewDir = mul(objectToTangent, ObjSpaceViewDir(v.vertex));
	#endif

	return i;
}

[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("fractional_odd")]
[UNITY_patchconstantfunc("MyPatchConstantFunction")]
TessellationControlPoint MyHullProgram(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID) {
	return patch[id];
}

float TessellationEdgeFactor(TessellationControlPoint cp0, TessellationControlPoint cp1) {
	float3 p0 = mul(unity_ObjectToWorld, float4(cp0.vertex.xyz, 1)).xyz;
	float3 p1 = mul(unity_ObjectToWorld, float4(cp1.vertex.xyz, 1)).xyz;
	float edgeLength = distance(p0, p1);

	float3 edgeCenter = (p0 + p1) * 0.5;
	float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);

	return (edgeLength / _TessellationEdgeLength) * (viewDistance < _TessellationEnableDistance) + (viewDistance >= _TessellationEnableDistance);
}

TessellationFactors MyPatchConstantFunction (InputPatch<TessellationControlPoint, 3> patch) {
	TessellationFactors f;
    f.edge[0] = TessellationEdgeFactor(patch[1], patch[2]);
    f.edge[1] = TessellationEdgeFactor(patch[2], patch[0]);
    f.edge[2] = TessellationEdgeFactor(patch[0], patch[1]);
	f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) * (1 / 3.0);
	return f;
}

[UNITY_domain("tri")]
InterpolatorsVertex MyDomainProgram (TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DomainLocation) {
	VertexData data;

	#define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) data.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
	MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
	MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv1)
	MY_DOMAIN_PROGRAM_INTERPOLATE(uv2)
	
	int isTessellating = (factors.edge[0] > 1) || (factors.edge[1] > 1) || (factors.edge[2] > 1);
	return MyVertexProgramSimplex(data, isTessellating);
}

TessellationControlPoint MyTessellationVertexProgram (VertexData v) {
	TessellationControlPoint p;
	p.vertex = v.vertex;
	p.normal = v.normal;
	p.tangent = v.tangent;
	p.uv = v.uv;
	p.uv1 = v.uv1;
	p.uv2 = v.uv2;
	return p;
}

#endif