#include "RenderCommon.fxh"

float4x4 ViewMatrixRotationOnly;
float4x4 LastFrameViewProjectionMatrixRotationOnly;
float4x4 LastFrameWorldMatrix;

struct RenderVSInput
{
	float4 position : POSITION0;
	float2 uvCoordinates : TEXCOORD0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex)
{
	float4 worldPosition = float4(mul(input.position.xyz, (float3x3)WorldMatrix), 1);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrixRotationOnly);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	tex.uvCoordinates = input.uvCoordinates;
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex);
	clipData = GetClipData(output.position);
}

// Motion blur vertex shader
void MotionBlurVS(	in RenderVSInput input,
					out RenderVSOutput vs,
					out RenderPSInput output,
					out TexturePSInput tex,
					out MotionBlurPSInput motionBlur)
{
	RenderVS(input, vs, output, tex);
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;

	motionBlur.previousPosition = mul(float4(mul(input.position.xyz, (float3x3)LastFrameWorldMatrix), 1), LastFrameViewProjectionMatrixRotationOnly);
}

// No shadow technique. We don't want the skybox casting shadows.

technique Render
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderTextureNoDepthPlainPS();
	}
}

technique Clip
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = false;

		VertexShader = compile vs_3_0 ClipVS();
		PixelShader = compile ps_3_0 ClipTextureNoDepthPlainPS();
	}
}

technique MotionBlur
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 MotionBlurVS();
		PixelShader = compile ps_3_0 MotionBlurTextureNoDepthPlainPS();
	}
}