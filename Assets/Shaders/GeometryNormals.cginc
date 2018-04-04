#if !defined(GEOMETRY_NORMALS_INCLUDED)
#define GEOMETRY_NORMALS_INCLUDED

#include "BaseStructs.cginc"

struct GeometryOutputData {
	float4 position : POSITION;
	float2 uv : TEXCOORD0;
	float4 worldPosition;
	float3 cumulativeNormal;
};

float4 NilFragment(FragmentData f) : SV_Target {
	return 0;
}

FragmentData VertexNormalNormalizer(GeometryOutputData g) {
	FragmentData f;

	f.position = g.position;
	f.uv.xy = g.uv;
	f.worldPosition = g.worldPosition;

	f.normal = 0;

	return f;
}

[maxvertexcount(3)]
void GeometryCalculateNormals (triangle FragmentData p[3], inout TriangleStream<FragmentData> triStream) {
	float3 triangleNormal = normalize(cross(p[1].worldPosition.xyz - p[0].worldPosition.xyz, p[2].worldPosition.xyz - p[0].worldPosition.xyz));

	//p[0].normal += triangleNormal;
	//p[1].normal += triangleNormal;
	//p[2].normal += triangleNormal;

	FragmentData p0;
	p0.position = p[0].position;
	p0.uv = p[0].uv;
	p0.worldPosition = p[0].worldPosition;
	p0.normal = triangleNormal;
	
	FragmentData p1;
	p1.position = p[1].position;
	p1.uv = p[1].uv;
	p1.worldPosition = p[1].worldPosition;
	p1.normal = triangleNormal;
	
	FragmentData p2;
	p2.position = p[2].position;
	p2.uv = p[2].uv;
	p2.worldPosition = p[2].worldPosition;
	p2.normal = triangleNormal;

	triStream.Append(p0);
	triStream.Append(p1);
	triStream.Append(p2);
}

#endif