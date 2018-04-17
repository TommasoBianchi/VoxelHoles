// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(TERRAIN_RENDERING_INCLUDED)
#define TERRAIN_RENDERING_INCLUDED

#include "BaseStructs.cginc"
#include "UnityPBSLighting.cginc"

sampler2D _PlainTexture;
sampler2D _PlainNormalMap;
sampler2D _PlainDetailTexture;
sampler2D _PlainDetailNormalMap;
sampler2D _SlopeTexture;
sampler2D _SlopeNormalMap;
sampler2D _SlopeDetailTexture;
sampler2D _SlopeDetailNormalMap;

float4 _PlainTexture_ST;
float4 _PlainDetailTexture_ST;
float4 _SlopeTexture_ST;
float4 _SlopeDetailTexture_ST;

float _NormalBumpScale;

float _SlopeAngleTreshold;
float _MountainTransitionStartAltitude;
float _MountainTransitionEndAltitude;

struct SplatParameters {
	float slopeness;
	float altitude;
};

float3 TriplanarMappingAlbedo(FragmentData f, SplatParameters sp);
float3 TriplanarMappingNormal(FragmentData f, SplatParameters sp);
SplatParameters CalculateSplatParameters(FragmentData f);
float3 CalculateAlbedo(FragmentData f, SplatParameters sp);
float3 CalculateNormal(FragmentData f, SplatParameters sp);
float4 ApplyFog(float4 color, float3 worldPosition);

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
	SplatParameters sp = CalculateSplatParameters(f);
	float3 albedo = TriplanarMappingAlbedo(f, sp);//CalculateAlbedo(f, sp);//
	//float3 diffuse = albedo * lightColor * DotClamped(lightDirection, f.normal);	// Not needed anymore
	f.normal = normalize(TriplanarMappingNormal(f, sp));

	UnityLight light;
	light.color = lightColor;
	light.dir = lightDirection;
	light.ndotl = DotClamped(f.normal, lightDirection);

	UnityIndirect indirectLight;
	indirectLight.diffuse = max(0, ShadeSH9(float4(f.normal, 1)));	// Sperical armonics, used for ambient and skybox lighting
	indirectLight.specular = 0;

	float4 fragmentColor = UNITY_BRDF_PBS(
		albedo, 0,
		1, 0,
		f.normal, viewDirection,
		light, indirectLight
	);

	return ApplyFog(fragmentColor, f.worldPosition);
}

SplatParameters CalculateSplatParameters(FragmentData f) {
	SplatParameters sp;

	// Slopeness
	sp.slopeness = saturate((acos(f.normal.y) / radians(180)) / radians(_SlopeAngleTreshold));

	// Altitude
	sp.altitude = (f.worldPosition.y - _MountainTransitionStartAltitude) / (_MountainTransitionEndAltitude - _MountainTransitionStartAltitude);
	sp.altitude = MUX(0, sp.altitude, f.worldPosition.y < _MountainTransitionStartAltitude);
	sp.altitude = MUX(1, sp.altitude, f.worldPosition.y > _MountainTransitionEndAltitude);

	return sp;
}

// Code from https://gamedevelopment.tutsplus.com/articles/use-tri-planar-texture-mapping-for-better-terrain--gamedev-13821
float3 TriplanarMappingAlbedo(FragmentData f, SplatParameters sp){
	float3 blending = abs(f.normal);
	blending = normalize(max(blending, 0.00001)); // Force weights to sum to 1.0
	float b = (blending.x + blending.y + blending.z);
	blending /= b;

	FragmentData temp;
	temp.position = f.position;
	temp.worldPosition = f.worldPosition;
	temp.normal = f.normal;

	temp.uv.xy = f.worldPosition.yz;
	float3 xaxis = CalculateAlbedo(temp, sp);
	temp.uv.xy = f.worldPosition.xz;
	float3 yaxis = CalculateAlbedo(temp, sp);
	temp.uv.xy = f.worldPosition.xy;
	float3 zaxis = CalculateAlbedo(temp, sp);
	float3 tex = xaxis * blending.x + yaxis * blending.y + zaxis * blending.z;

	return tex;
}

