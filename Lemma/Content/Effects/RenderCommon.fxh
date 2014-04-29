#include "Common.fxh"
#include "ClipCommon.fxh"

// Transform matrices
float4x4 WorldMatrix;
float4x4 LastFrameWorldViewProjectionMatrix;
float2 DestinationDimensions;

// Diffuse texture (optional)
texture2D DiffuseTexture;
sampler2D DiffuseSampler = sampler_state
{
	Texture = <DiffuseTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

// Normal map (optional)
texture2D NormalMapTexture;
sampler2D NormalMapSampler = sampler_state
{
	Texture = <NormalMapTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

// Far clip plane
float FarPlaneDistance;

// Material parameters
float3 DiffuseColor = float3(1.0f, 1.0f, 1.0f);
int Materials[2];

// Motion blur
float2 ProcessMotionBlur(in MotionBlurPSInput input)
{
	float2 velocity = (input.currentPosition.xy / input.currentPosition.w) - (input.previousPosition.xy / input.previousPosition.w);
	velocity.y *= -1.0f;
	return EncodeVelocity(velocity, DestinationDimensions);
}

// Shadow pixel shader
void ShadowPS (	in ShadowPSInput input,
				out float4 out_Depth : COLOR0)
{
	out_Depth = float4(1.0f - (input.clipSpacePosition.z / input.clipSpacePosition.w), 1.0f, 1.0f, 1.0f);
}

void ShadowAlphaPS (	in ShadowPSInput input,
				in TexturePSInput tex,
				out float4 out_Depth : COLOR0)
{
	clip(tex2D(DiffuseSampler, tex.uvCoordinates).a - 0.5f);
	out_Depth = float4(1.0f - (input.clipSpacePosition.z / input.clipSpacePosition.w), 1.0f, 1.0f, 1.0f);
}

// Prefab pixel shaders

void RenderTextureFlatPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						uniform bool clipAlpha)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);

	if (clipAlpha)
		clip(color.a - 0.5f);

	float3 normal = normalize(flat.normal);
	
	output.color.rgb = DiffuseColor.rgb * color.rgb;
	output.color.a = EncodeMaterial(Materials[(int)(color.a < 0.9f)]);
	output.depth.x = length(input.viewSpacePosition);
	output.normal.xy = EncodeNormal(normal.xy);
	output.depth.y = normal.z;
	output.depth.zw = (float2)0;
	output.normal.zw = ProcessMotionBlur(motionBlurInput);
}

void RenderTextureFlatPlainPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, motionBlurInput, false);
}

void RenderTextureFlatClipAlphaPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, motionBlurInput, true);
}

void ClipTextureFlatPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureFlatPS(input, tex, flat, output, motionBlurInput, clipAlpha);
}

void ClipTextureFlatPlainPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, motionBlurInput, false);
}

void ClipTextureFlatClipAlphaPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, motionBlurInput, true);
}

void RenderTextureNormalMapPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								out RenderPSOutput output,
								in MotionBlurPSInput motionBlurInput,
								uniform bool clipAlpha)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	float3 normal = mul(DecodeNormalMap(tex2D(NormalMapSampler, tex.uvCoordinates).xyz), normalMap.tangentToWorld);
	output.color.rgb = DiffuseColor.rgb * color.rgb;
	output.color.a = EncodeMaterial(Materials[(int)(color.a < 0.9f)]);
	output.depth.x = length(input.viewSpacePosition);
	output.normal.xy = EncodeNormal(normal.xy);
	output.depth.y = normal.z;
	output.depth.zw = (float2)0;
	output.normal.zw = ProcessMotionBlur(motionBlurInput);
}

void RenderTextureNormalMapPlainPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, motionBlurInput, false);
}

void RenderTextureNormalMapClipAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, motionBlurInput, true);
}

void ClipTextureNormalMapPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								out RenderPSOutput output,
								in MotionBlurPSInput motionBlurInput,
								uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureNormalMapPS(input, tex, normalMap, output, motionBlurInput, clipAlpha);
}

void ClipTextureNormalMapPlainPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, motionBlurInput, false);
}

void ClipTextureNormalMapClipAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, motionBlurInput, true);
}

void RenderFlatPS(in RenderPSInput input,
						in FlatPSInput flat,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	float3 normal = normalize(flat.normal);
	
	output.color.rgb = DiffuseColor.rgb;
	output.color.a = EncodeMaterial(Materials[0]);
	output.depth.x = length(input.viewSpacePosition);
	output.normal.xy = EncodeNormal(normal.xy);
	output.depth.y = normal.z;
	output.depth.zw = (float2)0;
	output.normal.zw = ProcessMotionBlur(motionBlurInput);
}

void ClipFlatPS(in RenderPSInput input,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderFlatPS(input, flat, motionBlurInput, output);
}

void RenderTexturePS(	in RenderPSInput input,
						in FlatPSInput flat,
						in TexturePSInput tex,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						uniform bool clipAlpha)
{
	float3 normal = normalize(flat.normal);
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	output.color.rgb = DiffuseColor.rgb * color.rgb;
	output.color.a = 0.0f;
	output.depth.x = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal.xy = EncodeNormal(normal.xy);
	output.depth.y = normal.z;
	output.depth.zw = (float2)0;
	output.normal.zw = ProcessMotionBlur(motionBlurInput);
}

void RenderTexturePlainPS(	in RenderPSInput input,
						in FlatPSInput flat,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	RenderTexturePS(input, flat, tex, output, motionBlurInput, false);
}

void RenderTextureClipAlphaPS(	in RenderPSInput input,
						in FlatPSInput flat,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	RenderTexturePS(input, flat, tex, output, motionBlurInput, true);
}

void ClipTexturePS(	in RenderPSInput input,
						in FlatPSInput flat,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTexturePS(input, flat, tex, output, motionBlurInput, clipAlpha);
}

void ClipTexturePlainPS(	in RenderPSInput input,
						in FlatPSInput flat,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	ClipTexturePS(input, flat, tex, clipData, output, motionBlurInput, false);
}

void ClipTextureClipAlphaPS(	in RenderPSInput input,
						in FlatPSInput flat,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	ClipTexturePS(input, flat, tex, clipData, output, motionBlurInput, true);
}

void RenderTextureNoDepthPS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						uniform bool clipAlpha)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	output.color.rgb = DiffuseColor.rgb * color.rgb;
	output.color.a = 0.0f;
	output.depth.x = FarPlaneDistance;
	output.depth.zw = (float2)0;
	output.normal.xy = EncodeNormal(float2(0.0f, 0.0f));
	output.depth.y = 1.0f;
	output.normal.zw = ProcessMotionBlur(motionBlurInput);
}

void RenderTextureNoDepthPlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	RenderTextureNoDepthPS(input, tex, output, motionBlurInput, false);
}

void RenderTextureNoDepthClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	RenderTextureNoDepthPS(input, tex, output, motionBlurInput, true);
}

void ClipTextureNoDepthPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureNoDepthPS(input, tex, output, motionBlurInput, clipAlpha);
}

void ClipTextureNoDepthPlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	ClipTextureNoDepthPS(input, tex, clipData, output, motionBlurInput, false);
}

void ClipTextureNoDepthClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out RenderPSOutput output)
{
	ClipTextureNoDepthPS(input, tex, clipData, output, motionBlurInput, true);
}

void RenderSolidColorPS(	in RenderPSInput input,
							in FlatPSInput flat,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output)
{
	float3 normal = normalize(flat.normal);

	output.color.rgb = DiffuseColor.rgb;
	output.color.a = 0.0f;
	output.depth.x = length(input.viewSpacePosition);
	output.normal.xy = EncodeNormal(normal.xy);
	output.depth.y = normal.z;
	output.depth.zw = (float2)0;
	output.normal.zw = ProcessMotionBlur(motionBlurInput);
}

void ClipSolidColorPS(	in RenderPSInput input,
							in FlatPSInput flat,
							in ClipPSInput clipData,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderSolidColorPS(input, flat, motionBlurInput, output);
}