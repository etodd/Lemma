#include "RenderCommon.fxh"

float2x2 UVScaleRotation;
float2 UVOffset;
float OverlayTiling;

float2 CalculateUVs(float3 pos, float3 normal)
{
	float diff = length(pos * normal) * 2;
	float2 uv = float2(diff + pos.x + (pos.z * normal.x), diff - pos.y + (pos.z * normal.y));
	return mul(uv, UVScaleRotation) + UVOffset;
}

void CalculateOverlayUVs(float3 pos, float3 normal, out TexturePSInput tex, out OverlayPSInput overlay)
{
	float diff = length(pos * normal) * 2;
	float2 uv = float2(diff + pos.x + (pos.z * normal.x), diff - pos.y + (pos.z * normal.y));
	overlay.uvCoordinates = uv * OverlayTiling + UVOffset;
	tex.uvCoordinates = mul(uv, UVScaleRotation) + UVOffset;
}

// Overlay pixel shaders
texture2D OverlayTexture;
sampler2D OverlaySampler = sampler_state
{
	Texture = <OverlayTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

void RenderTextureNormalMapPlainOverlayPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								in OverlayPSInput overlay,
								out RenderPSOutput output)
{
	RenderTextureNormalMapOverlayPS(input, tex, normalMap, output, motionBlurInput, overlay, OverlaySampler, false);
}


void ClipTextureNormalMapPlainOverlayPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								in MotionBlurPSInput motionBlurInput,
								in OverlayPSInput overlay,
								out RenderPSOutput output)
{
	ClipTextureNormalMapOverlayPS(input, tex, normalMap, clipData, output, motionBlurInput, overlay, OverlaySampler, false);
}

void RenderTextureNormalMapClipOverlayAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								in OverlayPSInput overlay,
								out RenderPSOutput output)
{
	RenderTextureNormalMapOverlayPS(input, tex, normalMap, output, motionBlurInput, overlay, OverlaySampler, true);
}

void ClipTextureNormalMapClipOverlayAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								in MotionBlurPSInput motionBlurInput,
								in OverlayPSInput overlay,
								out RenderPSOutput output)
{
	ClipTextureNormalMapOverlayPS(input, tex, normalMap, clipData, output, motionBlurInput, overlay, OverlaySampler, true);
}