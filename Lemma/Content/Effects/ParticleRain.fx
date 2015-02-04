#include "ParticleOpaqueCommon.fxh"

technique RenderOpaqueParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 OpaqueVS(true, 0.01f);
		PixelShader = compile ps_3_0 OpaquePS();
		AlphaBlendEnable = false;
		ZEnable = true;
		ZWriteEnable = true;
	}
}