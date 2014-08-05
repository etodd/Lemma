#include "ParticleCommon.fxh"

// Pixel shader for drawing particles.
float4 ParticlePS(VertexShaderOutput input) : COLOR0
{
	// Convert from clip space to UV coordinate space
	float2 uv = 0.5f * input.ClipSpacePosition.xy / input.ClipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;

	float depth = length(input.ViewSpacePosition);
	float depthDiff = tex2D(DepthSampler, uv).r - depth;
	clip(depthDiff);

	float4 color = tex2D(Sampler, input.TextureCoordinate) * input.Color;
	color.a *= min(1, depth * 0.1f) * min(1, depthDiff * 0.5f);
	return color;
}

float4 ClipParticlePS(VertexShaderOutput input, ClipPSInput clipData) : COLOR0
{
	HandleClipPlanes(clipData.clipPlaneDistances);
	return ParticlePS(input);
}

// Effect technique for drawing particles with additive blending.
technique RenderAdditiveParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 ParticleVS(true);
		PixelShader = compile ps_3_0 ParticlePS();
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = One;
		ZEnable = true;
		ZWriteEnable = false;
	}
}

// Effect technique for drawing particles with alpha blending.
technique RenderAlphaParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 ParticleVS(true);
		PixelShader = compile ps_3_0 ParticlePS();
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		ZEnable = true;
		ZWriteEnable = false;
	}
}

// Effect technique for drawing particles with additive blending.
technique ClipAdditiveParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 ClipParticleVS(true);
		PixelShader = compile ps_3_0 ClipParticlePS();
		AlphaBlendEnable = true;
		SrcBlend = One;
		DestBlend = One;
		ZEnable = true;
		ZWriteEnable = false;
	}
}

// Effect technique for drawing particles with alpha blending.
technique ClipAlphaParticles
{
	pass P0
	{
		VertexShader = compile vs_3_0 ClipParticleVS(true);
		PixelShader = compile ps_3_0 ClipParticlePS();
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		ZEnable = true;
		ZWriteEnable = false;
	}
}