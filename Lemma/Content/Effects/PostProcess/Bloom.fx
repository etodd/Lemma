#include "EffectCommon.fxh"
#include "BloomCommon.fxh"

// Samplers
float2 SourceDimensions0;
texture2D SourceTexture0;
sampler2D SourceSampler0 = sampler_state
{
	Texture = <SourceTexture0>;
	MinFilter = point;
	MagFilter = point;
	MipFilter = point;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float2 SourceDimensions1;
texture2D SourceTexture1;
sampler2D SourceSampler1 = sampler_state
{
	Texture = <SourceTexture1>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float3 Tint = float3(1, 1, 1);

float Gamma = 1.0f;
float Brightness = 0.0f;

texture1D RampTexture;
sampler1D RampSampler = sampler_state
{
	Texture = <RampTexture>;
	MinFilter = linear;
	MagFilter = linear;
	MipFilter = linear;
	MaxAnisotropy = 1;
	AddressU = CLAMP;
	AddressV = CLAMP;
};

float3 toneMap(float3 color)
{
	color.x = tex1D(RampSampler, color.x).x;
	color.y = tex1D(RampSampler, color.y).y;
	color.z = tex1D(RampSampler, color.z).z;
	return Brightness + color * Tint * Gamma;
}

const float GaussianKernel[16] = { 0.003829872f, 0.0088129551f, 0.0181463396f, 0.03343381f, 0.0551230286f, 0.0813255467f, 0.1073650667f, 0.1268369298f, 0.1340827751f, 0.1268369298f, 0.1073650667f, 0.0813255467f, 0.0551230286f, 0.03343381f, 0.0181463396f, 0.0088129551 };

void DownsamplePS(in PostProcessPSInput input, out float4 output : COLOR0)
{
	float2 pixelSize = 1.0f / SourceDimensions0;

	float2 pos = floor(input.texCoord * SourceDimensions0) * pixelSize;

	float3 bl = tex2D(SourceSampler0, pos + float2(0, 0)).rgb;
	float3 br = tex2D(SourceSampler0, pos + float2(pixelSize.x, 0)).rgb;
	float3 tl = tex2D(SourceSampler0, pos + float2(0, pixelSize.y)).rgb;
	float3 tr = tex2D(SourceSampler0, pos + pixelSize).rgb;

	output = float4(EncodeColor(toneMap(max(max(bl, br), max(tl, tr))) - BloomThreshold), 1);
}

void BlurHorizontalPS(	in PostProcessPSInput input,
						out float4 out_Color		: COLOR0)
{
	float xInterval = 1.0f / SourceDimensions0.x;

	float3 sum = 0;
	[unroll]
	for (int x = -8; x < 8; x++)
		sum += DecodeColor(tex2D(SourceSampler0, float2(input.texCoord.x + (x * xInterval), input.texCoord.y)).xyz) * GaussianKernel[x + 8];
	
	// Return the average color of all the samples
	out_Color.xyz = EncodeColor(sum);
	out_Color.w = 1.0f;
}

void BlurVerticalPS(	in PostProcessPSInput input,
						out float4 out_Color		: COLOR0)
{
	float yInterval = 1.0f / SourceDimensions0.y;

	float3 sum = 0;
	[unroll]
	for (int y = -8; y < 8; y++)
		sum += DecodeColor(tex2D(SourceSampler0, float2(input.texCoord.x, input.texCoord.y + (y * yInterval))).rgb) * GaussianKernel[y + 8];
	
	// Return the average color of all the samples
	out_Color.rgb = EncodeColor(sum);
	out_Color.w = 1.0f;
}

void CompositePS(	in PostProcessPSInput input,
					out float4 out_Color		: COLOR0)
{
	out_Color.rgb = toneMap(DecodeColor(tex2D(SourceSampler0, input.texCoord).rgb)) + (DecodeColor(tex2D(SourceSampler1, input.texCoord).rgb) / (1.0f - BloomThreshold));
	out_Color.a = 1.0f;
}

void ToneMapPS(	in PostProcessPSInput input,
					out float4 out_Color		: COLOR0)
{
	out_Color.rgb = toneMap(DecodeColor(tex2D(SourceSampler0, input.texCoord).rgb));
	out_Color.a = 1.0f;
}

technique Downsample
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 DownsamplePS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique BlurHorizontal
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 BlurHorizontalPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique BlurVertical
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 BlurVerticalPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique Composite
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 CompositePS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}

technique ToneMapOnly
{
	pass p0
	{
		VertexShader = compile vs_3_0 PostProcessVS();
		PixelShader = compile ps_3_0 ToneMapPS();
		
		ZEnable = false;
		ZWriteEnable = false;
		AlphaBlendEnable = false;
	}
}