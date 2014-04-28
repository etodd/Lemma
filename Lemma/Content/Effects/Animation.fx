#include "RenderCommon.fxh"

float4x3 Bones[78];

struct RenderVSInput
{
	float4 position : POSITION0;
	float3 normal : NORMAL0;
	float2 uvCoordinates : TEXCOORD0;
	int4 indices : BLENDINDICES0;
	float4 weights : BLENDWEIGHT0;
};

// Motion blur vertex shader
void RenderVS (	in RenderVSInput input,
					out RenderVSOutput vs,
					out RenderPSInput output,
					out TexturePSInput tex,
					out FlatPSInput flat,
					out MotionBlurPSInput motionBlur)
{
	float4x3 skinning = 0;

	[unroll]
	for (int i = 0; i < 4; i++)
		skinning += Bones[input.indices[i]] * input.weights[i];

	float4 pos = float4(mul(input.position, skinning), input.position.w);
	input.normal = mul(input.normal, (float3x3)skinning);
	float4 worldPosition = mul(pos, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;

	tex.uvCoordinates = input.uvCoordinates;

	flat.normal = mul(input.normal, WorldMatrix);

	// Pass along the current vertex position in clip-space,
	// as well as the previous vertex position in clip-space
	motionBlur.currentPosition = vs.position;
	motionBlur.previousPosition = mul(pos, LastFrameWorldViewProjectionMatrix);
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out FlatPSInput flat,
				out MotionBlurPSInput motionBlur,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, flat, motionBlur);
	clipData = GetClipData(output.position);
}

// Shadow vertex shader
void ShadowVS (	in float4 in_Position			: POSITION,
				in int4 in_Indices				: BLENDINDICES0,
				in float4 in_Weights			: BLENDWEIGHT0,
				out ShadowVSOutput vs,
				out ShadowPSInput output)
{
	// Calculate shadow-space position
	float4x3 skinning = 0;

	[unroll]
	for (int i = 0; i < 3; i++)
		skinning += Bones[in_Indices[i]] * in_Weights[i];
	float4 pos = float4(mul(in_Position, skinning), in_Position.w);
	
	// Calculate shadow-space position
	float4 worldPosition = mul(pos, WorldMatrix);
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