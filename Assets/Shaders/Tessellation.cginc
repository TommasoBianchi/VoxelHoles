#if !defined(TESSELLATION_INCLUDED)
#define TESSELLATION_INCLUDED

#include "BaseStructs.cginc"
#include "TessellationStructs.cginc"
#include "SimplexNoise.cginc"
#include "UnityCG.cginc"

#define MUX(val1, val2, condition) ((val1) * (condition)) + ((val2) * (1 - (condition)))

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
	#ifdef TESSELLATION_VIEW_DISTANCE_BASED
		// The math is useful to keep factors constant when the player is close enough
		float viewDistance = max(50, distance(edgeCenter, _WorldSpaceCameraPos)) - 49;
	#else
		float viewDistance = 1;
	#endif

	//float maxFactor = edgeLength / _TessellationEdgeLength;
	//float lamda = 0.03;
	//float edgeFactor = maxFactor * exp(-lamda * viewDistance);
	//edgeFactor = max(1, edgeFactor); // Edge factor should never go below zero

	// Useful to keep factors constant when the player is close enough
	/*float edgeFactor = MUX(edgeLength / (_TessellationEdgeLength * viewDistance), 
						   edgeLength / _TessellationEdgeLength, 
						   viewDistance > _TessellationEnableDistance / 20);*/
	float edgeFactor = edgeLength / (_TessellationEdgeLength * viewDistance);
	edgeFactor = MUX(edgeFactor, 1, edgeFactor >= 5);

	return MUX(edgeFactor, 1, viewDistance < _TessellationEnableDistance);
}

float FixedTessellationEdgeFactor(TessellationControlPoint tcp) {
	float3 p = mul(unity_ObjectToWorld, float4(tcp.vertex.xyz, 1)).xyz;
	float viewDistance = max(50, distance(p, _WorldSpaceCameraPos)) - 49;

	//float edgeFactor = 1 / (_TessellationEdgeLength * viewDistance);
	//edgeFactor = MUX(edgeFactor, 1, edgeFactor >= 5);
	// TODO: check why (viewDistance / _TessellationEnableDistance) seems to give results not in (0, 1)
	float edgeFactor = lerp(1 / _TessellationEdgeLength, 1, viewDistance / _TessellationEnableDistance);
	edgeFactor = MUX(edgeFactor, 1, edgeFactor >= 2);

	return MUX(edgeFactor, 1, viewDistance < _TessellationEnableDistance);
}

TessellationFactors PatchConstantFunction (InputPatch<TessellationControlPoint, 3> patch) {
	/*TessellationFactors f;
    f.edge[0] = TessellationEdgeFactor(patch[1], patch[2]);
    f.edge[1] = TessellationEdgeFactor(patch[2], patch[0]);
    f.edge[2] = TessellationEdgeFactor(patch[0], patch[1]);
	f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) * (1 / 3.0);
	return f;*/

	float f0 = FixedTessellationEdgeFactor(patch[0]);
	float f1 = FixedTessellationEdgeFactor(patch[1]);
	float f2 = FixedTessellationEdgeFactor(patch[2]);

	TessellationFactors f;
	f.edge[0] = (f1 + f2) / 2;
	f.edge[1] = (f0 + f2) / 2;
	f.edge[2] = (f0 + f1) / 2;
	f.inside = (f0 + f1 + f2) / 3;
	return f;
}

float SampleSimplex(float3 worldPosition) {
	float3 a = worldPosition * _SimplexNoiseFrequency;
	float3 b = (worldPosition + float3(0, 0, 1234)) * _SimplexNoiseFrequency;
	float3 c = (worldPosition + float3(1234, 0, 0)) * _SimplexNoiseFrequency;
	float3 vec = float3(noise3D(a.x, a.y, a.z), noise3D(b.x, b.y, b.z), noise3D(c.x, c.y, c.z));
	float displacement = noise3D(vec.x, vec.y, vec.z);
	float sign = ((displacement > 0) - (displacement < 0));
	displacement = (displacement + displacement * displacement + displacement * displacement * displacement) / 3;

	float viewDistance = distance(worldPosition, _WorldSpaceCameraPos);
	//displacement *= MUX(1, exp(-0.5 * viewDistance), viewDistance < _TessellationEnableDistance / 1.5);
	float t = (viewDistance / _TessellationEnableDistance - 0.5) * 2;
	t = viewDistance / (float)_TessellationEnableDistance;
	float amortizedDisplacement = (1 - t * t * t * t * t * t) * displacement;//lerp(displacement, 0, t);
	displacement = MUX(displacement, 
					   displacement / (1 + viewDistance - (_TessellationEnableDistance / 1.2)),
					   viewDistance <= _TessellationEnableDistance / 1.2);

	return displacement * _SimplexNoiseAmplitude;
}

float3 CalculateSimplexGradient(float3 p) {
	float delta = 0.01;
	float partialX = (SampleSimplex(p + float3(delta, 0, 0)) - SampleSimplex(p)) / delta;
	float partialY = (SampleSimplex(p + float3(0, delta, 0)) - SampleSimplex(p)) / delta;
	float partialZ = (SampleSimplex(p + float3(0, 0, delta)) - SampleSimplex(p)) / delta;
	return float3(partialX, partialY, partialZ);
}

// Math from https://math.stackexchange.com/questions/1071662/surface-normal-to-point-on-displaced-sphere
float3 CalculateSimplexNormal(float3 worldPosition, float3 normal, float displacement) {
	float3 gradient = CalculateSimplexGradient(worldPosition);
	float3 h = gradient - dot(gradient, normal) * normal;
	float3 n = normal - displacement * h;
	return normalize(n);	// Is this is object space or in world space?
}

FragmentData SimplexDisplacement (VertexData v, int isTessellating) {
	FragmentData o;

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
	
	float displacement = SampleSimplex(worldPos) * isTessellating;	

	float3 normal = normalize(v.normal);
	float3 displacementNormal = float3(0, 1, 0);//normal;//
	float3 displacedVertex = v.vertex.xyz + displacementNormal * displacement;

	o.position = UnityObjectToClipPos(float4(displacedVertex, 1));
	o.worldPosition.xyz = mul(unity_ObjectToWorld, float4(displacedVertex, 1));	
	o.uv.xy = v.uv;//TRANSFORM_TEX(v.uv, _MainTex);
	o.normal = MUX(CalculateSimplexNormal(worldPos, displacementNormal, displacement), 
				   UnityObjectToWorldNormal(normal), isTessellating);

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