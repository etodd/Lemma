#include "RenderCommon.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out MotionBlurPSInput motionBlur)
{
	float4 worldPosition = mul(input.position, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(input.position, LastFrameWorldViewProjectionMatrix);
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out MotionBlurPSInput motionBlur,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, motionBlur);
	clipData = GetClipData(output.position);
}

// No shadow technique. We don't want unlit objects casting shadows.

technique Render
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderSolidColorPS();
	}
}

technique Clip
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipVS();
		PixelShader = compile ps_3_0 ClipSolidColorPS();
	}
}