#include "RenderCommonAlpha.fxh"

struct RenderVSInput
{
	float4 position : POSITION0;
	float2 uvCoordinates : TEXCOORD0;
};

void RenderVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex)
{
	// Calculate the clip-space vertex position
	float4 worldPosition = mul(input.position, WorldMatrix);
	output.position = worldPosition;
	float4 viewSpacePosition = mul(worldPosition, ViewMatrix);
	vs.position = mul(viewSpacePosition, ProjectionMatrix);
	output.viewSpacePosition = viewSpacePosition;

	tex.uvCoordinates = input.uvCoordinates;
}

void ClipVS(	in RenderVSInput input,
				out RenderVSOutput vs,
				out RenderPSInput output,
				out TexturePSInput tex,
				out ClipPSInput clipData)
{
	RenderVS(input, vs, output, tex);
	clipData = GetClipData(output.position);
}

void RenderTextureAlphaNoDepthPS(in TexturePSInput tex,
						out float4 output : COLOR0)
{
	float4 color = tex2D(DiffuseSampler, tex.uvCoordinates);
	
	output.rgb = DiffuseColor.rgb * color.rgb;
	output.a = Alpha * color.a;
}

void ClipTextureAlphaNoDepthPS(in TexturePSInput tex,
						in ClipPSInput clipData,
						out float4 output : COLOR0)
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	RenderTextureAlphaNoDepthPS(tex, output);
}

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
		PixelShader = compile ps_3_0 RenderTextureAlphaNoDepthPS();
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
		PixelShader = compile ps_3_0 ClipTextureAlphaNoDepthPS();
	}
}
