#include "RenderCommonAlpha.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
	float4 color : COLOR0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out VertexColorPSInput color,
				out AlphaPSInput alpha)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = input.position;
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	alpha.clipSpacePosition = vs.position;
	color.color = input.color;
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out VertexColorPSInput color,
				out AlphaPSInput alpha,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, color, alpha);
	clipData = GetClipData(output.position);
}

technique Render
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderVertexColorAlphaPS();
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
		DestBlend = InvSrcAlpha;

		VertexShader = compile vs_3_0 ClipVS();
		PixelShader = compile ps_3_0 ClipVertexColorAlphaPS();
	}
}