#include "Common.fxh"
#include "ClipCommon.fxh"

// Transform matrices
float4x4 WorldMatrix;
float4x4 LastFrameWorldViewProjectionMatrix;

float3 PointLightPosition;
float PointLightRadius;

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
float SpecularPower = 20.0f;
float SpecularIntensity = 0.2f;

// Motion blur
void ProcessMotionBlur(in MotionBlurPSInput input, out MotionBlurPSOutput output)
{
	// Calculate the instantaneous pixel velocity. Since clip-space coordinates are of the range [-1, 1] 
	// with Y increasing from the bottom to the top of screen, we'll rescale x and y and flip y so that
	// the velocity corresponds to texture coordinates (which are of the range [0,1], and y increases from top to bottom)
	float2 velocity = (input.currentPosition.xy / input.currentPosition.w) - (input.previousPosition.xy / input.previousPosition.w);
	velocity *= 0.5f;
	velocity.y *= -1.0f;
	velocity = float2(0.5f, 0.5f) + velocity;
	output.velocity = float4(velocity, 1.0f, 1.0f);
}

// Shadow pixel shader
void ShadowPS (	in ShadowPSInput input,
				out float4 out_Depth : COLOR0)
{
	out_Depth = float4(1.0f - (input.clipSpacePosition.z / input.clipSpacePosition.w), 1.0f, 1.0f, 1.0f);
}

// Point light shadow pixel shader
void PointLightShadowPS (	in ShadowPSInput input,
							out float4 out_Depth : COLOR0)
{
	out_Depth = float4(1.0f - (length(input.worldPosition - PointLightPosition) / PointLightRadius), 1.0f, 1.0f, 1.0f);
}

void ShadowAlphaPS (	in ShadowPSInput input,
				in TexturePSInput tex,
				out float4 out_Depth : COLOR0)
{
	clip(tex2D(DiffuseSampler, tex.uvCoordinates).a - 0.5f);
	out_Depth = float4(1.0f - (input.clipSpacePosition.z / input.clipSpacePosition.w), 1.0f, 1.0f, 1.0f);
}

// Point light shadow pixel shader
void PointLightShadowAlphaPS (	in ShadowPSInput input,
							in TexturePSInput tex,
							out float4 out_Depth : COLOR0)
{
	clip(tex2D(DiffuseSampler, tex.uvCoordinates).a - 0.5f);
	out_Depth = float4(1.0f - (length(input.worldPosition - PointLightPosition) / PointLightRadius), 1.0f, 1.0f, 1.0f);
}

// Prefab pixel shaders

void RenderTextureFlatPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						out RenderPSOutput output,
						uniform bool clipAlpha,
						uniform bool glow)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);

	if (clipAlpha)
		clip(color.a - 0.5f);

	float3 normal = normalize(flat.normal);
	
	output.color.xyz = EncodeColor(DiffuseColor.xyz * color.xyz);
	if (glow)
	{
		if (color.a < 0.9f)
			output.color.a = SpecularPower / 255.0f;
		else
		{
			output.color.a = 0.0f;
			output.color.rgb *= 2.0f;
		}
	}
	else
		output.color.a = SpecularPower / 255.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal.xyz = EncodeNormal(normal);
	output.normal.w = SpecularIntensity;
}

void RenderTextureFlatPlainPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, false, false);
}

void RenderTextureFlatClipAlphaPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, true, false);
}

void RenderTextureFlatGlowPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, false, true);
}

void ClipTextureFlatPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						uniform bool clipAlpha,
						uniform bool glow)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureFlatPS(input, tex, flat, output, clipAlpha, glow);
}

void ClipTextureFlatPlainPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, false, false);
}

void ClipTextureFlatClipAlphaPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, true, false);
}

void ClipTextureFlatGlowPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, false, true);
}

