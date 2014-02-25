#include "Common.fxh"

float3 Position;
float2 Scale;

float3 Color = float3(1, 1, 1);
float3 UnderwaterColor = float3(1, 1, 1);
float Brightness = 0.5f;
float Fresnel = 0.7f;
float ActualFarPlaneDistance;
float2 DestinationDimensions;
float Clearness = 1.0f;

float Distortion = 1.0f;

float RippleDensity = 1.0f;

float Speed = 0.075f;

float Time = 0.0f;

struct RenderVSInput
{
	float4 position : POSITION0;
	float2 uvCoordinates : TEXCOORD0;
	float3 normal : NORMAL0;
};

struct SurfacePSInput
{
	float4 clipSpacePosition : TEXCOORD5;
	float3 worldPosition : TEXCOORD6;
	float distortionAmount : TEXCOORD7;
	float4 viewSpacePosition : TEXCOORD8;
};

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

texture2D ReflectionTexture;
sampler2D ReflectionSampler = sampler_state
{
	Texture = <ReflectionTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

void SurfaceVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out SurfacePSInput output,
				out TexturePSInput tex,
				out FlatPSInput flat)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = input.position;
	worldPosition.xyz *= float3(Scale.x, 1.0f, Scale.y);
	worldPosition.xyz += Position;
	output.worldPosition = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	output.viewSpacePosition = viewSpacePosition;
	float4 clipSpacePosition = mul(viewSpacePosition, ProjectionMatrix);
	vs.position = clipSpacePosition;
	output.clipSpacePosition = clipSpacePosition;

	float4 clipSpacePosition2 = mul(worldPosition + float4(0, 0, 0.2f, 0), ViewProjectionMatrix);
	output.distortionAmount = length(clipSpacePosition2 - clipSpacePosition) * Distortion;

	tex.uvCoordinates = input.uvCoordinates * Scale * RippleDensity * (400.0f / 2000.0f) + (float2(Time, Time) * Speed);
	flat.normal = input.normal;
}

void SurfacePS(	in SurfacePSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						uniform bool reflection,
						out float4 color : COLOR0)
{
	// Convert from clip space to UV coordinate space
	float2 uv = 0.5f * input.clipSpacePosition.xy / input.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;

	float2 distortion = (tex2D(NormalMapSampler, tex.uvCoordinates).xy - float2(0.5f, 0.5f)) * 2.0f * input.distortionAmount;

	float existingDepth = tex2D(DepthSampler, uv).r;

	float depth = existingDepth - length(input.viewSpacePosition);

	if (existingDepth < ActualFarPlaneDistance)
		clip(depth - distortion.x * 5.0f);

	uv += distortion;

	float depthBlend = clamp(depth * 0.5f * (1.0f - Clearness), 0.0f, 1.0f);

	float3 normal = flat.normal;
	normal.xz += distortion;
	normal = normalize(normal);
	float fresnel = abs(dot(normalize(CameraPosition - input.worldPosition), normal));

	float3 refraction = lerp(tex2D(FrameSampler, uv).xyz * Color + Brightness, UnderwaterColor, depthBlend);

	if (reflection)
		color = float4(EncodeColor(lerp(tex2D(ReflectionSampler, uv).xyz, refraction, fresnel * Fresnel)), 1.0f);
	else
		color = float4(EncodeColor(refraction), 1.0f);
}

struct UnderwaterPSInput
{
	float4 clipSpacePosition : TEXCOORD5;
};

void UnderwaterVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out UnderwaterPSInput output)
{
	float4 pos = float4(input.position.xyz, 1.0f);
	vs.position = pos;
	output.clipSpacePosition = pos;
}

void UnderwaterPS(	in UnderwaterPSInput input,
					out float4 color : COLOR0)
{
	// Convert from clip space to UV coordinate space
	float2 uv = 0.5f * input.clipSpacePosition.xy / input.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;

	float depth = tex2D(DepthSampler, uv).r;

	float depthBlend = clamp((depth - 2.0f) * 0.25f * (1.0f - Clearness), 0.0f, 1.0f);

	color = float4(EncodeColor(UnderwaterColor), 0.2f + (0.8f * depthBlend));
}

void SurfaceWithReflection(in SurfacePSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						out float4 color : COLOR0)
{
	SurfacePS(input, tex, flat, true, color);
}

void SurfaceWithoutReflection(in SurfacePSInput input,
						in TexturePSInput tex,
						in FlatPSInput flat,
						out float4 color : COLOR0)
{
	SurfacePS(input, tex, flat, false, color);
}

technique SurfaceReflection
{
	pass p0
	{
		VertexShader = compile vs_3_0 SurfaceVS();
		PixelShader = compile ps_3_0 SurfaceWithReflection();
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	}
}

technique Surface
{
	pass p0
	{
		VertexShader = compile vs_3_0 SurfaceVS();
		PixelShader = compile ps_3_0 SurfaceWithoutReflection();
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = false;
	}
}

technique Underwater
{
	pass p0
	{
		VertexShader = compile vs_3_0 UnderwaterVS();
		PixelShader = compile ps_3_0 UnderwaterPS();
		ZEnable = true;
		ZWriteEnable = true;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	}
}