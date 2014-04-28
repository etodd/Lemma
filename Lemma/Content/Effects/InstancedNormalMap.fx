#include "RenderCommon.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
	float3 normal : NORMAL0;
	float3 binormal : BINORMAL0;
	float3 tangent : TANGENT0;
	float2 uvCoordinates : TEXCOORD0;
	float4x4 instanceTransform : BLENDWEIGHT0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out NormalMapPSInput normalMap,
				in float4x4 lastInstanceTransform : BLENDWEIGHT4,
				out MotionBlurPSInput motionBlur)
{
	// Calculate the clip-space vertex position
	float4x4 world = mul(WorldMatrix, transpose(input.instanceTransform));
	float4 worldPosition = mul(input.position, world);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;

	tex.uvCoordinates = input.uvCoordinates;

	normalMap.tangentToWorld[0] = normalize(mul(input.tangent, world));
	normalMap.tangentToWorld[1] = normalize(mul(input.binormal, world));
	normalMap.tangentToWorld[2] = normalize(mul(input.normal, world));
	
	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(input.position, mul(transpose(lastInstanceTransform), LastFrameWorldViewProjectionMatrix));
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out NormalMapPSInput normalMap,
				in float4x4 lastInstanceTransform : BLENDWEIGHT4,
				out MotionBlurPSInput motionBlur,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, normalMap, lastInstanceTransform, motionBlur);
	clipData = GetClipData(output.position);
}

// Shadow vertex shader
void ShadowVS (	in float4 in_Position			: POSITION,
				in float4x4 instanceTransform	: BLENDWEIGHT,
				out ShadowVSOutput vs,
				out ShadowPSInput output)
{
	// Calculate shadow-space position
	float4x4 world = mul(WorldMatrix, transpose(instanceTransform));
	float4 worldPosition = mul(in_Position, world);
	output.worldPosition = worldPosition.xyz;
	vs.position = mul(worldPosition, ViewProjectionMatrix);
	output.clipSpacePosition = vs.position;
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

technique Render
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 RenderTextureNormalMapPlainPS();
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
		PixelShader = compile ps_3_0 ClipTextureNormalMapPlainPS();
	}
}