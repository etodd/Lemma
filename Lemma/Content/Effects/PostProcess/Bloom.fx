#include "EffectCommon.fxh"

float GaussianKernel[16] = { 0.006579f, 0.015139f, 0.031172f, 0.057433f, 0.094691f, 0.139702f, 0.184433f, 0.217882f, 0.230329f, 0.217882f, 0.184433f, 0.139702f, 0.094691f, 0.057433f, 0.031172f, 0.015139f };

float BloomThreshold = 0.9f;

void BlurHorizontalPS(	in PostProcessPSInput input,
						out float4 out_Color		: COLOR0)
{
	float xInterval = 1.0f / SourceDimensions0.x;

	float3 sum = 0;
	[unroll]
	for (int x = -8; x < 8; x++)
		sum += saturate((DecodeColor(tex2D(PointSampler0, float2(input.texCoord.x + (x * xInterval), input.texCoord.y)).xyz) - BloomThreshold) / (1.0f - BloomThreshold)) * GaussianKernel[x + 8];
	
	// Return the average color of all the samples
	out_Color.xyz = sum;
	out_Color.w = 1.0f;
}

void CompositePS(	in PostProcessPSInput input,
					out float4 out_Color		: COLOR0)
{
	float yInterval = 1.0f / SourceDimensions1.y;

	float3 sum = 0;
	[unroll]
	for (int y = -8; y < 8; y++)
		sum += tex2D(LinearSampler1, float2(input.texCoord.x, input.texCoord.y + (y * yInterval))).xyz * GaussianKernel[y + 8];
	
	out_Color.xyz = (DecodeColor(tex2D(PointSampler0, input.texCoord).xyz) * (1.0f - saturate(sum))) + sum;
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