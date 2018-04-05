// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(TERRAIN_RENDERING_INCLUDED)
#define TERRAIN_RENDERING_INCLUDED

#include "BaseStructs.cginc"
#include "UnityPBSLighting.cginc"

sampler2D _PlainTexture;
sampler2D _PlainDetailTexture;
sampler2D _SlopeTexture;
sampler2D _SlopeDetailTexture;

float4 _PlainTexture_ST;
float4 _PlainDetailTexture_ST;
float4 _SlopeTexture_ST;
float4 _SlopeDetailTexture_ST;

float _SlopeAngleTreshold;
float _MountainTransitionStartAltitude;
float _MountainTransitionEndAltitude;

float3 TriplanarMappingAlbedo(FragmentData f);
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
	float3 albedo = TriplanarMappingAlbedo(f);//CalculateAlbedo(f);//
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

// Code from https://gamedevelopment.tutsplus.com/articles/use-tri-planar-texture-mapping-for-better-terrain--gamedev-13821
float3 TriplanarMappingAlbedo(FragmentData f){
	float3 blending = abs(f.normal);
	blending = normalize(max(blending, 0.00001)); // Force weights to sum to 1.0
	float b = (blending.x + blending.y + blending.z);
	blending /= b;

	FragmentData temp1;
	temp1.position = f.position;
	temp1.worldPosition = f.worldPosition;
	temp1.normal = f.normal;
	/*FragmentData temp2;
	temp2.position = f.position;
	temp2.worldPosition = f.worldPosition;
	temp2.normal = f.normal;
	FragmentData temp3;
	temp3.position = f.position;
	temp3.worldPosition = f.worldPosition;
	temp3.normal = f.normal;*/

	temp1.uv.xy = f.worldPosition.yz;
	float3 xaxis = CalculateAlbedo(temp1);
	temp1.uv.xy = f.worldPosition.xz;
	float3 yaxis = CalculateAlbedo(temp1);
	temp1.uv.xy = f.worldPosition.xy;
	float3 zaxis = CalculateAlbedo(temp1);
	float3 tex = xaxis * blending.x + yaxis * blending.y + zaxis * blending.z;

	return tex;
}

float3 CalculateAlbedo(FragmentData f) {
	// Parameters
	float slopeness = saturate((acos(f.normal.y) / radians(180)) / radians(_SlopeAngleTreshold));

	float altitude = (f.worldPosition.y - _MountainTransitionStartAltitude) / (_MountainTransitionEndAltitude - _MountainTransitionStartAltitude);
	altitude = MUX(0, altitude, f.worldPosition.y < _MountainTransitionStartAltitude);
	altitude = MUX(1, altitude, f.worldPosition.y > _MountainTransitionEndAltitude);

	float2 plainUV = TRANSFORM_TEX(f.uv, _PlainTexture);
	float2 plainDetailUV = TRANSFORM_TEX(f.uv, _PlainDetailTexture);
	float2 slopeUV = TRANSFORM_TEX(f.uv, _SlopeTexture);
	float2 slopeDetailUV = TRANSFORM_TEX(f.uv, _SlopeDetailTexture);

	float3 plainAlbedo = tex2D(_PlainTexture, plainUV).rgb;
	float3 plainDetailAlbedo = tex2D(_PlainDetailTexture, plainDetailUV).rgb;
	//float freq = 0.05;
	//float t = (noise3D(f.worldPosition.x * freq, f.worldPosition.y * freq, f.worldPosition.z * freq) + 1) / 2;
	plainAlbedo = (plainAlbedo + plainDetailAlbedo) / 2;
	//plainAlbedo = t * plainAlbedo + (1 - t) * plainDetailAlbedo;

	float3 slopeAlbedo = tex2D(_SlopeTexture, slopeUV).rgb;
	float3 slopeDetailAlbedo = tex2D(_SlopeDetailTexture, slopeDetailUV).rgb;
	//freq = 1;
	//t = (noise3D(f.worldPosition.x * freq, f.worldPosition.y * freq, f.worldPosition.z * freq) + 1) / 2;
	slopeAlbedo = (slopeAlbedo + slopeDetailAlbedo) / 2;
	//slopeAlbedo = t * slopeAlbedo + (1 - t) * slopeDetailAlbedo;
	
	float3 albedo = lerp(plainAlbedo, slopeAlbedo, slopeness);
	albedo = lerp(albedo, slopeAlbedo, altitude);

	return albedo;
}

#endif