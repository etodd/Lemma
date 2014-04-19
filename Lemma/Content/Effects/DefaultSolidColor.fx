#include "RenderCommon.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
	float3 normal : NORMAL0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out FlatPSInput flat,
				out MotionBlurPSInput motionBlur)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;

	flat.normal = mul(input.normal, WorldMatrix);
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(input.position, LastFrameWorldViewProjectionMatrix);
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out FlatPSInput flat,
				out ClipPSInput clipData,
				out MotionBlurPSInput motionBlur)
{
	RenderVS(input, vs, output, flat, motionBlur);
	clipData = GetClipData(output.position);
}

technique Render
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderFlatPS();
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
		PixelShader = compile ps_3_0 ClipFlatPS();
	}
}