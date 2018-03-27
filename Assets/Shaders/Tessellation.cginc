#if !defined(TESSELLATION_INCLUDED)
#define TESSELLATION_INCLUDED

#include "BaseStructs.cginc"
#include "TessellationStructs.cginc"
#include "SimplexNoise.cginc"
#include "UnityCG.cginc"

#define MUTEX(val1, val2, condition) ((val1) * (condition)) + ((val2) * (1 - (condition)))

float4 _MainTex_ST;

float _TessellationEdgeLength;
float _TessellationEnableDistance;
float _SimplexNoiseFrequency;
float _SimplexNoiseAmplitude;

TessellationControlPoint TessellationVertex (VertexData v) {
	TessellationControlPoint t;
	t.vertex = v.vertex;
	t.normal = v.normal;
	t.tangent = v.tangent;
	t.uv = v.uv;
	t.uv1 = v.uv1;
	t.uv2 = v.uv2;
	return t;
}

[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("fractional_odd")]
[UNITY_patchconstantfunc("PatchConstantFunction")]
TessellationControlPoint TessellationHull (InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID) {
	return patch[id];
}

float TessellationEdgeFactor(TessellationControlPoint cp0, TessellationControlPoint cp1) {
	float3 p0 = mul(unity_ObjectToWorld, float4(cp0.vertex.xyz, 1)).xyz;
	float3 p1 = mul(unity_ObjectToWorld, float4(cp1.vertex.xyz, 1)).xyz;
	float edgeLength = distance(p0, p1);

	float3 edgeCenter = (p0 + p1) * 0.5;
	float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);
	float edgeFactor = edgeLength / (_TessellationEdgeLength * viewDistance);
	//edgeFactor = MUTEX(edgeFactor, 1, edgeFactor > 2); // Use to remove small stupid stretched tessellations

	return MUTEX(edgeFactor, 1, viewDistance < _TessellationEnableDistance);
}

TessellationFactors PatchConstantFunction (InputPatch<TessellationControlPoint, 3> patch) {
	TessellationFactors f;
    f.edge[0] = TessellationEdgeFactor(patch[1], patch[2]);
    f.edge[1] = TessellationEdgeFactor(patch[2], patch[0]);
    f.edge[2] = TessellationEdgeFactor(patch[0], patch[1]);
	f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) * (1 / 3.0);
	return f;
}

FragmentData SimplexDisplacement (VertexData v, int isTessellating) {
	FragmentData o;

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
	float displacement = noise3D(worldPos.x * _SimplexNoiseFrequency, worldPos.y * _SimplexNoiseFrequency, worldPos.z * _SimplexNoiseFrequency);
	float sign = ((displacement > 0) - (displacement < 0));
	displacement = displacement * displacement * sign;
	displacement *= isTessellating; // Disable displacement if is not tessellating
	//displacement = ((displacement > 0) - (displacement < 0)) - displacement; // Creates cool canyons
	float3 normal = normalize(v.normal);
	v.vertex.xyz += normal * displacement * _SimplexNoiseAmplitude;

	o.position = UnityObjectToClipPos(v.vertex);
	o.worldPosition.xyz = worldPos;	
	o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
	o.normal = normal;	

	return o;
}

[UNITY_domain("tri")]
FragmentData TessellationDomain (TessellationFactors factors,
							   OutputPatch<TessellationControlPoint, 3> patch,
							   float3 barycentricCoordinates : SV_DomainLocation) {
	VertexData data;

	#define DOMAIN_INTERPOLATE(fieldName) data.fieldName = \
		patch[0].fieldName * barycentricCoordinates.x + \
		patch[1].fieldName * barycentricCoordinates.y + \
		patch[2].fieldName * barycentricCoordinates.z;

	DOMAIN_INTERPOLATE(vertex)
	DOMAIN_INTERPOLATE(normal)
	DOMAIN_INTERPOLATE(tangent)
	DOMAIN_INTERPOLATE(uv)
	DOMAIN_INTERPOLATE(uv1)
	DOMAIN_INTERPOLATE(uv2)
	
	int isTessellating = (factors.edge[0] > 1) || (factors.edge[1] > 1) || (factors.edge[2] > 1);
	return SimplexDisplacement(data, isTessellating);
}

#endif