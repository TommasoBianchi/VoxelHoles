#if !defined(TERRAIN_RENDERING_INCLUDED)
#define TERRAIN_RENDERING_INCLUDED

#include "BaseStructs.cginc"

sampler2D _MainTex;

float4 TerrainFragment(FragmentData f) : SV_Target {
	return tex2D(_MainTex, f.uv);;
}

#endif