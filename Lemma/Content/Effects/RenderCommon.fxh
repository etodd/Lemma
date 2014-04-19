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
	float2 velocity = (input.currentPosition.xy / input.currentPosition.w) - (input.previousPosition.xy / input.previousPosition.w);
	velocity.y *= -1.0f;
	output.velocity = float4(EncodeVelocity(velocity), 1.0f, 1.0f);
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
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
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
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void RenderTextureFlatPlainPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, motionBlurInput, motionBlurOutput, false, false);
}

void RenderTextureFlatClipAlphaPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, motionBlurInput, motionBlurOutput, true, false);
}

void RenderTextureFlatGlowPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	RenderTextureFlatPS(input, tex, flat, output, motionBlurInput, motionBlurOutput, false, true);
}

void ClipTextureFlatPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						uniform bool clipAlpha,
						uniform bool glow)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureFlatPS(input, tex, flat, output, motionBlurInput, motionBlurOutput, clipAlpha, glow);
}

void ClipTextureFlatPlainPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, motionBlurInput, motionBlurOutput, false, false);
}

void ClipTextureFlatClipAlphaPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, motionBlurInput, motionBlurOutput, true, false);
}

void ClipTextureFlatGlowPS(in RenderPSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	ClipTextureFlatPS(input, tex, flat, clipData, output, motionBlurInput, motionBlurOutput, false, true);
}

void RenderTextureNormalMapPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								out RenderPSOutput output,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
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
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void RenderTextureNormalMapPlainPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, motionBlurInput, motionBlurOutput, false, false);
}

void RenderTextureNormalMapClipAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, motionBlurInput, motionBlurOutput, true, false);
}

void RenderTextureNormalMapGlowPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
								out RenderPSOutput output)
{
	RenderTextureNormalMapPS(input, tex, normalMap, output, motionBlurInput, motionBlurOutput, false, true);
}

void ClipTextureNormalMapPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								out RenderPSOutput output,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
								uniform bool clipAlpha,
								uniform bool glow)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureNormalMapPS(input, tex, normalMap, output, motionBlurInput, motionBlurOutput, clipAlpha, glow);
}

void ClipTextureNormalMapPlainPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, motionBlurInput, motionBlurOutput, false, false);
}

void ClipTextureNormalMapClipAlphaPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, motionBlurInput, motionBlurOutput, true, false);
}

void ClipTextureNormalMapGlowPS(	in RenderPSInput input,
								in TexturePSInput tex,
								in NormalMapPSInput normalMap,
								in ClipPSInput clipData,
								in MotionBlurPSInput motionBlurInput,
								out MotionBlurPSOutput motionBlurOutput,
								out RenderPSOutput output)
{
	ClipTextureNormalMapPS(input, tex, normalMap, clipData, output, motionBlurInput, motionBlurOutput, false, true);
}

void RenderFlatPS(in RenderPSInput input,
						in FlatPSInput flat,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	float3 normal = normalize(flat.normal);
	
	output.color.xyz = EncodeColor(DiffuseColor.xyz);
	output.color.w = SpecularPower / 255.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal.xyz = EncodeNormal(normal);
	output.normal.w = SpecularIntensity;
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void ClipFlatPS(in RenderPSInput input,
						in FlatPSInput flat,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderFlatPS(input, flat, motionBlurInput, motionBlurOutput, output);
}

void RenderTexturePS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						uniform bool clipAlpha)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	output.color.xyz = EncodeColor(DiffuseColor.xyz * color.xyz);
	output.color.w = 0.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal = float4(0.5f, 0.5f, 0.5f, 0.0f);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void RenderTexturePlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	RenderTexturePS(input, tex, output, motionBlurInput, motionBlurOutput, false);
}

void RenderTextureClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	RenderTexturePS(input, tex, output, motionBlurInput, motionBlurOutput, true);
}

void ClipTexturePS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTexturePS(input, tex, output, motionBlurInput, motionBlurOutput, clipAlpha);
}

void ClipTexturePlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	ClipTexturePS(input, tex, clipData, output, motionBlurInput, motionBlurOutput, false);
}

void ClipTextureClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	ClipTexturePS(input, tex, clipData, output, motionBlurInput, motionBlurOutput, true);
}

void RenderTextureNoDepthPS(	in RenderPSInput input,
						in TexturePSInput tex,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						uniform bool clipAlpha)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	if (clipAlpha)
		clip(color.a - 0.5f);
	output.color.xyz = EncodeColor(DiffuseColor.xyz * color.xyz);
	output.color.w = 0.0f;
	output.depth = float4(FarPlaneDistance, 1.0f, 1.0f, 1.0f);
	output.normal = float4(0.5f, 0.5f, 0.5f, 0.0f);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void RenderTextureNoDepthPlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	RenderTextureNoDepthPS(input, tex, output, motionBlurInput, motionBlurOutput, false);
}

void RenderTextureNoDepthClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	RenderTextureNoDepthPS(input, tex, output, motionBlurInput, motionBlurOutput, true);
}

void ClipTextureNoDepthPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out RenderPSOutput output,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						uniform bool clipAlpha)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureNoDepthPS(input, tex, output, motionBlurInput, motionBlurOutput, clipAlpha);
}

void ClipTextureNoDepthPlainPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	ClipTextureNoDepthPS(input, tex, clipData, output, motionBlurInput, motionBlurOutput, false);
}

void ClipTextureNoDepthClipAlphaPS(	in RenderPSInput input,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						in MotionBlurPSInput motionBlurInput,
						out MotionBlurPSOutput motionBlurOutput,
						out RenderPSOutput output)
{
	ClipTextureNoDepthPS(input, tex, clipData, output, motionBlurInput, motionBlurOutput, true);
}

void RenderSolidColorPS(	in RenderPSInput input,
							in MotionBlurPSInput motionBlurInput,
							out MotionBlurPSOutput motionBlurOutput,
							out RenderPSOutput output)
{
	output.color.xyz = EncodeColor(DiffuseColor.xyz);
	output.color.w = 0.0f;
	output.depth = float4(length(input.viewSpacePosition), 1.0f, 1.0f, 1.0f);
	output.normal = float4(0.5f, 0.5f, 0.5f, 0.0f);
	ProcessMotionBlur(motionBlurInput, motionBlurOutput);
}

void ClipSolidColorPS(	in RenderPSInput input,
							in ClipPSInput clipData,
							in MotionBlurPSInput motionBlurInput,
							out MotionBlurPSOutput motionBlurOutput,
							out RenderPSOutput output)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderSolidColorPS(input, motionBlurInput, motionBlurOutput, output);
}