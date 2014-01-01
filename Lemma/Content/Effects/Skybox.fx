#include "RenderCommonAlpha.fxh"

float StartDistance;

float4x4 ViewMatrixRotationOnly;
float4x4 LastFrameViewProjectionMatrixRotationOnly;
float4x4 LastFrameWorldMatrix;

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

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out AlphaPSInput alpha,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex, alpha);
	clipData = GetClipData(output.position);
}

void SkyboxPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						out float4 output : COLOR0)
{
	float2 uv = 0.5f * alpha.clipSpacePosition.xy / alpha.clipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;
	uv = (round(uv * DestinationDimensions) + float2(0.5f, 0.5f)) / DestinationDimensions;

	float blend = clamp(lerp(0, 1, (tex2D(DepthSampler, uv).r - StartDistance) / (FarPlaneDistance - StartDistance)), 0, 1);

	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	
	output.xyz = EncodeColor(DiffuseColor.xyz * color.xyz);
	output.w = blend;
}

void ClipSkyboxPS(in RenderPSInput input,
						in AlphaPSInput alpha,
						in TexturePSInput tex,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	SkyboxPS(input, alpha, tex, output);
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
		PixelShader = compile ps_3_0 SkyboxPS();
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

		VertexShader = compile vs_3_0 ClipVS();
		PixelShader = compile ps_3_0 ClipSkyboxPS();
	}
}

technique MotionBlur
{
	pass p0
	{
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
	
		VertexShader = compile vs_3_0 RenderVS();
		PixelShader = compile ps_3_0 SkyboxPS();
	}
}