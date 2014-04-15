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
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 CloudPS();
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

		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 CloudPS();
	}
}

technique MotionBlur
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = One;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 CloudPS();
	}
}