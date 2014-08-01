#include "ParticleOpaqueCommon.fxh"

technique RenderOpaqueParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 OpaqueVS(false, 1.0f);
		PixelShader = compile ps_3_0 OpaquePS();
		AlphaBlendEnable = false;
		ZEnable = true;
		ZWriteEnable = true;
	}
}

technique ClipOpaqueParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 ClipOpaqueVS(false, 1.0f);
		PixelShader = compile ps_3_0 ClipOpaquePS();
		AlphaBlendEnable = false;
		ZEnable = true;
		ZWriteEnable = true;
	}
}