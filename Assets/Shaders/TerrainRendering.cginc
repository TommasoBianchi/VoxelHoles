// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(TERRAIN_RENDERING_INCLUDED)
#define TERRAIN_RENDERING_INCLUDED

#include "BaseStructs.cginc"
#include "UnityPBSLighting.cginc"

sampler2D _MainTex;

sampler2D _PlainTexture;
sampler2D _SlopeTexture;

float3 CalculateAlbedo(FragmentData f);

FragmentData TerrainVertex(VertexData v){
	FragmentData f;
	f.position = UnityObjectToClipPos(v.vertex);
	f.worldPosition.xyz = mul(unity_ObjectToWorld, v.vertex);;	
	f.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
	f.normal = UnityObjectToWorldNormal(v.normal);	
	return f;
}

float4 TerrainFragment(FragmentData f) : SV_Target {
	f.normal = normalize(f.normal);
	float3 viewDirection = normalize(_WorldSpaceCameraPos - f.worldPosition);
	float3 lightDirection = _WorldSpaceLightPos0.xyz;
	float3 lightColor = _LightColor0.rgb;
	float3 albedo = CalculateAlbedo(f);
	float3 diffuse = albedo * lightColor * DotClamped(lightDirection, f.normal);	// Not needed anymore

	UnityLight light;
	light.color = lightColor;
	light.dir = lightDirection;
	light.ndotl = DotClamped(f.normal, lightDirection);

	UnityIndirect indirectLight;
	indirectLight.diffuse = max(0, ShadeSH9(float4(f.normal, 1)));	// Sperical armonics, used for ambient and skybox lighting
	indirectLight.specular = 0;

	return UNITY_BRDF_PBS(
		albedo, 0,
		1, 0,
		f.normal, viewDirection,
		light, indirectLight
	);
}

float3 CalculateAlbedo(FragmentData f) {
	float plainness = saturate(abs(f.normal.y));
	float slopeness = 1 - plainness * plainness * plainness * plainness * plainness;
	float3 plainAlbedo = tex2D(_PlainTexture, f.uv).rgb;
	float3 slopeAlbedo = tex2D(_SlopeTexture, f.uv).rgb;
	return  slopeness * slopeAlbedo + (1 - slopeness) * plainAlbedo;
}

#endif