#include "RenderCommonAlpha.fxh"
#include "PostProcess\Shadow2D.fxh"

float StartDistance;
float VerticalSize;
float VerticalCenter;
float3 CameraPosition;
float GodRayStrength;
float GodRayExtinction;
float WaterHeight;

sampler2D SkyboxSampler = sampler_state
{
	Texture = <DiffuseTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float4x4 ShadowViewProjectionMatrix;

float4x4 ViewMatrixRotationOnly;

texture2D RandomTexture;
sampler2D RandomSampler = sampler_state
{
	Texture = <RandomTexture>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	AddressU = WRAP;
	AddressV = WRAP;
};

struct RenderVSInput
{
	float4 position : POSITION0;
	float2 uvCoordinates : TEXCOORD0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out AlphaPSInput alpha)
{
	float4 worldPosition = float4(mul(input.position.xyz, (float3x3)WorldMatrix), 1);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrixRotationOnly);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	tex.uvCoordinates = input.uvCoordinates;
	alpha.clipSpacePosition = vs.position;
}

#define FOG_SHADOW_SAMPLES 11
void SkyboxPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0,
						uniform bool vertical,
						uniform bool shadow,
						uniform bool water)
{
	float2 uv = (0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w) + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;

	float depth = tex2D(DepthSampler, uv).r;

	float3 viewRay = normalize(input.position);

	if (water)
	{
		float waterDepth = (WaterHeight - CameraPosition.y) / viewRay.y;
		if (waterDepth > 0)
			depth = min(depth, waterDepth);
	}

	float blend = clamp(lerp(0, 1, (depth - StartDistance) / (FarPlaneDistance - StartDistance)), 0, 1);

	if (vertical)
		blend = min(1.0f, blend + max(0, 1.0f - ((CameraPosition + viewRay * depth).y - VerticalCenter) / VerticalSize));

	float4 color = tex2D(SkyboxSampler, tex.uvCoordinates);

	float interval = (depth - StartDistance) / FOG_SHADOW_SAMPLES;

	if (shadow)
	{
		float shadowValue = FOG_SHADOW_SAMPLES;

		float3 s = CameraPosition + viewRay * StartDistance;
		viewRay *= interval;

		float lastValue = 0.0f;
		[unroll]
		for (int i = 0; i < FOG_SHADOW_SAMPLES; i++)
		{
			s += viewRay;
			float4 shadowPos = mul(float4(s + tex2D(RandomSampler, s.xy), 1.0f), ShadowViewProjectionMatrix);
			float v = GetShadowValueRaw(shadowPos);
			float newValue = 0.0f;
			if (v > 0.0f)
				newValue = 1.0f - min(v * GodRayExtinction, 1);
			shadowValue -= (newValue + lastValue) * 0.5f;
			lastValue = newValue;
		}
		output.rgb = DiffuseColor.rgb * color.rgb * ((1.0f - GodRayStrength) + (shadowValue / FOG_SHADOW_SAMPLES) * GodRayStrength);
	}
	else
		output.rgb = DiffuseColor.rgb * color.rgb;

	output.a = blend;
}

void SkyboxNormalPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, false, false, false);
}

void SkyboxVerticalPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, true, false, false);
}

void SkyboxNormalGodRayPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, false, true, false);
}

void SkyboxVerticalGodRayPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, true, true, false);
}

void SkyboxWaterPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, false, false, true);
}

void SkyboxWaterGodRayPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, false, true, true);
}

// No shadow technique. We don't want the skybox casting shadows.

technique Render
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxNormalPS();
	}
}

technique Clip
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;

		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxNormalPS();
	}
}

technique RenderGodRays
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxNormalGodRayPS();
	}
}

technique ClipGodRays
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxNormalPS();
	}
}

technique RenderVertical
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxVerticalPS();
	}
}

technique ClipVertical
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;

		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxVerticalPS();
	}
}

technique RenderVerticalGodRays
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxVerticalGodRayPS();
	}
}

technique ClipVerticalGodRays
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxVerticalPS();
	}
}

technique RenderWater
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxWaterPS();
	}
}

technique ClipWater
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;

		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxNormalPS();
	}
}

technique RenderWaterGodRays
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxWaterGodRayPS();
	}
}

technique ClipWaterGodRays
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxNormalPS();
	}
}