void MotionBlurTextureFlatPS ( in RenderPSInput input,
					in TexturePSInput tex,
					in FlatPSInput flat,
					in MotionBlurPSInput motionBlurInput,
					out RenderPSOutput output,
					out MotionBlurPSOutput motionBlurOutput,
					uniform bool clipAlpha,
					uniform bool glow)
{
	RenderTextureFlatPS(input, tex, flat, output, clipAlpha, glow);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void MotionBlurTextureFlatPlainPS ( in RenderPSInput input,
					in TexturePSInput tex,
					in FlatPSInput flat,
					in MotionBlurPSInput motionBlurInput,
					out RenderPSOutput output,
					out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureFlatPS(input, tex, flat, motionBlurInput, output, motionBlurOutput, false, false);
}

void MotionBlurTextureFlatClipAlphaPS ( in RenderPSInput input,
					in TexturePSInput tex,
					in FlatPSInput flat,
					in MotionBlurPSInput motionBlurInput,
					out RenderPSOutput output,
					out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureFlatPS(input, tex, flat, motionBlurInput, output, motionBlurOutput, true, false);
}

void MotionBlurTextureFlatGlowPS ( in RenderPSInput input,
					in TexturePSInput tex,
					in FlatPSInput flat,
					in MotionBlurPSInput motionBlurInput,
					out RenderPSOutput output,
					out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureFlatPS(input, tex, flat, motionBlurInput, output, motionBlurOutput, false, true);
}

void RenderTextureNormalMapPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								out RenderPSOutput output,
								uniform bool clipAlpha,
								uniform bool glow)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	float3 normal = mul(DecodeNormalMap(tex2D(NormalMapSampler, tex.uvCoordinates).xyz), normalMap.tangentToWorld);
	output.normal.xyz = EncodeNormal(normal);
	output.normal.w = SpecularIntensity;
	output.color.rgb = EncodeColor(DiffuseColor.rgb * color.rgb);
	if (glow)
	{
		if (color.a < 0.9f)
			output.color.a = SpecularPower / 255.0f;
		else
		{
			output.color.a = 0.0f;
			output.color.rgb *= 2.0f;
		}
	}
	else
		output.color.a = SpecularPower / 255.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
}

void RenderTextureNormalMapPlainPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, false, false);
}

void RenderTextureNormalMapClipAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, true, false);
}

void RenderTextureNormalMapGlowPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, false, true);
}

void ClipTextureNormalMapPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								out RenderPSOutput output,
								uniform bool clipAlpha,
								uniform bool glow)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureNormalMapPS(input, tex, normalMap, output, clipAlpha, glow);
}

void ClipTextureNormalMapPlainPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, false, false);
}

void ClipTextureNormalMapClipAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, true, false);
}

void ClipTextureNormalMapGlowPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, false, true);
}

void MotionBlurTextureNormalMapPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output,
								out MotionBlurPSOutput motionBlurOutput,
								uniform bool clipAlpha,
								uniform bool glow)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, clipAlpha, glow);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void MotionBlurTextureNormalMapPlainPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output,
								out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureNormalMapPS(input, tex, normalMap, motionBlurInput, output, motionBlurOutput, false, false);
}

void MotionBlurTextureNormalMapClipAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output,
								out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureNormalMapPS(input, tex, normalMap, motionBlurInput, output, motionBlurOutput, true, false);
}

void MotionBlurTextureNormalMapGlowPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output,
								out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureNormalMapPS(input, tex, normalMap, motionBlurInput, output, motionBlurOutput, false, true);
}

void RenderFlatPS(in RenderPSInput input,
						in FlatPSInput flat,
						out RenderPSOutput output)
{
	float3 normal = normalize(flat.normal);
	
	output.color.xyz = EncodeColor(DiffuseColor.xyz);
	output.color.w = SpecularPower / 255.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal.xyz = EncodeNormal(normal);
	output.normal.w = SpecularIntensity;
}

void ClipFlatPS(in RenderPSInput input,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderFlatPS(input, flat, output);
}

