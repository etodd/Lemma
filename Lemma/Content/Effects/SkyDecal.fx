#include "RenderCommonAlpha.fxh"

float3 CameraPosition;
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
	float4 worldPosition = float4(mul(input.position.xyz + float3(1, 0, 0), (float3x3)WorldMatrix), 1);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrixRotationOnly);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;
	tex.uvCoordinates = input.uvCoordinates;
	alpha.clipSpacePosition = vs.position;
}

void SkyDecalPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;
	clip(tex2D(DepthSampler, uv).r - FarPlaneDistance);
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	
	output.rgb = DiffuseColor.rgb * color.rgb;
	output.a = Alpha * color.a;
}

// No shadow technique. We don't want the sky decal casting shadows.

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
		PixelShader = compile ps_3_0 SkyDecalPS();
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
		PixelShader = compile ps_3_0 SkyDecalPS();
	}
}