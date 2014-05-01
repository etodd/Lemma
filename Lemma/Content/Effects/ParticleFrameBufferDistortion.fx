#include "ParticleCommon.fxh"

// Frame buffer sampler
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

// Pixel shader for drawing particles.
float4 ParticlePS(VertexShaderOutput input) : COLOR0
{
	// Convert from clip space to UV coordinate space
	float2 uv = 0.5f * input.ClipSpacePosition.xy / input.ClipSpacePosition.w + float2(0.5f, 0.5f);
	uv.y = 1.0f - uv.y;

	clip(tex2D(DepthSampler, uv).r - length(input.ViewSpacePosition));

	float4 t = tex2D(Sampler, input.TextureCoordinate);
	clip(t.a - 0.1f);
	
	float alpha = t.a * input.Color.a;
	float2 distortion = ((t.xy * 2.0f) - 1.0f) * alpha * 0.12f;

	float4 color = tex2D(FrameSampler, uv + distortion) * input.Color;
	return float4(color.rgb, alpha);
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
		VertexShader = compile vs_3_0 ParticleVS();
		PixelShader = compile ps_3_0 ParticlePS();
		AlphaBlendEnable = true;
		SrcBlend = One;
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
		VertexShader = compile vs_3_0 ParticleVS();
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
		VertexShader = compile vs_3_0 ClipParticleVS();
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
		VertexShader = compile vs_3_0 ClipParticleVS();
		PixelShader = compile ps_3_0 ClipParticlePS();
		AlphaBlendEnable = true;
		SrcBlend = SrcAlpha;
		DestBlend = InvSrcAlpha;
		ZEnable = true;
		ZWriteEnable = false;
	}
}