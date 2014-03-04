#include "RenderCommonAlpha.fxh"

float StartDistance;

float Time;
float2 Velocity;
float Height;

struct RenderVSInput
{
	float4 position : POSITION0;
	float2 uvCoordinates : TEXCOORD0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out AlphaPSInput alpha,
				out TexturePSInput tex)
{
	float3 worldPosition = mul(input.position, WorldMatrix);
	worldPosition.y += Height;
	output.position = float4(worldPosition, 1);
	float3 viewSpacePosition = mul(worldPosition, (float3x3)ViewMatrix);
	vs.position = mul(float4(viewSpacePosition, 1), ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	alpha.clipSpacePosition = vs.position;

	tex.uvCoordinates = input.uvCoordinates;
}

void CloudPS(in RenderPSInput input,
						in TexturePSInput tex,
						in AlphaPSInput alpha,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	float depth = tex2D(DepthSampler, uv).r;
	float4 texColor = tex2D(DiffuseSampler, tex.uvCoordinates + Velocity * Time);
	output.xyz = EncodeColor(DiffuseColor.xyz * texColor.xyz);

	float blend = clamp(lerp(0, 1, (depth - StartDistance) / (FarPlaneDistance - StartDistance)), 0, 1);

	output.w = Alpha * texColor.w * blend * (1.0f - 2.0f * length(tex.uvCoordinates - float2(0.5f, 0.5f)));
}

void ClipCloudPS(in RenderPSInput input,
						in TexturePSInput tex,
						in AlphaPSInput alpha,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	CloudPS(input, tex, alpha, output);
}

// No shadow technique. We don't want the clouds casting shadows.

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
		PixelShader = compile ps_3_0 CloudPS();
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

		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 CloudPS();
	}
}

technique MotionBlur
{
	pass p0
	{
		ZEnable = true;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 CloudPS();
	}
}