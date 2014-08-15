#include "RenderCommonAlpha.fxh"

float3 Scale;
float3 Offset;

// Frame buffer sampler
texture2D FrameTexture;
sampler2D FrameSampler = sampler_state
{
	Texture = <FrameTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

struct RenderVSInput
{
	float4 position : POSITION0;
	float3 normal : NORMAL0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out AlphaPSInput alpha)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	alpha.clipSpacePosition = vs.position;

	float diff = length(input.position * input.normal) * 2;

	float3 pos = (input.position.xyz + Offset) * Scale;

	tex.uvCoordinates = float2(diff + pos.x + (pos.z * input.normal.x), diff - pos.y + (pos.z * input.normal.y)) * 0.125f;
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out AlphaPSInput alpha,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, alpha);
	clipData = GetClipData(output.position);
}

void DistortionPS(
	in RenderPSInput input,
	in TexturePSInput tex,
	in AlphaPSInput alpha,
	out float4 output : COLOR0)
{
	// Convert from clip space to UV coordinate space
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;

	float depth = length(input.viewSpacePosition);
	clip(tex2D(DepthSampler, uv).r - depth);

	float4 t = tex2D(NormalMapSampler, tex.uvCoordinates);
	
	float a = t.a * Alpha;
	float2 distortion = ((t.xy * 2.0f) - 1.0f) * a * 0.2f * (1.0f - (depth / FarPlaneDistance));

	float3 color = tex2D(FrameSampler, uv + distortion).rgb * DiffuseColor.rgb;
	output = float4(color, a);
}

void ClipDistortionPS(in RenderPSInput input,
						in TexturePSInput tex,
						in AlphaPSInput alpha,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	DistortionPS(input, tex, alpha, output);
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
		PixelShader = compile ps_3_0 DistortionPS();
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
		PixelShader = compile ps_3_0 ClipDistortionPS();
	}
}