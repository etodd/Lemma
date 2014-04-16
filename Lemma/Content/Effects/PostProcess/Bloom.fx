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

const float GaussianKernel[32] = { 0.00007674196886264266, 0.00018155522036679086, 0.0004063095905967308, 0.0008601574100431575, 0.001722547970551906, 0.0032631515508544876, 0.0058475735365225955, 0.009912579160885948, 0.0158953528849071, 0.024111609868575554, 0.03459830086448056, 0.046963003325653944, 0.06030168991413035, 0.07324460350487474, 0.08415778769784837, 0.09147144024753913, 0.0940479325354748, 0.09147144024753913, 0.08415778769784837, 0.07324460350487474, 0.06030168991413035, 0.046963003325653944, 0.03459830086448056, 0.024111609868575554, 0.0158953528849071, 0.009912579160885948, 0.0058475735365225955, 0.0032631515508544876, 0.001722547970551906, 0.0008601574100431575, 0.0004063095905967308, 0.00018155522036679086, };

void BlurHorizontalPS(	in PostProcessPSInput input,
						out float4 out_Color		: COLOR0)
{
	float xInterval = 1.0f / SourceDimensions0.x;

	float3 sum = 0;
	[unroll]
	for (int x = -16; x < 16; x++)
		sum += DecodeColor(tex2D(SourceSampler0, float2(input.texCoord.x + (x * xInterval), input.texCoord.y)).xyz) * GaussianKernel[x + 16];
	
	// Return the average color of all the samples
	out_Color.xyz = EncodeColor(sum);
	out_Color.w = 1.0f;
}

void CompositePS(	in PostProcessPSInput input,
					out float4 out_Color		: COLOR0)
{
	float yInterval = 1.0f / SourceDimensions1.y;

	float3 sum = 0;
	[unroll]
	for (int y = -16; y < 16; y++)
		sum += DecodeColor(tex2D(SourceSampler1, float2(input.texCoord.x, input.texCoord.y + (y * yInterval))).xyz) * GaussianKernel[y + 16];
	
	out_Color.xyz = DecodeColor(tex2D(SourceSampler0, input.texCoord).xyz) + sum / (1.0f - BloomThreshold);
	out_Color.w = 1.0f;
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