#include "EffectCommon.fxh"
#include "EffectSamplers.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
};

float4 Color;
float StartDistance;
float EndDistance;
float VerticalSize;
float VerticalCenter;

const float2 Scale = float2(500.0f, 500.0f);

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

struct SurfacePSInput
{
	float4 clipSpacePosition : TEXCOORD5;
	float3 viewRay : TEXCOORD6;
};

void SurfaceVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out SurfacePSInput output)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = input.position;
	worldPosition.xyz *= float3(Scale.x, 1.0f, Scale.y);
	worldPosition.y += VerticalCenter;
	float3 viewPos = mul(worldPosition.xyz, (float3x3)ViewMatrix);
	output.viewRay = worldPosition;
	float4 clipSpacePosition = mul(float4(viewPos, 1), ProjectionMatrix);
	vs.position = clipSpacePosition;
	output.clipSpacePosition = clipSpacePosition;
}

void FogVerticalSurfacePS(	in SurfacePSInput input,
					out float4 color : COLOR0)
{
	// Convert from clip space to UV coordinate space
	float2 uv = 0.5f * input.clipSpacePosition.xy / input.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;

	float3 worldPosition = PositionFromDepthSampler(DepthSampler, uv, normalize(input.viewRay));

	float verticalBlend = 1.0f - clamp((worldPosition.y - (VerticalCenter - VerticalSize)) / VerticalSize, 0, 1);

	color = float4(Color.rgb, Color.a * verticalBlend);
}

struct UndersurfacePSInput
{
	float2 texCoord : TEXCOORD5;
	float3 viewRay : TEXCOORD6;
};

void UndersurfaceVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out UndersurfacePSInput output)
{
	float4 pos = float4(input.position.xyz, 1.0f);
	vs.position = pos;

	// Convert from clip space to UV coordinate space
	output.texCoord = 0.5f * pos.xy / pos.w + float2(0.5f, 0.5f);
	output.texCoord.y = 1.0f - output.texCoord.y;
	output.texCoord = (round(output.texCoord * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;

	float4 v = mul(pos, InverseViewProjectionMatrix);
	output.viewRay = v.xyz / v.w;
}

void FogPS(	in UndersurfacePSInput input,
					out float4 color : COLOR0)
{
	// Convert from clip space to UV coordinate space
	float depth = tex2D(DepthSampler, input.texCoord).r;

	float blend = clamp((depth - StartDistance) / (EndDistance - StartDistance), 0, 1);

	color = float4(Color.rgb, Color.a * blend);
}

void FogVerticalPS(	in UndersurfacePSInput input,
					out float4 color : COLOR0)
{
	// Convert from clip space to UV coordinate space

	float3 worldPosition = PositionFromDepthSampler(DepthSampler, input.texCoord, normalize(input.viewRay));

	float verticalBlend = 1.0f - clamp((worldPosition.y - (VerticalCenter - VerticalSize)) / VerticalSize, 0, 1);

	color = float4(Color.rgb, Color.a * verticalBlend);
}

technique FogVerticalSurface
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		
		VertexShader = compile vs_3_0 SurfaceVS();
		PixelShader = compile ps_3_0 FogVerticalSurfacePS();
	}
}

technique FogVertical
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		
		VertexShader = compile vs_3_0 UndersurfaceVS();
		PixelShader = compile ps_3_0 FogVerticalPS();
	}
}

technique Fog
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		
		VertexShader = compile vs_3_0 UndersurfaceVS();
		PixelShader = compile ps_3_0 FogPS();
	}
}