float3 CalculateAlbedo(FragmentData f, SplatParameters sp) {
	float2 plainUV = TRANSFORM_TEX(f.uv, _PlainTexture);
	float2 plainDetailUV = TRANSFORM_TEX(f.uv, _PlainDetailTexture);
	float2 slopeUV = TRANSFORM_TEX(f.uv, _SlopeTexture);
	float2 slopeDetailUV = TRANSFORM_TEX(f.uv, _SlopeDetailTexture);

	float3 plainAlbedo = tex2D(_PlainTexture, plainUV).rgb;
	float3 plainDetailAlbedo = tex2D(_PlainDetailTexture, plainDetailUV).rgb;
	plainAlbedo = (plainAlbedo + plainDetailAlbedo) / 2;

	float3 slopeAlbedo = tex2D(_SlopeTexture, slopeUV).rgb;
	float3 slopeDetailAlbedo = tex2D(_SlopeDetailTexture, slopeDetailUV).rgb;
	slopeAlbedo = (slopeAlbedo + slopeDetailAlbedo) / 2;
	
	float3 albedo = lerp(plainAlbedo, slopeAlbedo, sp.slopeness);
	albedo = lerp(albedo, slopeAlbedo, sp.altitude);

	return albedo;
}

// Code from https://gamedevelopment.tutsplus.com/articles/use-tri-planar-texture-mapping-for-better-terrain--gamedev-13821
float3 TriplanarMappingNormal(FragmentData f, SplatParameters sp) {
	float3 blending = abs(f.normal);
	blending = normalize(max(blending, 0.00001)); // Force weights to sum to 1.0
	float b = (blending.x + blending.y + blending.z);
	blending /= b;

	FragmentData temp;
	temp.position = f.position;
	temp.worldPosition = f.worldPosition;
	temp.normal = f.normal;

	temp.uv.xy = f.worldPosition.yz;
	float3 xaxis = CalculateNormal(temp, sp);
	temp.uv.xy = f.worldPosition.xz;
	float3 yaxis = CalculateNormal(temp, sp);
	temp.uv.xy = f.worldPosition.xy;
	float3 zaxis = CalculateNormal(temp, sp);
	float3 tex = xaxis * blending.x + yaxis * blending.y + zaxis * blending.z;

	return tex;
}

float3 CalculateNormal(FragmentData f, SplatParameters sp) {
	float2 plainUV = TRANSFORM_TEX(f.uv, _PlainTexture);
	float2 plainDetailUV = TRANSFORM_TEX(f.uv, _PlainDetailTexture);
	float2 slopeUV = TRANSFORM_TEX(f.uv, _SlopeTexture);
	float2 slopeDetailUV = TRANSFORM_TEX(f.uv, _SlopeDetailTexture);

	float3 plainNormal = normalize(UnpackScaleNormal(tex2D(_PlainNormalMap, f.uv), _NormalBumpScale).xzy);
	float3 plainDetailNormal = normalize(UnpackScaleNormal(tex2D(_PlainDetailNormalMap, f.uv), _NormalBumpScale).xzy);
	plainNormal = (plainNormal + plainDetailNormal) / 2;

	float3 slopeNormal = normalize(UnpackScaleNormal(tex2D(_SlopeNormalMap, f.uv), _NormalBumpScale).xzy);
	float3 slopeDetailNormal = normalize(UnpackScaleNormal(tex2D(_SlopeDetailNormalMap, f.uv), _NormalBumpScale).xzy);
	slopeNormal = (slopeNormal + slopeDetailNormal) / 2;

	float3 normal = lerp(plainNormal, slopeNormal, sp.slopeness);
	normal = lerp(normal, slopeNormal, sp.altitude);

	return normalize(normal);
	return plainNormal;
}

float4 ApplyFog(float4 color, float3 worldPosition) {
	#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
		float viewDistance = length(_WorldSpaceCameraPos - worldPosition);
		UNITY_CALC_FOG_FACTOR_RAW(viewDistance);
		color.rgb = lerp(unity_FogColor.rgb, color.rgb, saturate(unityFogFactor));
		color.a = 1 - saturate(unityFogFactor);
		return color;
	#else
		return color;
	#endif
}

#endif