void MotionBlurFlatPS ( in RenderPSInput input,
					in FlatPSInput flat,
					in MotionBlurPSInput motionBlurInput,
					out RenderPSOutput output,
					out MotionBlurPSOutput motionBlurOutput)
{
	RenderFlatPS(input, flat, output);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void RenderTexturePS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output,
						uniform bool clipAlpha)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	output.color.xyz = EncodeColor(DiffuseColor.xyz * color.xyz);
	output.color.w = 0.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal = float4(0.5f, 0.5f, 0.5f, 0.0f);
}

void RenderTexturePlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output)
{
	RenderTexturePS(input, tex, output, false);
}

void RenderTextureClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output)
{
	RenderTexturePS(input, tex, output, true);
}

void ClipTexturePS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTexturePS(input, tex, output, clipAlpha);
}

void ClipTexturePlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	ClipTexturePS(input, tex, clipData, output, false);
}

void ClipTextureClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	ClipTexturePS(input, tex, clipData, output, true);
}

void MotionBlurTexturePS(	in RenderPSInput input,
							in TexturePSInput tex,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output,
							out MotionBlurPSOutput motionBlurOutput,
							uniform bool clipAlpha)
{
	RenderTexturePS(input, tex, output, clipAlpha);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void MotionBlurTexturePlainPS(	in RenderPSInput input,
							in TexturePSInput tex,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output,
							out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTexturePS(input, tex, motionBlurInput, output, motionBlurOutput, false);
}

void MotionBlurTextureClipAlphaPS(	in RenderPSInput input,
							in TexturePSInput tex,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output,
							out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTexturePS(input, tex, motionBlurInput, output, motionBlurOutput, true);
}

void RenderTextureNoDepthPS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output,
						uniform bool clipAlpha)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	output.color.xyz = EncodeColor(DiffuseColor.xyz * color.xyz);
	output.color.w = 0.0f;
	output.depth = float4(FarPlaneDistance, 1.0f, 1.0f, 1.0f);
	output.normal = float4(0.5f, 0.5f, 0.5f, 0.0f);
}

void RenderTextureNoDepthPlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output)
{
	RenderTextureNoDepthPS(input, tex, output, false);
}

void RenderTextureNoDepthClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output)
{
	RenderTextureNoDepthPS(input, tex, output, true);
}

void ClipTextureNoDepthPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureNoDepthPS(input, tex, output, clipAlpha);
}

void ClipTextureNoDepthPlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	ClipTextureNoDepthPS(input, tex, clipData, output, false);
}

void ClipTextureNoDepthClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output)
{
	ClipTextureNoDepthPS(input, tex, clipData, output, true);
}

void MotionBlurTextureNoDepthPS(	in RenderPSInput input,
							in TexturePSInput tex,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output,
							out MotionBlurPSOutput motionBlurOutput,
							uniform bool clipAlpha)
{
	RenderTextureNoDepthPS(input, tex, output, clipAlpha);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void MotionBlurTextureNoDepthPlainPS(	in RenderPSInput input,
							in TexturePSInput tex,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output,
							out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureNoDepthPS(input, tex, motionBlurInput, output, motionBlurOutput, false);
}

void MotionBlurTextureNoDepthClipAlphaPS(	in RenderPSInput input,
							in TexturePSInput tex,
							in MotionBlurPSInput motionBlurInput,
							out RenderPSOutput output,
							out MotionBlurPSOutput motionBlurOutput)
{
	MotionBlurTextureNoDepthPS(input, tex, motionBlurInput, output, motionBlurOutput, true);
}

void RenderSolidColorPS(	in RenderPSInput input,
							out RenderPSOutput output)
{
	output.color.xyz = EncodeColor(DiffuseColor.xyz);
	output.color.w = 0.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal = float4(0.5f, 0.5f, 0.5f, 0.0f);
}

void ClipSolidColorPS(	in RenderPSInput input,
							in ClipPSInput clipData,
							out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderSolidColorPS(input, output);
}

void MotionBlurSolidColorPS(	in RenderPSInput input,
								in MotionBlurPSInput motionBlurInput,
								out RenderPSOutput output,
								out MotionBlurPSOutput motionBlurOutput)
{
	RenderSolidColorPS(input, output);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}