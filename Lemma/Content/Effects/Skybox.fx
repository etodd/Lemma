#include "RenderCommonAlpha.fxh"

float StartDistance;
float VerticalSize;
float VerticalCenter;

float4x4 ViewMatrixRotationOnly;

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

void SkyboxPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0,
						uniform bool vertical)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;

	float depth = tex2D(DepthSampler, uv).r;

	float blend = clamp(lerp(0, 1, (depth - StartDistance) / (FarPlaneDistance - StartDistance)), 0, 1);

	if (vertical)
	{
		float3 worldPosition = normalize(input.position) * depth;
		blend = min(1.0f, blend + max(0, 1.0f - (worldPosition.y - VerticalCenter) / VerticalSize));
	}

	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	
	output.rgb = DiffuseColor.rgb * color.rgb;
	output.a = blend;
}

void SkyboxNormalPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, false);
}

void SkyboxVerticalPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	SkyboxPS(input, alpha, tex, output, true);
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