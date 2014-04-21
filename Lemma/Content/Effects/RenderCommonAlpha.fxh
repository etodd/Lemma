#include "RenderCommon.fxh"

float Alpha = 1.0f;
float2 DestinationDimensions;

texture2D DepthTexture;
sampler2D DepthSampler = sampler_state
{
	Texture = <DepthTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

void RenderTextureAlphaPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	clip(tex2D(DepthSampler, uv).r - length(input.viewSpacePosition));
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	
	output.rgb = DiffuseColor.rgb * color.rgb;
	output.a = Alpha * color.a;
}

void ClipTextureAlphaPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureAlphaPS(input, alpha, tex, output);
}

void RenderSolidColorAlphaPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	clip(tex2D(DepthSampler, uv).r - length(input.viewSpacePosition));
	output.rgb = DiffuseColor.rgb;
	output.a = Alpha;
}

void ClipSolidColorAlphaPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderSolidColorAlphaPS(input, alpha, output);
}

void RenderVertexColorAlphaPS(in RenderPSInput input,
						in VertexColorPSInput color,
						in AlphaPSInput alpha,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	clip(tex2D(DepthSampler, uv).r - length(input.viewSpacePosition));
	output.rgb = DiffuseColor.rgb * color.color.rgb;
	output.a = Alpha * color.color.a;
}

void ClipVertexColorAlphaPS(in RenderPSInput input,
						in VertexColorPSInput color,
						in AlphaPSInput alpha,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderVertexColorAlphaPS(input, color, alpha, output);
}