#include "RenderCommonAlpha.fxh"

float Time;
float2 Scale;

const float BorderFadeSize = 2.0f;

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
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	alpha.clipSpacePosition = vs.position;

	tex.uvCoordinates = input.uvCoordinates;
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out AlphaPSInput alpha,
				out TexturePSInput tex,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, alpha, tex);
	clipData = GetClipData(output.position);
}

void RenderPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	float depth = length(input.viewSpacePosition);
	clip(tex2D(DepthSampler, uv).r - depth);
	uv = tex.uvCoordinates * Scale * 0.15f;
	float4 color = tex2D(DiffuseSampler, uv + float2(Time, -Time) * 0.01f + tex2D(NormalMapSampler, uv * 0.15f + float2(Time, Time) * 0.02f).xy * 0.75f);
	
	output.xyz = EncodeColor(DiffuseColor.xyz * color.xyz);

	float2 borderSize = float2(BorderFadeSize / Scale.x, BorderFadeSize / Scale.y);
	float borderFadeX;
	if (tex.uvCoordinates.x < borderSize.x)
		borderFadeX = tex.uvCoordinates.x / borderSize.x;
	else
	{
		float x = tex.uvCoordinates.x - (1.0f - borderSize.x);
		if (x > 0)
			borderFadeX = 1.0f - (x / borderSize.x);
		else
			borderFadeX = 1;
	}

	float borderFadeY;
	if (tex.uvCoordinates.y < borderSize.y)
		borderFadeY = tex.uvCoordinates.y / borderSize.y;
	else
	{
		float y = tex.uvCoordinates.y - (1.0f - borderSize.y);
		if (y > 0)
			borderFadeY = 1.0f - (y / borderSize.y);
		else
			borderFadeY = 1;
	}

	output.w = Alpha * color.w * (0.8f - (depth / 30.0f)) * borderFadeX * borderFadeY;
}

void ClipPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderPS(input, alpha, tex, output);
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
		PixelShader = compile ps_3_0 RenderPS();
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
		PixelShader = compile ps_3_0 ClipPS();
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
		PixelShader = compile ps_3_0 RenderPS();
	}
}