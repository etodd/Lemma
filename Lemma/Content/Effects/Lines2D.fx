#include "Common.fxh"

float4x4 Transform;
float4 Color = float4(1, 1, 1, 1);

struct VSInput
{
	float4 position : POSITION0;
	float4 color : COLOR0;
};

struct PSInput
{
	float4 position : POSITION0;
	float4 color: TEXCOORD0;
};

void RenderVS(	in VSInput input,
				out PSInput output)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, Transform);
	output.position = worldPosition;
	output.color = input.color * Color;
}

void RenderPS (in PSInput input, out float4 color : COLOR0)
{
	color = input.color;
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
		PixelShader = compile ps_3_0 RenderPS();
	}
}