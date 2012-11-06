#include "RenderCommon.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
	float3 normal : NORMAL0;
	float2 uvCoordinates : TEXCOORD0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out FlatPSInput flat)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;

	tex.uvCoordinates = input.uvCoordinates;
	flat.normal = mul(input.normal, WorldMatrix);
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out FlatPSInput flat,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, flat);
	clipData = GetClipData(output.position);
}

// Motion blur vertex shader
void MotionBlurVS ( in RenderVSInput input,
					out RenderVSOutput vs,
					out RenderPSInput output,
					out TexturePSInput tex,
					out FlatPSInput flat,
					out MotionBlurPSInput motionBlur)
{
	RenderVS(input, vs, output, tex, flat);
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(input.position, LastFrameWorldViewProjectionMatrix);
}

// Shadow vertex shader
void ShadowVS (	in float4 in_Position : POSITION,
				in float2 in_UvCoordinates : TEXCOORD0,
				out ShadowVSOutput vs,
				out TexturePSInput tex,
				out ShadowPSInput output)
{
	// Calculate shadow-space position
	float4 worldPosition = mul(in_Position, WorldMatrix);
	output.worldPosition = worldPosition.xyz;
	vs.position = mul(worldPosition, ViewProjectionMatrix);
	output.clipSpacePosition = vs.position;
	tex.uvCoordinates = in_UvCoordinates;
}

technique Shadow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowVS();
		PixelShader = compile ps_3_0 ShadowPS();
	}
}

technique PointLightShadow
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 ShadowVS();
		PixelShader = compile ps_3_0 PointLightShadowPS();
	}
}

technique Render
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderTextureFlatPlainPS();
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
		PixelShader = compile ps_3_0 ClipTextureFlatPlainPS();
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
		PixelShader = compile ps_3_0 MotionBlurTextureFlatPlainPS();
	}
}