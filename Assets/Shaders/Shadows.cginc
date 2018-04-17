#if !defined(MY_SHADOWS_INCLUDED)
#define MY_SHADOWS_INCLUDED

#include "UnityCG.cginc"
#include "BaseStructs.cginc"

FragmentData ShadowCasterVertex (VertexData v) : SV_POSITION {
	FragmentData f;
	float4 position = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
	f.pos = UnityApplyLinearShadowBias(position);
	return f;
}

half4 ShadowCasterFragment () : SV_TARGET {
	return 0;
}

#endif