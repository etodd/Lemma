#include "CloudCommon.fxh"

// No shadow technique. We don't want the clouds casting shadows.

technique Render
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = One;
	
		VertexShader = compile vs_3_0 RenderVS(false);
		PixelShader = compile ps_3_0 CloudPS(false);
	}
}

technique RenderInfinite
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = One;
	
		VertexShader = compile vs_3_0 RenderVS(true);
		PixelShader = compile ps_3_0 CloudPS(true);
	}
}

technique Clip
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = One;

		VertexShader = compile vs_3_0 RenderVS(false);
		PixelShader = compile ps_3_0 CloudPS(false);
	}
}

technique ClipInfinite
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = One;

		VertexShader = compile vs_3_0 RenderVS(true);
		PixelShader = compile ps_3_0 CloudPS(true);
	}
}