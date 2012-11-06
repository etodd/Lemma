#include "RenderCommon.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output)
{
	float4 worldPosition = mul(input.position, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output);
	clipData = GetClipData(output.position);
}

// Motion blur vertex shader
void MotionBlurVS(	in RenderVSInput input,
					out RenderVSOutput vs,
					out RenderPSInput output,
					out MotionBlurPSInput motionBlur)
{
	RenderVS(input, vs, output);
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(input.position, LastFrameWorldViewProjectionMatrix);
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

technique MotionBlur
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 MotionBlurVS();
		PixelShader = compile ps_3_0 MotionBlurSolidColorPS();
	}
}